# 0013. Testes automatizados

**Data:** 2026-10-11
**Status:** aceita

## Contexto

Até a entrada no jogo (marco M0), não havia teste automatizado. Agentes e revisores conferiam tudo à mão: subiam o servidor, rodavam dezenas de `curl` e montavam projetos temporários para compilar o client contra o Unity, a cada PR. Isso não protege contra bugs futuros e gasta muito tempo e tokens.

## Decisão

- **xUnit** em dois projetos, na solução `JogoDoGrupo.slnx`:
  - `tests/JogoDoGrupo.Shared.Tests/`: regras e contratos do shared;
  - `tests/JogoDoGrupo.Server.Tests/`: unidade e **integração da API**, com o servidor de verdade em memória (`WebApplicationFactory`) e um banco SQLite temporário por classe de teste (`Infra/ServidorDeTeste.cs`).
- As pastas dos testes espelham o código: `Core/<Area>/` e `Features/<X>/`.
- **Todo PR de código traz testes** do que mudou: regras novas, rotas novas, casos de erro e, quando houver, concorrência. O revisor recusa PR de código sem teste.
- **CI:** `dotnet test` roda em todo PR (job `build`).
- **Client:** `npm run unity:compilar` compila cada `.asmdef` contra as DLLs do Unity, sem abrir o editor. Testes do Unity Test Framework ficam para quando houver lógica de client que valha testar fora do editor.

## Consequências

- Verificação vira um comando (`dotnet test`, `npm run unity:compilar`) em vez de testes manuais repetidos.
- Bugs de regressão aparecem no CI.
- PRs ficam um pouco maiores, por causa dos testes.
- O client ainda depende do teste humano no editor para comportamento visual.
