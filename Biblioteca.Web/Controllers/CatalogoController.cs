using Biblioteca.Web.Data;
using Biblioteca.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Biblioteca.Web.Controllers
{
    /// <summary>
    /// Controller responsável pela área pública do catálogo de livros.
    /// </summary>
    [AllowAnonymous]
    public class CatalogoController : Controller
    {
        private const int PageSize = 6;
        private const string CacheKeyPrefix = "catalogo-publico";

        private readonly BibliotecaDbContext _context;
        private readonly ILogger<CatalogoController> _logger;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Inicializa uma nova instância do controller de catálogo público.
        /// </summary>
        public CatalogoController(
            BibliotecaDbContext context,
            ILogger<CatalogoController> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Exibe o catálogo público de livros com busca, filtro por disponibilidade e paginação.
        /// </summary>
        public IActionResult Index(string? busca = null, string disponibilidade = "todos", int page = 1)
        {
            busca = busca?.Trim() ?? string.Empty;

            disponibilidade = string.IsNullOrWhiteSpace(disponibilidade)
                ? "todos"
                : disponibilidade.Trim().ToLowerInvariant();

            if (page < 1)
                page = 1;

            var cacheKey = CriarChaveCache(busca, disponibilidade, page);

            if (_cache.TryGetValue(cacheKey, out CatalogoIndexViewModel? modelCache) && modelCache is not null)
            {
                _logger.LogInformation(
                    "Catálogo público carregado do cache. Busca: {Busca}, Disponibilidade: {Disponibilidade}, Página: {Pagina}",
                    busca,
                    disponibilidade,
                    page);

                return View(modelCache);
            }

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

            var totalPages = (int)Math.Ceiling(totalLivros / (double)PageSize);

            if (totalPages == 0)
                totalPages = 1;

            if (page > totalPages)
                page = totalPages;

            var livros = query
                .OrderBy(l => l.Titulo)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var model = new CatalogoIndexViewModel
            {
                Livros = livros,
                Busca = busca,
                Disponibilidade = disponibilidade,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalLivros = totalLivros,
                TotalDisponiveis = totalDisponiveis,
                TotalEmprestados = totalEmprestados
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(3))
                .SetSlidingExpiration(TimeSpan.FromMinutes(1))
                .SetPriority(CacheItemPriority.Normal);

            _cache.Set(cacheKey, model, cacheOptions);

            _logger.LogInformation(
                "Catálogo público armazenado em cache. Busca: {Busca}, Disponibilidade: {Disponibilidade}, Página: {Pagina}",
                busca,
                disponibilidade,
                page);

            return View(model);
        }

        private static string CriarChaveCache(string? busca, string disponibilidade, int page)
        {
            var buscaNormalizada = string.IsNullOrWhiteSpace(busca)
                ? "sem-busca"
                : busca.Trim().ToLowerInvariant();

            return $"{CacheKeyPrefix}:busca={buscaNormalizada}:disponibilidade={disponibilidade}:page={page}";
        }
    }
}