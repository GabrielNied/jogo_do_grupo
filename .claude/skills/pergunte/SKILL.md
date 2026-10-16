---
name: pergunte
description: Levanta o que falta decidir nas docs de funcionalidade e nas issues com `decisao`, faz um quiz ao Gabriel em rodadas de perguntas com opções, registra as respostas na doc e abre ou destrava as tarefas. Use para "/pergunte", "quiz de definições" ou "o que falta decidir". Não implementa.
---

# Pergunte

Você descobre o que só o Gabriel decide, pergunta em rodadas curtas e transforma as respostas em doc e tarefas. Não implementa jogo. `/pergunte` já autoriza o PR de docs, criar, comentar, rotular e fechar issues.

Argumentos (opcionais): **filtro** de funcionalidade (`mobs`) e **N** rodadas (padrão 6, quatro perguntas cada).

## 1. Levantar

- `git fetch origin main`; doc vale `origin/main` ([Doc e regras atuais](../../../docs/agentes.md#doc-e-regras-atuais)).
- Itens abertos: `git grep -n "^\s*- \[ \]" origin/main -- docs/features`, mais "Pendente" e "proposta, não validada".
- Issues: `gh issue list --label decisao` e as `planejamento` do filtro (só o corpo antes do `<details>`).
- Quizzes anteriores: `git log --oneline --grep quiz -- docs/features`. Não repita o que já foi respondido.

## 2. Filtrar e ordenar

Pergunte só o que **muda produto** e quem implementa não decide sozinho ([Decisões de design](../../../docs/agentes.md#decisões-de-design)): regra marcada "decisão do grupo/Gabriel", número que a doc reserva a ele (#627), conflito com algo **Definido**, ou dúvida que trava uma issue.

Não pergunte. Resolva no PR:

- item já respondido em outro trecho da doc (marque `[x]` com o link);
- ficha que contradiz a doc (comente na issue: a doc vence);
- épico com todas as subtarefas fechadas (feche).

Ordem: foco atual e o que trava uma cadeia de issues primeiro; depois por funcionalidade, juntando as perguntas da mesma área numa rodada.

## 3. Quiz

Antes da primeira rodada, mostre um resumo de até 10 linhas: quantos itens abertos, o que vai perguntar por rodada e o que vai resolver sem perguntar. Então use `AskUserQuestion`:

- até 4 perguntas por chamada, 2 a 4 opções; a recomendada vem primeiro, com "(Recommended)" e o porquê na descrição;
- cada opção diz a referência (WoW, New World, RYL...) e o custo de produzir, quando pesar;
- várias propostas de uma área: uma pergunta "adotar tudo" com o resumo delas;
- liga/desliga independentes: `multiSelect`.

Ler a resposta:

- **Texto livre** (Other) que pede alternativa ou equilíbrio: na rodada seguinte, ofereça 2 ou 3 desenhos concretos que respondam a ele. Aponte o conflito com algo Definido (ex.: desconexão por inatividade).
- **Opção desmarcada** no `multiSelect` não decide nada se contrariar a proposta escrita: deixe em aberto e diga no relatório.
- Resposta que vai além da pergunta (ex.: "raça é só visual") e derruba issue ou regra: confirme o alcance na próxima rodada antes de fechar.
- Detalhe que ficou sem dono (qual tecla, qual número): decida o fácil de mudar e registre em "Decisões de design" do PR.

## 4. Registrar na doc

Branch `docs/<escopo>/registra-quiz-<assunto>` a partir da `origin/main`. Por funcionalidade:

- `Definido`: bloco "**Decisões do Gabriel em AAAA-MM-DD** (quiz de definições; números em constante nomeada)" com uma linha por decisão e link para a doc dona;
- `Em aberto`: `[x]` + "Decidido em AAAA-MM-DD: …";
- texto antigo que contradiz: `~~antigo~~ Substituído em AAAA-MM-DD: …`; regra decidida e ainda não implementada leva "falta implementar".

`git grep` o termo mudado (tecla, nome, número) em `docs/features` e acerte as outras menções. Cada informação mora num lugar só (regra 14).

## 5. Tarefas

Script `.mjs` no scratchpad, com `gh` via `execFileSync` (um `gh` por issue, sem esperar entre eles):

- **Nova**, uma por mudança de código: título `[<Funcionalidade>] <o que passa a acontecer>`, corpo como [ficha](../../../docs/agentes.md#ficha-da-tarefa) (`<!-- ficha -->`, Regra com link, Objetivo, Testes, Onde mexer, Dono, Depende de, Lote sugerido), rótulos: funcionalidade + `agente:*` + `prio:*`. Depois, `npm run tarefa -- relevancia <N> --conferida "aberta pelo /pergunte"`; com segure ativo no escopo, `npm run tarefa -- segure rotular <N>`.
- **Existente:** comentário com a decisão e o link; tire `decisao` se destravou; feche `not planned` a que a decisão derrubou ou superou (cite a nova) e `completed` o épico concluído.

## 6. Entregar

`npm run entregar -- --titulo "docs(<escopo>): registra decisoes do quiz de <assunto>" --corpo pr-corpo.md`, com as decisões por funcionalidade, as issues novas e as destravadas no corpo. Relatório curto:

- decisões, por funcionalidade;
- issues novas, com os lotes;
- destravadas e fechadas;
- o que ficou em aberto e por quê;
- sugestão de `/clear`.
