using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Core.Canais;

/// <summary>
/// Implementação em memória do gerenciamento e balanceamento de canais (ADR 0045). Thread-safe.
/// </summary>
public sealed class GerenciadorDeCanais : IGerenciadorDeCanais
{
    private readonly Lock _trava = new();
    private readonly Dictionary<int, HashSet<long>> _jogadoresPorCanal = [];
    private readonly Dictionary<long, int> _canalPorPersonagem = [];

    public int QuantidadeCanais { get; }
    public int CapacidadePorCanal { get; }

    public GerenciadorDeCanais(ConfiguracaoCanais? config = null)
    {
        var configuracao = config ?? new ConfiguracaoCanais();
        QuantidadeCanais = Math.Max(1, configuracao.QuantidadeCanais);
        CapacidadePorCanal = Math.Max(1, configuracao.CapacidadePorCanal);

        for (var i = 1; i <= QuantidadeCanais; i++)
        {
            _jogadoresPorCanal[i] = [];
        }
    }

    public int? ObterCanalDoPersonagem(long personagemId)
    {
        lock (_trava)
        {
            return _canalPorPersonagem.TryGetValue(personagemId, out var canal) ? canal : null;
        }
    }

    public int OcupacaoDoCanal(int canal)
    {
        lock (_trava)
        {
            return _jogadoresPorCanal.TryGetValue(canal, out var conjunto) ? conjunto.Count : 0;
        }
    }

    public IReadOnlyList<InfoCanal> ObterStatusDosCanais()
    {
        lock (_trava)
        {
            var lista = new List<InfoCanal>(QuantidadeCanais);
            for (var i = 1; i <= QuantidadeCanais; i++)
            {
                lista.Add(
                    new InfoCanal
                    {
                        Canal = i,
                        Jogadores = _jogadoresPorCanal[i].Count,
                        Capacidade = CapacidadePorCanal,
                    }
                );
            }

            return lista;
        }
    }

    public int? EscolherCanalDeEntrada(long personagemId, IEnumerable<long>? membrosDoGrupo = null)
    {
        lock (_trava)
        {
            if (
                _canalPorPersonagem.TryGetValue(personagemId, out var existente)
                && existente >= 1
                && existente <= QuantidadeCanais
            )
            {
                return existente;
            }

            if (membrosDoGrupo != null)
            {
                foreach (var membroId in membrosDoGrupo)
                {
                    if (
                        _canalPorPersonagem.TryGetValue(membroId, out var canalMembro)
                        && canalMembro >= 1
                        && canalMembro <= QuantidadeCanais
                        && _jogadoresPorCanal[canalMembro].Count < CapacidadePorCanal
                    )
                    {
                        return canalMembro;
                    }
                }
            }

            for (var c = 1; c <= QuantidadeCanais; c++)
            {
                if (_jogadoresPorCanal[c].Count < CapacidadePorCanal)
                {
                    return c;
                }
            }

            return null;
        }
    }

    public bool RegistrarEntrada(long personagemId, int canal)
    {
        lock (_trava)
        {
            if (canal < 1 || canal > QuantidadeCanais)
            {
                return false;
            }

            if (_canalPorPersonagem.TryGetValue(personagemId, out var atual) && atual == canal)
            {
                return true;
            }

            if (_jogadoresPorCanal[canal].Count >= CapacidadePorCanal)
            {
                return false;
            }

            if (_canalPorPersonagem.TryGetValue(personagemId, out var antigo))
            {
                _jogadoresPorCanal[antigo].Remove(personagemId);
            }

            _jogadoresPorCanal[canal].Add(personagemId);
            _canalPorPersonagem[personagemId] = canal;
            return true;
        }
    }

    public void RegistrarSaida(long personagemId)
    {
        lock (_trava)
        {
            if (_canalPorPersonagem.Remove(personagemId, out var canal))
            {
                _jogadoresPorCanal[canal].Remove(personagemId);
            }
        }
    }

    public ResultadoTrocaDeCanal TentarTrocarCanal(
        long personagemId,
        int canalDestino,
        bool emCombate
    )
    {
        lock (_trava)
        {
            var canalAtual = _canalPorPersonagem.TryGetValue(personagemId, out var c) ? c : 1;

            if (emCombate)
            {
                return new(false, canalAtual, canalDestino, CodigosErroMundo.EmCombate);
            }

            if (canalDestino < 1 || canalDestino > QuantidadeCanais)
            {
                return new(false, canalAtual, canalDestino, CodigosErroMundo.CanalInvalido);
            }

            if (canalDestino == canalAtual)
            {
                return new(false, canalAtual, canalDestino, CodigosErroMundo.MesmoCanal);
            }

            if (_jogadoresPorCanal[canalDestino].Count >= CapacidadePorCanal)
            {
                return new(false, canalAtual, canalDestino, CodigosErroMundo.CanalLotado);
            }

            _jogadoresPorCanal[canalAtual].Remove(personagemId);
            _jogadoresPorCanal[canalDestino].Add(personagemId);
            _canalPorPersonagem[personagemId] = canalDestino;

            return new(true, canalAtual, canalDestino);
        }
    }
}
