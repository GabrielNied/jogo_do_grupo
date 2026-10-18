using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Features.Itens.Loot;
using JogoDoGrupo.Client.Features.Itens.Visual;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class LootNoChaoDoClienteTestes
    {
        [Test]
        public void Marcador_de_loot_usa_cor_por_tier_e_neutro_para_tiers_sem_cor()
        {
            var neutra = new Color(0.45f, 0.45f, 0.45f);

            Assert.AreEqual(neutra, CoresMarcadorDeLoot.Para(false, TierItem.SMais));
            Assert.AreEqual(neutra, CoresMarcadorDeLoot.Para(true, TierItem.D));
            Assert.AreEqual(neutra, CoresMarcadorDeLoot.Para(true, TierItem.C));
            Assert.AreEqual(
                ConfigAuraTier.Cor(TierItem.B),
                CoresMarcadorDeLoot.Para(true, TierItem.B)
            );
            Assert.AreEqual(
                ConfigAuraTier.Cor(TierItem.A),
                CoresMarcadorDeLoot.Para(true, TierItem.A)
            );
            Assert.AreEqual(
                ConfigAuraTier.Cor(TierItem.S),
                CoresMarcadorDeLoot.Para(true, TierItem.S)
            );
            Assert.AreEqual(
                ConfigAuraTier.Cor(TierItem.SMais),
                CoresMarcadorDeLoot.Para(true, TierItem.SMais)
            );
        }

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
        public void Filtro_de_tier_esconde_a_peca_abaixo_e_nao_conta_para_a_dica()
        {
            var loot = new LootNoChaoDoCliente();
            var mensagem = Apareceu(1);
            mensagem.Loot.TemTier = true;
            mensagem.Loot.Tier = TierItem.C;
            loot.Receber(mensagem);

            Assert.IsTrue(loot.AlgumAoAlcance(0f, 0f), "Sem filtro, tudo aparece.");
            Assert.IsTrue(loot.AlgumAoAlcance(0f, 0f, TierItem.C), "Tier igual passa.");
            Assert.IsFalse(loot.AlgumAoAlcance(0f, 0f, TierItem.B), "Abaixo do mínimo some.");
            Assert.IsFalse(loot.Itens.Single().Passa(TierItem.SMais));
        }

        [Test]
        public void Filtro_de_tier_nao_barra_item_sem_tier_e_valor_invalido_volta_a_tudo()
        {
            var loot = new LootNoChaoDoCliente();
            loot.Receber(Apareceu(2));

            Assert.IsTrue(loot.AlgumAoAlcance(0f, 0f, TierItem.SMais), "Sem tier sempre passa.");
            Assert.AreEqual(TierItem.D, FiltroDeLoot.Normalizar(-1));
            Assert.AreEqual(TierItem.D, FiltroDeLoot.Normalizar(99));
            Assert.AreEqual(TierItem.A, FiltroDeLoot.Normalizar((int)TierItem.A));
            Assert.AreEqual(
                System.Enum.GetValues(typeof(TierItem)).Length,
                FiltroDeLoot.Rotulos.Length
            );
        }

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

        [Test]
        public void ObterAoAlcance_EncontraItemMaisProximoETotal()
        {
            var loot = new LootNoChaoDoCliente();
            Assert.IsFalse(loot.ObterAoAlcance(0f, 0f, TierItem.D, out _, out var totalZero));
            Assert.AreEqual(0, totalZero);

            loot.Receber(Apareceu(1, x: 2f, z: 0f, nome: "Item Longe"));
            loot.Receber(Apareceu(2, x: 1f, z: 0f, nome: "Item Perto"));
            loot.Receber(Apareceu(3, x: 10f, z: 0f, nome: "Item Fora"));

            var ok = loot.ObterAoAlcance(0f, 0f, TierItem.D, out var maisProximo, out var total);
            Assert.IsTrue(ok);
            Assert.AreEqual(2, total);
            Assert.NotNull(maisProximo);
            Assert.AreEqual(2L, maisProximo.LootId);
            Assert.AreEqual("Item Perto", maisProximo.Nome);
        }

        [Test]
        public void ObterAoAlcance_RespeitaFiltroDeTier()
        {
            var loot = new LootNoChaoDoCliente();
            var item1 = Apareceu(1, x: 1f, z: 0f, nome: "Elmo D");
            item1.Loot.TemTier = true;
            item1.Loot.Tier = TierItem.D;
            loot.Receber(item1);

            var item2 = Apareceu(2, x: 1.5f, z: 0f, nome: "Espada S");
            item2.Loot.TemTier = true;
            item2.Loot.Tier = TierItem.S;
            loot.Receber(item2);

            // Com filtro para S, apenas o item 2 passa
            var ok = loot.ObterAoAlcance(0f, 0f, TierItem.S, out var maisProximo, out var total);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, total);
            Assert.AreEqual(2L, maisProximo.LootId);
            Assert.AreEqual("Espada S", maisProximo.Nome);
        }

        [Test]
        public void Textos_DescricaoItem_FormataNomeETier()
        {
            Assert.AreEqual(
                "Espada de treino",
                TextosDoLoot.DescricaoItem("Espada de treino", false, TierItem.D)
            );
            Assert.AreEqual(
                "Elmo do Vigia (tier B)",
                TextosDoLoot.DescricaoItem("Elmo do Vigia", true, TierItem.B)
            );
            Assert.AreEqual("Item (tier A)", TextosDoLoot.DescricaoItem("", true, TierItem.A));
            Assert.AreEqual("Item", TextosDoLoot.DescricaoItem(null, false, TierItem.D));
        }

        [Test]
        public void Textos_FormatarDica_UmOuMaisItensETeclaPersonalizada()
        {
            var itemTier = new ItemNoChao(
                new LootNoChao
                {
                    LootId = 1,
                    Nome = "Elmo do Vigia",
                    TemTier = true,
                    Tier = TierItem.B,
                },
                100f
            );
            var itemComum = new ItemNoChao(
                new LootNoChao
                {
                    LootId = 2,
                    Nome = "Poção de Vida",
                    TemTier = false,
                },
                100f
            );

            // 1 item com tier
            Assert.AreEqual(
                "F: pegar — Elmo do Vigia (tier B)",
                TextosDoLoot.FormatarDica("F", itemTier, 1)
            );
            // 1 item comum
            Assert.AreEqual(
                "F: pegar — Poção de Vida",
                TextosDoLoot.FormatarDica("F", itemComum, 1)
            );
            // Mais de 1 item
            Assert.AreEqual("F: pegar (3 itens)", TextosDoLoot.FormatarDica("F", itemTier, 3));
            // Tecla personalizada (ex.: Gamepad Y ou tecla remapeada)
            Assert.AreEqual(
                "E: pegar — Elmo do Vigia (tier B)",
                TextosDoLoot.FormatarDica("E", itemTier, 1)
            );
            Assert.AreEqual("Y: pegar (2 itens)", TextosDoLoot.FormatarDica("Y", itemTier, 2));
            // Sem itens ou nulo
            Assert.AreEqual("F: pegar", TextosDoLoot.FormatarDica("F", null, 0));
        }
    }
}
