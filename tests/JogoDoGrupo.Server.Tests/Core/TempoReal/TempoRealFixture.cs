using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

/// <summary>Mensagem de uma funcionalidade de mentira, na faixa <see cref="TempoRealFixture.FaixaTeste"/>.</summary>
public class MensagemTeste
{
    public string Texto = "";
    public int Numero;
}

/// <summary>Personagens permitidos por conta, no lugar do que a funcionalidade Personagem registrar.</summary>
public sealed class VerificadorFalso : IVerificadorPersonagem
{
    private readonly ConcurrentDictionary<(long, long), bool> _permitidos = new();

    public void Permitir(long contaId, long personagemId) =>
        _permitidos[(contaId, personagemId)] = true;

    public Func<Task<bool>>? Consulta { get; set; }

    public Task<bool> PertenceAContaAsync(
        long contaId,
        long personagemId,
        CancellationToken cancelamento
    ) => Consulta?.Invoke() ?? Task.FromResult(_permitidos.ContainsKey((contaId, personagemId)));
}

/// <summary>
/// Servidor de teste com prazos curtos, um verificador de personagem falso e uma faixa de
/// teste registrada: <see cref="IdEco"/> devolve a mensagem para quem mandou.
/// </summary>
public sealed class TempoRealFixture : IDisposable
{
    public static readonly MessageIdRange FaixaTeste = new(60000, 60099);
    public const ushort IdEco = 60000;
    public const ushort IdErro = 60001;
    public const ushort IdSemManipulador = 60050;
    public const int TempoParaAutenticarMs = 1500;

    private readonly ServidorDeTeste _base = new();
    private static long _proximoPersonagem;

    public TempoRealFixture()
        : this(null) { }

    internal TempoRealFixture(Dictionary<string, string>? limites)
    {
        Relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        Fabrica = _base.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("TempoReal:TempoParaAutenticarMs", $"{TempoParaAutenticarMs}");
            builder.UseSetting("TempoReal:TempoQuedaMs", "1500");
            builder.UseSetting("TempoReal:MaximoConexoesPendentesPorIp", "64");
            foreach (var (nome, valor) in limites ?? [])
                builder.UseSetting("TempoReal:" + nome, valor);
            builder.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<TimeProvider>();
                servicos.AddSingleton<TimeProvider>(Relogio);
                servicos.AddSingleton<IVerificadorPersonagem>(Verificador);
            });
        });
        TempoReal = Fabrica.Services.GetRequiredService<ServidorTempoReal>();
        Fabrica
            .Services.GetRequiredService<MensagensTempoReal>()
            .Faixa(FaixaTeste)
            .Ao<MensagemTeste>(
                IdEco,
                (sessao, mensagem) =>
                {
                    Recebidas.Enqueue((sessao, mensagem));
                    TempoReal.Enviar(sessao, IdEco, mensagem);
                }
            )
            .Ao<MensagemTeste>(
                IdErro,
                (SessaoTempoReal _, MensagemTeste _) =>
                    throw new InvalidOperationException("erro de teste")
            );
        TempoReal.MensagemRecusada += (sessao, id, motivo) =>
            Recusadas.Enqueue((sessao, id, motivo));
    }

    public WebApplicationFactory<Program> Fabrica { get; }
    public FakeTimeProvider Relogio { get; }
    public VerificadorFalso Verificador { get; } = new();
    public ServidorTempoReal TempoReal { get; }
    public ConcurrentQueue<(SessaoTempoReal Sessao, MensagemTeste Mensagem)> Recebidas { get; } =
        new();
    public ConcurrentQueue<(
        SessaoTempoReal Sessao,
        ushort Id,
        MotivoRecusa Motivo
    )> Recusadas { get; } = new();

    public int Porta => TempoReal.Porta;

    /// <summary>Avança o relógio injetado no tempo real (prazos e limite de mensagens).</summary>
    public void AvancarRelogio(TimeSpan delta) => Relogio.Advance(delta);

    /// <summary>
    /// Alinha o relógio falso ao UTC real quando o relógio real passou adiante (FakeTimeProvider não
    /// permite voltar no tempo).
    /// </summary>
    public void SincronizarRelogioComAgora()
    {
        var agora = DateTimeOffset.UtcNow;
        if (agora > Relogio.GetUtcNow())
        {
            Relogio.SetUtcNow(agora);
        }
    }

    /// <summary>Conta nova e um personagem permitido para ela.</summary>
    public async Task<(string Token, long ContaId, long PersonagemId)> NovaContaAsync()
    {
        using var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(Fabrica);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var contaId = long.Parse(new JsonWebToken(token).Subject);
        var personagemId = Interlocked.Increment(ref _proximoPersonagem);
        Verificador.Permitir(contaId, personagemId);
        return (token, contaId, personagemId);
    }

    /// <summary>Conecta e autentica uma conta nova; falha o teste se não for aceita.</summary>
    public async Task<(
        ClienteTempoRealDeTeste Cliente,
        long ContaId,
        long PersonagemId
    )> EntrarAsync()
    {
        var (token, contaId, personagemId) = await NovaContaAsync();
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            Porta,
            token,
            personagemId
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        return (cliente, contaId, personagemId);
    }

    public SessaoTempoReal? SessaoDaConta(long contaId) =>
        TempoReal.Sessoes.FirstOrDefault(s => s.ContaId == contaId);

    /// <summary>Token assinado à mão (para testar expirado ou com outra chave).</summary>
    public string Token(DateTime expira, string? chave = null)
    {
        var jwt = Fabrica.Services.GetRequiredService<ConfiguracaoJwt>();
        return new JsonWebTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "1")]),
                Issuer = jwt.Emissor,
                Audience = jwt.Audiencia,
                IssuedAt = expira.AddHours(-2),
                NotBefore = expira.AddHours(-2),
                Expires = expira,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave ?? jwt.Chave)),
                    SecurityAlgorithms.HmacSha256
                ),
            }
        );
    }

    /// <summary>Espera a condição virar verdadeira (até 5 s).</summary>
    public static async Task EsperarAsync(Func<bool> condicao, string descricao)
    {
        var limite = DateTime.UtcNow + ClienteTempoRealDeTeste.Espera;
        while (!condicao())
        {
            Assert.True(DateTime.UtcNow < limite, $"Tempo esgotado esperando: {descricao}");
            await Task.Delay(20);
        }
    }

    public void Dispose()
    {
        Fabrica.Dispose();
        _base.Dispose();
    }
}
