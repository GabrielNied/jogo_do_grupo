using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Npcs;

public class NpcsPresencaTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public async Task JogadorEntraNaCidade_RecebeNpcsDaArea()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());

        var npcsDaArea = await cliente.ReceberAsync<NpcsDaArea>(NpcsMessageIds.NpcsDaArea);
        Assert.NotNull(npcsDaArea);
        Assert.Equal("cidade", npcsDaArea.Area);
        Assert.NotNull(npcsDaArea.Npcs);
        Assert.Equal(7, npcsDaArea.Npcs.Length);
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Mercador Alden" && n.Papel == PapelNpc.Vendedor
        );
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Ferreiro Brom" && n.Papel == PapelNpc.Ferreiro
        );
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Banqueiro Cassian" && n.Papel == PapelNpc.Banqueiro
        );
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Armeiro Gareth" && n.Papel == PapelNpc.Armeiro
        );
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Armadureira Bruna" && n.Papel == PapelNpc.Armadureiro
        );
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Taverneiro Barnabé" && n.Papel == PapelNpc.Taverneiro
        );
        Assert.Contains(
            npcsDaArea.Npcs,
            n => n.Nome == "Merceeiro Tomas" && n.Papel == PapelNpc.Merceeiro
        );
    }

    [Fact]
    public async Task SolicitarNpcs_RetornaNpcsDaArea()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Enviar(NpcsMessageIds.SolicitarNpcs, new SolicitarNpcs());

        var npcsDaArea = await cliente.ReceberAsync<NpcsDaArea>(NpcsMessageIds.NpcsDaArea);
        Assert.NotNull(npcsDaArea);
        Assert.Equal("cidade", npcsDaArea.Area);
        Assert.Equal(7, npcsDaArea.Npcs.Length);
    }
}
