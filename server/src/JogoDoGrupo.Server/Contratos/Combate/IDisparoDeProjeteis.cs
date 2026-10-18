using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Contrato do servidor para disparar projéteis autoritativos no mundo (ADR 0016).
/// Usado tanto por Combate (jogadores) quanto por Mobs (inimigos à distância).
/// </summary>
public interface IDisparoDeProjeteis
{
    long Disparar(
        long atacanteId,
        string area,
        TipoDeProjetil tipoProjetil,
        TipoDeAtaque tipoAtaque,
        float origemX,
        float origemY,
        float origemZ,
        float direcaoYGraus,
        int dano,
        float elevacaoGraus = 0f,
        int sequencia = 0,
        int passo = 0,
        long instanteVistoMs = 0
    );
}
