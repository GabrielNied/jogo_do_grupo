# 0029. Main protegida pelo GitHub com o repo público

**Data:** 2026-10-11
**Status:** aceita (substitui o [ADR 0007](0007-protecao-local-da-main.md))

## Contexto

Com o repo privado no plano grátis, a `main` só era protegida por combinado ([ADR 0007](0007-protecao-local-da-main.md)). Em 2026-10-11, com dezenas de PRs por dia feitos por agentes, dois PRs verdes mesclados um depois do outro quebraram a `main` várias vezes (#386, #387, #398, #412): 7 das 16 correções do dia foram "conserta o que quebrou na main". O dono tornou o repo **público** no mesmo dia, o que libera ruleset e minutos de Actions sem custo.

## Decisão

- O ruleset [proteger-main.json](../../.github/rulesets/proteger-main.json) fica **ativo** na `main`:
  - PR obrigatório, só squash, histórico linear, sem push forçado nem exclusão;
  - checks obrigatórios `csharpier`, `build` e `padrao-git`, com a **branch atualizada** com a `main` antes do merge ("Require branches to be up to date"). É isso que impede dois PRs verdes de quebrarem a `main` juntos;
  - conversas do review resolvidas.
- **Aprovação não é obrigatória** (0): quase todo PR é aberto por agente na conta do dono, que não pode aprovar o próprio PR, e esperar outra pessoa vira gargalo. O merge continua humano (AGENTS.md, regra 12), e pedir review ao responsável continua o combinado para PR que mexe em `Core/` ou na pasta de outra pessoa.
- Admins podem passar por cima **só pelo PR** (bypass "pull_request"), para emergência; não há push direto.
- Os checks obrigatórios rodam em **todo** PR (sem filtro de caminho), porque um check obrigatório que não roda trava o PR. Custam ~2,5 min.
- O CI "Client (Unity)" roda em todo PR que mexe em `client/` ou `shared/Package/` (~10 min), mas **não** é obrigatório: com a branch sempre atualizada, cada merge obrigaria os outros PRs a rodar o Unity de novo. Depois do merge ele continua rodando na `main` ([ADR 0019](0019-unity-no-ci.md)).
- Auto-merge do GitHub ligado: quem faz o merge pode marcar "Enable auto-merge" e o GitHub mescla quando os checks passarem.

## Consequências

- A `main` não fica mais vermelha por dois PRs que só funcionam separados.
- Quando a `main` anda, os PRs abertos precisam de "Update branch" e de uma rodada de ~2,5 min antes do merge.
- Um PR de client pode entrar com o Unity vermelho se quem mescla não olhar; o CI da `main` abre a pendência nesse caso.
- Voltar a ser privado no plano grátis desliga o ruleset: aí vale de novo o ADR 0007.
