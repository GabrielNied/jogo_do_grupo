using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Inscrição ativa de um jogador na fila automática de dungeon.
/// </summary>
public sealed record InscricaoNaFila(long PersonagemId, PapelNoGrupo Papel, long EntrouEmMs);

/// <summary>
/// Gerenciador autoritativo da fila automática de grupo (Dungeon Finder).
/// Regra: emparelha 1 Tank, 1 Healer e 3 DPS em um grupo de 5 jogadores.
/// Thread-safe: todas as mutações e checagens são protegidas por trava.
/// </summary>
public sealed class FilaDeGrupoDoServidor(
    GruposDoServidor grupos,
    Action<long, ushort, object> enviarMensagem,
    Func<long> relogioMs
)
{
    private readonly Lock _trava = new();
    private readonly Dictionary<long, InscricaoNaFila> _inscricoes = [];

    public int TotalEmFila
    {
        get
        {
            lock (_trava)
            {
                return _inscricoes.Count;
            }
        }
    }

    public int QuantidadeTanques
    {
        get
        {
            lock (_trava)
            {
                return _inscricoes.Values.Count(i => i.Papel == PapelNoGrupo.Tank);
            }
        }
    }

    public int QuantidadeHealers
    {
        get
        {
            lock (_trava)
            {
                return _inscricoes.Values.Count(i => i.Papel == PapelNoGrupo.Healer);
            }
        }
    }

    public int QuantidadeDps
    {
        get
        {
            lock (_trava)
            {
                return _inscricoes.Values.Count(i => i.Papel == PapelNoGrupo.Dps);
            }
        }
    }

    /// <summary>
    /// Inscreve um personagem na fila com o papel desejado.
    /// Retorna false se o papel for inválido ou se o personagem já estiver em grupo.
    /// </summary>
    public bool Entrar(long personagemId, PapelNoGrupo papel)
    {
        if (!RegrasFilaGrupo.PapelValido(papel))
        {
            return false;
        }

        // Jogador já em grupo não pode entrar na fila solo
        if (grupos.Consultar(personagemId) != null)
        {
            return false;
        }

        lock (_trava)
        {
            var agora = relogioMs();
            _inscricoes[personagemId] = new InscricaoNaFila(personagemId, papel, agora);

            // Tenta formar grupo imediatamente se houver composição completa
            if (!TentarFormarGrupo(agora))
            {
                // Envia status atualizado a quem acabou de entrar
                EnviarStatus(personagemId, agora);
            }

            return true;
        }
    }

    /// <summary>
    /// Cancela a inscrição de um personagem na fila.
    /// </summary>
    public bool Sair(long personagemId)
    {
        lock (_trava)
        {
            if (!_inscricoes.Remove(personagemId))
            {
                return false;
            }

            enviarMensagem(
                personagemId,
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo { EmFila = false }
            );

            return true;
        }
    }

    /// <summary>
    /// Chamado quando um jogador desconecta da sessão de tempo real.
    /// </summary>
    public void AoDesconectar(long personagemId)
    {
        lock (_trava)
        {
            _inscricoes.Remove(personagemId);
        }
    }

    /// <summary>
    /// Obtém o status da fila para um personagem específico.
    /// </summary>
    public StatusDaFilaDeGrupo ObterStatus(long personagemId)
    {
        lock (_trava)
        {
            var agora = relogioMs();
            if (!_inscricoes.TryGetValue(personagemId, out var inscricao))
            {
                return new StatusDaFilaDeGrupo
                {
                    EmFila = false,
                    Tanques = Math.Min(1, QuantidadeTanques),
                    Healers = Math.Min(1, QuantidadeHealers),
                    Dps = Math.Min(3, QuantidadeDps),
                };
            }

            var segundos = (int)Math.Max(0, (agora - inscricao.EntrouEmMs) / 1000);
            return new StatusDaFilaDeGrupo
            {
                EmFila = true,
                Papel = inscricao.Papel,
                Tanques = Math.Min(1, QuantidadeTanques),
                Healers = Math.Min(1, QuantidadeHealers),
                Dps = Math.Min(3, QuantidadeDps),
                SegundosNaFila = segundos,
            };
        }
    }

    private bool TentarFormarGrupo(long agora)
    {
        var tanque = _inscricoes
            .Values.Where(i => i.Papel == PapelNoGrupo.Tank)
            .OrderBy(i => i.EntrouEmMs)
            .FirstOrDefault();
        var healer = _inscricoes
            .Values.Where(i => i.Papel == PapelNoGrupo.Healer)
            .OrderBy(i => i.EntrouEmMs)
            .FirstOrDefault();
        var dpsList = _inscricoes
            .Values.Where(i => i.Papel == PapelNoGrupo.Dps)
            .OrderBy(i => i.EntrouEmMs)
            .Take(3)
            .ToList();

        if (tanque == null || healer == null || dpsList.Count < 3)
        {
            return false;
        }

        var candidatos = new List<InscricaoNaFila> { tanque, healer };
        candidatos.AddRange(dpsList);

        var ids = candidatos.Select(c => c.PersonagemId).ToArray();
        var grupoId = grupos.FormarGrupoDeFila(ids);
        if (grupoId == null)
        {
            // Se algum candidato estava inválido, remove apenas quem falhou
            foreach (var c in candidatos)
            {
                if (grupos.Consultar(c.PersonagemId) != null)
                {
                    _inscricoes.Remove(c.PersonagemId);
                }
            }

            return false;
        }

        // Grupo formado com sucesso! Remove da fila e avisa a todos os 5
        foreach (var c in candidatos)
        {
            _inscricoes.Remove(c.PersonagemId);

            enviarMensagem(
                c.PersonagemId,
                SocialMessageIds.GrupoDeFilaFormado,
                new GrupoDeFilaFormado
                {
                    GrupoId = grupoId.Value,
                    SeuPapel = c.Papel,
                    Mensagem = "Grupo de dungeon formado com sucesso!",
                }
            );

            enviarMensagem(
                c.PersonagemId,
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo { EmFila = false }
            );
        }

        return true;
    }

    private void EnviarStatus(long personagemId, long agora)
    {
        if (_inscricoes.TryGetValue(personagemId, out var inscricao))
        {
            var segundos = (int)Math.Max(0, (agora - inscricao.EntrouEmMs) / 1000);
            var status = new StatusDaFilaDeGrupo
            {
                EmFila = true,
                Papel = inscricao.Papel,
                Tanques = Math.Min(1, QuantidadeTanques),
                Healers = Math.Min(1, QuantidadeHealers),
                Dps = Math.Min(3, QuantidadeDps),
                SegundosNaFila = segundos,
            };

            enviarMensagem(personagemId, SocialMessageIds.StatusDaFilaDeGrupo, status);
        }
    }
}
