using Biblioteca.Web.Data;
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
            busca = busca?.Trim();

            disponibilidade = string.IsNullOrWhiteSpace(disponibilidade)
                ? "todos"
                : disponibilidade.Trim().ToLowerInvariant();

            if (page < 1)
                page = 1;

            var cacheKey = CriarChaveCache(busca, disponibilidade, page);

            if (_cache.TryGetValue(cacheKey, out CatalogoCacheResult? resultadoCache) && resultadoCache is not null)
            {
                _logger.LogInformation(
                    "Catálogo público carregado do cache. Busca: {Busca}, Disponibilidade: {Disponibilidade}, Página: {Pagina}",
                    busca,
                    disponibilidade,
                    page);

                PreencherViewBag(resultadoCache, busca, disponibilidade);

                return View(resultadoCache.Livros);
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

            var resultado = new CatalogoCacheResult
            {
                Livros = livros,
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

            _cache.Set(cacheKey, resultado, cacheOptions);

            _logger.LogInformation(
                "Catálogo público armazenado em cache. Busca: {Busca}, Disponibilidade: {Disponibilidade}, Página: {Pagina}",
                busca,
                disponibilidade,
                page);

            PreencherViewBag(resultado, busca, disponibilidade);

            return View(livros);
        }

        private static string CriarChaveCache(string? busca, string disponibilidade, int page)
        {
            var buscaNormalizada = string.IsNullOrWhiteSpace(busca)
                ? "sem-busca"
                : busca.Trim().ToLowerInvariant();

            return $"{CacheKeyPrefix}:busca={buscaNormalizada}:disponibilidade={disponibilidade}:page={page}";
        }

        private void PreencherViewBag(CatalogoCacheResult resultado, string? busca, string disponibilidade)
        {
            ViewBag.CurrentPage = resultado.CurrentPage;
            ViewBag.TotalPages = resultado.TotalPages;
            ViewBag.HasPreviousPage = resultado.CurrentPage > 1;
            ViewBag.HasNextPage = resultado.CurrentPage < resultado.TotalPages;

            ViewBag.TotalLivros = resultado.TotalLivros;
            ViewBag.TotalDisponiveis = resultado.TotalDisponiveis;
            ViewBag.TotalEmprestados = resultado.TotalEmprestados;
            ViewBag.Busca = busca ?? string.Empty;
            ViewBag.Disponibilidade = disponibilidade;
        }

        private sealed class CatalogoCacheResult
        {
            public List<Domain.Entities.Livro> Livros { get; init; } = new();

            public int CurrentPage { get; init; }

            public int TotalPages { get; init; }

            public int TotalLivros { get; init; }

            public int TotalDisponiveis { get; init; }

            public int TotalEmprestados { get; init; }
        }
    }
}