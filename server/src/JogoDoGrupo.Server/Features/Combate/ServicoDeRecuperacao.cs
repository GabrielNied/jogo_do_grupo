using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Combate;

public sealed record BuffRefeicao(
    int ModeloId,
    string Nome,
    double BonusRegenVida,
    long ExpiraEmMs
);

/// <summary>
/// Responsável pela recuperação do personagem fora de combate, consumo de poções com recarga
/// compartilhada, buffs de refeição e aceleração por descanso/sentar (#347).
/// </summary>
public sealed class ServicoDeRecuperacao : IFonteDeModificadores, IDisposable
{
    private readonly ServidorTempoReal? _tempoReal;
    private readonly ServicoDeEstadoDeCombate _estadoDeCombate;
    private readonly Func<RecursosEmMemoria> _obterRecursos;
    private readonly IServiceScopeFactory _escopos;
    private readonly IPosicoesDoMundo? _posicoes;
    private readonly AvisosNaArea? _avisos;
    private readonly EventosDoServidor? _eventos;
    private readonly Func<long> _relogioMs;
    private readonly ILogger<ServicoDeRecuperacao> _log;

    private readonly ConcurrentDictionary<long, long> _recargaPocaoAteMs = new();
    private readonly ConcurrentDictionary<long, BuffRefeicao> _refeicoes = new();
    private readonly ConcurrentDictionary<long, bool> _descansando = new();

    public FonteDeModificador Fonte => FonteDeModificador.Efeito;

    public ServicoDeRecuperacao(
        ServidorTempoReal? tempoReal,
        ServicoDeEstadoDeCombate estadoDeCombate,
        Func<RecursosEmMemoria> obterRecursos,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeRecuperacao> log,
        IPosicoesDoMundo? posicoes = null,
        IVizinhanca? vizinhanca = null,
        EventosDoServidor? eventos = null,
        Func<long>? relogioMs = null
    )
    {
        _tempoReal = tempoReal;
        _estadoDeCombate = estadoDeCombate;
        _obterRecursos = obterRecursos;
        _escopos = escopos;
        _log = log;
        _posicoes = posicoes;
        _avisos =
            (posicoes != null && tempoReal != null)
                ? new AvisosNaArea(tempoReal, posicoes, vizinhanca)
                : null;
        _eventos = eventos;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);

