using System.Linq;
using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>
    /// O controller que o gerador de Personagem cria tem os parâmetros que Combate e Core tocam, com
    /// os tipos certos (a tabela do contrato está em <c>docs/features/personagem.md</c>).
    /// </summary>
    public class ControllerDoPersonagemTestes
    {
        private static readonly string[] Racas =
        {
            "Valdren",
            "Aurideo",
            "Granitico",
            "Cinereo",
            "Silvano",
        };

        private static string Caminho(string raca) =>
            "Assets/_Game/Features/Personagem/Art/Jogador_" + raca + ".controller";

        private static readonly (string nome, AnimatorControllerParameterType tipo)[] Esperados =
        {
            ("Velocidade", AnimatorControllerParameterType.Float),
            ("NoChao", AnimatorControllerParameterType.Bool),
            (AnimadorDeEsquiva.ParametroEsquiva, AnimatorControllerParameterType.Trigger),
            (AnimadorDeAtaque.ParametroAtaqueLeve, AnimatorControllerParameterType.Trigger),
            (AnimadorDeAtaque.ParametroAtaquePesado, AnimatorControllerParameterType.Trigger),
            (AnimadorDeAtaque.ParametroPasso, AnimatorControllerParameterType.Int),
            (AnimadorDeAtaque.ParametroVelocidadeDoGolpe, AnimatorControllerParameterType.Float),
            (AnimadorDeAtaque.ParametroCancelar, AnimatorControllerParameterType.Trigger),
            (AnimadorDeDefesa.ParametroGuarda, AnimatorControllerParameterType.Bool),
            (AnimadorDeDefesa.ParametroInterrompido, AnimatorControllerParameterType.Trigger),
            (AnimadorDeDefesa.ParametroCaido, AnimatorControllerParameterType.Bool),
        };

        [Test]
        public void Controllers_TemOsParametrosDeCombateEDoCore_ComOsTiposCertos(
            [ValueSource(nameof(Racas))] string raca
        )
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Caminho(raca));
            Assert.IsNotNull(controller, "Rode o gerador de jogadores das raças: " + raca);

            foreach (var (nome, tipo) in Esperados)
            {
                var parametro = controller.parameters.FirstOrDefault(p => p.name == nome);
                Assert.IsNotNull(parametro, raca + " sem o parâmetro " + nome);
                Assert.AreEqual(tipo, parametro.type, raca + ": tipo de " + nome);
            }
        }

        [Test]
        public void Controller_ComAnimator_AnimadorDeAtaqueToca()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Caminho("Valdren"));
            var objeto = new GameObject("Teste do golpe");
            try
            {
                var animator = objeto.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                Assert.IsTrue(AnimadorDeAtaque.Tocar(animator, TipoDeAtaque.Leve, 1));
                Assert.IsTrue(AnimadorDeAtaque.Tocar(animator, TipoDeAtaque.Pesado));
                Assert.IsTrue(AnimadorDeDefesa.DefinirGuarda(animator, true));
                Assert.IsTrue(AnimadorDeDefesa.Interromper(animator));
                Assert.IsTrue(AnimadorDeDefesa.DefinirCaido(animator, true));
                Assert.IsTrue(AnimadorDeAtaque.Cancelar(animator));
                Assert.AreEqual(
                    (float)RegrasAtaque.VelocidadeDoClipe(TipoDeAtaque.Pesado),
                    animator.GetFloat(AnimadorDeAtaque.ParametroVelocidadeDoGolpe),
                    0.0001f
                );
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void CamadaDoGolpe_SoMexeNoTronco_ENosCaminhosQueOsClipesAnimam()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Caminho("Valdren"));
            var camada = controller.layers.FirstOrDefault(l => l.name == "Golpe");
            Assert.IsNotNull(camada, "Controller sem a camada Golpe");
            var mascara = camada.avatarMask;
            Assert.IsNotNull(mascara, "A camada Golpe precisa da máscara do tronco");

            var clipe = AssetDatabase
                .LoadAllAssetsAtPath("Assets/_Game/Features/Personagem/Art/Modelos/Valdren.fbx")
                .OfType<AnimationClip>()
                .First(c => c.name == "Atacar");
            var ativos = 0;
            foreach (var ligacao in AnimationUtility.GetCurveBindings(clipe))
            {
                var indice = -1;
                for (var i = 0; i < mascara.transformCount; i++)
                {
                    if (mascara.GetTransformPath(i) == ligacao.path)
                    {
                        indice = i;
                        break;
                    }
                }

                Assert.GreaterOrEqual(indice, 0, "A máscara não tem o osso " + ligacao.path);
                var abaixoDoTronco = ligacao.path.Split('/').Contains("Tronco");
                Assert.AreEqual(abaixoDoTronco, mascara.GetTransformActive(indice), ligacao.path);
                ativos += abaixoDoTronco ? 1 : 0;
            }

            Assert.Greater(ativos, 0, "Nenhum osso do tronco na máscara");
        }

        [Test]
        public void VelocidadeDoClipe_FazOContatoCairNoFimDoPreparo()
        {
            foreach (var tipo in new[] { TipoDeAtaque.Leve, TipoDeAtaque.Pesado })
            {
                var velocidade = RegrasAtaque.VelocidadeDoClipe(tipo);
                Assert.AreEqual(
                    RegrasAtaque.PreparoSegundos(tipo),
                    RegrasAtaque.ContatoNoClipeSegundos / velocidade,
                    1e-9
                );
            }
        }
    }
}
