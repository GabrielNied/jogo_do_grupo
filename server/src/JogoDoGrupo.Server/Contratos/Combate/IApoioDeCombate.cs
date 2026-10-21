using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Apoio de grupo que Combate (dono da vida, do dano recebido e dos efeitos) aplica a pedido de
/// quem usa uma skill de apoio, hoje Skills (ADR 0016, #170). Quem pede escolhe os alvos; aqui só
/// se aplica: quem está caído ou não existe não recebe nada. Pode ser chamada de qualquer thread.
/// </summary>
public interface IApoioDeCombate
{
    /// <summary>
    /// Dá ao personagem um escudo de <paramref name="fracaoDaVidaMaxima"/> da vida máxima dele,
    /// por <paramref name="duracaoSegundos"/>. Escudo ativo não soma: vale o mais forte e a
    /// duração renova. Falso se o personagem não pode receber (caído).
    /// </summary>
    Task<bool> AplicarEscudoAsync(
        long personagemId,
        double fracaoDaVidaMaxima,
        double duracaoSegundos,
        CancellationToken cancelamento
    );

    /// <summary>
    /// Dá o buff ao personagem; o mesmo tipo não acumula (só renova a duração). Falso se o
    /// personagem não pode receber (caído).
    /// </summary>
    bool AplicarBuff(long personagemId, TipoEfeitoDeStatus tipo, double duracaoSegundos);
}
