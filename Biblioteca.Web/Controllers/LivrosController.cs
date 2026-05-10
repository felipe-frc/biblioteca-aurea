using Biblioteca.Domain.Entities;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Controllers
{
    [Authorize]
    public class LivrosController : Controller
    {
        private readonly BibliotecaDbContext _context;
        private readonly ILogger<LivrosController> _logger;

        public LivrosController(BibliotecaDbContext context, ILogger<LivrosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index(int page = 1, string? busca = null, string disponibilidade = "todos")
        {
            const int pageSize = 6;

            try
            {
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

                query = query.OrderBy(l => l.Titulo);

                var totalLivros = query.Count();
                var totalDisponiveis = query.Count(l => l.Disponivel);
                var totalEmprestados = totalLivros - totalDisponiveis;

                var publicacaoMaisRecente = totalLivros > 0
                    ? query.Max(l => l.DataPublicacao).ToString("dd/MM/yyyy")
                    : "-";

                var totalPages = (int)Math.Ceiling(totalLivros / (double)pageSize);
                if (totalPages == 0)
                    totalPages = 1;

                if (page < 1)
                    page = 1;

                if (page > totalPages)
                    page = totalPages;

                var livros = query
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
                ViewBag.PublicacaoMaisRecente = publicacaoMaisRecente;

                ViewBag.Busca = busca ?? string.Empty;
                ViewBag.Disponibilidade = disponibilidade;

                return View(livros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar livros na página {Page}", page);
                TempData["Erro"] = Messages.ErroCarregarLivros;
                return RedirectToAction(nameof(Index), new { page = 1 });
            }
        }

        public IActionResult Create()
        {
            return View(new LivroFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LivroFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var livro = new Livro(
                    model.Titulo,
                    model.Autor,
                    model.Editora,
                    model.Edicao,
                    model.DataPublicacao!.Value,
                    model.NumeroPaginas!.Value);

                _context.Livros.Add(livro);
                _context.SaveChanges();

                TempData["Sucesso"] = Messages.LivroAdicionado;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                AdicionarErroDeDominio(model, ex);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao criar livro: {Titulo}", model.Titulo);
                ModelState.AddModelError(string.Empty, Messages.ErroSalvarLivroDadosInvalidos);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar livro: {Titulo}", model.Titulo);
                ModelState.AddModelError(string.Empty, Messages.ErroSalvarLivroInesperado);
                return View(model);
            }
        }

        public IActionResult Edit(int id)
        {
            try
            {
                var livro = _context.Livros.FirstOrDefault(l => l.Id == id);

                if (livro is null)
                    return NotFound();

                var model = new LivroFormViewModel
                {
                    Id = livro.Id,
                    Titulo = livro.Titulo,
                    Autor = livro.Autor,
                    Editora = livro.Editora,
                    Edicao = livro.Edicao,
                    DataPublicacao = livro.DataPublicacao,
                    NumeroPaginas = livro.NumeroPaginas
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar formulário de edição. ID: {LivroId}", id);
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(LivroFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var livro = _context.Livros.FirstOrDefault(l => l.Id == model.Id);

                if (livro is null)
                    return NotFound();

                livro.AtualizarDados(
                    model.Titulo,
                    model.Autor,
                    model.Editora,
                    model.Edicao,
                    model.DataPublicacao!.Value,
                    model.NumeroPaginas!.Value);

                _context.SaveChanges();

                TempData["Sucesso"] = Messages.LivroAtualizado;
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                AdicionarErroDeDominio(model, ex);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao atualizar livro. ID: {LivroId}", model.Id);
                ModelState.AddModelError(string.Empty, Messages.ErroAtualizarLivro);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar livro. ID: {LivroId}", model.Id);
                ModelState.AddModelError(string.Empty, Messages.ErroAtualizarLivroInesperado);
                return View(model);
            }
        }

        public IActionResult Delete(int id)
        {
            try
            {
                var livro = _context.Livros
                    .AsNoTracking()
                    .FirstOrDefault(l => l.Id == id);

                if (livro is null)
                    return NotFound();

                bool temEmprestimoRelacionado = _context.Emprestimos.Any(e => e.LivroId == id);
                ViewBag.TemEmprestimoRelacionado = temEmprestimoRelacionado;

                return View(livro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar página de exclusão. ID: {LivroId}", id);
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var livro = _context.Livros.FirstOrDefault(l => l.Id == id);

                if (livro is null)
                    return NotFound();

                bool temEmprestimoRelacionado = _context.Emprestimos.Any(e => e.LivroId == id);

                if (temEmprestimoRelacionado)
                {
                    TempData["Erro"] = Messages.ErroLivroComEmprestimo;
                    return RedirectToAction(nameof(Index));
                }

                _context.Livros.Remove(livro);
                _context.SaveChanges();

                TempData["Sucesso"] = Messages.LivroRemovido;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao deletar livro. ID: {LivroId}", id);
                TempData["Erro"] = Messages.ErroExcluirLivro;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao deletar livro. ID: {LivroId}", id);
                TempData["Erro"] = Messages.ErroExcluirLivroInesperado;
                return RedirectToAction(nameof(Index));
            }
        }

        private void AdicionarErroDeDominio(LivroFormViewModel model, ArgumentException ex)
        {
            var mensagem = LimparMensagemDeExcecao(ex.Message);

            switch (ex.ParamName)
            {
                case "titulo":
                    ModelState.AddModelError(nameof(model.Titulo), mensagem);
                    break;
                case "autor":
                    ModelState.AddModelError(nameof(model.Autor), mensagem);
                    break;
                case "editora":
                    ModelState.AddModelError(nameof(model.Editora), mensagem);
                    break;
                case "edicao":
                    ModelState.AddModelError(nameof(model.Edicao), mensagem);
                    break;
                case "dataPublicacao":
                    ModelState.AddModelError(nameof(model.DataPublicacao), mensagem);
                    break;
                case "numeroPaginas":
                    ModelState.AddModelError(nameof(model.NumeroPaginas), mensagem);
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
