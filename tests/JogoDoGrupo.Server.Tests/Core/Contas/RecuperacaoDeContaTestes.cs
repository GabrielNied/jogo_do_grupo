using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Email;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace JogoDoGrupo.Server.Tests.Core.Contas;

/// <summary>Recuperação de conta por código mandado ao e-mail (#1167), com provedor falso.</summary>
public sealed partial class RecuperacaoDeContaTestes : IClassFixture<ServidorDeTeste>, IDisposable
{
    private readonly ProvedorEmailFalso _emails = new();
    private readonly WebApplicationFactory<Program> _fabrica;

    public RecuperacaoDeContaTestes(ServidorDeTeste servidor)
    {
        _fabrica = servidor.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<IProvedorDeEmail>(_emails))
        );
    }

    public void Dispose() => _fabrica.Dispose();

    [Fact]
    public async Task Codigo_redefine_a_senha_e_encerra_o_token_antigo_no_HTTP_e_no_UDP()
    {
        var (usuario, email, tokenAntigo) = await RegistrarComEmailAsync();

        var codigo = await PedirCodigoAsync(email);
        var resposta = await RedefinirAsync(email, codigo, "senha-redefinida");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        await SegurancaAutenticacaoTestes.AssertTokenRevogadoAsync(_fabrica, tokenAntigo);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(usuario, "senha-redefinida")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await LoginAsync(usuario, SegurancaAutenticacaoTestes.SenhaPadrao)).StatusCode
        );
    }

    [Fact]
    public async Task Codigo_usado_uma_vez_nao_serve_de_novo()
    {
        var (_, email, _) = await RegistrarComEmailAsync();
        var codigo = await PedirCodigoAsync(email);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await RedefinirAsync(email, codigo, "senha-nova-um")).StatusCode
        );
        await AssertCodigoInvalidoAsync(await RedefinirAsync(email, codigo, "senha-nova-dois"));
    }

    [Fact]
    public async Task Novo_pedido_invalida_o_codigo_anterior()
    {
        var (_, email, _) = await RegistrarComEmailAsync();
        var primeiro = await PedirCodigoAsync(email);
        var segundo = await PedirCodigoAsync(email);
        // Um em um milhão: os dois sorteios coincidem e não há o que comparar.
        if (primeiro == segundo)
        {
            return;
        }

        await AssertCodigoInvalidoAsync(await RedefinirAsync(email, primeiro, "senha-nova-um"));
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await RedefinirAsync(email, segundo, "senha-nova-um")).StatusCode
        );
    }

    [Fact]
    public async Task Codigo_vencido_e_recusado()
    {
        var (usuario, email, _) = await RegistrarComEmailAsync();
        var codigo = await PedirCodigoAsync(email);

        using (var escopo = _fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>()
                .SingleAsync(c => c.UsuarioNormalizado == Conta.Normalizar(usuario));
            conta.CodigoRecuperacaoExpiraEm = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        await AssertCodigoInvalidoAsync(await RedefinirAsync(email, codigo, "senha-nova-um"));
    }

    [Fact]
    public void Codigo_vale_15_minutos_e_cai_depois_das_tentativas()
    {
        var agora = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        var conta = new Conta();
        conta.IniciarRecuperacao("hash", agora + RecuperacaoDeConta.ValidadeCodigo);

        Assert.Equal(TimeSpan.FromMinutes(15), RecuperacaoDeConta.ValidadeCodigo);
        Assert.True(Ativa(conta, agora.AddMinutes(14).AddSeconds(59)));
        Assert.False(Ativa(conta, agora.AddMinutes(15)));

        conta.CodigoRecuperacaoTentativas = RecuperacaoDeConta.TentativasPorCodigo;
        Assert.False(Ativa(conta, agora));

        static bool Ativa(Conta conta, DateTime em) => Conta.RecuperacaoAtiva(em).Compile()(conta);
    }

    [Fact]
    public async Task Rajada_de_tentativas_erradas_compara_no_maximo_o_limite()
    {
        var (usuario, email, _) = await RegistrarComEmailAsync();
        var codigo = await PedirCodigoAsync(email);
        var errado = codigo == "000000" ? "000001" : "000000";

        // Sem a condição de limite no update atômico, todas as 20 passam pela checagem e comparam.
        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => RedefinirAsync(email, errado, "senha-nova-um"))
        );

        foreach (var resposta in respostas)
        {
            await AssertCodigoInvalidoAsync(resposta);
        }

        Assert.Equal(RecuperacaoDeConta.TentativasPorCodigo, await TentativasAsync(usuario));
        await AssertCodigoInvalidoAsync(await RedefinirAsync(email, codigo, "senha-nova-um"));
    }

    [Fact]
    public async Task Rajada_de_redefinicoes_com_o_codigo_certo_aceita_uma_so()
    {
        var (usuario, email, _) = await RegistrarComEmailAsync();
        var codigo = await PedirCodigoAsync(email);

        var respostas = await Task.WhenAll(
            Enumerable
                .Range(0, 10)
                .Select(i => RedefinirAsync(email, codigo, $"senha-paralela-{i}"))
        );

        var aceitas = respostas.Where(r => r.StatusCode == HttpStatusCode.NoContent).ToList();
        Assert.Single(aceitas);
        foreach (var resposta in respostas.Except(aceitas))
        {
            await AssertCodigoInvalidoAsync(resposta);
        }

        var vencedora = Array.IndexOf(respostas, aceitas[0]);
        Assert.Equal(
            HttpStatusCode.OK,
            (await LoginAsync(usuario, $"senha-paralela-{vencedora}")).StatusCode
        );
    }

    [Fact]
    public async Task Redefinicoes_por_IP_acima_do_limite_da_hora_respondem_429()
    {
        using var fabrica = _fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Contas:RateLimit:Ativo", "true");
            builder.UseSetting("Contas:Recuperacao:RedefinicoesPorHoraPorIp", "2");
        });
        using var cliente = fabrica.CreateClient();
        var pedido = new RedefinirSenhaRequest
        {
            Email = $"ip-{Guid.NewGuid():N}@exemplo.com",
            Codigo = "123456",
            NovaSenha = "senha-nova-um",
        };

        for (var i = 0; i < 2; i++)
        {
            await AssertCodigoInvalidoAsync(
                await ServidorDeTeste.PostJsonAsync(cliente, RotasContas.RedefinirSenha, pedido)
            );
        }

        var acima = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.RedefinirSenha,
            pedido
        );
        Assert.Equal(HttpStatusCode.TooManyRequests, acima.StatusCode);
        Assert.Equal(
            CodigosErro.MuitasTentativas,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(acima)).Codigo
        );
    }

    [Fact]
    public async Task Trocar_o_email_descarta_o_codigo_pendente()
    {
        var (cliente, _, _) = await SegurancaAutenticacaoTestes.RegistrarELogarAsync(
            _fabrica,
            "Troca"
        );
        var antigo = $"antigo-{Guid.NewGuid():N}@exemplo.com";
        var novo = $"novo-{Guid.NewGuid():N}@exemplo.com";
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await DefinirEmailAsync(cliente, antigo)).StatusCode
        );
        var codigo = await PedirCodigoAsync(antigo);

        Assert.Equal(HttpStatusCode.NoContent, (await DefinirEmailAsync(cliente, novo)).StatusCode);

        // Nem pelo e-mail antigo nem pelo novo: o código foi apagado, não só desligado do endereço.
        await AssertCodigoInvalidoAsync(await RedefinirAsync(antigo, codigo, "senha-nova-um"));
        await AssertCodigoInvalidoAsync(await RedefinirAsync(novo, codigo, "senha-nova-um"));
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Development", true)]
    public async Task Provedor_so_de_log_mostra_o_codigo_apenas_em_Development(
        string ambiente,
        bool mostra
    )
    {
        var log = new LogColetor<ProvedorEmailSoLog>();
        var provedor = new ProvedorEmailSoLog(
            new AmbienteFalso { EnvironmentName = ambiente },
            log
        );
        const string codigo = "482913";

        await provedor.EnviarAsync(
            new MensagemEmail(
                "alguem@exemplo.com",
                RecuperacaoDeConta.AssuntoEmail,
                RecuperacaoDeConta.TextoEmail("Alguem", codigo)
            ),
            CancellationToken.None
        );

        Assert.Single(log.Mensagens);
        Assert.Equal(mostra, log.Mensagens[0].Contains(codigo, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Tentativas_erradas_derrubam_o_codigo()
    {
        var (_, email, _) = await RegistrarComEmailAsync();
        var codigo = await PedirCodigoAsync(email);
        var errado = codigo == "000000" ? "000001" : "000000";

        for (var i = 0; i < RecuperacaoDeConta.TentativasPorCodigo; i++)
        {
            await AssertCodigoInvalidoAsync(await RedefinirAsync(email, errado, "senha-nova-um"));
        }

        await AssertCodigoInvalidoAsync(await RedefinirAsync(email, codigo, "senha-nova-um"));
    }

    [Fact]
    public async Task Senha_nova_passa_pela_regra_e_nao_gasta_o_codigo()
    {
        var (_, email, _) = await RegistrarComEmailAsync();
        var codigo = await PedirCodigoAsync(email);

        var curta = await RedefinirAsync(email, codigo, "curta");
        Assert.Equal(HttpStatusCode.BadRequest, curta.StatusCode);
        Assert.Equal(
            CodigosErro.SenhaInvalida,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(curta)).Codigo
        );
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await RedefinirAsync(email, codigo, "senha-nova-um")).StatusCode
        );
    }

    [Fact]
    public async Task Pedido_responde_igual_para_email_desconhecido()
    {
        var (_, email, _) = await RegistrarComEmailAsync();
        using var cliente = _fabrica.CreateClient();

        var conhecido = await PedirAsync(cliente, email);
        var desconhecido = await PedirAsync(cliente, $"ninguem-{Guid.NewGuid():N}@exemplo.com");

        Assert.Equal(HttpStatusCode.Accepted, conhecido.StatusCode);
        Assert.Equal(conhecido.StatusCode, desconhecido.StatusCode);
        Assert.Equal(
            await conhecido.Content.ReadAsStringAsync(),
            await desconhecido.Content.ReadAsStringAsync()
        );
        await _emails.AguardarCodigoAsync(email);
        Assert.Equal(1, _emails.Total);
    }

    [Fact]
    public async Task Pedidos_por_conta_tem_limite_por_hora_sem_mudar_a_resposta()
    {
        var (_, email, _) = await RegistrarComEmailAsync();
        using var cliente = _fabrica.CreateClient();

        for (var i = 0; i <= RecuperacaoDeConta.PedidosPorHoraPorContaPadrao; i++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await PedirAsync(cliente, email)).StatusCode);
        }

        await _emails.AguardarAsync(email, RecuperacaoDeConta.PedidosPorHoraPorContaPadrao);
        await Task.Delay(200);
        Assert.Equal(RecuperacaoDeConta.PedidosPorHoraPorContaPadrao, _emails.Para(email).Count);
    }

    [Fact]
    public void Controle_de_pedidos_por_conta_libera_depois_de_uma_hora()
    {
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var controle = new ControleDePedidosRecuperacao(relogio, limitePorHora: 2);

        Assert.True(controle.TentarRegistrar(1));
        Assert.True(controle.TentarRegistrar(1));
        Assert.False(controle.TentarRegistrar(1));
        Assert.True(controle.TentarRegistrar(2));

        relogio.Advance(TimeSpan.FromMinutes(59));
        Assert.False(controle.TentarRegistrar(1));
        relogio.Advance(TimeSpan.FromMinutes(1));
        Assert.True(controle.TentarRegistrar(1));
    }

    [Fact]
    public async Task Pedidos_por_IP_acima_do_limite_da_hora_respondem_429()
    {
        using var fabrica = _fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Contas:RateLimit:Ativo", "true");
            builder.UseSetting("Contas:Recuperacao:PedidosPorHoraPorIp", "2");
        });
        using var cliente = fabrica.CreateClient();
        var email = $"ip-{Guid.NewGuid():N}@exemplo.com";

        Assert.Equal(HttpStatusCode.Accepted, (await PedirAsync(cliente, email)).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await PedirAsync(cliente, email)).StatusCode);
        var acima = await PedirAsync(cliente, email);

        Assert.Equal(HttpStatusCode.TooManyRequests, acima.StatusCode);
        Assert.Equal(
            CodigosErro.MuitasTentativas,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(acima)).Codigo
        );
    }

    [Fact]
    public async Task Definir_email_exige_senha_atual_e_email_unico()
    {
        var (_, emailOcupado, _) = await RegistrarComEmailAsync();
        var (cliente, _, _) = await SegurancaAutenticacaoTestes.RegistrarELogarAsync(
            _fabrica,
            "Mail"
        );
        var novo = $"novo-{Guid.NewGuid():N}@Exemplo.com";

        var semSenha = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.DefinirEmail,
            new DefinirEmailRequest { SenhaAtual = "senha-errada", Email = novo }
        );
        Assert.Equal(HttpStatusCode.BadRequest, semSenha.StatusCode);
        Assert.Equal(
            CodigosErro.SenhaAtualIncorreta,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(semSenha)).Codigo
        );

        var ocupado = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.DefinirEmail,
            new DefinirEmailRequest
            {
                SenhaAtual = SegurancaAutenticacaoTestes.SenhaPadrao,
                Email = emailOcupado.ToUpperInvariant(),
            }
        );
        Assert.Equal(HttpStatusCode.Conflict, ocupado.StatusCode);

        var certo = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.DefinirEmail,
            new DefinirEmailRequest
            {
                SenhaAtual = SegurancaAutenticacaoTestes.SenhaPadrao,
                Email = novo,
            }
        );
        Assert.Equal(HttpStatusCode.NoContent, certo.StatusCode);

        var dados = await ServidorDeTeste.LerJsonAsync<DadosContaResponse>(
            await cliente.GetAsync(RotasContas.Dados)
        );
        Assert.Equal(novo, dados.Email);
        // O pedido acha a conta sem diferenciar maiúsculas.
        Assert.False(string.IsNullOrEmpty(await PedirCodigoAsync(novo.ToLowerInvariant())));
    }

    [Theory]
    [InlineData("sem-arroba.com")]
    [InlineData("a@b")]
    [InlineData("a@@b.com")]
    [InlineData("com espaco@b.com")]
    [InlineData("@b.com")]
    public async Task Registro_recusa_email_fora_do_formato(string email)
    {
        using var cliente = _fabrica.CreateClient();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest
            {
                Usuario = ServidorDeTeste.NomeUnico("Fmt"),
                Senha = SegurancaAutenticacaoTestes.SenhaPadrao,
                Email = email,
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(
            CodigosErro.EmailInvalido,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta)).Codigo
        );
    }

    private async Task<(string Usuario, string Email, string Token)> RegistrarComEmailAsync()
    {
        using var cliente = _fabrica.CreateClient();
        var usuario = ServidorDeTeste.NomeUnico("Rec");
        var email = $"{usuario.ToLowerInvariant()}@exemplo.com";
        var registro = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest
            {
                Usuario = usuario,
                Senha = SegurancaAutenticacaoTestes.SenhaPadrao,
                Email = email,
            }
        );
        registro.EnsureSuccessStatusCode();
        var login = await LoginAsync(usuario, SegurancaAutenticacaoTestes.SenhaPadrao);
        login.EnsureSuccessStatusCode();
        return (usuario, email, (await ServidorDeTeste.LerJsonAsync<LoginResponse>(login)).Token);
    }

    private async Task<HttpResponseMessage> LoginAsync(string usuario, string senha)
    {
        using var cliente = _fabrica.CreateClient();
        return await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senha }
        );
    }

    private static Task<HttpResponseMessage> PedirAsync(HttpClient cliente, string email) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.PedirCodigoRecuperacao,
            new PedirCodigoRecuperacaoRequest { Email = email }
        );

    /// <summary>Pede um código e devolve o que chegou no e-mail falso.</summary>
    private async Task<string> PedirCodigoAsync(string email)
    {
        var antes = _emails.Para(email).Count;
        using var cliente = _fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.Accepted, (await PedirAsync(cliente, email)).StatusCode);
        var mensagens = await _emails.AguardarAsync(email, antes + 1);
        return CodigoDoTexto(mensagens[^1].Texto);
    }

    private async Task<HttpResponseMessage> RedefinirAsync(
        string email,
        string codigo,
        string novaSenha
    )
    {
        using var cliente = _fabrica.CreateClient();
        return await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.RedefinirSenha,
            new RedefinirSenhaRequest
            {
                Email = email,
                Codigo = codigo,
                NovaSenha = novaSenha,
            }
        );
    }

    private static Task<HttpResponseMessage> DefinirEmailAsync(HttpClient cliente, string email) =>
        ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.DefinirEmail,
            new DefinirEmailRequest
            {
                SenhaAtual = SegurancaAutenticacaoTestes.SenhaPadrao,
                Email = email,
            }
        );

    private async Task<int> TentativasAsync(string usuario)
    {
        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        return await db.Set<Conta>()
            .Where(c => c.UsuarioNormalizado == Conta.Normalizar(usuario))
            .Select(c => c.CodigoRecuperacaoTentativas)
            .SingleAsync();
    }

    private static async Task AssertCodigoInvalidoAsync(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(
            CodigosErro.CodigoRecuperacaoInvalido,
            (await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta)).Codigo
        );
    }

    private static string CodigoDoTexto(string texto) => SeisDigitos().Match(texto).Value;

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex SeisDigitos();

    private sealed class AmbienteFalso : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class LogColetor<T> : ILogger<T>
    {
        public List<string> Mensagens { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Mensagens.Add(formatter(state, exception));
    }

    /// <summary>Guarda os e-mails em vez de enviar.</summary>
    private sealed class ProvedorEmailFalso : IProvedorDeEmail
    {
        private readonly ConcurrentQueue<MensagemEmail> _enviadas = new();

        public int Total => _enviadas.Count;

        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancelamento)
        {
            _enviadas.Enqueue(mensagem);
            return Task.CompletedTask;
        }

        public List<MensagemEmail> Para(string email) =>
            _enviadas
                .Where(m => string.Equals(m.Para, email, StringComparison.OrdinalIgnoreCase))
                .ToList();

        public Task<List<MensagemEmail>> AguardarCodigoAsync(string email) =>
            AguardarAsync(email, 1);

        /// <summary>Espera (até 5 s) chegarem <paramref name="quantidade"/> e-mails para o endereço.</summary>
        public async Task<List<MensagemEmail>> AguardarAsync(string email, int quantidade)
        {
            var limite = DateTime.UtcNow.AddSeconds(5);
            while (true)
            {
                var mensagens = Para(email);
                if (mensagens.Count >= quantidade)
                {
                    return mensagens;
                }

                Assert.True(DateTime.UtcNow < limite, $"Não chegaram {quantidade} e-mails.");
                await Task.Delay(20);
            }
        }
    }
}
