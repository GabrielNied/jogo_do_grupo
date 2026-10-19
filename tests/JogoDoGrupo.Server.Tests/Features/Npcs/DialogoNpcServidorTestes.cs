using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Npcs;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Npcs;

public class DialogoNpcServidorTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    private sealed class PosicoesFalsas(string area, float x, float y, float z) : IPosicoesDoMundo
    {
        public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
        {
            posicao = new PosicaoNoMundo(area, x, y, z, 0f);
            return true;
        }

        public IReadOnlyList<long> PersonagensNaArea(string a) => [];
    }

    [Fact]
    public async Task FalarComNpc_NpcValido_RecebeDialogoDoNpc()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(NpcsMessageIds.FalarComNpc, new FalarComNpc(CatalogoNpcs.IdVendedor));

        var dialogo = await cliente.ReceberAsync<DialogoDoNpc>(NpcsMessageIds.DialogoDoNpc);
        Assert.NotNull(dialogo);
        Assert.Equal(CatalogoNpcs.IdVendedor, dialogo.NpcId);
        Assert.Equal("Mercador Alden", dialogo.Nome);
        Assert.Equal(PapelNpc.Vendedor, dialogo.Papel);
        Assert.False(string.IsNullOrWhiteSpace(dialogo.Fala));
        Assert.NotEmpty(dialogo.Opcoes);
        Assert.Contains(dialogo.Opcoes, o => o.Id == "negociar");
    }

    [Fact]
    public async Task FalarComNpc_NpcInexistente_RecebeNpcNaoEncontrado()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(NpcsMessageIds.FalarComNpc, new FalarComNpc(9999));

        var recusa = await cliente.ReceberAsync<DialogoRecusado>(NpcsMessageIds.DialogoRecusado);
        Assert.NotNull(recusa);
        Assert.Equal(9999, recusa.NpcId);
        Assert.Equal(MotivoRecusaDialogo.NpcNaoEncontrado, recusa.Motivo);
    }

    [Fact]
    public async Task ExecutarOpcaoDeDialogo_OpcaoValida_RecebeConfirmacao()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(
            NpcsMessageIds.ExecutarOpcaoDeDialogo,
            new ExecutarOpcaoDeDialogo(CatalogoNpcs.IdVendedor, "negociar")
        );

        var resposta = await cliente.ReceberAsync<DialogoRecusado>(NpcsMessageIds.DialogoRecusado);
        Assert.NotNull(resposta);
        Assert.Equal(MotivoRecusaDialogo.Nenhum, resposta.Motivo);
    }

    [Fact]
    public async Task ExecutarOpcaoDeDialogo_OpcaoInvalida_RecebeOpcaoInvalida()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(
            NpcsMessageIds.ExecutarOpcaoDeDialogo,
            new ExecutarOpcaoDeDialogo(CatalogoNpcs.IdVendedor, "opcao_inexistente")
        );

        var recusa = await cliente.ReceberAsync<DialogoRecusado>(NpcsMessageIds.DialogoRecusado);
        Assert.NotNull(recusa);
        Assert.Equal(MotivoRecusaDialogo.OpcaoInvalida, recusa.Motivo);
    }

    [Fact]
    public async Task FalarComNpc_LongeDoNpc_RecebeLongeDemais()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(
            JogoDoGrupo.Shared.Features.Mundo.MundoMessageIds.EntrarNaCidade,
            new JogoDoGrupo.Shared.Features.Mundo.EntrarNaCidade()
        );
        await cliente.ReceberAsync<NpcsDaArea>(NpcsMessageIds.NpcsDaArea);

        cliente.Enviar(NpcsMessageIds.FalarComNpc, new FalarComNpc(CatalogoNpcs.IdCavalarico));

        var recusa = await cliente.ReceberAsync<DialogoRecusado>(NpcsMessageIds.DialogoRecusado);
        Assert.NotNull(recusa);
        Assert.Equal(CatalogoNpcs.IdCavalarico, recusa.NpcId);
        Assert.Equal(MotivoRecusaDialogo.LongeDemais, recusa.Motivo);
    }

    [Fact]
    public async Task ExecutarOpcaoDeDialogo_LongeDoNpc_RecebeLongeDemais()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(
            JogoDoGrupo.Shared.Features.Mundo.MundoMessageIds.EntrarNaCidade,
            new JogoDoGrupo.Shared.Features.Mundo.EntrarNaCidade()
        );
        await cliente.ReceberAsync<NpcsDaArea>(NpcsMessageIds.NpcsDaArea);

        cliente.Enviar(
            NpcsMessageIds.ExecutarOpcaoDeDialogo,
            new ExecutarOpcaoDeDialogo(CatalogoNpcs.IdCavalarico, "estabulo")
        );

        var recusa = await cliente.ReceberAsync<DialogoRecusado>(NpcsMessageIds.DialogoRecusado);
        Assert.NotNull(recusa);
        Assert.Equal(CatalogoNpcs.IdCavalarico, recusa.NpcId);
        Assert.Equal(MotivoRecusaDialogo.LongeDemais, recusa.Motivo);
    }
}
