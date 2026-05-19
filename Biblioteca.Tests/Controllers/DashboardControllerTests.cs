using Biblioteca.Domain.Entities;
using Biblioteca.Domain.Enums;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Controllers;
using Biblioteca.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Biblioteca.Tests.Controllers;

public class DashboardControllerTests
{
    private static BibliotecaDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<BibliotecaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BibliotecaDbContext(options);
    }

    private static DashboardController CriarController(BibliotecaDbContext context)
    {
        var controller = new DashboardController(
            context,
            NullLogger<DashboardController>.Instance);

        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            new TempDataProviderFake());

        return controller;
    }

    private static Livro CriarLivro(
        string titulo = "Clean Code",
        bool disponivel = true)
    {
        var livro = new Livro(
            titulo,
            "Robert C. Martin",
            "Alta Books",
            "1ª edição",
            new DateTime(2008, 8, 1),
            425);

        if (!disponivel)
            livro.MarcarComoEmprestado();

        return livro;
    }

    private static Usuario CriarUsuario(
        string nome = "Marcos Felipe",
        string email = "marcos@email.com")
    {
        return new Usuario(nome, email);
    }

    // =========================================================
    // Dashboard vazio
    // =========================================================

    [Fact]
    public void Index_SemDados_DeveRetornarViewComIndicadoresZerados()
    {
        using var context = CriarContexto();
        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var emprestimos = Assert.IsAssignableFrom<IEnumerable<Emprestimo>>(viewResult.Model).ToList();

        Assert.Empty(emprestimos);

        Assert.Equal(0, controller.ViewBag.TotalLivros);
        Assert.Equal(0, controller.ViewBag.LivrosDisponiveis);
        Assert.Equal(0, controller.ViewBag.LivrosEmprestados);
        Assert.Equal(0, controller.ViewBag.TotalUsuarios);
        Assert.Equal(0, controller.ViewBag.TotalEmprestimos);
        Assert.Equal(0, controller.ViewBag.EmprestimosAtivos);
        Assert.Equal(0, controller.ViewBag.EmprestimosAtrasados);
        Assert.Equal(0, controller.ViewBag.EmprestimosDevolvidos);
    }

    // =========================================================
    // Indicadores gerais
    // =========================================================

    [Fact]
    public void Index_ComLivrosUsuariosEEmprestimos_DeveCalcularIndicadoresCorretamente()
    {
        using var context = CriarContexto();

        var usuario1 = CriarUsuario("Usuário 1", "usuario1@email.com");
        var usuario2 = CriarUsuario("Usuário 2", "usuario2@email.com");

        var livroDisponivel = CriarLivro("Livro Disponível");
        var livroEmprestado1 = CriarLivro("Livro Emprestado 1");
        var livroEmprestado2 = CriarLivro("Livro Emprestado 2");

        context.Usuarios.AddRange(usuario1, usuario2);
        context.Livros.AddRange(livroDisponivel, livroEmprestado1, livroEmprestado2);
        context.SaveChanges();

        var emprestimoAtivo = new Emprestimo(
            livroEmprestado1,
            usuario1,
            DateTime.Today.AddDays(7));

        var emprestimoAtrasado = new Emprestimo(
            livroEmprestado2,
            usuario2,
            DateTime.Today.AddDays(7));

        DefinirDataPrevistaDevolucao(emprestimoAtrasado, DateTime.Today.AddDays(-1));

        context.Emprestimos.AddRange(emprestimoAtivo, emprestimoAtrasado);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var emprestimos = Assert.IsAssignableFrom<IEnumerable<Emprestimo>>(viewResult.Model).ToList();

        Assert.Equal(2, emprestimos.Count);

        Assert.Equal(3, controller.ViewBag.TotalLivros);
        Assert.Equal(1, controller.ViewBag.LivrosDisponiveis);
        Assert.Equal(2, controller.ViewBag.LivrosEmprestados);
        Assert.Equal(2, controller.ViewBag.TotalUsuarios);
        Assert.Equal(2, controller.ViewBag.TotalEmprestimos);
        Assert.Equal(1, controller.ViewBag.EmprestimosAtivos);
        Assert.Equal(1, controller.ViewBag.EmprestimosAtrasados);
        Assert.Equal(0, controller.ViewBag.EmprestimosDevolvidos);
    }

    [Fact]
    public void Index_ComEmprestimoDevolvido_DeveContarComoDevolvido()
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

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var emprestimos = Assert.IsAssignableFrom<IEnumerable<Emprestimo>>(viewResult.Model).ToList();

        Assert.Single(emprestimos);

        Assert.Equal(1, controller.ViewBag.TotalEmprestimos);
        Assert.Equal(0, controller.ViewBag.EmprestimosAtivos);
        Assert.Equal(0, controller.ViewBag.EmprestimosAtrasados);
        Assert.Equal(1, controller.ViewBag.EmprestimosDevolvidos);
    }

    // =========================================================
    // Últimos empréstimos
    // =========================================================

    [Fact]
    public void Index_ComMaisDeCincoEmprestimos_DeveRetornarApenasCincoUltimos()
    {
        using var context = CriarContexto();

        for (var i = 1; i <= 7; i++)
        {
            var usuario = CriarUsuario($"Usuário {i:D2}", $"usuario{i}@email.com");
            var livro = CriarLivro($"Livro {i:D2}");

            context.Usuarios.Add(usuario);
            context.Livros.Add(livro);
            context.SaveChanges();

            var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
            DefinirDataEmprestimo(emprestimo, DateTime.Today.AddDays(i));

            context.Emprestimos.Add(emprestimo);
            context.SaveChanges();
        }

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var emprestimos = Assert.IsAssignableFrom<IEnumerable<Emprestimo>>(viewResult.Model).ToList();

        Assert.Equal(5, emprestimos.Count);
        Assert.Equal(7, controller.ViewBag.TotalEmprestimos);

        Assert.True(emprestimos[0].DataEmprestimo >= emprestimos[1].DataEmprestimo);
        Assert.True(emprestimos[1].DataEmprestimo >= emprestimos[2].DataEmprestimo);
        Assert.True(emprestimos[2].DataEmprestimo >= emprestimos[3].DataEmprestimo);
        Assert.True(emprestimos[3].DataEmprestimo >= emprestimos[4].DataEmprestimo);
    }

    [Fact]
    public void Index_DeveAtualizarStatusDosUltimosEmprestimos()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario();
        var livro = CriarLivro();

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));
        DefinirDataPrevistaDevolucao(emprestimo, DateTime.Today.AddDays(-3));

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var emprestimos = Assert.IsAssignableFrom<IEnumerable<Emprestimo>>(viewResult.Model).ToList();

        Assert.Single(emprestimos);
        Assert.Equal(StatusEmprestimo.Atrasado, emprestimos[0].Status);
    }

    [Fact]
    public void Index_DeveCarregarLivroEUsuarioNosUltimosEmprestimos()
    {
        using var context = CriarContexto();

        var usuario = CriarUsuario("Marcos Felipe", "marcos@email.com");
        var livro = CriarLivro("Clean Code");

        context.Usuarios.Add(usuario);
        context.Livros.Add(livro);
        context.SaveChanges();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));

        context.Emprestimos.Add(emprestimo);
        context.SaveChanges();

        var controller = CriarController(context);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var emprestimos = Assert.IsAssignableFrom<IEnumerable<Emprestimo>>(viewResult.Model).ToList();

        Assert.Single(emprestimos);
        Assert.NotNull(emprestimos[0].Livro);
        Assert.NotNull(emprestimos[0].Usuario);
        Assert.Equal("Clean Code", emprestimos[0].Livro.Titulo);
        Assert.Equal("Marcos Felipe", emprestimos[0].Usuario.Nome);
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

    private static void DefinirDataEmprestimo(Emprestimo emprestimo, DateTime dataEmprestimo)
    {
        var propriedade = typeof(Emprestimo).GetProperty(nameof(Emprestimo.DataEmprestimo));

        if (propriedade is null)
            throw new InvalidOperationException("Propriedade DataEmprestimo não encontrada.");

        propriedade.SetValue(emprestimo, dataEmprestimo);
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