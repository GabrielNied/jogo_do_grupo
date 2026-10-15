using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Mobs e Combate usando a área de interesse (IVizinhanca) - issue #788.
/// </summary>
public class AreaDeInteresseTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    private async Task<(
        ClienteTempoRealDeTeste Cliente,
        HttpClient Http,
        long PersonagemId
    )> CriarJogadorAsync()
    {
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(f.Fabrica);
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var contaId = long.Parse(new JsonWebToken(token).Subject);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta);
        f.Verificador.Permitir(contaId, personagem.Id);

        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            f.Porta,
            token,
            personagem.Id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        return (cliente, http, personagem.Id);
    }

    [Fact]
    public async Task Mob_longe_nao_aparece_no_snapshot_e_ao_aproximar_recebe_inimigo_apareceu()
    {
        var (cliente, _, personagemId) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var snapshot = await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);

        var comuns = snapshot
            .Inimigos.Where(i =>
                CatalogoCriaturas.Obter(i.CriaturaId).Categoria == CategoriaDeCriatura.Comum
            )
            .ToArray();
        Assert.Empty(comuns);

        // Teleporta para perto do ponto de spawn no campo
        var teleporte = f.Fabrica.Services.GetRequiredService<ITeleporteDeTeste>();
        Assert.True(teleporte.Teleportar(personagemId, 0f, 0f, 175f).Aceito);

        var servico = f.Fabrica.Services.GetRequiredService<ServicoDeInimigos>();
        servico.Avancar();

        var apareceu = await cliente.ReceberAsync<InimigoApareceu>(MobsMessageIds.InimigoApareceu);
        Assert.NotNull(apareceu.Inimigo);
        Assert.Equal(
            CategoriaDeCriatura.Comum,
            CatalogoCriaturas.Obter(apareceu.Inimigo.CriaturaId).Categoria
        );
    }

    [Fact]
    public async Task Dano_em_alvo_longe_nao_chega_a_jogador_fora_do_raio()
    {
        var (clienteA, httpA, idA) = await CriarJogadorAsync();
        using var _httpA = httpA;
        using var _a = clienteA;
        var (clienteB, httpB, idB) = await CriarJogadorAsync();
        using var _httpB = httpB;
        using var _b = clienteB;

        clienteA.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await clienteA.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        clienteB.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await clienteB.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        // Teleporta B para longe de A (> 180 m)
        var teleporte = f.Fabrica.Services.GetRequiredService<ITeleporteDeTeste>();
        Assert.True(teleporte.Teleportar(idB, 900f, 0f, 900f).Aceito);

        var vizinhanca = f.Fabrica.Services.GetRequiredService<IVizinhanca>();
        Assert.DoesNotContain(idB, vizinhanca.QuemVe(idA));
        Assert.DoesNotContain(idA, vizinhanca.QuemVe(idB));

        // Aplica dano em B
        var eventos = f.Fabrica.Services.GetRequiredService<EventosDoServidor>();
        eventos.Publicar(
            new GolpeDeInimigoAcertou(
                InimigoId: 1,
                CriaturaId: 1,
                PersonagemId: idB,
                Dano: 20,
                Area: AreasDoMundo.Cidade,
                Tipo: TipoDeAtaqueDeInimigo.Investida
            )
        );

        // B recebe a interrupção/reação do acerto especial
        var interrompidoB = await clienteB.ReceberAsync<AcaoInterrompida>(
            CombateMessageIds.AcaoInterrompida
        );
        Assert.Equal(idB, interrompidoB.AlvoId);

        // A não pode receber a interrupção nem outros avisos de combate de B
        await Task.Delay(300);
        Assert.DoesNotContain(CombateMessageIds.AcaoInterrompida, clienteA.IdsRecebidos);
    }

    [Fact]
    public void Em_instancia_todos_os_mobs_sao_replicados_mesmo_longe()
    {
        var controle = new ControleInteresseDeInimigos();
        var inimigoLonge = new InimigoNoMundo
        {
            InimigoId = 100,
            CriaturaId = 1,
            X = 500,
            Z = 500,
            Vida = 100,
            VidaMaxima = 100,
        };

        // Na cidade: mob a 500m é filtrado fora
        var naCidade = controle.FiltrarEntrada(
            personagemId: 1,
            area: AreasDoMundo.Cidade,
            jogadorX: 0,
            jogadorZ: 0,
            vivos: [inimigoLonge]
        );
        Assert.Empty(naCidade);

        // Na instância: mob a 500m é preservado (dungeons replicam toda a área)
        var naDungeon = controle.FiltrarEntrada(
            personagemId: 2,
            area: "instancia:dungeon-1",
            jogadorX: 0,
            jogadorZ: 0,
            vivos: [inimigoLonge]
        );
        Assert.Single(naDungeon);
        Assert.Equal(100L, naDungeon[0].InimigoId);
    }
}
