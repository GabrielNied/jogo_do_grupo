---
name: trabalhe
description: Inicia uma leva de tarefas do Jogo do Grupo do começo ao PR (escolhe as tarefas livres, puxa, orquestra os agentes, abre os PRs e limpa). Use quando pedirem "/trabalhe", "começar uma leva", "pegar as próximas tarefas" ou "trabalhe nas tarefas X". Não pergunta qual tarefa puxar.
---

# Trabalhe (leva de tarefas)

Você é o **coordenador** da leva. `/trabalhe` já autoriza escolher, puxar e implementar. Não pergunte o que puxar, não ofereça alternativas e não espere OK.

Argumentos opcionais após `/trabalhe` (todos podem faltar):

- **Números de issues** (`#123 #130`): são as tarefas da leva; pule a escolha do passo 2.
- **Filtro** (`mundo`, `unity`, `bugs`): restringe a escolha a essa funcionalidade ou tipo.
- **N** (`3`): máximo de tarefas na leva. Sem N, use 3.

## 1. Estado inicial

- `git status` e branch atual. Mudança não commitada que não é sua: não leve para a leva e não apague; avise no relatório.
- Branch atual com commit sem PR: abra o PR (regra 12) e siga a leva. Não pare para oferecer.
- A leva sai de branches novas a partir da `main` atualizada (skill `nova-branch`), cada uma no seu worktree.
- Prepare o ambiente se precisar (`npm run nuvem:preparar` na nuvem). O hook de início de sessão lista pendências de Unity: considere-as candidatas se houver Unity na máquina.

## 2. Escolher as tarefas

`npm run tarefa -- listar`. Para cada candidata livre, anote funcionalidade, camadas (docs, contrato, server, client) e dependências. Prefira, nesta ordem: o que está quebrado (CI, main), o que destrava outras tarefas, o que é independente das demais. Duas tarefas na mesma funcionalidade ou em `Core/` não rodam em paralelo. Tarefa já em andamento: não entra.

A escolha é sua. Não devolva a lista para a pessoa decidir.

## 3. Começar no mesmo turno

Decida a leva com os critérios do passo 2 e siga para o passo 4 na mesma resposta. A tabela (tarefa, funcionalidade, camadas, por que entrou, o que ficou de fora) sai junto com o início do trabalho. É informe, não pedido de confirmação.

Dúvida de regra ou contrato que impeça implementar: pergunte só essa dúvida (regra 13). Escolha de tarefa não é dúvida.

## 4. Executar

Para cada tarefa da leva, siga a skill [orquestrar-tarefa](../orquestrar-tarefa/SKILL.md) (ficha, contrato primeiro, server e client em paralelo, revisor). O título "pedir aprovação" dessa skill não para esta leva: `/trabalhe` já decidiu. Antes de qualquer agente, puxe com `npm run tarefa -- puxar <N>` (skill [puxar-tarefa](../puxar-tarefa/SKILL.md)); tarefa já em andamento de outra pessoa: pule e avise (regra 15). Agentes só nas pastas da funcionalidade (regra 1); se precisar de `Core/` ou de outra funcionalidade, pare e avise.

## 5. Entregar

Cada tarefa vira um PR próprio, pela skill [preparar-pr](../preparar-pr/SKILL.md): testes do que mudou, `dotnet csharpier format .`, verificação proporcional à matriz, `Closes #<N>`. Faça commit, push e abra o PR sem esperar pedido (regra 12). Merge é humano. Sem Unity no ambiente, registre `pendencia-unity` (regra 10).

## 6. Fechar a leva

Você integra e limpa o que criou: `npm run limpar-agentes -- --aplicar`. Gargalo de processo que apareceu: corrija em PR pequeno ou registre com [registrar-licao](../registrar-licao/SKILL.md) (regra 16). Relatório final curto: PRs abertos (links), o que ficou de fora e por quê, pendências.
