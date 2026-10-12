# 0030. Processo v2: dono da funcionalidade, foco do marco e design decidido por quem implementa

**Data:** 2026-10-11
**Status:** aceita

## Contexto

O fluxo de agentes dividia cada funcionalidade por camada: `shared-contratos`, depois `server-funcionalidade` e `client-funcionalidade` em paralelo, cada um com issue e PR próprios. A velocidade era alta (60 merges em cerca de 5 horas), mas as entregas chegavam cruas: cada camada ficava "pronta" sem que o jogador conseguisse usar a funcionalidade (lição "Entrega crua", issues #395 e #125). Ninguém tinha o contexto inteiro.

Outros sinais: 91 issues abertas, quase todas marcadas por camada; 32 issues de decisão paradas esperando o grupo; 7 tarefas em andamento ao mesmo tempo, espalhadas por mundo, social, pvp e profissões.

## Decisão

1. **Fatia vertical com dono único.** Uma issue de jogo descreve algo que o jogador faz. Um agente `funcionalidade` é dono dela por inteiro: contrato no shared, servidor, client (inclusive cena, prefab e asset pelo Unity), testes e doc, num worktree e num PR. Os donos fixos que já existiam (`itens`, `economia`) seguem o mesmo modelo. Os agentes por camada (`shared-contratos`, `server-funcionalidade`, `client-funcionalidade`, `regras-do-jogo`) ficam para Core e para o caso raro de uma fatia grande demais para um PR.
2. **Paralelismo entre funcionalidades, não entre camadas.** No máximo **3 funcionalidades de jogo em andamento** ao mesmo tempo, escolhidas pelo foco do marco ([fatia jogável](../features/fatia-jogavel.md#foco-atual)). Hoje o foco é: mais mobs no mapa, combate mais refinado e progressão balanceada.
3. **Quem implementa decide o design.** Todo mundo decide, inclusive o agente, sem esperar input humano. A decisão segue a visão do jogo, fica na `docs/features/<x>.md` (fonte única) e é listada no PR. Qualquer pessoa ou agente pode rever depois, com um PR. Confirmação humana continua só no que o AGENTS.md já exige (regra 0 e ações que não voltam).

## Consequências

- Some a rodada "contrato, merge, paralelo": o dono faz o contrato e o usa no mesmo PR. O PR fica maior; em troca, entrega a funcionalidade inteira e com um responsável pelo plano e pela checklist de experiência.
- Funcionalidades diferentes ainda andam em paralelo, cada uma no seu worktree. Unity continua um por vez na máquina.
- As issues por camada que já existem continuam válidas; issue nova de jogo nasce como fatia vertical.
- Decisão de design deixa de ser gargalo, e o risco passa a ser decisão ruim e silenciosa. Por isso ela fica escrita na doc e no PR, onde o grupo vê e pode reverter.
- Substitui o passo "contrato primeiro, em PR separado" de [docs/agentes.md](../agentes.md) para funcionalidades de jogo. A [regra 2](../../AGENTS.md) (funcionalidades só se falam por contrato) continua.
