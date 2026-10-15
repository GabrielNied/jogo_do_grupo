# Padrões de fix na main

Referência para `/decida` e revisão de processo. O **`/melhore` roda a varredura da §0** da skill [melhore](../../.claude/skills/melhore/SKILL.md) em cima desta tabela antes de abrir issues. Sintomas que parecem bug de feature costumam ser contrato ou teste faltando.

## Desencontro client / servidor / shared

| Sintoma no jogo | Commits típicos | Causa | Corte |
|---|---|---|---|
| Entidades saltando, paradas, detecção estranha | `fix(core)` transporte, `fix(mobs)` | Servidor mudou UDP; Unity ficou no default; teste xUnit usa outro `NetManager` (#725, #732, [#826](https://github.com/GabrielNied/jogo_do_grupo/pull/826)) | `TransporteTempoReal` no shared, `npm run conferir-contratos-rede`, teste `TransporteLiteNetLibTestes` |
| Combate/stamina/atributos “não atualizam” | `fix(combate)` assinar evento | `EquipamentoMudou` ou cache não invalidado (#720) | Assinar evento no módulo + teste de servidor que prova efeito |
| `fix(core)` testes EditMode na main | #765, #741 | Client sem `unity:testes` na rotina ([ADR 0034](../decisoes/0034-menos-verificacao-repetida.md)) | Label `testar-unity` em PR arriscado no client |
| `fix(itens)` compilação ou painel de teste | #774, #652 | Catálogo/atributos mudaram; teste preso a número velho | Constantes nomeadas; atualizar teste no mesmo PR da regra |

## Transporte UDP (LiteNetLib)

- Constantes compartilhadas: `shared/.../TransporteTempoReal.cs`.
- Três pontos obrigatórios: `ServidorTempoReal`, `TransporteLiteNetLib`, `ClienteTempoRealDeTeste` — todos com `ChannelsCount = TransporteTempoReal.Canais`.
- Não contorne mandando `Sequenciada` no canal 0 (ex.: Pets pós-#826).
- Verificação: `npm run conferir-contratos-rede` e, na branch, `node scripts/agentes/conferir-contratos-rede.mjs origin/main`.
- Detalhe: [arquitetura.md — tempo real](../arquitetura.md#servidor-tempo-real-udp).

## Core em PRs separados

Lote de feature espera merge de `server-core`/`client-core` antes. **Contrato de fio** (UDP, serialização, IDs de mensagem) não divide só o lado servidor: um PR com shared + server + client Unity quando o contrato muda.
