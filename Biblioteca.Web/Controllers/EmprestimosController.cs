using Biblioteca.Web.Constants;
using Biblioteca.Web.Data;
using Biblioteca.Web.Services;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        /// Exibe a listagem paginada de empréstimos com filtro por status.
        /// </summary>
        public IActionResult Index(int page = 1, string? filtroStatus = "todos")
        {
            const int pageSize = 6;
            var hoje = DateTime.Today;

            filtroStatus = string.IsNullOrWhiteSpace(filtroStatus)
                ? "todos"
                : filtroStatus.Trim().ToLowerInvariant();

            var baseQuery = _context.Emprestimos
                .Include(e => e.Usuario)
                .Include(e => e.Livro)
                .AsNoTracking()
                .AsQueryable();

            var queryFiltrada = baseQuery;

            if (filtroStatus == "ativos")
            {
                queryFiltrada = queryFiltrada.Where(e =>
                    e.DataDevolucao == null &&
                    e.DataPrevistaDevolucao >= hoje);
            }
            else if (filtroStatus == "atrasados")
            {
                queryFiltrada = queryFiltrada.Where(e =>
                    e.DataDevolucao == null &&
                    e.DataPrevistaDevolucao < hoje);
            }
            else if (filtroStatus == "devolvidos")
            {
                queryFiltrada = queryFiltrada.Where(e =>
                    e.DataDevolucao != null);
            }

            queryFiltrada = queryFiltrada.OrderByDescending(e => e.Id);

            var totalEncontrados = queryFiltrada.Count();

            var totalAtivos = baseQuery.Count(e =>
                e.DataDevolucao == null &&
                e.DataPrevistaDevolucao >= hoje);

            var totalAtrasados = baseQuery.Count(e =>
                e.DataDevolucao == null &&
                e.DataPrevistaDevolucao < hoje);

            var totalDevolvidos = baseQuery.Count(e =>
                e.DataDevolucao != null);

            var totalPages = (int)Math.Ceiling(totalEncontrados / (double)pageSize);
            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var emprestimos = queryFiltrada
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var emprestimo in emprestimos)
            {
                emprestimo.AtualizarStatus(hoje);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            ViewBag.TotalEmprestimos = totalEncontrados;
            ViewBag.Ativos = totalAtivos;
            ViewBag.Atrasados = totalAtrasados;
            ViewBag.Devolvidos = totalDevolvidos;
            ViewBag.FiltroStatus = filtroStatus;

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
                ModelState.AddModelError(nameof(model.UsuarioId), Messages.ErroUsuarioInvalido);

            if (!livroExiste)
                ModelState.AddModelError(nameof(model.LivroId), Messages.ErroLivroInvalido);

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

                TempData["Sucesso"] = Messages.EmprestimoAdicionado;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao registrar empréstimo.");
                AdicionarErroDeDominio(model, ex);
                CarregarCombos(model);
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao registrar empréstimo.");
                ModelState.AddModelError(string.Empty, LimparMensagemDeExcecao(ex.Message));
                CarregarCombos(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao registrar empréstimo.");
                ModelState.AddModelError(string.Empty, Messages.ErroSalvarEmprestimoInesperado);
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

                TempData["Sucesso"] = Messages.EmprestimoDevolvidoComSucesso;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao devolver o empréstimo de ID {EmprestimoId}.", id);
                TempData["Erro"] = LimparMensagemDeExcecao(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao devolver o empréstimo de ID {EmprestimoId}.", id);
                TempData["Erro"] = Messages.ErroRegistrarDevolucao;
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

        private void AdicionarErroDeDominio(EmprestimoFormViewModel model, ArgumentException ex)
        {
            var mensagem = LimparMensagemDeExcecao(ex.Message);

            switch (ex.ParamName)
            {
                case "usuario":
                case "usuarioId":
                    ModelState.AddModelError(nameof(model.UsuarioId), mensagem);
                    break;
                case "livro":
                case "livroId":
                    ModelState.AddModelError(nameof(model.LivroId), mensagem);
                    break;
                case "dataPrevistaDevolucao":
                    ModelState.AddModelError(nameof(model.DataPrevistaDevolucao), mensagem);
                    break;
                default:
                    ModelState.AddModelError(string.Empty, Messages.ErroValidacao);
                    break;
            }
        }

        private static string LimparMensagemDeExcecao(string mensagem)
        {
            var indiceParametro = mensagem.IndexOf(" (Parameter", StringComparison.Ordinal);
            return indiceParametro >= 0 ? mensagem[..indiceParametro] : mensagem;
        }
    }
}
