using Biblioteca.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly BibliotecaDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(BibliotecaDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                var totalLivros = _context.Livros.Count();
                var livrosDisponiveis = _context.Livros.Count(l => l.Disponivel);
                var livrosEmprestados = totalLivros - livrosDisponiveis;

                var totalUsuarios = _context.Usuarios.Count();

                var totalEmprestimos = _context.Emprestimos.Count();
                var emprestimosAtivos = _context.Emprestimos.Count(e => e.DataDevolucao == null);
                var emprestimosDevolvidos = _context.Emprestimos.Count(e => e.DataDevolucao != null);

                var ultimosEmprestimos = _context.Emprestimos
                    .AsNoTracking()
                    .Include(e => e.Livro)
                    .Include(e => e.Usuario)
                    .OrderByDescending(e => e.DataEmprestimo)
                    .Take(5)
                    .ToList();

                ViewBag.TotalLivros = totalLivros;
                ViewBag.LivrosDisponiveis = livrosDisponiveis;
                ViewBag.LivrosEmprestados = livrosEmprestados;
                ViewBag.TotalUsuarios = totalUsuarios;
                ViewBag.TotalEmprestimos = totalEmprestimos;
                ViewBag.EmprestimosAtivos = emprestimosAtivos;
                ViewBag.EmprestimosDevolvidos = emprestimosDevolvidos;

                return View(ultimosEmprestimos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard administrativo.");
                TempData["Erro"] = "Erro ao carregar o dashboard administrativo.";
                return RedirectToAction("Index", "Livros");
            }
        }
    }
}
