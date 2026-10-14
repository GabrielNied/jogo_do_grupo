using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A esquiva no servidor (docs/features/fatia-jogavel.md, seção 3). O client manda a intenção;
/// aqui se decide se ela vale (não está em outra ação, não está caído, tem stamina), cobra-se o
/// stamina uma vez e pede-se ao Mundo a janela de deslocamento. Quem valida cada posição continua
/// sendo o Mundo. Os primeiros 0,3 s da esquiva não levam dano direto (ControleDeAcoes); depois, ela evita dano por tirar o personagem da área. Na recuperação de um golpe, corta o que falta dela.
/// </summary>
public sealed class ServicoDeEsquivas : IDisposable
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly IDeslocamentosDoMundo _deslocamentos;
    private readonly ControleDeAcoes _controle;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeEsquivas> _log;
    private readonly Func<long> _relogioMs;
    private readonly ServicoDeQueda? _queda;

    public ServicoDeEsquivas(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        IDeslocamentosDoMundo deslocamentos,
        ControleDeAcoes controle,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeEsquivas> log,
        Func<long>? relogioMs = null,
        ServicoDeQueda? queda = null
    )
    {
        _queda = queda;
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _deslocamentos = deslocamentos;
        _controle = controle;
        _escopos = escopos;
        _log = log;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    public void Dispose() => _tempoReal.SessaoEncerrada -= AoEncerrar;

    /// <summary>Trata um <c>IntencaoDeEsquiva</c> (laço de rede).</summary>
    public void AoEsquivar(SessaoTempoReal sessao, IntencaoDeEsquiva intencao)
    {
        var personagemId = sessao.PersonagemId;

        // Direção inválida ou personagem fora do mundo: o contrato não tem código para isso,
        // então o pedido é descartado sem resposta (um client honesto nunca manda).
        if (!float.IsFinite(intencao.DirecaoY) || !_posicoes.TentarObter(personagemId, out _))
        {
            return;
        }

        // Esquivar interrompe quem levantava um aliado (quem está caído é recusado mais adiante).
        _queda?.AoAgir(personagemId);

        if (_controle.EstaAtordoado(personagemId))
        {
            Recusar(sessao, intencao.Sequencia, CodigosErroCombate.VoceEstaAtordoado);
            return;
        }

        var ocupou = _controle.TentarOcupar(
            personagemId,
            _relogioMs(),
            RegrasEsquiva.DuracaoMs,
            out var ocupacaoId
        );
        if (!ocupou)
        {
            Recusar(sessao, intencao.Sequencia, CodigosErroCombate.AcaoEmAndamento);
            return;
        }

        _ = ExecutarAsync(sessao, intencao, ocupacaoId);
    }

    // Cobra a stamina; só depois de pago abre a janela no Mundo e avisa a área.
    private async Task ExecutarAsync(
        SessaoTempoReal sessao,
        IntencaoDeEsquiva intencao,
        long ocupacaoId
    )
    {
        var personagemId = sessao.PersonagemId;
        try
        {
            var inicio = await RecursosDoAtaque.TentarGastarStaminaAsync(
                _escopos,
                personagemId,
                RegrasEsquiva.CustoDeStamina,
                CancellationToken.None
            );
            if (inicio.Recusa != null)
            {
                _controle.Desocupar(personagemId, ocupacaoId);
                Recusar(sessao, intencao.Sequencia, inicio.Recusa);
                return;
            }

            if (inicio.Recursos != null)
            {
                _tempoReal.Enviar(sessao, CombateMessageIds.RecursosMudaram, inicio.Recursos);
            }

            var abriu = _deslocamentos.AbrirJanela(
                personagemId,
                RegrasEsquiva.DistanciaMetros,
                RegrasEsquiva.DuracaoSegundos
            );
            if (!abriu || !_posicoes.TentarObter(personagemId, out var posicao))
            {
                return; // Saiu do mundo enquanto o banco respondia.
            }

            var aviso = new EsquivaIniciada
            {
                PersonagemId = personagemId,
                DirecaoY = intencao.DirecaoY,
                DuracaoMs = RegrasEsquiva.DuracaoMs,
            };
            var presentes = new HashSet<long>(_posicoes.PersonagensNaArea(posicao.Area))
            {
                personagemId,
            };
            foreach (var outra in _tempoReal.Sessoes)
            {
                if (presentes.Contains(outra.PersonagemId))
                {
                    _tempoReal.Enviar(outra, CombateMessageIds.EsquivaIniciada, aviso);
                }
            }
        }
        catch (Exception e)
        {
            _log.LogError(e, "Erro na esquiva do personagem {Personagem}.", personagemId);
            _controle.Desocupar(personagemId, ocupacaoId);
        }
    }

    private void Recusar(SessaoTempoReal sessao, int sequencia, string codigo) =>
        _tempoReal.Enviar(
            sessao,
            CombateMessageIds.EsquivaRecusada,
            new EsquivaRecusada { Sequencia = sequencia, Codigo = codigo }
        );

    // Saiu do jogo de vez (não foi só troca de sessão): a reserva dele não precisa ficar.
    private void AoEncerrar(SessaoTempoReal sessao)
    {
        var id = sessao.PersonagemId;
        if (
            !_tempoReal.TentarObterSessao(id, out var outra)
            || outra is null
            || ReferenceEquals(outra, sessao)
        )
        {
            _controle.Esquecer(id);
        }
    }
}
