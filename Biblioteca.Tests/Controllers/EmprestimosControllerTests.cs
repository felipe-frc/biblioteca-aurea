using Biblioteca.Domain.Entities;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Controllers;
using Biblioteca.Web.Data;
using Biblioteca.Web.Services;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Biblioteca.Tests.Controllers;

public class EmprestimosControllerTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static EmprestimosController CriarController(
        BibliotecaDbContext context,
        IEmprestimoAppService? service = null)
    {
        var controller = new EmprestimosController(
            context,
            NullLogger<EmprestimosController>.Instance,
            service ?? new EmprestimoAppServiceFake());

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

    private static Emprestimo CriarEmprestimo(
        Livro livro,
        Usuario usuario,
        DateTime? dataPrevista = null)
    {
        return new Emprestimo(
            livro,
            usuario,
            dataPrevista ?? DateTime.Today.AddDays(7));
    }

    private static EmprestimoFormViewModel CriarEmprestimoFormViewModel(
        int? usuarioId = 1,
        int? livroId = 1,
        DateTime? dataPrevista = null)
    {
        return new EmprestimoFormViewModel
        {
            UsuarioId = usuarioId,
            LivroId = livroId,
            DataPrevistaDevolucao = dataPrevista ?? DateTime.Today.AddDays(7)
        };
    }

    private static EmprestimosIndexViewModel ObterIndexViewModel(IActionResult result)
    {
        var viewResult = Assert.IsType<ViewResult>(result);
        return Assert.IsType<EmprestimosIndexViewModel>(viewResult.Model);
    }

    // =========================================================
    // Index
    // =========================================================

    [Fact]
    public void Index_SemEmprestimos_DeveRetornarViewModelVazioComValoresPadrao()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Index();

        var model = ObterIndexViewModel(result);

        Assert.Empty(model.Emprestimos);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(1, model.TotalPages);
        Assert.False(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
        Assert.Equal(0, model.TotalEmprestimos);
        Assert.Equal(0, model.Ativos);
        Assert.Equal(0, model.Atrasados);
        Assert.Equal(0, model.Devolvidos);
        Assert.Equal("todos", model.FiltroStatus);
        Assert.False(model.HasEmprestimos);
        Assert.False(model.HasFiltroStatus);
    }

    [Fact]
    public void Index_ComMaisDeSeisEmprestimos_DevePaginarPrimeiraPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 8; i++)
        {
            var usuario = CriarUsuario($"Usuário {i:D2}", $"usuario{i}@email.com");
            var livro = CriarLivro($"Livro {i:D2}");

            context.Usuarios.Add(usuario);
            context.Livros.Add(livro);
            context.SaveChanges();

            context.Emprestimos.Add(CriarEmprestimo(livro, usuario));
            context.SaveChanges();
        }

        var controller = CriarController(context);

        var result = controller.Index();

        var model = ObterIndexViewModel(result);

        Assert.Equal(6, model.Emprestimos.Count);
        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(2, model.TotalPages);
        Assert.False(model.HasPreviousPage);
        Assert.True(model.HasNextPage);
        Assert.Equal(8, model.TotalEmprestimos);
        Assert.True(model.HasEmprestimos);
    }

    [Fact]
    public void Index_ComPaginaMaiorQueTotal_DeveAjustarParaUltimaPagina()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 13; i++)
        {
            var usuario = CriarUsuario($"Usuário {i:D2}", $"usuario{i}@email.com");
            var livro = CriarLivro($"Livro {i:D2}");

            context.Usuarios.Add(usuario);
            context.Livros.Add(livro);
            context.SaveChanges();

            context.Emprestimos.Add(CriarEmprestimo(livro, usuario));
            context.SaveChanges();
        }

        var controller = CriarController(context);

        var result = controller.Index(page: 99);

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Emprestimos);
        Assert.Equal(3, model.CurrentPage);
        Assert.Equal(3, model.TotalPages);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
    }

    [Fact]
    public void Index_ComPaginaMenorQueUm_DeveAjustarParaPrimeiraPagina()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        context.Emprestimos.Add(CriarEmprestimo(livro, usuario));
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(page: 0);

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Emprestimos);
        Assert.Equal(1, model.CurrentPage);
        Assert.False(model.HasPreviousPage);
    }

    [Fact]
    public void Index_ComFiltroAtivos_DeveRetornarSomenteEmprestimosAtivos()
    {
        using var context = CriarContexto();

        var usuario1 = CriarUsuario("Usuário 1", "usuario1@email.com");
        var livro1 = CriarLivro("Livro 1");

        var usuario2 = CriarUsuario("Usuário 2", "usuario2@email.com");
        var livro2 = CriarLivro("Livro 2");

        context.Usuarios.AddRange(usuario1, usuario2);
        context.Livros.AddRange(livro1, livro2);
        context.SaveChanges();

        var emprestimoAtivo = CriarEmprestimo(livro1, usuario1, DateTime.Today.AddDays(3));
        var emprestimoAtrasado = CriarEmprestimo(livro2, usuario2, DateTime.Today.AddDays(3));
        DefinirDataPrevistaDevolucao(emprestimoAtrasado, DateTime.Today.AddDays(-1));

        context.Emprestimos.AddRange(emprestimoAtivo, emprestimoAtrasado);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(filtroStatus: "ativos");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Emprestimos);
        Assert.Equal(emprestimoAtivo.Id, model.Emprestimos[0].Id);
        Assert.Equal("ativos", model.FiltroStatus);
        Assert.Equal(1, model.TotalEmprestimos);
        Assert.Equal(1, model.Ativos);
        Assert.Equal(1, model.Atrasados);
        Assert.Equal(0, model.Devolvidos);
        Assert.True(model.HasFiltroStatus);
    }

    [Fact]
    public void Index_ComFiltroAtrasados_DeveRetornarSomenteEmprestimosAtrasados()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = CriarEmprestimo(livro, usuario, DateTime.Today.AddDays(7));
        DefinirDataPrevistaDevolucao(emprestimo, DateTime.Today.AddDays(-2));

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(filtroStatus: "atrasados");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Emprestimos);
        Assert.Equal(emprestimo.Id, model.Emprestimos[0].Id);
        Assert.Equal("atrasados", model.FiltroStatus);
        Assert.Equal(1, model.Atrasados);
        Assert.True(model.HasFiltroStatus);
    }

    [Fact]
    public void Index_ComFiltroDevolvidos_DeveRetornarSomenteEmprestimosDevolvidos()
    {
        using var context = CriarContexto();

        var usuario1 = CriarUsuario("Usuário 1", "usuario1@email.com");
        var livro1 = CriarLivro("Livro 1");

        var usuario2 = CriarUsuario("Usuário 2", "usuario2@email.com");
        var livro2 = CriarLivro("Livro 2");

        context.Usuarios.AddRange(usuario1, usuario2);
        context.Livros.AddRange(livro1, livro2);
        context.SaveChanges();

        var emprestimoAtivo = CriarEmprestimo(livro1, usuario1);
        var emprestimoDevolvido = CriarEmprestimo(livro2, usuario2);
        emprestimoDevolvido.Devolver();

        context.Emprestimos.AddRange(emprestimoAtivo, emprestimoDevolvido);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(filtroStatus: "devolvidos");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Emprestimos);
        Assert.Equal(emprestimoDevolvido.Id, model.Emprestimos[0].Id);
        Assert.Equal("devolvidos", model.FiltroStatus);
        Assert.Equal(1, model.Devolvidos);
        Assert.True(model.HasFiltroStatus);
    }

    [Fact]
    public void Index_ComFiltroVazio_DeveUsarTodos()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        context.Emprestimos.Add(CriarEmprestimo(livro, usuario));
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index(filtroStatus: "   ");

        var model = ObterIndexViewModel(result);

        Assert.Single(model.Emprestimos);
        Assert.Equal("todos", model.FiltroStatus);
        Assert.False(model.HasFiltroStatus);
    }

    // =========================================================
    // Create GET
    // =========================================================

    [Fact]
    public void Create_Get_DeveRetornarViewComCombosCarregados()
    {
        using var context = CriarContexto();

        var usuarioB = CriarUsuario("Bruno", "bruno@email.com");
        var usuarioA = CriarUsuario("Ana", "ana@email.com");

        var livroB = CriarLivro("Livro B");
        var livroA = CriarLivro("Livro A");
        var livroIndisponivel = CriarLivro("Livro Indisponível");
        livroIndisponivel.MarcarComoEmprestado();

        context.Usuarios.AddRange(usuarioB, usuarioA);
        context.Livros.AddRange(livroB, livroA, livroIndisponivel);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EmprestimoFormViewModel>(viewResult.Model);

        Assert.Equal(2, model.Usuarios.Count);
        Assert.Equal("Ana", model.Usuarios[0].Text);
        Assert.Equal("Bruno", model.Usuarios[1].Text);

        Assert.Equal(2, model.Livros.Count);
        Assert.Equal("Livro A", model.Livros[0].Text);
        Assert.Equal("Livro B", model.Livros[1].Text);
        Assert.DoesNotContain(model.Livros, item => item.Text == "Livro Indisponível");
    }

    // =========================================================
    // Create POST
    // =========================================================

    [Fact]
    public void Create_Post_ComModelStateInvalido_DeveRetornarViewComCombosCarregados()
    {
        using var context = CriarContexto();

        context.Usuarios.Add(CriarUsuario());
        context.Livros.Add(CriarLivro());
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarEmprestimoFormViewModel();

        controller.ModelState.AddModelError(nameof(model.UsuarioId), "Selecione um usuário.");

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        var retorno = Assert.IsType<EmprestimoFormViewModel>(viewResult.Model);

        Assert.Same(model, retorno);
        Assert.False(controller.ModelState.IsValid);
        Assert.NotEmpty(retorno.Usuarios);
        Assert.NotEmpty(retorno.Livros);
    }

    [Fact]
    public void Create_Post_ComUsuarioInexistente_DeveAdicionarErroNoUsuario()
    {
        using var context = CriarContexto();

        var livro = CriarLivro();

        context.Livros.Add(livro);
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarEmprestimoFormViewModel(usuarioId: 999, livroId: livro.Id);

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.UsuarioId)));
        Assert.Contains(
            controller.ModelState[nameof(model.UsuarioId)]!.Errors,
            error => error.ErrorMessage == Messages.ErroUsuarioInvalido);
    }

    [Fact]
    public void Create_Post_ComLivroInexistente_DeveAdicionarErroNoLivro()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();

        context.Usuarios.Add(usuario);
        context.SaveChanges();

        var controller = CriarController(context);
        var model = CriarEmprestimoFormViewModel(usuarioId: usuario.Id, livroId: 999);

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.LivroId)));
        Assert.Contains(
            controller.ModelState[nameof(model.LivroId)]!.Errors,
            error => error.ErrorMessage == Messages.ErroLivroInvalido);
    }

    [Fact]
    public void Create_Post_ComDadosValidos_DeveChamarServiceERedirecionarParaIndex()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake();
        var controller = CriarController(context, service);

        var model = CriarEmprestimoFormViewModel(
            usuarioId: usuario.Id,
            livroId: livro.Id,
            dataPrevista: DateTime.Today.AddDays(10));

        var result = controller.Create(model);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(EmprestimosController.Index), redirectResult.ActionName);
        Assert.Equal(livro.Id, service.LivroIdRecebido);
        Assert.Equal(usuario.Id, service.UsuarioIdRecebido);
        Assert.Equal(DateTime.Today.AddDays(10), service.DataPrevistaRecebida);
        Assert.Equal(Messages.EmprestimoAdicionado, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void Create_Post_ServiceComErroDeUsuarioAtrasado_DeveAdicionarErroNoUsuario()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake
        {
            ExcecaoRealizar = new InvalidOperationException(Messages.ErroUsuarioPossuiEmprestimoAtrasado)
        };

        var controller = CriarController(context, service);
        var model = CriarEmprestimoFormViewModel(usuario.Id, livro.Id);

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.UsuarioId)));
        Assert.Contains(
            controller.ModelState[nameof(model.UsuarioId)]!.Errors,
            error => error.ErrorMessage == Messages.ErroUsuarioPossuiEmprestimoAtrasado);
    }

    [Fact]
    public void Create_Post_ServiceComLivroNaoDisponivel_DeveAdicionarErroNoLivro()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake
        {
            ExcecaoRealizar = new InvalidOperationException(Messages.ErroLivroNaoDisponivel)
        };

        var controller = CriarController(context, service);
        var model = CriarEmprestimoFormViewModel(usuario.Id, livro.Id);

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.LivroId)));
        Assert.Contains(
            controller.ModelState[nameof(model.LivroId)]!.Errors,
            error => error.ErrorMessage == Messages.ErroLivroNaoDisponivel);
    }

    [Fact]
    public void Create_Post_ServiceComArgumentExceptionDeData_DeveAdicionarErroNaData()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake
        {
            ExcecaoRealizar = new ArgumentException(
                "A data prevista não pode ultrapassar 365 dias a partir de hoje.",
                "dataPrevistaDevolucao")
        };

        var controller = CriarController(context, service);
        var model = CriarEmprestimoFormViewModel(
            usuario.Id,
            livro.Id,
            DateTime.Today.AddDays(366));

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(nameof(model.DataPrevistaDevolucao)));
        Assert.Contains(
            controller.ModelState[nameof(model.DataPrevistaDevolucao)]!.Errors,
            error => error.ErrorMessage == Messages.ErroDataPrevistaMaiorQueUmAno);
    }

    [Fact]
    public void Create_Post_ServiceComErroInesperado_DeveAdicionarErroGeral()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake
        {
            ExcecaoRealizar = new Exception("Erro inesperado.")
        };

        var controller = CriarController(context, service);
        var model = CriarEmprestimoFormViewModel(usuario.Id, livro.Id);

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Same(model, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(string.Empty));
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == Messages.ErroSalvarEmprestimoInesperado);
    }

    // =========================================================
    // Devolver
    // =========================================================

    [Fact]
    public void Devolver_ComIdInexistente_DeveRetornarNotFound()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Devolver(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Devolver_ComIdExistente_DeveChamarServiceERedirecionarParaIndex()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = CriarEmprestimo(livro, usuario);
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake();
        var controller = CriarController(context, service);

        var result = controller.Devolver(emprestimo.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(EmprestimosController.Index), redirectResult.ActionName);
        Assert.Equal(emprestimo.Id, service.EmprestimoIdDevolvido);
        Assert.Equal(Messages.EmprestimoDevolvidoComSucesso, controller.TempData["Sucesso"]);
    }

    [Fact]
    public void Devolver_ServiceComInvalidOperationException_DeveAdicionarErroNoTempData()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = CriarEmprestimo(livro, usuario);
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake
        {
            ExcecaoDevolver = new InvalidOperationException("Este empréstimo já foi devolvido.")
        };

        var controller = CriarController(context, service);

        var result = controller.Devolver(emprestimo.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(EmprestimosController.Index), redirectResult.ActionName);
        Assert.Equal("Este empréstimo já foi devolvido.", controller.TempData["Erro"]);
    }

    [Fact]
    public void Devolver_ServiceComErroInesperado_DeveAdicionarErroPadraoNoTempData()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = CriarEmprestimo(livro, usuario);
        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var service = new EmprestimoAppServiceFake
        {
            ExcecaoDevolver = new Exception("Erro inesperado.")
        };

        var controller = CriarController(context, service);

        var result = controller.Devolver(emprestimo.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal(nameof(EmprestimosController.Index), redirectResult.ActionName);
        Assert.Equal(Messages.ErroRegistrarDevolucao, controller.TempData["Erro"]);
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

    private sealed class EmprestimoAppServiceFake : IEmprestimoAppService
    {
        public int? LivroIdRecebido { get; private set; }
        public int? UsuarioIdRecebido { get; private set; }
        public DateTime? DataPrevistaRecebida { get; private set; }
        public int? EmprestimoIdDevolvido { get; private set; }

        public Exception? ExcecaoRealizar { get; init; }
        public Exception? ExcecaoDevolver { get; init; }

        public Emprestimo Realizar(int livroId, int usuarioId, DateTime dataPrevistaDevolucao)
        {
            if (ExcecaoRealizar is not null)
                throw ExcecaoRealizar;

            LivroIdRecebido = livroId;
            UsuarioIdRecebido = usuarioId;
            DataPrevistaRecebida = dataPrevistaDevolucao.Date;

            return null!;
        }

        public void Devolver(int emprestimoId)
        {
            if (ExcecaoDevolver is not null)
                throw ExcecaoDevolver;

            EmprestimoIdDevolvido = emprestimoId;
        }
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