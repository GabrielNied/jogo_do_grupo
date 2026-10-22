using System.IO;
using System.Text;
using JogoDoGrupo.Client.Features.Itens.Visual;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Editor
{
    /// <summary>
    /// Gera os prefabs das armas equipadas a partir dos modelos CC0 de <c>ThirdParty/</c> (#1188):
    /// espada, arco, martelo e escudo do Quaternius ("LowPoly Medieval Weapons") e o cajado do
    /// "Low-poly Staffs". Cada prefab segue o contrato de <see cref="ModelosDeArma"/>: cabo na
    /// origem, ponta para +Y e o comprimento em metros de <see cref="ModelosDeArma.Comprimento"/>
    /// (o escudo tem o centro na origem e a face para +Z). Reproduzível: rodar de novo regrava os
    /// mesmos prefabs. Uso (editor fechado):
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Itens.Editor.ModelosDeItensImportador.Gerar</c>.
    /// </summary>
    public static class ModelosDeItensImportador
    {
        private const string PastaDosPrefabs =
            "Assets/_Game/Features/Itens/Resources/Itens/Modelos";
        private const string PastaQuaternius = "Assets/ThirdParty/Quaternius/MedievalWeapons/";

        private enum Origem
        {
            /// <summary>Base da peça (menor Y) no zero, centrada em X e Z.</summary>
            Base,

            /// <summary>Centro da peça no zero.</summary>
            Centro,
        }

        private sealed class Especificacao
        {
            public TipoEquipamento Tipo;
            public string Fbx;
            public Vector3 GiroEuler;
            public Origem Origem;

            /// <summary>
            /// Acerto fino da origem: x em unidades do FBX (antes da escala), y em metros (para baixo).
            /// </summary>
            public Vector3 AjusteDaEmpunhadura;
        }

        // O giro põe o eixo do comprimento em +Y, a ponta para cima e, no arco, a corda no -X
        // (o desenho de ModelosDeArma e os encaixes de ApresentadorDoVisual assumem isso).
        private static readonly Especificacao[] Especificacoes =
        {
            new Especificacao
            {
                Tipo = TipoEquipamento.Espada,
                Fbx = PastaQuaternius + "Sword.fbx",
                GiroEuler = new Vector3(-90f, 0f, 0f),
                Origem = Origem.Base,
            },
            new Especificacao
            {
                Tipo = TipoEquipamento.Martelo,
                Fbx = PastaQuaternius + "Hammer_Double.fbx",
                GiroEuler = new Vector3(-90f, 0f, 0f),
                Origem = Origem.Base,
            },
            new Especificacao
            {
                Tipo = TipoEquipamento.Arco,
                Fbx = PastaQuaternius + "Bow_Wooden.fbx",
                GiroEuler = new Vector3(-90f, 0f, 0f),
                Origem = Origem.Base,
                // A empunhadura do arco fica em x 0,07 do FBX (a corda em -0,9) e a 0,45 m da base.
                AjusteDaEmpunhadura = new Vector3(0.07f, 0.1f, 0f),
            },
            new Especificacao
            {
                Tipo = TipoEquipamento.Cajado,
                Fbx = "Assets/ThirdParty/OpenGameArt/LowPolyStaffs/SorcerersStaff.fbx",
                GiroEuler = Vector3.zero,
                Origem = Origem.Base,
            },
            new Especificacao
            {
                Tipo = TipoEquipamento.Escudo,
                Fbx = PastaQuaternius + "Shield_Heater.fbx",
                GiroEuler = new Vector3(-90f, 0f, 0f),
                Origem = Origem.Centro,
            },
        };

        /// <summary>Regrava os prefabs de arma.</summary>
        public static void Gerar()
        {
            Directory.CreateDirectory(PastaDosPrefabs);
            AssetDatabase.Refresh();
            foreach (var especificacao in Especificacoes)
            {
                GerarUm(especificacao);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Itens] Prefabs de arma gerados em " + PastaDosPrefabs);
        }

        /// <summary>Escreve no log as medidas de cada FBX (para escolher o giro de cada arma).</summary>
        public static void Inspecionar()
        {
            foreach (var especificacao in Especificacoes)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(especificacao.Fbx);
                if (fbx == null)
                {
                    Debug.LogError("[Itens] FBX ausente: " + especificacao.Fbx);
                    continue;
                }

                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                instancia.transform.rotation = Quaternion.Euler(especificacao.GiroEuler);
                var texto = new StringBuilder();
                texto.AppendLine("[Itens] " + especificacao.Fbx);
                var limites = Limites(instancia);
                texto.AppendLine(
                    "  tamanho "
                        + limites.size.ToString("F3")
                        + " centro "
                        + limites.center.ToString("F3")
                );
                var eixo = EixoLongo(limites.size);
                texto.AppendLine("  eixo longo " + eixo);
                var fatias = 10;
                var largura = new float[fatias];
                var cont = new int[fatias];
                var mediaOutro = new float[fatias];
                var minOutro = new float[fatias];
                var maxOutro = new float[fatias];
                for (var i = 0; i < fatias; i++)
                {
                    minOutro[i] = float.MaxValue;
                    maxOutro[i] = float.MinValue;
                }

                var soma = Vector3.zero;
                var total = 0;
                foreach (var v in Vertices(instancia))
                {
                    var t = Mathf.InverseLerp(limites.min[eixo], limites.max[eixo], v[eixo]);
                    var i = Mathf.Min(fatias - 1, (int)(t * fatias));
                    var outro1 = (eixo + 1) % 3;
                    var outro2 = (eixo + 2) % 3;
                    largura[i] = Mathf.Max(
                        largura[i],
                        Mathf.Abs(v[outro1] - limites.center[outro1]),
                        Mathf.Abs(v[outro2] - limites.center[outro2])
                    );
                    cont[i]++;
                    mediaOutro[i] += v[outro1];
                    minOutro[i] = Mathf.Min(minOutro[i], v[outro1]);
                    maxOutro[i] = Mathf.Max(maxOutro[i], v[outro1]);
                    soma += v;
                    total++;
                }

                var linha = new StringBuilder("  meia-largura por fatia (do min ao max): ");
                for (var i = 0; i < fatias; i++)
                {
                    linha.Append(largura[i].ToString("F2")).Append(' ');
                }

                texto.AppendLine(linha.ToString());
                var linhaMedia = new StringBuilder("  eixo+1: media/min/max por fatia: ");
                for (var i = 0; i < fatias; i++)
                {
                    linhaMedia
                        .Append((mediaOutro[i] / Mathf.Max(1, cont[i])).ToString("F2"))
                        .Append("[")
                        .Append(minOutro[i].ToString("F2"))
                        .Append(",")
                        .Append(maxOutro[i].ToString("F2"))
                        .Append("] ");
                }

                texto.AppendLine(linhaMedia.ToString());
                texto.AppendLine(
                    "  centroide dos vértices " + (soma / Mathf.Max(1, total)).ToString("F3")
                );
                foreach (var r in instancia.GetComponentsInChildren<Renderer>())
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        texto.AppendLine(
                            "  material " + (m != null ? m.name + " / " + m.shader.name : "nulo")
                        );
                    }
                }

                Debug.Log(texto.ToString());
                Object.DestroyImmediate(instancia);
            }
        }

        private static void GerarUm(Especificacao especificacao)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(especificacao.Fbx);
            if (fbx == null)
            {
                throw new FileNotFoundException("FBX ausente: " + especificacao.Fbx);
            }

            var raiz = new GameObject("Arma_" + especificacao.Tipo);
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            instancia.name = "Modelo";
            instancia.transform.SetParent(raiz.transform, false);
            instancia.transform.localRotation = Quaternion.Euler(especificacao.GiroEuler);

            foreach (var colisor in raiz.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(colisor);
            }

            // Escala para o comprimento em metros do contrato (a maior dimensão em Y depois do giro).
            var limites = Limites(raiz);
            var comprimento = ModelosDeArma.Comprimento(especificacao.Tipo);
            var escala = comprimento / limites.size.y;
            instancia.transform.localScale *= escala;
            limites = Limites(raiz);

            var deslocamento =
                especificacao.Origem == Origem.Base
                    ? new Vector3(limites.center.x, limites.min.y, limites.center.z)
                    : limites.center;
            deslocamento.x += especificacao.AjusteDaEmpunhadura.x * escala;
            deslocamento.y += especificacao.AjusteDaEmpunhadura.y;
            instancia.transform.localPosition -= deslocamento;

            var caminho = PastaDosPrefabs + "/Arma_" + especificacao.Tipo + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
            Object.DestroyImmediate(raiz);
            Debug.Log("[Itens] " + caminho + " escala " + escala.ToString("F4"));
        }

        private static Bounds Limites(GameObject objeto)
        {
            var achou = false;
            var limites = new Bounds();
            foreach (var renderizador in objeto.GetComponentsInChildren<Renderer>())
            {
                if (!achou)
                {
                    limites = renderizador.bounds;
                    achou = true;
                }
                else
                {
                    limites.Encapsulate(renderizador.bounds);
                }
            }

            return limites;
        }

        private static int EixoLongo(Vector3 tamanho)
        {
            if (tamanho.x >= tamanho.y && tamanho.x >= tamanho.z)
            {
                return 0;
            }

            return tamanho.y >= tamanho.z ? 1 : 2;
        }

        private static System.Collections.Generic.IEnumerable<Vector3> Vertices(GameObject objeto)
        {
            foreach (var filtro in objeto.GetComponentsInChildren<MeshFilter>())
            {
                foreach (var v in filtro.sharedMesh.vertices)
                {
                    yield return filtro.transform.TransformPoint(v);
                }
            }

            foreach (var pele in objeto.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (var v in pele.sharedMesh.vertices)
                {
                    yield return pele.transform.TransformPoint(v);
                }
            }
        }
    }
}
