[![CI (.NET)](https://github.com/felipe-frc/biblioteca-aurea/actions/workflows/main_biblioteca-aurea.yml/badge.svg)](https://github.com/felipe-frc/biblioteca-aurea/actions)
[![codecov](https://codecov.io/github/felipe-frc/biblioteca-aurea/graph/badge.svg?token=2L8U69ZZ33)](https://codecov.io/github/felipe-frc/biblioteca-aurea)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

# 📚 Biblioteca Áurea

Sistema web de gerenciamento de biblioteca desenvolvido com **ASP.NET Core MVC**, **Entity Framework Core** e **Azure SQL Server**, com foco em arquitetura em camadas, regras de negócio, autenticação administrativa, testes automatizados, cobertura de testes, CI/CD e deploy em nuvem.

O projeto simula um sistema real para controle de biblioteca, com **catálogo público para visitantes**, **área administrativa protegida por login**, **dashboard com indicadores**, cadastro de livros e usuários, controle de empréstimos, devoluções, atrasos e histórico de movimentações.

---

## 🔗 Links Rápidos

- 🌐 **Deploy:** [Biblioteca Áurea no Azure](https://biblioteca-aurea-gec0a3cnafddecgz.brazilsouth-01.azurewebsites.net/)
- 📂 **Repositório:** [github.com/felipe-frc/biblioteca-aurea](https://github.com/felipe-frc/biblioteca-aurea)
- 🧪 **Actions:** [GitHub Actions](https://github.com/felipe-frc/biblioteca-aurea/actions)
- 📊 **Cobertura:** [Codecov](https://codecov.io/github/felipe-frc/biblioteca-aurea)

> ⚠️ A aplicação está hospedada no plano gratuito do Azure App Service. O primeiro acesso pode levar alguns segundos enquanto o servidor inicializa.

---

## 📌 Objetivo do Projeto

A **Biblioteca Áurea** foi criada para demonstrar, em um projeto completo de portfólio, conhecimentos práticos em desenvolvimento web com .NET.

O projeto cobre desde regras de domínio até publicação em nuvem, incluindo:

- desenvolvimento web com **ASP.NET Core MVC**;
- persistência com **Entity Framework Core** e **Azure SQL Server**;
- autenticação por cookie para proteção da área administrativa;
- separação entre área pública e área administrativa;
- regras de negócio para livros, usuários, empréstimos e devoluções;
- testes automatizados com **xUnit**;
- cobertura de testes com **Coverlet** e **Codecov**;
- CI/CD com **GitHub Actions**;
- deploy no **Azure App Service**;
- configuração segura com **User Secrets** e variáveis de ambiente.

---

## ⭐ Destaques Técnicos

- Arquitetura organizada em projetos separados para domínio, aplicação web e testes.
- Catálogo público acessível sem login.
- Área administrativa protegida por autenticação.
- Dashboard com indicadores de acervo, usuários e empréstimos.
- Controle de empréstimos ativos, atrasados, devolvidos e devolvidos com atraso.
- Validações de regras de negócio no domínio.
- Entity Framework Core com migrations.
- Azure SQL Server como banco de dados em produção.
- Configuração segura de credenciais fora do repositório.
- Testes unitários, testes de services, ViewModels, controllers e integração.
- Pipeline automatizado de build, testes, cobertura e deploy.
- README estruturado para apresentação técnica e avaliação de portfólio.

---

## 🚀 Funcionalidades

### 🌐 Área Pública

A área pública permite que visitantes consultem o acervo sem autenticação.

- Página inicial institucional;
- Catálogo público de livros;
- Busca por título, autor, gênero ou ISBN;
- Filtro por disponibilidade;
- Paginação no catálogo;
- Exibição de informações bibliográficas;
- Layout responsivo.

### 🔐 Área Administrativa

A área administrativa é protegida por login e concentra as operações de gerenciamento.

#### 📊 Dashboard

- Total de livros;
- Total de usuários;
- Total de empréstimos;
- Empréstimos ativos;
- Empréstimos atrasados;
- Indicadores resumidos para acompanhamento do acervo.

#### 📚 Livros

- Cadastro de livros;
- Edição de livros;
- Exclusão com regras de proteção;
- Listagem administrativa;
- Busca e filtros;
- Controle de disponibilidade;
- Dados bibliográficos como título, autor, gênero, ISBN, editora, ano de publicação e quantidade de páginas.

#### 👥 Usuários

- Cadastro de usuários;
- Edição de usuários;
- Exclusão com bloqueios quando houver empréstimos vinculados;
- Listagem administrativa;
- Busca por dados do usuário.

#### 🔄 Empréstimos

- Registro de novos empréstimos;
- Devolução de livros;
- Cálculo de prazo previsto de devolução;
- Identificação de empréstimos em atraso;
- Diferenciação entre empréstimos atrasados em aberto e empréstimos devolvidos fora do prazo;
- Histórico de movimentações.

---

## 🔐 Acesso Administrativo

A aplicação possui uma área administrativa protegida para demonstrar autenticação, autorização de acesso e separação entre operações públicas e operações internas.

Por segurança, as credenciais reais do ambiente publicado no Azure **não são armazenadas no repositório** e **não são expostas no README**.

Para avaliação técnica, o projeto pode ser executado localmente com credenciais configuradas por **User Secrets**, conforme a seção [Como Executar](#️-como-executar).

As chaves utilizadas são:

```txt
AdminCredentials:Username
AdminCredentials:Password
```

No Azure App Service, as mesmas credenciais devem ser configuradas como variáveis de ambiente:

```txt
AdminCredentials__Username
AdminCredentials__Password
```

---

## 🛠️ Tecnologias

### Back-end

- C#
- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- Azure SQL Server
- Cookie Authentication
- Razor Views
- Bootstrap

### Testes e Qualidade

- xUnit
- EF Core InMemory
- Coverlet
- Codecov
- GitHub Actions

### Deploy e Infraestrutura

- Azure App Service
- GitHub Actions
- User Secrets
- Variáveis de ambiente
- Git/GitHub

---

## 🏗️ Arquitetura

O projeto utiliza uma organização em camadas para separar responsabilidades, facilitar manutenção e permitir evolução gradual.

```txt
biblioteca-aurea/
│
├── Biblioteca/                    # Domínio e regras de negócio
│   ├── Domain/Entities/           # Livro, Usuario, Emprestimo
│   ├── Domain/Enums/              # StatusEmprestimo
│   └── Services/                  # Serviços e validações de domínio
│
├── Biblioteca.Web/                # Aplicação web ASP.NET Core MVC
│   ├── Controllers/               # Controllers MVC
│   ├── Views/                     # Razor Views
│   ├── ViewModels/                # Modelos usados pelas telas
│   ├── Data/                      # DbContext, migrations, repositories e Unit of Work
│   ├── Services/                  # Serviços de aplicação
│   ├── Constants/                 # Mensagens centralizadas
│   ├── Helpers/                   # Helpers da aplicação
│   ├── wwwroot/                   # Arquivos estáticos
│   ├── appsettings.json           # Configuração base
│   ├── appsettings.example.json   # Template sem credenciais reais
│   └── Program.cs                 # Configuração da aplicação e DI
│
├── Biblioteca.Tests/              # Testes automatizados
│   ├── Controllers/               # Testes de controllers MVC
│   ├── Services/                  # Testes de services
│   ├── ViewModels/                # Testes de validação
│   ├── Integration/               # Testes de integração
│   ├── EmprestimoTests.cs
│   ├── LivroTests.cs
│   └── UsuarioTests.cs
│
├── docs/images/                   # Imagens utilizadas na documentação
│
├── .github/workflows/             # Pipelines de CI/CD
│   ├── ci.yml                     # Build, testes e cobertura
│   └── main_biblioteca-aurea.yml  # Build, testes, publish e deploy no Azure
│
├── Biblioteca.sln                 # Solution do projeto
├── README.md
├── LICENSE.txt
└── .gitignore
```

> Observação: o projeto começou como uma versão console em memória nas primeiras releases. A estrutura atual foi evoluída para uma aplicação web ASP.NET Core MVC com domínio, aplicação web, testes automatizados, CI/CD e deploy no Azure.

---

## 📸 Interface do Sistema

### 🏠 Home

![Home](docs/images/home.png)

### 📖 Catálogo Público

![Catálogo Público](docs/images/catalogo-publico.png)

### 🔐 Login Administrativo

![Login Administrativo](docs/images/login-administrativo.png)

### 📊 Dashboard Administrativo

![Dashboard Administrativo](docs/images/dashboard-administrativo.png)

### 📋 Listagem de Livros

![Listagem de Livros](docs/images/livros-listagem.png)

### ➕ Cadastro de Livro

![Cadastro de Livro](docs/images/livro-cadastro.png)

### ✏️ Edição de Livro

![Edição de Livro](docs/images/livro-edicao.png)

### 👥 Listagem de Usuários

![Listagem de Usuários](docs/images/usuarios-listagem.png)

### ➕ Cadastro de Usuário

![Cadastro de Usuário](docs/images/usuario-cadastro.png)

### ✏️ Edição de Usuário

![Edição de Usuário](docs/images/usuario-edicao.png)

### 🔄 Controle de Empréstimos

![Controle de Empréstimos](docs/images/emprestimos-listagem.png)

### ➕ Novo Empréstimo

![Novo Empréstimo](docs/images/emprestimo-cadastro.png)

---

## ⚙️ Como Executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/) com extensão C#
- [Git](https://git-scm.com/)
- Entity Framework Core CLI
- Banco SQL Server ou Azure SQL Server configurado

Caso ainda não tenha o EF CLI instalado:

```bash
dotnet tool install --global dotnet-ef
```

---

### 1. Clone o repositório

```bash
git clone https://github.com/felipe-frc/biblioteca-aurea.git
cd biblioteca-aurea
```

---

### 2. Restaure as dependências

```bash
dotnet restore Biblioteca.sln
```

---

### 3. Consulte o template de configuração

O projeto possui um arquivo de exemplo com os campos necessários para configuração local:

```txt
Biblioteca.Web/appsettings.example.json
```

Esse arquivo serve apenas como referência e **não contém credenciais reais**.

Ele indica quais chaves precisam existir para a aplicação funcionar localmente ou em produção:

```txt
ConnectionStrings__DefaultConnection
AdminCredentials__Username
AdminCredentials__Password
```

---

### 4. Configure a connection string com User Secrets

Por segurança, a connection string real não fica salva no `appsettings.json`.

```bash
cd Biblioteca.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "SUA_CONNECTION_STRING_DO_SQL_SERVER"
```

---

### 5. Configure as credenciais administrativas com User Secrets

As credenciais do administrador também não ficam no repositório.

```bash
dotnet user-secrets set "AdminCredentials:Username" "admin"
dotnet user-secrets set "AdminCredentials:Password" "SUA_SENHA_ADMINISTRATIVA"
```

No Azure App Service, configure as credenciais nas variáveis de ambiente usando:

```txt
AdminCredentials__Username
AdminCredentials__Password
```

---

### 6. Aplique as migrations

```bash
dotnet ef database update --project Biblioteca.Web --startup-project Biblioteca.Web
```

---

### 7. Execute a aplicação

Volte para a raiz do projeto e execute:

```bash
cd ..
dotnet run --project Biblioteca.Web
```

Acesse no navegador:

```txt
http://localhost:5026
```

---

### 8. Execute os testes

```bash
dotnet test Biblioteca.sln
```

---

### 9. Execute os testes com cobertura

```bash
dotnet test Biblioteca.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

---

## ✅ Qualidade e Testes

O projeto possui testes automatizados cobrindo regras de domínio, validações, services, ViewModels, controllers e fluxos de integração.

A pipeline de CI executa:

```txt
dotnet restore
dotnet build
dotnet test
coleta de cobertura
upload de cobertura para o Codecov
```

A pipeline principal também executa build, testes, publish e deploy para o Azure App Service.

---

## 🧠 Decisões de Desenvolvimento

### Arquitetura em camadas

A separação entre `Biblioteca` para domínio, `Biblioteca.Web` para aplicação MVC e `Biblioteca.Tests` para testes mantém o projeto mais organizado, testável e preparado para evolução.

### Catálogo público separado da área administrativa

Visitantes podem consultar o acervo sem login, enquanto operações de gerenciamento exigem autenticação. Essa separação aproxima o projeto de um cenário real: consulta pública de dados e controle interno protegido.

### Autenticação por cookie

A autenticação por cookie foi escolhida por ser adequada para aplicações MVC tradicionais. As credenciais administrativas são configuradas fora do repositório por User Secrets no ambiente local e por variáveis de ambiente no Azure.

### Regras de empréstimo no domínio

A entidade `Emprestimo` concentra comportamentos importantes, como atualização de status, verificação de atraso e diferenciação entre empréstimos ativos, atrasados, devolvidos e devolvidos com atraso.

Essa modelagem evita que regras críticas fiquem espalhadas apenas em controllers ou views.

### Entity Framework Core com Azure SQL Server

O projeto utiliza EF Core com Azure SQL Server para aproximar a aplicação de um ambiente real de produção. As migrations permitem controlar a evolução do banco de dados de forma versionada.

### User Secrets, variáveis de ambiente e template de configuração

Nenhum dado sensível deve ser armazenado no repositório. O arquivo `Biblioteca.Web/appsettings.example.json` serve como template para orientar a configuração local sem expor valores reais.

### CI/CD com GitHub Actions

O projeto possui pipeline automatizado para build, testes, cobertura e deploy. Isso reduz risco de regressão e demonstra uma rotina mais próxima de desenvolvimento profissional.

### Bootstrap + Razor Views

O Bootstrap foi utilizado para acelerar a construção da interface e manter o foco principal do projeto em arquitetura, regras de negócio, persistência, autenticação, testes e deploy.

---

## 🧾 Releases

| Versão | Destaque |
| ------ | -------- |
| [v3.6.1](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.6.1) | Template de configuração e documentação de ambiente |
| [v3.6.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.6.0) | Cobertura de testes, Codecov e qualidade da suíte automatizada |
| [v3.5.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.5.0) | Paginação no catálogo, CI/CD refinado e padronização de mensagens |
| [v3.4.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.4.0) | Testes ampliados e melhoria no domínio de empréstimos |
| [v3.3.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.3.0) | Expansão da cobertura de testes |
| [v3.2.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.2.0) | Prazo previsto e empréstimos em atraso |
| [v3.1.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.1.0) | Dashboard administrativo |
| [v3.0.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v3.0.0) | Catálogo público e área administrativa com login |
| [v2.4.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v2.4.0) | Dados bibliográficos dos livros |
| [v2.3.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v2.3.0) | Deploy no Azure App Service |
| [v2.2.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v2.2.0) | Migração para Azure SQL Server |
| [v2.1.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v2.1.0) | Arquitetura em camadas e documentação técnica |
| [v2.0.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v2.0.0) | CRUD MVC completo |
| [v1.0.1](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v1.0.1) | Testes automatizados |
| [v1.0.0](https://github.com/felipe-frc/biblioteca-aurea/releases/tag/v1.0.0) | Primeira versão console em memória |

---

## 📈 Melhorias Futuras

- Criar API REST para consumo por aplicações externas;
- Adicionar Docker para facilitar execução local;
- Evoluir cobertura de testes acima de 60%;
- Criar testes E2E para os principais fluxos administrativos;
- Melhorar acessibilidade e responsividade;
- Adicionar cadastro de categorias de livros;
- Configurar domínio personalizado.

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE.txt](LICENSE.txt) para mais detalhes.

---

## 👨🏻‍💻 Autor

**Marcos Felipe França**

[LinkedIn](https://www.linkedin.com/in/marcosfelipefrc) · [GitHub](https://github.com/felipe-frc)