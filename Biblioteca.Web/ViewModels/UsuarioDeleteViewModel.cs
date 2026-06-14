namespace Biblioteca.Web.ViewModels
{
    public class UsuarioDeleteViewModel
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool TemEmprestimoAtivo { get; set; }

        public bool TemHistoricoEmprestimo { get; set; }

        public bool PodeExcluir => !TemEmprestimoAtivo && !TemHistoricoEmprestimo;
    }
}