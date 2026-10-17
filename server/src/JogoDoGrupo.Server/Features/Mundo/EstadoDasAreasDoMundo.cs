using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Estado isolado de presença e movimento de cada área do Mundo.</summary>
internal sealed class EstadoDasAreasDoMundo<TSessao>(Func<long> relogioMs)
    where TSessao : class
{
    private readonly object _trava = new();
    private readonly Dictionary<string, CidadeDoMundo<TSessao>> _areas = new(StringComparer.Ordinal)
    {
        [AreasDoMundo.Cidade] = new(relogioMs),
    };
    private readonly Dictionary<long, string> _areaPorPersonagem = [];

    public CidadeDoMundo<TSessao> Cidade => _areas[AreasDoMundo.Cidade];

    public CidadeDoMundo<TSessao>? ParaPersonagem(long personagemId)
    {
        lock (_trava)
        {
            return _areaPorPersonagem.TryGetValue(personagemId, out var area) ? _areas[area] : null;
        }
    }

    public CidadeDoMundo<TSessao> ObterOuCriar(string area)
    {
        lock (_trava)
        {
            if (!_areas.TryGetValue(area, out var estado))
            {
                estado = new CidadeDoMundo<TSessao>(relogioMs);
                _areas.Add(area, estado);
            }

            return estado;
        }
    }

    public (bool Novo, AvatarNaCidade<TSessao> Eu, AvatarNaCidade<TSessao>[] Outros) Entrar(
        long personagemId,
        TSessao sessao,
        string area,
        PosicaoNaCidade nascimento
    )
    {
        lock (_trava)
        {
            var estado = ObterOuCriar(area);
            var resultado = estado.Entrar(personagemId, sessao, nascimento);
            _areaPorPersonagem[personagemId] = area;
            return resultado;
        }
    }

    public void RegistrarEntrada(long personagemId, string area)
    {
        lock (_trava)
        {
            _areaPorPersonagem[personagemId] = area;
        }
    }

    public void RegistrarSaida(long personagemId, string area)
    {
        lock (_trava)
        {
            if (_areaPorPersonagem.TryGetValue(personagemId, out var atual) && atual == area)
            {
                _areaPorPersonagem.Remove(personagemId);
            }

            RemoverAreaVazia(area);
        }
    }

    public (string Area, CidadeDoMundo<TSessao> Estado)? Localizar(long personagemId)
    {
        lock (_trava)
        {
            return _areaPorPersonagem.TryGetValue(personagemId, out var area)
                ? (area, _areas[area])
                : null;
        }
    }

    public TransicaoDeArea<TSessao>? Mover(
        long personagemId,
        TSessao sessao,
        string destino,
        PosicaoNaCidade posicao
    )
    {
        lock (_trava)
        {
            var origem = Localizar(personagemId);
            if (origem == null || origem.Value.Area == destino)
            {
                return null;
            }

            var avatar = origem.Value.Estado.Obter(personagemId);
            if (avatar == null || !ReferenceEquals(avatar.Sessao, sessao))
            {
                return null;
            }

            if (!origem.Value.Estado.Sair(personagemId, sessao))
            {
                return null;
            }

            var origemArea = origem.Value.Area;
            var outrosDaOrigem = origem.Value.Estado.Presentes();
            var estadoDestino = ObterOuCriar(destino);
            var (novo, avatarDestino, outrosDestino) = estadoDestino.Entrar(
                personagemId,
                sessao,
                posicao
            );
            RegistrarSaida(personagemId, origem.Value.Area);
            RegistrarEntrada(personagemId, destino);
            return new(origemArea, outrosDaOrigem, novo, avatarDestino, outrosDestino);
        }
    }

    public IReadOnlyList<long> PersonagensNaArea(string area)
    {
        lock (_trava)
        {
            return _areas.TryGetValue(area, out var estado)
                ? estado.Presentes().Select(a => a.PersonagemId).ToArray()
                : [];
        }
    }

    /// <summary>Todos os avatares presentes em qualquer área (mapa do grupo, #903).</summary>
    public AvatarNaCidade<TSessao>[] PresentesEmTodasAsAreas()
    {
        lock (_trava)
        {
            var lista = new List<AvatarNaCidade<TSessao>>();
            foreach (var estado in _areas.Values)
            {
                lista.AddRange(estado.Presentes());
            }

            return lista.ToArray();
        }
    }

    public (string Area, AvatarNaCidade<TSessao> Avatar)? Obter(long personagemId)
    {
        lock (_trava)
        {
            if (!_areaPorPersonagem.TryGetValue(personagemId, out var area))
            {
                return null;
            }

            var avatar = _areas[area].Obter(personagemId);
            return avatar == null ? null : (area, avatar);
        }
    }

    private void RemoverAreaVazia(string area)
    {
        if (
            area != AreasDoMundo.Cidade
            && _areas.TryGetValue(area, out var estado)
            && estado.Quantidade == 0
        )
        {
            _areas.Remove(area);
        }
    }
}

internal sealed record TransicaoDeArea<TSessao>(
    string Origem,
    AvatarNaCidade<TSessao>[] OutrosNaOrigem,
    bool Novo,
    AvatarNaCidade<TSessao> Eu,
    AvatarNaCidade<TSessao>[] OutrosNoDestino
);
