using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Economia;

namespace JogoDoGrupo.Server.Features.Economia;

internal enum PontoNpcEconomia
{
    Vendedor,
    Ferreiro,
}

internal static class ValidacaoNpcEconomia
{
    internal static bool PertoDoVendedor(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioVendedor(posicao.X, posicao.Z);

    internal static bool PertoDoFerreiro(PosicaoNoMundo posicao) =>
        posicao.Area == ConfigNpcEconomia.Area
        && ConfigNpcEconomia.DentroDoRaioFerreiro(posicao.X, posicao.Z);

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
            _ => false,
        };
    }
}
