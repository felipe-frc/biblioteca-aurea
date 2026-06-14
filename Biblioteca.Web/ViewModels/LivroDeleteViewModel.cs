namespace Biblioteca.Web.ViewModels
{
    public class LivroDeleteViewModel
    {
        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Autor { get; set; } = string.Empty;

        public string Editora { get; set; } = string.Empty;

        public string Edicao { get; set; } = string.Empty;

        public DateTime DataPublicacao { get; set; }

        public int NumeroPaginas { get; set; }

        public bool Disponivel { get; set; }

        public bool TemEmprestimoRelacionado { get; set; }

        public bool PodeExcluir => !TemEmprestimoRelacionado;
    }
}