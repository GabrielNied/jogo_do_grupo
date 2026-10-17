using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.Configuration;

namespace JogoDoGrupo.Server.Core.Canais;

/// <summary>Configurações de capacidade e contagem de canais do mundo persistente.</summary>
public sealed record ConfiguracaoCanais(
    int QuantidadeCanais = ConfigCanais.QuantidadeCanaisPadrao,
    int CapacidadePorCanal = ConfigCanais.CapacidadePorCanal
)
{
    public static ConfiguracaoCanais Ler(IConfiguration configuracao)
    {
        var quantidade =
            configuracao.GetValue<int?>("Canais:Quantidade") ?? ConfigCanais.QuantidadeCanaisPadrao;
        var capacidade =
            configuracao.GetValue<int?>("Canais:Capacidade") ?? ConfigCanais.CapacidadePorCanal;
        return new ConfiguracaoCanais(Math.Max(1, quantidade), Math.Max(1, capacidade));
    }
}
