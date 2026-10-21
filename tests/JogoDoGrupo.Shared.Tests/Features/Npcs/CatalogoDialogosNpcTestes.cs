using System;
using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Npcs
{
    public class CatalogoDialogosNpcTestes
    {
        [Fact]
        public void TodosOsNpcsDoCatalogo_PossuemDialogoValido()
        {
            var npcs = CatalogoNpcs.Todos;
            Assert.NotEmpty(npcs);

            foreach (var npc in npcs)
            {
                var dialogo = CatalogoDialogosNpc.ObterDialogo(npc);
                Assert.NotNull(dialogo);
                Assert.Equal(npc.Id, dialogo.NpcId);
                Assert.Equal(npc.Nome, dialogo.Nome);
                Assert.Equal(npc.Papel, dialogo.Papel);
                Assert.False(string.IsNullOrWhiteSpace(dialogo.Fala));
                Assert.NotEmpty(dialogo.Opcoes);

                foreach (var opcao in dialogo.Opcoes)
                {
                    Assert.False(string.IsNullOrWhiteSpace(opcao.Id));
                    Assert.False(string.IsNullOrWhiteSpace(opcao.Texto));
                    if (opcao.Acao == AcaoDeDialogo.AbrirServico)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(opcao.Parametro));
                    }
                }

                Assert.Contains(dialogo.Opcoes, o => o.Acao == AcaoDeDialogo.Fechar);
            }
        }

        [Fact]
        public void NpcNulo_LancaExcecao()
        {
            Assert.Throws<ArgumentNullException>(() => CatalogoDialogosNpc.ObterDialogo(null!));
        }

        [Theory]
        [InlineData(CatalogoNpcs.IdVendedor, "economia.vendedor")]
        [InlineData(CatalogoNpcs.IdFerreiro, "economia.ferreiro")]
        [InlineData(CatalogoNpcs.IdBanqueiro, "itens.banco")]
        [InlineData(CatalogoNpcs.IdArmeiro, "economia.armeiro")]
        [InlineData(CatalogoNpcs.IdArmadureiro, "economia.armadureiro")]
        [InlineData(CatalogoNpcs.IdTaverneiro, "economia.taverneiro")]
        [InlineData(CatalogoNpcs.IdMerceeiro, "economia.merceeiro")]
        [InlineData(CatalogoNpcs.IdCavalarico, "montarias.estabulo")]
        [InlineData(CatalogoNpcs.IdCuidadorDePets, "pets.cuidador")]
        [InlineData(CatalogoNpcs.IdLeiloeiro, "economia.leilao")]
        [InlineData(CatalogoNpcs.IdQuadroDeDiarias, "missoes.quadro")]
        public void NpcDeServico_PossuiOpcaoDeAbrirServicoCorrespondente(
            int npcId,
            string servicoEsperado
        )
        {
            var npc = CatalogoNpcs.Buscar(npcId);
            Assert.NotNull(npc);

            var dialogo = CatalogoDialogosNpc.ObterDialogo(npc);
            Assert.Contains(
                dialogo.Opcoes,
                o => o.Acao == AcaoDeDialogo.AbrirServico && o.Parametro == servicoEsperado
            );
        }
    }
}
