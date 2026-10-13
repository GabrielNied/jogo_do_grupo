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
                ModeloId = 1,
                Nome = nome,
                Tipo = tipo,
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
                    new AtributoItem { Status = StatusItem.DefesaFisica, Valor = forcaEDefesa[1] },
                },
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
        public void MostraQuantosDoadoresTemNaBolsa()
        {
            var principal = Peca("Elmo", TipoEquipamento.Capacete, 50, 10);
            var estado = new EquipamentosResponse
            {
                Inventario = new[]
                {
                    Entrada(principal, PosicaoEquipamento.Inventario),
                    DeOutroId(Peca("Elmo B", TipoEquipamento.Capacete, 20, 10), 2),
                    DeOutroId(Peca("Elmo C", TipoEquipamento.Capacete, 20, 10), 3),
                    DeOutroId(Peca("Anel", TipoEquipamento.Anel, 20, 10), 4),
                },
            };

            var dados = TextosTooltip.Montar(estado.Inventario[0], estado);

            Assert.IsTrue(dados.Linhas.Any(l => l.Texto == "2 doadores na bolsa"));
            Assert.AreEqual("1 doador na bolsa", TextosTooltip.TextoDosDoadores(1));
            Assert.AreEqual("Nenhum doador na bolsa", TextosTooltip.TextoDosDoadores(0));
        }

        private static EntradaInventario DeOutroId(Equipamento item, long id)
        {
            var entrada = Entrada(item, PosicaoEquipamento.Inventario);
            entrada.InstanciaId = id;
            return entrada;
        }

        [Test]
        public void MostraNomeTierGrauFamiliaNivelERequisito()
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
            // Força 6 e Defesa 10 na faixa de 0 a 100: qualidade média 8%, tier D.
            Assert.AreEqual(TierItem.D, dados.Tier);
            StringAssert.Contains("Tier D · Capacete", texto);
            StringAssert.Contains("Grau Medio · nível 3/10", texto);
            StringAssert.Contains("Família Vigia/Bastião · variante A", texto);
            StringAssert.Contains("Requer personagem nível 10", texto);
            StringAssert.Contains("Força 6/100", texto);
            StringAssert.Contains("Defesa física 10/100", texto);
            StringAssert.Contains("Fusão +0/10", texto);
            Assert.IsNull(dados.TituloComparacao);
        }

        [Test]
        public void TierAtualSeguePelosValoresEMostraAOrigemQuandoDifere()
        {
            var peca = Peca("Elmo", TipoEquipamento.Capacete, 90, 85);
            peca.VersaoDosAtributos = RegrasTier.VersaoAtual;
            peca.TierOrigem = TierItem.C;

            var dados = TextosTooltip.Montar(Entrada(peca, PosicaoEquipamento.Inventario), null);

            Assert.AreEqual(TierItem.S, dados.Tier);
            StringAssert.Contains(
                "Tier S (origem C) · Capacete",
                string.Join("\n", dados.Linhas.Select(l => l.Texto))
            );
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
            Assert.IsNull(dados.Tier);
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
                TextosTooltip.SetaGanho + " Defesa física +" + TextosEquipamento.Numero(6),
                ganho.Texto
            );
            Assert.AreEqual(
                TextosTooltip.SetaPerda + " Força -" + TextosEquipamento.Numero(2),
                perda.Texto
            );
        }

        [Test]
        public void PecaEquipadaVaiNoSegundoTooltipComCabecalhoEquipado()
        {
            var equipado = Peca("Elmo velho", TipoEquipamento.Capacete, 8, 4);
            var estado = new EquipamentosResponse
            {
                Equipados = new[] { Entrada(equipado, PosicaoEquipamento.Capacete) },
            };
            var novo = Peca("Elmo novo", TipoEquipamento.Capacete, 6, 10);

            var dados = TextosTooltip.Montar(Entrada(novo, PosicaoEquipamento.Inventario), estado);

            Assert.IsNotNull(dados.Equipado);
            Assert.AreEqual("Elmo velho", dados.Equipado.Nome);
            StringAssert.StartsWith("Equipado", dados.Equipado.Cabecalho);
            Assert.IsNull(dados.Equipado.Equipado);
            Assert.IsEmpty(dados.Equipado.Comparacao);
        }

        [Test]
        public void EncaixeVazioNaoTemSegundoTooltip()
        {
            var dados = TextosTooltip.Montar(
                Entrada(
                    Peca("Elmo", TipoEquipamento.Capacete, 6, 10),
                    PosicaoEquipamento.Inventario
                ),
                new EquipamentosResponse()
            );
            Assert.IsNull(dados.Equipado);
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
