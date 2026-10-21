using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>
/// A arma equipada vai à área com o estado inteiro do visual (#497): quem equipa, quem já estava
/// na cidade e quem entra depois recebem a mesma coisa.
/// </summary>
public class VisualEquipadoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private const int Espada = CatalogoItens.EspadaDeTeste;
    private const int Martelo = 100;

    private sealed record Jogador(HttpClient Http, ClienteTempoRealDeTeste Cliente, long Id);

    private async Task<(HttpClient Http, long Id)> NovoPersonagemAsync()
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (http, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    private async Task<ClienteTempoRealDeTeste> ConectarAsync(HttpClient http, long id)
    {
        var porta = servidor.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            porta,
            http.DefaultRequestHeaders.Authorization!.Parameter!,
            id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        return cliente;
    }

    private async Task<Jogador> NovoNaCidadeAsync()
    {
        var (http, id) = await NovoPersonagemAsync();
        var cliente = await ConectarAsync(http, id);
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        // Ao entrar, o servidor manda o próprio visual (vazio).
        var jogador = new Jogador(http, cliente, id);
        Assert.Empty((await DeAsync(jogador, id)).Armas);
        return jogador;
    }

    private async Task<long> ItemAsync(
        long personagemId,
        int modelo,
        TipoEquipamento? tipoOverride = null
    )
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario { PersonagemId = personagemId, ItemId = modelo };
        var dados = new GeradorEquipamentos(
            new ConfigEquipamentos { QuantidadeAleatorios = 0 }
        ).Gerar(modelo, new Random(1));
        if (tipoOverride.HasValue && dados != null)
        {
            dados.Tipo = tipoOverride.Value;
        }
        item.GravarEquipamento(dados!);
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static async Task EquiparAsync(Jogador j, long item, PosicaoEquipamento posicao) =>
        (
            await ServidorDeTeste.PostJsonAsync(
                j.Http,
                RegrasEquipamentos.RotaDe(j.Id) + "/equipar",
                new PedidoEquipar { InstanciaId = item, Posicao = posicao }
            )
        ).EnsureSuccessStatusCode();

    private static async Task PostAsync<T>(Jogador j, string sufixo, T corpo) =>
        (
            await ServidorDeTeste.PostJsonAsync(
                j.Http,
                RegrasEquipamentos.RotaDe(j.Id) + sufixo,
                corpo
            )
        ).EnsureSuccessStatusCode();

    private static int Quantas(Jogador j) =>
        j.Cliente.IdsRecebidos.Count(i => i == ItensMessageIds.VisualEquipado);

    /// <summary>A próxima mensagem de visual que fala de <paramref name="personagemId"/>.</summary>
    private static async Task<VisualEquipado> DeAsync(Jogador quem, long personagemId)
    {
        while (true)
        {
            var visual = await quem.Cliente.ReceberAsync<VisualEquipado>(
                ItensMessageIds.VisualEquipado
            );
            if (visual.PersonagemId == personagemId)
            {
                return visual;
            }
        }
    }

    [Fact]
    public async Task Equipar_TrocarDeConjuntoEDesequipar_AvisamADonaEAOutroJogadorComOEstadoInteiro()
    {
        var dono = await NovoNaCidadeAsync();
        var outro = await NovoNaCidadeAsync();
        await DeAsync(dono, outro.Id);
        await Task.Delay(300);
        var baseDono = Quantas(dono);
        var baseOutro = Quantas(outro);
        var espada = await ItemAsync(dono.Id, Espada);
        var martelo = await ItemAsync(dono.Id, Martelo);

        await EquiparAsync(dono, espada, PosicaoEquipamento.Arma1);
        foreach (
            var visual in new[] { await DeAsync(dono, dono.Id), await DeAsync(outro, dono.Id) }
        )
        {
            Assert.Equal(1, visual.ConjuntoAtivo);
            var arma = Assert.Single(visual.Armas);
            Assert.Equal(PosicaoEquipamento.Arma1, arma.Posicao);
            Assert.Equal(TipoEquipamento.Espada, arma.Tipo);
        }

        // Segunda arma no conjunto 2 e troca de conjunto: o estado traz as duas, e a do conjunto
        // agora ativo é a que vai à mão.
        await EquiparAsync(dono, martelo, PosicaoEquipamento.Arma2);
        Assert.Equal(2, (await DeAsync(outro, dono.Id)).Armas.Length);
        await PostAsync(dono, "/conjunto", new PedidoConjunto { Conjunto = 2 });
        var trocado = await DeAsync(outro, dono.Id);
        Assert.Equal(2, trocado.ConjuntoAtivo);
        Assert.Equal(2, trocado.Armas.Length);
        Assert.Equal(
            EncaixeDoVisual.MaoDireita,
            RegrasVisualEquipado.EncaixeDe(PosicaoEquipamento.Arma2, trocado.ConjuntoAtivo)
        );

        await PostAsync(
            dono,
            "/desequipar",
            new PedidoDesequipar { Posicao = PosicaoEquipamento.Arma2 }
        );
        await PostAsync(
            dono,
            "/desequipar",
            new PedidoDesequipar { Posicao = PosicaoEquipamento.Arma1 }
        );
        Assert.Single((await DeAsync(outro, dono.Id)).Armas);
        Assert.Empty((await DeAsync(outro, dono.Id)).Armas);

        // Uma mensagem por mudança (equipar, equipar, conjunto, desequipar, desequipar) para cada um.
        await Task.Delay(300);
        Assert.Equal(5, Quantas(outro) - baseOutro);
        Assert.Equal(5, Quantas(dono) - baseDono);
        dono.Cliente.Dispose();
        outro.Cliente.Dispose();
    }

    [Fact]
    public async Task QuemEntraDepois_RecebeOVisualDeQuemJaTemArmaEquipada()
    {
        var dono = await NovoNaCidadeAsync();
        var espada = await ItemAsync(dono.Id, Espada);
        await EquiparAsync(dono, espada, PosicaoEquipamento.Arma1);
        await DeAsync(dono, dono.Id);

        var (http, id) = await NovoPersonagemAsync();
        var novo = new Jogador(http, await ConectarAsync(http, id), id);

        Assert.Empty((await DeAsync(novo, id)).Armas);
        var doDono = await DeAsync(novo, dono.Id);
        Assert.Equal(TipoEquipamento.Espada, Assert.Single(doDono.Armas).Tipo);
        novo.Cliente.Dispose();
        dono.Cliente.Dispose();
    }

    [Fact]
    public async Task RecusaDoServidor_NaoAvisaNinguem()
    {
        var dono = await NovoNaCidadeAsync();
        await Task.Delay(300);
        var antes = Quantas(dono);
        // Item inexistente: 404, nada mudou, ninguém é avisado.
        var resposta = await ServidorDeTeste.PostJsonAsync(
            dono.Http,
            RegrasEquipamentos.RotaDe(dono.Id) + "/equipar",
            new PedidoEquipar { InstanciaId = 999999, Posicao = PosicaoEquipamento.Arma1 }
        );
        Assert.False(resposta.IsSuccessStatusCode);
        await Task.Delay(200);
        Assert.Equal(antes, Quantas(dono));
        dono.Cliente.Dispose();
    }

    [Fact]
    public async Task EquiparArmadura_AvisaAreaComArmaduraVisual_EQuemEntraDepoisRecebe()
    {
        var dono = await NovoNaCidadeAsync();
        var outro = await NovoNaCidadeAsync();
        await DeAsync(dono, outro.Id);

        var capacete = await ItemAsync(dono.Id, 101);
        var peito = await ItemAsync(dono.Id, 102);

        await EquiparAsync(dono, capacete, PosicaoEquipamento.Capacete);
        var v1 = await DeAsync(outro, dono.Id);
        Assert.Single(v1.Armaduras);
        Assert.Equal(PosicaoEquipamento.Capacete, v1.Armaduras[0].Posicao);
        Assert.Equal(TipoEquipamento.Capacete, v1.Armaduras[0].Tipo);

        await EquiparAsync(dono, peito, PosicaoEquipamento.Peito);
        var v2 = await DeAsync(outro, dono.Id);
        Assert.Equal(2, v2.Armaduras.Length);
        Assert.Equal(PosicaoEquipamento.Capacete, v2.Armaduras[0].Posicao);
        Assert.Equal(PosicaoEquipamento.Peito, v2.Armaduras[1].Posicao);

        // Quem entra depois recebe as armaduras do dono
        var (httpNovo, idNovo) = await NovoPersonagemAsync();
        var novo = new Jogador(httpNovo, await ConectarAsync(httpNovo, idNovo), idNovo);
        var doDono = await DeAsync(novo, dono.Id);
        Assert.Equal(2, doDono.Armaduras.Length);

        novo.Cliente.Dispose();
        dono.Cliente.Dispose();
        outro.Cliente.Dispose();
    }
}
