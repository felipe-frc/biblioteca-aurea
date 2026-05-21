using Biblioteca.Domain.Entities;
using Biblioteca.Web.Data.Repositories;

namespace Biblioteca.Web.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Livro> Livros { get; }

        IRepository<Usuario> Usuarios { get; }

        IRepository<Emprestimo> Emprestimos { get; }

        int SaveChanges();
    }
}
