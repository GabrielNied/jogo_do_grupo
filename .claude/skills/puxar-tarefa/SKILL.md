---
name: puxar-tarefa
description: Confere se uma tarefa está livre, puxa-a para em andamento (bloqueia concorrência), registra no GitHub Issues/Project e cria a branch de desenvolvimento. Use antes de iniciar o desenvolvimento de qualquer tarefa ou funcionalidade, ou ao liberar/trocar de tarefa.
---

# Puxar tarefa (gestão e concorrência)

Evita que mais de uma pessoa ou agente trabalhe na mesma tarefa ao mesmo tempo. Ninguém começa a codificar sem puxar a tarefa antes.

## 1. Listar tarefas disponíveis

```
npm run tarefa -- listar
```

Confira o quadro:
- **EM ANDAMENTO:** já tem alguém trabalhando (bloqueada contra concorrência). **Não mexa.**
- **DISPONÍVEIS:** abertas e livres. Escolha uma daqui.
- **BLOQUEADAS:** dependem de decisão de design ou contrato pendente.

## 2. Puxar a tarefa

```
npm run tarefa -- puxar <numero> [nome-da-branch] [--agente <papel>] [--worktree <caminho>]
```

Exemplos:
- Pessoa: `npm run tarefa -- puxar 36` (deduz a branch automaticamente ou você passa uma)
- Agente em worktree: `npm run tarefa -- puxar 36 feature/core/entrada-no-jogo-client --agente client-core --worktree ../jdg-core-client`

O que o comando faz:
1. Valida se a issue existe e se está aberta.
2. **Confere se já está em andamento:** se outro agente ou pessoa já puxou, o comando **recusa a ação e para imediatamente** para evitar trabalho duplicado.
3. Adiciona a label `em-andamento` na issue.
4. Move o status no GitHub Projects para `In Progress`.
5. Atribui ao desenvolvedor (`@me`) ou anota o papel do agente.
6. Publica comentário na issue registrando o início e a branch.
7. Atualiza com a `origin/main` e cria a branch (ou worktree) no padrão `<tipo>/<escopo>/<descricao>`.

## 2.1. Plano antes de codar (tarefa de jogo)

Logo depois de puxar, escreva o plano num comentário da issue (`gh issue comment <N>`), ou confirme o da ficha se o coordenador já fez: casos de uso, estados da checklist de experiência que se aplicam e os testes que saem deles. Formato e checklist em [Plano da tarefa](../../../docs/agentes.md#plano-da-tarefa). Siga em frente; só pare se houver dúvida de regra. Tarefa de processo, docs ou infra não precisa.

## 3. Subtarefas para vários agentes em paralelo

Quando uma issue é um épico ou conjunto de tarefas para múltiplos agentes (ex.: contrato + server + client):

```
npm run tarefa -- subtarefa <parent-id> --titulo "<titulo>" --agente <papel> [--escopo <escopo>]
```

Isso cria uma issue filha vinculada, com a label do agente (`agente:<papel>`), permitindo que cada agente puxe e trave sua própria tarefa sem conflito com os outros.

## 4. Liberar a tarefa (se não for concluir)

Se você ou o agente precisar pausar, cancelar ou transferir o trabalho antes de abrir o PR:

```
npm run tarefa -- liberar <numero> --motivo "Pausa ou impedimento"
```

O que o comando faz:
- Remove a label `em-andamento`.
- Remove atribuição e move no Project de volta para `Todo`.
- Comenta na issue o motivo da liberação para que outra pessoa ou agente possa assumir.

## 5. Pendências de Unity

Sem Unity no ambiente (ex.: nuvem), o agente abre o PR normal e registra o que não conseguiu fazer ou validar:

```
npm run tarefa -- pendencia-unity <pr> --falta "compilar e rodar os testes EditMode" --falta ".meta das texturas novas"
```

Isso cria uma issue `pendente-unity` com a branch e os passos. O PR só menciona a pendência (sem `Closes`), então ela continua aberta depois do merge. O `listar` mostra as pendências numa seção própria, e numa máquina com Unity o início da sessão do Claude Code avisa delas. Quem tem Unity puxa com `npm run tarefa -- puxar <N>`: com o PR aberto, o comando abre a branch dele; com o PR já na `main`, cria `chore/<escopo>/unity-pendencia-<N>`.

## 6. Conclusão

Ao abrir o PR, adicione sempre `Closes #<numero>` no corpo do PR. No squash merge da `main`, a issue é fechada e o item vai para `Done`. Se precisar marcar manualmente:

```
npm run tarefa -- concluir <numero>
```
