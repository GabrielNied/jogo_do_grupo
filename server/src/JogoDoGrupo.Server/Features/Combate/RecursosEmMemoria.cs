using System.Collections.Concurrent;
using System.Data;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Dono dos recursos (vida, vigor, energia) e dos atributos finais dos personagens em uso. Cada
/// golpe, esquiva e dano mexe só na memória, sob a trava do personagem (o vigor nunca é cobrado
/// duas vezes); o banco é lido uma vez, na primeira consulta do personagem, e recebe o que mudou a
/// cada <see cref="ConfigRecursos.IntervaloDeGravacaoSegundos"/>, quando o personagem sai do jogo
/// e quando o servidor para. Combate é o único que escreve em <see cref="RecursosPersonagem"/>.
/// <para>
/// Os atributos finais (Itens + efeitos + pontos) ficam em memória por
/// <see cref="ConfigRecursos.ValidadeDosAtributosSegundos"/>; a rota de recursos lê sempre do
/// zero e renova o que está guardado.
/// </para>
/// </summary>
public sealed class RecursosEmMemoria : IHostedService, IDisposable
{
    // Mesma espera pela trava do banco da rota de recursos (o client desiste em 10 s).
    private const int EsperaTravaSegundos = 5;

    private readonly IServiceScopeFactory _escopos;
    private readonly IBonusDeAtributos _bonus;
    private readonly IPontosDistribuidos _pontos;
    private readonly ServidorTempoReal _tempoReal;
    private readonly TimeProvider _relogio;
    private readonly ILogger<RecursosEmMemoria> _log;
    private readonly ConcurrentDictionary<long, Entrada> _entradas = new();
    private readonly SemaphoreSlim _gravacao = new(1, 1);
    private readonly CancellationTokenSource _parada = new();
    private Task _laco = Task.CompletedTask;

