# 0042. Robôs de carga do tempo real em tools/

**Data:** 2026-10-15
**Status:** aceita

## Contexto

Até a issue #150 o tempo real só era exercitado pelo client Unity: não dava para saber quantos jogadores o servidor aguenta numa área nem pegar regressão de protocolo sem abrir o editor. Opções: ferramenta pronta de carga (k6, Gatling, que não falam LiteNetLib), script fora de .NET (reescreveria os DTOs) ou um console .NET com o `shared`.

## Decisão

1. **Console .NET em `tools/JogoDoGrupo.Robos/`**, no `JogoDoGrupo.slnx`. Usa só o `shared` e a LiteNetLib, nunca o projeto do servidor: fala o protocolo como um client de fora.
2. **Mesma carga no console e no teste:** `CargaDeRobos` serve ao console (carga grande, à mão) e a um teste curto no `dotnet test` (5 robôs por 5 s contra o `ServidorDeTeste`).
3. **Tick do servidor pelo Core:** o laço registra a duração de cada passo (`ServidorTempoReal.Passos`) e `/admin/metricas` passa a devolver o resumo. A rota continua só para `permissao=administrar` ([ADR 0022](0022-serilog-para-observabilidade.md)); como nenhuma conta tem essa claim, o console assina um token curto com a chave JWT que a pessoa passar (`--chave-jwt`). Isso não abre nada novo: quem tem a chave já assina qualquer token.
4. **Meta de carga** em [arquitetura.md](../arquitetura.md#carga-do-tempo-real).

## Consequências

- Protocolo quebrado (entrada, movimento, ataque, réplica) falha no CI sem Unity.
- O codec do pacote (2 bytes + JSON) fica repetido no robô, de propósito: é um client de fora.
- Robôs e servidor na mesma máquina disputam a CPU; números de capacidade só valem como ordem de grandeza até rodar em máquinas separadas.
- `tools/` passa a existir para ferramentas .NET fora do jogo; cada uma com README próprio.
