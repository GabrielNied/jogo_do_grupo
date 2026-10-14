---
name: trabalhe
description: Inicia uma leva de tarefas do Jogo do Grupo do começo ao PR (escolhe as tarefas livres, decide o que a doc deixa em aberto, puxa, orquestra os agentes, abre os PRs e limpa). Use quando pedirem "/trabalhe", "começar uma leva", "pegar as próximas tarefas" ou "trabalhe nas tarefas X". Não pede input humano. Organizar o quadro é o /decida.
---

# Trabalhe (leva de tarefas)

Você é o **coordenador** da leva. `/trabalhe` já autoriza escolher, puxar, decidir o que a doc deixa em aberto e implementar. Não pergunte o que puxar, não peça regra, contrato nem confirmação, não ofereça alternativas e não espere OK. Organizar o quadro e destravar sem implementar é o `/decida`. Abrir tarefa de melhoria do que já existe é o `/melhore`.

Argumentos opcionais após `/trabalhe` (todos podem faltar):

- **Números de issues** (`#123 #130`): são as tarefas da leva; pule a escolha do passo 2.
- **Filtro** (`mundo`, `unity`, `bugs`): restringe a escolha a essa funcionalidade ou tipo.
- **N** (`3`): máximo de tarefas na leva. Sem N, use 3. É o controle de custo, igual em qualquer ferramenta: com o plano no fim, a pessoa pede `/trabalhe 1`.

## 1. Estado inicial

- `git status` e branch atual. Mudança não commitada que não é sua: não leve para a leva e não apague; avise no relatório.
- Branch atual com commit sem PR: abra o PR (regra 12) e siga a leva. Não pare para oferecer.
- A leva sai de branches novas a partir da `main` atualizada (skill `nova-branch`), cada uma no seu worktree.
- Prepare o ambiente se precisar (`npm run nuvem:preparar` na nuvem). O hook de início de sessão lista pendências de Unity: considere-as candidatas se houver Unity na máquina.

## 2. Escolher as tarefas

`npm run tarefa -- listar`. Para cada candidata livre, anote funcionalidade, camadas (docs, contrato, server, client) e dependências. Prefira, nesta ordem: o que está quebrado (CI, main), depois o rótulo `prio:*` (menor número primeiro, [Prioridade das tarefas](../../../docs/agentes.md#prioridade-das-tarefas)), depois o que destrava outras tarefas e o que é independente das demais. O tamanho da leva é N. Duas tarefas na mesma funcionalidade ou em `Core/` não rodam em paralelo. Não entra: tarefa já em andamento, label `bloqueada` ou `decisao`. Essas ficam para o `/decida`.

A escolha é sua. Não devolva a lista para a pessoa decidir.

## 3. Começar no mesmo turno

Decida a leva com os critérios do passo 2 e siga para o passo 4 na mesma resposta. A tabela (tarefa, funcionalidade, camadas, por que entrou, o que ficou de fora) sai junto com o início do trabalho. É informe. A resposta não termina na tabela.

O que a doc e a proposta da issue já dizem, siga. O que estiver em aberto, decida e registre ([Decisões de design](../../../docs/agentes.md#decisões-de-design)). Se a decisão mudaria algo marcado como Definido, pule a tarefa, comente na issue e escolha outra. Isso vai para um `/decida`, e a leva continua.

## 4. Executar

Para cada tarefa da leva, siga a skill [orquestrar-tarefa](../orquestrar-tarefa/SKILL.md) (ficha, contrato primeiro, server e client em paralelo, revisor). Essa skill não pede aprovação nesta leva: `/trabalhe` já decidiu. Antes de qualquer agente, puxe com `npm run tarefa -- puxar <N>` (skill [puxar-tarefa](../puxar-tarefa/SKILL.md)); tarefa já em andamento por outra pessoa: pule e registre no relatório (regra 15). Se o `puxar` avisar **REVISAR RELEVÂNCIA**, você confere antes de passar para o agente ([Conferir a relevância](../puxar-tarefa/SKILL.md#21-conferir-a-relevância)) e manda na ficha a issue já ajustada. Se ela estiver feita ou obsoleta, troque por outra tarefa. A tarefa de Core fica com o agente de Core. Se uma funcionalidade exigir a pasta de outra, ou Core que a issue não pediu, comente na issue, deixe essa parte de fora e termine o que cabe na pasta dela.

## 5. Entregar

Cada tarefa vira um PR próprio, pela skill [preparar-pr](../preparar-pr/SKILL.md): testes do que mudou, `dotnet csharpier format .`, verificação proporcional à matriz, `Closes #<N>`. Faça commit, push e abra o PR sem esperar pedido (regra 12). Merge é humano. Sem Unity no ambiente, registre `pendencia-unity` (regra 10).

## 6. Fechar a leva

Você integra e limpa o que criou: `npm run limpar-agentes -- --aplicar`. Antes do relatório, a reflexão da regra 16 (skill `preparar-pr`): corte que cabe, no PR; sem corte, silêncio. Relatório final curto: PRs abertos (links), o que ficou de fora e por quê, o que um `/decida` ainda precisa destravar. Merge continua humano: não peça para mergear.
