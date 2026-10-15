using System.Linq;
using JogoDoGrupo.Shared.Features.Mobs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs.Tests
{
    /// <summary>
    /// Todo id do CatalogoCriaturas tem um prefab em Resources/Mobs com Animator, o gatilho Morrer
    /// que o ApresentadorDeInimigos dispara e os clipes do padrao de modelos
    /// (docs/arte/padrao-de-modelos.md), feito a partir dos modelos proprios do Blender.
    /// </summary>
    public sealed class PrefabsDasCriaturasTestes
    {
        private const string PastaModelos = "Assets/_Game/Features/Mobs/Art/Modelos/";
        private static readonly string[] Clipes = { "Idle", "Andar", "Atacar", "Morrer" };
        private const float ToleranciaDoChao = 0.05f;

        private static GameObject Carregar(int id)
        {
            var prefab = Resources.Load<GameObject>(ApresentadorDeInimigos.PrefixoDoPrefab + id);
            Assert.IsNotNull(prefab, "Sem prefab para a criatura " + id);
            return prefab;
        }

        [Test]
        public void CadaCriaturaDoCatalogoTemPrefabComAnimatorEGatilhoDeMorte()
        {
            foreach (var criatura in CatalogoCriaturas.Todas)
            {
                var prefab = Carregar(criatura.Id);

                var animator = prefab.GetComponentInChildren<Animator>();
                Assert.IsNotNull(animator, "Sem Animator em " + prefab.name);
                Assert.IsNotNull(
                    animator.runtimeAnimatorController,
                    "Animator sem controlador em " + prefab.name
                );
                Assert.IsTrue(
                    TemParametro(
                        animator,
                        ApresentadorDeInimigos.GatilhoDeMorte,
                        AnimatorControllerParameterType.Trigger
                    ),
                    "Sem o gatilho Morrer em " + prefab.name
                );
                Assert.AreEqual(Vector3.one, prefab.transform.localScale, "Raiz em escala 1");
                Assert.IsNotEmpty(prefab.GetComponentsInChildren<Renderer>(), "Sem malha");
            }
        }

        [Test]
        public void CadaCriaturaTemOsClipesEOsParametrosDoPadrao()
        {
            foreach (var criatura in CatalogoCriaturas.Todas)
            {
                var animator = Carregar(criatura.Id).GetComponentInChildren<Animator>();
                var nomes = animator.runtimeAnimatorController.animationClips.Select(c => c.name);
                CollectionAssert.IsSubsetOf(Clipes, nomes.ToArray(), "Clipes de " + criatura.Id);
                Assert.IsTrue(
                    TemParametro(animator, "Atacar", AnimatorControllerParameterType.Trigger),
                    "Sem o gatilho Atacar em " + criatura.Id
                );
                Assert.IsTrue(
                    TemParametro(animator, "Andando", AnimatorControllerParameterType.Bool),
                    "Sem o parametro Andando em " + criatura.Id
                );
            }
        }

        [Test]
        public void ChefeDaDungeonTemUmClipeEUmGatilhoPorTipoDeAtaque()
        {
            var animator = Carregar(CatalogoCriaturas.GuardiaoDoReflexo)
                .GetComponentInChildren<Animator>();
            var nomes = animator.runtimeAnimatorController.animationClips.Select(c => c.name);

            foreach (
                var tipo in new[]
                {
                    TipoDeAtaqueDeInimigo.Investida,
                    TipoDeAtaqueDeInimigo.Raios,
                    TipoDeAtaqueDeInimigo.Pisada,
                }
            )
            {
                var clipe = ContatosDosClipes.ClipeDoAtaque(tipo);
                CollectionAssert.Contains(nomes.ToArray(), clipe, "Clipe do ataque " + tipo);
                Assert.IsTrue(
                    TemParametro(animator, clipe, AnimatorControllerParameterType.Trigger),
                    "Sem o gatilho " + clipe
                );
            }
        }

        [Test]
        public void CadaCriaturaUsaOModeloProprioComTextura()
        {
            foreach (var criatura in CatalogoCriaturas.Todas)
            {
                var renderer = Carregar(criatura.Id).GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.IsNotNull(renderer, "Sem SkinnedMeshRenderer em " + criatura.Id);
                StringAssert.StartsWith(
                    PastaModelos,
                    AssetDatabase.GetAssetPath(renderer.sharedMesh),
                    "Malha fora de Art/Modelos em " + criatura.Id
                );
                var material = renderer.sharedMaterial;
                Assert.IsNotNull(material, "Sem material em " + criatura.Id);
                Assert.IsNotNull(material.GetTexture("_BaseMap"), "Sem textura em " + criatura.Id);
            }
        }

        [Test]
        public void CadaCriaturaOlhaParaMaisZComOsPesNoChao()
        {
            foreach (var criatura in CatalogoCriaturas.Todas)
            {
                var instancia = Object.Instantiate(Carregar(criatura.Id));
                try
                {
                    var cabeca = instancia
                        .GetComponentsInChildren<Transform>()
                        .FirstOrDefault(t => t.name == "Cabeca");
                    Assert.IsNotNull(cabeca, "Sem osso Cabeca em " + criatura.Id);
                    Assert.Greater(cabeca.position.z, 0f, "Cabeca para tras em " + criatura.Id);

                    var limites = instancia.GetComponentInChildren<Renderer>().bounds;
                    Assert.AreEqual(
                        0f,
                        limites.min.y,
                        ToleranciaDoChao,
                        "Pes fora do chao em " + criatura.Id
                    );
                }
                finally
                {
                    Object.DestroyImmediate(instancia);
                }
            }
        }

        private static bool TemParametro(
            Animator animator,
            string nome,
            AnimatorControllerParameterType tipo
        )
        {
            foreach (var parametro in animator.parameters)
            {
                if (parametro.name == nome && parametro.type == tipo)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
