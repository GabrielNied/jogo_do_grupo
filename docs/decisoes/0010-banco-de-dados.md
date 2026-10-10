# 0010. Banco de dados: EF Core com SQLite, depois PostgreSQL

**Data:** 2026-10-10
**Status:** aceita

## Contexto

Contas e personagens precisam ser salvos. O grupo quer que qualquer pessoa rode o servidor sem instalar nada além do .NET.

## Decisão

- **Entity Framework Core** como camada de acesso a dados.
- **SQLite** no desenvolvimento: um arquivo local criado pelo próprio servidor, fora do git (`*.db` no `.gitignore`).
- **PostgreSQL** quando o servidor for hospedado, trocando só o provedor do EF Core.
- **Migrações** do EF Core versionadas no repo (`dotnet dotnet-ef`, instalado como ferramenta local em `dotnet-tools.json`).
- **Sem arquivo central de entidades:** cada funcionalidade declara as suas entidades e configurações na própria pasta, e o `DbContext` do Core as descobre sozinho (como os módulos).
- Operações de economia (ouro, itens, trocas) sempre numa **única transação** (veja "Economia atômica" em [arquitetura.md](../arquitetura.md)).

## Consequências

- Rodar o servidor continua sendo só `dotnet run`.
- Algumas diferenças entre SQLite e PostgreSQL (tipos, concorrência) podem aparecer na troca; o grupo testa no PostgreSQL antes de hospedar.
- Quem muda uma entidade gera a migração no mesmo PR.
