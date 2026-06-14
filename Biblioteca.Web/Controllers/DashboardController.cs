using Biblioteca.Web.Constants;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Controllers
{
    /// <summary>
    /// Controller responsável pelo dashboard administrativo da biblioteca.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly BibliotecaDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        /// <summary>
        /// Inicializa uma nova instância do controller de dashboard.
        /// </summary>
        public DashboardController(BibliotecaDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Exibe os indicadores administrativos de livros, usuários e empréstimos.
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                var hoje = DateTime.Today;

                var totalLivros = _context.Livros.Count();
                var livrosDisponiveis = _context.Livros.Count(l => l.Disponivel);
                var livrosEmprestados = totalLivros - livrosDisponiveis;

                var totalUsuarios = _context.Usuarios.Count();

                var totalEmprestimos = _context.Emprestimos.Count();

                var emprestimosAtivos = _context.Emprestimos.Count(e =>
                    e.DataDevolucao == null &&
                    e.DataPrevistaDevolucao.Date >= hoje);

                var emprestimosAtrasados = _context.Emprestimos.Count(e =>
                    e.DataDevolucao == null &&
                    e.DataPrevistaDevolucao.Date < hoje);

                var emprestimosDevolvidos = _context.Emprestimos.Count(e =>
                    e.DataDevolucao != null);

                var ultimosEmprestimos = _context.Emprestimos
                    .AsNoTracking()
                    .Include(e => e.Livro)
                    .Include(e => e.Usuario)
                    .OrderByDescending(e => e.DataEmprestimo)
                    .Take(5)
                    .ToList();

                foreach (var emprestimo in ultimosEmprestimos)
                {
                    emprestimo.AtualizarStatus(hoje);
                }

                var model = new DashboardViewModel
                {
                    UltimosEmprestimos = ultimosEmprestimos,
                    Hoje = hoje,
                    TotalLivros = totalLivros,
                    LivrosDisponiveis = livrosDisponiveis,
                    LivrosEmprestados = livrosEmprestados,
                    TotalUsuarios = totalUsuarios,
                    TotalEmprestimos = totalEmprestimos,
                    EmprestimosAtivos = emprestimosAtivos,
                    EmprestimosAtrasados = emprestimosAtrasados,
                    EmprestimosDevolvidos = emprestimosDevolvidos
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard administrativo.");
                TempData["Erro"] = Messages.ErroCarregarDashboard;
                return RedirectToAction("Index", "Livros");
            }
        }
    }
}