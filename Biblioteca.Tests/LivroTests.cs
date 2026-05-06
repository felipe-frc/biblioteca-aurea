using Biblioteca.Domain.Entities;
using Xunit;

namespace Biblioteca.Tests;

public class LivroTests
{
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
    public void CriarLivro_ComDataPublicacaoFutura_DeveLancarExcecao()
    {
        var dataFutura = DateTime.Today.AddDays(1);

        Assert.Throws<ArgumentException>(() =>
            new Livro(
                "Clean Code",
                "Robert C. Martin",
                "Alta Books",
                "1ª edição",
                dataFutura,
                425));
    }

    [Fact]
    public void CriarLivro_ComNumeroPaginasInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Livro(
                "Clean Code",
                "Robert C. Martin",
                "Alta Books",
                "1ª edição",
                new DateTime(2008, 8, 1),
                0));
    }

    private static Livro CriarLivro()
    {
        return new Livro(
            1,
            "Clean Code",
            "Robert C. Martin",
            "Alta Books",
            "1ª edição",
            new DateTime(2008, 8, 1),
            425);
    }
}