    public RecursosEmMemoria(
        IServiceScopeFactory escopos,
        IBonusDeAtributos bonus,
        IPontosDistribuidos pontos,
        ServidorTempoReal tempoReal,
        TimeProvider relogio,
        ILogger<RecursosEmMemoria> log
    )
    {
        _escopos = escopos;
        _bonus = bonus;
        _pontos = pontos;
        _tempoReal = tempoReal;
        _relogio = relogio;
        _log = log;
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    public Task StartAsync(CancellationToken cancelamento)
    {
        _laco = Task.Run(GravarPeriodicamenteAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            await _parada.CancelAsync();
            await _laco;
            await GravarTudoAsync();
        }
        catch (ObjectDisposedException) { }
    }

    public void Dispose() => _tempoReal.SessaoEncerrada -= AoEncerrar;

    /// <summary>Atributos finais do personagem; só vai a Itens e Personagem se o guardado venceu.</summary>
    public async Task<AtributosCombate> AtributosAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            return await AtributosDaEntradaAsync(entrada, cancelamento);
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// Recusa quem está caído ou não tem <paramref name="custo"/> de vigor; senão cobra o vigor.
    /// Só na memória: o custo 0 (golpe leve) não toca no banco.
    /// </summary>
    public async Task<InicioDoAtaque> TentarGastarVigorAsync(
        long personagemId,
        int custo,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            var atributos = await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada, atributos);

            if (recursos.Vida <= 0)
            {
                return new InicioDoAtaque(CodigosErroCombate.VoceEstaCaido, null, atributos.Forca);
            }

            if (recursos.Vigor < custo)
            {
                return new InicioDoAtaque(
                    CodigosErroCombate.VigorInsuficiente,
                    null,
                    atributos.Forca
                );
            }

            if (custo > 0)
            {
                var agora = Agora();
                recursos.Vigor -= custo;
                recursos.UltimoGastoVigorEm = agora;
                recursos.VigorContadoAte = agora;
                entrada.Sujo = true;
            }

            return new InicioDoAtaque(
                null,
                custo > 0 ? Estado(recursos, atributos) : null,
                atributos.Forca
            );
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// O golpe de <paramref name="dano"/> contra a guarda segurada: se o vigor paga
    /// <see cref="RegrasGuarda.CustoDoBloqueio"/>, ele é gasto e o golpe é bloqueado; senão o
    /// vigor vai a zero e a guarda quebra (<c>Bloqueou</c> falso). Uma ida só, sob a trava.
    /// </summary>
    public async Task<(bool Bloqueou, int VigorGasto, RecursosMudaram Recursos)> BloquearAsync(
        long personagemId,
        int dano,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            var atributos = await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada, atributos);
            var custo = RegrasGuarda.CustoDoBloqueio(dano);
            var bloqueou = RegrasGuarda.Segura(recursos.Vigor, dano);
            var gasto = bloqueou ? custo : recursos.Vigor;
            if (gasto > 0)
            {
                var agora = Agora();
                recursos.Vigor -= gasto;
                recursos.UltimoGastoVigorEm = agora;
                recursos.VigorContadoAte = agora;
                entrada.Sujo = true;
            }

            return (bloqueou, gasto, Estado(recursos, atributos));
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// A guarda subiu ou desceu: enquanto segurada, a espera da regeneração do vigor não corre
    /// (regras da fatia, seção 1). Regenera até agora antes de mudar, para não perder o que já
    /// era devido; ao soltar, a espera de 2 s recomeça.
    /// </summary>
    public async Task<RecursosMudaram> DefinirGuardaAsync(
        long personagemId,
        bool segurando,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            var atributos = await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada, atributos);
            entrada.Guardando = segurando;
            var agora = Agora();
            recursos.UltimoGastoVigorEm = agora;
            recursos.VigorContadoAte = agora;
            entrada.Sujo = true;
            return Estado(recursos, atributos);
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>Tira vida (nunca abaixo de 0); golpes simultâneos somam.</summary>
    public async Task<RecursosMudaram> TirarVidaAsync(
        long personagemId,
        int dano,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            var atributos = await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada, atributos);
            recursos.Vida = Math.Max(0, recursos.Vida - dano);
            entrada.Sujo = true;
            return Estado(recursos, atributos);
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>Regenera até agora e deixa <paramref name="mudar"/> alterar os recursos (queda).</summary>
    public async Task<RecursosMudaram> AtualizarAsync(
        long personagemId,
        Action<RecursosPersonagem, AtributosCombate> mudar,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            var atributos = await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada, atributos);
            mudar(recursos, atributos);
            var agora = Agora();
            recursos.VigorContadoAte = agora;
            recursos.EnergiaContadaAte = agora;
            entrada.Sujo = true;
            return Estado(recursos, atributos);
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// A consulta da rota: lê os atributos de novo (renovando os guardados), regenera e devolve o
    /// estado completo.
    /// </summary>
    public async Task<RecursosResponse> ConsultarAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            entrada.AtributosEm = null;
            var atributos = await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada, atributos);
            var estado = Estado(recursos, atributos);
            return new RecursosResponse
            {
                VidaAtual = estado.VidaAtual,
                VidaMaxima = estado.VidaMaxima,
                VigorAtual = estado.VigorAtual,
                VigorMaximo = estado.VigorMaximo,
                EnergiaAtual = estado.EnergiaAtual,
                EnergiaMaxima = estado.EnergiaMaxima,
                Atributos = atributos,
            };
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>Grava o que mudou de todos os personagens em memória.</summary>
    public async Task GravarTudoAsync()
    {
        foreach (var id in _entradas.Keys)
        {
            await GravarAsync(id);
        }
    }

