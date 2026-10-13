using System.Net;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>Fonte de modificadores que o teste controla.</summary>
public sealed class FonteFalsa(FonteDeModificador fonte) : IFonteDeModificadores
{
    public FonteDeModificador Fonte { get; } = fonte;
    public volatile Modificador[] Modificadores = [];

    public Task<IReadOnlyList<Modificador>> ObterAsync(long personagemId, CancellationToken c) =>
        Task.FromResult<IReadOnlyList<Modificador>>(Modificadores);
}

public sealed class ServidorDeSecundarios : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public RelogioFalso Relogio { get; } = new();
    public BonusFalso Bonus { get; } = new();
    public FonteFalsa Arvore { get; } = new(FonteDeModificador.Arvore);
    public WebApplicationFactory<Program> Falso { get; }

    public ServidorDeSecundarios()
    {
        Falso = Real.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.AddSingleton<TimeProvider>(Relogio);
                s.AddSingleton<IBonusDeAtributos>(Bonus);
                s.AddSingleton<IFonteDeModificadores>(Arvore);
            })
        );
    }

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

public class SecundariosTestes(ServidorDeSecundarios servidor)
    : IClassFixture<ServidorDeSecundarios>
{
    private static Modificador Soma(
        AtributoSecundario atributo,
        double valor,
        string grupo = "g"
    ) =>
        new()
        {
            Atributo = atributo,
            Fonte = FonteDeModificador.Arvore,
            Origem = "Nó de teste",
            Operacao = OperacaoDeModificador.Soma,
            Ordem = ConfigAtributosSecundarios.OrdemArvore,
            Grupo = grupo,
            Valor = valor,
        };

    private async Task<(HttpClient Cliente, long Id)> NovoPersonagem()
    {
        servidor.Relogio.Reiniciar();
        servidor.Arvore.Modificadores = [];
        var cliente = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (cliente, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    private static async Task<RecursosResponse> Recursos(HttpClient cliente, long id)
    {
        var resposta = await cliente.GetAsync(RotasCombate.RecursosDe(id));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await ServidorDeTeste.LerJsonAsync<RecursosResponse>(resposta);
    }

    private Task Mudar(long id, Action<RecursosPersonagem> mudar) =>
        servidor
            .Falso.Services.GetRequiredService<RecursosEmMemoria>()
            .AtualizarAsync(id, (r, _) => mudar(r), CancellationToken.None);

    [Fact]
    public async Task A_rota_devolve_os_nove_secundarios_com_a_base_como_origem()
    {
        var (cliente, id) = await NovoPersonagem();

        var r = await Recursos(cliente, id);

        Assert.Equal(RegrasSecundarios.Todos, r.Secundarios.Select(s => s.Atributo));
        foreach (var s in r.Secundarios)
        {
            Assert.Equal(FonteDeModificador.Base, Assert.Single(s.Parcelas).Fonte);
            Assert.Equal(RegrasSecundarios.Base(s.Atributo), s.Valor);
        }
    }

    [Fact]
    public async Task Modificador_da_fonte_entra_com_a_origem_e_o_mesmo_estado_da_o_mesmo_resultado()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Arvore.Modificadores =
        [
            Soma(AtributoSecundario.ChanceCritica, 0.10),
            Soma(AtributoSecundario.RegeneracaoMana, 5),
        ];

        var primeira = await Recursos(cliente, id);
        var segunda = await Recursos(cliente, id);

        var chance = primeira.Secundarios.Single(s =>
            s.Atributo == AtributoSecundario.ChanceCritica
        );
        Assert.Equal(ConfigAtributosSecundarios.BaseChanceCritica + 0.10, chance.Valor, 9);
        Assert.Equal(
            [FonteDeModificador.Base, FonteDeModificador.Arvore],
            chance.Parcelas.Select(p => p.Fonte)
        );
        Assert.Equal(
            primeira.Secundarios.Select(s => s.Valor),
            segunda.Secundarios.Select(s => s.Valor)
        );
    }

    [Fact]
    public async Task Fonte_que_declara_outra_fonte_ou_nao_declara_grupo_e_recusada()
    {
        var (cliente, id) = await NovoPersonagem();
        var disfarcado = Soma(AtributoSecundario.ChanceCritica, 0.5);
        disfarcado.Fonte = FonteDeModificador.Equipamento; // a fonte registrada é a árvore
        servidor.Arvore.Modificadores =
        [
            disfarcado,
            Soma(AtributoSecundario.ChanceCritica, 0.5, grupo: ""),
        ];

        var r = await Recursos(cliente, id);

        var chance = r.Secundarios.Single(s => s.Atributo == AtributoSecundario.ChanceCritica);
        Assert.Equal(ConfigAtributosSecundarios.BaseChanceCritica, chance.Valor);
    }

    [Fact]
    public async Task A_regeneracao_de_mana_usa_o_valor_calculado()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Arvore.Modificadores = [Soma(AtributoSecundario.RegeneracaoMana, 5)];
        await Recursos(cliente, id);
        var agora = servidor.Relogio.GetUtcNow().UtcDateTime;
        await Mudar(
            id,
            r =>
            {
                r.Mana = 0;
                r.UltimoGastoManaEm = agora;
            }
        );

        servidor.Relogio.Avancar(4.0); // 2 s pagos a 5 + 5 = 10 por segundo
        Assert.Equal(20, (await Recursos(cliente, id)).ManaAtual);
    }

    [Fact]
    public async Task Vida_nao_regenera_com_a_taxa_de_teste_zero()
    {
        var (cliente, id) = await NovoPersonagem();
        await Recursos(cliente, id);
        await Mudar(id, r => r.Vida = 100);

        servidor.Relogio.Avancar(60);

        Assert.Equal(100, (await Recursos(cliente, id)).VidaAtual);
    }

    [Fact]
    public async Task Vida_regenera_quando_ha_taxa_mas_quem_caiu_nao_volta_sozinho()
    {
        var (cliente, id) = await NovoPersonagem();
        servidor.Arvore.Modificadores = [Soma(AtributoSecundario.RegeneracaoVida, 2)];
        await Recursos(cliente, id);
        await Mudar(id, r => r.Vida = 100);

        servidor.Relogio.Avancar(10);
        Assert.Equal(120, (await Recursos(cliente, id)).VidaAtual);

        await Mudar(id, r => r.Vida = 0);
        servidor.Relogio.Avancar(60);
        Assert.Equal(0, (await Recursos(cliente, id)).VidaAtual);
    }
}
