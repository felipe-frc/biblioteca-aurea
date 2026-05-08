using Biblioteca.Domain.Entities;
using Biblioteca.Domain.Enums;
using Xunit;

namespace Biblioteca.Tests;

public class EmprestimoTests
{
    // =========================================================
    // Criação de empréstimo
    // =========================================================

    [Fact]
    public void Emprestimo_Valido_DeveSerCriadoComStatusAtivo()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var dataPrevista = DateTime.Today.AddDays(7);

        var emprestimo = new Emprestimo(1, livro, usuario, dataPrevista);

        Assert.Equal(1, emprestimo.Id);
        Assert.Equal(livro, emprestimo.Livro);
        Assert.Equal(usuario, emprestimo.Usuario);
        Assert.Equal(dataPrevista.Date, emprestimo.DataPrevistaDevolucao);
        Assert.Equal(StatusEmprestimo.Ativo, emprestimo.Status);
        Assert.Null(emprestimo.DataDevolucao);
        Assert.False(livro.Disponivel);
    }

    [Fact]
    public void Emprestimo_DeveMarcarLivroComoIndisponivelAoCriar()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();

        Assert.True(livro.Disponivel);

        _ = new Emprestimo(livro, usuario, DateTime.Today.AddDays(7));

        Assert.False(livro.Disponivel);
    }

    [Fact]
    public void Emprestimo_ComLivroIndisponivel_DeveLancarExcecao()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        livro.MarcarComoEmprestado();

        Assert.Throws<InvalidOperationException>(() =>
            new Emprestimo(livro, usuario, DateTime.Today.AddDays(7)));
    }

    [Fact]
    public void Emprestimo_ComDataNoPassado_DeveLancarExcecao()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentException>(() =>
            new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(-1)));
    }

    [Fact]
    public void Emprestimo_ComDataMaiorQue365Dias_DeveLancarExcecao()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentException>(() =>
            new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(366)));
    }

    [Fact]
    public void Emprestimo_ComDataExatamenteHoje_DeveSerValido()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today);

        Assert.Equal(DateTime.Today, emprestimo.DataPrevistaDevolucao);
    }

    [Fact]
    public void Emprestimo_ComDataExatamente365Dias_DeveSerValido()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();

        var emprestimo = new Emprestimo(livro, usuario, DateTime.Today.AddDays(365));

        Assert.Equal(DateTime.Today.AddDays(365), emprestimo.DataPrevistaDevolucao);
    }

    [Fact]
    public void Emprestimo_ComLivroNulo_DeveLancarExcecao()
    {
        var usuario = CriarUsuario();

        Assert.Throws<ArgumentNullException>(() =>
            new Emprestimo(null!, usuario, DateTime.Today.AddDays(7)));
    }

    [Fact]
    public void Emprestimo_ComUsuarioNulo_DeveLancarExcecao()
    {
        var livro = CriarLivro();

        Assert.Throws<ArgumentNullException>(() =>
            new Emprestimo(livro, null!, DateTime.Today.AddDays(7)));
    }

    // =========================================================
    // Devolução
    // =========================================================

    [Fact]
    public void Devolver_DeveRegistrarDataDevolucaoETornarLivroDisponivel()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(7));

        emprestimo.Devolver();

        Assert.NotNull(emprestimo.DataDevolucao);
        Assert.True(livro.Disponivel);
    }

    [Fact]
    public void Devolver_DentroDoPrazo_DeveMarcarComoDevolvido()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(7));

        emprestimo.Devolver();

        Assert.Equal(StatusEmprestimo.Devolvido, emprestimo.Status);
    }

    [Fact]
    public void Devolver_QuandoJaDevolvido_DeveLancarExcecao()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(7));
        emprestimo.Devolver();

        Assert.Throws<InvalidOperationException>(() => emprestimo.Devolver());
    }

    // =========================================================
    // Status e atraso
    // =========================================================

    [Fact]
    public void AtualizarStatus_AntesDoVencimento_DevePermaneceAtivo()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(5));

        emprestimo.AtualizarStatus(DateTime.Today);

        Assert.Equal(StatusEmprestimo.Ativo, emprestimo.Status);
    }

    [Fact]
    public void AtualizarStatus_ComDataAposVencimento_DeveFicarAtrasado()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today);

        emprestimo.AtualizarStatus(DateTime.Today.AddDays(1));

        Assert.Equal(StatusEmprestimo.Atrasado, emprestimo.Status);
    }

    [Fact]
    public void AtualizarStatus_QuandoDevolvido_NaoDeveAlterarStatus()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(7));
        emprestimo.Devolver();

        emprestimo.AtualizarStatus(DateTime.Today.AddDays(30));

        Assert.Equal(StatusEmprestimo.Devolvido, emprestimo.Status);
    }

    [Fact]
    public void EstaAtrasado_AntesDoVencimento_DeveRetornarFalso()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(5));

        var resultado = emprestimo.EstaAtrasado(DateTime.Today);

        Assert.False(resultado);
    }

    [Fact]
    public void EstaAtrasado_AposVencimento_DeveRetornarVerdadeiro()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today);

        var resultado = emprestimo.EstaAtrasado(DateTime.Today.AddDays(1));

        Assert.True(resultado);
    }

    [Fact]
    public void EstaAtrasado_NoExatoDiaDoVencimento_DeveRetornarFalso()
    {
        var livro = CriarLivro();
        var usuario = CriarUsuario();
        var emprestimo = new Emprestimo(1, livro, usuario, DateTime.Today.AddDays(3));

        var resultado = emprestimo.EstaAtrasado(DateTime.Today.AddDays(3));

        Assert.False(resultado);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static Livro CriarLivro() =>
        new("Clean Code", "Robert C. Martin", "Alta Books", "1ª edição",
            new DateTime(2008, 8, 1), 425);

    private static Usuario CriarUsuario() =>
        new(1, "Marcos Felipe", "marcos@email.com");
}
