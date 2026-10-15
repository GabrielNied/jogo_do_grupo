using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    public class RespostaVisualTestes
    {
        private GameObject _boneco;

        [TearDown]
        public void Limpar()
        {
            if (_boneco != null)
            {
                Object.DestroyImmediate(_boneco);
            }
        }

        // ── Boneco de treino e registro de alvos (#476) ──────────────────────────

        [Test]
        public void Registro_AchaOBonecoDeTreinoPeloIdNegativo()
        {
            var boneco = BonecoDeTreinoVisual.Criar();
            _boneco = boneco.gameObject;

            var achado = RegistroDeAlvos.Achar(RegrasBonecoDeTreino.AlvoId);

            Assert.AreSame(_boneco, achado);
            Assert.AreEqual((float)RegrasBonecoDeTreino.X, _boneco.transform.position.x, 0.01f);
            Assert.AreEqual((float)RegrasBonecoDeTreino.Z, _boneco.transform.position.z, 0.01f);
        }

        [Test]
        public void Registro_AlvoSemObjeto_DevolveNulo()
        {
            Assert.IsNull(RegistroDeAlvos.Achar(987654));
        }

        [Test]
        public void Boneco_BarraCaiNoGolpe_EVoltaCheiaDepoisDeCincoSegundos()
        {
            var boneco = BonecoDeTreinoVisual.Criar();
            _boneco = boneco.gameObject;
            var vida = RegrasBonecoDeTreino.Vida;

            boneco.AplicarAcerto(
                new AcertoDeAtaque
                {
                    AlvoId = RegrasBonecoDeTreino.AlvoId,
                    Dano = 20,
                    VidaRestante = vida - 20,
                    VidaMaxima = vida,
                },
                10f
            );
            Assert.AreEqual((vida - 20f) / vida, boneco.Fracao, 0.001f);

            boneco.Atualizar(10f + (float)RegrasBonecoDeTreino.SegundosParaRecuperar - 0.1f);
            Assert.Less(boneco.Fracao, 1f);

            boneco.Atualizar(10f + (float)RegrasBonecoDeTreino.SegundosParaRecuperar);
            Assert.AreEqual(1f, boneco.Fracao, 0.001f);
        }

        [Test]
        public void Boneco_IgnoraAcertoDeOutroAlvo()
        {
            var boneco = BonecoDeTreinoVisual.Criar();
            _boneco = boneco.gameObject;

            boneco.AplicarAcerto(
                new AcertoDeAtaque
                {
                    AlvoId = 3,
                    Dano = 50,
                    VidaRestante = 1,
                    VidaMaxima = 100,
                },
                1f
            );

            Assert.AreEqual(1f, boneco.Fracao, 0.001f);
        }

        // ── Número de dano sobre o alvo (#482) ───────────────────────────────────

        [Test]
        public void Numero_NasceSobreACabecaDoAlvo_ESobeComOProgresso()
        {
            var boneco = BonecoDeTreinoVisual.Criar();
            _boneco = boneco.gameObject;

            var origem = RegistroDeAlvos.AcimaDaCabeca(_boneco);

            Assert.AreEqual(_boneco.transform.position.x, origem.x, 0.5f);
            Assert.AreEqual(_boneco.transform.position.z, origem.z, 0.5f);
            Assert.Greater(origem.y, _boneco.transform.position.y + 1.5f);

            var depois = ApresentacaoAtaque.PosicaoDoNumero(origem, 1f, 0.9f);
            Assert.AreEqual(origem.y + 0.9f, depois.y, 0.001f);
            Assert.AreEqual(origem.x, depois.x, 0.0001f);
        }

        [Test]
        public void Fila_GuardaAOrigemDoNumero()
        {
            var fila = new FilaDeNumeros();
            fila.Adicionar("20", false, 0f, new Vector3(1f, 2f, 3f));
            fila.Adicionar("21", false, 0f);

            Assert.AreEqual(new Vector3(1f, 2f, 3f), fila.Itens[0].Origem);
            Assert.IsNull(fila.Itens[1].Origem);
        }

        [Test]
        public void EfeitoDoPesado_VaiPeloTipoDoAcerto_NaoPeloUltimoGolpeIniciado()
        {
            // Com um leve na fila depois do pesado, o acerto do pesado ainda é do pesado.
            Assert.IsTrue(EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Pesado, 44, out var pesado));
            Assert.IsTrue(EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Leve, 20, out var leve));
            Assert.AreEqual(OrigemDoImpacto.GolpePesado, pesado.Origem);
            Assert.AreEqual(OrigemDoImpacto.GolpeLeve, leve.Origem);
            Assert.IsTrue(ApresentacaoAtaque.GolpeForte(TipoDeAtaque.Pesado, 0));
            Assert.IsTrue(ApresentacaoAtaque.GolpeForte(TipoDeAtaque.Leve, 2));
            Assert.IsFalse(ApresentacaoAtaque.GolpeForte(TipoDeAtaque.Leve, 1));
        }

        // ── Golpe no vazio ───────────────────────────────────────────────────────

        [Test]
        public void GolpeSemAcerto_VenceDepoisDoContato_ComTolerancia()
        {
            var golpes = new GolpesEmAberto();
            golpes.Registrar(5, TipoDeAtaque.Leve, 90f, 10);

            var prazo = 10 + RegrasAtaque.PreparoSegundos(TipoDeAtaque.Leve);
            Assert.IsFalse(golpes.TirarVencido(prazo, out _));
            Assert.IsTrue(
                golpes.TirarVencido(
                    prazo + GolpesEmAberto.ToleranciaDeRedeSegundos,
                    out var vencido
                )
            );
            Assert.AreEqual(5, vencido.Sequencia);
            Assert.AreEqual(90f, vencido.DirecaoY);
        }

        [Test]
        public void GolpeConfirmado_NaoViraGolpeNoVazio_EALigacaoESPelaSequencia()
        {
            var golpes = new GolpesEmAberto();
            golpes.Registrar(1, TipoDeAtaque.Leve, 0f, 10);
            golpes.Registrar(2, TipoDeAtaque.Pesado, 0f, 10.6);

            Assert.IsTrue(golpes.Confirmar(2, out var golpe));
            Assert.AreEqual(TipoDeAtaque.Pesado, golpe.Tipo);
            Assert.IsFalse(golpes.Confirmar(2, out _));
            Assert.AreEqual(1, golpes.Quantidade);
        }

        [Test]
        public void Rastro_ApareceCheioESomeSemVoltar()
        {
            Assert.AreEqual(RastroDoGolpe.OpacidadeInicial, RastroDoGolpe.Opacidade(0f), 0.0001f);
            Assert.Less(RastroDoGolpe.Opacidade(0.1f), RastroDoGolpe.Opacidade(0.05f));
            Assert.AreEqual(0f, RastroDoGolpe.Opacidade(RastroDoGolpe.DuracaoSegundos));
        }

        // ── Pausa de pose (#812) ─────────────────────────────────────────────────

        [Test]
        public void DeveSegurarPose_InimigoDeMobs_NaoSeguraPoseNoCombate()
        {
            var inimigo = new GameObject(RegistroDeAlvos.NomeDoInimigo + "123");
            try
            {
                inimigo.AddComponent<Animator>();
                Assert.IsFalse(ControleImpacto.DeveSegurarPose(inimigo));
            }
            finally
            {
                Object.DestroyImmediate(inimigo);
            }
        }

        [Test]
        public void DeveSegurarPose_BonecoDeTreinoOuJogador_SeguraPose()
        {
            var jogador = new GameObject("Jogador");
            try
            {
                jogador.AddComponent<Animator>();
                Assert.IsTrue(ControleImpacto.DeveSegurarPose(jogador));
            }
            finally
            {
                Object.DestroyImmediate(jogador);
            }
        }
    }
}
