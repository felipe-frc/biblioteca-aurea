using Biblioteca.Domain.Entities;

namespace Biblioteca.Web.ViewModels
{
    public class UsuariosIndexViewModel
    {
        public List<Usuario> Usuarios { get; set; } = new();

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int TotalUsuarios { get; set; }

        public int UsuariosInadimplentes { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;

        public bool HasUsuarios => Usuarios.Any();
    }
}