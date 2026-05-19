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

public class UsuariosControllerTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static UsuariosController CriarController(BibliotecaDbContext context)
    {
        var controller = new UsuariosController(
            context,
            NullLogger<UsuariosController>.Instance);

        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            new TempDataProviderFake());

        return controller;
    }

    private static Usuario CriarUsuario(
        string nome = "Marcos Felipe",
        string email = "marcos@email.com")
    {
        return new Usuario(nome, email);
    }

    private static Livro CriarLivro(
        string titulo = "Clean Code")
    {
        return new Livro(
            titulo,
            "Robert C. Martin",
            "Alta Books",
            "1ª edição",
            new DateTime(2008, 8, 1),
            425);
    }

    private static UsuarioFormViewModel CriarUsuarioFormViewModel(
        int id = 0,
        string nome = "Marcos Felipe",
        string email = "marcos@email.com")
    {
        return new UsuarioFormViewModel
        {
            Id = id,
            Nome = nome,
            Email = email
        };
    }

    // =========================================================
    // Index
    // =========================================================

    [Fact]
    public void Index_SemUsuarios_DeveRetornarListaVaziaComViewBagsPadrao()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.Model).ToList();

        Assert.Empty(usuarios);
        Assert.Equal(1, controller.ViewBag.CurrentPage);
        Assert.Equal(1, controller.ViewBag.TotalPages);
        Assert.False(controller.ViewBag.HasPreviousPage);
        Assert.False(controller.ViewBag.HasNextPage);
        Assert.Equal(0, controller.ViewBag.TotalUsuarios);
        Assert.Equal(0, controller.ViewBag.UsuariosInadimplentes);
    }

    [Fact]
    public void Index_ComMaisDeSeisUsuarios_DevePaginarPrimeiraPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 8; i++)
        {
            context.Usuarios.Add(CriarUsuario($"Usuário {i:D2}", $"usuario{i}@email.com"));
        }

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.Model).ToList();

        Assert.Equal(6, usuarios.Count);
        Assert.Equal(1, controller.ViewBag.CurrentPage);
        Assert.Equal(2, controller.ViewBag.TotalPages);
        Assert.False(controller.ViewBag.HasPreviousPage);
        Assert.True(controller.ViewBag.HasNextPage);
        Assert.Equal(8, controller.ViewBag.TotalUsuarios);
    }

    [Fact]
    public void Index_ComPaginaMaiorQueTotal_DeveAjustarParaUltimaPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 13; i++)
        {
            context.Usuarios.Add(CriarUsuario($"Usuário {i:D2}", $"usuario{i}@email.com"));
        }

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 99);

        var viewResult = Assert.IsType<ViewResult>(result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.Model).ToList();

        Assert.Single(usuarios);
        Assert.Equal(3, controller.ViewBag.CurrentPage);
        Assert.Equal(3, controller.ViewBag.TotalPages);
        Assert.True(controller.ViewBag.HasPreviousPage);
        Assert.False(controller.ViewBag.HasNextPage);
    }

    [Fact]
    public void Index_ComPaginaMenorQueUm_DeveAjustarParaPrimeiraPagina()
    {
        using var context = CriarContexto();

        context.Usuarios.AddRange(
            CriarUsuario("Marcos Felipe", "marcos@email.com"),
            CriarUsuario("Ana Silva", "ana@email.com"));

        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 0);

        var viewResult = Assert.IsType<ViewResult>(result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.Model).ToList();

        Assert.Equal(2, usuarios.Count);
        Assert.Equal(1, controller.ViewBag.CurrentPage);
        Assert.False(controller.ViewBag.HasPreviousPage);
    }

    [Fact]
    public void Index_ComEmprestimoAtrasadoAberto_DeveContarUsuarioInadimplente()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        DefinirDataPrevistaDevolucao(emprestimo, DateTime.Today.AddDays(-1));

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<Usuario>>(viewResult.Model).ToList();

        Assert.Single(usuarios);
        Assert.Equal(1, controller.ViewBag.UsuariosInadimplentes);
    }

    [Fact]
    public void Index_ComEmprestimoAtrasadoDevolvido_NaoDeveContarUsuarioInadimplente()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        DefinirDataPrevistaDevolucao(emprestimo, DateTime.Today.AddDays(-1));
        emprestimo.Devolver();

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        _ = controller.Index();

        Assert.Equal(0, controller.ViewBag.UsuariosInadimplentes);
    }

    // =========================================================
    // Create
    // =========================================================

    [Fact]
    public void Create_Get_DeveRetornarViewComUsuarioFormViewModel()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UsuarioFormViewModel>(viewResult.Model);

        Assert.Equal(0, model.Id);
        Assert.Equal(string.Empty, model.Nome);
        Assert.Equal(string.Empty, model.Email);
    }

    [Fact]
    public void Create_Post_ComModelStateInvalido_DeveRetornarViewComMesmoModel()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel();

        controller.ModelState.AddModelError(nameof(model.Nome), "Informe o nome.");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(context.Usuarios);
    }

    [Fact]
    public void Create_Post_ComDadosValidos_DeveAdicionarUsuarioERedirecionarParaIndex()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(
            nome: "Marcos Felipe",
            email: "MARCOS@EMAIL.COM");

        var result = controller.Create(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        var usuario = Assert.Single(context.Usuarios);

        Assert.Equal(nameof(UsuariosController.Index), redirectResult.ActionName);
        Assert.Equal("Marcos Felipe", usuario.Nome);
        Assert.Equal("marcos@email.com", usuario.Email);
        Assert.Equal(Messages.UsuarioAdicionado, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void Create_Post_ComEmailDuplicado_DeveAdicionarErroNoEmail()
    {
        using var context = CriarContexto();

        context.Usuarios.Add(CriarUsuario("Marcos Felipe", "marcos@email.com"));
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(
            nome: "Outro Usuário",
            email: "MARCOS@EMAIL.COM");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
        Assert.Contains(
            controller.ModelState[nameof(model.Email)]!.Errors,
            error => error.ErrorMessage == Messages.ErroEmailDuplicado);
        Assert.Single(context.Usuarios);
    }

    [Fact]
    public void Create_Post_ComNomeInvalido_DeveAdicionarErroNoCampoNome()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(nome: "   ", email: "marcos@email.com");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Nome)));
        Assert.Contains(
            controller.ModelState[nameof(model.Nome)]!.Errors,
            error => error.ErrorMessage == Messages.ErroNomeInvalido);
        Assert.Empty(context.Usuarios);
    }

    [Fact]
    public void Create_Post_ComEmailInvalido_DeveAdicionarErroNoCampoEmail()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(nome: "Marcos Felipe", email: "email-invalido");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
        Assert.Contains(
            controller.ModelState[nameof(model.Email)]!.Errors,
            error => error.ErrorMessage == Messages.ErroEmailInvalido);
        Assert.Empty(context.Usuarios);
    }

    // =========================================================
    // Edit GET
    // =========================================================

    [Fact]
    public void Edit_Get_ComIdExistente_DeveRetornarViewComModelPreenchido()
    {
        using var context = CriarContexto();
        var usuario = CriarUsuario("Marcos Felipe", "marcos@email.com");

        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Edit(usuario.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UsuarioFormViewModel>(viewResult.Model);

        Assert.Equal(usuario.Id, model.Id);
        Assert.Equal("Marcos Felipe", model.Nome);
        Assert.Equal("marcos@email.com", model.Email);
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
        var model = CriarUsuarioFormViewModel(id: 1);

        controller.ModelState.AddModelError(nameof(model.Nome), "Informe o nome.");

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
        var model = CriarUsuarioFormViewModel(id: 999);

        var result = controller.Edit(model);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Edit_Post_ComEmailDuplicadoDeOutroUsuario_DeveAdicionarErroNoEmail()
    {
        using var context = CriarContexto();

        var usuario1 = CriarUsuario("Marcos Felipe", "marcos@email.com");
        var usuario2 = CriarUsuario("Ana Silva", "ana@email.com");

        context.Usuarios.AddRange(usuario1, usuario2);
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(
            id: usuario2.Id,
            nome: "Ana Silva",
            email: "MARCOS@EMAIL.COM");

        var result = controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
        Assert.Contains(
            controller.ModelState[nameof(model.Email)]!.Errors,
            error => error.ErrorMessage == Messages.ErroEmailDuplicadoOutroUsuario);
    }

    [Fact]
    public void Edit_Post_ComMesmoEmailDoUsuario_DeveAtualizarUsuario()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario("Marcos Felipe", "marcos@email.com");
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(
            id: usuario.Id,
            nome: "Marcos Felipe França",
            email: "MARCOS@EMAIL.COM");

        var result = controller.Edit(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        var usuarioAtualizado = context.Usuarios.Single(u => u.Id == usuario.Id);

        Assert.Equal(nameof(UsuariosController.Index), redirectResult.ActionName);
        Assert.Equal("Marcos Felipe França", usuarioAtualizado.Nome);
        Assert.Equal("marcos@email.com", usuarioAtualizado.Email);
        Assert.Equal(Messages.UsuarioAtualizado, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void Edit_Post_ComEmailInvalido_DeveAdicionarErroNoCampoEmail()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario("Marcos Felipe", "marcos@email.com");
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(
            id: usuario.Id,
            nome: "Marcos Felipe",
            email: "email-invalido");

        var result = controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
        Assert.Contains(
            controller.ModelState[nameof(model.Email)]!.Errors,
            error => error.ErrorMessage == Messages.ErroEmailInvalido);
    }

    [Fact]
    public void Edit_Post_ComNomeInvalido_DeveAdicionarErroNoCampoNome()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario("Marcos Felipe", "marcos@email.com");
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarUsuarioFormViewModel(
            id: usuario.Id,
            nome: "   ",
            email: "marcos@email.com");

        var result = controller.Edit(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.Nome)));
        Assert.Contains(
            controller.ModelState[nameof(model.Nome)]!.Errors,
            error => error.ErrorMessage == Messages.ErroNomeInvalido);
    }

    // =========================================================
    // Delete GET
    // =========================================================

    [Fact]
    public void Delete_Get_ComIdExistenteSemEmprestimos_DeveRetornarViewComFlagsFalsas()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Delete(usuario.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Usuario>(viewResult.Model);

        Assert.Equal(usuario.Id, model.Id);
        Assert.False(controller.ViewBag.TemEmprestimoAtivo);
        Assert.False(controller.ViewBag.TemHistoricoEmprestimo);
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
    public void Delete_Get_ComEmprestimoAtivo_DeveRetornarFlagsVerdadeiras()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Delete(usuario.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Usuario>(viewResult.Model);

        Assert.Equal(usuario.Id, model.Id);
        Assert.True(controller.ViewBag.TemEmprestimoAtivo);
        Assert.True(controller.ViewBag.TemHistoricoEmprestimo);
    }

    [Fact]
    public void Delete_Get_ComHistoricoSemEmprestimoAtivo_DeveRetornarSomenteHistoricoVerdadeiro()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        emprestimo.Devolver();

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Delete(usuario.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Usuario>(viewResult.Model);

        Assert.Equal(usuario.Id, model.Id);
        Assert.False(controller.ViewBag.TemEmprestimoAtivo);
        Assert.True(controller.ViewBag.TemHistoricoEmprestimo);
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
    public void DeleteConfirmed_ComUsuarioSemEmprestimos_DeveRemoverUsuarioERedirecionarParaIndex()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.DeleteConfirmed(usuario.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(UsuariosController.Index), redirectResult.ActionName);
        Assert.Empty(context.Usuarios);
        Assert.Equal(Messages.UsuarioRemovido, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void DeleteConfirmed_ComEmprestimoAtivo_DeveBloquearExclusao()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.DeleteConfirmed(usuario.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(UsuariosController.Index), redirectResult.ActionName);
        Assert.Single(context.Usuarios);
        Assert.Equal(Messages.ErroUsuarioPossuiEmprestimoAtivo, controller.TempData["Erro"]);
    }

    [Fact]
    public void DeleteConfirmed_ComHistoricoSemEmprestimoAtivo_DeveBloquearExclusao()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        emprestimo.Devolver();

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.DeleteConfirmed(usuario.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(UsuariosController.Index), redirectResult.ActionName);
        Assert.Single(context.Usuarios);
        Assert.Equal(Messages.ErroUsuarioComHistoricoEmprestimo, controller.TempData["Erro"]);
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