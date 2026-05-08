using Biblioteca.Domain.Entities;
using Xunit;

namespace Biblioteca.Tests;

public class LivroTests
{
    // =========================================================
    // Criação
    // =========================================================

    [Fact]
    public void Livro_Novo_DeveComecarDisponivel()
    {
        var livro = CriarLivro();

        Assert.True(livro.Disponivel);
    }

    [Fact]
    public void Livro_Novo_DevePreencherDadosBibliograficos()
    {
        var dataPublicacao = new DateTime(2008, 8, 1);

        var livro = new Livro(
            1,
            "Clean Code",
            "Robert C. Martin",
            "Alta Books",
            "1ª edição",
            dataPublicacao,
            425);

        Assert.Equal("Clean Code", livro.Titulo);
        Assert.Equal("Robert C. Martin", livro.Autor);
        Assert.Equal("Alta Books", livro.Editora);
        Assert.Equal("1ª edição", livro.Edicao);
        Assert.Equal(dataPublicacao, livro.DataPublicacao);
        Assert.Equal(425, livro.NumeroPaginas);
    }

    [Fact]
    public void Livro_DeveTrimTituloEAutorEEditora()
    {
        var livro = new Livro(
            "  Clean Code  ",
            "  Robert C. Martin  ",
            "  Alta Books  ",
            "  1ª edição  ",
            new DateTime(2008, 8, 1),
            425);

        Assert.Equal("Clean Code", livro.Titulo);
        Assert.Equal("Robert C. Martin", livro.Autor);
        Assert.Equal("Alta Books", livro.Editora);
        Assert.Equal("1ª edição", livro.Edicao);
    }

    [Fact]
    public void CriarLivro_ComTituloVazio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("", "Robert C. Martin", "Alta Books", "1ª edição",
                new DateTime(2008, 8, 1), 425));
    }

    [Fact]
    public void CriarLivro_ComAutorVazio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("Clean Code", "", "Alta Books", "1ª edição",
                new DateTime(2008, 8, 1), 425));
    }

    [Fact]
    public void CriarLivro_ComEditoraVazia_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("Clean Code", "Robert C. Martin", "", "1ª edição",
                new DateTime(2008, 8, 1), 425));
    }

    [Fact]
    public void CriarLivro_ComEdicaoVazia_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("Clean Code", "Robert C. Martin", "Alta Books", "",
                new DateTime(2008, 8, 1), 425));
    }

    [Fact]
    public void CriarLivro_ComDataPublicacaoFutura_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
                DateTime.Today.AddDays(1), 425));
    }

    [Fact]
    public void CriarLivro_ComNumeroPaginasZero_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
                new DateTime(2008, 8, 1), 0));
    }

    [Fact]
    public void CriarLivro_ComNumeroPaginasNegativo_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro("Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
                new DateTime(2008, 8, 1), -1));
    }

    [Fact]
    public void CriarLivro_ComIdInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Livro(0, "Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
                new DateTime(2008, 8, 1), 425));
    }

    // =========================================================
    // Disponibilidade
    // =========================================================

    [Fact]
    public void MarcarComoEmprestado_DeveTornarIndisponivel()
    {
        var livro = CriarLivro();

        livro.MarcarComoEmprestado();

        Assert.False(livro.Disponivel);
    }

    [Fact]
    public void MarcarComoEmprestado_QuandoJaEmprestado_DeveLancarExcecao()
    {
        var livro = CriarLivro();
        livro.MarcarComoEmprestado();

        Assert.Throws<InvalidOperationException>(() => livro.MarcarComoEmprestado());
    }

    [Fact]
    public void MarcarComoDisponivel_QuandoEmprestado_DeveTornarDisponivel()
    {
        var livro = CriarLivro();
        livro.MarcarComoEmprestado();

        livro.MarcarComoDisponivel();

        Assert.True(livro.Disponivel);
    }

    [Fact]
    public void MarcarComoDisponivel_QuandoJaDisponivel_DeveLancarExcecao()
    {
        var livro = CriarLivro();

        Assert.Throws<InvalidOperationException>(() => livro.MarcarComoDisponivel());
    }

    [Fact]
    public void MarcarComoDevolvido_DeveEquivalerAMarcarComoDisponivel()
    {
        var livro = CriarLivro();
        livro.MarcarComoEmprestado();

        livro.MarcarComoDevolvido();

        Assert.True(livro.Disponivel);
    }

    // =========================================================
    // Atualização de dados
    // =========================================================

    [Fact]
    public void AtualizarDados_DeveAlterarTodosOsCampos()
    {
        var livro = CriarLivro();
        var novaData = new DateTime(2019, 1, 1);

        livro.AtualizarDados("The Pragmatic Programmer", "David Thomas", "Addison-Wesley",
            "2ª edição", novaData, 352);

        Assert.Equal("The Pragmatic Programmer", livro.Titulo);
        Assert.Equal("David Thomas", livro.Autor);
        Assert.Equal("Addison-Wesley", livro.Editora);
        Assert.Equal("2ª edição", livro.Edicao);
        Assert.Equal(novaData, livro.DataPublicacao);
        Assert.Equal(352, livro.NumeroPaginas);
    }

    [Fact]
    public void AtualizarDados_ComTituloVazio_DeveLancarExcecao()
    {
        var livro = CriarLivro();

        Assert.Throws<ArgumentException>(() =>
            livro.AtualizarDados("", "Autor", "Editora", "1ª edição",
                new DateTime(2008, 1, 1), 100));
    }

    [Fact]
    public void AnoPublicacao_DeveRetornarAnoCorreto()
    {
        var livro = new Livro("Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
            new DateTime(2008, 8, 1), 425);

        Assert.Equal(2008, livro.AnoPublicacao);
    }

    // =========================================================
    // Helper
    // =========================================================

    private static Livro CriarLivro() =>
        new(1, "Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
            new DateTime(2008, 8, 1), 425);
}
