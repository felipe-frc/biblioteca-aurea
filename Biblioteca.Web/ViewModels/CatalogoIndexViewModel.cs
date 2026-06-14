using Biblioteca.Domain.Entities;

namespace Biblioteca.Web.ViewModels
{
    public class CatalogoIndexViewModel
    {
        public List<Livro> Livros { get; set; } = new();

        public string Busca { get; set; } = string.Empty;

        public string Disponibilidade { get; set; } = "todos";

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int TotalLivros { get; set; }

        public int TotalDisponiveis { get; set; }

        public int TotalEmprestados { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;

        public bool HasLivros => Livros.Any();

        public bool HasFiltros =>
            !string.IsNullOrWhiteSpace(Busca) ||
            !string.Equals(Disponibilidade, "todos", StringComparison.OrdinalIgnoreCase);
    }
}