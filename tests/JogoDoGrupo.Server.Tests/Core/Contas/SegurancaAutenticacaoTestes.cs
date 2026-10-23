using System.Net;
using System.Net.Http.Json;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using JogoDoGrupo.Shared.Core.TempoReal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace JogoDoGrupo.Server.Tests.Core.Contas;

/// <summary>
/// Testes das regras de segurança de contas: rate limit, bloqueio por falhas e revogação de tokens (#517).
/// </summary>
public class SegurancaAutenticacaoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public void K_falhas_consecutivas_bloqueiam_usuario_com_FakeTimeProvider()
    {
        var relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var controle = new ControleDeFalhasLogin(relogio);
        var usuario = "guerreiro";

        // 1ª e 2ª falhas: instantâneas
        var t1 = controle.RegistrarFalhaAsync(usuario);
        Assert.True(t1.IsCompleted);
        Assert.False(controle.EstaBloqueado(usuario, out _));

        var t2 = controle.RegistrarFalhaAsync(usuario);
        Assert.True(t2.IsCompleted);
        Assert.False(controle.EstaBloqueado(usuario, out _));

        // 3ª falha: atraso de 500ms
        var t3 = controle.RegistrarFalhaAsync(usuario);
        Assert.False(t3.IsCompleted);
        relogio.Advance(TimeSpan.FromMilliseconds(500));
        Assert.True(t3.IsCompleted);
        Assert.False(controle.EstaBloqueado(usuario, out _));

        // 4ª falha: atraso de 1000ms
        var t4 = controle.RegistrarFalhaAsync(usuario);
        Assert.False(t4.IsCompleted);
        relogio.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.True(t4.IsCompleted);
        Assert.False(controle.EstaBloqueado(usuario, out _));

        // 5ª falha: aciona bloqueio temporário
        _ = controle.RegistrarFalhaAsync(usuario);
        Assert.True(controle.EstaBloqueado(usuario, out var tempoRestante));
        Assert.True(tempoRestante > TimeSpan.Zero);

        // Avança 4 minutos: ainda bloqueado
        relogio.Advance(TimeSpan.FromMinutes(4));
        Assert.True(controle.EstaBloqueado(usuario, out _));

        // Avança mais 2 minutos (total 6 min, além dos 5 min de bloqueio): deve liberar
        relogio.Advance(TimeSpan.FromMinutes(2));
        Assert.False(controle.EstaBloqueado(usuario, out _));

        // Registro de sucesso limpa o histórico
        _ = controle.RegistrarFalhaAsync(usuario);
        controle.RegistrarSucesso(usuario);
        Assert.False(controle.EstaBloqueado(usuario, out _));
    }

    [Fact]
    public async Task Login_bloqueia_e_retorna_429_apos_K_falhas_consecutivas()
    {
        using var fabrica = servidor.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Contas:RateLimit:Ativo", "false");
        });
        using var cliente = fabrica.CreateClient();

        var usuario = ServidorDeTeste.NomeUnico("Bloq");
        var senhaCorreta = "senha-correta";
        var senhaErrada = "senha-errada";

        // Cria a conta
        var reg = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = senhaCorreta }
        );
        reg.EnsureSuccessStatusCode();

        // 4 tentativas com senha errada -> 401
        for (var i = 0; i < 4; i++)
        {
            var r = await ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasContas.Login,
                new LoginRequest { Usuario = usuario, Senha = senhaErrada }
            );
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        // 5ª tentativa com senha errada -> 401 (que gera o bloqueio)
        var r5 = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senhaErrada }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, r5.StatusCode);

        // Próxima tentativa (mesmo com senha correta) é recusada com 429 MuitasTentativas
        var rBloqueada = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senhaCorreta }
        );
        Assert.Equal(HttpStatusCode.TooManyRequests, rBloqueada.StatusCode);

        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(rBloqueada);
        Assert.Equal(CodigosErro.MuitasTentativas, erro.Codigo);
        Assert.Contains("Muitas tentativas falhas", erro.Mensagem);
    }

    [Fact]
    public async Task Acima_do_limite_por_IP_retorna_429_e_ErroApi()
    {
        // Cria fábrica isolada com rate limit ativo e limite baixo (3 por minuto)
        using var fabrica = servidor.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Contas:RateLimit:Ativo", "true");
            builder.UseSetting("Contas:RateLimit:LimitePorMinuto", "3");
        });
        using var cliente = fabrica.CreateClient();

        var usuarioBase = ServidorDeTeste.NomeUnico("Rate");

        // 3 requisições são aceitas (passam pelo rate limiter)
        for (var i = 1; i <= 3; i++)
        {
            var r = await ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasContas.Registrar,
                new RegistrarContaRequest { Usuario = $"{usuarioBase}{i}", Senha = "senha-segura" }
            );
            Assert.True(
                r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.Conflict
            );
        }

        // 4ª requisição deve ser barrada pelo RateLimiter com status 429
        var r4 = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = $"{usuarioBase}4", Senha = "senha-segura" }
        );

        Assert.Equal(HttpStatusCode.TooManyRequests, r4.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(r4);
        Assert.Equal(CodigosErro.MuitasTentativas, erro.Codigo);
    }

    [Fact]
    public async Task Token_com_carimbo_antigo_e_recusado_no_HTTP()
    {
        using var fabrica = servidor.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Contas:RateLimit:Ativo", "false");
        });

        var usuario = ServidorDeTeste.NomeUnico("Revog");
        var senha = "senha-segura";

        using var cliente = fabrica.CreateClient();
        var reg = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = senha }
        );
        reg.EnsureSuccessStatusCode();

        var login = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senha }
        );
        login.EnsureSuccessStatusCode();

        var loginResp = await ServidorDeTeste.LerJsonAsync<LoginResponse>(login);
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResp.Token);

        // Com o token recém-emitido, rota protegida funciona (ex: criar ou listar personagens)
        var respOk = await cliente.GetAsync("/api/personagens");
        Assert.Equal(HttpStatusCode.OK, respOk.StatusCode);

        // Altera o CarimboSeguranca no banco e invalida o verificador para a conta exata
        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>()
                .SingleAsync(c => c.UsuarioNormalizado == Conta.Normalizar(usuario));
            conta.RenovarCarimboSeguranca();
            await db.SaveChangesAsync();

            var verificador = escopo.ServiceProvider.GetRequiredService<IVerificadorCarimboConta>();
            verificador.Invalidar(conta.Id);
        }

        // Nova requisição com o token antigo deve falhar com 401 NaoAutenticado
        var respRevogada = await cliente.GetAsync("/api/personagens");
        Assert.Equal(HttpStatusCode.Unauthorized, respRevogada.StatusCode);

        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(respRevogada);
        Assert.Equal(CodigosErro.NaoAutenticado, erro.Codigo);
    }

    [Fact]
    public async Task Token_com_carimbo_antigo_e_recusado_no_UDP()
    {
        using var fabrica = servidor.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Contas:RateLimit:Ativo", "false");
        });

        var usuario = ServidorDeTeste.NomeUnico("Udp");
        var senha = "senha-segura";

        using var clienteHttp = fabrica.CreateClient();
        var reg = await ServidorDeTeste.PostJsonAsync(
            clienteHttp,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = senha }
        );
        reg.EnsureSuccessStatusCode();

        var login = await ServidorDeTeste.PostJsonAsync(
            clienteHttp,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = senha }
        );
        login.EnsureSuccessStatusCode();

        var loginResp = await ServidorDeTeste.LerJsonAsync<LoginResponse>(login);
        var token = loginResp.Token;

        long contaId;
        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>()
                .SingleAsync(c => c.UsuarioNormalizado == Conta.Normalizar(usuario));
            contaId = conta.Id;
        }

        var jwt = fabrica.Services.GetRequiredService<ConfiguracaoJwt>();
        var escopos = fabrica.Services.GetRequiredService<IServiceScopeFactory>();
        var autenticador = new AutenticadorTempoReal(
            jwt,
            escopos,
            NullLogger<AutenticadorTempoReal>.Instance
        );

        // Personagem mock pertencente à conta
        var verificadorPersonagem = new VerificadorPersonagemMock(contaId, personagemId: 100);
        using var escopoSub = fabrica.Services.CreateScope();

        // Altera o carimbo da conta
        using (var escopo = fabrica.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>().SingleAsync(c => c.Id == contaId);
            conta.RenovarCarimboSeguranca();
            await db.SaveChangesAsync();

            var verificador = escopo.ServiceProvider.GetRequiredService<IVerificadorCarimboConta>();
            verificador.Invalidar(contaId);
        }

        // Tentar autenticar no UDP com o token antigo deve ser recusado
        var resultado = await autenticador.AutenticarAsync(
            new AutenticarTempoReal { Token = token, PersonagemId = 100 },
            CancellationToken.None
        );

        Assert.False(resultado.Aceita);
        Assert.Equal(CodigosErro.NaoAutenticado, resultado.Codigo);
    }

    [Fact]
    public async Task Trocar_senha_com_senha_atual_errada_recusa_e_conta_falha()
    {
        var (cliente, usuario, _) = await RegistrarELogarAsync(servidor, "TrocaErr");

        for (var i = 0; i < ControleDeFalhasLogin.FalhasParaBloqueio; i++)
        {
            var errada = await ServidorDeTeste.PostJsonAsync(
                cliente,
                RotasContas.TrocarSenha,
                new TrocarSenhaRequest { SenhaAtual = "senha-errada", NovaSenha = "senha-nova-1" }
            );
            Assert.Equal(HttpStatusCode.BadRequest, errada.StatusCode);
            var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(errada);
            Assert.Equal(CodigosErro.SenhaAtualIncorreta, erro.Codigo);
        }

        // As falhas da troca valem para o mesmo controle do login: a conta fica bloqueada.
        var login = await ServidorDeTeste.PostJsonAsync(
            servidor.CreateClient(),
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = SenhaPadrao }
        );
        Assert.Equal(HttpStatusCode.TooManyRequests, login.StatusCode);
    }

    [Fact]
    public async Task Trocar_senha_exige_senha_nova_dentro_da_regra()
    {
        var (cliente, _, _) = await RegistrarELogarAsync(servidor, "TrocaReg");

        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.TrocarSenha,
            new TrocarSenhaRequest { SenhaAtual = SenhaPadrao, NovaSenha = "curta" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(resposta);
        Assert.Equal(CodigosErro.SenhaInvalida, erro.Codigo);
        // Nada mudou: o token e a senha antigos continuam valendo.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/personagens")).StatusCode);
    }

    [Fact]
    public async Task Trocar_senha_invalida_token_antigo_no_HTTP_e_no_UDP()
    {
        var (cliente, usuario, tokenAntigo) = await RegistrarELogarAsync(servidor, "TrocaOk");
        var novaSenha = "senha-nova-segura";

        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.TrocarSenha,
            new TrocarSenhaRequest { SenhaAtual = SenhaPadrao, NovaSenha = novaSenha }
        );
        resposta.EnsureSuccessStatusCode();
        var tokenNovo = (await ServidorDeTeste.LerJsonAsync<LoginResponse>(resposta)).Token;
        Assert.NotEqual(tokenAntigo, tokenNovo);

        await AssertTokenRevogadoAsync(servidor, tokenAntigo);

        // A sessão de quem trocou segue com o token novo.
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenNovo);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/personagens")).StatusCode);

        // A senha antiga não entra mais; a nova entra.
        var anonimo = servidor.CreateClient();
        var antiga = await ServidorDeTeste.PostJsonAsync(
            anonimo,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = SenhaPadrao }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, antiga.StatusCode);
        var nova = await ServidorDeTeste.PostJsonAsync(
            anonimo,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = novaSenha }
        );
        Assert.Equal(HttpStatusCode.OK, nova.StatusCode);
    }

    [Fact]
    public async Task Trocar_senha_sem_token_responde_401()
    {
        var resposta = await ServidorDeTeste.PostJsonAsync(
            servidor.CreateClient(),
            RotasContas.TrocarSenha,
            new TrocarSenhaRequest { SenhaAtual = SenhaPadrao, NovaSenha = "senha-nova-1" }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    internal const string SenhaPadrao = "senha-segura";

    /// <summary>Registra uma conta e devolve um cliente logado, o usuário e o token.</summary>
    internal static async Task<(
        HttpClient Cliente,
        string Usuario,
        string Token
    )> RegistrarELogarAsync(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> fabrica,
        string prefixo
    )
    {
        var cliente = fabrica.CreateClient();
        var usuario = ServidorDeTeste.NomeUnico(prefixo);
        var registro = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = SenhaPadrao }
        );
        registro.EnsureSuccessStatusCode();
        var login = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = SenhaPadrao }
        );
        login.EnsureSuccessStatusCode();
        var token = (await ServidorDeTeste.LerJsonAsync<LoginResponse>(login)).Token;
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return (cliente, usuario, token);
    }

    /// <summary>O token não abre rota protegida no HTTP nem autentica no tempo real.</summary>
    internal static async Task AssertTokenRevogadoAsync(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> fabrica,
        string token
    )
    {
        using var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var http = await cliente.GetAsync("/api/personagens");
        Assert.Equal(HttpStatusCode.Unauthorized, http.StatusCode);

        var autenticador = new AutenticadorTempoReal(
            fabrica.Services.GetRequiredService<ConfiguracaoJwt>(),
            fabrica.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AutenticadorTempoReal>.Instance
        );
        var udp = await autenticador.AutenticarAsync(
            new AutenticarTempoReal { Token = token, PersonagemId = 1 },
            CancellationToken.None
        );
        Assert.False(udp.Aceita);
        Assert.Equal(CodigosErro.NaoAutenticado, udp.Codigo);
    }

    private sealed class VerificadorPersonagemMock(long contaId, long personagemId)
        : IVerificadorPersonagem
    {
        public Task<bool> PertenceAContaAsync(
            long cId,
            long pId,
            CancellationToken cancelamento = default
        ) => Task.FromResult(cId == contaId && pId == personagemId);
    }
}
