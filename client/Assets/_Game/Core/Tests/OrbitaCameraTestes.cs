using JogoDoGrupo.Client.Core.Jogo.Controle;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class OrbitaCameraTestes
    {
        private const float Tolerancia = 0.001f;

        [Test]
        public void Girar_MouseParaDireita_AumentaHorizontal()
        {
            var orbita = new OrbitaCamera();
            orbita.Definir(10f, 0f);

            orbita.Girar(new Vector2(5f, 0f));

            Assert.AreEqual(15f, orbita.Horizontal, Tolerancia);
        }

        [Test]
        public void Horizontal_DaAVoltaEm360()
        {
            var orbita = new OrbitaCamera();
            orbita.Definir(350f, 0f);

            orbita.Girar(new Vector2(20f, 0f));

            Assert.AreEqual(10f, orbita.Horizontal, Tolerancia);
        }

        [Test]
        public void Vertical_RespeitaOsLimites()
        {
            var orbita = new OrbitaCamera { VerticalMinimo = -30f, VerticalMaximo = 70f };

            orbita.Girar(new Vector2(0f, -500f));
            Assert.AreEqual(70f, orbita.Vertical, Tolerancia);

            orbita.Girar(new Vector2(0f, 500f));
            Assert.AreEqual(-30f, orbita.Vertical, Tolerancia);
        }

        [Test]
        public void Posicao_SemInclinacao_FicaAtrasDoPivo()
        {
            var orbita = new OrbitaCamera();
            orbita.Definir(0f, 0f);
            var pivo = new Vector3(1f, 2f, 3f);

            var posicao = orbita.Posicao(pivo, 4f);

            Assert.AreEqual(1f, posicao.x, Tolerancia);
            Assert.AreEqual(2f, posicao.y, Tolerancia);
            Assert.AreEqual(-1f, posicao.z, Tolerancia);
        }

        [Test]
        public void Posicao_OlhandoDeCima_FicaAcimaDoPivo()
        {
            var orbita = new OrbitaCamera();
            orbita.Definir(0f, 45f);

            var posicao = orbita.Posicao(Vector3.zero, 4f);

            Assert.Greater(posicao.y, 0f);
            Assert.AreEqual(4f, posicao.magnitude, Tolerancia);
        }

        [Test]
        public void Pivo_FicaNaAlturaDaCabeca()
        {
            var orbita = new OrbitaCamera { AlturaPivo = 1.6f };

            var pivo = orbita.Pivo(new Vector3(0f, 1f, 0f));

            Assert.AreEqual(2.6f, pivo.y, Tolerancia);
        }

        private const float Frame = 1f / 60f;

        [Test]
        public void Zoom_RespeitaOsLimites()
        {
            var orbita = new OrbitaCamera();

            orbita.Zoom(100f);
            Assert.AreEqual(OrbitaCamera.ZoomMinimo, orbita.Distancia, Tolerancia);

            orbita.Zoom(-100f);
            Assert.AreEqual(OrbitaCamera.ZoomMaximo, orbita.Distancia, Tolerancia);
        }

        [Test]
        public void Zoom_UmCliqueParaFrente_AproximaUmPasso()
        {
            var orbita = new OrbitaCamera();

            orbita.Zoom(1f);

            Assert.AreEqual(
                OrbitaCamera.ZoomPadrao - OrbitaCamera.PassoZoom,
                orbita.Distancia,
                Tolerancia
            );
        }

        [Test]
        public void Distancia_ObstaculoAparece_AproximaRapidoSemEntrarNele()
        {
            var orbita = new OrbitaCamera();

            var primeiro = orbita.AtualizarDistancia(1f, Frame);
            Assert.LessOrEqual(primeiro, 1f + OrbitaCamera.FolgaAtrasDoObstaculo + Tolerancia);

            for (var i = 0; i < 15; i++)
            {
                orbita.AtualizarDistancia(1f, Frame);
            }

            Assert.AreEqual(1f, orbita.DistanciaAtual, 0.01f);
        }

        [Test]
        public void Distancia_ObstaculoSai_RecuaDevagarSemSalto()
        {
            var orbita = new OrbitaCamera();
            for (var i = 0; i < 60; i++)
            {
                orbita.AtualizarDistancia(1f, Frame);
            }

            var depoisDeUmFrame = orbita.AtualizarDistancia(float.PositiveInfinity, Frame);
            Assert.Less(depoisDeUmFrame - 1f, 0.25f, "recuou de uma vez");

            for (var i = 0; i < 300; i++)
            {
                orbita.AtualizarDistancia(float.PositiveInfinity, Frame);
            }

            Assert.AreEqual(OrbitaCamera.ZoomPadrao, orbita.DistanciaAtual, 0.01f);
        }

        [Test]
        public void Distancia_NaoPassaDoZoom()
        {
            var orbita = new OrbitaCamera();
            orbita.Zoom(1f);

            for (var i = 0; i < 300; i++)
            {
                orbita.AtualizarDistancia(float.PositiveInfinity, Frame);
            }

            Assert.AreEqual(orbita.Distancia, orbita.DistanciaAtual, 0.01f);
        }

        [Test]
        public void PivoFiltrado_Degrau_SobeAosPoucos()
        {
            var orbita = new OrbitaCamera { AlturaPivo = 0f };
            orbita.Reiniciar(Vector3.zero);

            var pivo = orbita.PivoFiltrado(new Vector3(0f, 0.4f, 0f), Frame);
            Assert.Greater(pivo.y, 0f);
            Assert.Less(pivo.y, 0.2f);

            for (var i = 0; i < 120; i++)
            {
                pivo = orbita.PivoFiltrado(new Vector3(0f, 0.4f, 0f), Frame);
            }

            Assert.AreEqual(0.4f, pivo.y, 0.001f);
        }

        [Test]
        public void PivoFiltrado_HorizontalSegueMaisRapidoQueVertical()
        {
            var orbita = new OrbitaCamera { AlturaPivo = 0f };
            orbita.Reiniciar(Vector3.zero);

            var pivo = orbita.PivoFiltrado(new Vector3(1f, 1f, 0f), Frame);

            Assert.Greater(pivo.x, pivo.y);
        }

        [Test]
        public void PivoFiltrado_Teleporte_VaiDireto()
        {
            var orbita = new OrbitaCamera { AlturaPivo = 0f };
            orbita.Reiniciar(Vector3.zero);
            var destino = new Vector3(50f, 10f, 0f);

            var pivo = orbita.PivoFiltrado(destino, Frame);

            Assert.AreEqual(destino, pivo);
        }
    }
}
