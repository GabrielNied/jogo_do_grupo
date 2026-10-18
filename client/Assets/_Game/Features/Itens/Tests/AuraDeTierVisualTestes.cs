using JogoDoGrupo.Client.Features.Itens.Visual;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    [TestFixture]
    public class AuraDeTierVisualTestes
    {
        private GameObject _alvo;

        [SetUp]
        public void Montar()
        {
            _alvo = new GameObject("AlvoTeste");
        }

        [TearDown]
        public void Desmontar()
        {
            if (_alvo != null)
            {
                Object.DestroyImmediate(_alvo);
            }
        }

        [Test]
        public void ConfigAuraTier_ApenasTierBIgualOuMaiorTemAura()
        {
            Assert.IsFalse(ConfigAuraTier.TemAura(TierItem.D));
            Assert.IsFalse(ConfigAuraTier.TemAura(TierItem.C));
            Assert.IsTrue(ConfigAuraTier.TemAura(TierItem.B));
            Assert.IsTrue(ConfigAuraTier.TemAura(TierItem.A));
            Assert.IsTrue(ConfigAuraTier.TemAura(TierItem.S));
            Assert.IsTrue(ConfigAuraTier.TemAura(TierItem.SMais));
        }

        [Test]
        public void ConfigAuraTier_IntensidadesCrescemComOTier()
        {
            var intB = ConfigAuraTier.IntensidadeChao(TierItem.B);
            var intA = ConfigAuraTier.IntensidadeChao(TierItem.A);
            var intS = ConfigAuraTier.IntensidadeChao(TierItem.S);
            var intSMais = ConfigAuraTier.IntensidadeChao(TierItem.SMais);

            Assert.Greater(intA, intB);
            Assert.Greater(intS, intA);
            Assert.Greater(intSMais, intS);

            var armB = ConfigAuraTier.IntensidadeArma(TierItem.B);
            var armA = ConfigAuraTier.IntensidadeArma(TierItem.A);
            var armS = ConfigAuraTier.IntensidadeArma(TierItem.S);
            var armSMais = ConfigAuraTier.IntensidadeArma(TierItem.SMais);

            Assert.Greater(armA, armB);
            Assert.Greater(armS, armA);
            Assert.Greater(armSMais, armS);
        }

        [Test]
        public void ConfigAuraTier_CoresCondizemComTabela()
        {
            Assert.AreEqual(Color.clear, ConfigAuraTier.Cor(TierItem.D));
            Assert.AreEqual(Color.clear, ConfigAuraTier.Cor(TierItem.C));
            Assert.AreEqual(ConfigAuraTier.CorTierB, ConfigAuraTier.Cor(TierItem.B));
            Assert.AreEqual(ConfigAuraTier.CorTierA, ConfigAuraTier.Cor(TierItem.A));
            Assert.AreEqual(ConfigAuraTier.CorTierS, ConfigAuraTier.Cor(TierItem.S));
            Assert.AreEqual(ConfigAuraTier.CorTierSMais, ConfigAuraTier.Cor(TierItem.SMais));
        }

        [Test]
        public void Aplicar_ItemSemAura_NaoCriaComponenteNemLuz()
        {
            var resultado = AuraDeTierVisual.Aplicar(_alvo, TierItem.D, noChao: true);

            Assert.IsNull(resultado);
            Assert.IsNull(_alvo.GetComponent<AuraDeTierVisual>());
            Assert.IsNull(_alvo.GetComponentInChildren<Light>());
        }

        [Test]
        public void Aplicar_ItemComAura_CriaComponenteELuzComParametrosCertos()
        {
            var resultado = AuraDeTierVisual.Aplicar(_alvo, TierItem.S, noChao: true);

            Assert.IsNotNull(resultado);
            Assert.AreEqual(TierItem.S, resultado.Tier);
            Assert.IsNotNull(resultado.Luz);
            Assert.AreEqual(LightType.Point, resultado.Luz.type);
            Assert.AreEqual(ConfigAuraTier.Cor(TierItem.S), resultado.Luz.color);
            Assert.AreEqual(ConfigAuraTier.AlcanceChao(TierItem.S), resultado.Luz.range);
        }

        [Test]
        public void ApresentadorDoVisual_ComArmaTierB_AplicaAuraNaArma()
        {
            var encaixe = new GameObject("EncaixeMaoDireita");
            encaixe.transform.SetParent(_alvo.transform, false);

            var visual = new VisualEquipado
            {
                PersonagemId = 1,
                ConjuntoAtivo = 1,
                Armas = new[]
                {
                    new ArmaVisual
                    {
                        Posicao = PosicaoEquipamento.Arma1,
                        Tipo = TipoEquipamento.Espada,
                        Tier = TierItem.A,
                    },
                },
            };

            var postos = ApresentadorDoVisual.Aplicar(_alvo.transform, visual);
            Assert.AreEqual(1, postos);

            var arma = _alvo.GetComponentInChildren<ArmaNoAvatar>();
            Assert.IsNotNull(arma);
            Assert.AreEqual(TierItem.A, arma.Tier);

            var aura = _alvo.GetComponentInChildren<AuraDeTierVisual>();
            Assert.IsNotNull(aura);
            Assert.AreEqual(TierItem.A, aura.Tier);
        }

        [Test]
        public void ApresentadorDoVisual_ComArmaTierD_NaoAplicaAuraNaArma()
        {
            var encaixe = new GameObject("EncaixeMaoDireita");
            encaixe.transform.SetParent(_alvo.transform, false);

            var visual = new VisualEquipado
            {
                PersonagemId = 1,
                ConjuntoAtivo = 1,
                Armas = new[]
                {
                    new ArmaVisual
                    {
                        Posicao = PosicaoEquipamento.Arma1,
                        Tipo = TipoEquipamento.Espada,
                        Tier = TierItem.D,
                    },
                },
            };

            var postos = ApresentadorDoVisual.Aplicar(_alvo.transform, visual);
            Assert.AreEqual(1, postos);

            var arma = _alvo.GetComponentInChildren<ArmaNoAvatar>();
            Assert.IsNotNull(arma);
            Assert.AreEqual(TierItem.D, arma.Tier);

            var aura = _alvo.GetComponentInChildren<AuraDeTierVisual>();
            Assert.IsNull(aura);
        }
    }
}
