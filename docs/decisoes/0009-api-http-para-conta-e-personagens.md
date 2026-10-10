# 0009. API HTTP para conta e personagens

**Data:** 2026-10-10
**Status:** aceita

## Contexto

A primeira parte a implementar é a entrada no jogo: abertura, login, seleção e criação de personagem. Nada disso é tempo real, e a biblioteca de rede do jogo (combate e movimento) ainda não foi escolhida.

## Decisão

- Conta, login e personagens usam uma **API HTTP com JSON**, servida pelo próprio servidor (.NET 10, ASP.NET Core).
- O login devolve um **token JWT**, enviado nas chamadas seguintes no header `Authorization: Bearer <token>`.
- Senhas guardadas só como **hash** (nunca em texto), com o hasher de senha do ASP.NET Core Identity.
- O **contrato** (DTOs, rotas, códigos de erro e validações de formato) fica no shared, para client e servidor usarem os mesmos tipos.
- Os DTOs seguem as limitações do `JsonUtility` do Unity: classes `[Serializable]` com campos públicos em PascalCase, listas dentro de um objeto, enums como número e datas como segundos Unix.
- O **tempo real** do jogo (movimento, combate, sessões) vem depois, por UDP, com a biblioteca escolhida num ADR próprio.

## Consequências

- A entrada no jogo pode ser feita e testada agora, sem decidir a rede de tempo real.
- O servidor passa a ser um app ASP.NET Core; as funcionalidades ganham pontos de extensão para registrar serviços e rotas, sem arquivo central.
- Haverá dois canais (HTTP e UDP). É o padrão comum em jogos online; o token do login também vai autenticar a conexão de tempo real.
- Em produção, a API precisa de HTTPS. No desenvolvimento local, o Unity precisa permitir HTTP (configuração do projeto, feita pelo core).
