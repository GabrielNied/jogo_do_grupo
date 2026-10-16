using System.Security.Claims;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>
/// Servidor de teste com o diretório de personagens e o verificador trocados por falsos, para
/// criar personagens sem banco e conectar clients UDP de verdade.
/// </summary>
public sealed class GrupoFixture : IDisposable
{
    private static long _proximoPersonagem = 5000;
    private readonly ServidorDeTeste _base = new();

    public GrupoFixture()
    {
        Fabrica = _base.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("TempoReal:TempoParaAutenticarMs", "3000");
            builder.UseSetting("TempoReal:TempoQuedaMs", "1500");
            builder.ConfigureServices(servicos =>
            {
                servicos.AddSingleton<IVerificadorPersonagem>(Verificador);
                servicos.AddSingleton<IDiretorioDePersonagens>(Diretorio);
            });
        });
        TempoReal = Fabrica.Services.GetRequiredService<ServidorTempoReal>();
    }

    public WebApplicationFactory<Program> Fabrica { get; }
    public VerificadorFalso Verificador { get; } = new();
    public DiretorioFalso Diretorio { get; } = new();
    public ServidorTempoReal TempoReal { get; }

    /// <summary>Um jogador conectado: o client UDP, o personagem e o token para reconectar.</summary>
    public sealed record Jogador(
        ClienteTempoRealDeTeste Cliente,
        long PersonagemId,
        string Nome,
        string Token
    ) : IDisposable
    {
        public void Dispose() => Cliente.Dispose();
    }

    /// <summary>Conta e personagem novos, já autenticados no tempo real.</summary>
    public async Task<Jogador> EntrarAsync()
    {
        using var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(Fabrica);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var contaId = long.Parse(new JsonWebToken(token).Subject);
        var personagemId = Interlocked.Increment(ref _proximoPersonagem);
        var nome = ServidorDeTeste.NomeUnico("J");
        Verificador.Permitir(contaId, personagemId);
        Diretorio.Adicionar(personagemId, nome);
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            TempoReal.Porta,
            token,
            personagemId
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        return new Jogador(cliente, personagemId, nome, token);
    }

    /// <summary>Entra de novo com a mesma conta e personagem (depois de sair ou cair).</summary>
    public async Task<Jogador> ReentrarAsync(Jogador antes)
    {
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            TempoReal.Porta,
            antes.Token,
            antes.PersonagemId
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        return antes with { Cliente = cliente };
    }

    public void Dispose()
    {
        Fabrica.Dispose();
        _base.Dispose();
    }
}

/// <summary>Integração: clients UDP de verdade contra o servidor com o SocialModule.</summary>
public class GrupoTempoRealTestes(GrupoFixture f) : IClassFixture<GrupoFixture>
{
    private static Task<AvisoDoGrupo> AvisoAsync(GrupoFixture.Jogador j) =>
        j.Cliente.ReceberAsync<AvisoDoGrupo>(SocialMessageIds.AvisoDoGrupo);

    private static Task<ListaDoGrupo> ListaAsync(GrupoFixture.Jogador j) =>
        j.Cliente.ReceberAsync<ListaDoGrupo>(SocialMessageIds.ListaDoGrupo);

    private static void Convidar(GrupoFixture.Jogador de, string nome) =>
        de.Cliente.Enviar(
            SocialMessageIds.ConvidarParaGrupo,
            new ConvidarParaGrupo { Nome = nome }
        );

    private static void Responder(GrupoFixture.Jogador quem, long conviteId, bool aceitar) =>
        quem.Cliente.Enviar(
            SocialMessageIds.ResponderConvite,
            new ResponderConvite { ConviteId = conviteId, Aceitar = aceitar }
        );

    /// <summary>A convida B pelo nome e B aceita; devolve a lista que A recebeu.</summary>
    private static async Task<ListaDoGrupo> JuntarAsync(
        GrupoFixture.Jogador lider,
        GrupoFixture.Jogador novo
    )
    {
        Convidar(lider, novo.Nome);
        var convite = await novo.Cliente.ReceberAsync<ConviteRecebido>(
            SocialMessageIds.ConviteRecebido
        );
        Responder(novo, convite.ConviteId, true);
        await ListaAsync(novo);
        return await ListaAsync(lider);
    }

