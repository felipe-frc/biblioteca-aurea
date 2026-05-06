using Biblioteca.Web.Services;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biblioteca.Web.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de empréstimos da biblioteca.
    /// </summary>
    [Authorize]
    public class EmprestimosController : Controller
    {
        private readonly BibliotecaDbContext _context;
        private readonly ILogger<EmprestimosController> _logger;
        private readonly IEmprestimoAppService _emprestimoAppService;

        /// <summary>
        /// Inicializa uma nova instância do controller de empréstimos.
        /// </summary>
        public EmprestimosController(
            BibliotecaDbContext context,
            ILogger<EmprestimosController> logger,
            IEmprestimoAppService emprestimoAppService)
        {
            _context = context;
            _logger = logger;
            _emprestimoAppService = emprestimoAppService;
        }

        /// <summary>
        /// Exibe a listagem paginada de empréstimos.
        /// </summary>
        public IActionResult Index(int page = 1)
        {
            const int pageSize = 6;

            var query = _context.Emprestimos
                .Include(e => e.Usuario)
                .Include(e => e.Livro)
                .AsNoTracking()
                .OrderByDescending(e => e.Id);

            var totalEmprestimos = query.Count();
            var pendentes = query.Count(e => e.DataDevolucao == null);
            var devolvidos = query.Count(e => e.DataDevolucao != null);

            var totalPages = (int)Math.Ceiling(totalEmprestimos / (double)pageSize);
            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var emprestimos = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var emprestimo in emprestimos)
            {
                emprestimo.AtualizarStatus(DateTime.Today);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            ViewBag.TotalEmprestimos = totalEmprestimos;
            ViewBag.Pendentes = pendentes;
            ViewBag.Devolvidos = devolvidos;

            return View(emprestimos);
        }

        /// <summary>
        /// Exibe o formulário de criação de empréstimo.
        /// </summary>
        public IActionResult Create()
        {
            var model = new EmprestimoFormViewModel();
            CarregarCombos(model);
            return View(model);
        }

        /// <summary>
        /// Processa o registro de um novo empréstimo.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EmprestimoFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CarregarCombos(model);
                return View(model);
            }

            var usuarioExiste = _context.Usuarios.Any(u => u.Id == model.UsuarioId);
            var livroExiste = _context.Livros.Any(l => l.Id == model.LivroId);

            if (!usuarioExiste)
                ModelState.AddModelError(nameof(model.UsuarioId), "Usuário inválido.");

            if (!livroExiste)
                ModelState.AddModelError(nameof(model.LivroId), "Livro inválido.");

            if (!ModelState.IsValid)
            {
                CarregarCombos(model);
                return View(model);
            }

            try
            {
                _emprestimoAppService.Realizar(
    model.LivroId!.Value,
    model.UsuarioId!.Value,
    model.DataPrevistaDevolucao!.Value);

                TempData["Sucesso"] = "Empréstimo registrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao registrar empréstimo.");
                ModelState.AddModelError(string.Empty, ex.Message);
                CarregarCombos(model);
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao registrar empréstimo.");
                ModelState.AddModelError(string.Empty, ex.Message);
                CarregarCombos(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao registrar empréstimo.");
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao registrar o empréstimo.");
                CarregarCombos(model);
                return View(model);
            }
        }

        /// <summary>
        /// Processa a devolução de um empréstimo.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Devolver(int id)
        {
            var emprestimoExiste = _context.Emprestimos.Any(e => e.Id == id);

            if (!emprestimoExiste)
                return NotFound();

            try
            {
                _emprestimoAppService.Devolver(id);

                TempData["Sucesso"] = "Devolução registrada com sucesso!";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao devolver o empréstimo de ID {EmprestimoId}.", id);
                TempData["Erro"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao devolver o empréstimo de ID {EmprestimoId}.", id);
                TempData["Erro"] = "Ocorreu um erro inesperado ao registrar a devolução.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carrega os combos de usuários e livros disponíveis para o formulário de empréstimo.
        /// </summary>
        private void CarregarCombos(EmprestimoFormViewModel model)
        {
            model.Usuarios = _context.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nome)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Nome
                })
                .ToList();

            model.Livros = _context.Livros
                .AsNoTracking()
                .Where(l => l.Disponivel)
                .OrderBy(l => l.Titulo)
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Titulo
                })
                .ToList();
        }
    }
}
