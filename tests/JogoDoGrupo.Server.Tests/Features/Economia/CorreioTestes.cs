using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Economia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Economia;

public sealed class CorreioTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> CorreioAsync<T>(Func<ServicoDeCorreio, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeCorreio>());
    }

    private async Task<(long Id, string Nome)> NovoJogadorAsync(
        long coroas = 0,
        int itemId = 0,
        int itens = 0
    )
    {
        var id = await _ambiente.NovoPersonagemAsync();
        if (coroas > 0)
            await _ambiente.CreditarAsync(id, coroas);
        if (itens > 0)
            await _ambiente.DarItensAsync(id, itemId, itens);

        using var escopo = servidor.Services.CreateAsyncScope();
        var diretorio = escopo.ServiceProvider.GetRequiredService<IDiretorioDePersonagens>();
        diretorio.TentarObterNome(id, out var nome);
        return (id, nome);
    }

    [Fact]
    public async Task EnviarCarta_DebitaTaxaECoroasEGuardaItensEmAnexo()
    {
        var (remetenteId, _) = await NovoJogadorAsync(
            coroas: 100,
            itemId: CatalogoItens.EspadaDeTeste,
            itens: 1
        );
        var (destinatarioId, destinatarioNome) = await NovoJogadorAsync(coroas: 10);

        var bolsa = await _ambiente.BolsaAsync(remetenteId);
        var instancia = bolsa.First(e => e.ItemId == CatalogoItens.EspadaDeTeste).InstanciaId;

        var (erro, cartaId, destId) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                destinatarioNome,
                "Espada de presente",
                "Aproveite a arma!",
                coroas: 30,
                itensInstancias: [instancia],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        Assert.Null(erro);
        Assert.True(cartaId > 0);
        Assert.Equal(destinatarioId, destId);

        // Remetente pagou 30 coroas anexadas + 5 de taxa = 35 coroas gastas
        Assert.Equal(65, await _ambiente.SaldoAsync(remetenteId));

        // Item saiu da bolsa do remetente
        var bolsaRemetente = await _ambiente.BolsaAsync(remetenteId);
        Assert.DoesNotContain(bolsaRemetente, e => e.InstanciaId == instancia);

        // Destinatário consulta correio
        var estado = await CorreioAsync(s => s.ConsultarAsync(destinatarioId, default));
        var carta = Assert.Single(estado.Cartas);
        Assert.Equal(cartaId, carta.Id);
        Assert.Equal("Espada de presente", carta.Assunto);
        Assert.False(carta.Lida);
        Assert.True(carta.TemAnexos);
        Assert.Equal(30, carta.Coroas);
        Assert.Equal(1, carta.TotalItens);
        Assert.Equal(1, estado.NaoLidas);
    }

    [Fact]
    public async Task LerCarta_MarcaComoLidaERetornaDetalhesComAtributos()
    {
        var (remetenteId, _) = await NovoJogadorAsync(
            coroas: 50,
            itemId: CatalogoItens.EscudoDeTeste,
            itens: 1
        );
        var (destinatarioId, destinatarioNome) = await NovoJogadorAsync();

        var bolsa = await _ambiente.BolsaAsync(remetenteId);
        var instancia = bolsa.First(e => e.ItemId == CatalogoItens.EscudoDeTeste).InstanciaId;

        var (_, cartaId, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                destinatarioNome,
                "Escudo protetor",
                "Defenda-se bem.",
                coroas: 15,
                itensInstancias: [instancia],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        // Ler a carta
        var (erro, detalhes) = await CorreioAsync(s =>
            s.LerCartaAsync(destinatarioId, cartaId, default)
        );
        Assert.Null(erro);
        Assert.NotNull(detalhes);
        Assert.Equal("Escudo protetor", detalhes.Assunto);
        Assert.Equal("Defenda-se bem.", detalhes.Mensagem);
        Assert.True(detalhes.Lida);
        Assert.Equal(15, detalhes.Coroas);
        Assert.Single(detalhes.Itens);
        Assert.Equal(CatalogoItens.EscudoDeTeste, detalhes.Itens[0].ItemId);
        Assert.NotNull(detalhes.Itens[0].Item);

        // Nova consulta confirma Lida
        var estado = await CorreioAsync(s => s.ConsultarAsync(destinatarioId, default));
        Assert.True(estado.Cartas[0].Lida);
        Assert.Equal(0, estado.NaoLidas);
    }

    [Fact]
    public async Task ColetarAnexos_TransfereCoroasEItensParaODestinatario()
    {
        var (remetenteId, _) = await NovoJogadorAsync(
            coroas: 50,
            itemId: CatalogoItens.EspadaDeTeste,
            itens: 1
        );
        var (destinatarioId, destinatarioNome) = await NovoJogadorAsync(coroas: 10);

        var bolsa = await _ambiente.BolsaAsync(remetenteId);
        var instancia = bolsa.First().InstanciaId;

        var (_, cartaId, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                destinatarioNome,
                "Itens para você",
                "Pegue tudo.",
                coroas: 25,
                itensInstancias: [instancia],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        // Coletar anexos
        var (erro, coroas, itens) = await CorreioAsync(s =>
            s.ColetarAnexosAsync(
                destinatarioId,
                cartaId,
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        Assert.Null(erro);
        Assert.Equal(25, coroas);
        Assert.Equal(1, itens);

        // Saldo do destinatário aumentou em 25
        Assert.Equal(35, await _ambiente.SaldoAsync(destinatarioId));

        // Item está na bolsa do destinatário
        var bolsaDest = await _ambiente.BolsaAsync(destinatarioId);
        Assert.Contains(bolsaDest, e => e.ItemId == CatalogoItens.EspadaDeTeste);

        // Tentar coletar de novo recusa com SemAnexos
        var (erro2, _, _) = await CorreioAsync(s =>
            s.ColetarAnexosAsync(
                destinatarioId,
                cartaId,
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );
        Assert.Equal(CodigosErroEconomia.SemAnexos, erro2);
    }

    [Fact]
    public async Task EnviarCarta_DestinatarioInexistente_RecusaESemDebito()
    {
        var (remetenteId, _) = await NovoJogadorAsync(coroas: 50);

        var (erro, _, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                "PersonagemQueNaoExisteNunca",
                "Olá",
                "Mensagem",
                coroas: 10,
                itensInstancias: [],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        Assert.Equal(CodigosErroEconomia.DestinatarioNaoEncontrado, erro);
        Assert.Equal(50, await _ambiente.SaldoAsync(remetenteId));
    }

    [Fact]
    public async Task EnviarCarta_ParaSiMesmo_Recusa()
    {
        var (remetenteId, remetenteNome) = await NovoJogadorAsync(coroas: 50);

        var (erro, _, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                remetenteNome,
                "Autoenvio",
                "Não pode",
                coroas: 0,
                itensInstancias: [],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        Assert.Equal(CodigosErroEconomia.DestinatarioInvalido, erro);
        Assert.Equal(50, await _ambiente.SaldoAsync(remetenteId));
    }

    [Fact]
    public async Task EnviarCarta_SaldoInsuficiente_Recusa()
    {
        var (remetenteId, _) = await NovoJogadorAsync(coroas: 3); // menos que a taxa de 5
        var (_, destinatarioNome) = await NovoJogadorAsync();

        var (erro, _, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                destinatarioNome,
                "Sem fundos",
                "Não tenho dinheiro",
                coroas: 0,
                itensInstancias: [],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        Assert.Equal(CodigosErroEconomia.SaldoInsuficiente, erro);
    }

    [Fact]
    public async Task ExcluirCarta_ComAnexosNaoColetados_Recusa()
    {
        var (remetenteId, _) = await NovoJogadorAsync(coroas: 30);
        var (destinatarioId, destinatarioNome) = await NovoJogadorAsync();

        var (_, cartaId, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                destinatarioNome,
                "Carta com ouro",
                "Não apague antes de pegar",
                coroas: 10,
                itensInstancias: [],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        // Tentar excluir com anexo pendente
        var erro = await CorreioAsync(s => s.ExcluirCartaAsync(destinatarioId, cartaId, default));
        Assert.Equal(CodigosErroEconomia.CartaComAnexos, erro);

        // Coleta o anexo
        await CorreioAsync(s =>
            s.ColetarAnexosAsync(destinatarioId, cartaId, false, 0, 0, default)
        );

        // Agora pode excluir com sucesso
        var erro2 = await CorreioAsync(s => s.ExcluirCartaAsync(destinatarioId, cartaId, default));
        Assert.Null(erro2);

        var estado = await CorreioAsync(s => s.ConsultarAsync(destinatarioId, default));
        Assert.Empty(estado.Cartas);
    }

    [Fact]
    public async Task EnviarCartaDoSistema_SemTaxa_CriaCartaComRemetenteSistema()
    {
        var (destinatarioId, _) = await NovoJogadorAsync();

        var cartaId = await CorreioAsync(s =>
            s.EnviarCartaDoSistemaAsync(
                destinatarioId,
                "Mercado Central",
                "Ordem de venda expirada",
                "Seu item foi devolvido.",
                coroas: 100,
                itens: null,
                default
            )
        );

        Assert.True(cartaId > 0);

        var estado = await CorreioAsync(s => s.ConsultarAsync(destinatarioId, default));
        var carta = Assert.Single(estado.Cartas);
        Assert.True(carta.DoSistema);
        Assert.Equal("Mercado Central", carta.Remetente);
        Assert.Equal(100, carta.Coroas);
    }

    [Fact]
    public async Task ProcessarExpiracoes_DevolveAoRemetenteAposValidade()
    {
        var (remetenteId, _) = await NovoJogadorAsync(coroas: 50);
        var (destinatarioId, destinatarioNome) = await NovoJogadorAsync();

        var (_, cartaId, _) = await CorreioAsync(s =>
            s.EnviarCartaAsync(
                remetenteId,
                destinatarioNome,
                "Carta que vai expirar",
                "Mensagem antiga",
                coroas: 20,
                itensInstancias: [],
                exigirProximidade: false,
                x: 0,
                z: 0,
                default
            )
        );

        // Adianta o relógio em 31 dias
        using var escopo = servidor.Services.CreateAsyncScope();
        var relogio = escopo.ServiceProvider.GetRequiredService<RelogioDoMercado>();
        relogio.Avancar(TimeSpan.FromDays(31));

        var processadas = await CorreioAsync(s => s.ProcessarExpiracoesAsync(default));
        Assert.True(processadas >= 1);

        // Remetente original agora deve ter recebido uma carta de devolução
        var estadoRemetente = await CorreioAsync(s => s.ConsultarAsync(remetenteId, default));
        var cartaDevolucao = Assert.Single(estadoRemetente.Cartas);
        Assert.Contains("[Devolução]", cartaDevolucao.Assunto);
        Assert.Equal(20, cartaDevolucao.Coroas);
    }
}
