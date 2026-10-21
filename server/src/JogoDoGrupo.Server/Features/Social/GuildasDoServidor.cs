using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Estado e regras de guildas no servidor, em memória.
/// Cuida de criação, convites, cargos, expulsão, liderança, saída e entrega de dados em tempo real.
/// Regras puras e permissões em <see cref="RegrasGuilda"/> (shared).
/// </summary>
public sealed class GuildasDoServidor(
    IDiretorioDePersonagens diretorio,
    ISaidaDaGuilda saida,
    Func<long, long, Task<bool>> debitarCoroas,
    Action<long> aoMovimentarCoroas,
    Func<long, long, bool>? contatoBarradoEntrePersonagens = null
)
{
    private sealed class Guilda
    {
        public long Id { get; init; }
        public string Nome { get; init; } = "";
        public long LiderId { get; set; }
        public Dictionary<long, MembroInterno> Membros { get; } = [];
    }

    private sealed class MembroInterno
    {
        public long PersonagemId { get; init; }
        public string Nome { get; init; } = "";
        public CargoDaGuilda Cargo { get; set; }
        public long EntrouEmUnix { get; init; }
    }

    private readonly Lock _trava = new();
    private readonly HashSet<long> _online = [];
    private readonly Dictionary<long, Guilda> _guildasPorId = [];
    private readonly Dictionary<string, Guilda> _guildasPorNome = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<long, long> _guildaDoPersonagem = [];
    private readonly Dictionary<
        (long GuildaId, long AlvoId),
        (long ConvidouId, long ExpiraMs)
    > _convites = [];
    private long _proximoGuildaId = 1;

    public void AoConectar(long personagemId)
    {
        lock (_trava)
        {
            _online.Add(personagemId);
            if (
                _guildaDoPersonagem.TryGetValue(personagemId, out var guildaId)
                && _guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                TransmitirDadosParaMembros(guilda);
            }
        }
    }

    public void AoDesconectar(long personagemId)
    {
        lock (_trava)
        {
            _online.Remove(personagemId);
            if (
                _guildaDoPersonagem.TryGetValue(personagemId, out var guildaId)
                && _guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                TransmitirDadosParaMembros(guilda);
            }
        }
    }

    public IReadOnlyList<long> ObterMembrosDaGuildaDoPersonagem(long personagemId)
    {
        lock (_trava)
        {
            if (
                _guildaDoPersonagem.TryGetValue(personagemId, out var guildaId)
                && _guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                return guilda.Membros.Keys.ToArray();
            }

            return [];
        }
    }

    public long ObterGuildaId(long personagemId)
    {
        lock (_trava)
        {
            return _guildaDoPersonagem.TryGetValue(personagemId, out var guildaId) ? guildaId : 0;
        }
    }

    public string ObterNomeGuilda(long guildaId)
    {
        lock (_trava)
        {
            return _guildasPorId.TryGetValue(guildaId, out var guilda) ? guilda.Nome : "";
        }
    }

    public CargoDaGuilda? ObterCargo(long personagemId)
    {
        lock (_trava)
        {
            if (
                _guildaDoPersonagem.TryGetValue(personagemId, out var guildaId)
                && _guildasPorId.TryGetValue(guildaId, out var guilda)
                && guilda.Membros.TryGetValue(personagemId, out var membro)
            )
            {
                return membro.Cargo;
            }

            return null;
        }
    }

    public void Consultar(long personagemId)
    {
        lock (_trava)
        {
            if (
                _guildaDoPersonagem.TryGetValue(personagemId, out var guildaId)
                && _guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                saida.Dados(personagemId, GerarDados(guilda, personagemId));
            }
            else
            {
                saida.Dados(
                    personagemId,
                    new DadosDaGuilda
                    {
                        GuildaId = 0,
                        Nome = "",
                        MeuCargo = CargoDaGuilda.Recruta,
                        Membros = [],
                    }
                );
            }
        }
    }

    public async Task CriarAsync(long personagemId, string? nomeRaw)
    {
        if (string.IsNullOrWhiteSpace(nomeRaw))
        {
            saida.Recusada(personagemId, CodigosErroSocial.NomeDeGuildaInvalido);
            return;
        }

        var nome = nomeRaw.Trim();
        if (!RegrasGuilda.ValidarNome(nome))
        {
            saida.Recusada(personagemId, CodigosErroSocial.NomeDeGuildaInvalido);
            return;
        }

        lock (_trava)
        {
            if (_guildaDoPersonagem.ContainsKey(personagemId))
            {
                saida.Recusada(personagemId, CodigosErroSocial.JaPossuiGuilda);
                return;
            }

            if (_guildasPorNome.ContainsKey(nome))
            {
                saida.Recusada(personagemId, CodigosErroSocial.NomeDeGuildaEmUso);
                return;
            }
        }

        var debitou = await debitarCoroas(personagemId, RegrasGuilda.CustoCriacaoCoroas);
        if (!debitou)
        {
            saida.Recusada(personagemId, CodigosErroSocial.CoroasInsuficientes);
            return;
        }

        Guilda guilda;
        lock (_trava)
        {
            if (_guildaDoPersonagem.ContainsKey(personagemId) || _guildasPorNome.ContainsKey(nome))
            {
                saida.Recusada(personagemId, CodigosErroSocial.NomeDeGuildaEmUso);
                return;
            }

            diretorio.TentarObterNome(personagemId, out var nomePersonagem);
            var guildaId = _proximoGuildaId++;
            guilda = new Guilda
            {
                Id = guildaId,
                Nome = nome,
                LiderId = personagemId,
            };
            guilda.Membros[personagemId] = new MembroInterno
            {
                PersonagemId = personagemId,
                Nome = nomePersonagem,
                Cargo = CargoDaGuilda.Lider,
                EntrouEmUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            _guildasPorId[guildaId] = guilda;
            _guildasPorNome[nome] = guilda;
            _guildaDoPersonagem[personagemId] = guildaId;
        }

        aoMovimentarCoroas(personagemId);
        saida.Dados(personagemId, GerarDados(guilda, personagemId));
    }

    public void Convidar(long executorId, string? nomeAlvoRaw)
    {
        lock (_trava)
        {
            if (
                !_guildaDoPersonagem.TryGetValue(executorId, out var guildaId)
                || !_guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                saida.Recusada(executorId, CodigosErroSocial.NaoPossuiGuilda);
                return;
            }

            var executor = guilda.Membros[executorId];
            if (!RegrasGuilda.PodeConvidar(executor.Cargo))
            {
                saida.Recusada(executorId, CodigosErroSocial.SemPermissaoNaGuilda);
                return;
            }

            if (guilda.Membros.Count >= RegrasGuilda.MaximoDeMembros)
            {
                saida.Recusada(executorId, CodigosErroSocial.GuildaCheia);
                return;
            }

            if (
                string.IsNullOrWhiteSpace(nomeAlvoRaw)
                || !diretorio.TentarObterPorNome(nomeAlvoRaw.Trim(), out var alvoId)
            )
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            if (!_online.Contains(alvoId))
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            if (alvoId == executorId)
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoInvalido);
                return;
            }

            if (_guildaDoPersonagem.ContainsKey(alvoId))
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoJaPossuiGuilda);
                return;
            }

            if (contatoBarradoEntrePersonagens?.Invoke(executorId, alvoId) == true)
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            _convites[(guilda.Id, alvoId)] = (executorId, Environment.TickCount64 + 60_000);

            saida.Convite(
                alvoId,
                new ConviteDeGuildaRecebido
                {
                    GuildaId = guilda.Id,
                    NomeDaGuilda = guilda.Nome,
                    ConvidanteNome = executor.Nome,
                }
            );
        }
    }

    public void ResponderConvite(long personagemId, long guildaId, bool aceitar)
    {
        lock (_trava)
        {
            if (!_convites.Remove((guildaId, personagemId)))
            {
                saida.Recusada(personagemId, CodigosErroSocial.ConviteInvalido);
                return;
            }

            if (!aceitar)
            {
                return;
            }

            if (_guildaDoPersonagem.ContainsKey(personagemId))
            {
                saida.Recusada(personagemId, CodigosErroSocial.JaPossuiGuilda);
                return;
            }

            if (!_guildasPorId.TryGetValue(guildaId, out var guilda))
            {
                saida.Recusada(personagemId, CodigosErroSocial.GuildaNaoEncontrada);
                return;
            }

            if (guilda.Membros.Count >= RegrasGuilda.MaximoDeMembros)
            {
                saida.Recusada(personagemId, CodigosErroSocial.GuildaCheia);
                return;
            }

            diretorio.TentarObterNome(personagemId, out var nomePersonagem);
            guilda.Membros[personagemId] = new MembroInterno
            {
                PersonagemId = personagemId,
                Nome = nomePersonagem,
                Cargo = CargoDaGuilda.Recruta,
                EntrouEmUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            _guildaDoPersonagem[personagemId] = guildaId;

            TransmitirDadosParaMembros(guilda);
        }
    }

    public void Expulsar(long executorId, long alvoId)
    {
        lock (_trava)
        {
            if (
                !_guildaDoPersonagem.TryGetValue(executorId, out var guildaId)
                || !_guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                saida.Recusada(executorId, CodigosErroSocial.NaoPossuiGuilda);
                return;
            }

            if (!guilda.Membros.TryGetValue(alvoId, out var alvo))
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoNaoEstaNaGuilda);
                return;
            }

            if (alvoId == executorId)
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoInvalido);
                return;
            }

            var executor = guilda.Membros[executorId];
            if (!RegrasGuilda.PodeExpulsar(executor.Cargo, alvo.Cargo))
            {
                saida.Recusada(executorId, CodigosErroSocial.SemPermissaoNaGuilda);
                return;
            }

            guilda.Membros.Remove(alvoId);
            _guildaDoPersonagem.Remove(alvoId);

            saida.Dados(
                alvoId,
                new DadosDaGuilda
                {
                    GuildaId = 0,
                    Nome = "",
                    MeuCargo = CargoDaGuilda.Recruta,
                    Membros = [],
                }
            );

            TransmitirDadosParaMembros(guilda);
        }
    }

    public void AlterarCargo(long executorId, long alvoId, CargoDaGuilda novoCargo)
    {
        lock (_trava)
        {
            if (
                !_guildaDoPersonagem.TryGetValue(executorId, out var guildaId)
                || !_guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                saida.Recusada(executorId, CodigosErroSocial.NaoPossuiGuilda);
                return;
            }

            if (!guilda.Membros.TryGetValue(alvoId, out var alvo))
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoNaoEstaNaGuilda);
                return;
            }

            if (alvoId == executorId)
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoInvalido);
                return;
            }

            if (
                novoCargo == CargoDaGuilda.Lider
                || !Enum.IsDefined(typeof(CargoDaGuilda), novoCargo)
            )
            {
                saida.Recusada(executorId, CodigosErroSocial.SemPermissaoNaGuilda);
                return;
            }

            var executor = guilda.Membros[executorId];
            if (!RegrasGuilda.PodeAlterarCargo(executor.Cargo, alvo.Cargo, novoCargo))
            {
                saida.Recusada(executorId, CodigosErroSocial.SemPermissaoNaGuilda);
                return;
            }

            alvo.Cargo = novoCargo;
            TransmitirDadosParaMembros(guilda);
        }
    }

    public void PassarLideranca(long executorId, long novoLiderId)
    {
        lock (_trava)
        {
            if (
                !_guildaDoPersonagem.TryGetValue(executorId, out var guildaId)
                || !_guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                saida.Recusada(executorId, CodigosErroSocial.NaoPossuiGuilda);
                return;
            }

            if (!guilda.Membros.TryGetValue(novoLiderId, out var novoLider))
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoNaoEstaNaGuilda);
                return;
            }

            if (novoLiderId == executorId)
            {
                saida.Recusada(executorId, CodigosErroSocial.AlvoInvalido);
                return;
            }

            var executor = guilda.Membros[executorId];
            if (executor.Cargo != CargoDaGuilda.Lider)
            {
                saida.Recusada(executorId, CodigosErroSocial.SemPermissaoNaGuilda);
                return;
            }

            executor.Cargo = CargoDaGuilda.Oficial;
            novoLider.Cargo = CargoDaGuilda.Lider;
            guilda.LiderId = novoLiderId;

            TransmitirDadosParaMembros(guilda);
        }
    }

    public void Sair(long personagemId)
    {
        lock (_trava)
        {
            if (
                !_guildaDoPersonagem.TryGetValue(personagemId, out var guildaId)
                || !_guildasPorId.TryGetValue(guildaId, out var guilda)
            )
            {
                saida.Recusada(personagemId, CodigosErroSocial.NaoPossuiGuilda);
                return;
            }

            var membro = guilda.Membros[personagemId];
            if (membro.Cargo == CargoDaGuilda.Lider && guilda.Membros.Count > 1)
            {
                saida.Recusada(personagemId, CodigosErroSocial.LiderNaoPodeSair);
                return;
            }

            guilda.Membros.Remove(personagemId);
            _guildaDoPersonagem.Remove(personagemId);

            saida.Dados(
                personagemId,
                new DadosDaGuilda
                {
                    GuildaId = 0,
                    Nome = "",
                    MeuCargo = CargoDaGuilda.Recruta,
                    Membros = [],
                }
            );

            if (guilda.Membros.Count == 0)
            {
                _guildasPorId.Remove(guildaId);
                _guildasPorNome.Remove(guilda.Nome);
            }
            else
            {
                TransmitirDadosParaMembros(guilda);
            }
        }
    }

    private void TransmitirDadosParaMembros(Guilda guilda)
    {
        foreach (var membro in guilda.Membros.Values)
        {
            if (_online.Contains(membro.PersonagemId))
            {
                saida.Dados(membro.PersonagemId, GerarDados(guilda, membro.PersonagemId));
            }
        }
    }

    private DadosDaGuilda GerarDados(Guilda guilda, long destinatarioId)
    {
        var meuCargo = guilda.Membros.TryGetValue(destinatarioId, out var m)
            ? m.Cargo
            : CargoDaGuilda.Recruta;

        return new DadosDaGuilda
        {
            GuildaId = guilda.Id,
            Nome = guilda.Nome,
            MeuCargo = meuCargo,
            Membros = guilda
                .Membros.Values.Select(m => new MembroDaGuilda
                {
                    PersonagemId = m.PersonagemId,
                    Nome = m.Nome,
                    Cargo = m.Cargo,
                    Online = _online.Contains(m.PersonagemId),
                    Area = "",
                    EntrouEmUnix = m.EntrouEmUnix,
                })
                .ToArray(),
        };
    }
}
