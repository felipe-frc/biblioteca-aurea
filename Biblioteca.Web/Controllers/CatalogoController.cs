using Biblioteca.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Controllers
{
    [AllowAnonymous]
    public class CatalogoController : Controller
    {
        private readonly BibliotecaDbContext _context;
        private readonly ILogger<CatalogoController> _logger;

        public CatalogoController(
            BibliotecaDbContext context,
            ILogger<CatalogoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index(string? busca = null, string disponibilidade = "todos", int page = 1)
        {
            const int pageSize = 6;

            busca = busca?.Trim();

            disponibilidade = string.IsNullOrWhiteSpace(disponibilidade)
                ? "todos"
                : disponibilidade.Trim().ToLowerInvariant();

            var query = _context.Livros
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                query = query.Where(l =>
                    l.Titulo.Contains(busca) ||
                    l.Autor.Contains(busca) ||
                    l.Editora.Contains(busca));
            }

            query = disponibilidade switch
            {
                "disponiveis" => query.Where(l => l.Disponivel),
                "emprestados" => query.Where(l => !l.Disponivel),
                _ => query
            };

            var totalLivros = query.Count();
            var totalDisponiveis = query.Count(l => l.Disponivel);
            var totalEmprestados = totalLivros - totalDisponiveis;

            var totalPages = (int)Math.Ceiling(totalLivros / (double)pageSize);

            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var livros = query
                .OrderBy(l => l.Titulo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            ViewBag.TotalLivros = totalLivros;
            ViewBag.TotalDisponiveis = totalDisponiveis;
            ViewBag.TotalEmprestados = totalEmprestados;
            ViewBag.Busca = busca ?? string.Empty;
            ViewBag.Disponibilidade = disponibilidade;

            return View(livros);
        }
    }
}