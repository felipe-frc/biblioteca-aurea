using Biblioteca.Domain.Entities;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Controllers;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Biblioteca.Tests.Controllers;

public class LivrosControllerTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static LivrosController CriarController(BibliotecaDbContext context)
    {
        var controller = new LivrosController(
            context,
            NullLogger<LivrosController>.Instance);

        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            new TempDataProviderFake());

        return controller;
    }

    private static Livro CriarLivro(
        string titulo = "Clean Code",
        string autor = "Robert C. Martin",
        string editora = "Alta Books",
        string edicao = "1ª edição",
        DateTime? dataPublicacao = null,
        int numeroPaginas = 425)
    {
        return new Livro(
            titulo,
            autor,
            editora,
            edicao,
            dataPublicacao ?? new DateTime(2008, 8, 1),
            numeroPaginas);
    }

    private static LivroFormViewModel CriarLivroFormViewModel(
        int id = 0,
        string titulo = "Clean Code",
        string autor = "Robert C. Martin",
        string editora = "Alta Books",
        string edicao = "1ª edição",
        DateTime? dataPublicacao = null,
        int? numeroPaginas = 425)
    {
        return new LivroFormViewModel
        {
            Id = id,
            Titulo = titulo,
            Autor = autor,
            Editora = editora,
            Edicao = edicao,
            DataPublicacao = dataPublicacao ?? new DateTime(2008, 8, 1),
            NumeroPaginas = numeroPaginas
        };
    }

    private static LivrosIndexViewModel ObterIndexViewModel(IActionResult result)
    {
        var viewResult = Assert.IsType<ViewResult>(result);
        return Assert.IsType<LivrosIndexViewModel>(viewResult.Model);
    }

    private static LivroDeleteViewModel ObterDeleteViewModel(IActionResult result)
    {
        var viewResult = Assert.IsType<ViewResult>(result);
        return Assert.IsType<LivroDeleteViewModel>(viewResult.Model);
    }

    // =========================================================
    // Index
    // =========================================================

    [Fact]
    public void Index_SemLivros_DeveRetornarViewModelVazioComValoresPadrao()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Index();

        var model = ObterIndexViewModel(result);

        Assert.Empty(model.Livros);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(1, model.TotalPages);
        Assert.False(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
        Assert.Equal(0, model.TotalLivros);
        Assert.Equal(0, model.TotalDisponiveis);
        Assert.Equal(0, model.TotalEmprestados);
        Assert.Equal("-", model.PublicacaoMaisRecente);
        Assert.Equal(string.Empty, model.Busca);
        Assert.Equal("todos", model.Disponibilidade);
        Assert.False(model.HasFiltros);
        Assert.False(model.HasLivros);
    }

    [Fact]
    public void Index_ComMaisDeSeisLivros_DevePaginarPrimeiraPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 8; i++)
        {
            context.Livros.Add(CriarLivro(titulo: $"Livro {i:D2}"));
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
        Assert.True(model.HasLivros);
    }

    [Fact]
    public void Index_ComPaginaMaiorQueTotal_DeveAjustarParaUltimaPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 13; i++)
        {
            context.Livros.Add(CriarLivro(titulo: $"Livro {i:D2}"));
        }

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 99);

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal(3, model.CurrentPage);
        Assert.Equal(3, model.TotalPages);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
    }

    [Fact]
    public void Index_ComPaginaMenorQueUm_DeveAjustarParaPrimeiraPagina()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Clean Code"),
            CriarLivro(titulo: "Refactoring"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 0);

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal(1, model.CurrentPage);
        Assert.False(model.HasPreviousPage);
    }

    [Fact]
    public void Index_ComBuscaPorTitulo_DeveRetornarLivroCorrespondente()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Clean Code"),
            CriarLivro(titulo: "Refactoring"),
            CriarLivro(titulo: "Domain-Driven Design"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Clean");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Clean Code", model.Livros[0].Titulo);
        Assert.Equal("Clean", model.Busca);
        Assert.True(model.HasFiltros);
    }

    [Fact]
    public void Index_ComBuscaPorAutor_DeveRetornarLivroCorrespondente()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Clean Code", autor: "Robert C. Martin"),
            CriarLivro(titulo: "Refactoring", autor: "Martin Fowler"),
            CriarLivro(titulo: "Domain-Driven Design", autor: "Eric Evans"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Eric");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Domain-Driven Design", model.Livros[0].Titulo);
    }

    [Fact]
    public void Index_ComBuscaPorEditora_DeveRetornarLivroCorrespondente()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Clean Code", editora: "Alta Books"),
            CriarLivro(titulo: "C# Essencial", editora: "Casa do Código"),
            CriarLivro(titulo: "Refactoring", editora: "Addison-Wesley"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "Casa");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("C# Essencial", model.Livros[0].Titulo);
    }

    [Fact]
    public void Index_ComBuscaComEspacos_DeveNormalizarBusca()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Clean Code"),
            CriarLivro(titulo: "Refactoring"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(busca: "  Clean  ");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Livros);
        Assert.Equal("Clean", model.Busca);
    }

    [Fact]
    public void Index_ComFiltroDisponiveis_DeveRetornarSomenteLivrosDisponiveis()
    {
        using var context = CriarContexto();

        var livroDisponivel1 = CriarLivro(titulo: "Livro Disponível 1");
        var livroDisponivel2 = CriarLivro(titulo: "Livro Disponível 2");
        var livroEmprestado = CriarLivro(titulo: "Livro Emprestado");
        livroEmprestado.MarcarComoEmprestado();

        context.Livros.AddRange(livroDisponivel1, livroDisponivel2, livroEmprestado);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "disponiveis");

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.All(model.Livros, livro => Assert.True(livro.Disponivel));
        Assert.Equal("disponiveis", model.Disponibilidade);
        Assert.Equal(2, model.TotalDisponiveis);
        Assert.Equal(0, model.TotalEmprestados);
        Assert.True(model.HasFiltros);
    }

    [Fact]
    public void Index_ComFiltroEmprestados_DeveRetornarSomenteLivrosEmprestados()
    {
        using var context = CriarContexto();

        var livroDisponivel = CriarLivro(titulo: "Livro Disponível");
        var livroEmprestado1 = CriarLivro(titulo: "Livro Emprestado 1");
        var livroEmprestado2 = CriarLivro(titulo: "Livro Emprestado 2");

        livroEmprestado1.MarcarComoEmprestado();
        livroEmprestado2.MarcarComoEmprestado();

        context.Livros.AddRange(livroDisponivel, livroEmprestado1, livroEmprestado2);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "emprestados");

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.All(model.Livros, livro => Assert.False(livro.Disponivel));
        Assert.Equal("emprestados", model.Disponibilidade);
        Assert.Equal(0, model.TotalDisponiveis);
        Assert.Equal(2, model.TotalEmprestados);
        Assert.True(model.HasFiltros);
    }

    [Fact]
    public void Index_ComDisponibilidadeVazia_DeveUsarTodos()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Clean Code"),
            CriarLivro(titulo: "Refactoring"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(disponibilidade: "   ");

        var model = ObterIndexViewModel(result);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal("todos", model.Disponibilidade);
        Assert.False(model.HasFiltros);
    }

    [Fact]
    public void Index_DeveCalcularPublicacaoMaisRecente()
    {
        using var context = CriarContexto();

        context.Livros.AddRange(
            CriarLivro(titulo: "Livro Antigo", dataPublicacao: new DateTime(2000, 1, 10)),
            CriarLivro(titulo: "Livro Novo", dataPublicacao: new DateTime(2020, 5, 15)));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var model = ObterIndexViewModel(result);

        Assert.Equal("15/05/2020", model.PublicacaoMaisRecente);
    }

    // =========================================================
    // Create
    // =========================================================

    [Fact]
    public void Create_Get_DeveRetornarViewComLivroFormViewModel()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LivroFormViewModel>(viewResult.Model);

        Assert.Equal(0, model.Id);
        Assert.Equal(string.Empty, model.Titulo);
        Assert.Equal(string.Empty, model.Autor);
        Assert.Equal(string.Empty, model.Editora);
        Assert.Equal(string.Empty, model.Edicao);
        Assert.Null(model.DataPublicacao);
        Assert.Null(model.NumeroPaginas);
    }

    [Fact]
    public void Create_Post_ComModelStateInvalido_DeveRetornarViewComMesmoModel()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarLivroFormViewModel();

        controller.ModelState.AddModelError(nameof(model.Titulo), "O título é obrigatório.");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(context.Livros);
    }

    [Fact]
    public void Create_Post_ComDadosValidos_DeveAdicionarLivroERedirecionarParaIndex()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarLivroFormViewModel(titulo: "Clean Code");

        var result = controller.Create(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        var livro = Assert.Single(context.Livros);

        Assert.Equal(nameof(LivrosController.Index), redirectResult.ActionName);
        Assert.Equal("Clean Code", livro.Titulo);
        Assert.Equal(Messages.LivroAdicionado, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void Create_Post_ComErroDeDominio_DeveAdicionarErroNoCampoCorreto()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarLivroFormViewModel(titulo: "   ");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Titulo)));
        Assert.Empty(context.Livros);
    }

    // =========================================================
    // Edit GET
    // =========================================================

    [Fact]
    public void Edit_Get_ComIdExistente_DeveRetornarViewComModelPreenchido()
    {
        using var context = CriarContexto();
        var livro = CriarLivro(titulo: "Clean Code");

        context.Livros.Add(livro);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Edit(livro.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LivroFormViewModel>(viewResult.Model);

        Assert.Equal(livro.Id, model.Id);
        Assert.Equal("Clean Code", model.Titulo);
        Assert.Equal(livro.Autor, model.Autor);
        Assert.Equal(livro.Editora, model.Editora);
        Assert.Equal(livro.Edicao, model.Edicao);
        Assert.Equal(livro.DataPublicacao, model.DataPublicacao);
        Assert.Equal(livro.NumeroPaginas, model.NumeroPaginas);
    }

    [Fact]
    public void Edit_Get_ComIdInexistente_DeveRetornarNotFound()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // =========================================================
    // Edit POST
    // =========================================================

    [Fact]
    public void Edit_Post_ComModelStateInvalido_DeveRetornarViewComMesmoModel()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarLivroFormViewModel(id: 1);

        controller.ModelState.AddModelError(nameof(model.Titulo), "O título é obrigatório.");

        var result = controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void Edit_Post_ComIdInexistente_DeveRetornarNotFound()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarLivroFormViewModel(id: 999);

        var result = controller.Edit(model);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Edit_Post_ComDadosValidos_DeveAtualizarLivroERedirecionarParaIndex()
    {
        using var context = CriarContexto();
        var livro = CriarLivro(titulo: "Clean Code");

        context.Livros.Add(livro);
        context.SaveChanges();

        var controller = CriarController(context);

        var model = CriarLivroFormViewModel(
            id: livro.Id,
            titulo: "Refactoring",
            autor: "Martin Fowler",
            editora: "Addison-Wesley",
            edicao: "2ª edição",
            dataPublicacao: new DateTime(2018, 11, 19),
            numeroPaginas: 448);

        var result = controller.Edit(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        var livroAtualizado = context.Livros.Single(l => l.Id == livro.Id);

        Assert.Equal(nameof(LivrosController.Index), redirectResult.ActionName);
        Assert.Equal("Refactoring", livroAtualizado.Titulo);
        Assert.Equal("Martin Fowler", livroAtualizado.Autor);
        Assert.Equal("Addison-Wesley", livroAtualizado.Editora);
        Assert.Equal("2ª edição", livroAtualizado.Edicao);
        Assert.Equal(new DateTime(2018, 11, 19), livroAtualizado.DataPublicacao);
        Assert.Equal(448, livroAtualizado.NumeroPaginas);
        Assert.Equal(Messages.LivroAtualizado, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void Edit_Post_ComErroDeDominio_DeveAdicionarErroNoCampoCorreto()
    {
        using var context = CriarContexto();
        var livro = CriarLivro();

        context.Livros.Add(livro);
        context.SaveChanges();

        var controller = CriarController(context);

        var model = CriarLivroFormViewModel(
            id: livro.Id,
            titulo: "Título Atualizado",
            autor: "Autor Atualizado",
            editora: "Editora Atualizada",
            edicao: "2ª edição",
            dataPublicacao: DateTime.Today.AddDays(1),
            numeroPaginas: 100);

        var result = controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.DataPublicacao)));
    }

    // =========================================================
    // Delete GET
    // =========================================================

    [Fact]
    public void Delete_Get_ComIdExistente_DeveRetornarViewModelComDadosDoLivro()
    {
        using var context = CriarContexto();
        var livro = CriarLivro();

        context.Livros.Add(livro);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Delete(livro.Id);

        var model = ObterDeleteViewModel(result);

        Assert.Equal(livro.Id, model.Id);
        Assert.Equal(livro.Titulo, model.Titulo);
        Assert.Equal(livro.Autor, model.Autor);
        Assert.Equal(livro.Editora, model.Editora);
        Assert.Equal(livro.Edicao, model.Edicao);
        Assert.Equal(livro.DataPublicacao, model.DataPublicacao);
        Assert.Equal(livro.NumeroPaginas, model.NumeroPaginas);
        Assert.Equal(livro.Disponivel, model.Disponivel);
        Assert.False(model.TemEmprestimoRelacionado);
        Assert.True(model.PodeExcluir);
    }

    [Fact]
    public void Delete_Get_ComIdInexistente_DeveRetornarNotFound()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Delete_Get_ComLivroComEmprestimoRelacionado_DeveRetornarViewModelComBloqueio()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Delete(livro.Id);

        var model = ObterDeleteViewModel(result);

        Assert.Equal(livro.Id, model.Id);
        Assert.True(model.TemEmprestimoRelacionado);
        Assert.False(model.PodeExcluir);
    }

    // =========================================================
    // DeleteConfirmed POST
    // =========================================================

    [Fact]
    public void DeleteConfirmed_ComIdInexistente_DeveRetornarNotFound()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.DeleteConfirmed(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void DeleteConfirmed_ComLivroSemEmprestimo_DeveRemoverLivroERedirecionarParaIndex()
    {
        using var context = CriarContexto();
        var livro = CriarLivro();

        context.Livros.Add(livro);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.DeleteConfirmed(livro.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(LivrosController.Index), redirectResult.ActionName);
        Assert.Empty(context.Livros);
        Assert.Equal(Messages.LivroRemovido, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void DeleteConfirmed_ComLivroComEmprestimoRelacionado_DeveBloquearExclusao()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        context.Livros.Add(livro);
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.DeleteConfirmed(livro.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(LivrosController.Index), redirectResult.ActionName);
        Assert.Single(context.Livros);
        Assert.Equal(Messages.ErroLivroComEmprestimo, controller.TempData["Erro"]);
    }

    private sealed class TempDataProviderFake : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}