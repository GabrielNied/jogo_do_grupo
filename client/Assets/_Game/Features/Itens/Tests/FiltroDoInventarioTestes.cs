using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class FiltroDoInventarioTestes
    {
        private const int Anel = 107;
        private const int Capacete = 101;
        private const int Pocao = CatalogoItens.PocaoDeTeste;

        private static EntradaInventario Peca(
            long id,
            int modelo,
            string nome,
            bool favorito = false,
            bool lixo = false
        )
        {
            var equipamento = CatalogoEquipamentos.Obter(modelo);
            equipamento.Favorito = favorito;
            equipamento.Lixo = lixo;
            return new EntradaInventario
            {
                InstanciaId = id,
                ItemId = modelo,
                Equipamento = equipamento,
                Nome = nome,
            };
        }

        private static EntradaInventario Consumivel() =>
            new EntradaInventario
            {
                InstanciaId = 99,
                ItemId = Pocao,
                Nome = "Poção",
            };

        [Test]
        public void SemCriterioTudoPassaInclusiveConsumivel()
        {
            var filtro = new FiltroDoInventario();
            Assert.IsFalse(filtro.Ativo);
            Assert.IsTrue(filtro.Passa(Peca(1, Anel, "Anel"), false));
            Assert.IsTrue(filtro.Passa(Consumivel(), false));
        }

        [Test]
        public void BuscaIgnoraCaixaEAcentoEExigeTodasAsPalavras()
        {
            var filtro = new FiltroDoInventario { Texto = "ANEL da presa" };
            Assert.IsTrue(filtro.Ativo);
            Assert.IsTrue(filtro.Passa(Peca(1, Anel, "Anel da Presa-Velha"), false));
            Assert.IsFalse(filtro.Passa(Peca(2, Anel, "Anel do Corvo"), false));

            filtro.Texto = "poçao";
            Assert.IsTrue(filtro.Passa(Consumivel(), false));
        }

        [Test]
        public void BuscaTambemCasaPeloTipoDaPeca()
        {
            var filtro = new FiltroDoInventario { Texto = "capacete" };
            Assert.IsTrue(filtro.Passa(Peca(1, Capacete, "Elmo Ressonante"), false));
            Assert.IsFalse(filtro.Passa(Peca(2, Anel, "Anel"), false));
        }

        [Test]
        public void FiltroPorTipoExcluiOutrosTiposEConsumiveis()
        {
            var filtro = new FiltroDoInventario { Tipo = TipoEquipamento.Anel };
            Assert.IsTrue(filtro.Passa(Peca(1, Anel, "Anel"), false));
            Assert.IsFalse(filtro.Passa(Peca(2, Capacete, "Elmo"), false));
            Assert.IsFalse(filtro.Passa(Consumivel(), false));
        }

        [Test]
        public void TierMinimoAltoEscondeConsumivelENaoEquipamento()
        {
            var filtro = new FiltroDoInventario { TierMinimo = TierItem.SMais };
            Assert.IsFalse(filtro.Passa(Consumivel(), false));
            // Peça de teste sem atributos sorteados nunca chega a S+.
            Assert.IsFalse(filtro.Passa(Peca(1, Anel, "Anel"), false));

            filtro.TierMinimo = TierItem.D;
            Assert.IsFalse(filtro.Ativo);
        }

        [Test]
        public void EscopoFavoritosLixoENovos()
        {
            var favorita = Peca(1, Anel, "Anel", favorito: true);
            var lixo = Peca(2, Anel, "Anel", lixo: true);
            var comum = Peca(3, Anel, "Anel");

            var filtro = new FiltroDoInventario { Escopo = EscopoDoFiltro.Favoritos };
            Assert.IsTrue(filtro.Passa(favorita, false));
            Assert.IsFalse(filtro.Passa(lixo, false));
            Assert.IsFalse(filtro.Passa(Consumivel(), false));

            filtro.Escopo = EscopoDoFiltro.Lixo;
            Assert.IsTrue(filtro.Passa(lixo, false));
            Assert.IsFalse(filtro.Passa(favorita, false));

            filtro.Escopo = EscopoDoFiltro.Novos;
            Assert.IsTrue(filtro.Passa(comum, true));
            Assert.IsFalse(filtro.Passa(comum, false));
            Assert.IsTrue(filtro.Passa(Consumivel(), true));
        }

        [Test]
        public void CriteriosSeCombinamELimparDesliga()
        {
            var filtro = new FiltroDoInventario
            {
                Texto = "anel",
                Tipo = TipoEquipamento.Anel,
                Escopo = EscopoDoFiltro.Favoritos,
            };
            Assert.IsTrue(filtro.Passa(Peca(1, Anel, "Anel", favorito: true), false));
            Assert.IsFalse(filtro.Passa(Peca(2, Anel, "Anel"), false));

            filtro.Limpar();
            Assert.IsFalse(filtro.Ativo);
            Assert.IsTrue(filtro.Passa(Peca(2, Anel, "Anel"), false));
        }

        [Test]
        public void SimplificarTiraAcentoECaixa()
        {
            Assert.AreEqual("calca ruptor", FiltroDoInventario.Simplificar("Calça RUPTOR"));
            Assert.AreEqual("", FiltroDoInventario.Simplificar(null));
        }
    }
}
