using Biblioteca.Domain.Entities;

namespace Biblioteca.Web.ViewModels
{
    public class DashboardViewModel
    {
        public List<Emprestimo> UltimosEmprestimos { get; set; } = new();

        public DateTime Hoje { get; set; } = DateTime.Today;

        public int TotalLivros { get; set; }

        public int LivrosDisponiveis { get; set; }

        public int LivrosEmprestados { get; set; }

        public int TotalUsuarios { get; set; }

        public int TotalEmprestimos { get; set; }

        public int EmprestimosAtivos { get; set; }

        public int EmprestimosAtrasados { get; set; }

        public int EmprestimosDevolvidos { get; set; }

        public bool HasUltimosEmprestimos => UltimosEmprestimos.Any();
    }
}