using Biblioteca.Domain.Entities;
using Biblioteca.Web.Controllers;
using Biblioteca.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Biblioteca.Tests.Controllers;

public class CatalogoControllerTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static CatalogoController CriarController(BibliotecaDbContext context)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());

        return new CatalogoController(
            context,
            NullLogger<CatalogoController>.Instance,
            cache);
    }

    private static Livro CriarLivro(
        string titulo,
        string autor = "Robert C. Martin",
        string editora = "Alta Books",
        bool disponivel = true)
    {
        var livro = new Livro(
            titulo,
            autor,
            editora,
            "1ª edição",
            new DateTime(2008, 8, 1),
            425);

        if (!disponivel)
        {
            livro.MarcarComoEmprestado();
        }

        return livro;
    }

    // =========================================================
    // Cenários base
    // =========================================================

    [Fact]
    public void Index_SemLivros_DeveRetornarListaVaziaComUmaPagina()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Empty(livros);
        Assert.Equal(0, controller.ViewData["TotalLivros"]);
        Assert.Equal(0, controller.ViewData["TotalDisponiveis"]);
        Assert.Equal(0, controller.ViewData["TotalEmprestados"]);
        Assert.Equal(1, controller.ViewData["CurrentPage"]);
        Assert.Equal(1, controller.ViewData["TotalPages"]);
        Assert.Equal(false, controller.ViewData["HasPreviousPage"]);
        Assert.Equal(false, controller.ViewData["HasNextPage"]);
        Assert.Equal(string.Empty, controller.ViewData["Busca"]);
        Assert.Equal("todos", controller.ViewData["Disponibilidade"]);
    }

    [Fact]
    public void Index_ComLivros_DeveRetornarOrdenadoPorTitulo()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Refactoring"),
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(3, livros.Count);
        Assert.Equal("Clean Code", livros[0].Titulo);
        Assert.Equal("Domain-Driven Design", livros[1].Titulo);
        Assert.Equal("Refactoring", livros[2].Titulo);
    }

    [Fact]
    public void Index_ComMaisDeSeisLivros_DeveRetornarApenasPrimeiraPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 8; i++)
        {
            context.Livros.Add(CriarLivro($"Livro {i:D2}"));
        }

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(6, livros.Count);
        Assert.Equal(1, controller.ViewData["CurrentPage"]);
        Assert.Equal(2, controller.ViewData["TotalPages"]);
        Assert.Equal(false, controller.ViewData["HasPreviousPage"]);
        Assert.Equal(true, controller.ViewData["HasNextPage"]);
    }

    [Fact]
    public void Index_ComMaisDeSeisLivrosESegundaPagina_DeveRetornarItensRestantes()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 8; i++)
        {
            context.Livros.Add(CriarLivro($"Livro {i:D2}"));
        }

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 2);

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.Equal("Livro 07", livros[0].Titulo);
        Assert.Equal("Livro 08", livros[1].Titulo);
        Assert.Equal(2, controller.ViewData["CurrentPage"]);
        Assert.Equal(2, controller.ViewData["TotalPages"]);
        Assert.Equal(true, controller.ViewData["HasPreviousPage"]);
        Assert.Equal(false, controller.ViewData["HasNextPage"]);
    }

    [Fact]
    public void Index_ComPaginaMenorQueUm_DeveAjustarParaPrimeiraPagina()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 0);

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.Equal(1, controller.ViewData["CurrentPage"]);
        Assert.Equal(false, controller.ViewData["HasPreviousPage"]);
    }

    [Fact]
    public void Index_ComPaginaMaiorQueTotal_DeveAjustarParaUltimaPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 13; i++)
        {
            context.Livros.Add(CriarLivro($"Livro {i:D2}"));
        }

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 99);

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("Livro 13", livros[0].Titulo);
        Assert.Equal(3, controller.ViewData["CurrentPage"]);
        Assert.Equal(3, controller.ViewData["TotalPages"]);
        Assert.Equal(true, controller.ViewData["HasPreviousPage"]);
        Assert.Equal(false, controller.ViewData["HasNextPage"]);
    }

    // =========================================================
    // Busca
    // =========================================================

    [Fact]
    public void Index_ComBuscaPorTitulo_DeveRetornarLivrosCorrespondentes()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"),
            CriarLivro("Refactoring"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Clean");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("Clean Code", livros[0].Titulo);
        Assert.Equal("Clean", controller.ViewData["Busca"]);
        Assert.Equal(1, controller.ViewData["TotalLivros"]);
    }

    [Fact]
    public void Index_ComBuscaPorAutor_DeveRetornarLivrosCorrespondentes()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code", autor: "Robert C. Martin"),
            CriarLivro("Domain-Driven Design", autor: "Eric Evans"),
            CriarLivro("Refactoring", autor: "Martin Fowler"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Eric");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("Domain-Driven Design", livros[0].Titulo);
        Assert.Equal("Eric", controller.ViewData["Busca"]);
    }

    [Fact]
    public void Index_ComBuscaPorEditora_DeveRetornarLivrosCorrespondentes()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code", editora: "Alta Books"),
            CriarLivro("C# Essencial", editora: "Casa do Código"),
            CriarLivro("Refactoring", editora: "Addison-Wesley"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Casa");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("C# Essencial", livros[0].Titulo);
        Assert.Equal("Casa", controller.ViewData["Busca"]);
    }

    [Fact]
    public void Index_ComBuscaComEspacos_DeveNormalizarBusca()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "  Clean  ");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("Clean", controller.ViewData["Busca"]);
    }

    [Fact]
    public void Index_ComBuscaSemResultado_DeveRetornarListaVazia()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Livro Inexistente");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Empty(livros);
        Assert.Equal(0, controller.ViewData["TotalLivros"]);
        Assert.Equal(1, controller.ViewData["TotalPages"]);
        Assert.Equal(1, controller.ViewData["CurrentPage"]);
        Assert.Equal("Livro Inexistente", controller.ViewData["Busca"]);
    }

    [Fact]
    public void Index_ComBuscaVazia_DeveRetornarTodosOsLivros()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "   ");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.Equal(string.Empty, controller.ViewData["Busca"]);
    }

    // =========================================================
    // Filtros
    // =========================================================

    [Fact]
    public void Index_ComFiltroDisponiveis_DeveRetornarSomenteLivrosDisponiveis()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Livro Disponível 1"),
            CriarLivro("Livro Disponível 2"),
            CriarLivro("Livro Emprestado", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "disponiveis");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.All(livros, livro => Assert.True(livro.Disponivel));
        Assert.Equal("disponiveis", controller.ViewData["Disponibilidade"]);
        Assert.Equal(2, controller.ViewData["TotalLivros"]);
        Assert.Equal(2, controller.ViewData["TotalDisponiveis"]);
        Assert.Equal(0, controller.ViewData["TotalEmprestados"]);
    }

    [Fact]
    public void Index_ComFiltroEmprestados_DeveRetornarSomenteLivrosEmprestados()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Livro Disponível"),
            CriarLivro("Livro Emprestado 1", disponivel: false),
            CriarLivro("Livro Emprestado 2", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "emprestados");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.All(livros, livro => Assert.False(livro.Disponivel));
        Assert.Equal("emprestados", controller.ViewData["Disponibilidade"]);
        Assert.Equal(2, controller.ViewData["TotalLivros"]);
        Assert.Equal(0, controller.ViewData["TotalDisponiveis"]);
        Assert.Equal(2, controller.ViewData["TotalEmprestados"]);
    }

    [Fact]
    public void Index_ComDisponibilidadeVazia_DeveUsarFiltroTodos()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Livro Disponível"),
            CriarLivro("Livro Emprestado", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "   ");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.Equal("todos", controller.ViewData["Disponibilidade"]);
    }

    [Fact]
    public void Index_ComDisponibilidadeMaiusculaEComEspacos_DeveNormalizarDisponibilidade()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Livro Disponível"),
            CriarLivro("Livro Emprestado", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "  DISPONIVEIS  ");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.True(livros[0].Disponivel);
        Assert.Equal("disponiveis", controller.ViewData["Disponibilidade"]);
    }

    [Fact]
    public void Index_ComDisponibilidadeInvalida_DeveRetornarTodos()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Livro Disponível"),
            CriarLivro("Livro Emprestado", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "qualquer-coisa");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Equal(2, livros.Count);
        Assert.Equal("qualquer-coisa", controller.ViewData["Disponibilidade"]);
    }

    // =========================================================
    // Busca + filtro combinados
    // =========================================================

    [Fact]
    public void Index_ComBuscaEFiltroDisponiveis_DeveAplicarAmbosOsFiltros()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code", disponivel: true),
            CriarLivro("Clean Architecture", disponivel: false),
            CriarLivro("Refactoring", disponivel: true));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Clean", disponibilidade: "disponiveis");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("Clean Code", livros[0].Titulo);
        Assert.True(livros[0].Disponivel);
        Assert.Equal("Clean", controller.ViewData["Busca"]);
        Assert.Equal("disponiveis", controller.ViewData["Disponibilidade"]);
        Assert.Equal(1, controller.ViewData["TotalLivros"]);
    }

    [Fact]
    public void Index_ComBuscaEFiltroEmprestados_DeveAplicarAmbosOsFiltros()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code", disponivel: true),
            CriarLivro("Clean Architecture", disponivel: false),
            CriarLivro("Refactoring", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Clean", disponibilidade: "emprestados");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Single(livros);
        Assert.Equal("Clean Architecture", livros[0].Titulo);
        Assert.False(livros[0].Disponivel);
        Assert.Equal("Clean", controller.ViewData["Busca"]);
        Assert.Equal("emprestados", controller.ViewData["Disponibilidade"]);
        Assert.Equal(1, controller.ViewData["TotalLivros"]);
    }

    [Fact]
    public void Index_ComBuscaEFiltroSemResultado_DeveRetornarListaVazia()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code", disponivel: true),
            CriarLivro("Clean Architecture", disponivel: true),
            CriarLivro("Refactoring", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Clean", disponibilidade: "emprestados");

        var viewResult = Assert.IsType<ViewResult>(result);
        var livros = Assert.IsAssignableFrom<IEnumerable<Livro>>(viewResult.Model).ToList();

        Assert.Empty(livros);
        Assert.Equal(0, controller.ViewData["TotalLivros"]);
        Assert.Equal(0, controller.ViewData["TotalDisponiveis"]);
        Assert.Equal(0, controller.ViewData["TotalEmprestados"]);
        Assert.Equal(1, controller.ViewData["TotalPages"]);
        Assert.Equal(1, controller.ViewData["CurrentPage"]);
    }
}