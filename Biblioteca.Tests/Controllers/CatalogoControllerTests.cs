using Biblioteca.Domain.Entities;
using Biblioteca.Web.Controllers;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
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

    private static CatalogoIndexViewModel ObterIndexViewModel(IActionResult result)
    {
        var viewResult = Assert.IsType<ViewResult>(result);
        return Assert.IsType<CatalogoIndexViewModel>(viewResult.Model);
    }

    // =========================================================
    // Cenários base
    // =========================================================

    [Fact]
    public void Index_SemLivros_DeveRetornarViewModelVazioComUmaPagina()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Index();

        var model = ObterIndexViewModel(result);

        Assert.Empty(model.Livros);
        Assert.Equal(0, model.TotalLivros);
        Assert.Equal(0, model.TotalDisponiveis);
        Assert.Equal(0, model.TotalEmprestados);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(1, model.TotalPages);
        Assert.False(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
        Assert.Equal(string.Empty, model.Busca);
        Assert.Equal("todos", model.Disponibilidade);
        Assert.False(model.HasLivros);
        Assert.False(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(3, model.Livros.Count);
        Assert.Equal("Clean Code", model.Livros[0].Titulo);
        Assert.Equal("Domain-Driven Design", model.Livros[1].Titulo);
        Assert.Equal("Refactoring", model.Livros[2].Titulo);
        Assert.True(model.HasLivros);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(6, model.Livros.Count);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(2, model.TotalPages);
        Assert.False(model.HasPreviousPage);
        Assert.True(model.HasNextPage);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal("Livro 07", model.Livros[0].Titulo);
        Assert.Equal("Livro 08", model.Livros[1].Titulo);
        Assert.Equal(2, model.CurrentPage);
        Assert.Equal(2, model.TotalPages);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal(1, model.CurrentPage);
        Assert.False(model.HasPreviousPage);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Livro 13", model.Livros[0].Titulo);
        Assert.Equal(3, model.CurrentPage);
        Assert.Equal(3, model.TotalPages);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Clean Code", model.Livros[0].Titulo);
        Assert.Equal("Clean", model.Busca);
        Assert.Equal(1, model.TotalLivros);
        Assert.True(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Domain-Driven Design", model.Livros[0].Titulo);
        Assert.Equal("Eric", model.Busca);
        Assert.True(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("C# Essencial", model.Livros[0].Titulo);
        Assert.Equal("Casa", model.Busca);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Clean", model.Busca);
    }

    [Fact]
    public void Index_ComBuscaSemResultado_DeveRetornarViewModelVazio()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code"),
            CriarLivro("Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Livro Inexistente");

        var model = ObterIndexViewModel(result);

        Assert.Empty(model.Livros);
        Assert.Equal(0, model.TotalLivros);
        Assert.Equal(1, model.TotalPages);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal("Livro Inexistente", model.Busca);
        Assert.False(model.HasLivros);
        Assert.True(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal(string.Empty, model.Busca);
        Assert.False(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.All(model.Livros, livro => Assert.True(livro.Disponivel));
        Assert.Equal("disponiveis", model.Disponibilidade);
        Assert.Equal(2, model.TotalLivros);
        Assert.Equal(2, model.TotalDisponiveis);
        Assert.Equal(0, model.TotalEmprestados);
        Assert.True(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.All(model.Livros, livro => Assert.False(livro.Disponivel));
        Assert.Equal("emprestados", model.Disponibilidade);
        Assert.Equal(2, model.TotalLivros);
        Assert.Equal(0, model.TotalDisponiveis);
        Assert.Equal(2, model.TotalEmprestados);
        Assert.True(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal("todos", model.Disponibilidade);
        Assert.False(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.True(model.Livros[0].Disponivel);
        Assert.Equal("disponiveis", model.Disponibilidade);
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

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal("qualquer-coisa", model.Disponibilidade);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Clean Code", model.Livros[0].Titulo);
        Assert.True(model.Livros[0].Disponivel);
        Assert.Equal("Clean", model.Busca);
        Assert.Equal("disponiveis", model.Disponibilidade);
        Assert.Equal(1, model.TotalLivros);
        Assert.True(model.HasFiltros);
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

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Clean Architecture", model.Livros[0].Titulo);
        Assert.False(model.Livros[0].Disponivel);
        Assert.Equal("Clean", model.Busca);
        Assert.Equal("emprestados", model.Disponibilidade);
        Assert.Equal(1, model.TotalLivros);
        Assert.True(model.HasFiltros);
    }

    [Fact]
    public void Index_ComBuscaEFiltroSemResultado_DeveRetornarViewModelVazio()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro("Clean Code", disponivel: true),
            CriarLivro("Clean Architecture", disponivel: true),
            CriarLivro("Refactoring", disponivel: false));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Clean", disponibilidade: "emprestados");

        var model = ObterIndexViewModel(result);

        Assert.Empty(model.Livros);
        Assert.Equal(0, model.TotalLivros);
        Assert.Equal(0, model.TotalDisponiveis);
        Assert.Equal(0, model.TotalEmprestados);
        Assert.Equal(1, model.TotalPages);
        Assert.Equal(1, model.CurrentPage);
        Assert.False(model.HasLivros);
        Assert.True(model.HasFiltros);
    }
}