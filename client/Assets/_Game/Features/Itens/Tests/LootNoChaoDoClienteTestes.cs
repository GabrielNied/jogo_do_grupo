using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Features.Itens.Loot;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class LootNoChaoDoClienteTestes
    {
        private static LootApareceu Apareceu(
            long id,
            float x = 0f,
            float z = 0f,
            int expira = RegrasLoot.ReservaSegundos,
            string nome = "Espada de treino"
        ) =>
            new LootApareceu
            {
                Loot = new LootNoChao
                {
                    LootId = id,
                    ItemId = 1,
                    Nome = nome,
                    X = x,
                    Z = z,
                    ExpiraEmSegundos = expira,
                },
            };

        [Test]
        public void Item_que_aparece_entra_e_o_mesmo_id_troca_sem_duplicar()
        {
            var loot = new LootNoChaoDoCliente();
            var saidas = new List<long>();
            loot.Saiu += item => saidas.Add(item.LootId);

            loot.Receber(Apareceu(5, x: 1f));
            loot.Receber(Apareceu(5, x: 2f, expira: 60));

            Assert.AreEqual(1, loot.Quantidade);
            var item = loot.Itens.Single();
            Assert.AreEqual(2f, item.X);
            Assert.AreEqual(60f, item.SegundosRestantes);
            CollectionAssert.AreEqual(new[] { 5L }, saidas);
        }

        [Test]
        public void Mensagem_vazia_ou_com_id_de_tudo_por_perto_e_ignorada()
        {
            var loot = new LootNoChaoDoCliente();
            loot.Receber((LootApareceu)null);
            loot.Receber(new LootApareceu { Loot = null });
            loot.Receber(Apareceu(RegrasLoot.TudoPorPerto));
            loot.Receber((LootSumiu)null);

            Assert.AreEqual(0, loot.Quantidade);
            Assert.AreEqual("", loot.Receber((LootColetado)null));
        }

        [Test]
        public void Coletado_sai_do_chao_e_devolve_o_nome()
        {
            var loot = new LootNoChaoDoCliente();
            loot.Receber(Apareceu(7, nome: "Escudo velho"));

            var nome = loot.Receber(
                new LootColetado
                {
                    LootId = 7,
                    Entrada = new EntradaInventario { Nome = "Outro" },
                }
            );

            Assert.AreEqual("Escudo velho", nome);
            Assert.AreEqual(0, loot.Quantidade);
        }

        [Test]
        public void Coletado_que_o_client_nao_conhecia_usa_o_nome_da_entrada()
        {
            var loot = new LootNoChaoDoCliente();
            var nome = loot.Receber(
                new LootColetado
                {
                    LootId = 99,
                    Entrada = new EntradaInventario { Nome = "Poção" },
                }
            );

            Assert.AreEqual("Poção", nome);
        }

        [Test]
        public void Sumiu_e_prazo_vencido_tiram_o_item()
        {
            var loot = new LootNoChaoDoCliente();
            loot.Receber(Apareceu(1, expira: 10));
            loot.Receber(Apareceu(2, expira: 100));

            loot.Avancar(9.5f);
            Assert.AreEqual(2, loot.Quantidade);
            loot.Avancar(0.5f);
            CollectionAssert.AreEqual(new[] { 2L }, loot.Itens.Select(i => i.LootId).ToArray());

            loot.Receber(new LootSumiu { LootId = 2 });
            Assert.AreEqual(0, loot.Quantidade);
        }

        [Test]
        public void Tempo_invalido_nao_anda()
        {
            var loot = new LootNoChaoDoCliente();
            loot.Receber(Apareceu(1, expira: 10));

            loot.Avancar(-5f);
            loot.Avancar(float.NaN);
            loot.Avancar(float.PositiveInfinity);

            Assert.AreEqual(10f, loot.Itens.Single().SegundosRestantes);
        }

        [Test]
        public void Alcance_da_tecla_e_o_raio_de_coleta_no_plano_do_chao()
        {
            var loot = new LootNoChaoDoCliente();
            Assert.IsFalse(loot.AlgumAoAlcance(0f, 0f), "Sem item, nada ao alcance.");

            loot.Receber(Apareceu(1, x: 10f, z: 0f));
            var raio = (float)RegrasLoot.RaioDeColetaMetros;
            Assert.IsTrue(loot.AlgumAoAlcance(10f - raio, 0f), "Na borda vale.");
            Assert.IsFalse(loot.AlgumAoAlcance(10f - raio - 0.01f, 0f));
        }

        [Test]
        public void Item_aparece_na_tela_ate_o_raio_de_visao()
        {
            var item = new ItemNoChao(Apareceu(1, x: 0f, z: 0f).Loot, 10f);
            var visao = (float)RegrasLoot.RaioDeVisaoMetros;

            Assert.IsTrue(LootNoChaoDoCliente.Visivel(item, 0f, visao));
            Assert.IsFalse(LootNoChaoDoCliente.Visivel(item, 0f, visao + 0.01f));
        }

        [Test]
        public void Textos_traduzem_os_codigos_do_servidor()
        {
            Assert.AreEqual("Você pegou Espada.", TextosDoLoot.Coletado("Espada"));
            Assert.AreEqual("Você pegou um item.", TextosDoLoot.Coletado(""));
            Assert.AreEqual(
                "Você pegou Elmo do Vigia (tier B).",
                TextosDoLoot.Coletado("Elmo do Vigia", TierItem.B)
            );
            Assert.AreEqual(
                "Você pegou Anel (tier S+).",
                TextosDoLoot.Coletado("Anel", TierItem.SMais)
            );
            Assert.AreEqual("Você pegou um item.", TextosDoLoot.Coletado(null, TierItem.B));
            StringAssert.Contains(
                "Inventário cheio",
                TextosDoLoot.Recusa(CodigosErroItens.InventarioCheio)
            );
            Assert.AreNotEqual(
                TextosDoLoot.Recusa(CodigosErroItens.LongeDemais),
                TextosDoLoot.Recusa(CodigosErroItens.LootNaoEncontrado)
            );
            Assert.IsNotEmpty(TextosDoLoot.Recusa("CodigoNovo"));
            Assert.IsFalse(TextosDoLoot.Recusa(CodigosErroItens.LongeDemais).Contains("Longe"));
        }
    }
}