    /// <summary>
    /// Grava o que mudou deste personagem. Falha de banco não perde a mudança: ela volta a valer
    /// para a próxima gravação.
    /// </summary>
    public async Task GravarAsync(long personagemId)
    {
        if (!_entradas.TryGetValue(personagemId, out var entrada))
        {
            return;
        }

        // Uma gravação por vez: duas cópias do mesmo personagem nunca chegam fora de ordem.
        await _gravacao.WaitAsync();
        try
        {
            RecursosPersonagem copia;
            await entrada.Trava.WaitAsync();
            try
            {
                if (!entrada.Sujo || entrada.Recursos == null)
                {
                    return;
                }

                copia = Copiar(entrada.Recursos);
                entrada.Sujo = false;
            }
            finally
            {
                entrada.Trava.Release();
            }

            try
            {
                using var escopo = _escopos.CreateScope();
                var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
                var linha = await db.Set<RecursosPersonagem>()
                    .FirstOrDefaultAsync(r => r.PersonagemId == personagemId);
                if (linha == null)
                {
                    db.Set<RecursosPersonagem>().Add(copia);
                }
                else
                {
                    Copiar(copia, linha);
                }

                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                entrada.Sujo = true;
                _log.LogError(
                    e,
                    "Erro ao gravar os recursos do personagem {Personagem}.",
                    personagemId
                );
            }
        }
        finally
        {
            _gravacao.Release();
        }
    }

    /// <summary>
    /// Esquece o personagem sem gravar: a próxima consulta lê o banco de novo. Para quem altera a
    /// linha por fora (testes).
    /// </summary>
    public void Descartar(long personagemId)
    {
        if (_entradas.TryRemove(personagemId, out var entrada))
        {
            entrada.Descartada = true;
        }
    }

    private DateTime Agora() => _relogio.GetUtcNow().UtcDateTime;

    private RecursosPersonagem Regenerado(Entrada entrada, AtributosCombate atributos)
    {
        var recursos = entrada.Recursos!;
        if (entrada.Guardando)
        {
            // Guarda segurada: a espera da regeneração do vigor fica parada.
            recursos.UltimoGastoVigorEm = Agora();
        }

        CalculadoraRecursos.Atualizar(recursos, atributos, Agora());
        return recursos;
    }

    private static RecursosMudaram Estado(RecursosPersonagem r, AtributosCombate a) =>
        new()
        {
            VidaAtual = r.Vida,
            VidaMaxima = RegrasRecursos.VidaMaxima(a.Vitalidade),
            VigorAtual = r.Vigor,
            VigorMaximo = RegrasRecursos.VigorMaximo(),
            EnergiaAtual = r.Energia,
            EnergiaMaxima = RegrasRecursos.EnergiaMaxima(a.Espirito),
        };

    // Com a trava da entrada na mão: atributos guardados, ou lidos de novo se venceram. Na
    // primeira vez do personagem, lê (ou cria) a linha do banco.
    private async Task<AtributosCombate> AtributosDaEntradaAsync(
        Entrada entrada,
        CancellationToken cancelamento
    )
    {
        var agora = _relogio.GetUtcNow();
        entrada.UltimoUso = agora;
        if (
            entrada.Recursos != null
            && entrada.Atributos != null
            && entrada.AtributosEm is { } em
            && (agora - em).TotalSeconds < ConfigRecursos.ValidadeDosAtributosSegundos
        )
        {
            return entrada.Atributos;
        }

        var lidos = CalculadoraRecursos.AtributosFinais(
            await _bonus.EquipamentosAsync(entrada.PersonagemId, cancelamento),
            await _bonus.EfeitosAsync(entrada.PersonagemId, cancelamento),
            await _pontos.ObterAsync(entrada.PersonagemId, cancelamento)
        );
        entrada.Recursos ??= await CarregarAsync(entrada.PersonagemId, lidos, cancelamento);
        entrada.Atributos = lidos;
        entrada.AtributosEm = agora;
        return lidos;
    }

    // Devolve a entrada com a trava tomada.
    private async Task<Entrada> EntrarAsync(long personagemId, CancellationToken cancelamento)
    {
        while (true)
        {
            var entrada = _entradas.GetOrAdd(personagemId, id => new Entrada(id));
            await entrada.Trava.WaitAsync(cancelamento);
            if (!entrada.Descartada)
            {
                return entrada;
            }

            entrada.Trava.Release();
        }
    }

