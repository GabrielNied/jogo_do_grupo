using System.Linq;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class TextosTooltipTestes
    {
        private static Equipamento Peca(
            string nome,
            TipoEquipamento tipo,
            params double[] forcaEDefesa
        )
        {
            return new Equipamento
            {
                Nome = nome,
                Tipo = tipo,
                Raridade = RaridadeItem.Raro,
                Grau = GrauEquipamento.Medio,
                Nivel = 3,
                NivelPersonagemMinimo = 10,
                Familia = "Vigia/Bastião",
                Variacao = "A",
                Atributos = new[]
                {
                    new AtributoItem
                    {
                        Status = StatusItem.Forca,
                        Valor = forcaEDefesa[0],
                        Fixo = true,
                    },
                    new AtributoItem { Status = StatusItem.Defesa, Valor = forcaEDefesa[1] },
                },
                IndiceInfusivel = 1,
            };
        }

        private static EntradaInventario Entrada(Equipamento item, PosicaoEquipamento posicao) =>
            new EntradaInventario
            {
                InstanciaId = 1,
                ItemId = item.ModeloId,
                Equipamento = item,
                Nome = item.Nome,
                Posicao = posicao,
            };

        [Test]
        public void MostraNomeRaridadeGrauFamiliaNivelERequisito()
        {
            var dados = TextosTooltip.Montar(
                Entrada(
                    Peca("Elmo", TipoEquipamento.Capacete, 6, 10),
                    PosicaoEquipamento.Inventario
                ),
                null
            );
            var texto = string.Join("\n", dados.Linhas.Select(l => l.Texto));
            Assert.AreEqual("Elmo", dados.Nome);
            Assert.AreEqual(RaridadeItem.Raro, dados.Raridade);
            StringAssert.Contains("Raro · Capacete", texto);
            StringAssert.Contains("Grau Medio · nível 3/20", texto);
            StringAssert.Contains("Família Vigia/Bastião · variante A", texto);
            StringAssert.Contains("Requer personagem nível 10", texto);
            StringAssert.Contains("(fixo)", texto);
            StringAssert.Contains("(sorteado) · infusível", texto);
            StringAssert.Contains("Infusões: 0/20", texto);
            Assert.IsNull(dados.TituloComparacao);
        }

        [Test]
        public void ItemSemEquipamentoMostraSoONome()
        {
            var dados = TextosTooltip.Montar(
                new EntradaInventario
                {
                    ItemId = CatalogoItens.PocaoDeTeste,
                    Nome = "Poção de teste",
                },
                new EquipamentosResponse()
            );
            Assert.AreEqual("Poção de teste", dados.Nome);
            Assert.IsNull(dados.Raridade);
            Assert.IsEmpty(dados.Linhas);
            Assert.IsNull(dados.TituloComparacao);
        }

        [Test]
        public void ComparaComAPecaEquipadaNoMesmoEncaixeComGanhoEPerda()
        {
            var equipado = Peca("Elmo velho", TipoEquipamento.Capacete, 8, 4);
            var estado = new EquipamentosResponse
            {
                Equipados = new[] { Entrada(equipado, PosicaoEquipamento.Capacete) },
            };
            var novo = Peca("Elmo novo", TipoEquipamento.Capacete, 6, 10);

            var dados = TextosTooltip.Montar(Entrada(novo, PosicaoEquipamento.Inventario), estado);

            StringAssert.Contains("Elmo velho", dados.TituloComparacao);
            var ganho = dados.Comparacao.Single(l => l.Estilo == EstiloLinha.Ganho);
            var perda = dados.Comparacao.Single(l => l.Estilo == EstiloLinha.Perda);
            Assert.AreEqual(
                TextosTooltip.SetaGanho + " Defesa +" + TextosEquipamento.Numero(6),
                ganho.Texto
            );
            Assert.AreEqual(
                TextosTooltip.SetaPerda + " Força -" + TextosEquipamento.Numero(2),
                perda.Texto
            );
        }

        [Test]
        public void EncaixeVazioTrataTudoComoGanho()
        {
            var dados = TextosTooltip.Montar(
                Entrada(
                    Peca("Elmo", TipoEquipamento.Capacete, 6, 10),
                    PosicaoEquipamento.Inventario
                ),
                new EquipamentosResponse()
            );
            StringAssert.Contains("vazio", dados.TituloComparacao);
            Assert.AreEqual(2, dados.Comparacao.Count);
            Assert.IsTrue(dados.Comparacao.All(l => l.Estilo == EstiloLinha.Ganho));
        }

        [Test]
        public void AnelCompararComOPrimeiroEncaixeQuandoOsDoisEstaoCheios()
        {
            var anel1 = Peca("Anel um", TipoEquipamento.Anel, 1, 1);
            var anel2 = Peca("Anel dois", TipoEquipamento.Anel, 5, 5);
            var estado = new EquipamentosResponse
            {
                Equipados = new[]
                {
                    Entrada(anel1, PosicaoEquipamento.Anel1),
                    Entrada(anel2, PosicaoEquipamento.Anel2),
                },
            };
            var dados = TextosTooltip.Montar(
                Entrada(
                    Peca("Anel novo", TipoEquipamento.Anel, 3, 3),
                    PosicaoEquipamento.Inventario
                ),
                estado
            );
            StringAssert.Contains("Anel um", dados.TituloComparacao);
        }

        [Test]
        public void PecaEquipadaNaoTemComparacao()
        {
            var item = Peca("Elmo", TipoEquipamento.Capacete, 6, 10);
            var estado = new EquipamentosResponse
            {
                Equipados = new[] { Entrada(item, PosicaoEquipamento.Capacete) },
            };
            Assert.IsNull(
                TextosTooltip
                    .Montar(Entrada(item, PosicaoEquipamento.Capacete), estado)
                    .TituloComparacao
            );
        }

        [Test]
        public void PecaIgualAEquipadaDiz()
        {
            var item = Peca("Elmo", TipoEquipamento.Capacete, 6, 10);
            var estado = new EquipamentosResponse
            {
                Equipados = new[] { Entrada(item, PosicaoEquipamento.Capacete) },
            };
            var dados = TextosTooltip.Montar(Entrada(item, PosicaoEquipamento.Inventario), estado);
            Assert.AreEqual(1, dados.Comparacao.Count);
            Assert.AreEqual(EstiloLinha.Suave, dados.Comparacao[0].Estilo);
        }
    }
}
