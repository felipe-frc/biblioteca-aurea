using Biblioteca.Domain.Entities;
using Biblioteca.Domain.Enums;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Data;
using Biblioteca.Web.Data.UnitOfWork;
using Biblioteca.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biblioteca.Tests.Services;

public class EmprestimoAppServiceTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static Livro CriarLivro(string titulo = "Clean Code")
    {
        return new Livro(
            titulo,
            "Robert C. Martin",
            "Alta Books",
            "1ª edição",
            new DateTime(2008, 8, 1),
            425);
    }

    private static Usuario CriarUsuario(
        string nome = "Marcos Felipe",
        string email = "marcos@email.com")
    {
        return new Usuario(nome, email);
    }

    // =========================================================
    // Realizar empréstimo
    // =========================================================

    [Fact]
    public void Realizar_ComDadosValidos_DeveCriarEmprestimo()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7));

        Assert.NotEqual(0, emprestimo.Id);
        Assert.Equal(livro.Id, emprestimo.LivroId);
        Assert.Equal(usuario.Id, emprestimo.UsuarioId);
        Assert.Equal(DateTime.Today.AddDays(7), emprestimo.DataPrevistaDevolucao);
        Assert.Equal(StatusEmprestimo.Ativo, emprestimo.Status);
        Assert.Null(emprestimo.DataDevolucao);

        var livroAtualizado = context.Livros.Single(l => l.Id == livro.Id);
        Assert.False(livroAtualizado.Disponivel);

        Assert.Single(context.Emprestimos);
    }

    [Fact]
    public void Realizar_ComLivroInexistente_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(999, usuario.Id, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroLivroNaoEncontrado, exception.Message);
        Assert.Empty(context.Emprestimos);
    }

    [Fact]
    public void Realizar_ComUsuarioInexistente_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(livro.Id, 999, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroUsuarioNaoEncontrado, exception.Message);
        Assert.Empty(context.Emprestimos);
    }

    [Fact]
    public void Realizar_ComLivroIndisponivel_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        livro.MarcarComoEmprestado();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroLivroNaoDisponivel, exception.Message);
        Assert.Empty(context.Emprestimos);
    }

    [Fact]
    public void Realizar_ComUsuarioComEmprestimoAtrasado_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();

        var livroAtrasado = CriarLivro("Livro Atrasado");
        var livroNovo = CriarLivro("Livro Novo");

        context.Usuarios.Add(usuario);
        context.Livros.AddRange(livroAtrasado, livroNovo);
        context.SaveChanges();

        var emprestimoAtrasado = new Emprestimo(
            livroAtrasado,
            usuario,
            DateTime.Today.AddDays(7));

        DefinirDataPrevistaDevolucao(emprestimoAtrasado, DateTime.Today.AddDays(-1));

        context.Emprestimos.Add(emprestimoAtrasado);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(livroNovo.Id, usuario.Id, DateTime.Today.AddDays(7)));

        Assert.Equal(Messages.ErroUsuarioPossuiEmprestimoAtrasado, exception.Message);
        Assert.Equal(1, context.Emprestimos.Count());
    }

    [Fact]
    public void Realizar_ComUsuarioComEmprestimoAtivoDoMesmoLivro_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimoAtivo = new Emprestimo(
            livro,
            usuario,
            DateTime.Today.AddDays(7));

        livro.MarcarComoDisponivel();

        context.Emprestimos.Add(emprestimoAtivo);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(10)));

        Assert.Equal(Messages.ErroUsuarioComEmprestimoAtivo, exception.Message);
        Assert.Single(context.Emprestimos);
    }

    [Fact]
    public void Realizar_ComDataPrevistaNoPassado_DeveLancarArgumentException()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<ArgumentException>(() =>
            service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(-1)));

        Assert.Equal("dataPrevistaDevolucao", exception.ParamName);
        Assert.Empty(context.Emprestimos);
    }

    [Fact]
    public void Realizar_ComDataPrevistaMaiorQue365Dias_DeveLancarArgumentException()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<ArgumentException>(() =>
            service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(366)));

        Assert.Equal("dataPrevistaDevolucao", exception.ParamName);
        Assert.Empty(context.Emprestimos);
    }

    // =========================================================
    // Devolver empréstimo
    // =========================================================

    [Fact]
    public void Devolver_ComEmprestimoValido_DeveRegistrarDevolucao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        service.Devolver(emprestimo.Id);

        var emprestimoAtualizado = context.Emprestimos
            .Include(e => e.Livro)
            .Single(e => e.Id == emprestimo.Id);

        Assert.NotNull(emprestimoAtualizado.DataDevolucao);
        Assert.Equal(StatusEmprestimo.Devolvido, emprestimoAtualizado.Status);
        Assert.True(emprestimoAtualizado.Livro.Disponivel);
    }

    [Fact]
    public void Devolver_ComEmprestimoInexistente_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Devolver(999));

        Assert.Equal(Messages.ErroEmprestimoNaoEncontrado, exception.Message);
    }

    [Fact]
    public void Devolver_ComEmprestimoJaDevolvido_DeveLancarExcecao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        emprestimo.Devolver();

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Devolver(emprestimo.Id));

        Assert.Equal("Este empréstimo já foi devolvido.", exception.Message);
    }

    [Fact]
    public void Devolver_ComEmprestimoAtrasado_DeveRegistrarDevolvidoComAtraso()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = CriarUsuario();

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        DefinirDataPrevistaDevolucao(emprestimo, DateTime.Today.AddDays(-2));

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var service = new EmprestimoAppService(new UnitOfWork(context));

        service.Devolver(emprestimo.Id);

        var emprestimoAtualizado = context.Emprestimos
            .Include(e => e.Livro)
            .Single(e => e.Id == emprestimo.Id);

        Assert.NotNull(emprestimoAtualizado.DataDevolucao);
        Assert.Equal(StatusEmprestimo.DevolvidoComAtraso, emprestimoAtualizado.Status);
        Assert.True(emprestimoAtualizado.Livro.Disponivel);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static void DefinirDataPrevistaDevolucao(Emprestimo emprestimo, DateTime dataPrevistaDevolucao)
    {
        var propriedade = typeof(Emprestimo).GetProperty(nameof(Emprestimo.DataPrevistaDevolucao));

        if (propriedade is null)
            throw new InvalidOperationException("Propriedade DataPrevistaDevolucao não encontrada.");

        propriedade.SetValue(emprestimo, dataPrevistaDevolucao.Date);
    }
}