    [Fact]
    public async Task Convidar_pelo_nome_e_aceitar_forma_o_grupo_e_os_dois_recebem_a_lista()
    {
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();

        // O nome é achado sem diferenciar maiúsculas.
        Convidar(a, b.Nome.ToUpperInvariant());
        var convite = await b.Cliente.ReceberAsync<ConviteRecebido>(
            SocialMessageIds.ConviteRecebido
        );
        Assert.Equal(a.Nome, convite.NomeDoConvidante);
        Assert.Equal(RegrasGrupo.ValidadeDoConviteSegundos, convite.ValidadeSegundos);

        Responder(b, convite.ConviteId, true);

        var entrou = await AvisoAsync(a);
        Assert.Equal(TipoDeAvisoDoGrupo.EntrouNoGrupo, entrou.Tipo);
        Assert.Equal(b.PersonagemId, entrou.PersonagemId);
        foreach (var quem in new[] { a, b })
        {
            var lista = await ListaAsync(quem);
            Assert.Equal(a.PersonagemId, lista.LiderId);
            Assert.Equal(
                [a.PersonagemId, b.PersonagemId],
                lista.Membros.Select(m => m.PersonagemId)
            );
            Assert.All(lista.Membros, m => Assert.True(m.Online));
        }
    }

    [Fact]
    public async Task Convidar_quem_nao_existe_devolve_a_recusa_so_para_quem_pediu()
    {
        using var a = await f.EntrarAsync();

        Convidar(a, "NinguemComEsseNome");

        var recusa = await a.Cliente.ReceberAsync<OperacaoDoGrupoRecusada>(
            SocialMessageIds.OperacaoDoGrupoRecusada
        );
        Assert.Equal(CodigosErroSocial.AlvoNaoEncontrado, recusa.Codigo);
    }

    [Fact]
    public async Task Sair_sem_grupo_e_recusado_e_o_nome_vazio_nao_derruba_o_servidor()
    {
        using var a = await f.EntrarAsync();

        a.Cliente.Enviar(SocialMessageIds.SairDoGrupo, new SairDoGrupo());
        var recusa = await a.Cliente.ReceberAsync<OperacaoDoGrupoRecusada>(
            SocialMessageIds.OperacaoDoGrupoRecusada
        );
        Assert.Equal(CodigosErroSocial.NaoEstaEmGrupo, recusa.Codigo);

        a.Cliente.EnviarBruto([0xC8, 0x32]); // id 13000 sem corpo
        Convidar(a, "");
        var vazio = await a.Cliente.ReceberAsync<OperacaoDoGrupoRecusada>(
            SocialMessageIds.OperacaoDoGrupoRecusada
        );
        Assert.Equal(CodigosErroSocial.AlvoNaoEncontrado, vazio.Codigo);
    }

    [Fact]
    public async Task Recusar_o_convite_avisa_o_convidante()
    {
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();
        Convidar(a, b.Nome);
        var convite = await b.Cliente.ReceberAsync<ConviteRecebido>(
            SocialMessageIds.ConviteRecebido
        );

        Responder(b, convite.ConviteId, false);

        var aviso = await AvisoAsync(a);
        Assert.Equal(TipoDeAvisoDoGrupo.ConviteRecusado, aviso.Tipo);
        Assert.Equal(b.PersonagemId, aviso.PersonagemId);
        Assert.Equal(b.Nome, aviso.Nome);
    }

