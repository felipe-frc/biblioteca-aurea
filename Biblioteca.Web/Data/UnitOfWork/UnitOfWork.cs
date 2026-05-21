using Biblioteca.Domain.Entities;
using Biblioteca.Web.Data.Repositories;

namespace Biblioteca.Web.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BibliotecaDbContext _context;

        public UnitOfWork(BibliotecaDbContext context)
        {
            _context = context;
            Livros = new Repository<Livro>(_context);
            Usuarios = new Repository<Usuario>(_context);
            Emprestimos = new Repository<Emprestimo>(_context);
        }

        public IRepository<Livro> Livros { get; }

        public IRepository<Usuario> Usuarios { get; }

        public IRepository<Emprestimo> Emprestimos { get; }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
