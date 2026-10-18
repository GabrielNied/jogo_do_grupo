using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Economia;

namespace JogoDoGrupo.Server.Features.Economia;

internal enum PontoNpcEconomia
{
    Vendedor,
    Ferreiro,
    Armeiro,
    Armadureiro,
    Taverneiro,
    Merceeiro,
    QualquerLoja,
}

internal static class ValidacaoNpcEconomia
{
    internal static bool PertoDoVendedor(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioVendedor(posicao.X, posicao.Z);

    internal static bool PertoDoFerreiro(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioFerreiro(posicao.X, posicao.Z);

    internal static bool PertoDoArmeiro(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioArmeiro(posicao.X, posicao.Z);

    internal static bool PertoDoArmadureiro(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioArmadureiro(posicao.X, posicao.Z);

    internal static bool PertoDoTaverneiro(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioTaverneiro(posicao.X, posicao.Z);

    internal static bool PertoDoMerceeiro(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioMerceeiro(posicao.X, posicao.Z);

    internal static bool PertoDeQualquerLoja(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioQualquerLoja(posicao.X, posicao.Z);

    internal static bool EstaPerto(
        IPosicoesDoMundo posicoes,
        long personagemId,
        PontoNpcEconomia ponto,
        OpcoesNpcEconomia opcoes
    )
    {
        if (!opcoes.ExigirProximidade)
        {
            return true;
        }

        if (!posicoes.TentarObter(personagemId, out var posicao))
        {
            return false;
        }

        return ponto switch
        {
            PontoNpcEconomia.Vendedor => PertoDoVendedor(posicao),
            PontoNpcEconomia.Ferreiro => PertoDoFerreiro(posicao),
            PontoNpcEconomia.Armeiro => PertoDoArmeiro(posicao),
            PontoNpcEconomia.Armadureiro => PertoDoArmadureiro(posicao),
            PontoNpcEconomia.Taverneiro => PertoDoTaverneiro(posicao),
            PontoNpcEconomia.Merceeiro => PertoDoMerceeiro(posicao),
            PontoNpcEconomia.QualquerLoja => PertoDeQualquerLoja(posicao),
            _ => false,
        };
    }

    internal static bool EstaPertoDaLoja(
        IPosicoesDoMundo posicoes,
        long personagemId,
        CategoriaLoja categoria,
        int npcId,
        OpcoesNpcEconomia opcoes
    )
    {
        if (!opcoes.ExigirProximidade)
        {
            return true;
        }

        if (!posicoes.TentarObter(personagemId, out var posicao))
        {
            return false;
        }

        if (posicao.Area != ConfigNpcEconomia.Area)
        {
            return false;
        }

        if (npcId > 0)
        {
            return JogoDoGrupo.Shared.Features.Npcs.CatalogoNpcs.DentroDoAlcance(
                npcId,
                posicao.X,
                posicao.Z
            );
        }

        if (categoria != CategoriaLoja.Nenhuma)
        {
            return ConfigNpcEconomia.DentroDoRaioLoja(categoria, posicao.X, posicao.Z);
        }

        return ConfigNpcEconomia.DentroDoRaioQualquerLoja(posicao.X, posicao.Z);
    }
}
