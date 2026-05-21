using Biblioteca.Domain.Entities;
using Biblioteca.Web.Constants;
using Biblioteca.Web.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Services
{
    public class EmprestimoAppService : IEmprestimoAppService
    {
        private readonly IUnitOfWork _unitOfWork;

        public EmprestimoAppService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Emprestimo Realizar(int livroId, int usuarioId, DateTime dataPrevistaDevolucao)
        {
            var livro = _unitOfWork.Livros.GetById(livroId);
            var usuario = _unitOfWork.Usuarios.GetById(usuarioId);

            if (livro is null)
                throw new InvalidOperationException(Messages.ErroLivroNaoEncontrado);

            if (usuario is null)
                throw new InvalidOperationException(Messages.ErroUsuarioNaoEncontrado);

            if (!livro.Disponivel)
                throw new InvalidOperationException(Messages.ErroLivroNaoDisponivel);

            bool usuarioPossuiEmprestimoAtrasado = _unitOfWork.Emprestimos.Any(e =>
                e.UsuarioId == usuarioId &&
                e.DataDevolucao == null &&
                e.DataPrevistaDevolucao.Date < DateTime.Today);

            if (usuarioPossuiEmprestimoAtrasado)
                throw new InvalidOperationException(Messages.ErroUsuarioPossuiEmprestimoAtrasado);

            bool usuarioPossuiEmprestimoAtivoDoMesmoLivro = _unitOfWork.Emprestimos.Any(e =>
                e.UsuarioId == usuarioId &&
                e.LivroId == livroId &&
                e.DataDevolucao == null);

            if (usuarioPossuiEmprestimoAtivoDoMesmoLivro)
                throw new InvalidOperationException(Messages.ErroUsuarioComEmprestimoAtivo);

            var emprestimo = new Emprestimo(livro, usuario, dataPrevistaDevolucao);

            _unitOfWork.Livros.Update(livro);
            _unitOfWork.Emprestimos.Add(emprestimo);
            _unitOfWork.SaveChanges();

            return emprestimo;
        }

        public void Devolver(int emprestimoId)
        {
            var emprestimo = _unitOfWork.Emprestimos
                .Query()
                .Include(e => e.Livro)
                .Include(e => e.Usuario)
                .FirstOrDefault(e => e.Id == emprestimoId);

            if (emprestimo is null)
                throw new InvalidOperationException(Messages.ErroEmprestimoNaoEncontrado);

            if (emprestimo.DataDevolucao is not null)
                throw new InvalidOperationException("Este empréstimo já foi devolvido.");

            emprestimo.Devolver();

            _unitOfWork.Livros.Update(emprestimo.Livro);
            _unitOfWork.Emprestimos.Update(emprestimo);
            _unitOfWork.SaveChanges();
        }
    }
}
