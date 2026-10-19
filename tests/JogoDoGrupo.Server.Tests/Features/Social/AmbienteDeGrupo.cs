using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>Diretório de personagens em memória (nomes sem diferenciar maiúsculas).</summary>
public sealed class DiretorioFalso : IDiretorioDePersonagens
{
    private readonly Dictionary<long, string> _nomes = [];
    private readonly Dictionary<long, long> _contas = [];

    public void Adicionar(long id, string nome, long? contaId = null)
    {
        lock (_nomes)
        {
            _nomes[id] = nome;
            _contas[id] = contaId ?? id;
        }
    }

    public bool TentarObterPorNome(string nome, out long personagemId)
    {
        lock (_nomes)
        {
            foreach (var (id, atual) in _nomes)
            {
                if (string.Equals(atual, nome, StringComparison.OrdinalIgnoreCase))
                {
                    personagemId = id;
                    return true;
                }
            }
        }

        personagemId = 0;
        return false;
    }

    public bool TentarObterNome(long personagemId, out string nome)
    {
        lock (_nomes)
        {
            var achou = _nomes.TryGetValue(personagemId, out var atual);
            nome = atual ?? "";
            return achou;
        }
    }

    public bool TentarObterRaca(long personagemId, out int raca)
    {
        raca = 1;
        return true;
    }

    public bool TentarObterContaDoPersonagem(long personagemId, out long contaId)
    {
        lock (_nomes)
        {
            return _contas.TryGetValue(personagemId, out contaId);
        }
    }
}

/// <summary>Guarda tudo que o grupo mandou, na ordem, para os testes conferirem.</summary>
public sealed class SaidaGravada : ISaidaDoGrupo
{
    private readonly List<(long Para, object Mensagem)> _enviadas = [];

    public IReadOnlyList<(long Para, object Mensagem)> Todas
    {
        get
        {
            lock (_enviadas)
            {
                return [.. _enviadas];
            }
        }
    }

    public void Lista(long personagemId, ListaDoGrupo lista) => Gravar(personagemId, lista);

    public void Aviso(long personagemId, AvisoDoGrupo aviso) => Gravar(personagemId, aviso);

    public void Convite(long personagemId, ConviteRecebido convite) =>
        Gravar(personagemId, convite);

    public void Recusada(long personagemId, string codigo) => Gravar(personagemId, codigo);

    public void Limpar()
    {
        lock (_enviadas)
        {
            _enviadas.Clear();
        }
    }

    public List<T> De<T>(long para) =>
        Todas.Where(e => e.Para == para && e.Mensagem is T).Select(e => (T)e.Mensagem).ToList();

    public List<AvisoDoGrupo> Avisos(long para, TipoDeAvisoDoGrupo tipo) =>
        De<AvisoDoGrupo>(para).Where(a => a.Tipo == tipo).ToList();

    public ListaDoGrupo UltimaLista(long para) =>
        De<ListaDoGrupo>(para).LastOrDefault()
        ?? throw new InvalidOperationException($"{para} não recebeu lista.");

    public string? UltimaRecusa(long para) => De<string>(para).LastOrDefault();

    private void Gravar(long para, object mensagem)
    {
        lock (_enviadas)
        {
            _enviadas.Add((para, mensagem));
        }
    }
}

/// <summary>
/// Doze personagens online (A a L) e um relógio que só anda quando o teste manda. Sempre espera
/// o intervalo mínimo entre convites do mesmo convidante, a não ser que o teste peça o contrário.
/// </summary>
public sealed class AmbienteDeGrupo
{
    public const long A = 1,
        B = 2,
        C = 3,
        D = 4,
        E = 5,
        F = 6,
        G = 7,
        H = 8,
        I = 9,
        J = 10,
        K = 11,
        L = 12;

    public static readonly long[] Todos = [A, B, C, D, E, F, G, H, I, J, K, L];

    private static readonly string[] Nomes =
    [
        "Ana",
        "Bia",
        "Caio",
        "Duda",
        "Edu",
        "Fabi",
        "Gabi",
        "Hugo",
        "Igor",
        "Joca",
        "Kaka",
        "Lia",
    ];

    private long _agoraMs = 1_000_000;

    public AmbienteDeGrupo()
    {
        for (var i = 0; i < Nomes.Length; i++)
        {
            Diretorio.Adicionar(Todos[i], Nomes[i]);
        }

        Grupos = new GruposDoServidor(Diretorio, Saida, () => Interlocked.Read(ref _agoraMs));
        foreach (var id in Todos)
        {
            Grupos.AoConectar(id);
        }
    }

    public DiretorioFalso Diretorio { get; } = new();
    public SaidaGravada Saida { get; } = new();
    public GruposDoServidor Grupos { get; }
    public long AgoraMs => Interlocked.Read(ref _agoraMs);

    public static string Nome(long id) => Nomes[id - 1];

    public void Avancar(double segundos) =>
        Interlocked.Add(ref _agoraMs, (long)Math.Round(segundos * 1000));

    /// <summary>Convida (esperando 1 s antes) e devolve o id do convite recebido pelo alvo.</summary>
    public long Convidar(long de, long para)
    {
        Avancar(RegrasGrupo.IntervaloEntreConvitesSegundos);
        Saida.Limpar();
        Grupos.Convidar(de, Nome(para));
        var convites = Saida.De<ConviteRecebido>(para);
        Assert.True(
            convites.Count > 0,
            $"{Nome(para)} não recebeu convite: {Saida.UltimaRecusa(de)}"
        );
        return convites[^1].ConviteId;
    }

    /// <summary>Convida e o alvo aceita.</summary>
    public void Juntar(long lider, long novo)
    {
        var id = Convidar(lider, novo);
        Grupos.Responder(novo, id, true);
        Assert.Contains(novo, Grupos.ListaDe(lider).Membros.Select(m => m.PersonagemId));
    }

    /// <summary>Forma um grupo: o primeiro é o líder e os outros entram na ordem.</summary>
    public void FormarGrupo(params long[] ids)
    {
        foreach (var id in ids.Skip(1))
        {
            Juntar(ids[0], id);
        }

        Saida.Limpar();
    }

    /// <summary>Um texto com o grupo de cada personagem e os convites pendentes.</summary>
    public string Foto() =>
        string.Join(
            "|",
            Todos.Select(id =>
            {
                var l = Grupos.ListaDe(id);
                return $"{id}:{l.GrupoId}:{l.LiderId}:"
                    + string.Join(
                        ",",
                        l.Membros.Select(m => $"{m.PersonagemId}{(m.Online ? "+" : "-")}")
                    );
            })
        ) + $"|g{Grupos.QuantidadeDeGrupos}|c{Grupos.ConvitesPendentes}";

    /// <summary>A operação foi recusada com o código e nada mais foi enviado a ninguém.</summary>
    public void AssertSoRecusa(long quem, string codigo)
    {
        var enviadas = Saida.Todas;
        var unica = Assert.Single(enviadas);
        Assert.Equal(quem, unica.Para);
        Assert.Equal(codigo, unica.Mensagem);
    }

    public List<long> Ids(long de) =>
        Grupos.ListaDe(de).Membros.Select(m => m.PersonagemId).ToList();
}
