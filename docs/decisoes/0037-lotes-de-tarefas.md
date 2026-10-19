# 0037. Lotes de tarefas, ficha no corpo da issue e orçamento de contexto

**Data:** 2026-10-14
**Status:** aceita (decidido por Gabriel). Emenda o [ADR 0030](0030-processo-v2-dono-da-funcionalidade.md): o dono passa a ser de um lote, e o limite passa a ser de lotes, não de funcionalidades.

## Contexto

- O `/trabalhe` lançava um agente e um PR por issue e proibia juntar duas da mesma funcionalidade. Cada agente custa 50–80 mil tokens fixos e cada PR uma rodada de CI, mesmo quando várias issues mexem no mesmo código.
- Dentro do mesmo `prio:*`, a fila saía pela issue mais nova. Cerca de 60 das 85 abertas eram de 2026-10-11, com rótulos dos agentes por camada e sem ficha, e o `/decida` só via as que tinham doc ligada.
- Todo agente carregava cerca de 60 mil caracteres de instruções (AGENTS.md, docs/agentes.md, a própria definição, puxar-tarefa e preparar-pr) antes de ler código, reenviados em todas as rodadas.

## Decisão

1. **Lote:** o `/trabalhe` junta numa branch, num dono e num PR as tarefas livres que mexem no mesmo código (mesmos arquivos ou fluxo, dependência ou "Lote sugerido"), de uma ou várias funcionalidades, sem limite de quantidade. Um commit e um `Closes #` por issue. Core continua em lote próprio, com o agente de Core. PR acima de ~1500 linhas se divide em PRs empilhados pelo mesmo dono.
2. **Limite:** no máximo 3 lotes de jogo em andamento, no lugar de 3 funcionalidades. O `puxar` conta branches distintas nas travas.
3. **Ordem:** `prio:*`, depois a issue mais antiga. Quem espera dependência aberta vai para o fim da faixa. O `/decida` ajusta `prio:*`. ~~Rótulo `foco` entre os dois~~ removido em 2026-10-19.
4. **Ficha no corpo da issue:** o `/decida`, o `/melhore` e o coordenador reescrevem o corpo no formato de [Ficha da tarefa](../agentes.md#ficha-da-tarefa), com o texto original num `<details>`. O `/decida` confere as issues da mais antiga para a mais nova (`npm run tarefa -- revisar`).
5. **Agentes por camada saem:** `shared-contratos`, `server-funcionalidade`, `client-funcionalidade` e `regras-do-jogo` já eram exceção pelo ADR 0030; fatia grande se divide por caso de uso com o próprio dono. O `orquestrar-tarefa` entra no `/trabalhe`.
6. **Orçamento de contexto:** AGENTS.md, docs/agentes.md, cada agente e cada skill têm teto em caracteres, conferido por `npm run contexto` no CI. Os valores ficam em [orcamento-contexto.mjs](../../scripts/agentes/orcamento-contexto.mjs), não aqui. O detalhe mora no doc que só se lê quando preciso. Lição que vira script, hook ou regra sai do `licoes.md`.

## Consequências

- Menos agentes e rodadas de CI por leva; o PR de um lote é maior e o "Como testar" vem por issue.
- O pacote de instruções do dono caiu de ~60 mil para ~25 mil caracteres. Para crescer, outro trecho precisa sair.
- Issues antigas ganham ficha aos poucos, 15 por `/decida`; até lá o `listar` as marca com `sem ficha` e `rótulos v1`.

## Emenda 2026-10-16 (Gabriel)

O item 2 (**Limite de 3 lotes**) passa a ser **recomendação**, não trava:

- **Recomendado:** até **3 lotes de jogo** em andamento para manter o custo de CI/IA.
- **`puxar`:** **não recusa** o quarto lote; emite **aviso** listando os lotes em curso. Core e desenvolvimento continuam fora da contagem.
- Quem coordena pode manter a disciplina manualmente; o script só lembra.
