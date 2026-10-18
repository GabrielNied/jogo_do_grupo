using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Regras puras do banco pessoal da conta: capacidade, casas ocupadas e primeira casa livre.
/// </summary>
internal static class Banco
{
    public static CapacidadeBolsa CapacidadeDe(EstadoBanco? estado) =>
        RegrasBanco.CapacidadeBancoDe(estado?.Abas ?? RegrasBanco.AbasBancoInicial);

    public static int Ocupadas(IEnumerable<ItemBanco> itens) => itens.Count();

    public static int Livres(IEnumerable<ItemBanco> itens, CapacidadeBolsa capacidade) =>
        Math.Max(0, capacidade.Total - Ocupadas(itens));

    public static HashSet<(int, int)> CasasOcupadas(IEnumerable<ItemBanco> itens)
    {
        var ocupadas = new HashSet<(int, int)>();
        foreach (var item in itens)
        {
            if (item.Aba is int aba && item.Casa is int casa)
            {
                ocupadas.Add((aba, casa));
            }
        }

        return ocupadas;
    }

    public static (int Aba, int Casa)? PrimeiraLivre(
        IEnumerable<ItemBanco> itens,
        CapacidadeBolsa capacidade
    ) => PrimeiraLivre(CasasOcupadas(itens), capacidade);

    public static (int Aba, int Casa)? PrimeiraLivre(
        HashSet<(int, int)> ocupadas,
        CapacidadeBolsa capacidade
    )
    {
        for (var aba = 0; aba < capacidade.Abas; aba++)
        {
            for (var casa = 0; casa < capacidade.CasasPorAba; casa++)
            {
                if (!ocupadas.Contains((aba, casa)))
                {
                    return (aba, casa);
                }
            }
        }

        return null;
    }

    public static ItemBanco? Em(IEnumerable<ItemBanco> itens, int aba, int casa) =>
        itens.FirstOrDefault(i => i.Aba == aba && i.Casa == casa);

    public static void Colocar(ItemBanco item, int aba, int casa)
    {
        item.Aba = aba;
        item.Casa = casa;
    }

    public static void Limpar(ItemBanco item)
    {
        item.Aba = null;
        item.Casa = null;
    }

    public static void Normalizar(IEnumerable<ItemBanco> itens, CapacidadeBolsa capacidade)
    {
        var naLista = itens.OrderBy(i => i.Id).ToList();
        var ocupadas = new HashSet<(int, int)>();
        var semCasa = new List<ItemBanco>();

        foreach (var item in naLista)
        {
            if (
                item.Aba is int aba
                && item.Casa is int casa
                && capacidade.Contem(aba, casa)
                && ocupadas.Add((aba, casa))
            )
            {
                continue;
            }

            semCasa.Add(item);
        }

        foreach (var item in semCasa)
        {
            var livre = PrimeiraLivre(ocupadas, capacidade);
            if (livre == null)
            {
                item.Aba = null;
                item.Casa = null;
                continue;
            }

            Colocar(item, livre.Value.Aba, livre.Value.Casa);
            ocupadas.Add(livre.Value);
        }
    }
}
