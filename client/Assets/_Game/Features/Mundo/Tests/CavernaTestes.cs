using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class CavernaTestes
    {
        [Test]
        public void Malha_do_tunel_tem_piso_teto_e_paredes_dentro_dos_limites()
        {
            var malha = GeometriaDaCaverna.CriarMalha();
            try
            {
                Assert.Greater(malha.vertexCount, 0);
                Assert.AreEqual(malha.vertexCount, malha.normals.Length);
                Assert.AreEqual(0, malha.triangles.Length % 3);

                PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
                var piso = float.MaxValue;
                var teto = float.MinValue;
                foreach (var v in malha.vertices)
                {
                    Assert.GreaterOrEqual(v.x, minX - 0.01f);
                    Assert.LessOrEqual(v.x, maxX + 0.01f);
                    Assert.GreaterOrEqual(v.z, minZ - 0.01f);
                    Assert.LessOrEqual(v.z, maxZ + 0.01f);
                    piso = Mathf.Min(piso, v.y);
                    teto = Mathf.Max(teto, v.y);
                }

                Assert.Less(piso, PrimeiraCaverna.EntradaY - 8f, "o túnel desce da boca");
                Assert.Greater(teto, PrimeiraCaverna.EntradaY, "o teto passa do nível da boca");
            }
            finally
            {
                Object.DestroyImmediate(malha);
            }
        }

        [Test]
        public void Os_pisos_da_malha_olham_para_cima_e_os_tetos_para_baixo()
        {
            var malha = GeometriaDaCaverna.CriarMalha();
            try
            {
                var vertices = malha.vertices;
                var indices = malha.triangles;
                var normais = malha.normals;
                for (var t = 0; t < indices.Length; t += 3)
                {
                    var a = vertices[indices[t]];
                    var b = vertices[indices[t + 1]];
                    var c = vertices[indices[t + 2]];
                    var face = Vector3.Cross(b - a, c - a).normalized;

                    // O lado da frente do triângulo é o da normal gravada na malha.
                    Assert.Greater(
                        Vector3.Dot(face, normais[indices[t]]),
                        0.5f,
                        $"triângulo {t / 3} virado"
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(malha);
            }
        }

        [Test]
        public void Piso_da_malha_acompanha_o_piso_do_shared()
        {
            var malha = GeometriaDaCaverna.CriarMalha();
            try
            {
                PrimeiraCaverna.PontoDoPercurso(40f, out var x, out var z);
                var esperado = PrimeiraCaverna.PisoNoPlano(x, z).Value;
                var achou = false;
                foreach (var v in malha.vertices)
                {
                    if (
                        Mathf.Abs(v.x - x) < 0.3f
                        && Mathf.Abs(v.z - z) < 0.3f
                        && Mathf.Abs(v.y - esperado) < 0.2f
                    )
                    {
                        achou = true;
                        break;
                    }
                }

                Assert.IsTrue(achou, "há vértice de piso perto do eixo");
            }
            finally
            {
                Object.DestroyImmediate(malha);
            }
        }

        [Test]
        public void Boca_da_caverna_fica_aberta_para_sair()
        {
            var objeto = new GameObject("Tunel");
            var malha = GeometriaDaCaverna.CriarMalha();
            try
            {
                objeto.AddComponent<MeshCollider>().sharedMesh = malha;
                Physics.SyncTransforms();
                var origem = new Vector3(
                    PrimeiraCaverna.EntradaX,
                    PrimeiraCaverna.EntradaY + 1.4f,
                    PrimeiraCaverna.EntradaZ + 0.45f
                );

                Assert.IsFalse(
                    Physics.Raycast(origem, Vector3.back, 1.5f),
                    "não há tampa ou colisor na boca quando o jogador volta para fora"
                );
            }
            finally
            {
                Object.DestroyImmediate(objeto);
                Object.DestroyImmediate(malha);
            }
        }

        [Test]
        public void Montar_cria_o_tunel_com_colisao_e_as_luzes_sem_teleporte()
        {
            var pai = new GameObject("Pai").transform;
            try
            {
                Caverna.Montar(pai);

                var raiz = pai.Find(ConstrutorHub.NomeCaverna);
                Assert.IsNotNull(raiz);
                var tunel = raiz.Find(Caverna.NomeTunel);
                Assert.IsNotNull(tunel);
                var colisor = tunel.GetComponent<MeshCollider>();
                Assert.IsNotNull(colisor);
                Assert.IsNotNull(colisor.sharedMesh);
                Assert.Greater(colisor.sharedMesh.triangles.Length, 0);
                Assert.Greater(raiz.Find(Caverna.NomeLuzes).childCount, 3);
                Assert.Greater(raiz.Find(Caverna.NomeEstruturas).childCount, 0);
                Assert.AreEqual(
                    0,
                    raiz.GetComponents<MonoBehaviour>().Length,
                    "nada de lógica de teleporte"
                );
            }
            finally
            {
                Object.DestroyImmediate(pai.gameObject);
            }
        }
    }
}
