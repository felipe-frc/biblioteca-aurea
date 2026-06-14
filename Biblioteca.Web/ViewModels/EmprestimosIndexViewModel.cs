using Biblioteca.Domain.Entities;

namespace Biblioteca.Web.ViewModels
{
    public class EmprestimosIndexViewModel
    {
        public List<Emprestimo> Emprestimos { get; set; } = new();

        public DateTime Hoje { get; set; } = DateTime.Today;

        public string FiltroStatus { get; set; } = "todos";

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int TotalEmprestimos { get; set; }

        public int Ativos { get; set; }

        public int Atrasados { get; set; }

        public int Devolvidos { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;

        public bool HasEmprestimos => Emprestimos.Any();

        public bool HasFiltroStatus =>
            !string.Equals(FiltroStatus, "todos", StringComparison.OrdinalIgnoreCase);
    }
}