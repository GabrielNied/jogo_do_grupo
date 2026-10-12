using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class PainelEquipamentosTestes
    {
        [Test]
        public void CatalogoMostraFamiliaVarianteENomeAcentuado()
        {
            var item = CatalogoEquipamentos.Obter(172);
            var volta = JsonUtility.FromJson<Equipamento>(JsonUtility.ToJson(item));
            var texto = PainelEquipamentos.Descrever(new EntradaInventario { Equipamento = volta });
            StringAssert.Contains("Talismã do Olho Longo", texto);
            StringAssert.Contains("Família Batedor/Olho Longo · variante B", texto);
            StringAssert.Contains("Vigor +10 (fixo)", texto);
        }

        [Test]
        public void PainelOfereceTresGrausDeTeste()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var raiz = new VisualElement();
            new PainelEquipamentos(raiz, (_, __) => { }).Mostrar(new EquipamentosResponse());
            var seletor = raiz.Q<DropdownField>();
            CollectionAssert.AreEqual(new[] { "Inicial", "Médio", "Forte" }, seletor.choices);
#endif
        }

        [Test]
        public void JsonPreservaStatusFracionarioEIdentidadeDoExemplar()
        {
            var entrada = new EntradaInventario
            {
                InstanciaId = 123,
                Posicao = PosicaoEquipamento.Anel2,
                Equipamento = new Equipamento
                {
                    Atributos = new[]
                    {
                        new AtributoItem { Status = StatusItem.Forca, Valor = 20.4 },
                    },
                },
            };
            var volta = JsonUtility.FromJson<EntradaInventario>(JsonUtility.ToJson(entrada));
            Assert.AreEqual(123, volta.InstanciaId);
            Assert.AreEqual(PosicaoEquipamento.Anel2, volta.Posicao);
            Assert.AreEqual(20.4, volta.Equipamento.Atributos[0].Valor, 0.00001);
        }

        [Test]
        public void PainelMostraPosicoesEEnviaIntencaoSemAlterarEstado()
        {
            var raiz = new VisualElement();
            string acao = null;
            PedidoEquipar pedido = null;
            var painel = new PainelEquipamentos(
                raiz,
                (nome, corpo) =>
                {
                    acao = nome;
                    pedido = corpo as PedidoEquipar;
                }
            );
            var estado = new EquipamentosResponse
            {
                Inventario = new[]
                {
                    new EntradaInventario
                    {
                        InstanciaId = 42,
                        ItemId = 107,
                        Equipamento = CatalogoEquipamentos.Obter(107),
                    },
                },
            };
            painel.Mostrar(estado);
            var seletor = raiz.Q<DropdownField>();
            CollectionAssert.AreEqual(new[] { "Anel 1", "Anel 2" }, seletor.choices);
            Assert.AreEqual(1, estado.Inventario.Length);
            Assert.IsTrue(raiz.Query<Label>().ToList().Exists(l => l.text.StartsWith("Brinco 2:")));
            // A montagem da apresentação não envia pedidos nem modifica o inventário.
            Assert.IsNull(acao);
            Assert.IsNull(pedido);
            Assert.IsEmpty(estado.Equipados);
        }

        [Test]
        public void DescricaoDistingueNivelRequisitoFixoESorteado()
        {
            var item = CatalogoEquipamentos.Obter(100);
            item.NivelPersonagemMinimo = 20;
            item.Atributos = new[]
            {
                new AtributoItem
                {
                    Status = StatusItem.Forca,
                    Valor = 20,
                    Fixo = true,
                },
                new AtributoItem { Status = StatusItem.Vida, Valor = 30 },
            };
            item.IndiceInfusivel = 1;
            var texto = PainelEquipamentos.Descrever(new EntradaInventario { Equipamento = item });
            StringAssert.Contains("nível 1/10", texto);
            StringAssert.Contains("personagem nível 20", texto);
            StringAssert.Contains("Força +20 (fixo)", texto);
            StringAssert.Contains("Vida +30 (sorteado) · infusível", texto);
        }
    }
}
