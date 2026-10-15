using System;
using System.IO;
using System.Text;
using JogoDoGrupo.Shared.Features.Pets;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Pets.Editor
{
    /// <summary>
    /// Gera, de forma reproduzível, os materiais URP e o prefab de cada pet a partir do modelo CC0
    /// em ThirdParty (docs/arte/licencas.md). O prefab fica em
    /// <c>Resources/Pets/Pet_&lt;especie&gt;</c> (o ApresentadorDePets carrega por esse nome): uma
    /// raiz vazia com o modelo no filho "Modelo", virado para +Z, com os pés em Y=0 e na altura
    /// do cervo. Menu: Jogo do Grupo/Pets/Gerar pets.
    /// </summary>
    public static class GeradorDosPets
    {
        public const string PastaArte = "Assets/_Game/Features/Pets/Art";
        public const string PastaResources = "Assets/_Game/Features/Pets/Resources/Pets";
        public const string ModeloDoCervo = "Assets/ThirdParty/OpenGameArt/DeerLowPoly/Deer.obj";

        // Valores ajustáveis (balanceamento visual).
        const float AlturaDoCervoMetros = 1.1f;
        const float LisuraDoPelo = 0.1f;

        // Tons do modelo original (Deer.mtl), clareados um pouco para a luz do jogo.
        static readonly (string Nome, Color Cor)[] Materiais =
        {
            ("deer_skin", new Color(0.62f, 0.40f, 0.24f)),
            ("deer_horn", new Color(0.86f, 0.80f, 0.66f)),
            ("Deer_hoaves", new Color(0.12f, 0.10f, 0.09f)),
        };

        public static string CaminhoPrefab(EspecieDePet especie) =>
            PastaResources + "/Pet_" + (int)especie + ".prefab";

        [MenuItem("Jogo do Grupo/Pets/Gerar pets")]
        public static void Gerar()
        {
            CriarPasta(PastaArte);
            CriarPasta(PastaResources);
            var relatorio = new StringBuilder();
            GerarCervoJovem(relatorio);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "gerador-pets.txt"),
                relatorio.ToString()
            );
        }

        static void CriarPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
            CriarPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
        }

        static Material CriarMaterial(string nome, Color cor)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Shader URP Lit nao encontrado.");
            var caminho = PastaArte + "/CervoJovem_" + nome + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, caminho);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", cor);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", LisuraDoPelo);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void GerarCervoJovem(StringBuilder relatorio)
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ModeloDoCervo);
            if (modelo == null)
                throw new InvalidOperationException(
                    "Modelo do cervo nao encontrado: " + ModeloDoCervo
                );

            var raiz = new GameObject("Pet_" + (int)EspecieDePet.CervoJovem);
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                instancia.name = "Modelo";
                instancia.transform.SetParent(raiz.transform, false);

                // Troca os materiais do OBJ (Standard) pelos URP, pelo nome do material original.
                foreach (var renderizador in instancia.GetComponentsInChildren<Renderer>())
                {
                    var antigos = renderizador.sharedMaterials;
                    var novos = new Material[antigos.Length];
                    for (var i = 0; i < novos.Length; i++)
                        novos[i] = MaterialPorNome(antigos[i] != null ? antigos[i].name : "");
                    renderizador.sharedMaterials = novos;
                    renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }

                // O modelo vem com a cabeca para um lado qualquer: o galho (chifre) fica do lado
                // da cabeca, entao vira-se o corpo para a cabeca apontar para +Z.
                var yaw = GiroParaOLadoDaCabeca(instancia);
                instancia.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                // Na altura do cervo, com os pes em Y=0 e centrado.
                var limites = Limites(instancia);
                var escala = AlturaDoCervoMetros / Mathf.Max(0.01f, limites.size.y);
                instancia.transform.localScale = Vector3.one * escala;
                limites = Limites(instancia);
                instancia.transform.localPosition = new Vector3(
                    -limites.center.x,
                    -limites.min.y,
                    -limites.center.z
                );

                relatorio.AppendLine(
                    "CervoJovem yaw="
                        + yaw
                        + " escala="
                        + escala.ToString("F3")
                        + " limites="
                        + Limites(instancia).size.ToString("F3")
                );
                PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab(EspecieDePet.CervoJovem));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        static Material MaterialPorNome(string nomeOriginal)
        {
            foreach (var (nome, cor) in Materiais)
            {
                if (nomeOriginal.StartsWith(nome, StringComparison.OrdinalIgnoreCase))
                    return CriarMaterial(nome, cor);
            }
            // Material desconhecido: usa o do pelo.
            return CriarMaterial(Materiais[0].Nome, Materiais[0].Cor);
        }

        // Quanto girar (em graus, multiplo de 90) para a cabeca apontar para +Z: compara o centro
        // dos chifres com o centro do corpo, no plano do chao.
        static float GiroParaOLadoDaCabeca(GameObject instancia)
        {
            var todos = Limites(instancia);
            Bounds? chifres = null;
            foreach (var renderizador in instancia.GetComponentsInChildren<Renderer>())
            {
                var materiais = renderizador.sharedMaterials;
                var malha = renderizador.GetComponent<MeshFilter>()?.sharedMesh;
                if (malha == null)
                    continue;
                for (var i = 0; i < materiais.Length && i < malha.subMeshCount; i++)
                {
                    if (
                        materiais[i] != null
                        && materiais[i].name.Contains("horn", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        var parte = malha.GetSubMesh(i).bounds;
                        var centro = renderizador.transform.TransformPoint(parte.center);
                        chifres = new Bounds(centro, Vector3.zero);
                    }
                }
            }

            if (chifres == null)
                return 0f;
            var dx = chifres.Value.center.x - todos.center.x;
            var dz = chifres.Value.center.z - todos.center.z;
            var angulo = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
            return -Mathf.Round(angulo / 90f) * 90f;
        }

        static Bounds Limites(GameObject objeto)
        {
            var renderizadores = objeto.GetComponentsInChildren<Renderer>();
            var limites = renderizadores[0].bounds;
            foreach (var r in renderizadores)
                limites.Encapsulate(r.bounds);
            return limites;
        }
    }
}
