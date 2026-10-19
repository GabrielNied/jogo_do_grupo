using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

/// <summary>Mercado de ponta a ponta (#176): clients UDP, mensagens e avisos de verdade.</summary>
public sealed class MercadoTempoRealTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<(ClienteTempoRealDeTeste Cliente, long Id)> EntrarAsync()
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest
            {
                Nome = ServidorDeTeste.NomeUnico("M"),
                Raca = Raca.Valdren,
            }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        var porta = servidor.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(porta, token, id);
        Assert.True(resultado.Aceita, resultado.Codigo);
        return (cliente, id);
    }

    [Fact]
    public async Task Anunciar_e_comprar_avisa_os_dois_e_atualiza_o_estado()
    {
        var (vendedor, idVendedor) = await EntrarAsync();
        using var _v = vendedor;
        var (comprador, idComprador) = await EntrarAsync();
        using var _c = comprador;
        await _ambiente.DarItensAsync(idVendedor, CatalogoItens.SinoDoCoelhoDoPasto);
        await _ambiente.CreditarAsync(idComprador, 1000);
        var instancia = (await _ambiente.BolsaAsync(idVendedor)).Single().InstanciaId;

        vendedor.Enviar(
            EconomiaMessageIds.OferecerVenda,
            new OferecerVenda { InstanciaId = instancia, PrecoCoroas = 300 }
        );
        var estadoDoVendedor = await vendedor.ReceberAsync<EstadoDoMercado>(
            EconomiaMessageIds.EstadoDoMercado
        );
        Assert.Single(estadoDoVendedor.Minhas);

        comprador.Enviar(EconomiaMessageIds.ConsultarMercado, new ConsultarMercado());
        var estado = await comprador.ReceberAsync<EstadoDoMercado>(
            EconomiaMessageIds.EstadoDoMercado
        );
        var ordem = estado.Vendas.Single(o => o.ItemId == CatalogoItens.SinoDoCoelhoDoPasto);
        Assert.Equal(300, ordem.PrecoUnitario);

        comprador.Enviar(EconomiaMessageIds.ComprarOrdem, new ComprarOrdem { OrdemId = ordem.Id });

        var avisoVenda = await vendedor.ReceberAsync<AvisoDoMercado>(
            EconomiaMessageIds.AvisoDoMercado
        );
        Assert.Equal(TiposAvisoDoMercado.Vendeu, avisoVenda.Tipo);
        Assert.Equal(15, avisoVenda.Taxa);
        var avisoCompra = await comprador.ReceberAsync<AvisoDoMercado>(
            EconomiaMessageIds.AvisoDoMercado
        );
        Assert.Equal(TiposAvisoDoMercado.Comprou, avisoCompra.Tipo);
        Assert.Equal(700, await _ambiente.SaldoAsync(idComprador));
        Assert.Equal(285, await _ambiente.SaldoAsync(idVendedor));
    }

    [Fact]
    public async Task Pedido_invalido_recebe_a_recusa_com_o_motivo()
    {
        var (jogador, _) = await EntrarAsync();
        using var _j = jogador;

        jogador.Enviar(
            EconomiaMessageIds.OferecerVenda,
            new OferecerVenda { InstanciaId = 999999, PrecoCoroas = 1 }
        );

        var recusa = await jogador.ReceberAsync<EconomiaRecusada>(
            EconomiaMessageIds.EconomiaRecusada
        );
        Assert.Equal(CodigosErroEconomia.PrecoInvalido, recusa.Motivo);
    }
}
