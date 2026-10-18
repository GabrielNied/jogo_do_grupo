using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    public class ProjetilVisualTestes
    {
        private readonly List<GameObject> _criados = new List<GameObject>();

        [TearDown]
        public void Limpar()
        {
            foreach (var go in _criados)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _criados.Clear();
        }

        [Test]
        public void ProjetilVisual_ConfiguracaoInicial_AlinhaDirecaoERotacao()
        {
            var msg = new ProjetilLancado
            {
                ProjetilId = 1,
                AtacanteId = 10,
                OrigemX = 5f,
                OrigemY = 1.2f,
                OrigemZ = 10f,
                DirecaoX = 0f,
                DirecaoY = 0f,
                DirecaoZ = 1f,
                Velocidade = 35f,
                AlcanceMaximo = 30f,
            };

            var projetil = ProjetilVisual.Criar(msg, null);
            _criados.Add(projetil.gameObject);

            Assert.AreEqual(1, projetil.Id);
            Assert.AreEqual(10, projetil.AtacanteId);
            Assert.AreEqual(35f, projetil.Velocidade, 0.001f);
            Assert.AreEqual(30f, projetil.AlcanceMaximo, 0.001f);
            Assert.AreEqual(5f, projetil.transform.position.x, 0.001f);
            Assert.AreEqual(1.2f, projetil.transform.position.y, 0.001f);
            Assert.AreEqual(10f, projetil.transform.position.z, 0.001f);
            Assert.AreEqual(Vector3.forward, projetil.Direcao);
        }

        [Test]
        public void ProjetilVisual_Avancar_DeslocaNaDirecaoConformeVelocidade()
        {
            var msg = new ProjetilLancado
            {
                ProjetilId = 2,
                AtacanteId = 10,
                OrigemX = 0f,
                OrigemY = 0f,
                OrigemZ = 0f,
                DirecaoX = 1f,
                DirecaoY = 0f,
                DirecaoZ = 0f,
                Velocidade = 40f,
                AlcanceMaximo = 50f,
            };

            var projetil = ProjetilVisual.Criar(msg, null);
            _criados.Add(projetil.gameObject);

            projetil.Avancar(0.1f);

            Assert.AreEqual(4f, projetil.transform.position.x, 0.01f);
            Assert.AreEqual(4f, projetil.DistanciaPercorrida, 0.01f);
        }

        [Test]
        public void ProjetilVisual_AlcanceMaximo_DisparaImpactoEDestruicao()
        {
            var destruiu = false;
            var msg = new ProjetilLancado
            {
                ProjetilId = 3,
                AtacanteId = 10,
                OrigemX = 0f,
                OrigemY = 0f,
                OrigemZ = 0f,
                DirecaoX = 0f,
                DirecaoY = 0f,
                DirecaoZ = 1f,
                Velocidade = 50f,
                AlcanceMaximo = 10f,
            };

            var projetil = ProjetilVisual.Criar(msg, null, _ => destruiu = true);
            _criados.Add(projetil.gameObject);

            projetil.Avancar(0.25f); // 50 * 0.25 = 12.5m >= 10m

            Assert.IsTrue(destruiu);
        }

        [Test]
        public void SintetizadorDeSons_GeraAmostrasParaDisparoEImpactoFlecha()
        {
            var amostrasDisparo = SintetizadorDeSons.Gerar(SomDeCombate.DisparoFlecha);
            var amostrasImpacto = SintetizadorDeSons.Gerar(SomDeCombate.ImpactoFlecha);

            Assert.IsNotNull(amostrasDisparo);
            Assert.Greater(amostrasDisparo.Length, 100);

            Assert.IsNotNull(amostrasImpacto);
            Assert.Greater(amostrasImpacto.Length, 100);
        }

        [Test]
        public void CriarEfeitoImpacto_InstanciaParticleSystemSemExcecaoParaTodosOsTipos()
        {
            var tipos = new[]
            {
                TipoDeProjetil.BolaDeFogo,
                TipoDeProjetil.EsferaDeGelo,
                TipoDeProjetil.OrbeDeCura,
                TipoDeProjetil.Flecha,
            };

            foreach (var tipo in tipos)
            {
                Assert.DoesNotThrow(() =>
                {
                    VisualProjetilElemental.CriarEfeitoImpacto(Vector3.zero, tipo);
                });
            }
        }
    }
}
