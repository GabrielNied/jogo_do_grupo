# 0023. Tarefas para conflitos de PR

**Data:** 2026-10-11
**Status:** aceita

## Contexto

Conflitos entre uma PR e sua base podem ficar sem responsável quando o autor não está disponível.
Hooks locais não veem com confiança o estado remoto da PR.

## Decisão

- Uma GitHub Action consulta PRs atualizadas e, quando a base conflita, cria uma issue de Core.
- A Action procura uma issue aberta com o mesmo título antes de criar outra.
- Quem puxar a tarefa resolve por merge normal e abre uma PR para a branch original.

## Consequências

O conflito entra no quadro normal de tarefas sem concorrência. A automação não faz merge, rebase,
force-push nem fecha issues ou PRs.
