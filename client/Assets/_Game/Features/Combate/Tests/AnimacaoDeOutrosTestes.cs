using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>Golpe e esquiva dos outros jogadores no avatar deles (#481).</summary>
    public class AnimacaoDeOutrosTestes
    {
        private const long Eu = 1;
        private const long Aliado = 2;

        private GameObject _avatar;

        [SetUp]
        public void Montar()
        {
            _avatar = new GameObject("Jogador_2");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/_Game/Features/Personagem/Art/Jogador_Valdren.controller"
            );
            _avatar.AddComponent<Animator>().runtimeAnimatorController = controller;
            AnimacaoDeOutros.Buscar = id => id == Aliado ? _avatar : null;
        }

        [TearDown]
        public void Desmontar()
        {
            Object.DestroyImmediate(_avatar);
            AnimacaoDeOutros.RestaurarBusca();
        }

        [Test]
        public void GolpeDeOutro_DisparaOTriggerNoAvatarCertoEViraParaADirecao()
        {
            var golpe = new AtaqueIniciado
            {
                AtacanteId = Aliado,
                DirecaoY = 90f,
                Tipo = TipoDeAtaque.Pesado,
            };

            Assert.IsTrue(AnimacaoDeOutros.TocarGolpe(golpe, Eu));
            Assert.AreEqual(90f, _avatar.transform.eulerAngles.y, 0.01f);
            Assert.IsTrue(
                _avatar
                    .GetComponent<Animator>()
                    .GetBool(Animator.StringToHash(AnimadorDeAtaque.ParametroAtaquePesado))
            );
        }

        [Test]
        public void GolpeDeOutro_NaoTocaNoAvatarDeOutroId()
        {
            var golpe = new AtaqueIniciado { AtacanteId = 3, DirecaoY = 90f };

            Assert.IsFalse(AnimacaoDeOutros.TocarGolpe(golpe, Eu));
            Assert.AreEqual(0f, _avatar.transform.eulerAngles.y, 0.01f);
        }

        [Test]
        public void EsquivaDeOutro_AnimaEVira()
        {
            var esquiva = new EsquivaIniciada { PersonagemId = Aliado, DirecaoY = 180f };

            Assert.IsTrue(AnimacaoDeOutros.TocarEsquiva(esquiva, Eu));
            Assert.AreEqual(180f, _avatar.transform.eulerAngles.y, 0.01f);
        }

        [Test]
        public void OProprioJogador_NaoEAnimadoDuasVezes()
        {
            AnimacaoDeOutros.Buscar = _ => _avatar;

            var golpe = new AtaqueIniciado { AtacanteId = Eu, DirecaoY = 90f };
            var esquiva = new EsquivaIniciada { PersonagemId = Eu, DirecaoY = 90f };

            Assert.IsFalse(AnimacaoDeOutros.TocarGolpe(golpe, Eu));
            Assert.IsFalse(AnimacaoDeOutros.TocarEsquiva(esquiva, Eu));
            Assert.AreEqual(0f, _avatar.transform.eulerAngles.y, 0.01f);
        }

        [Test]
        public void SemAvatar_NaoQuebra()
        {
            AnimacaoDeOutros.Buscar = _ => null;

            Assert.IsFalse(
                AnimacaoDeOutros.TocarGolpe(new AtaqueIniciado { AtacanteId = Aliado }, Eu)
            );
        }
    }
}
