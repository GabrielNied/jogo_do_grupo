using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>Integração de transferências e visibilidade entre áreas.</summary>
public class AreasDoMundoTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public async Task Transferencias_isolam_presenca_e_replicam_so_na_mesma_area()
    {
        var transicoes = f.Fabrica.Services.GetRequiredService<ITransicoesDeAreaDoMundo>();
        var posicoes = f.Fabrica.Services.GetRequiredService<IPosicoesDoMundo>();
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, idB) = await f.EntrarAsync();
        using var _b = b;
        var (c, _, idC) = await f.EntrarAsync();
        using var _c = c;

        foreach (var cliente in new[] { a, b, c })
        {
            cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
            await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        }

        var instanciaA = AreasDoMundo.Instancia("dungeon-42");
        var instanciaB = AreasDoMundo.Instancia("dungeon-99");
        Assert.True(transicoes.TentarMoverParaArea(idA, Destino(instanciaA)));
        var estadoA = await a.ReceberAsync<EstadoDaArea>(MundoMessageIds.EstadoDaArea);
        Assert.Equal(instanciaA, estadoA.Area);
        Assert.Equal(idA, estadoA.Eu.PersonagemId);
        Assert.Empty(estadoA.Outros);
        Assert.Equal(
            idA,
            (await b.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu)).PersonagemId
        );
        Assert.Equal(
            idA,
            (await c.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu)).PersonagemId
        );

        Assert.True(transicoes.TentarMoverParaArea(idB, Destino(instanciaA)));
        var estadoB = await b.ReceberAsync<EstadoDaArea>(MundoMessageIds.EstadoDaArea);
        Assert.Equal(instanciaA, estadoB.Area);
        Assert.Contains(estadoB.Outros, jogador => jogador.PersonagemId == idA);
        Assert.Equal(
            idB,
            (await a.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou))
                .Jogador
                .PersonagemId
        );
        Assert.Equal(
            idB,
            (await c.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu)).PersonagemId
        );

        Assert.True(transicoes.TentarMoverParaArea(idC, Destino(instanciaB)));
        var estadoC = await c.ReceberAsync<EstadoDaArea>(MundoMessageIds.EstadoDaArea);
        Assert.Equal(instanciaB, estadoC.Area);
        Assert.Empty(estadoC.Outros);

        Assert.False(transicoes.TentarMoverParaArea(idA, Destino("instancia:")));
        Assert.False(transicoes.TentarMoverParaArea(idA, Destino(instanciaB, float.NaN)));
        Assert.False(transicoes.TentarMoverParaArea(long.MaxValue, Destino(instanciaA)));

        Assert.Contains(idA, posicoes.PersonagensNaArea(instanciaA));
        Assert.Contains(idB, posicoes.PersonagensNaArea(instanciaA));
        Assert.Equal(new[] { idC }, posicoes.PersonagensNaArea(instanciaB));
        Assert.Empty(posicoes.PersonagensNaArea(AreasDoMundo.Cidade));
        Assert.True(posicoes.TentarObter(idA, out var posicaoA));
        Assert.Equal(instanciaA, posicaoA.Area);
        Assert.True(posicoes.TentarObter(idC, out var posicaoC));
        Assert.Equal(instanciaB, posicaoC.Area);

        a.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento { X = 0.3f, Sequencia = 1 }
        );
        var replica = await b.ReceberAsync<PosicaoJogador>(MundoMessageIds.PosicaoJogador);
        Assert.Equal(idA, replica.PersonagemId);
        Assert.Equal(0.3f, replica.X);
        await Task.Delay(100);
        Assert.DoesNotContain(MundoMessageIds.PosicaoJogador, c.IdsRecebidos);

        Assert.True(transicoes.TentarMoverParaArea(idA, Destino(AreasDoMundo.Cidade)));
        var volta = await a.ReceberAsync<EstadoDaArea>(MundoMessageIds.EstadoDaArea);
        Assert.Equal(AreasDoMundo.Cidade, volta.Area);
        Assert.Equal(
            idA,
            (await b.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu)).PersonagemId
        );
        Assert.Contains(idA, posicoes.PersonagensNaArea(AreasDoMundo.Cidade));
        Assert.DoesNotContain(idA, posicoes.PersonagensNaArea(instanciaA));
        Assert.Contains(idC, posicoes.PersonagensNaArea(instanciaB));
    }

    private static PosicaoNoMundo Destino(string area, float x = 0f) => new(area, x, 0f, 0f, 0f);
}
