# 0006. Fluxo do Git com main protegida e commitlint

**Data:** 2026-10-10
**Status:** aceita; a parte do ruleset foi substituída pelo [ADR 0007](0007-protecao-local-da-main.md) (repo privado)

## Contexto

Várias pessoas (e assistentes de IA) vão trabalhar no mesmo repositório. Sem regras aplicadas por ferramenta, alguém acaba enviando direto para a `main`, o review do CODEOWNERS é pulado e o histórico fica sem padrão. O grupo conhece TypeScript, então ferramentas em Node não são barreira.

## Decisão

- **`main` protegida por ruleset do GitHub**, versionado em `.github/rulesets/proteger-main.json`: sem push direto, sem force push, PR com 1 aprovação, review do CODEOWNER, conversas resolvidas, checks `csharpier`, `build` e `padrao-git` obrigatórios, branch atualizada e só squash merge. Sem bypass para ninguém.
- **Commits no Conventional Commits**, validados pelo **commitlint** (`@commitlint/config-conventional`), com escopo obrigatório entre `core` e as funcionalidades. Tipos e escopos ficam em `commitlint.config.mjs`.
- **Branches `<tipo>/<escopo>/<descricao>`**, validadas por `scripts/git/validar-branch.mjs`, que lê as listas do `commitlint.config.mjs`.
- **Hooks locais com husky** (`npm install` ativa): `commit-msg` roda o commitlint e `pre-push` bloqueia push na `main` e branch fora do padrão.
- **No CI** (`padrao-git.yml`), o título do PR passa pelo commitlint, porque no squash merge ele vira o commit na `main`; o nome da branch também é validado.
- O workflow de build deixa de filtrar por `paths`, porque um check obrigatório que não roda trava o PR.

## Consequências

- Ninguém consegue alterar a `main` sem PR aprovado e CI verde, nem os admins.
- O histórico da `main` fica linear, com um commit por PR e mensagens padronizadas (dá para gerar changelog depois).
- Cada clone precisa de Node 20+ e `npm install`. Quem pular esse passo perde só a validação local; o CI confere tudo de novo.
- Funcionalidade nova precisa entrar na lista de escopos (a skill `nova-funcionalidade` cobre isso).
- Com o grupo pequeno, exigir aprovação pode atrasar PRs; se travar, dá para reduzir as regras no ruleset (e atualizar o JSON por PR).
