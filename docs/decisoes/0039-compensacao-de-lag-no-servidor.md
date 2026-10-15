# 0039. Compensação de lag no servidor (rewind de posição)

**Data:** 2026-10-15
**Status:** aceita

## Contexto

Com o client dono do próprio corpo (ADR 0024), o servidor decide acerto e dano com a posição que tinha quando o golpe resolve — quem desvia no último instante na tela ainda pode levar o golpe. Issue #172 pede rewind curto (~250 ms) para aproximar o FFXIV: o client manda o relógio do servidor que acreditava ter ao pedir ataque ou esquiva; o servidor rebobina só o teste de volume e a janela de invulnerabilidade da esquiva, sem mover ninguém no mundo.

## Decisão

1. **Janela:** 250 ms de histórico (`RegrasCompensacaoDeLag.JanelaMs`). Acima disso, ou sem `TempoVistoMs`, vale o instante atual.
2. **Histórico:** `IHistoricoDePosicoesDoMundo` no Core guarda amostras por entidade (jogador e inimigo), podadas pela janela. Mundo registra movimento aceito; Mobs registra quando a posição muda.
3. **Ataque:** no acerto, `AlvosNoVolume` recebe o instante de teste derivado de `IntencaoDeAtaque.TempoVistoMs` e usa posição histórica dos inimigos quando existir.
4. **Esquiva:** `IntencaoDeEsquiva.TempoVistoMs` antecipa o início da invulnerabilidade (`ControleDeAcoes.TentarOcupar`), dentro da mesma janela.
5. **Client:** campo novo no shared; preencher o estimador de relógio do servidor fica fora desta entrega (#480).

## Consequências

- Acerto e esquiva ficam mais justos com ping moderado, sem reconciliar posição do client.
- Memória bounded por entidade ativa (~ poucas amostras por 250 ms).
- Golpes de skill e PvP entre jogadores ainda medem alvos sem histórico de jogador no volume até extensão futura.
