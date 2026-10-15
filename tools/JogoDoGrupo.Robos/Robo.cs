using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JogoDoGrupo.Shared.Core.Contas;
using JogoDoGrupo.Shared.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Robos;

/// <summary>
/// Um jogador falso: conta e personagem pela API, entra na cidade pelo tempo real e anda em
/// círculo perto do nascimento atacando de tempos em tempos, como o client faria.
/// </summary>
public sealed class Robo : IAsyncDisposable
{
    public const string Senha = "senha-dos-robos";

    /// <summary>Prazo de cada resposta esperada (conexão, autenticação, estado, ataque).</summary>
    public static readonly TimeSpan Espera = TimeSpan.FromSeconds(10);

    /// <summary>Raio do círculo que o robô anda em volta de onde nasceu.</summary>
    private const float Raio = 2.5f;

    /// <summary>Fração da velocidade de andar: folga para a validação de movimento do servidor.</summary>
    private const float FracaoDaVelocidade = 0.8f;

    private readonly int _numero;
    private readonly string _usuario;
    private readonly OpcoesDeCarga _opcoes;
    private readonly ColetorDeMetricas _metricas;
    private readonly ConexaoDoRobo _conexao = new();
    private readonly object _trava = new();
    private readonly TaskCompletionSource<ResultadoAutenticacaoTempoReal> _autenticado = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly TaskCompletionSource<EstadoDaCidade> _naCidade = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private long _personagemId;
    private float _andar = RegrasVelocidade.AndarPadrao;
    private float _centroX;
    private float _centroY;
    private float _centroZ;
    private double _angulo;
    private float _x;
    private float _z;
    private int _sequenciaMovimento;
    private int _sequenciaAtaque;
    private long _ataquePendenteDesde;
    private long _paradoAte;
    private volatile bool _saindo;

    public Robo(int numero, string usuario, OpcoesDeCarga opcoes, ColetorDeMetricas metricas)
    {
        _numero = numero;
        _usuario = usuario;
        _opcoes = opcoes;
        _metricas = metricas;
        _conexao.AoReceber = Receber;
        // Cada robô começa num ponto diferente do círculo.
        _angulo = numero * 2.399963; // ângulo áureo, espalha sem repetir
    }

    public bool NoMundo { get; private set; }

    /// <summary>Registra (ou reaproveita) a conta, loga e garante um personagem. Devolve o token.</summary>
    public async Task<string> PrepararContaAsync(HttpClient http, CancellationToken parada)
    {
        var registro = await http.PostAsJsonAsync(
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = _usuario, Senha = Senha },
            ConexaoDoRobo.Json,
            parada
        );
        // 409: a conta já existe de uma carga anterior; reaproveita.
        if (!registro.IsSuccessStatusCode && registro.StatusCode != HttpStatusCode.Conflict)
        {
            throw new FalhaDoRobo(
                TipoDeErro.Api,
                $"registrar {_usuario}: {(int)registro.StatusCode}"
            );
        }

        var login = await http.PostAsJsonAsync(
            RotasContas.Login,
            new LoginRequest { Usuario = _usuario, Senha = Senha },
            ConexaoDoRobo.Json,
            parada
        );
        if (!login.IsSuccessStatusCode)
        {
            throw new FalhaDoRobo(TipoDeErro.Api, $"login {_usuario}: {(int)login.StatusCode}");
        }

        var token = (
            await login.Content.ReadFromJsonAsync<LoginResponse>(ConexaoDoRobo.Json, parada)
        )!.Token;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var lista = await http.GetFromJsonAsync<ListaPersonagensResponse>(
            RotasPersonagem.Personagens,
            ConexaoDoRobo.Json,
            parada
        );
        if (lista?.Personagens is { Length: > 0 } existentes)
        {
            _personagemId = existentes[0].Id;
            return token;
        }

