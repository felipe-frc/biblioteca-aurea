namespace Biblioteca.Web.Constants
{
    public static class Messages
    {
        // ============================================
        // ADMIN - SUCESSO
        // ============================================
        public const string LogoutRealizado = "Logout realizado com sucesso.";

        // ============================================
        // ADMIN - ERRO
        // ============================================
        public const string ErroCredenciaisAdminNaoConfiguradas = "Credenciais administrativas não configuradas.";
        public const string ErroCredenciaisInvalidas = "Usuário ou senha inválidos.";

        // ============================================
        // CATÁLOGO - ERRO
        // ============================================
        public const string ErroCarregarCatalogo = "Erro ao carregar o catálogo público. Tente novamente.";

        // ============================================
        // DASHBOARD - ERRO
        // ============================================
        public const string ErroCarregarDashboard = "Erro ao carregar o dashboard administrativo.";

        // ============================================
        // LIVROS - SUCESSO
        // ============================================
        public const string LivroAdicionado = "Livro cadastrado com sucesso!";
        public const string LivroAtualizado = "Livro atualizado com sucesso!";
        public const string LivroRemovido = "Livro excluído com sucesso!";

        // ============================================
        // LIVROS - ERRO
        // ============================================
        public const string ErroCarregarLivros = "Erro ao carregar livros. Tente novamente.";
        public const string ErroSalvarLivro = "Erro ao salvar o livro. Verifique se os dados já existem.";
        public const string ErroSalvarLivroDadosInvalidos = "Erro ao salvar o livro. Verifique os dados informados.";
        public const string ErroSalvarLivroInesperado = "Ocorreu um erro inesperado ao salvar o livro.";
        public const string ErroAtualizarLivro = "Erro ao atualizar o livro.";
        public const string ErroAtualizarLivroInesperado = "Ocorreu um erro inesperado ao atualizar o livro.";
        public const string ErroExcluirLivro = "Erro ao excluir o livro. Tente novamente.";
        public const string ErroExcluirLivroInesperado = "Ocorreu um erro inesperado ao excluir o livro.";
        public const string ErroLivroComEmprestimo = "Não é possível excluir este livro porque ele já possui empréstimos registrados.";
        public const string ErroLivroNaoEncontrado = "Livro não encontrado.";

        // ============================================
        // USUÁRIOS - SUCESSO
        // ============================================
        public const string UsuarioAdicionado = "Usuário cadastrado com sucesso!";
        public const string UsuarioAtualizado = "Usuário atualizado com sucesso!";
        public const string UsuarioRemovido = "Usuário excluído com sucesso!";

        // ============================================
        // USUÁRIOS - ERRO
        // ============================================
        public const string ErroCarregarUsuarios = "Erro ao carregar usuários. Tente novamente.";
        public const string ErroSalvarUsuario = "Erro ao salvar o usuário.";
        public const string ErroSalvarUsuarioInesperado = "Ocorreu um erro inesperado ao salvar o usuário.";
        public const string ErroAtualizarUsuario = "Erro ao atualizar o usuário.";
        public const string ErroAtualizarUsuarioInesperado = "Ocorreu um erro inesperado ao atualizar o usuário.";
        public const string ErroExcluirUsuario = "Erro ao excluir o usuário. Tente novamente.";
        public const string ErroExcluirUsuarioInesperado = "Ocorreu um erro inesperado ao excluir o usuário.";
        public const string ErroUsuarioNaoEncontrado = "Usuário não encontrado.";
        public const string ErroNomeInvalido = "Informe um nome válido.";
        public const string ErroEmailDuplicado = "Este e-mail já está cadastrado.";
        public const string ErroEmailDuplicadoOutroUsuario = "Este e-mail já está cadastrado para outro usuário.";
        public const string ErroEmailInvalido = "Informe um e-mail válido.";
        public const string ErroUsuarioPossuiEmprestimoAtivo = "Não é possível excluir este usuário porque ele possui empréstimo ativo. Primeiro registre a devolução do empréstimo em aberto.";
        public const string ErroUsuarioComHistoricoEmprestimo = "Não é possível excluir este usuário porque ele já possui histórico de empréstimos na biblioteca. Para preservar a integridade dos registros, o cadastro deve permanecer no sistema.";
        public const string ErroUsuarioRelacionado = "Não foi possível excluir o usuário porque ele ainda está relacionado a registros do sistema.";

        // ============================================
        // EMPRÉSTIMOS - SUCESSO
        // ============================================
        public const string EmprestimoAdicionado = "Empréstimo registrado com sucesso!";
        public const string EmprestimoAtualizado = "Empréstimo atualizado com sucesso!";
        public const string EmprestimoDevolvidoComSucesso = "Empréstimo devolvido com sucesso!";

        // ============================================
        // EMPRÉSTIMOS - ERRO
        // ============================================
        public const string ErroCarregarEmprestimos = "Erro ao carregar empréstimos. Tente novamente.";
        public const string ErroSalvarEmprestimo = "Erro ao registrar empréstimo.";
        public const string ErroSalvarEmprestimoInesperado = "Ocorreu um erro inesperado ao registrar o empréstimo.";
        public const string ErroAtualizarEmprestimo = "Erro ao atualizar empréstimo.";
        public const string ErroExcluirEmprestimo = "Erro ao excluir empréstimo. Tente novamente.";
        public const string ErroEmprestimoNaoEncontrado = "Empréstimo não encontrado.";
        public const string ErroLivroNaoDisponivel = "Este livro não está disponível para empréstimo.";
        public const string ErroUsuarioComEmprestimoAtivo = "Este usuário já possui um empréstimo ativo deste livro.";
        public const string ErroUsuarioPossuiEmprestimoAtrasado = "Este usuário possui empréstimo em atraso. Registre a devolução antes de realizar um novo empréstimo.";
        public const string ErroUsuarioOuLivroNaoEncontrado = "Usuário ou livro não encontrado.";
        public const string ErroUsuarioInvalido = "Usuário inválido.";
        public const string ErroLivroInvalido = "Livro inválido.";
        public const string ErroDataDevolucaoInvalida = "A data de devolução deve ser no futuro.";
        public const string ErroDataPrevistaAnteriorHoje = "A data prevista não pode ser anterior a hoje.";
        public const string ErroDataPrevistaMaiorQueUmAno = "A data prevista não pode ultrapassar 365 dias a partir de hoje.";
        public const string ErroRegistrarDevolucao = "Ocorreu um erro inesperado ao registrar a devolução.";

        // ============================================
        // VALIDAÇÃO
        // ============================================
        public const string ErroValidacao = "Erro de validação. Verifique os dados informados.";
        public const string ErroConflitoConcorrencia = "O registro foi modificado por outro usuário. Atualize a página e tente novamente.";

        // ============================================
        // GERAL
        // ============================================
        public const string ErroInesperado = "Ocorreu um erro inesperado. Tente novamente.";
    }
}
