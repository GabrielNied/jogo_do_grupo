using JogoDoGrupo.Shared.Features.Mobs;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs.Tests
{
    /// <summary>
    /// Todo id do CatalogoCriaturas tem um prefab em Resources/Mobs com Animator e o gatilho Morrer
    /// que o ApresentadorDeInimigos dispara.
    /// </summary>
    public sealed class PrefabsDasCriaturasTestes
    {
        [Test]
        public void CadaCriaturaDoCatalogoTemPrefabComAnimatorEGatilhoDeMorte()
        {
            foreach (var criatura in CatalogoCriaturas.Todas)
            {
                var prefab = Resources.Load<GameObject>(
                    ApresentadorDeInimigos.PrefixoDoPrefab + criatura.Id
                );
                Assert.IsNotNull(prefab, "Sem prefab para a criatura " + criatura.Id);

                var animator = prefab.GetComponentInChildren<Animator>();
                Assert.IsNotNull(animator, "Sem Animator em " + prefab.name);
                Assert.IsNotNull(
                    animator.runtimeAnimatorController,
                    "Animator sem controlador em " + prefab.name
                );
                Assert.IsTrue(
                    TemGatilho(animator, ApresentadorDeInimigos.GatilhoDeMorte),
                    "Sem o gatilho Morrer em " + prefab.name
                );
                Assert.AreEqual(Vector3.one, prefab.transform.localScale, "Raiz em escala 1");
                Assert.IsNotEmpty(prefab.GetComponentsInChildren<Renderer>(), "Sem malha");
            }
        }

        private static bool TemGatilho(Animator animator, string nome)
        {
            foreach (var parametro in animator.parameters)
            {
                if (
                    parametro.name == nome
                    && parametro.type == AnimatorControllerParameterType.Trigger
                )
                {
                    return true;
                }
            }
            return false;
        }
    }
}
