using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O dano que o personagem recebe de fora de Combate. Hoje só o golpe de inimigo: Mobs decide
/// quem estava no círculo e publica <see cref="GolpeDeInimigoAcertou"/>; aqui, como dono da vida,
/// se decide quanto o golpe tira. Vida 0 vira o estado caído em <see cref="ServicoDeQueda"/>; quem já está caído não leva dano.
/// <para>
/// O evento sai do laço de IA de Mobs e o manipulador não bloqueia: o banco roda numa tarefa,
/// e o aviso ao dono sai pelo <see cref="ServidorTempoReal"/> (seguro para qualquer thread).
/// </para>
/// </summary>
public sealed class ServicoDeDanoRecebido : IHostedService
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeDanoRecebido> _log;
    private readonly ServicoDeQueda? _queda;
    private readonly ControleDeAcoes? _acoes;
    private readonly Func<long> _relogioMs;
    private readonly ServicoDeGuarda? _guarda;
    private readonly AvisosNaArea? _avisos;
    private readonly GerenciadorDeEfeitosDeStatus? _efeitos;
    private readonly IAtributosDosEquipamentos? _equipamentos;
    private readonly EventosDoServidor? _eventos;
    private readonly CancellationTokenSource _parada = new();

    public ServicoDeDanoRecebido(
        ServidorTempoReal tempoReal,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeDanoRecebido> log,
        ServicoDeQueda? queda = null,
        ControleDeAcoes? acoes = null,
        Func<long>? relogioMs = null,
        ServicoDeGuarda? guarda = null,
        IPosicoesDoMundo? posicoes = null,
        GerenciadorDeEfeitosDeStatus? efeitos = null,
        IVizinhanca? vizinhanca = null,
        IAtributosDosEquipamentos? equipamentos = null,
        EventosDoServidor? eventos = null
    )
    {
        _equipamentos = equipamentos;
        _efeitos = efeitos;
        _guarda = guarda;
        _avisos = posicoes == null ? null : new AvisosNaArea(tempoReal, posicoes, vizinhanca);
        _queda = queda;
        _acoes = acoes;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _tempoReal = tempoReal;
        _escopos = escopos;
        _log = log;
        _eventos = eventos;
    }

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException) { }

        return Task.CompletedTask;
    }

    /// <summary>
    /// O que o golpe tira de vida. Sem defesa por enquanto: quando ela existir, entra aqui, sem
    /// mexer em quem chama.
    /// </summary>
    public static int DanoFinal(int danoDoGolpe) => Math.Max(0, danoDoGolpe);

    /// <summary>
    /// O que o golpe tira de vida considerando a defesa de equipamento e a potência de bloqueio do golpe (#344).
    /// </summary>
    public static int DanoFinal(int danoDoGolpe, double defesa, double potenciaBloqueio) =>
        RegrasBloqueioPorDefesa.CalcularDanoFinal(danoDoGolpe, defesa, potenciaBloqueio);

    /// <summary>Trata <see cref="GolpeDeInimigoAcertou"/> sem bloquear quem publica.</summary>
    public void AoGolpeDeInimigo(GolpeDeInimigoAcertou golpe) => _ = AplicarAsync(golpe);

    private async Task AplicarAsync(GolpeDeInimigoAcertou golpe)
    {
        try
        {
            var dano = DanoFinal(golpe.Dano);
            if (dano <= 0)
            {
                return;
            }

            if (_efeitos != null)
            {
                dano = Math.Max(
                    1,
                    (int)Math.Round(dano * _efeitos.MultiplicadorDanoRecebido(golpe.PersonagemId))
                );
            }

            // Nos primeiros 0,3 s da esquiva o dano direto some (o servidor confere o instante).
            if (_acoes?.EstaInvulneravel(golpe.PersonagemId, _relogioMs()) == true)
            {
                return;
            }

            // Quem está caído ou com imunidade temporária após levantar não recebe dano.
            if (
                _queda?.EstaCaido(golpe.PersonagemId) == true
                || _queda?.EstaImuneAposLevantar(golpe.PersonagemId, _relogioMs()) == true
            )
            {
                return;
            }

            _tempoReal.TentarObterSessao(golpe.PersonagemId, out var sessao);

            var quebrouGuarda = false;
            // Guarda segurada: o golpe custa 1 de stamina por ponto de dano, antes da defesa do
            // equipamento. Com stamina ele zera; sem stamina a guarda quebra e o dano entra.
            if (_acoes?.EstaGuardando(golpe.PersonagemId) == true)
            {
                var (bloqueou, staminaGasto, estado) = await RecursosDoAtaque.BloquearAsync(
                    _escopos,
                    golpe.PersonagemId,
                    dano,
                    _parada.Token
                );
                if (sessao != null)
                {
                    _tempoReal.Enviar(sessao, CombateMessageIds.RecursosMudaram, estado);
                }

                if (bloqueou)
                {
                    _guarda?.AoBloquear(golpe.PersonagemId, golpe.Area, staminaGasto);
                    return;
                }

                quebrouGuarda = true;
                _guarda?.AoQuebrar(golpe.PersonagemId, golpe.Area);
            }

            double defesa = 0;
            if (_equipamentos != null)
            {
                try
                {
                    var atributos = await _equipamentos.ObterAsync(
                        golpe.PersonagemId,
                        _parada.Token
                    );
                    defesa =
                        golpe.Tipo == TipoDeAtaqueDeInimigo.Raios
                            ? atributos.DefesaMagica
                            : atributos.DefesaFisica;
                }
                catch (Exception ex)
                {
                    _log.LogWarning(
                        ex,
                        "Erro ao obter atributos de equipamento do personagem {Personagem}.",
                        golpe.PersonagemId
                    );
                }
            }

            var danoFinal = DanoFinal(dano, defesa, golpe.PotenciaBloqueio);

            if (sessao != null && quebrouGuarda && danoFinal > 0)
            {
                _tempoReal.Enviar(
                    sessao,
                    CombateMessageIds.DefesaInsuficiente,
                    new DefesaInsuficiente
                    {
                        PersonagemId = golpe.PersonagemId,
                        DanoResidual = danoFinal,
                        DanoOriginal = dano,
                    }
                );
            }

            var recursos = await RecursosDoAtaque.TirarVidaAsync(
                _escopos,
                golpe.PersonagemId,
                danoFinal,
                _parada.Token
            );

            if (sessao != null)
            {
                _tempoReal.Enviar(sessao, CombateMessageIds.RecursosMudaram, recursos);
            }

            _eventos?.Publicar(
                new VidaDoPersonagemMudou(
                    golpe.PersonagemId,
                    recursos.VidaAtual,
                    recursos.VidaMaxima
                )
            );

            // Vida 0 leva a caído; vida acima de 0 interrompe quem este personagem levantava.
            _queda?.AoDano(golpe.PersonagemId, recursos.VidaAtual);

            // O acerto só interrompe a ação do jogador se a guarda quebrou ou o golpe for especial (#795):
            // golpes comuns de criatura (TipoDeAtaqueDeInimigo.Golpe) não atordoam nem giram o jogador.
            var deveInterromper = quebrouGuarda || golpe.InterrompeAlvo;
            if (
                danoFinal > 0
                && recursos.VidaAtual > 0
                && deveInterromper
                && _acoes?.TentarReagir(golpe.PersonagemId, _relogioMs()) == true
            )
            {
                _avisos?.Enviar(
                    golpe.Area,
                    golpe.PersonagemId,
                    CombateMessageIds.AcaoInterrompida,
                    new AcaoInterrompida
                    {
                        AlvoId = golpe.PersonagemId,
                        Jogador = true,
                        DuracaoMs = RegrasReacao.AtordoamentoNoAcertoMs,
                    }
                );
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(
                e,
                "Erro ao aplicar o golpe do inimigo {Inimigo} no personagem {Personagem}.",
                golpe.InimigoId,
                golpe.PersonagemId
            );
        }
    }
}
