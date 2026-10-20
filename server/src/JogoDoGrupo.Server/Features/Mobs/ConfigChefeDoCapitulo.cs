using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// O chefe fixo do capítulo 1 perto da Brasa Mansa (issue #1059, docs/features/missoes.md).
/// Fica na Colina do Vigia e volta com respawn curto para que novos jogadores concluam
/// a etapa 4 da história principal sem fila excessiva.
/// </summary>
public static class ConfigChefeDoCapitulo
{
    public const int CriaturaId = CatalogoCriaturas.PresaVelha;

    public const string Area = AreasDoMundo.Cidade;

    public const double PontoX = 0.0;

    public const double PontoZ = 160.0;

    public const double IntervaloRespawnSegundos = 60.0;

    public const long PrimeiroId = 1_000_000_300_000;
}