        var criado = await http.PostAsJsonAsync(
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = NomeDePersonagem(), Raca = Raca.Valdren },
            ConexaoDoRobo.Json,
            parada
        );
        if (!criado.IsSuccessStatusCode)
        {
            throw new FalhaDoRobo(
                TipoDeErro.Api,
                $"criar personagem de {_usuario}: {(int)criado.StatusCode}"
            );
        }

        _personagemId = (
            await criado.Content.ReadFromJsonAsync<PersonagemResumo>(ConexaoDoRobo.Json, parada)
        )!.Id;
        return token;
    }

    /// <summary>Conecta, autentica e entra na cidade.</summary>
    public async Task EntrarAsync(string token)
    {
        try
        {
            await _conexao.ConectarAsync(
                _opcoes.HostTempoReal,
                _opcoes.PortaTempoReal,
                _opcoes.ChaveConexao,
                Espera
            );
        }
        catch (Exception e) when (e is TimeoutException or InvalidOperationException)
        {
            throw new FalhaDoRobo(TipoDeErro.Conexao, $"{_usuario}: {e.Message}");
        }

        Enviar(
            TempoRealIds.AutenticarTempoReal,
            new AutenticarTempoReal { Token = token, PersonagemId = _personagemId }
        );
        var resultado = await Esperar(_autenticado.Task, TipoDeErro.Conexao, "autenticação");
        if (!resultado.Aceita)
        {
            throw new FalhaDoRobo(
                TipoDeErro.Conexao,
                $"{_usuario}: autenticação recusada ({resultado.Codigo})"
            );
        }

        Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estado = await Esperar(_naCidade.Task, TipoDeErro.EntradaNaCidade, "EstadoDaCidade");

        lock (_trava)
        {
            _centroX = estado.Eu.X;
            _centroY = estado.Eu.Y;
            _centroZ = estado.Eu.Z;
            _x = _centroX;
            _z = _centroZ;
        }

        NoMundo = true;
        _ = _conexao.Desconectado.ContinueWith(
            t =>
            {
                if (!_saindo)
                {
                    _metricas.Erro(TipoDeErro.Queda, $"{_usuario}: {t.Result.Reason}");
                }
            },
            TaskScheduler.Default
        );
    }

    /// <summary>Anda e ataca até <paramref name="parada"/>.</summary>
    public async Task AgirAsync(CancellationToken parada)
    {
        var intervalo = TimeSpan.FromSeconds(1.0 / _opcoes.MovimentosPorSegundo);
        using var relogio = new PeriodicTimer(intervalo);
        // Robôs não atacam todos no mesmo instante.
        var proximoAtaque =
            Stopwatch.GetTimestamp()
            + (long)(Stopwatch.Frequency * (_numero % 10) / 10.0 * _opcoes.AtaqueACadaMs / 1000);
        var ultimo = Stopwatch.GetTimestamp();
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                var agora = Stopwatch.GetTimestamp();
                var decorrido = Stopwatch.GetElapsedTime(ultimo, agora).TotalSeconds;
                ultimo = agora;
                if (agora >= proximoAtaque)
                {
                    Atacar(agora);
                    proximoAtaque = agora + Stopwatch.Frequency * _opcoes.AtaqueACadaMs / 1000;
                }

                Mover(agora, decorrido);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void Mover(long agora, double decorrido)
    {
        IntencaoDeMovimento intencao;
        lock (_trava)
        {
            // Parado durante o golpe: o servidor limita a velocidade enquanto ele dura.
            if (agora >= _paradoAte)
            {
                var velocidade = _andar * FracaoDaVelocidade;
                _angulo += velocidade * decorrido / Raio;
                _x = _centroX + Raio * (float)Math.Cos(_angulo);
                _z = _centroZ + Raio * (float)Math.Sin(_angulo);
            }

            intencao = new IntencaoDeMovimento
            {
                X = _x,
                Y = _centroY,
                Z = _z,
                RotacaoY = (float)(_angulo * 180 / Math.PI),
                Sequencia = ++_sequenciaMovimento,
            };
        }

        Enviar(MundoMessageIds.IntencaoDeMovimento, intencao);
    }

    private void Atacar(long agora)
    {
        IntencaoDeAtaque intencao;
        lock (_trava)
        {
            if (_ataquePendenteDesde != 0)
            {
                _metricas.Erro(
                    TipoDeErro.AtaqueSemResposta,
                    $"{_usuario}: ataque {_sequenciaAtaque} sem resposta"
                );
            }

            _ataquePendenteDesde = agora;
            var duracao = RegrasAtaque.PreparoLeveSegundos + RegrasAtaque.RecuperacaoLeveSegundos;
            _paradoAte = agora + (long)(Stopwatch.Frequency * duracao);
            intencao = new IntencaoDeAtaque
            {
                Tipo = TipoDeAtaque.Leve,
                DirecaoY = (float)(_angulo * 180 / Math.PI),
                Sequencia = ++_sequenciaAtaque,
            };
        }

        Enviar(CombateMessageIds.IntencaoDeAtaque, intencao);
    }

    private void Enviar<T>(ushort id, T mensagem)
    {
        _conexao.Enviar(id, mensagem);
        _metricas.Enviou();
    }

    // Thread da LiteNetLib.
    private void Receber(ushort id, byte[] pacote)
    {
        _metricas.Recebeu(pacote.Length);
        switch (id)
        {
            case TempoRealIds.ResultadoAutenticacaoTempoReal:
                if (Ler<ResultadoAutenticacaoTempoReal>(id, pacote) is { } resultado)
                {
                    _autenticado.TrySetResult(resultado);
                }

                break;
            case MundoMessageIds.EstadoDaCidade:
                if (Ler<EstadoDaCidade>(id, pacote) is { } estado)
                {
                    _naCidade.TrySetResult(estado);
                }

                break;
            case MundoMessageIds.PosicaoJogador:
                if (Ler<PosicaoJogador>(id, pacote) is { } posicao)
                {
                    if (posicao.PersonagemId != _personagemId)
                    {
                        _metricas.RecebeuReplica();
                    }
                }

                break;
            case MundoMessageIds.VelocidadesDeMovimento:
                if (Ler<VelocidadesDeMovimento>(id, pacote) is { } velocidades)
                {
                    lock (_trava)
                    {
                        _andar = velocidades.Andar;
                    }

                    Enviar(
                        MundoMessageIds.ConfirmarVelocidade,
                        new ConfirmarVelocidade { Sequencia = velocidades.Sequencia }
                    );
                }

                break;
            case MundoMessageIds.PosicaoForcada:
                if (Ler<PosicaoForcada>(id, pacote) is { } forcada)
                {
                    _metricas.Erro(
                        TipoDeErro.PosicaoForcada,
                        $"{_usuario}: servidor corrigiu a posição"
                    );
                    lock (_trava)
                    {
                        _centroX = _x = forcada.X;
                        _centroY = forcada.Y;
                        _centroZ = _z = forcada.Z;
                        _angulo = 0;
                        _centroX -= Raio;
                    }

                    Enviar(
                        MundoMessageIds.ConfirmarPosicaoForcada,
                        new ConfirmarPosicaoForcada { Sequencia = forcada.Sequencia }
                    );
                }

                break;
            case CombateMessageIds.AtaqueIniciado:
                if (
                    Ler<AtaqueIniciado>(id, pacote) is { } iniciado
                    && iniciado.AtacanteId == _personagemId
                )
                {
                    ConcluirAtaque(recusado: false);
                }

                break;
            case CombateMessageIds.AtaqueRecusado:
                if (Ler<AtaqueRecusado>(id, pacote) is not null)
                {
                    ConcluirAtaque(recusado: true);
                }

                break;
            case 0 when pacote.Length < 2:
                _metricas.Erro(TipoDeErro.PacoteInvalido, $"{_usuario}: pacote sem cabeçalho");
                break;
        }
    }

    private void ConcluirAtaque(bool recusado)
    {
        long desde;
        lock (_trava)
        {
            desde = _ataquePendenteDesde;
            _ataquePendenteDesde = 0;
        }

        if (desde != 0)
        {
            _metricas.Latencia(Stopwatch.GetElapsedTime(desde).TotalMilliseconds, recusado);
        }
    }

    private T? Ler<T>(ushort id, byte[] pacote)
        where T : class
    {
        var corpo = ConexaoDoRobo.Ler<T>(pacote);
        if (corpo == null)
        {
            _metricas.Erro(
                TipoDeErro.PacoteInvalido,
                $"{_usuario}: corpo inválido na mensagem {id}"
            );
        }

        return corpo;
    }

    private async Task<T> Esperar<T>(Task<T> resposta, TipoDeErro tipo, string oQue)
    {
        try
        {
            return await resposta.WaitAsync(Espera);
        }
        catch (TimeoutException)
        {
            throw new FalhaDoRobo(tipo, $"{_usuario}: sem resposta ({oQue})");
        }
    }

    // Só letras, como pede RegrasPersonagem.ValidarNome; único entre cargas.
    private static string NomeDePersonagem() =>
        "Robo"
        + new string(
            Guid.NewGuid().ToString("N").Take(10).Select(c => (char)('a' + c % 26)).ToArray()
        );

    public ValueTask DisposeAsync()
    {
        _saindo = true;
        _conexao.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>O robô não conseguiu entrar; o tipo vai para o relatório.</summary>
public sealed class FalhaDoRobo(TipoDeErro tipo, string mensagem) : Exception(mensagem)
{
    public TipoDeErro Tipo { get; } = tipo;
}
