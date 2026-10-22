# 0045. Canais do mundo persistente

**Data:** 2026-10-16
**Status:** aceita (decidido por Gabriel)

## Contexto

Com o mundo persistente (D1 opção A), falta como aguentar mais jogadores na mesma área sem sharding geográfico nem várias máquinas. A [#188](https://github.com/GabrielNied/jogo_do_grupo/issues/188) trazia proposta técnica (canais estilo Lost Ark / Black Desert), marcada como proposta IA, sem validação do grupo. A meta de desempenho [#150](https://github.com/GabrielNied/jogo_do_grupo/issues/150) assume **80 jogadores por área/canal**.

## Decisão

1. **Canais:** um processo de servidor roda **vários canais** (cópias lógicas do **mesmo** mundo). Cada canal tem lotação de **80 jogadores** (constante nomeada; calibrável).
2. **Entrada:** ao entrar no mundo, o jogador vai para o **canal do grupo** (party/guilda conforme [social](../features/social.md)) ou, se avulso, para o **primeiro canal com vaga** (1, depois 2, depois 3...). *Revisado em 2026-10-21 (Gabriel): era "canal menos cheio", que espalhava os primeiros jogadores sem motivo.*
3. **Troca de canal:** permitida **fora de combate**; recusada em combate ([combate](../features/combate.md)).
4. **Fora do escopo** desta decisão: sharding por região em vários processos, hub instanciado separado, matchmaking cross-server.
5. **Quando implementar:** depois da [fatia jogável](../features/fatia-jogavel.md); a fatia pode rodar **um canal só** até a entrega de #188. A regra de produto já vale para ADRs e docs.

## Consequências

- Implementação em `server/.../Core/` (roteamento, presença por canal) e contratos shared; client pode exibir canal na UI quando existir.
- `docs/arquitetura.md` deixa de listar “como dividir o mundo” como aberto; detalhe operacional (número de canais por processo) fica em constante/config na implementação.
- Issue [#188](https://github.com/GabrielNied/jogo_do_grupo/issues/188) vira backlog de implementação Core, não decisão pendente.
