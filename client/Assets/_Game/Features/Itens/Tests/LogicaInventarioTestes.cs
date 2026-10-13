using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class LogicaInventarioTestes
    {
        private const int Anel = 107;
        private const int Capacete = 101;
        private const int Pocao = CatalogoItens.PocaoDeTeste;

        private static EntradaInventario NaBolsa(long id, int modelo, int casa) =>
            new EntradaInventario
            {
                InstanciaId = id,
                ItemId = modelo,
                Equipamento = CatalogoEquipamentos.Obter(modelo),
                Nome = "item" + id,
                Aba = 0,
                Casa = casa,
            };

        private static EntradaInventario Vestido(long id, int modelo, PosicaoEquipamento posicao)
        {
            var entrada = NaBolsa(id, modelo, 0);
            entrada.Posicao = posicao;
            entrada.Aba = RegrasItens.SemCasa;
            entrada.Casa = RegrasItens.SemCasa;
            return entrada;
        }

        private static AcaoDoInventario Soltar(EntradaInventario item, AlvoSoltura alvo) =>
            LogicaInventario.ResolverSoltura(LogicaInventario.OrigemDe(item), alvo);

        [Test]
        public void SoltarNaBolsaEmOutraCasaMove()
        {
            var acao = Soltar(NaBolsa(1, Anel, 3), AlvoSoltura.EmCasa(0, 9));
            Assert.AreEqual(TipoAcao.Mover, acao.Tipo);
            Assert.AreEqual(1, acao.InstanciaId);
            Assert.AreEqual(9, acao.Casa);
        }

        [Test]
        public void SoltarNaMesmaCasaNaoFazNada()
        {
            Assert.AreEqual(
                TipoAcao.Nada,
                Soltar(NaBolsa(1, Anel, 3), AlvoSoltura.EmCasa(0, 3)).Tipo
            );
        }

        [Test]
        public void SoltarNoEncaixeQueAceitaEquipa()
        {
            var acao = Soltar(NaBolsa(1, Anel, 3), AlvoSoltura.EmEncaixe(PosicaoEquipamento.Anel2));
            Assert.AreEqual(TipoAcao.Equipar, acao.Tipo);
            Assert.AreEqual(PosicaoEquipamento.Anel2, acao.Posicao);
        }

        [Test]
        public void SoltarNoEncaixeInvalidoRecusaSemEnviar()
        {
            var acao = Soltar(
                NaBolsa(1, Anel, 3),
                AlvoSoltura.EmEncaixe(PosicaoEquipamento.Capacete)
            );
            Assert.AreEqual(TipoAcao.Recusada, acao.Tipo);
            Assert.AreEqual(LogicaInventario.AvisoNaoCabe, acao.Aviso);
        }

        [Test]
        public void ItemQueNaoEhEquipamentoNaoVaiParaEncaixe()
        {
            var pocao = new EntradaInventario
            {
                InstanciaId = 5,
                ItemId = Pocao,
                Aba = 0,
                Casa = 1,
            };
            var acao = Soltar(pocao, AlvoSoltura.EmEncaixe(PosicaoEquipamento.Adorno));
            Assert.AreEqual(TipoAcao.Recusada, acao.Tipo);
            Assert.AreEqual(LogicaInventario.AvisoNaoEquipavel, acao.Aviso);
        }

        [Test]
        public void SoltarForaDaJanelaPedeDescarte()
        {
            var acao = Soltar(NaBolsa(1, Anel, 3), AlvoSoltura.Em(TipoAlvo.Fora));
            Assert.AreEqual(TipoAcao.Descartar, acao.Tipo);
            Assert.AreEqual(1, acao.InstanciaId);
        }

        [Test]
        public void SoltarDentroDaJanelaMasForaDeCasaNaoFazNada()
        {
            Assert.AreEqual(
                TipoAcao.Nada,
                Soltar(NaBolsa(1, Anel, 3), AlvoSoltura.Em(TipoAlvo.Nenhum)).Tipo
            );
        }

        [Test]
        public void ArrastarDoEncaixeParaUmaCasaDesequipaParaAquelaCasa()
        {
            var acao = Soltar(
                Vestido(2, Anel, PosicaoEquipamento.Anel1),
                AlvoSoltura.EmCasa(0, 12)
            );
            Assert.AreEqual(TipoAcao.Desequipar, acao.Tipo);
            Assert.AreEqual(PosicaoEquipamento.Anel1, acao.Posicao);
            Assert.IsTrue(acao.TemDestino);
            Assert.AreEqual(12, acao.Casa);
        }

        [Test]
        public void ArrastarEquipadoParaOutroEncaixeDoMesmoTipoReequipa()
        {
            var acao = Soltar(
                Vestido(2, Anel, PosicaoEquipamento.Anel1),
                AlvoSoltura.EmEncaixe(PosicaoEquipamento.Anel2)
            );
            Assert.AreEqual(TipoAcao.Equipar, acao.Tipo);
            Assert.AreEqual(PosicaoEquipamento.Anel2, acao.Posicao);
        }

        [Test]
        public void ArrastarEquipadoParaOMesmoEncaixeNaoFazNada()
        {
            var acao = Soltar(
                Vestido(2, Anel, PosicaoEquipamento.Anel1),
                AlvoSoltura.EmEncaixe(PosicaoEquipamento.Anel1)
            );
            Assert.AreEqual(TipoAcao.Nada, acao.Tipo);
        }

        [Test]
        public void ArrastarEquipadoParaForaRecusaEPedeParaDesequiparAntes()
        {
            var acao = Soltar(
                Vestido(2, Anel, PosicaoEquipamento.Anel1),
                AlvoSoltura.Em(TipoAlvo.Fora)
            );
            Assert.AreEqual(TipoAcao.Recusada, acao.Tipo);
        }

        [Test]
        public void EncaixesValidosSaoOsQueAceitamOTipo()
        {
            CollectionAssert.AreEquivalent(
                new[] { PosicaoEquipamento.Anel1, PosicaoEquipamento.Anel2 },
                LogicaInventario.EncaixesValidos(TipoEquipamento.Anel)
            );
            CollectionAssert.AreEquivalent(
                new[] { PosicaoEquipamento.Capacete },
                LogicaInventario.EncaixesValidos(TipoEquipamento.Capacete)
            );
            CollectionAssert.AreEquivalent(
                new[] { PosicaoEquipamento.Arma1, PosicaoEquipamento.Arma2 },
                LogicaInventario.EncaixesValidos(TipoEquipamento.Espada)
            );
        }

        [Test]
        public void EncaixesValidosDoItemSomemQuandoONivelNaoBasta()
        {
            var capacete = CatalogoEquipamentos.Obter(Capacete);
            capacete.NivelPersonagemMinimo = 10;
            var estado = new EquipamentosResponse { NivelDoPersonagem = 5 };

            CollectionAssert.IsEmpty(LogicaInventario.EncaixesValidos(capacete, estado));
            Assert.AreEqual(
                CodigosErroItens.NivelInsuficiente,
                LogicaInventario.RecusaNoEncaixe(capacete, PosicaoEquipamento.Capacete, estado)
            );

            estado.NivelDoPersonagem = 10;
            CollectionAssert.AreEquivalent(
                new[] { PosicaoEquipamento.Capacete },
                LogicaInventario.EncaixesValidos(capacete, estado)
            );
            // Resposta sem o nível (0): a tela não recusa, o servidor decide.
            estado.NivelDoPersonagem = 0;
            Assert.IsNull(
                LogicaInventario.RecusaNoEncaixe(capacete, PosicaoEquipamento.Capacete, estado)
            );
        }

        [Test]
        public void CliqueDireitoNaBolsaEquipaNoEncaixeAutomatico()
        {
            var estado = new EquipamentosResponse
            {
                Equipados = new[] { Vestido(9, Anel, PosicaoEquipamento.Anel1) },
            };
            var acao = LogicaInventario.CliqueDireito(NaBolsa(1, Anel, 4), estado);
            Assert.AreEqual(TipoAcao.Equipar, acao.Tipo);
            Assert.AreEqual(PosicaoEquipamento.Anel2, acao.Posicao);
        }

        [Test]
        public void CliqueDireitoNoPaperdollDesequipaSemDestino()
        {
            var acao = LogicaInventario.CliqueDireito(
                Vestido(9, Capacete, PosicaoEquipamento.Capacete),
                new EquipamentosResponse()
            );
            Assert.AreEqual(TipoAcao.Desequipar, acao.Tipo);
            Assert.AreEqual(PosicaoEquipamento.Capacete, acao.Posicao);
            Assert.IsFalse(acao.TemDestino);
        }

        [Test]
        public void CliqueDireitoEmItemQueNaoEhEquipamentoRecusa()
        {
            var pocao = new EntradaInventario
            {
                InstanciaId = 5,
                ItemId = Pocao,
                Aba = 0,
                Casa = 1,
            };
            Assert.AreEqual(
                TipoAcao.Recusada,
                LogicaInventario.CliqueDireito(pocao, new EquipamentosResponse()).Tipo
            );
        }

        [Test]
        public void CliqueDireitoUsaOConjuntoAtivoParaArmas()
        {
            var estado = new EquipamentosResponse { ConjuntoAtivo = 2 };
            var acao = LogicaInventario.CliqueDireito(
                NaBolsa(1, CatalogoItens.EspadaDeTeste, 0),
                estado
            );
            Assert.AreEqual(PosicaoEquipamento.Arma2, acao.Posicao);
        }

        [Test]
        public void ItensEnvolvidosIncluemOOcupanteDoDestino()
        {
            var a = NaBolsa(1, Anel, 3);
            var b = NaBolsa(2, Capacete, 9);
            var estado = new EquipamentosResponse { Inventario = new[] { a, b } };
            var acao = Soltar(a, AlvoSoltura.EmCasa(0, 9));
            CollectionAssert.AreEquivalent(
                new long[] { 1, 2 },
                LogicaInventario.InstanciasEnvolvidas(acao, estado)
            );
        }
    }
}
