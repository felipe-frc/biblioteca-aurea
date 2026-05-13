namespace Biblioteca.Web.Helpers
{
    public static class ExceptionMessageHelper
    {
        public static string LimparMensagemDeExcecao(string mensagem)
        {
            var indiceParametro = mensagem.IndexOf(" (Parameter", StringComparison.Ordinal);
            return indiceParametro >= 0 ? mensagem[..indiceParametro] : mensagem;
        }
    }
}