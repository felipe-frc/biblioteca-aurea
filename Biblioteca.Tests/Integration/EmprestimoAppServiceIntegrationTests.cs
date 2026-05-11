using Biblioteca.Domain.Entities;
using Biblioteca.Domain.Enums;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Data;
using Biblioteca.Web.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Xunit;

namespace Biblioteca.Tests.Integration;

public class EmprestimoAppServiceIntegrationTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static Livro CriarLivro(string titulo = "Clean Code") =>
        new(titulo, "Robert C. Martin", "Alta Books", "1ª edição",
            new DateTime(2008, 8, 1), 425);

    private static Usuario CriarUsuario(string nome = "Marcos Felipe", string email = "marcos@email.com") =>
        new(nome, email);

    private static void DefinirDataPrevistaDevolucao(Emprestimo emprestimo, DateTime dataPrevistaDevolucao)
    {
        typeof(Emprestimo)
            .GetField("<DataPrevistaDevolucao>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(emprestimo, dataPrevistaDevolucao.Date);
    }

    // =========================================================
    // Realizar empréstimo
    // =========================================================

    [Fact]
    public void Realizar_DeveCriarEmprestimoEDeixarLivroIndisponivel()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7));

        Assert.NotEqual(0, emprestimo.Id);
        Assert.False(emprestimo.Livro.Disponivel);
        Assert.Single(context.Emprestimos);
        Assert.Equal(StatusEmprestimo.Ativo, emprestimo.Status);
    }

    [Fact]
    public void Realizar_DeveAssociarLivroEUsuarioCorretamente()
    {
        using var context = CriarContexto();

        var livro = CriarLivro("Domain-Driven Design");
        var usuario = CriarUsuario("Ana", "ana@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(14));

        Assert.Equal(livro.Id, emprestimo.LivroId);
        Assert.Equal(usuario.Id, emprestimo.UsuarioId);
    }

    [Fact]
    public void Realizar_MesmoLivro_SegundaVez_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario1 = CriarUsuario("Marcos", "marcos@email.com");
        var usuario2 = CriarUsuario("Ana", "ana@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario1);
        context.Usuarios.Add(usuario2);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        service.Realizar(livro.Id, usuario1.Id, DateTime.Today.AddDays(7));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(livro.Id, usuario2.Id, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroLivroNaoDisponivel, exception.Message);
    }

    [Fact]
    public void Realizar_QuandoLivroNaoExiste_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();

        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(999, usuario.Id, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroLivroNaoEncontrado, exception.Message);
    }

    [Fact]
    public void Realizar_QuandoUsuarioNaoExiste_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();

        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(livro.Id, 999, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroUsuarioNaoEncontrado, exception.Message);
    }

    [Fact]
    public void Realizar_DevePersistirEmprestimoNoBanco()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7));

        var doBanco = context.Emprestimos
            .Include(e => e.Livro)
            .Include(e => e.Usuario)
            .First(e => e.Id == emprestimo.Id);

        Assert.NotNull(doBanco);
        Assert.Equal(livro.Id, doBanco.LivroId);
        Assert.Equal(usuario.Id, doBanco.UsuarioId);
        Assert.False(doBanco.Livro.Disponivel);
    }

    [Fact]
    public void Realizar_QuandoUsuarioPossuiEmprestimoAtrasado_DeveBloquearNovoEmprestimo()
    {
        using var context = CriarContexto();

        var livroAtrasado = CriarLivro("Livro Atrasado");
        var livroNovo = CriarLivro("Livro Novo");
        var usuario = CriarUsuario("Marcos Felipe", "marcos@email.com");

        context.Livros.AddRange(livroAtrasado, livroNovo);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emprestimoAtrasado = service.Realizar(
            livroAtrasado.Id,
            usuario.Id,
            DateTime.Today.AddDays(7));

        DefinirDataPrevistaDevolucao(emprestimoAtrasado, DateTime.Today.AddDays(-1));

        context.Emprestimos.Update(emprestimoAtrasado);
        context.SaveChanges();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(
                livroNovo.Id,
                usuario.Id,
                DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroUsuarioPossuiEmprestimoAtrasado, exception.Message);
    }

    // =========================================================
    // Devolução
    // =========================================================

    [Fact]
    public void Devolver_DeveAtualizarDataDevolucaoELivroDisponivel()
    {
        using var context = CriarContexto();

        var livro = CriarLivro("DDD");
        var usuario = CriarUsuario("Ana", "ana@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(1));

        service.Devolver(emprestimo.Id);

        var atualizado = context.Emprestimos
            .Include(e => e.Livro)
            .First(e => e.Id == emprestimo.Id);

        Assert.NotNull(atualizado.DataDevolucao);
        Assert.True(atualizado.Livro.Disponivel);
        Assert.Equal(StatusEmprestimo.Devolvido, atualizado.Status);
    }

    [Fact]
    public void Devolver_DevePermitirNovoEmprestimoDoMesmoLivro()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario1 = CriarUsuario("Marcos", "marcos@email.com");
        var usuario2 = CriarUsuario("Ana", "ana@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario1);
        context.Usuarios.Add(usuario2);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var primeiroEmprestimo = service.Realizar(livro.Id, usuario1.Id, DateTime.Today.AddDays(7));

        service.Devolver(primeiroEmprestimo.Id);

        var segundoEmprestimo = service.Realizar(livro.Id, usuario2.Id, DateTime.Today.AddDays(7));

        Assert.NotNull(segundoEmprestimo);
        Assert.Equal(StatusEmprestimo.Ativo, segundoEmprestimo.Status);
        Assert.Equal(2, context.Emprestimos.Count());
    }

    [Fact]
    public void Devolver_QuandoEmprestimoNaoExiste_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var service = new EmprestimoAppService(context);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Devolver(999));

        Assert.Equal(Messages.ErroEmprestimoNaoEncontrado, exception.Message);
    }

    [Fact]
    public void Devolver_QuandoJaDevolvido_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7));
        service.Devolver(emprestimo.Id);

        Assert.Throws<InvalidOperationException>(() =>
            service.Devolver(emprestimo.Id));
    }

    // =========================================================
    // Fluxo completo
    // =========================================================

    [Fact]
    public void FluxoCompleto_Emprestimo_Devolucao_NovoEmprestimo_DeveSerValido()
    {
        using var context = CriarContexto();

        var livro = CriarLivro("Refactoring");
        var usuario1 = CriarUsuario("Marcos", "marcos@email.com");
        var usuario2 = CriarUsuario("Ana", "ana@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario1);
        context.Usuarios.Add(usuario2);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);

        var emp1 = service.Realizar(livro.Id, usuario1.Id, DateTime.Today.AddDays(7));

        Assert.False(livro.Disponivel);

        service.Devolver(emp1.Id);

        var livroAtualizado = context.Livros.First(l => l.Id == livro.Id);

        Assert.True(livroAtualizado.Disponivel);

        var emp2 = service.Realizar(livro.Id, usuario2.Id, DateTime.Today.AddDays(14));

        Assert.Equal(StatusEmprestimo.Ativo, emp2.Status);
        Assert.Equal(2, context.Emprestimos.Count());
    }
}
