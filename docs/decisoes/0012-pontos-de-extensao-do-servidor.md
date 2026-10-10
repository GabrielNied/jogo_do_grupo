# 0012. Pontos de extensão do servidor e migrações num lugar só

**Data:** 2026-10-10
**Status:** proposta

## Contexto

Com a API HTTP ([ADR 0009](0009-api-http-para-conta-e-personagens.md)) e o banco ([ADR 0010](0010-banco-de-dados.md)), cada funcionalidade precisa registrar serviços, rotas e entidades sem editar um arquivo central. Os 14 módulos já existem e implementam `IFeatureModule`.

Opções avaliadas para os pontos de extensão: interfaces separadas (`IModuloComRotas`, `IModuloComServicos`), atributos com reflection ou métodos opcionais na própria `IFeatureModule`. Para as migrações do EF Core: uma pasta só ou uma pasta por funcionalidade (um `DbContext` por funcionalidade).

## Decisão

- **Métodos padrão em `IFeatureModule`:** `ConfigurarServicos(IServiceCollection, IConfiguration)` e `MapearRotas(IEndpointRouteBuilder)`, com corpo vazio. O módulo só sobrescreve o que usa; nenhum módulo existente precisou mudar.
- **Um `DbContext` só** (`JogoDbContext`, no Core) que descobre as entidades pelas classes `IEntityTypeConfiguration<T>` de cada funcionalidade.
- **Migrações numa pasta só:** `server/src/JogoDoGrupo.Server/Core/Persistencia/Migrations/`, com um único snapshot do modelo (padrão do EF Core para um `DbContext`).
- Em desenvolvimento, o servidor aplica as migrações ao iniciar. Em produção, a aplicação das migrações fica para quando o servidor for hospedado.

## Consequências

- Adicionar rotas, serviços ou tabelas continua sem arquivo central de registro.
- Um `DbContext` permite transações que cruzam funcionalidades (necessário para a economia atômica).
- O snapshot das migrações é um arquivo compartilhado: dois PRs com migração ao mesmo tempo conflitam nele. Quem fizer merge depois atualiza a branch e gera a migração de novo (`dotnet dotnet-ef migrations remove` e `add`).
- Agentes de funcionalidade precisam de permissão para escrever em `Core/Persistencia/Migrations/` (a pasta fica fora da área deles hoje), ou uma pessoa gera a migração.
