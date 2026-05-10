using Biblioteca.Domain.Entities;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Services
{
    public class EmprestimoAppService : IEmprestimoAppService
    {
        private readonly BibliotecaDbContext _context;

        public EmprestimoAppService(BibliotecaDbContext context)
        {
            _context = context;
        }

        public Emprestimo Realizar(int livroId, int usuarioId, DateTime dataPrevistaDevolucao)
        {
            var livro = _context.Livros.FirstOrDefault(l => l.Id == livroId);
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);

            if (livro is null)
                throw new InvalidOperationException(Messages.ErroLivroNaoEncontrado);

            if (usuario is null)
                throw new InvalidOperationException(Messages.ErroUsuarioNaoEncontrado);

            if (!livro.Disponivel)
                throw new InvalidOperationException(Messages.ErroLivroNaoDisponivel);

            bool usuarioPossuiEmprestimoAtrasado = _context.Emprestimos.Any(e =>
                e.UsuarioId == usuarioId &&
                e.DataDevolucao == null &&
                e.DataPrevistaDevolucao.Date < DateTime.Today);

            if (usuarioPossuiEmprestimoAtrasado)
                throw new InvalidOperationException(Messages.ErroUsuarioPossuiEmprestimoAtrasado);

            bool usuarioPossuiEmprestimoAtivoDoMesmoLivro = _context.Emprestimos.Any(e =>
                e.UsuarioId == usuarioId &&
                e.LivroId == livroId &&
                e.DataDevolucao == null);

            if (usuarioPossuiEmprestimoAtivoDoMesmoLivro)
                throw new InvalidOperationException(Messages.ErroUsuarioComEmprestimoAtivo);

            var emprestimo = new Emprestimo(livro, usuario, dataPrevistaDevolucao);

            _context.Livros.Update(livro);
            _context.Emprestimos.Add(emprestimo);
            _context.SaveChanges();

            return emprestimo;
        }

        public void Devolver(int emprestimoId)
        {
            var emprestimo = _context.Emprestimos
                .Include(e => e.Livro)
                .Include(e => e.Usuario)
                .FirstOrDefault(e => e.Id == emprestimoId);

            if (emprestimo is null)
                throw new InvalidOperationException(Messages.ErroEmprestimoNaoEncontrado);

            if (emprestimo.DataDevolucao is not null)
                throw new InvalidOperationException("Este empréstimo já foi devolvido.");

            emprestimo.Devolver();

            _context.Livros.Update(emprestimo.Livro);
            _context.Emprestimos.Update(emprestimo);
            _context.SaveChanges();
        }
    }
}
