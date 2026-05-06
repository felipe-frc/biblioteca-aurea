using Biblioteca.Domain.Entities;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        public IActionResult Index(int page = 1)
        {
            const int pageSize = 6;

            try
            {
                var query = _context.Livros
                    .AsNoTracking()
                    .OrderBy(l => l.Titulo);

                var totalLivros = query.Count();
                var totalDisponiveis = query.Count(l => l.Disponivel);

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
                ViewBag.PublicacaoMaisRecente = publicacaoMaisRecente;

                return View(livros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar livros na página {Page}", page);
                TempData["Erro"] = "Erro ao carregar livros. Tente novamente.";
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

                TempData["Sucesso"] = "Livro cadastrado com sucesso!";
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
                ModelState.AddModelError(string.Empty, "Erro ao salvar o livro. Verifique os dados informados.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar livro: {Titulo}", model.Titulo);
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao salvar o livro.");
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

                TempData["Sucesso"] = "Livro atualizado com sucesso!";
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
                ModelState.AddModelError(string.Empty, "Erro ao atualizar o livro.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar livro. ID: {LivroId}", model.Id);
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao atualizar o livro.");
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
                    TempData["Erro"] = "Não é possível excluir este livro porque ele já possui empréstimos registrados.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Livros.Remove(livro);
                _context.SaveChanges();

                TempData["Sucesso"] = "Livro excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao deletar livro. ID: {LivroId}", id);
                TempData["Erro"] = "Erro ao excluir o livro. Tente novamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao deletar livro. ID: {LivroId}", id);
                TempData["Erro"] = "Ocorreu um erro inesperado ao excluir o livro.";
                return RedirectToAction(nameof(Index));
            }
        }

        private void AdicionarErroDeDominio(LivroFormViewModel model, ArgumentException ex)
        {
            switch (ex.ParamName)
            {
                case "titulo":
                    ModelState.AddModelError(nameof(model.Titulo), ex.Message);
                    break;
                case "autor":
                    ModelState.AddModelError(nameof(model.Autor), ex.Message);
                    break;
                case "editora":
                    ModelState.AddModelError(nameof(model.Editora), ex.Message);
                    break;
                case "edicao":
                    ModelState.AddModelError(nameof(model.Edicao), ex.Message);
                    break;
                case "dataPublicacao":
                    ModelState.AddModelError(nameof(model.DataPublicacao), ex.Message);
                    break;
                case "numeroPaginas":
                    ModelState.AddModelError(nameof(model.NumeroPaginas), ex.Message);
                    break;
                default:
                    ModelState.AddModelError(string.Empty, ex.Message);
                    break;
            }
        }
    }
}

