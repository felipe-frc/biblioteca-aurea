using Biblioteca.Domain.Entities;
using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biblioteca.Web.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de usuários da biblioteca.
    /// </summary>
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly BibliotecaDbContext _context;
        private readonly ILogger<UsuariosController> _logger;

        /// <summary>
        /// Inicializa uma nova instância do controller de usuários.
        /// </summary>
        public UsuariosController(BibliotecaDbContext context, ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Exibe a listagem paginada de usuários.
        /// </summary>
        public IActionResult Index(int page = 1)
        {
            const int pageSize = 6;

            var query = _context.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Id);

            var totalUsuarios = query.Count();

            var comEmail = query.Count(u => !string.IsNullOrWhiteSpace(u.Email));

            var usuariosInadimplentes = _context.Emprestimos
                .AsNoTracking()
                .Where(e => e.DataDevolucao == null && e.DataPrevistaDevolucao < DateTime.Today)
                .Select(e => e.UsuarioId)
                .Distinct()
                .Count();

            var totalPages = (int)Math.Ceiling(totalUsuarios / (double)pageSize);
            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var usuarios = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.ComEmail = comEmail;
            ViewBag.UsuariosInadimplentes = usuariosInadimplentes;

            return View(usuarios);
        }

        /// <summary>
        /// Exibe o formulário de criação de usuário.
        /// </summary>
        public IActionResult Create()
        {
            return View(new UsuarioFormViewModel());
        }

        /// <summary>
        /// Processa o cadastro de um novo usuário.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(UsuarioFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Usuarios.Any(u => u.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado.");
                return View(model);
            }

            try
            {
                var usuario = new Usuario(model.Nome, model.Email);

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();

                TempData["Sucesso"] = "Usuário cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao criar usuário.");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao criar usuário.");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar usuário.");
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao salvar o usuário.");
                return View(model);
            }
        }

        /// <summary>
        /// Exibe o formulário de edição de usuário.
        /// </summary>
        public IActionResult Edit(int id)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == id);

            if (usuario is null)
                return NotFound();

            var model = new UsuarioFormViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email
            };

            return View(model);
        }

        /// <summary>
        /// Processa a atualização dos dados do usuário.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UsuarioFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == model.Id);

            if (usuario is null)
                return NotFound();

            if (_context.Usuarios.Any(u => u.Id != model.Id && u.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado para outro usuário.");
                return View(model);
            }

            try
            {
                usuario.AtualizarDados(model.Nome, model.Email);
                _context.SaveChanges();

                TempData["Sucesso"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao editar o usuário de ID {UsuarioId}.", model.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operação inválida ao editar o usuário de ID {UsuarioId}.", model.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao editar o usuário de ID {UsuarioId}.", model.Id);
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao atualizar o usuário.");
                return View(model);
            }
        }

        /// <summary>
        /// Exibe a tela de confirmação de exclusão de usuário.
        /// </summary>
        public IActionResult Delete(int id)
        {
            var usuario = _context.Usuarios
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == id);

            if (usuario is null)
                return NotFound();

            bool temEmprestimoAtivo = _context.Emprestimos
                .Any(e => e.UsuarioId == id && e.DataDevolucao == null);

            bool temHistoricoEmprestimo = _context.Emprestimos
                .Any(e => e.UsuarioId == id);

            ViewBag.TemEmprestimoAtivo = temEmprestimoAtivo;
            ViewBag.TemHistoricoEmprestimo = temHistoricoEmprestimo;

            return View(usuario);
        }

        /// <summary>
        /// Processa a exclusão de um usuário.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == id);

            if (usuario is null)
                return NotFound();

            bool temEmprestimoAtivo = _context.Emprestimos
                .Any(e => e.UsuarioId == id && e.DataDevolucao == null);

            if (temEmprestimoAtivo)
            {
                TempData["Erro"] = "Não é possível excluir este usuário porque existe empréstimo em aberto.";
                return RedirectToAction(nameof(Index));
            }

            bool temHistoricoEmprestimo = _context.Emprestimos
                .Any(e => e.UsuarioId == id);

            if (temHistoricoEmprestimo)
            {
                TempData["Erro"] = "Não é possível excluir este usuário porque ele possui empréstimos registrados no histórico.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Usuarios.Remove(usuario);
                _context.SaveChanges();

                TempData["Sucesso"] = "Usuário excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco ao excluir o usuário de ID {UsuarioId}.", id);
                TempData["Erro"] = "Não foi possível excluir o usuário porque ele ainda está relacionado a registros do sistema.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao excluir o usuário de ID {UsuarioId}.", id);
                TempData["Erro"] = "Ocorreu um erro inesperado ao excluir o usuário.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
