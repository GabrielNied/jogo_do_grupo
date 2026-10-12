using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>Escolha do efeito de impacto, limites e detecção de esquiva (sem Unity em cena).</summary>
    public class ImpactoTestes
    {
        [Test]
        public void GolpeProprioLeve_TemFlashESomMasSemHitStopNemTremor()
        {
            Assert.IsTrue(EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Leve, 10, out var e));

            Assert.AreEqual(OrigemDoImpacto.GolpeLeve, e.Origem);
            Assert.Greater(e.FlashSegundos, 0f);
            Assert.AreEqual(0f, e.HitStopSegundos);
            Assert.AreEqual(0f, e.TremorAmplitude);
            Assert.AreEqual(SomDeCombate.AcertoLeve, e.Som);
        }

        [Test]
        public void GolpeProprioPesado_EMaisForteQueOLeve()
        {
            EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Leve, 10, out var leve);
            EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Pesado, 10, out var pesado);

            Assert.Greater(pesado.FlashSegundos, leve.FlashSegundos);
            Assert.Greater(pesado.RecuoMetros, leve.RecuoMetros);
            Assert.Greater(pesado.HitStopSegundos, 0f);
            Assert.Greater(pesado.TremorAmplitude, 0f);
            Assert.AreEqual(SomDeCombate.AcertoPesado, pesado.Som);
        }

        [Test]
        public void GolpeProprioSemDano_NaoGeraEfeito()
        {
            Assert.IsFalse(EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Pesado, 0, out _));
            Assert.IsFalse(EscolhaDeEfeito.DoGolpeProprio(TipoDeAtaque.Leve, -3, out _));
            Assert.IsFalse(EscolhaDeEfeito.DoGolpeDeOutro(0, out _));
        }

        [Test]
        public void GolpeDeOutro_FicaDiscreto()
        {
            Assert.IsTrue(EscolhaDeEfeito.DoGolpeDeOutro(10, out var e));

            Assert.AreEqual(OrigemDoImpacto.GolpeDeOutro, e.Origem);
            Assert.AreEqual(0f, e.HitStopSegundos);
            Assert.AreEqual(0f, e.TremorAmplitude);
            Assert.AreEqual(0f, e.RecuoMetros);
            Assert.Less(e.VolumeDoSom, EscolhaDeEfeito.VolumeLeve);
        }

        [Test]
        public void GolpeRecebido_SoQuandoAVidaCai()
        {
            Assert.IsFalse(EscolhaDeEfeito.DoGolpeRecebido(100, 100, 100, out _));
            Assert.IsFalse(EscolhaDeEfeito.DoGolpeRecebido(80, 100, 100, out _));
            Assert.IsFalse(EscolhaDeEfeito.DoGolpeRecebido(100, 90, 0, out _));
            Assert.IsTrue(EscolhaDeEfeito.DoGolpeRecebido(100, 95, 100, out _));
        }

        [Test]
        public void GolpeRecebido_FracoTemTremorLeveESemHitStop()
        {
            EscolhaDeEfeito.DoGolpeRecebido(100, 95, 100, out var e);

            Assert.AreEqual(OrigemDoImpacto.GolpeRecebido, e.Origem);
            Assert.AreEqual(0f, e.HitStopSegundos);
            Assert.AreEqual(EscolhaDeEfeito.TremorRecebido, e.TremorAmplitude);
            Assert.AreEqual(SomDeCombate.GolpeRecebido, e.Som);
        }

        [Test]
        public void GolpeRecebido_ForteNoLimiteTemHitStopETremorMaior()
        {
            EscolhaDeEfeito.DoGolpeRecebido(100, 80, 100, out var e);

            Assert.AreEqual(OrigemDoImpacto.GolpeRecebidoForte, e.Origem);
            Assert.Greater(e.HitStopSegundos, 0f);
            Assert.AreEqual(EscolhaDeEfeito.TremorRecebidoForte, e.TremorAmplitude);
        }

        [Test]
        public void Limite_FlashesNaoPassamDoMaximo()
        {
            var limite = new LimiteDeEfeitos();
            for (var i = 0; i < LimiteDeEfeitos.MaximoDeFlashes + 5; i++)
            {
                limite.RegistrarFlash(1f);
            }

            Assert.AreEqual(LimiteDeEfeitos.MaximoDeFlashes, limite.FlashesAtivos);
            Assert.IsFalse(limite.RegistrarFlash(0f));
            limite.Atualizar(2f);
            Assert.AreEqual(0, limite.FlashesAtivos);
        }

        [Test]
        public void Limite_SonsNaMesmaJanelaSaoDescartados()
        {
            var limite = new LimiteDeEfeitos();
            for (var i = 0; i < LimiteDeEfeitos.MaximoDeSons; i++)
            {
                Assert.IsTrue(limite.PodeTocarSom());
            }

            Assert.IsFalse(limite.PodeTocarSom());
            limite.Atualizar(LimiteDeEfeitos.JanelaDosSonsSegundos + 0.01f);
            Assert.IsTrue(limite.PodeTocarSom());
        }

        [Test]
        public void Limite_HitStopENaoSomaEOMaiorVale()
        {
            var limite = new LimiteDeEfeitos();
            limite.PedirHitStop(0.07f);
            limite.PedirHitStop(0.05f);
            limite.PedirHitStop(10f);

            Assert.AreEqual(LimiteDeEfeitos.HitStopMaximo, limite.HitStopRestante, 0.0001f);
            limite.Atualizar(1f);
            Assert.AreEqual(0f, limite.HitStopRestante);
        }

        [Test]
        public void Limite_TremorMaiorSubstituiMenorENaoPassaDoMaximo()
        {
            var limite = new LimiteDeEfeitos();
            limite.PedirTremor(0.04f, 0.2f);
            limite.PedirTremor(5f, 0.2f);
            Assert.AreEqual(LimiteDeEfeitos.TremorMaximo, limite.TremorAtual, 0.0001f);

            limite.Atualizar(0.1f);
            Assert.Less(limite.TremorAtual, LimiteDeEfeitos.TremorMaximo);
            limite.Atualizar(0.2f);
            Assert.AreEqual(0f, limite.TremorAtual);
        }

        [Test]
        public void Esquiva_DentroDaAreaEsquivaEFora_ContaUmaEsquiva()
        {
            var d = new DetectorDeEsquiva();
            Assert.IsTrue(d.AoAvisoDeGolpe(0f, 1f, Area(0, 0, 3), 0f, 0f));
            d.AoEsquivar(0.3f);

            Assert.AreEqual(0, d.Atualizar(1f, 10f, 0f));
            Assert.AreEqual(1, d.Atualizar(1.5f, 10f, 0f));
            Assert.AreEqual(0, d.Pendentes);
        }

        [Test]
        public void Esquiva_ForaDaAreaNoAviso_NaoVale()
        {
            var d = new DetectorDeEsquiva();

            Assert.IsFalse(d.AoAvisoDeGolpe(0f, 1f, Area(0, 0, 3), 20f, 0f));
            Assert.AreEqual(0, d.Pendentes);
        }

        [Test]
        public void Esquiva_SemEsquivar_NaoConta()
        {
            var d = new DetectorDeEsquiva();
            d.AoAvisoDeGolpe(0f, 1f, Area(0, 0, 3), 0f, 0f);

            Assert.AreEqual(0, d.Atualizar(2f, 10f, 0f));
        }

        [Test]
        public void Esquiva_AindaDentroNoGolpe_NaoConta()
        {
            var d = new DetectorDeEsquiva();
            d.AoAvisoDeGolpe(0f, 1f, Area(0, 0, 3), 0f, 0f);
            d.AoEsquivar(0.3f);

            Assert.AreEqual(0, d.Atualizar(1f, 1f, 0f));
            Assert.AreEqual(0, d.Atualizar(2f, 10f, 0f));
        }

        [Test]
        public void Esquiva_VidaPerdidaNoGolpe_Cancela()
        {
            var d = new DetectorDeEsquiva();
            d.AoAvisoDeGolpe(0f, 1f, Area(0, 0, 3), 0f, 0f);
            d.AoEsquivar(0.3f);
            d.Atualizar(1f, 10f, 0f);

            d.AoPerderVida(1.1f);

            Assert.AreEqual(0, d.Atualizar(2f, 10f, 0f));
        }

        [Test]
        public void Esquiva_DepoisDoGolpe_NaoVale()
        {
            var d = new DetectorDeEsquiva();
            d.AoAvisoDeGolpe(0f, 1f, Area(0, 0, 3), 0f, 0f);
            d.AoEsquivar(1.5f);

            Assert.AreEqual(0, d.Atualizar(2f, 10f, 0f));
        }

        [Test]
        public void Sons_TodosGeramAmostrasValidas()
        {
            foreach (SomDeCombate som in System.Enum.GetValues(typeof(SomDeCombate)))
            {
                var amostras = SintetizadorDeSons.Gerar(som);
                if (som == SomDeCombate.Nenhum)
                {
                    Assert.IsEmpty(amostras);
                    continue;
                }

                Assert.Greater(amostras.Length, 0, som.ToString());
                foreach (var a in amostras)
                {
                    Assert.IsFalse(float.IsNaN(a));
                    Assert.LessOrEqual(System.Math.Abs(a), 1f);
                }
            }
        }

        private static (float x, float z, float raio)[] Area(float x, float z, float raio) =>
            new[] { (x, z, raio) };
    }
}
