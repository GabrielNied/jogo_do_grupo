using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class PainelEquipamentosTestes
    {
        [Test]
        public void CatalogoMostraFamiliaVarianteENomeAcentuado()
        {
            var item = CatalogoEquipamentos.Obter(172);
            var volta = JsonUtility.FromJson<Equipamento>(JsonUtility.ToJson(item));
            var texto = TextosEquipamento.Descrever(new EntradaInventario { Equipamento = volta });
            StringAssert.Contains("Talismã do Olho Longo", texto);
            StringAssert.Contains("Família Batedor/Olho Longo · variante B", texto);
            StringAssert.Contains("Vigor +10 (fixo)", texto);
        }

        [Test]
        public void JsonPreservaStatusFracionarioEIdentidadeDoExemplar()
        {
            var entrada = new EntradaInventario
            {
                InstanciaId = 123,
                Posicao = PosicaoEquipamento.Anel2,
                Aba = 0,
                Casa = 17,
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
            Assert.AreEqual(17, volta.Casa);
            Assert.AreEqual(20.4, volta.Equipamento.Atributos[0].Valor, 0.00001);
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
            var texto = TextosEquipamento.Descrever(new EntradaInventario { Equipamento = item });
            StringAssert.Contains("nível 1/10", texto);
            StringAssert.Contains("personagem nível 20", texto);
            StringAssert.Contains("Força +20 (fixo)", texto);
            StringAssert.Contains("Vida +30 (sorteado) · infusível", texto);
        }

        [Test]
        public void CapacidadeDaRespostaVemDoServidorENaoDoTamanhoFixo()
        {
            var resposta = JsonUtility.FromJson<EquipamentosResponse>(
                "{\"Capacidade\":{\"Abas\":3,\"CasasPorAba\":64}}"
            );
            Assert.AreEqual(192, resposta.Capacidade.Total);
        }
    }
}
