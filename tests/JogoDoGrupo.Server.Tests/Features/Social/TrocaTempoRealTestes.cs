using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>
/// Troca direta de ponta a ponta (#175): dois clients UDP na cidade, personagens de verdade no
/// banco, a mensagem de um chegando ao outro.
/// </summary>
public sealed class TrocaTempoRealTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<(ClienteTempoRealDeTeste Cliente, long Id, string Nome)> EntrarAsync()
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("X");
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = nome, Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;

        var porta = servidor.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(porta, token, id);
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return (cliente, id, nome);
    }

    [Fact]
    public async Task Troca_completa_move_coroas_e_item_e_reenvia_o_saldo()
    {
        var (ana, idAna, nomeAna) = await EntrarAsync();
        using var _a = ana;
        var (beto, idBeto, nomeBeto) = await EntrarAsync();
        using var _b = beto;
        await _ambiente.CreditarAsync(idAna, 100);
        await _ambiente.DarItensAsync(idBeto, CatalogoItens.PocaoDeTeste);
        var pocao = (await _ambiente.BolsaAsync(idBeto)).Single().InstanciaId;

        ana.Enviar(SocialMessageIds.PedirTroca, new PedirTroca { Nome = nomeBeto });
        var convite = await beto.ReceberAsync<ConviteDeTroca>(SocialMessageIds.ConviteDeTroca);
        Assert.Equal(nomeAna, convite.NomeDe);
        beto.Enviar(
            SocialMessageIds.ResponderConviteDeTroca,
            new ResponderConviteDeTroca { TrocaId = convite.TrocaId, Aceitar = true }
        );
        var aberta = await ana.ReceberAsync<EstadoDaTroca>(SocialMessageIds.EstadoDaTroca);
        await beto.ReceberAsync<EstadoDaTroca>(SocialMessageIds.EstadoDaTroca);
        Assert.Equal(nomeBeto, aberta.NomeDoOutro);
        var bolsaDoBeto = await beto.ReceberAsync<BolsaParaTroca>(SocialMessageIds.BolsaParaTroca);
        Assert.Contains(bolsaDoBeto.Itens, i => i.InstanciaId == pocao);

        ana.Enviar(
            SocialMessageIds.AtualizarOfertaDeTroca,
            new AtualizarOfertaDeTroca { TrocaId = convite.TrocaId, Coroas = 40 }
        );
        beto.Enviar(
            SocialMessageIds.AtualizarOfertaDeTroca,
            new AtualizarOfertaDeTroca { TrocaId = convite.TrocaId, InstanciaIds = [pocao] }
        );
        // Espera os dois estados com as duas ofertas, na versão mais nova.
        EstadoDaTroca visto;
        do
        {
            visto = await ana.ReceberAsync<EstadoDaTroca>(SocialMessageIds.EstadoDaTroca);
        } while (visto.Dele.Itens.Length == 0 || visto.Minha.Coroas != 40);

        ana.Enviar(
            SocialMessageIds.ConfirmarTroca,
            new ConfirmarTroca { TrocaId = convite.TrocaId, VersaoDaOferta = visto.VersaoDaOferta }
        );
        beto.Enviar(
            SocialMessageIds.ConfirmarTroca,
            new ConfirmarTroca { TrocaId = convite.TrocaId, VersaoDaOferta = visto.VersaoDaOferta }
        );

        var fim = await ana.ReceberAsync<TrocaEncerrada>(SocialMessageIds.TrocaEncerrada);
        Assert.Equal(CodigosErroTroca.Concluida, fim.Motivo);
        var fimBeto = await beto.ReceberAsync<TrocaEncerrada>(SocialMessageIds.TrocaEncerrada);
        Assert.Equal(CodigosErroTroca.Concluida, fimBeto.Motivo);

        Assert.Equal(60, await _ambiente.SaldoAsync(idAna));
        Assert.Equal(40, await _ambiente.SaldoAsync(idBeto));
        Assert.Equal(
            CatalogoItens.PocaoDeTeste,
            (await _ambiente.BolsaAsync(idAna)).Single().ItemId
        );
        Assert.Empty(await _ambiente.BolsaAsync(idBeto));

        // Economia reenvia o saldo de quem mexeu em coroas fora dela.
        var saldoBeto = await beto.ReceberAsync<SaldoCoroasAlterado>(
            EconomiaMessageIds.SaldoCoroasAlterado
        );
        Assert.Equal(40, saldoBeto.Saldo);
    }

    [Fact]
    public async Task Quem_desconecta_cancela_a_troca_sem_perda()
    {
        var (ana, idAna, _) = await EntrarAsync();
        var (beto, idBeto, nomeBeto) = await EntrarAsync();
        using var _b = beto;
        await _ambiente.CreditarAsync(idAna, 50);

        ana.Enviar(SocialMessageIds.PedirTroca, new PedirTroca { Nome = nomeBeto });
        var convite = await beto.ReceberAsync<ConviteDeTroca>(SocialMessageIds.ConviteDeTroca);
        beto.Enviar(
            SocialMessageIds.ResponderConviteDeTroca,
            new ResponderConviteDeTroca { TrocaId = convite.TrocaId, Aceitar = true }
        );
        await ana.ReceberAsync<EstadoDaTroca>(SocialMessageIds.EstadoDaTroca);
        ana.Enviar(
            SocialMessageIds.AtualizarOfertaDeTroca,
            new AtualizarOfertaDeTroca { TrocaId = convite.TrocaId, Coroas = 50 }
        );
        await ana.ReceberAsync<EstadoDaTroca>(SocialMessageIds.EstadoDaTroca);

        ana.Sair();
        ana.Dispose();

        var fim = await beto.ReceberAsync<TrocaEncerrada>(SocialMessageIds.TrocaEncerrada);
        Assert.Equal(CodigosErroTroca.Desconectou, fim.Motivo);
        Assert.Equal(50, await _ambiente.SaldoAsync(idAna));
        Assert.Equal(0, await _ambiente.SaldoAsync(idBeto));
    }

    [Fact]
    public async Task Pedir_troca_a_quem_nao_existe_e_recusado()
    {
        var (ana, _, _) = await EntrarAsync();
        using var _a = ana;

        ana.Enviar(SocialMessageIds.PedirTroca, new PedirTroca { Nome = "Fantasma" });

        var recusa = await ana.ReceberAsync<TrocaRecusada>(SocialMessageIds.TrocaRecusada);
        Assert.Equal(CodigosErroTroca.AlvoNaoEncontrado, recusa.Codigo);
    }
}
