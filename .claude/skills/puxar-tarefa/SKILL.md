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
- [Worktree ao puxar](../../../docs/agentes.md#worktree-ao-puxar): **uma pasta por lote**. No checkout principal, com outro lote ativo ou com `--agente`, `--worktree <pasta>` é obrigatório (o script recusa). Agente já num worktree isolado: `--worktree .`.
- Cria a branch sempre da `origin/main` (ou de `--base`, em leva empilhada), com `git fetch origin main` na hora.
- O fetch do início do `/trabalhe`, `/decida` ou `/melhore` ([Doc e regras atuais](../../../docs/agentes.md#doc-e-regras-atuais)) não dispensa o do `puxar`: quem vai codificar parte da branch que o script acaba de criar.
- Pendência de Unity com PR aberto: usa a branch do PR.

Já em andamento por outro: pare e avise. Puxar de novo na própria branch só confirma.

**Segure:** issue de escopo reservado (`segure:*` ou bloco ativo no registro) só entra se o `gh auth` for o dono do segure; senão o script recusa.

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
