# 0018. Limpeza do que os agentes criam

**Data:** 2026-10-11
**Status:** aceita (ajusta a regra 0 do [AGENTS.md](../../AGENTS.md) e o fim do fluxo do [ADR 0008](0008-desenvolvimento-multiagente.md))

## Contexto

A regra 0 ("nunca apague o que as pessoas criaram") e o hook `bloquear-apagar` tratavam tudo como criado por pessoas, inclusive os worktrees e branches que os agentes abrem para trabalhar. A remoção ficava com uma pessoa, que raramente a fazia: em 2026-10-11 havia mais de 25 worktrees em `.claude/worktrees/`, cada um de client com a própria `Library/` do Unity, e branches remotas recriadas por push depois do merge.

Apagar à mão também é arriscado: com squash merge, o git não sabe que a branch entrou na `main`, e um commit feito depois do merge (ex.: `.meta` gerado pelo Unity) se perde sem aviso.

## Decisão

- A regra 0 vale para o que **pessoas** criaram. Worktrees e branches que agentes criam para uma tarefa são temporários e **devem** sair depois que o PR entra na `main`.
- A remoção é feita pelo `npm run limpar-agentes` (simula por padrão; `--aplicar` remove). Ele só remove um worktree ou branch quando:
  - não há mudança não commitada nem arquivo novo fora do `.gitignore`;
  - não foi mexido nas últimas 2 h (um agente pode estar começando; `--horas` muda);
  - não tem commit fora da `origin/main`, ou o PR da branch foi mesclado e não há commit depois do merge (ou o que veio depois já está igual na `main`).
- Por padrão olha só `.claude/worktrees/` e branches locais; `--todos` inclui outros worktrees e `--remotas` apaga no GitHub branches mescladas que sobraram (nunca com PR aberto).
- O hook continua bloqueando `git worktree remove`, `git branch -D` e `git push --delete` digitados direto e aponta para o script.
- O repositório já apaga a branch remota no merge; o script cobre as que voltam por push tardio.

## Consequências

- Disco e lista de branches limpos sem depender de alguém lembrar.
- Commit esquecido depois do merge aparece no relatório ("traga com cherry-pick num PR novo") em vez de sumir.
- Depende do `gh` autenticado para reconhecer PR mesclado; sem ele, branches com commits ficam (o script não arrisca).
- Worktree com o Unity aberto não sai (o `git worktree remove` falha) e é relatado.
