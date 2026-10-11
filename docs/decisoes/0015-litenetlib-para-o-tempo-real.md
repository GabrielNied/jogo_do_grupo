# 0015. LiteNetLib para o tempo real

**Data:** 2026-10-11
**Status:** aceita

## Contexto

O jogo é um mundo persistente (D1 opção A). Conta e personagens já usam HTTP ([ADR 0009](0009-api-http-para-conta-e-personagens.md)), e esse ADR deixou o tempo real para uma biblioteca UDP escolhida depois. A primeira presença na cidade precisa desse canal.

## Decisão

- O transporte de tempo real é a **LiteNetLib** (licença MIT), por UDP.
- O token JWT do login autentica a conexão. Sem token válido, a conexão não entra na cidade.
- Os IDs e os campos das mensagens ficam no shared, nas faixas que cada funcionalidade já tem. A biblioteca só transporta esses bytes.
- Nesta etapa, um processo de servidor hospeda a cidade. Dividir o mundo entre servidores continua em aberto.

## Consequências

- O client (Unity) e o servidor (.NET) referenciam a mesma biblioteca.
- O jogo fica com dois canais: HTTP para conta e personagens, UDP para o que acontece no mundo.
- A cidade cabe num processo por enquanto. A pergunta de como dividir o mundo por região segue em [arquitetura.md](../arquitetura.md).
