using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Instancias;

/// <summary>
/// O que cada salão da Mina do Reflexo tem (proposta, a calibrar): três salões de inimigos
/// comuns, em dificuldade crescente, e o boss no último.
/// </summary>
public static class ConfigDaMinaDoReflexo
{
    /// <summary>Criatura, quantidade e faixa de nível de cada salão comum, na ordem do eixo.</summary>
    public static readonly (
        int CriaturaId,
        int Quantidade,
        int NivelMinimo,
        int NivelMaximo
    )[] Saloes =
    [
        (CatalogoCriaturas.JavaliDoCampo, 3, 3, 4),
        (CatalogoCriaturas.LoboDoCampo, 3, 4, 5),
        (CatalogoCriaturas.TatuRochoso, 3, 5, 5),
    ];
}

/// <summary>Põe e tira inimigos comuns e boss quando uma corrida abre ou fecha.</summary>
public sealed class PopuladorDeInstancia(IInimigosPorInstancia inimigos, IChefesDaDungeon chefes)
{
    /// <summary>Os grupos dos salões comuns, no mundo (plano do chão).</summary>
    public static IReadOnlyList<GrupoDeInstancia> GruposDosSaloes()
    {
        var saloes = LayoutDaMinaDoReflexo.SaloesComuns();
        var grupos = new List<GrupoDeInstancia>(saloes.Length);
        for (var i = 0; i < saloes.Length; i++)
        {
            var conteudo = ConfigDaMinaDoReflexo.Saloes[i];
            grupos.Add(
                new GrupoDeInstancia(
                    conteudo.CriaturaId,
                    LayoutDaMinaDoReflexo.CentroX(saloes[i]),
                    LayoutDaMinaDoReflexo.OrigemZ,
                    conteudo.Quantidade,
                    conteudo.NivelMinimo,
                    conteudo.NivelMaximo
                )
            );
        }

        return grupos;
    }

    public void AoAbrir(string areaId)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaId);
        inimigos.Criar(areaId, GruposDosSaloes());
        chefes.Criar(areaId, LayoutDaMinaDoReflexo.BossCentroX, LayoutDaMinaDoReflexo.BossCentroZ);
    }

    public void AoFechar(string areaId)
    {
        if (string.IsNullOrEmpty(areaId))
        {
            return;
        }

        inimigos.Remover(areaId);
        chefes.Remover(areaId);
    }
}
