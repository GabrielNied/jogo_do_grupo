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
    }
}
