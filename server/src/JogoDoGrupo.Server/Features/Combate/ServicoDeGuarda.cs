using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A guarda segurada no servidor (docs/features/combate.md, "Guarda segurada"). O client só
/// informa que segura ou soltou; aqui se decide se a guarda sobe (<see cref="ControleDeAcoes"/>),
/// avisa a área e pede ao Mundo o teto de 40% da corrida. O custo do golpe e a quebra são
/// decididos em <see cref="ServicoDeDanoRecebido"/>, que chama <see cref="AoBloquear"/> e
/// <see cref="AoQuebrar"/> para o aviso.
/// </summary>
public sealed class ServicoDeGuarda
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly ControleDeAcoes _controle;
    private readonly AvisosNaArea _avisos;
    private readonly ServicoDeQueda? _queda;
    private readonly ILimiteDeVelocidadeDoMundo? _limite;
    private readonly Func<long> _relogioMs;
    private readonly IServiceScopeFactory? _escopos;
    private readonly ILogger<ServicoDeGuarda>? _log;

    public ServicoDeGuarda(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        ControleDeAcoes controle,
        ServicoDeQueda? queda = null,
        ILimiteDeVelocidadeDoMundo? limite = null,
        Func<long>? relogioMs = null,
        IServiceScopeFactory? escopos = null,
        ILogger<ServicoDeGuarda>? log = null
    )
    {
        _escopos = escopos;
        _log = log;
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _controle = controle;
        _avisos = new AvisosNaArea(tempoReal, posicoes);
        _queda = queda;
        _limite = limite;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    /// <summary>Trata um <c>IntencaoDeGuarda</c> (laço de rede).</summary>
    public void AoGuardar(SessaoTempoReal sessao, IntencaoDeGuarda intencao)
    {
        var personagemId = sessao.PersonagemId;
        if (!_posicoes.TentarObter(personagemId, out var posicao))
        {
            return; // Fora do mundo: não há de onde guardar.
        }

        if (!intencao.Segurando)
        {
            if (_controle.SoltarGuarda(personagemId))
            {
                LiberarVelocidade(personagemId);
                PausarRegeneracao(personagemId, segurando: false);
                Avisar(posicao.Area, personagemId, segurando: false, quebrou: false);
            }

            return;
        }

        // Segurar a guarda interrompe quem levantava um aliado, como atacar e esquivar.
        _queda?.AoAgir(personagemId);

        var caido = _queda?.EstaCaido(personagemId) == true;
        if (caido || !_controle.TentarGuardar(personagemId, _relogioMs()))
        {
            // Recusada: só o dono fica sabendo, para soltar a postura que antecipou.
            _tempoReal.Enviar(
                sessao,
                CombateMessageIds.GuardaMudou,
                new GuardaMudou { PersonagemId = personagemId, Segurando = false }
            );
            return;
        }

        _limite?.LimitarVelocidade(
            personagemId,
            RegrasGuarda.VelocidadeComGuarda,
            RegrasGuarda.LimiteDeVelocidadeSegundos
        );
        PausarRegeneracao(personagemId, segurando: true);
        Avisar(posicao.Area, personagemId, segurando: true, quebrou: false);
    }

    /// <summary>A guarda segurou um golpe: avisa a área.</summary>
    public void AoBloquear(long personagemId, string area, int vigorGasto) =>
        _avisos.Enviar(
            area,
            personagemId,
            CombateMessageIds.GolpeBloqueado,
            new GolpeBloqueado { PersonagemId = personagemId, VigorGasto = vigorGasto }
        );

    /// <summary>A guarda quebrou (sem vigor para o golpe): solta, devolve a corrida e avisa.</summary>
    public void AoQuebrar(long personagemId, string area)
    {
        _controle.SoltarGuarda(personagemId);
        LiberarVelocidade(personagemId);
        PausarRegeneracao(personagemId, segurando: false);
        Avisar(area, personagemId, segurando: false, quebrou: true);
    }

    private void LiberarVelocidade(long personagemId) =>
        _limite?.LimitarVelocidade(personagemId, 1.0, 0.1);

    // Guarda segurada pausa a espera da regeneração do vigor. Roda numa tarefa: o laço de rede
    // não espera o banco.
    private void PausarRegeneracao(long personagemId, bool segurando)
    {
        if (_escopos == null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var estado = await RecursosDoAtaque
                    .Recursos(_escopos)
                    .DefinirGuardaAsync(personagemId, segurando, CancellationToken.None);

                // O client prevê a regeneração do vigor; saber que ela parou (ou recomeçou) corrige a previsão.
                var dono = _tempoReal.Sessoes.FirstOrDefault(s => s.PersonagemId == personagemId);
                if (dono != null)
                {
                    _tempoReal.Enviar(dono, CombateMessageIds.RecursosMudaram, estado);
                }
            }
            catch (Exception e)
            {
                _log?.LogError(
                    e,
                    "Erro ao marcar a guarda do personagem {Personagem}.",
                    personagemId
                );
            }
        });
    }

    private void Avisar(string area, long personagemId, bool segurando, bool quebrou) =>
        _avisos.Enviar(
            area,
            personagemId,
            CombateMessageIds.GuardaMudou,
            new GuardaMudou
            {
                PersonagemId = personagemId,
                Segurando = segurando,
                Quebrou = quebrou,
            },
            // Quem soltou a guarda já sabe; o dono só ouve quando ela sobe ou quebra.
            incluirDono: segurando || quebrou
        );

    // Saiu do jogo de vez: a guarda não fica presa. Trocar de sessão não conta.
    private void AoEncerrar(SessaoTempoReal sessao)
    {
        var id = sessao.PersonagemId;
        if (!_tempoReal.Sessoes.Any(s => s.PersonagemId == id && !ReferenceEquals(s, sessao)))
        {
            _controle.SoltarGuarda(id);
        }
    }
}
