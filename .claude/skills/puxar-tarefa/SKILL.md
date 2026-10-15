---
name: puxar-tarefa
description: Puxa uma tarefa ou um lote para em andamento (trava contra concorrência) e cria a branch, ou libera a tarefa. Use antes de codar qualquer tarefa, ou ao pausar ou trocar de tarefa.
---

# Puxar tarefa

Ninguém codifica sem puxar antes (regra 15). O script diz o que fez e o próximo passo; `npm run tarefa -- ajuda` lista todas as opções.

## 1. Puxar

```
npm run tarefa -- puxar <N> [<M> ...] [branch] [--agente <papel>] [--worktree <caminho>] [--base <branch>]
```

- Vários números são um [lote](../../../docs/agentes.md#lote): uma branch e uma trava por issue. Se qualquer uma estiver em andamento por outro ou `bloqueada`, nada é puxado.
- Recusa o quarto lote de jogo em andamento.
- Cria a branch sempre da `origin/main` (ou de `--base`, em leva empilhada). Sessão que não é dona do checkout principal usa `--worktree`; agente que já nasceu num worktree passa `--worktree .`.
- Pendência de Unity com PR aberto: usa a branch do PR.

Já em andamento por outro: pare e avise. Puxar de novo na própria branch só confirma.

## 2.1. Conferir a relevância

Se a doc que a issue segue mudou depois dela, o `puxar` mostra **⚠ REVISAR RELEVÂNCIA** e o `git diff` exato. Rode só esse diff (não releia a doc inteira). **A doc atual vence a issue.**

- **Ainda vale:** siga.
- **Desatualizada em parte:** ajuste o corpo (`gh issue edit <N> --body-file`), registre com `npm run tarefa -- relevancia <N> --conferida "<o que mudou>"` e siga.
- **Feita ou obsoleta:** não implemente. `npm run tarefa -- liberar <N> --motivo "<o que a substituiu>"` e o rótulo `decisao`; o `/decida` fecha.

## 2.2. Plano antes de codar

Depois do `puxar`, a base mudou: releia só os trechos que vai editar antes de aplicar um diagnóstico antigo. Tarefa de jogo: escreva o [plano](../../../docs/agentes.md#plano-da-tarefa) na issue (`gh issue comment <N>`), ou confirme o da ficha, e siga.

## 3. Liberar ou concluir

- Pausar ou desistir: `npm run tarefa -- liberar <N> [<M>...] --motivo "..."`.
- O PR leva `Closes #<N>` de cada issue; o merge fecha e move para Done.
- Sem Unity, o que só o Unity cria: `npm run tarefa -- pendencia-unity <PR> --falta "..."` (uma por item; conferir tela, compilar e testar não entram).
