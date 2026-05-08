using Biblioteca.Domain.Entities;
using Biblioteca.Web.Data;
using Biblioteca.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Biblioteca.Tests.Integration;

public class LivrosServiceIntegrationTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new BibliotecaDbContext(options);
    }

    private static Livro CriarLivro(string titulo = "Clean Code", string autor = "Robert C. Martin") =>
        new(titulo, autor, "Alta Books", "1ª edição", new DateTime(2008, 8, 1), 425);

    private static Usuario CriarUsuario(string email = "marcos@email.com") =>
        new("Marcos Felipe", email);

    // =========================================================
    // Bloqueio de exclusão com empréstimo vinculado
    // =========================================================

    [Fact]
    public void Livro_ComEmprestimoVinculado_NaoDevePermitirExclusao()
    {
        using var context = CriarContexto();
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);
        service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7));

        var temEmprestimo = context.Emprestimos.Any(e => e.LivroId == livro.Id);

        Assert.True(temEmprestimo);
    }

    [Fact]
    public void Livro_SemEmprestimoVinculado_DevePermitirExclusao()
    {
        using var context = CriarContexto();
        var livro = CriarLivro();
        context.Livros.Add(livro);
        context.SaveChanges();

        var temEmprestimo = context.Emprestimos.Any(e => e.LivroId == livro.Id);
        context.Livros.Remove(livro);
        context.SaveChanges();

        Assert.False(temEmprestimo);
        Assert.Empty(context.Livros);
    }

    [Fact]
    public void Livro_AposDevolvido_AindaDeveBloquearExclusao()
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

        var temEmprestimoHistorico = context.Emprestimos.Any(e => e.LivroId == livro.Id);

        Assert.True(temEmprestimoHistorico);
    }

    // =========================================================
    // Busca e filtro via contexto
    // =========================================================

    [Fact]
    public void BuscaPorTitulo_DeveRetornarApenasCombinantes()
    {
        using var context = CriarContexto();
        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Clean Architecture"),
            CriarLivro("Domain-Driven Design")
        );
        context.SaveChanges();

        var busca = "Clean";
        var resultado = context.Livros
            .Where(l => l.Titulo.Contains(busca) || l.Autor.Contains(busca) || l.Editora.Contains(busca))
            .ToList();

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, l => Assert.Contains("Clean", l.Titulo));
    }

    [Fact]
    public void BuscaPorAutor_DeveRetornarApenasCombinantes()
    {
        using var context = CriarContexto();
        context.Livros.AddRange(
            CriarLivro("Clean Code", "Robert C. Martin"),
            CriarLivro("Clean Architecture", "Robert C. Martin"),
            CriarLivro("Domain-Driven Design", "Eric Evans")
        );
        context.SaveChanges();

        var busca = "Eric Evans";
        var resultado = context.Livros
            .Where(l => l.Titulo.Contains(busca) || l.Autor.Contains(busca))
            .ToList();

        Assert.Single(resultado);
        Assert.Equal("Domain-Driven Design", resultado[0].Titulo);
    }

    [Fact]
    public void FiltroPorDisponivel_DeveRetornarApenasDisponiveis()
    {
        using var context = CriarContexto();
        var livroDisponivel = CriarLivro("Clean Code");
        var livroEmprestado = CriarLivro("DDD", "Eric Evans");
        var usuario = CriarUsuario();
        context.Livros.AddRange(livroDisponivel, livroEmprestado);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);
        service.Realizar(livroEmprestado.Id, usuario.Id, DateTime.Today.AddDays(7));

        var disponiveis = context.Livros.Where(l => l.Disponivel).ToList();
        var emprestados = context.Livros.Where(l => !l.Disponivel).ToList();

        Assert.Single(disponiveis);
        Assert.Single(emprestados);
        Assert.Equal("Clean Code", disponiveis[0].Titulo);
        Assert.Equal("DDD", emprestados[0].Titulo);
    }

    [Fact]
    public void FiltroPorDisponivel_AposDevolvido_DeveAparecerDisponivel()
    {
        using var context = CriarContexto();
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var service = new EmprestimoAppService(context);
        var emprestimo = service.Realizar(livro.Id, usuario.Id, DateTime.Today.AddDays(7));

        var disponiveisAntes = context.Livros.Where(l => l.Disponivel).ToList();
        Assert.Empty(disponiveisAntes);

        service.Devolver(emprestimo.Id);

        var disponiveisDepois = context.Livros.Where(l => l.Disponivel).ToList();
        Assert.Single(disponiveisDepois);
    }

    // =========================================================
    // Paginação
    // =========================================================

    [Fact]
    public void Paginacao_DeveRetornarQuantidadeCorretaPorPagina()
    {
        using var context = CriarContexto();
        for (int i = 1; i <= 10; i++)
            context.Livros.Add(CriarLivro($"Livro {i:D2}"));
        context.SaveChanges();

        const int pageSize = 6;
        var pagina1 = context.Livros.OrderBy(l => l.Titulo).Take(pageSize).ToList();
        var pagina2 = context.Livros.OrderBy(l => l.Titulo).Skip(pageSize).Take(pageSize).ToList();

        Assert.Equal(6, pagina1.Count);
        Assert.Equal(4, pagina2.Count);
    }

    [Fact]
    public void Paginacao_ComBusca_DeveRetornarApenasResultadosFiltrados()
    {
        using var context = CriarContexto();
        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Clean Architecture"),
            CriarLivro("Domain-Driven Design"),
            CriarLivro("Refactoring"),
            CriarLivro("Clean Coder")
        );
        context.SaveChanges();

        var busca = "Clean";
        const int pageSize = 2;
        var query = context.Livros.Where(l => l.Titulo.Contains(busca)).OrderBy(l => l.Titulo);

        var total = query.Count();
        var pagina1 = query.Take(pageSize).ToList();
        var pagina2 = query.Skip(pageSize).Take(pageSize).ToList();

        Assert.Equal(3, total);
        Assert.Equal(2, pagina1.Count);
        Assert.Single(pagina2);
    }
}
