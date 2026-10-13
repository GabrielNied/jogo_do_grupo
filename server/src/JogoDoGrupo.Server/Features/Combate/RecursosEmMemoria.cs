using System.Collections.Concurrent;
using System.Data;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Dono dos recursos (vida, stamina, mana) e dos atributos finais dos personagens em uso. Cada
/// golpe, esquiva e dano mexe só na memória, sob a trava do personagem (a stamina nunca é cobrado
/// duas vezes); o banco é lido uma vez, na primeira consulta do personagem, e recebe o que mudou a
/// cada <see cref="ConfigRecursos.IntervaloDeGravacaoSegundos"/>, quando o personagem sai do jogo
/// e quando o servidor para. Combate é o único que escreve em <see cref="RecursosPersonagem"/>.
/// <para>
/// Os atributos finais (efeitos + pontos; o equipamento é fonte à parte) ficam em memória por
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
    private readonly IAtributosDosEquipamentos _equipamentos;
    private readonly IAtributosDaArvore _pontos;
    private readonly IReadOnlyList<IFonteDeModificadores> _fontes;
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
        IAtributosDosEquipamentos equipamentos,
        IAtributosDaArvore pontos,
        IEnumerable<IFonteDeModificadores> fontes,
        ServidorTempoReal tempoReal,
        TimeProvider relogio,
        ILogger<RecursosEmMemoria> log
    )
    {
        _escopos = escopos;
        _bonus = bonus;
        _equipamentos = equipamentos;
        _pontos = pontos;
        _fontes = fontes.OrderBy(f => f.Fonte).ToList();
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

    /// <summary>Os nove secundários do personagem (valores finais), pela pipeline única.</summary>
    public async Task<AtributosSecundarios> SecundariosAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            await AtributosDaEntradaAsync(entrada, cancelamento);
            return entrada.Valores!;
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// Recusa quem está caído ou não tem <paramref name="custo"/> de stamina; senão cobra a stamina.
    /// Só na memória: o custo 0 (golpe leve) não toca no banco.
    /// </summary>
    public async Task<InicioDoAtaque> TentarGastarStaminaAsync(
        long personagemId,
        int custo,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada);

            if (recursos.Vida <= 0)
            {
                return new InicioDoAtaque(CodigosErroCombate.VoceEstaCaido, null, entrada.Dano!);
            }

            if (recursos.Stamina < custo)
            {
                return new InicioDoAtaque(
                    CodigosErroCombate.StaminaInsuficiente,
                    null,
                    entrada.Dano!
                );
            }

            if (custo > 0)
            {
                var agora = Agora();
                recursos.Stamina -= custo;
                recursos.UltimoGastoStaminaEm = agora;
                recursos.StaminaContadaAte = agora;
                entrada.Sujo = true;
            }

            return new InicioDoAtaque(
                null,
                custo > 0 ? Estado(recursos, entrada.Maximos, entrada.Valores!) : null,
                entrada.Dano!
            );
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// O golpe de <paramref name="dano"/> contra a guarda segurada: se a stamina paga
    /// <see cref="RegrasGuarda.CustoDoBloqueio"/>, ele é gasto e o golpe é bloqueado; senão o
    /// stamina vai a zero e a guarda quebra (<c>Bloqueou</c> falso). Uma ida só, sob a trava.
    /// </summary>
    public async Task<(bool Bloqueou, int StaminaGasto, RecursosMudaram Recursos)> BloquearAsync(
        long personagemId,
        int dano,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada);
            var custo = RegrasGuarda.CustoDoBloqueio(dano);
            var bloqueou = RegrasGuarda.Segura(recursos.Stamina, dano);
            var gasto = bloqueou ? custo : recursos.Stamina;
            if (gasto > 0)
            {
                var agora = Agora();
                recursos.Stamina -= gasto;
                recursos.UltimoGastoStaminaEm = agora;
                recursos.StaminaContadaAte = agora;
                entrada.Sujo = true;
            }

            return (bloqueou, gasto, Estado(recursos, entrada.Maximos, entrada.Valores!));
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>
    /// A guarda subiu ou desceu: enquanto segurada, a espera da regeneração da stamina não corre
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
            await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada);
            entrada.Guardando = segurando;
            var agora = Agora();
            recursos.UltimoGastoStaminaEm = agora;
            recursos.StaminaContadaAte = agora;
            entrada.Sujo = true;
            return Estado(recursos, entrada.Maximos, entrada.Valores!);
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
            await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada);
            recursos.Vida = Math.Max(0, recursos.Vida - dano);
            entrada.Sujo = true;
            return Estado(recursos, entrada.Maximos, entrada.Valores!);
        }
        finally
        {
            entrada.Trava.Release();
        }
    }

    /// <summary>Regenera até agora e deixa <paramref name="mudar"/> alterar os recursos (queda).</summary>
    public async Task<RecursosMudaram> AtualizarAsync(
        long personagemId,
        Action<RecursosPersonagem, MaximosDeRecursos> mudar,
        CancellationToken cancelamento
    )
    {
        var entrada = await EntrarAsync(personagemId, cancelamento);
        try
        {
            await AtributosDaEntradaAsync(entrada, cancelamento);
            var recursos = Regenerado(entrada);
            mudar(recursos, entrada.Maximos);
            var agora = Agora();
            recursos.StaminaContadaAte = agora;
            recursos.ManaContadaAte = agora;
            entrada.VidaContadaAte = agora;
            entrada.Sujo = true;
            return Estado(recursos, entrada.Maximos, entrada.Valores!);
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
            var recursos = Regenerado(entrada);
            var estado = Estado(recursos, entrada.Maximos, entrada.Valores!);
            return new RecursosResponse
            {
                VidaAtual = estado.VidaAtual,
                VidaMaxima = estado.VidaMaxima,
                StaminaAtual = estado.StaminaAtual,
                StaminaMaxima = estado.StaminaMaxima,
                ManaAtual = estado.ManaAtual,
                ManaMaxima = estado.ManaMaxima,
                Atributos = atributos,
                Secundarios = entrada.Secundarios!,
                DoEquipamento = entrada.LinhasDoEquipamento,
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

    private RecursosPersonagem Regenerado(Entrada entrada)
    {
        var recursos = entrada.Recursos!;
        if (entrada.Guardando)
        {
            // Guarda segurada: a espera da regeneração da stamina fica parada.
            recursos.UltimoGastoStaminaEm = Agora();
        }

        CalculadoraRecursos.Atualizar(
            recursos,
            entrada.Maximos,
            entrada.Valores!,
            ref entrada.VidaContadaAte,
            Agora()
        );
        return recursos;
    }

    private static RecursosMudaram Estado(
        RecursosPersonagem r,
        MaximosDeRecursos maximos,
        AtributosSecundarios secundarios
    ) =>
        new()
        {
            VidaAtual = r.Vida,
            VidaMaxima = maximos.Vida,
            StaminaAtual = r.Stamina,
            StaminaMaxima = maximos.Stamina,
            ManaAtual = r.Mana,
            ManaMaxima = maximos.Mana,
            RegeneracaoStamina = secundarios.RegeneracaoStamina,
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
            await _bonus.EfeitosAsync(entrada.PersonagemId, cancelamento),
            await _pontos.ObterAsync(entrada.PersonagemId, cancelamento)
        );
        // O equipamento é uma fonte à parte: nunca soma nos cinco atributos do personagem.
        var equipamento = await _equipamentos.ObterAsync(entrada.PersonagemId, cancelamento);
        var maximos = CalculadoraRecursos.Maximos(lidos, equipamento);
        var secundarios = RegrasSecundarios.Calcular(
            await ModificadoresAsync(entrada.PersonagemId, cancelamento)
        );
        if (entrada.Recursos == null)
        {
            entrada.Recursos = await CarregarAsync(entrada.PersonagemId, maximos, cancelamento);
            entrada.VidaContadaAte = Agora();
        }

        entrada.Maximos = maximos;
        entrada.Dano = CalculadoraRecursos.ParcelasDoDano(lidos, equipamento);
        entrada.LinhasDoEquipamento = equipamento.Linhas.Select(l => l.Texto).ToArray();
        entrada.Atributos = lidos;
        entrada.Secundarios = secundarios;
        entrada.Valores = RegrasSecundarios.Valores(secundarios);
        entrada.AtributosEm = agora;
        return lidos;
    }

    // Junta o que as fontes publicam. Quem declara outra fonte que não a sua, ou a Base, é
    // recusado e registrado; falha de uma fonte não derruba a conta (o jogador fica sem aquela
    // fonte até a próxima leitura).
    private async Task<List<Modificador>> ModificadoresAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var todos = new List<Modificador>();
        foreach (var fonte in _fontes)
        {
            try
            {
                foreach (var m in await fonte.ObterAsync(personagemId, cancelamento))
                {
                    if (m.Fonte != fonte.Fonte || m.Fonte == FonteDeModificador.Base)
                    {
                        _log.LogWarning(
                            "Modificador {Origem} recusado: a fonte {Fonte} declarou {Declarada}.",
                            m.Origem,
                            fonte.Fonte,
                            m.Fonte
                        );
                        continue;
                    }

                    if (!PipelineDeModificadores.EhValido(m))
                    {
                        _log.LogWarning(
                            "Modificador {Origem} recusado: sem grupo de acúmulo ou valor inválido.",
                            m.Origem
                        );
                        continue;
                    }

                    todos.Add(m);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _log.LogError(
                    e,
                    "Erro ao ler os modificadores da fonte {Fonte} do personagem {Personagem}.",
                    fonte.Fonte,
                    personagemId
                );
            }
        }

        return todos;
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
        MaximosDeRecursos maximos,
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
                linha = CalculadoraRecursos.Novo(personagemId, maximos, Agora());
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
        destino.Stamina = origem.Stamina;
        destino.Mana = origem.Mana;
        destino.UltimoGastoStaminaEm = origem.UltimoGastoStaminaEm;
        destino.UltimoGastoManaEm = origem.UltimoGastoManaEm;
        destino.StaminaContadaAte = origem.StaminaContadaAte;
        destino.ManaContadaAte = origem.ManaContadaAte;
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
        public MaximosDeRecursos Maximos;
        public ParcelasDeDano? Dano;
        public string[] LinhasDoEquipamento = [];
        public SecundarioCalculado[]? Secundarios;
        public AtributosSecundarios? Valores;

        // Só em memória (sem migração): até onde a regeneração de vida já foi paga.
        public DateTime VidaContadaAte;
        public DateTimeOffset? AtributosEm;
        public DateTimeOffset UltimoUso = DateTimeOffset.UnixEpoch;
        public bool Sujo;
        public bool Guardando;
        public bool Descartada;
    }
}