        _estadoDeCombate.DefinirCallbackEntrada(AoEntrarEmCombate);
        if (_tempoReal != null)
        {
            _tempoReal.SessaoEncerrada += AoEncerrar;
        }
    }

    public void Dispose()
    {
        if (_tempoReal != null)
        {
            _tempoReal.SessaoEncerrada -= AoEncerrar;
        }
    }

    public Task<IReadOnlyList<Modificador>> ObterAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var mods = new List<Modificador>();
        var emCombate = _estadoDeCombate.EmCombate(personagemId);
        var descansando = _descansando.TryGetValue(personagemId, out var d) && d;
        var agoraMs = _relogioMs();

        // 1. Refeição ativa (Food buff)
        if (_refeicoes.TryGetValue(personagemId, out var buff) && buff.ExpiraEmMs > agoraMs)
        {
            mods.Add(
                new Modificador
                {
                    Atributo = AtributoSecundario.RegeneracaoVida,
                    Fonte = FonteDeModificador.Efeito,
                    Origem = buff.Nome,
                    Operacao = OperacaoDeModificador.Soma,
                    Ordem = ConfigAtributosSecundarios.OrdemEfeito,
                    Grupo = "refeicao_vida",
                    Valor = buff.BonusRegenVida,
                }
            );
        }

        // 2. Descanso (sentado / local seguro): só acelera fora de combate
        if (descansando && !emCombate)
        {
            mods.Add(
                new Modificador
                {
                    Atributo = AtributoSecundario.RegeneracaoVida,
                    Fonte = FonteDeModificador.Efeito,
                    Origem = "Descanso",
                    Operacao = OperacaoDeModificador.Soma,
                    Ordem = ConfigAtributosSecundarios.OrdemEfeito,
                    Grupo = "descanso_vida_base",
                    Valor = ConfigRecuperacao.RegeneracaoVidaForaDeCombate,
                }
            );

            mods.Add(
                new Modificador
                {
                    Atributo = AtributoSecundario.RegeneracaoStamina,
                    Fonte = FonteDeModificador.Efeito,
                    Origem = "Descanso",
                    Operacao = OperacaoDeModificador.Multiplica,
                    Ordem = ConfigAtributosSecundarios.OrdemEfeito,
                    Grupo = "descanso_stamina",
                    Valor = ConfigRecuperacao.MultiplicadorDescanso - 1.0,
                }
            );

            mods.Add(
                new Modificador
                {
                    Atributo = AtributoSecundario.RegeneracaoMana,
                    Fonte = FonteDeModificador.Efeito,
                    Origem = "Descanso",
                    Operacao = OperacaoDeModificador.Multiplica,
                    Ordem = ConfigAtributosSecundarios.OrdemEfeito,
                    Grupo = "descanso_mana",
                    Valor = ConfigRecuperacao.MultiplicadorDescanso - 1.0,
                }
            );
        }

        return Task.FromResult<IReadOnlyList<Modificador>>(mods);
    }

    public void AoConsumirPocao(SessaoTempoReal sessao, ConsumirPocao pedido) =>
        _ = ConsumirPocaoAsync(sessao, pedido);

    public void AoConsumirRefeicao(SessaoTempoReal sessao, ConsumirRefeicao pedido) =>
        _ = ConsumirRefeicaoAsync(sessao, pedido);

    public void AoDescansar(SessaoTempoReal sessao, IntencaoDeDescanso pedido)
    {
        var personagemId = sessao.PersonagemId;
        if (pedido.Descansando)
        {
            if (_estadoDeCombate.EmCombate(personagemId))
            {
                Recusar(sessao, CodigosErroCombate.AcaoBloqueadaEmCombate);
                return;
            }

            _descansando[personagemId] = true;
        }
        else
        {
            _descansando.TryRemove(personagemId, out _);
        }

        _obterRecursos?.Invoke()?.AoEquipamentoMudar(personagemId);
        AvisarDescanso(personagemId, pedido.Descansando);
    }

    public void AoConsultarEstado(SessaoTempoReal sessao)
    {
        var personagemId = sessao.PersonagemId;
        var emCombate = _estadoDeCombate.EmCombate(personagemId);
        var segCombate = _estadoDeCombate.SegundosRestantes(personagemId);
        var agoraMs = _relogioMs();

        var recargaPocao = 0;
        if (
            _recargaPocaoAteMs.TryGetValue(personagemId, out var pocaoAteMs)
            && pocaoAteMs > agoraMs
        )
        {
            recargaPocao = (int)Math.Ceiling((pocaoAteMs - agoraMs) / 1000.0);
        }

        var comidaNome = "";
        var comidaDuracao = 0;
        var comidaBonus = 0.0;
        if (_refeicoes.TryGetValue(personagemId, out var buff) && buff.ExpiraEmMs > agoraMs)
        {
            comidaNome = buff.Nome;
            comidaDuracao = (int)Math.Ceiling((buff.ExpiraEmMs - agoraMs) / 1000.0);
            comidaBonus = buff.BonusRegenVida;
        }

        var descansando = _descansando.TryGetValue(personagemId, out var d) && d && !emCombate;

        _tempoReal?.Enviar(
            sessao,
            CombateMessageIds.EstadoDeRecuperacao,
            new EstadoDeRecuperacao
            {
                EmCombate = emCombate,
                SegundosAteSairDeCombate = segCombate,
                RecargaPocaoRestanteSegundos = recargaPocao,
                ComidaNome = comidaNome,
                ComidaDuracaoRestanteSegundos = comidaDuracao,
                ComidaBonusRegenVida = comidaBonus,
                Descansando = descansando,
            }
        );
    }

    private async Task ConsumirPocaoAsync(SessaoTempoReal sessao, ConsumirPocao pedido)
    {
        try
        {
            var personagemId = sessao.PersonagemId;
            var agoraMs = _relogioMs();

            if (_recargaPocaoAteMs.TryGetValue(personagemId, out var ateMs) && agoraMs < ateMs)
            {
                Recusar(sessao, CodigosErroCombate.PocaoEmRecarga);
                return;
            }

            var recursos = await _obterRecursos()
                .ConsultarAsync(personagemId, CancellationToken.None);
            if (recursos.VidaAtual >= recursos.VidaMaxima)
            {
                Recusar(sessao, CodigosErroCombate.VidaCheia);
                return;
            }

            await using var escopo = _escopos.CreateAsyncScope();
            var consumo = escopo.ServiceProvider.GetRequiredService<IConsumoDeItens>();
            var consumido = await consumo.ConsumirConsumivelAsync(
                personagemId,
                pedido.InstanciaId,
                CatalogoItens.PocaoDeTeste,
                CancellationToken.None
            );

            if (consumido == null)
            {
                Recusar(sessao, CodigosErroCombate.SemPocao);
                return;
            }

            _recargaPocaoAteMs[personagemId] =
                agoraMs + (ConfigRecuperacao.RecargaPocaoSegundos * 1000L);

            var cura =
                ConfigRecuperacao.CuraFixaPocao
                + (int)Math.Round(recursos.VidaMaxima * ConfigRecuperacao.CuraPercentualPocao);

            var mudaram = await _obterRecursos()
                .CurarVidaAsync(personagemId, cura, CancellationToken.None);

            _tempoReal?.Enviar(
                sessao,
                CombateMessageIds.PocaoConsumida,
                new PocaoConsumida
                {
                    VidaCurada = cura,
                    RecargaSegundos = ConfigRecuperacao.RecargaPocaoSegundos,
                }
            );

            _tempoReal?.Enviar(sessao, CombateMessageIds.RecursosMudaram, mudaram);

            _eventos?.Publicar(
                new VidaDoPersonagemMudou(personagemId, mudaram.VidaAtual, mudaram.VidaMaxima)
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Erro ao consumir poção do personagem {Personagem}.",
                sessao.PersonagemId
            );
        }
    }

    private async Task ConsumirRefeicaoAsync(SessaoTempoReal sessao, ConsumirRefeicao pedido)
    {
        try
        {
            var personagemId = sessao.PersonagemId;
            if (_estadoDeCombate.EmCombate(personagemId))
            {
                Recusar(sessao, CodigosErroCombate.AcaoBloqueadaEmCombate);
                return;
            }

            if (
                pedido.ModeloId != ConfigRecuperacao.ModeloCaldoDosErrantes
                && pedido.ModeloId != ConfigRecuperacao.ModeloAssadoDeGalhada
                && pedido.ModeloId != ConfigRecuperacao.ModeloEnsopadoDaFornalha
            )
            {
                Recusar(sessao, CodigosErroCombate.SemRefeicao);
                return;
            }

            await using var escopo = _escopos.CreateAsyncScope();
            var consumo = escopo.ServiceProvider.GetRequiredService<IConsumoDeItens>();
            var consumido = await consumo.ConsumirConsumivelAsync(
                personagemId,
                0,
                pedido.ModeloId,
                CancellationToken.None
            );

            if (consumido == null)
            {
                Recusar(sessao, CodigosErroCombate.SemRefeicao);
                return;
            }

            var duracao = ConfigRecuperacao.DuracaoRefeicao(pedido.ModeloId);
            var bonus = ConfigRecuperacao.BonusRegenVida(pedido.ModeloId);
            var nome = ConfigRecuperacao.NomeRefeicao(pedido.ModeloId);
            var agoraMs = _relogioMs();

            _refeicoes[personagemId] = new BuffRefeicao(
                pedido.ModeloId,
                nome,
                bonus,
                agoraMs + (duracao * 1000L)
            );

            _obterRecursos?.Invoke()?.AoEquipamentoMudar(personagemId);

            _tempoReal?.Enviar(
                sessao,
                CombateMessageIds.RefeicaoConsumida,
                new RefeicaoConsumida
                {
                    ModeloId = pedido.ModeloId,
                    Nome = nome,
                    DuracaoSegundos = duracao,
                    BonusRegenVida = bonus,
                }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Erro ao consumir refeição do personagem {Personagem}.",
                sessao.PersonagemId
            );
        }
    }

    private void AoEntrarEmCombate(long personagemId)
    {
        if (_descansando.TryRemove(personagemId, out _))
        {
            AvisarDescanso(personagemId, false);
            _obterRecursos?.Invoke()?.AoEquipamentoMudar(personagemId);
        }
    }

    private void AvisarDescanso(long personagemId, bool descansando)
    {
        var msg = new DescansoMudou { PersonagemId = personagemId, Descansando = descansando };
        if (
            _posicoes != null
            && _posicoes.TentarObter(personagemId, out var pos)
            && _avisos != null
        )
        {
            _avisos.Enviar(
                pos.Area,
                personagemId,
                CombateMessageIds.DescansoMudou,
                msg,
                incluirDono: true
            );
        }
        else if (
            _tempoReal != null
            && _tempoReal.TentarObterSessao(personagemId, out var sessao)
            && sessao != null
        )
        {
            _tempoReal.Enviar(sessao, CombateMessageIds.DescansoMudou, msg);
        }
    }

    private void Recusar(SessaoTempoReal sessao, string codigo)
    {
        _tempoReal?.Enviar(
            sessao,
            CombateMessageIds.RecuperacaoRecusada,
            new RecuperacaoRecusada { Codigo = codigo }
        );
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        _descansando.TryRemove(sessao.PersonagemId, out _);
    }
}
