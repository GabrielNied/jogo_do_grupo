using System.Net;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using JogoDoGrupo.Shared.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Core.Moderacao;

public class AutenticacaoBanimentoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const string Senha = "senha-segura";
    private readonly HttpClient _cliente = servidor.CreateClient();

    [Fact]
    public async Task Login_com_conta_banida_retorna_403_e_codigo_conta_banida()
    {
        var usuario = ServidorDeTeste.NomeUnico();

        var registro = await ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = Senha }
        );
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);

        // Aplica banimento na conta no banco
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>()
                .SingleAsync(c => c.UsuarioNormalizado == usuario.ToUpperInvariant());
            conta.BanidaAte = DateTime.UtcNow.AddDays(7);
            conta.MotivoBanimento = "Uso indevido de software de terceiros";
            await db.SaveChangesAsync();
        }

        var login = await ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = Senha }
        );

        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        var erro = await ServidorDeTeste.LerJsonAsync<ErroApi>(login);
        Assert.Equal(CodigosErro.ContaBanida, erro.Codigo);
        Assert.Contains("Uso indevido", erro.Mensagem);
    }

    [Fact]
    public async Task Login_com_banimento_expirado_permite_acesso()
    {
        var usuario = ServidorDeTeste.NomeUnico();

        var registro = await ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Registrar,
            new RegistrarContaRequest { Usuario = usuario, Senha = Senha }
        );
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);

        // Banimento no passado (expirado)
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>()
                .SingleAsync(c => c.UsuarioNormalizado == usuario.ToUpperInvariant());
            conta.BanidaAte = DateTime.UtcNow.AddMinutes(-10);
            conta.MotivoBanimento = "Banimento temporario expirado";
            await db.SaveChangesAsync();
        }

        var login = await ServidorDeTeste.PostJsonAsync(
            _cliente,
            RotasContas.Login,
            new LoginRequest { Usuario = usuario, Senha = Senha }
        );

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var resposta = await ServidorDeTeste.LerJsonAsync<LoginResponse>(login);
        Assert.False(string.IsNullOrEmpty(resposta.Token));
    }

    [Fact]
    public async Task Autenticacao_tempo_real_com_conta_banida_e_recusada()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var token = cliente.DefaultRequestHeaders.Authorization!.Parameter!;

        var respostaPersonagem = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("P"),
                Raca = Raca.Valdren,
                Aparencia = new AparenciaPersonagem(),
            }
        );
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(respostaPersonagem);

        // Aplica banimento na conta
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var tokenHandler = new JsonWebTokenHandler();
            var jwtToken = tokenHandler.ReadJsonWebToken(token);
            var contaId = long.Parse(jwtToken.Subject);

            var conta = await db.Set<Conta>().SingleAsync(c => c.Id == contaId);
            conta.BanidaAte = DateTime.UtcNow.AddDays(1);
            conta.MotivoBanimento = "Conduta anti-jogo";
            await db.SaveChangesAsync();
        }

        var jwtConfig = servidor.Services.GetRequiredService<ConfiguracaoJwt>();
        var escopos = servidor.Services.GetRequiredService<IServiceScopeFactory>();
        var autenticador = new AutenticadorTempoReal(
            jwtConfig,
            escopos,
            NullLogger<AutenticadorTempoReal>.Instance
        );

        var resultado = await autenticador.AutenticarAsync(
            new AutenticarTempoReal { Token = token, PersonagemId = personagem.Id },
            CancellationToken.None
        );

        Assert.False(resultado.Aceita);
        Assert.Equal(CodigosErro.ContaBanida, resultado.Codigo);
    }

    [Fact]
    public async Task Autenticacao_tempo_real_retorna_eh_gm_corretamente()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var token = cliente.DefaultRequestHeaders.Authorization!.Parameter!;

        var respostaPersonagem = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("Gm"),
                Raca = Raca.Valdren,
                Aparencia = new AparenciaPersonagem(),
            }
        );
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(respostaPersonagem);

        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var tokenHandler = new JsonWebTokenHandler();
            var jwtToken = tokenHandler.ReadJsonWebToken(token);
            var contaId = long.Parse(jwtToken.Subject);

            var conta = await db.Set<Conta>().SingleAsync(c => c.Id == contaId);
            conta.EhGm = true;
            await db.SaveChangesAsync();
        }

        var jwtConfig = servidor.Services.GetRequiredService<ConfiguracaoJwt>();
        var escopos = servidor.Services.GetRequiredService<IServiceScopeFactory>();
        var autenticador = new AutenticadorTempoReal(
            jwtConfig,
            escopos,
            NullLogger<AutenticadorTempoReal>.Instance
        );

        var resultado = await autenticador.AutenticarAsync(
            new AutenticarTempoReal { Token = token, PersonagemId = personagem.Id },
            CancellationToken.None
        );

        Assert.True(resultado.Aceita);
        Assert.True(resultado.EhGm);
    }
}
