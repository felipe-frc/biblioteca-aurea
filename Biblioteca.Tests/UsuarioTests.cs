using Biblioteca.Domain.Entities;
using Xunit;

namespace Biblioteca.Tests;

public class UsuarioTests
{
    // =========================================================
    // Criação
    // =========================================================

    [Fact]
    public void Usuario_Valido_DeveSerCriadoComSucesso()
    {
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        Assert.Equal("Marcos Felipe", usuario.Nome);
        Assert.Equal("marcos@email.com", usuario.Email);
    }

    [Fact]
    public void Usuario_ComEspacos_DeveNormalizarNomeEEmail()
    {
        var usuario = new Usuario("  Marcos Felipe  ", "  MARCOS@EMAIL.COM  ");

        Assert.Equal("Marcos Felipe", usuario.Nome);
        Assert.Equal("marcos@email.com", usuario.Email);
    }

    [Fact]
    public void Usuario_EmailDeveFicarEmMinusculo()
    {
        var usuario = new Usuario("Marcos Felipe", "MARCOS.FELIPE@EMAIL.COM");

        Assert.Equal("marcos.felipe@email.com", usuario.Email);
    }

    [Fact]
    public void Usuario_ComIdValido_DeveSerCriadoComSucesso()
    {
        var usuario = new Usuario(1, "Marcos Felipe", "marcos@email.com");

        Assert.Equal(1, usuario.Id);
        Assert.Equal("Marcos Felipe", usuario.Nome);
    }

    [Fact]
    public void Usuario_ComIdInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Usuario(0, "Marcos Felipe", "marcos@email.com"));
    }

    [Fact]
    public void Usuario_ComIdNegativo_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Usuario(-1, "Marcos Felipe", "marcos@email.com"));
    }

    [Fact]
    public void Usuario_ComNomeVazio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("", "marcos@email.com"));
    }

    [Fact]
    public void Usuario_ComNomeApenasBrancos_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("   ", "marcos@email.com"));
    }

    [Fact]
    public void Usuario_ComEmailVazio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Marcos Felipe", ""));
    }

    [Fact]
    public void Usuario_ComEmailInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Marcos Felipe", "emailsemarroba"));
    }

    [Fact]
    public void Usuario_ComEmailSemDominio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Marcos Felipe", "marcos@"));
    }

    [Fact]
    public void Usuario_ComEmailComEspaco_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Marcos Felipe", "marcos @email.com"));
    }

    // =========================================================
    // Atualização de dados
    // =========================================================

    [Fact]
    public void AtualizarDados_DeveAtualizarNomeEEmail()
    {
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        usuario.AtualizarDados("Felipe França", "felipe@email.com");

        Assert.Equal("Felipe França", usuario.Nome);
        Assert.Equal("felipe@email.com", usuario.Email);
    }

    [Fact]
    public void AtualizarDados_DeveNormalizarEmail()
    {
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        usuario.AtualizarDados("Marcos Felipe França", "  MARCOS.FRANCA@EMAIL.COM  ");

        Assert.Equal("Marcos Felipe França", usuario.Nome);
        Assert.Equal("marcos.franca@email.com", usuario.Email);
    }

    [Fact]
    public void AtualizarDados_ComNomeVazio_DeveLancarExcecao()
    {
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        Assert.Throws<ArgumentException>(() =>
            usuario.AtualizarDados("", "marcos@email.com"));
    }

    [Fact]
    public void AtualizarDados_ComEmailInvalido_DeveLancarExcecao()
    {
        var usuario = new Usuario("Marcos Felipe", "marcos@email.com");

        Assert.Throws<ArgumentException>(() =>
            usuario.AtualizarDados("Marcos Felipe", "invalido"));
    }
}
