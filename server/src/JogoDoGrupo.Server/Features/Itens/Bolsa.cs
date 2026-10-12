using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Regras puras da bolsa de um personagem: capacidade, casas ocupadas e a primeira casa livre.
/// Nada de banco aqui; as rotas passam os itens já carregados. A capacidade vem de um só lugar
/// (<see cref="CapacidadeDe"/>), para a expansão em abas (#564) mudar só ele.
/// </summary>
internal static class Bolsa
{
    /// <summary>
    /// Capacidade da bolsa do personagem. Hoje é igual para todos
    /// (<see cref="RegrasItens.AbasBolsaInicial"/> x <see cref="RegrasItens.CasasPorAbaBolsa"/>).
    /// </summary>
    public static CapacidadeBolsa CapacidadeDe(long personagemId) =>
        RegrasItens.CapacidadeInicial();

    public static bool EstaNaBolsa(ItemInventario item) =>
        item.Posicao == PosicaoEquipamento.Inventario;

    public static int Ocupadas(IEnumerable<ItemInventario> itens) => itens.Count(EstaNaBolsa);

    public static int Livres(IEnumerable<ItemInventario> itens, CapacidadeBolsa capacidade) =>
        Math.Max(0, capacidade.Total - Ocupadas(itens));

    /// <summary>
    /// Dá casa aos itens da bolsa que ainda não têm (linhas anteriores à grade): na ordem do
    /// <c>Id</c>, cada um na primeira casa livre. Itens com casa válida não mudam.
    /// </summary>
    public static void Normalizar(IEnumerable<ItemInventario> itens, CapacidadeBolsa capacidade)
    {
        var naBolsa = itens.Where(EstaNaBolsa).OrderBy(i => i.Id).ToList();
        var ocupadas = new HashSet<(int, int)>();
        var semCasa = new List<ItemInventario>();
        foreach (var item in naBolsa)
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

    /// <summary>Primeira casa sem item (abas e casas em ordem), ou <c>null</c> se cheia.</summary>
    public static (int Aba, int Casa)? PrimeiraLivre(
        IEnumerable<ItemInventario> itens,
        CapacidadeBolsa capacidade
    ) => PrimeiraLivre(CasasOcupadas(itens), capacidade);

    public static HashSet<(int, int)> CasasOcupadas(IEnumerable<ItemInventario> itens)
    {
        var ocupadas = new HashSet<(int, int)>();
        foreach (var item in itens.Where(EstaNaBolsa))
        {
            if (item.Aba is int aba && item.Casa is int casa)
                ocupadas.Add((aba, casa));
        }
        return ocupadas;
    }

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
                    return (aba, casa);
            }
        }
        return null;
    }

    /// <summary>O item que está na casa (na bolsa), se houver.</summary>
    public static ItemInventario? Em(IEnumerable<ItemInventario> itens, int aba, int casa) =>
        itens.FirstOrDefault(i => EstaNaBolsa(i) && i.Aba == aba && i.Casa == casa);

    public static void Colocar(ItemInventario item, int aba, int casa)
    {
        item.Posicao = PosicaoEquipamento.Inventario;
        item.Conteiner = ConteinerItem.Bolsa;
        item.Aba = aba;
        item.Casa = casa;
    }

    /// <summary>Tira o item da casa (equipado, ou a caminho de outra casa).</summary>
    public static void Limpar(ItemInventario item)
    {
        item.Aba = null;
        item.Casa = null;
    }

    /// <summary>
    /// Põe os itens novos nas primeiras casas livres, na ordem recebida. Falso (sem mudar nada)
    /// se não houver casa para todos.
    /// </summary>
    public static bool Guardar(
        IEnumerable<ItemInventario> existentes,
        IReadOnlyList<ItemInventario> novos,
        CapacidadeBolsa capacidade
    )
    {
        var ocupadas = CasasOcupadas(existentes);
        var destinos = new List<(int Aba, int Casa)>();
        foreach (var _ in novos)
        {
            var livre = PrimeiraLivre(ocupadas, capacidade);
            if (livre == null)
                return false;
            ocupadas.Add(livre.Value);
            destinos.Add(livre.Value);
        }
        for (var i = 0; i < novos.Count; i++)
            Colocar(novos[i], destinos[i].Aba, destinos[i].Casa);
        return true;
    }
}
