using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>Tecla, intenção e predição da esquiva (os números vêm de RegrasEsquiva).</summary>
    public class EsquivaTestes
    {
        [Test]
        public void Tecla_EhOAltEsquerdo()
        {
            Assert.AreEqual("<Keyboard>/leftAlt", ApresentacaoEsquiva.TeclaPadrao);
        }

        [Test]
        public void Gerador_NumeraENormaliza()
        {
            var gerador = new GeradorDeEsquivas();

            var a = gerador.Criar(-90f);
            var b = gerador.Criar(450f);

            Assert.AreEqual(1, a.Sequencia);
            Assert.AreEqual(270f, a.DirecaoY, 0.001f);
            Assert.AreEqual(2, b.Sequencia);
            Assert.AreEqual(90f, b.DirecaoY, 0.001f);
        }

        [Test]
        public void Direcao_ParadoSaiParaTrasDaMira()
        {
            Assert.AreEqual(210f, ApresentacaoEsquiva.DirecaoDoPedido(Vector2.zero, 30f), 0.001f);
        }

        [TestCase(0f, 1f, 0f, 0f)]
        [TestCase(1f, 0f, 0f, 90f)]
        [TestCase(0f, -1f, 0f, 180f)]
        [TestCase(-1f, 0f, 0f, 270f)]
        [TestCase(0f, 1f, 90f, 90f)]
        public void Direcao_MovendoSegueOInputRelativoACamera(
            float x,
            float y,
            float camera,
            float esperado
        )
        {
            Assert.AreEqual(
                esperado,
                ApresentacaoEsquiva.DirecaoDoPedido(new Vector2(x, y), camera),
                0.001f
            );
        }

        [Test]
        public void PodePedir_ExigeConexaoCursorPresoESemEsquivaAtiva()
        {
            Assert.IsTrue(ApresentacaoEsquiva.PodePedir(true, true, false));
            Assert.IsFalse(ApresentacaoEsquiva.PodePedir(false, true, false));
            Assert.IsFalse(ApresentacaoEsquiva.PodePedir(true, false, false));
            Assert.IsFalse(ApresentacaoEsquiva.PodePedir(true, true, true));
        }

        [Test]
        public void TextoDaRecusa_TraduzCodigosEGenerico()
        {
            Assert.AreEqual(
                ApresentacaoEsquiva.TextoVigorInsuficiente,
                ApresentacaoEsquiva.TextoDaRecusa(CodigosErroCombate.VigorInsuficiente)
            );
            Assert.AreEqual(
                ApresentacaoEsquiva.TextoAcaoEmAndamento,
                ApresentacaoEsquiva.TextoDaRecusa(CodigosErroCombate.AcaoEmAndamento)
            );
            Assert.AreEqual(
                ApresentacaoEsquiva.TextoVoceEstaCaido,
                ApresentacaoEsquiva.TextoDaRecusa(CodigosErroCombate.VoceEstaCaido)
            );
            Assert.AreEqual(
                ApresentacaoEsquiva.TextoRecusaGenerica,
                ApresentacaoEsquiva.TextoDaRecusa("Outro")
            );
        }

        [Test]
        public void EhMinha_SoComIdValidoIgual()
        {
            Assert.IsTrue(ApresentacaoEsquiva.EhMinha(7, 7));
            Assert.IsFalse(ApresentacaoEsquiva.EhMinha(8, 7));
            Assert.IsFalse(ApresentacaoEsquiva.EhMinha(0, 0));
        }

        // ── Predição ───────────────────────────────────────────────────────────

        [Test]
        public void Predicao_PercorreADistanciaDasRegrasEPara()
        {
            var predicao = new PredicaoDeEsquiva();
            predicao.Iniciar(1, 0, 0, 90f); // +X

            double somaX = 0;
            double somaZ = 0;
            for (var i = 0; i < 100 && predicao.Ativa; i++)
            {
                predicao.Avancar(0.016, out var px, out var pz);
                somaX += px;
                somaZ += pz;
            }

            Assert.IsFalse(predicao.Ativa);
            Assert.AreEqual(RegrasEsquiva.DistanciaMetros, somaX, 1e-6);
            Assert.AreEqual(0.0, somaZ, 1e-6);
        }

        [Test]
        public void Predicao_NaMetadeDaDuracaoAndaMetadeDaDistancia()
        {
            var predicao = new PredicaoDeEsquiva();
            predicao.Iniciar(1, 0, 0, 0f); // +Z

            predicao.Avancar(RegrasEsquiva.DuracaoSegundos / 2, out _, out var pz);

            Assert.IsTrue(predicao.Ativa);
            Assert.AreEqual(RegrasEsquiva.DistanciaMetros / 2, pz, 1e-6);
        }

        [Test]
        public void Predicao_PassoGrandeNaoPassaDoLimite()
        {
            var predicao = new PredicaoDeEsquiva();
            predicao.Iniciar(1, 0, 0, 0f);

            predicao.Avancar(10.0, out _, out var pz);

            Assert.AreEqual(RegrasEsquiva.DistanciaMetros, pz, 1e-6);
            Assert.IsFalse(predicao.Ativa);
        }

        [Test]
        public void Predicao_SemEsquivaAtivaNaoAnda()
        {
            var predicao = new PredicaoDeEsquiva();

            predicao.Avancar(0.1, out var px, out var pz);

            Assert.AreEqual(0.0, px);
            Assert.AreEqual(0.0, pz);
        }

        [Test]
        public void Recusa_DaMesmaSequenciaDevolveOInicioECancela()
        {
            var predicao = new PredicaoDeEsquiva();
            predicao.Iniciar(3, 12.5, -4.0, 0f);
            predicao.Avancar(0.2, out _, out _);

            var desfez = predicao.Recusar(3, out var x, out var z);

            Assert.IsTrue(desfez);
            Assert.AreEqual(12.5, x);
            Assert.AreEqual(-4.0, z);
            Assert.IsFalse(predicao.Ativa);
        }

        [Test]
        public void Recusa_DeOutraSequenciaOuSemAtividadeNaoDesfaz()
        {
            var predicao = new PredicaoDeEsquiva();
            predicao.Iniciar(3, 0, 0, 0f);

            Assert.IsFalse(predicao.Recusar(2, out _, out _));
            Assert.IsTrue(predicao.Ativa);

            predicao.Avancar(10.0, out _, out _);
            Assert.IsFalse(predicao.Recusar(3, out _, out _));
        }

        [Test]
        public void Cancelar_ParaSemDevolver()
        {
            var predicao = new PredicaoDeEsquiva();
            predicao.Iniciar(1, 0, 0, 0f);

            predicao.Cancelar();

            Assert.IsFalse(predicao.Ativa);
        }
    }
}