    // Uma ida ao banco por personagem: na transação Serializable (BEGIN IMMEDIATE no SQLite),
    // como a rota sempre fez, porque cria a linha quando ela não existe.
    private async Task<RecursosPersonagem> CarregarAsync(
        long personagemId,
        AtributosCombate atributos,
        CancellationToken cancelamento
    )
    {
        using var escopo = _escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        var sqlite = db.Database.GetDbConnection() as SqliteConnection;
        var esperaAnterior = sqlite?.DefaultTimeout;
        if (sqlite != null)
        {
            sqlite.DefaultTimeout = EsperaTravaSegundos;
        }

        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancelamento
            );
            var linha = await db.Set<RecursosPersonagem>()
                .FirstOrDefaultAsync(r => r.PersonagemId == personagemId, cancelamento);
            if (linha == null)
            {
                linha = CalculadoraRecursos.Novo(personagemId, atributos, Agora());
                db.Set<RecursosPersonagem>().Add(linha);
                await db.SaveChangesAsync(cancelamento);
            }

            await transacao.CommitAsync(cancelamento);
            return Copiar(linha);
        }
        finally
        {
            if (sqlite != null && esperaAnterior != null)
            {
                sqlite.DefaultTimeout = esperaAnterior.Value;
            }
        }
    }

    private static RecursosPersonagem Copiar(RecursosPersonagem origem) =>
        Copiar(origem, new RecursosPersonagem { PersonagemId = origem.PersonagemId });

    private static RecursosPersonagem Copiar(RecursosPersonagem origem, RecursosPersonagem destino)
    {
        destino.Vida = origem.Vida;
        destino.Vigor = origem.Vigor;
        destino.Energia = origem.Energia;
        destino.UltimoGastoVigorEm = origem.UltimoGastoVigorEm;
        destino.UltimoGastoEnergiaEm = origem.UltimoGastoEnergiaEm;
        destino.VigorContadoAte = origem.VigorContadoAte;
        destino.EnergiaContadaAte = origem.EnergiaContadaAte;
        return destino;
    }

    // O personagem saiu do jogo de vez (não foi só troca de sessão): grava e libera a memória.
    private void AoEncerrar(SessaoTempoReal sessao)
    {
        var id = sessao.PersonagemId;
        if (_tempoReal.Sessoes.Any(s => s.PersonagemId == id && !ReferenceEquals(s, sessao)))
        {
            return;
        }

        _ = GravarEDescartarAsync(id, ocioso: false);
    }

    private async Task GravarEDescartarAsync(long personagemId, bool ocioso)
    {
        try
        {
            await GravarAsync(personagemId);
            if (!_entradas.TryGetValue(personagemId, out var entrada))
            {
                return;
            }

            await entrada.Trava.WaitAsync();
            try
            {
                var vencida =
                    (_relogio.GetUtcNow() - entrada.UltimoUso).TotalSeconds
                    >= ConfigRecursos.OciosoParaDescartarSegundos;
                if (!entrada.Sujo && (!ocioso || vencida))
                {
                    entrada.Descartada = true;
                    _entradas.TryRemove(personagemId, out _);
                }
            }
            finally
            {
                entrada.Trava.Release();
            }
        }
        catch (Exception e)
        {
            _log.LogError(
                e,
                "Erro ao liberar os recursos do personagem {Personagem}.",
                personagemId
            );
        }
    }

    private async Task GravarPeriodicamenteAsync()
    {
        using var temporizador = new PeriodicTimer(
            TimeSpan.FromSeconds(ConfigRecursos.IntervaloDeGravacaoSegundos)
        );
        try
        {
            while (await temporizador.WaitForNextTickAsync(_parada.Token))
            {
                foreach (var id in _entradas.Keys)
                {
                    await GravarEDescartarAsync(id, ocioso: true);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private sealed class Entrada(long personagemId)
    {
        public long PersonagemId { get; } = personagemId;
        public SemaphoreSlim Trava { get; } = new(1, 1);
        public RecursosPersonagem? Recursos;
        public AtributosCombate? Atributos;
        public DateTimeOffset? AtributosEm;
        public DateTimeOffset UltimoUso = DateTimeOffset.UnixEpoch;
        public bool Sujo;
        public bool Guardando;
        public bool Descartada;
    }
}