    [Fact]
    public async Task Passar_lideranca_expulsar_e_sair_pela_rede()
    {
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();
        using var c = await f.EntrarAsync();
        await JuntarAsync(a, b);
        await Task.Delay(1100); // intervalo mínimo entre convites do mesmo convidante
        Convidar(a, c.Nome);
        var convite = await c.Cliente.ReceberAsync<ConviteRecebido>(
            SocialMessageIds.ConviteRecebido
        );
        Responder(c, convite.ConviteId, true);
        await ListaAsync(c);

        // O líder passa a liderança para B; todos recebem a lista nova.
        a.Cliente.Enviar(
            SocialMessageIds.PassarLideranca,
            new PassarLideranca { PersonagemId = b.PersonagemId }
        );
        var lider = await ListaAsync(c);
        while (lider.LiderId != b.PersonagemId)
        {
            lider = await ListaAsync(c);
        }

        // A, que perdeu a liderança, não consegue mais expulsar; B consegue.
        a.Cliente.Enviar(
            SocialMessageIds.ExpulsarDoGrupo,
            new ExpulsarDoGrupo { PersonagemId = c.PersonagemId }
        );
        var recusa = await a.Cliente.ReceberAsync<OperacaoDoGrupoRecusada>(
            SocialMessageIds.OperacaoDoGrupoRecusada
        );
        Assert.Equal(CodigosErroSocial.NaoELider, recusa.Codigo);

        b.Cliente.Enviar(
            SocialMessageIds.ExpulsarDoGrupo,
            new ExpulsarDoGrupo { PersonagemId = c.PersonagemId }
        );
        var expulso = await c.Cliente.ReceberAsync<AvisoDoGrupo>(SocialMessageIds.AvisoDoGrupo);
        while (expulso.Tipo != TipoDeAvisoDoGrupo.ExpulsoDoGrupo)
        {
            expulso = await AvisoAsync(c);
        }

        Assert.Equal(c.PersonagemId, expulso.PersonagemId);
        var vazia = await ListaAsync(c);
        Assert.Empty(vazia.Membros);

        // A sai; sobra só B, e o grupo se desfaz.
        a.Cliente.Enviar(SocialMessageIds.SairDoGrupo, new SairDoGrupo());
        var desfeito = await AvisoAsync(b);
        while (desfeito.Tipo != TipoDeAvisoDoGrupo.GrupoDesfeito)
        {
            desfeito = await AvisoAsync(b);
        }
    }

    [Fact]
    public async Task Sair_do_jogo_guarda_a_vaga_e_reconectar_devolve_o_grupo()
    {
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();
        await JuntarAsync(a, b);

        b.Cliente.Sair();
        var offline = await AvisoAsync(a);
        Assert.Equal(TipoDeAvisoDoGrupo.MembroOffline, offline.Tipo);
        Assert.Equal(b.PersonagemId, offline.PersonagemId);
        var listaOffline = await ListaAsync(a);
        Assert.False(listaOffline.Membros.Single(m => m.PersonagemId == b.PersonagemId).Online);

        using var volta = await f.ReentrarAsync(b);

        var devolvida = await ListaAsync(volta);
        Assert.Equal(a.PersonagemId, devolvida.LiderId);
        Assert.All(devolvida.Membros, m => Assert.True(m.Online));
        var voltou = await AvisoAsync(a);
        Assert.Equal(TipoDeAvisoDoGrupo.MembroVoltou, voltou.Tipo);
        Assert.Equal(b.PersonagemId, voltou.PersonagemId);
    }

    [Fact]
    public async Task Cair_sem_avisar_tambem_deixa_o_membro_offline()
    {
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();
        await JuntarAsync(a, b);

        b.Cliente.Cair();

        var offline = await AvisoAsync(a);
        Assert.Equal(TipoDeAvisoDoGrupo.MembroOffline, offline.Tipo);
        Assert.Equal(b.PersonagemId, offline.PersonagemId);
    }

    [Fact]
    public async Task Mudanca_de_vida_notifica_os_membros_do_grupo_em_tempo_real()
    {
        using var a = await f.EntrarAsync();
        using var b = await f.EntrarAsync();
        await JuntarAsync(a, b);

        var eventos = f.Fabrica.Services.GetRequiredService<EventosDoServidor>();
        eventos.Publicar(
            new JogoDoGrupo.Server.Contratos.Combate.VidaDoPersonagemMudou(b.PersonagemId, 45, 100)
        );

        var msg = await a.Cliente.ReceberAsync<VidaDoMembroMudou>(
            SocialMessageIds.VidaDoMembroMudou
        );
        Assert.Equal(b.PersonagemId, msg.PersonagemId);
        Assert.Equal(45, msg.Vida);
        Assert.Equal(100, msg.VidaMaxima);
    }
}
