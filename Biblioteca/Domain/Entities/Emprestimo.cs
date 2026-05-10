using Biblioteca.Domain.Enums;

namespace Biblioteca.Domain.Entities
{
    /// <summary>
    /// Entidade que representa um empréstimo de livro para um usuário.
    /// </summary>
    public class Emprestimo
    {
        public int Id { get; private set; }

        public int LivroId { get; private set; }

        public Livro Livro { get; private set; } = default!;

        public int UsuarioId { get; private set; }

        public Usuario Usuario { get; private set; } = default!;

        public DateTime DataEmprestimo { get; private set; }

        public DateTime DataPrevistaDevolucao { get; private set; }

        public DateTime? DataDevolucao { get; private set; }

        public StatusEmprestimo Status { get; private set; }

        private Emprestimo()
        {
        }

        public Emprestimo(Livro livro, Usuario usuario, DateTime dataPrevistaDevolucao)
        {
            ValidarDados(livro, usuario, dataPrevistaDevolucao);

            livro.MarcarComoEmprestado();

            Livro = livro;
            Usuario = usuario;
            LivroId = livro.Id;
            UsuarioId = usuario.Id;
            DataEmprestimo = DateTime.Now;
            DataPrevistaDevolucao = dataPrevistaDevolucao.Date;
            Status = StatusEmprestimo.Ativo;
        }

        public Emprestimo(int id, Livro livro, Usuario usuario, DateTime dataPrevistaDevolucao)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id deve ser maior que zero.");

            ValidarDados(livro, usuario, dataPrevistaDevolucao);

            livro.MarcarComoEmprestado();

            Id = id;
            Livro = livro;
            Usuario = usuario;
            LivroId = livro.Id;
            UsuarioId = usuario.Id;
            DataEmprestimo = DateTime.Now;
            DataPrevistaDevolucao = dataPrevistaDevolucao.Date;
            Status = StatusEmprestimo.Ativo;
        }

        public void Devolver()
        {
            if (DataDevolucao is not null)
                throw new InvalidOperationException("Empréstimo já foi devolvido.");

            DataDevolucao = DateTime.Now;

            if (Livro is not null)
                Livro.MarcarComoDisponivel();

            Status = DataDevolucao.Value.Date > DataPrevistaDevolucao.Date
                ? StatusEmprestimo.DevolvidoComAtraso
                : StatusEmprestimo.Devolvido;
        }

        public bool EstaAtrasado(DateTime dataReferencia)
        {
            DateTime dataComparacao = DataDevolucao?.Date ?? dataReferencia.Date;
            return dataComparacao > DataPrevistaDevolucao.Date;
        }

        public void AtualizarStatus(DateTime dataReferencia)
        {
            if (DataDevolucao is not null)
            {
                Status = DataDevolucao.Value.Date > DataPrevistaDevolucao.Date
                    ? StatusEmprestimo.DevolvidoComAtraso
                    : StatusEmprestimo.Devolvido;

                return;
            }

            Status = dataReferencia.Date > DataPrevistaDevolucao.Date
                ? StatusEmprestimo.Atrasado
                : StatusEmprestimo.Ativo;
        }

        private static void ValidarDados(Livro livro, Usuario usuario, DateTime dataPrevistaDevolucao)
        {
            if (livro is null)
                throw new ArgumentNullException(nameof(livro));

            if (usuario is null)
                throw new ArgumentNullException(nameof(usuario));

            if (dataPrevistaDevolucao.Date < DateTime.Today)
                throw new ArgumentException("A data prevista não pode ser anterior a hoje.", nameof(dataPrevistaDevolucao));

            if (dataPrevistaDevolucao.Date > DateTime.Today.AddDays(365))
                throw new ArgumentException("A data prevista não pode ultrapassar 365 dias a partir de hoje.", nameof(dataPrevistaDevolucao));
        }
    }
}
