using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>Um item no chão, reservado ao dono. A posição é no plano do chão.</summary>
public sealed record LootReservado(
    long LootId,
    long DonoId,
    int ItemId,
    string Area,
    double X,
    double Z,
    DateTimeOffset ApareceuEm,
    RaridadeItem Raridade = RaridadeItem.Comum,
    GrauEquipamento Grau = GrauEquipamento.Inicial
)
{
    /// <summary>Segundos desde que o item apareceu.</summary>
    public double Idade(DateTimeOffset agora) => (agora - ApareceuEm).TotalSeconds;

    public LootNoChao ParaMensagem(DateTimeOffset agora) =>
        new()
        {
            LootId = LootId,
            ItemId = ItemId,
            Nome =
                CatalogoItens.ObterNome(ItemId) ?? CatalogoEquipamentos.Obter(ItemId)?.Nome ?? "",
            Raridade = Raridade,
            Grau = Grau,
            X = (float)X,
            Z = (float)Z,
            ExpiraEmSegundos = RegrasLoot.SegundosRestantes(Idade(agora)),
        };
}

/// <summary>
/// Os itens que estão no chão, em memória (docs/features/fatia-jogavel.md, seção 5). Cada item
/// tem um dono e só ele o alcança. Não fala com rede nem com banco: quem faz isso é o
/// <see cref="ServicoDeLoot"/>. Se o servidor reinicia, o que estava no chão some.
/// <para>
/// A coleta tem duas etapas para o mesmo item nunca entrar duas vezes no inventário:
/// <see cref="TentarSeparar"/> tira o item do alcance de outros pedidos enquanto o banco grava;
/// depois <see cref="Confirmar"/> o remove ou <see cref="Devolver"/> o põe de volta no chão.
/// Pode ser chamada de qualquer thread.
/// </para>
/// </summary>
public sealed class ChaoDeLoot
{
    private sealed class Entrada(LootReservado loot)
    {
        public LootReservado Loot { get; } = loot;
        public bool Separado { get; set; }
    }

    private readonly Lock _trava = new();
    private readonly Dictionary<long, Entrada> _itens = [];
    private long _ultimoId = RegrasLoot.TudoPorPerto;

    /// <summary>Quantos itens estão no chão, contando os que estão sendo coletados.</summary>
    public int Quantidade
    {
        get
        {
            lock (_trava)
            {
                return _itens.Count;
            }
        }
    }

    /// <summary>Põe no chão um item por id de <paramref name="itens"/> (Comum, Inicial), do mesmo dono.</summary>
    public IReadOnlyList<LootReservado> Largar(
        long donoId,
        IReadOnlyList<int> itens,
        string area,
        double x,
        double z,
        DateTimeOffset agora
    ) =>
        Largar(
            donoId,
            itens.Select(id => new QuedaDeLoot { ModeloId = id }).ToArray(),
            area,
            x,
            z,
            agora
        );

    /// <summary>Põe no chão uma peça por queda de <paramref name="quedas"/>, todas do mesmo dono.</summary>
    public IReadOnlyList<LootReservado> Largar(
        long donoId,
        IReadOnlyList<QuedaDeLoot> quedas,
        string area,
        double x,
        double z,
        DateTimeOffset agora
    )
    {
        var novos = new List<LootReservado>(quedas.Count);
        lock (_trava)
        {
            foreach (var queda in quedas)
            {
                var loot = new LootReservado(
                    ++_ultimoId,
                    donoId,
                    queda.ModeloId,
                    area,
                    x,
                    z,
                    agora,
                    queda.Raridade,
                    queda.Grau
                );
                _itens[loot.LootId] = new Entrada(loot);
                novos.Add(loot);
            }
        }

        return novos;
    }

    /// <summary>
    /// Os itens do dono que estão no chão, dentro do prazo e livres (sem coleta em andamento),
    /// do mais antigo para o mais novo.
    /// </summary>
    public IReadOnlyList<LootReservado> DoDono(long donoId, DateTimeOffset agora)
    {
        lock (_trava)
        {
            return _itens
                .Values.Where(e => Disponivel(e, donoId, agora))
                .Select(e => e.Loot)
                .OrderBy(l => l.LootId)
                .ToArray();
        }
    }

    /// <summary>
    /// O item, se ele existe, é desse dono, está no prazo e livre. Item de outro dono responde
    /// como se não existisse.
    /// </summary>
    public bool TentarObter(long donoId, long lootId, DateTimeOffset agora, out LootReservado loot)
    {
        lock (_trava)
        {
            if (_itens.TryGetValue(lootId, out var entrada) && Disponivel(entrada, donoId, agora))
            {
                loot = entrada.Loot;
                return true;
            }
        }

        loot = null!;
        return false;
    }

    /// <summary>
    /// Separa o item para a coleta. Só um pedido consegue: o seguinte recebe falso até o item
    /// ser devolvido (e para sempre, se a coleta for confirmada).
    /// </summary>
    public bool TentarSeparar(long donoId, long lootId, DateTimeOffset agora)
    {
        lock (_trava)
        {
            if (!_itens.TryGetValue(lootId, out var entrada) || !Disponivel(entrada, donoId, agora))
            {
                return false;
            }

            entrada.Separado = true;
            return true;
        }
    }

    /// <summary>O item separado entrou no inventário: sai do chão de vez.</summary>
    public void Confirmar(long lootId)
    {
        lock (_trava)
        {
            if (_itens.TryGetValue(lootId, out var entrada) && entrada.Separado)
            {
                _itens.Remove(lootId);
            }
        }
    }

    /// <summary>A coleta não deu certo: o item separado volta a ficar livre no chão.</summary>
    public void Devolver(long lootId)
    {
        lock (_trava)
        {
            if (_itens.TryGetValue(lootId, out var entrada))
            {
                entrada.Separado = false;
            }
        }
    }

    /// <summary>
    /// Tira do chão os itens que passaram do prazo e os devolve, para avisar os donos. Item com
    /// coleta em andamento fica: ou entra no inventário, ou volta e expira na próxima chamada.
    /// </summary>
    public IReadOnlyList<LootReservado> RemoverExpirados(DateTimeOffset agora)
    {
        lock (_trava)
        {
            var expirados = _itens
                .Values.Where(e => !e.Separado && RegrasLoot.Expirou(e.Loot.Idade(agora)))
                .Select(e => e.Loot)
                .OrderBy(l => l.LootId)
                .ToArray();
            foreach (var loot in expirados)
            {
                _itens.Remove(loot.LootId);
            }

            return expirados;
        }
    }

    private static bool Disponivel(Entrada entrada, long donoId, DateTimeOffset agora) =>
        !entrada.Separado
        && entrada.Loot.DonoId == donoId
        && !RegrasLoot.Expirou(entrada.Loot.Idade(agora));
}
