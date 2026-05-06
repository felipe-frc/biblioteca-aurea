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

        public CatalogoController(BibliotecaDbContext context, ILogger<CatalogoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                var livros = _context.Livros
                    .AsNoTracking()
                    .OrderBy(l => l.Titulo)
                    .ToList();

                ViewBag.TotalLivros = livros.Count;
                ViewBag.TotalDisponiveis = livros.Count(l => l.Disponivel);

                return View(livros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar catálogo público.");
                TempData["Erro"] = "Erro ao carregar o catálogo público. Tente novamente.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
