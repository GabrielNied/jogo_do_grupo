---
name: trabalhe
description: Inicia uma leva de tarefas do Jogo do Grupo do começo ao PR (escolhe tarefas livres, propõe a leva, puxa, orquestra os agentes, abre os PRs e limpa). Use quando pedirem "/trabalhe", "começar uma leva", "pegar as próximas tarefas" ou "trabalhe nas tarefas X".
---

# Trabalhe (leva de tarefas)

Você é o **coordenador** da leva. Argumentos opcionais após `/trabalhe` (todos podem faltar):

- **Números de issues** (`#123 #130`): são as tarefas da leva; pule a escolha do passo 2.
- **Filtro** (`mundo`, `unity`, `bugs`): restringe a escolha a essa funcionalidade ou tipo.
- **N** (`3`): máximo de tarefas na leva. Sem N, use 3.
- **`direto`**: não espere o OK do passo 3 (use só se a pessoa pedir).

## 1. Estado inicial

- `git status` e branch atual. Mudança não commitada que não é sua: não leve para a leva e não apague; avise.
- Branch atual com commit sem PR? Avise e ofereça abrir o PR antes. A leva sai de branches novas a partir da `main` atualizada (skill `nova-branch`), cada uma no seu worktree.
- Prepare o ambiente se precisar (`npm run nuvem:preparar` na nuvem). O hook de início de sessão lista pendências de Unity: considere-as candidatas se houver Unity na máquina.

## 2. Escolher as tarefas

`npm run tarefa -- listar`. Para cada candidata livre, anote funcionalidade, camadas (docs, contrato, server, client) e dependências. Prefira, nesta ordem: o que está quebrado (CI, main), o que destrava outras tarefas, o que é independente das demais. Duas tarefas na mesma funcionalidade ou em `Core/` não rodam em paralelo.

## 3. Propor a leva e esperar o OK

Mostre uma tabela curta: tarefa, funcionalidade, camadas, agentes, dependência. Marque o que foi deixado de fora e por quê. **Espere o OK** (exceto com `direto`). Dúvida de regra ou contrato: pergunte agora, não no meio (regra 13).

## 4. Executar

Para cada tarefa aprovada, siga a skill [orquestrar-tarefa](../orquestrar-tarefa/SKILL.md) (ficha, contrato primeiro, server e client em paralelo, revisor). Antes de qualquer agente, puxe com `npm run tarefa -- puxar <N>` (skill [puxar-tarefa](../puxar-tarefa/SKILL.md)); tarefa já em andamento de outra pessoa: pule e avise (regra 15). Agentes só nas pastas da funcionalidade (regra 1); se precisar de `Core/` ou de outra funcionalidade, pare e avise.

## 5. Entregar

Cada tarefa vira um PR próprio, pela skill [preparar-pr](../preparar-pr/SKILL.md): testes do que mudou, `dotnet csharpier format .`, verificação proporcional à matriz, `Closes #<N>`. Faça commit, push e abra o PR sem esperar pedido (regra 12). Merge é humano. Sem Unity no ambiente, registre `pendencia-unity` (regra 10).

## 6. Fechar a leva

Você integra e limpa o que criou: `npm run limpar-agentes -- --aplicar`. Gargalo de processo que apareceu: corrija em PR pequeno ou registre com [registrar-licao](../registrar-licao/SKILL.md) (regra 16). Relatório final curto: PRs abertos (links), tarefas puladas e por quê, pendências.
