namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Posições autoritativas recentes por entidade (jogador ou inimigo), para rebobinar até
/// <see cref="JogoDoGrupo.Shared.Features.Combate.RegrasCompensacaoDeLag.JanelaMs"/>.
/// Oferecido pelo Core; Mundo e Mobs registram, Combate consulta. Thread-safe.
/// </summary>
public interface IHistoricoDePosicoesDoMundo
{
    /// <summary>Guarda uma amostra e descarta o que passou da janela de retenção.</summary>
    void Registrar(long entidadeId, PosicaoNoMundo posicao, long instanteMs);

    /// <summary>
    /// A posição mais recente em ou antes de <paramref name="instanteMs"/>, ou falso se não há
    /// amostra nesse intervalo.
    /// </summary>
    bool TentarObterEm(long entidadeId, long instanteMs, out PosicaoNoMundo posicao);

    /// <summary>Quantas amostras a entidade ainda tem (para testes).</summary>
    int QuantidadeDeAmostras(long entidadeId);
}
