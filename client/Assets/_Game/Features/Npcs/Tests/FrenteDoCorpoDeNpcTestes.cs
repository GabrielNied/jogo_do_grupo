using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs.Tests
{
    /// <summary>
    /// A rota manda a raiz do habitante para o sentido do passo (+Z = 0 grau, +X = 90 graus). O corpo
    /// só anda de frente se a ponta do pé apontar para +Z da raiz (#1173).
    /// </summary>
    public class FrenteDoCorpoDeNpcTestes
    {
        [TestCase("Armeiro")]
        [TestCase("Morador")]
        public void Corpo_TemOsPesApontandoParaMaisZDaRaiz(string papel)
        {
            var prefab = Resources.Load<GameObject>(FabricaDeCorpoDeNpc.PrefixoDoPrefab + papel);
            Assert.IsNotNull(prefab, papel);
            var raiz = Object.Instantiate(prefab);
            try
            {
                var pe = Achar(raiz.transform, "Foot.L");
                Assert.IsNotNull(pe, "Foot.L");
                Assert.Greater(pe.childCount, 0);
                var frente = raiz.transform.InverseTransformDirection(
                    pe.GetChild(0).position - pe.position
                );
                Assert.Greater(frente.z, 0.5f * frente.magnitude, papel + ": " + frente);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static Transform Achar(Transform t, string nome)
        {
            if (t.name == nome)
            {
                return t;
            }

            foreach (Transform filho in t)
            {
                var r = Achar(filho, nome);
                if (r != null)
                {
                    return r;
                }
            }

            return null;
        }
    }
}
