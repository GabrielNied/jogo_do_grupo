# 0010. Banco de dados: EF Core com SQLite, depois PostgreSQL

**Data:** 2026-10-10
**Status:** aceita

## Contexto

Contas e personagens precisam ser salvos. O grupo quer que qualquer pessoa rode o servidor sem instalar nada além do .NET.

## Decisão

- **Entity Framework Core** como camada de acesso a dados.
- **Provedor selecionável:** via configuração `Banco:Provedor` (`Sqlite` | `Postgres`), com padrão `Sqlite`.
- **SQLite** no desenvolvimento: um arquivo local criado pelo próprio servidor, fora do git (`*.db` no `.gitignore`).
- **PostgreSQL** quando o servidor for hospedado, trocando o provedor do EF Core para `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Migrações separadas por provedor:**
  - Migrações SQLite em `Core/Persistencia/Migrations/` gerenciadas por `JogoDbContext`.
  - Migrações PostgreSQL em `Core/Persistencia/Migrations/Postgres/` gerenciadas por `PostgresJogoDbContext`.
  - Ambas as classes compartilham as mesmas entidades e mapeamentos descobertos por reflexão.
  - O comando `dotnet ef` usa `--context PostgresJogoDbContext --output-dir Core/Persistencia/Migrations/Postgres` para o PostgreSQL.
- **Sem arquivo central de entidades:** cada funcionalidade declara as suas entidades e configurações na própria pasta, e o `DbContext` do Core as descobre sozinho (como os módulos).
- Operações de economia (ouro, itens, trocas) sempre numa **única transação** (veja "Economia atômica" em [arquitetura.md](../arquitetura.md)).
- Testes no CI rodam contra PostgreSQL via container de serviço no GitHub Actions.

## Consequências

- Rodar o servidor continua sendo só `dotnet run`.
- O servidor hospedado roda com PostgreSQL conectando por connection string e migrando o esquema no início quando configurado.
- Quem muda uma entidade gera a migração no mesmo PR para SQLite e para PostgreSQL.
