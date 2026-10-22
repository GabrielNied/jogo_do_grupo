using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    /// <summary>Ensaio completo no Terrain real, isolado do mapa de autoria.</summary>
    public static class ValidacaoDosCruzamentos
    {
        private static void Verificar(bool valor, string mensagem)
        {
            if (!valor)
                throw new InvalidOperationException("[Cruzamentos] " + mensagem);
        }

        [MenuItem("Jogo do Grupo/Mundo/Estradas/Validar cruzamentos e criar demonstração")]
        public static void Executar()
        {
            ValidarCenasSalvas();
            var cena = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive
            );
            SceneManager.SetActiveScene(cena);
            var pasta = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/_Game/Features/Mundo/DemoCruzamentos"
            );
            AplicadorDeEstradas.GarantirPasta(pasta);
            var raiz = new GameObject("Hub");
            var solo = DemoDasEstradas.Textura(
                pasta,
                "Solo",
                new Color(0.28f, 0.36f, 0.17f),
                false
            );
            var grama = DemoDasEstradas.Textura(
                pasta,
                "Grama",
                new Color(0.3f, 0.45f, 0.16f),
                true
            );
            var camada = DemoDasEstradas.Camada(pasta, "Solo", solo);
            var camadaTerra = DemoDasEstradas.Camada(pasta, "Terra", solo);
            var modelo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var materialArvore = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            materialArvore.SetColor("_BaseColor", new Color(0.22f, 0.33f, 0.1f));
            AssetDatabase.CreateAsset(materialArvore, pasta + "/Arvore.mat");
            modelo.GetComponent<Renderer>().sharedMaterial = materialArvore;
            var arvore = PrefabUtility.SaveAsPrefabAsset(modelo, pasta + "/Arvore.prefab");
            Object.DestroyImmediate(modelo);
            var material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            AssetDatabase.CreateAsset(material, pasta + "/Terrain.mat");
            var terrenos = new[]
            {
                DemoDasEstradas.CriarTerreno(
                    pasta,
                    raiz.transform,
                    0f,
                    camada,
                    camadaTerra,
                    grama,
                    arvore,
                    material
                ),
                DemoDasEstradas.CriarTerreno(
                    pasta,
                    raiz.transform,
                    32f,
                    camada,
                    camadaTerra,
                    grama,
                    arvore,
                    material
                ),
            };
            terrenos[0].SetNeighbors(null, null, terrenos[1], null);
            terrenos[1].SetNeighbors(terrenos[0], null, null, null);
            // Árvores na união, na antiga faixa e fora dela; duas iguais preservam multiplicidade.
            foreach (var t in terrenos)
            {
                var arvores = t.terrainData.treeInstances.ToList();
                foreach (
                    var p in new[]
                    {
                        new Vector3(0.5f, 0f, 0.5f),
                        new Vector3(0.9f, 0f, 0.5f),
                        new Vector3(0.5f, 0f, 0.2f),
                    }
                )
                    arvores.Add(
                        new TreeInstance
                        {
                            position = p,
                            widthScale = 1f,
                            heightScale = 1f,
                            color = Color.white,
                            lightmapColor = Color.white,
                        }
                    );
                arvores.Add(arvores[arvores.Count - 1]);
                t.terrainData.SetTreeInstances(arvores.ToArray(), true);
            }
            var sol = new GameObject("Sol").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 2f;
            sol.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.5f);
            var a = CriarVia(raiz.transform, "Lajotas", TipoDeEstrada.Lajotas, 5f);
            var b = CriarVia(raiz.transform, "Terra", TipoDeEstrada.Terra, 3f);
            var idsGerados = new HashSet<string> { a.Id, b.Id };
            var caminhoCena = pasta + "/DemoCruzamentos.unity";
            EditorSceneManager.SaveScene(cena, caminhoCena);
            var alturasOriginais = terrenos
                .Select(t => t.terrainData.GetHeights(0, 0, 129, 129))
                .ToArray();
            var detalhesOriginais = terrenos
                .Select(t => t.terrainData.GetDetailLayer(0, 0, 64, 64, 0))
                .ToArray();
            var arvoresOriginais = terrenos.Select(t => t.terrainData.treeInstances).ToArray();
            for (var caso = 0; caso < 3; caso++)
            {
                Definir(
                    a,
                    caso == 2
                        ? new[]
                        {
                            new Vector3(6, 3, 25),
                            new Vector3(32, 3, 16),
                            new Vector3(58, 3, 25),
                        }
                        : new[] { new Vector3(6, 3, 16), new Vector3(58, 3, 16) }
                );
                Definir(
                    b,
                    caso == 0 ? new[] { new Vector3(32, 3, 16), new Vector3(32, 3, 26) }
                        : caso == 1 ? new[] { new Vector3(32, 3, 6), new Vector3(32, 3, 26) }
                        : new[] { new Vector3(32, 3, 6), new Vector3(32, 3, 16) }
                );
                AplicadorDeEstradas.Aplicar(a);
                AplicadorDeEstradas.Aplicar(b);
                ConferirUniao(a, b);
                var alturas = terrenos
                    .Select(t => t.terrainData.GetHeights(0, 0, 129, 129))
                    .ToArray();
                AplicadorDeEstradas.Aplicar(b);
                for (var t = 0; t < terrenos.Length; t++)
                    Verificar(
                        Iguais(alturas[t], terrenos[t].terrainData.GetHeights(0, 0, 129, 129)),
                        "Reaplicar acumulou o rebaixo."
                    );
                AplicadorDeEstradas.Restaurar(b);
                Verificar(
                    (a.Registro as RegistroDaEstrada).Aplicada,
                    "Restauração parcial removeu lajotas."
                );
                AplicadorDeEstradas.Restaurar(a);
                ConferirBase(terrenos, alturasOriginais, detalhesOriginais, arvoresOriginais);
                // IDs novos e ordem inversa: material deve continuar sendo decidido pela superfície.
                a.Id = Guid.NewGuid().ToString("N");
                b.Id = Guid.NewGuid().ToString("N");
                idsGerados.Add(a.Id);
                idsGerados.Add(b.Id);
                AplicadorDeEstradas.Aplicar(b);
                AplicadorDeEstradas.Aplicar(a);
                ConferirUniao(a, b);
                for (var t = 0; t < terrenos.Length; t++)
                    Verificar(
                        Iguais(alturas[t], terrenos[t].terrainData.GetHeights(0, 0, 129, 129)),
                        "Ordem de aplicação alterou o terreno."
                    );
                AplicadorDeEstradas.Restaurar(a);
                AplicadorDeEstradas.Restaurar(b);
                ConferirBase(terrenos, alturasOriginais, detalhesOriginais, arvoresOriginais);
                Debug.Log(
                    "[Cruzamentos] "
                        + new[] { "T", "X", "Y" }[caso]
                        + ": união, ordem inversa, repetição e restauração aprovadas."
                );
            }
            // Movimento, troca de conexões e configuração pendente da vizinha.
            Definir(a, new[] { new Vector3(6, 3, 16), new Vector3(58, 3, 16) });
            Definir(b, new[] { new Vector3(32, 3, 6), new Vector3(32, 3, 26) });
            AplicadorDeEstradas.Aplicar(a);
            AplicadorDeEstradas.Aplicar(b);
            var registroA = (RegistroDaEstrada)a.Registro;
            var larguraAplicada = registroA.Largura;
            var so = new SerializedObject(a);
            so.FindProperty("_largura").floatValue = 7f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Definir(b, new[] { new Vector3(44, 3, 6), new Vector3(44, 3, 26) });
            AplicadorDeEstradas.Aplicar(b);
            Verificar(
                Mathf.Approximately(a.Largura, 7f)
                    && Mathf.Approximately(registroA.Largura, larguraAplicada),
                "Aplicou ajuste pendente da vizinha."
            );
            Verificar(
                terrenos[0].terrainData.GetDetailLayer(63, 12, 1, 1, 0)[0, 0] == 1,
                "Movimento não devolveu grama na faixa abandonada."
            );
            // Proteção contra alteração manual dentro da união; nenhuma outra célula deve mudar.
            var dados = terrenos[1].terrainData;
            var h = dados.GetHeights(48, 64, 1, 1);
            var anteriorH = h[0, 0];
            h[0, 0] += 0.02f;
            dados.SetHeights(48, 64, h);
            var recusou = false;
            try
            {
                AplicadorDeEstradas.Aplicar(b);
            }
            catch (InvalidOperationException)
            {
                recusou = true;
            }
            Verificar(recusou, "Não protegeu edição externa dentro da união.");
            h[0, 0] = anteriorH;
            dados.SetHeights(48, 64, h);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            cena = EditorSceneManager.OpenScene(caminhoCena, OpenSceneMode.Single);
            a = Object.FindObjectsByType<EstradaNativa>().First(e => e.name == "Lajotas");
            b = Object.FindObjectsByType<EstradaNativa>().First(e => e.name == "Terra");
            terrenos = Object
                .FindObjectsByType<Terrain>()
                .OrderBy(t => t.transform.position.x)
                .ToArray();
            AplicadorDeEstradas.Restaurar(a);
            Verificar(((RegistroDaEstrada)b.Registro).Aplicada, "Recarga perdeu via restante.");
            AplicadorDeEstradas.Restaurar(b);
            ConferirBase(terrenos, alturasOriginais, detalhesOriginais, arvoresOriginais);
            // Diário interrompido misturando células na base e no resultado aplicado.
            AplicadorDeEstradas.Aplicar(a);
            AplicadorDeEstradas.Aplicar(b);
            var redeInterrompida = ((RegistroDaEstrada)a.Registro).Rede;
            redeInterrompida.EmAplicacao = true;
            ((RegistroDaEstrada)a.Registro).EmAplicacao = true;
            ((RegistroDaEstrada)b.Registro).EmAplicacao = true;
            foreach (var alteracao in redeInterrompida.Alteracoes)
            {
                if (alteracao.IndicesAltura.Length == 0)
                    continue;
                var indice = alteracao.IndicesAltura[0];
                alteracao.Dados.SetHeights(
                    indice % alteracao.ResolucaoAltura,
                    indice / alteracao.ResolucaoAltura,
                    new[,]
                    {
                        { alteracao.AlturasAntes[0] },
                    }
                );
            }
            EditorUtility.SetDirty(redeInterrompida);
            EditorUtility.SetDirty(a.Registro);
            EditorUtility.SetDirty(b.Registro);
            AssetDatabase.SaveAssets();
            AplicadorDeEstradas.RestaurarRedeInterrompida();
            ConferirBase(terrenos, alturasOriginais, detalhesOriginais, arvoresOriginais);
            Verificar(
                !((RegistroDaEstrada)a.Registro).Aplicada
                    && !((RegistroDaEstrada)b.Registro).Aplicada,
                "Recuperação interrompida deixou registros aplicados."
            );
            // Cenário final pronto para olhar e publicar pelo fluxo existente.
            AplicadorDeEstradas.Aplicar(a);
            AplicadorDeEstradas.Aplicar(b);
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            // O Terrain gráfico pode adiar o upload de alturas no mesmo executeMethod.
            // Reabrir a cena salva confere persistência e captura o resultado que o autor verá.
            cena = EditorSceneManager.OpenScene(caminhoCena, OpenSceneMode.Single);
            a = Object.FindObjectsByType<EstradaNativa>().First(e => e.name == "Lajotas");
            b = Object.FindObjectsByType<EstradaNativa>().First(e => e.name == "Terra");
            var saida = Path.Combine(Path.GetFullPath(".."), "cruzamentos-validacao.png");
            Capturar(saida, new Vector3(44, 3.2f, 16));
            Capturar(
                saida.Replace(".png", "-panorama.png"),
                new Vector3(32, 3.2f, 16),
                new Vector3(20f, 42f, -38f)
            );
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            Verificar(
                !ShaderUtil.ShaderHasError(Shader.Find("JogoDoGrupo/Mundo/EstradaNativa")),
                "Shader tem erros."
            );
            var manter = new HashSet<string> { a.Id, b.Id };
            foreach (var via in new[] { a, b })
            {
                manter.Add(
                    Path.GetFileName(
                        Path.GetDirectoryName(AssetDatabase.GetAssetPath(via.Registro))
                    )
                );
                manter.Add(
                    Path.GetFileName(
                        Path.GetDirectoryName(
                            AssetDatabase.GetAssetPath(((RegistroDaEstrada)via.Registro).Rede)
                        )
                    )
                );
            }
            foreach (var id in idsGerados)
                if (!manter.Contains(id))
                    AssetDatabase.DeleteAsset(AplicadorDeEstradas.PastaGerada + "/" + id);
            Debug.Log(
                "[Cruzamentos] TODOS OS ENSAIOS PASSARAM. Cena: "
                    + caminhoCena
                    + "; imagem: "
                    + saida
            );
        }

        internal static void ValidarCenasSalvas()
        {
            if (Application.isBatchMode)
                return;
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var cena = SceneManager.GetSceneAt(i);
                if (cena.isDirty || string.IsNullOrEmpty(cena.path))
                    throw new InvalidOperationException(
                        "Salve todas as cenas abertas antes de executar a demonstração isolada."
                    );
            }
        }

        private static EstradaNativa CriarVia(
            Transform pai,
            string nome,
            TipoDeEstrada tipo,
            float largura
        )
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(pai, false);
            var e = objeto.AddComponent<EstradaNativa>();
            e.Id = Guid.NewGuid().ToString("N");
            var so = new SerializedObject(e);
            so.FindProperty("_tipo").enumValueIndex = (int)tipo;
            so.FindProperty("_largura").floatValue = largura;
            so.ApplyModifiedPropertiesWithoutUndo();
            return e;
        }

        private static void Definir(EstradaNativa via, Vector3[] pontos)
        {
            via.Spline.Spline = new Spline();
            foreach (var p in pontos)
                via.Spline.Spline.Add(new BezierKnot((float3)p), TangentMode.Linear);
        }

        private static bool Iguais(float[,] a, float[,] b) =>
            a.Cast<float>()
                .Zip(b.Cast<float>(), (x, y) => Mathf.Abs(x - y) < 0.000001f)
                .All(v => v);

        private static void ConferirBase(
            Terrain[] terrenos,
            float[][,] alturas,
            int[][,] detalhes,
            TreeInstance[][] arvores
        )
        {
            for (var t = 0; t < terrenos.Length; t++)
            {
                var d = terrenos[t].terrainData;
                Verificar(
                    Iguais(alturas[t], d.GetHeights(0, 0, 129, 129)),
                    "Restauração não devolveu alturas originais."
                );
                Verificar(
                    detalhes[t]
                        .Cast<int>()
                        .SequenceEqual(d.GetDetailLayer(0, 0, 64, 64, 0).Cast<int>()),
                    "Restauração não devolveu grama."
                );
                Verificar(
                    d.treeInstanceCount == arvores[t].Length,
                    "Restauração perdeu ou duplicou árvores."
                );
                Verificar(
                    Mathf.Approximately(terrenos[t].heightmapPixelError, 5f),
                    "Restauração não devolveu qualidade do Terrain."
                );
            }
        }

        private static void ConferirUniao(EstradaNativa a, EstradaNativa b)
        {
            Verificar(
                ReferenceEquals(
                    ((RegistroDaEstrada)a.Registro).Rede,
                    ((RegistroDaEstrada)b.Registro).Rede
                ),
                "Backups não compõem a rede."
            );
            var filtros = a.GetComponentsInChildren<MeshFilter>()
                .Concat(b.GetComponentsInChildren<MeshFilter>())
                .Where(f => f.name == "SuperficieEstrada")
                .ToArray();
            Verificar(filtros.Length == 1, "Há superfícies sobrepostas no encontro.");
            var filtro = filtros[0];
            var mesh = filtro.sharedMesh;
            var cores = mesh.colors;
            var centro = Enumerable
                .Range(0, mesh.vertexCount)
                .OrderBy(i =>
                    (
                        filtro.transform.TransformPoint(mesh.vertices[i])
                        - new Vector3(32, 3.2f, 16)
                    ).sqrMagnitude
                )
                .First();
            Verificar(
                cores[centro].a > 0.99f && cores[centro].r > 0.99f,
                "Centro tem transparência ou interrompe lajotas."
            );
        }

        internal static void Capturar(string caminho, Vector3 alvo, Vector3? deslocamento = null)
        {
            var objeto = new GameObject("Camera temporaria");
            var camera = objeto.AddComponent<Camera>();
            var render = RenderTexture.GetTemporary(1600, 1000, 24);
            var anterior = RenderTexture.active;
            try
            {
                camera.transform.position = alvo + (deslocamento ?? new Vector3(9f, 12f, -14f));
                camera.transform.LookAt(alvo);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.45f, 0.57f, 0.68f);
                camera.targetTexture = render;
                foreach (var terreno in Terrain.activeTerrains)
                    terreno.Flush();
                var pedido = new RenderPipeline.StandardRequest { destination = render };
                if (RenderPipeline.SupportsRenderRequest(camera, pedido))
                    RenderPipeline.SubmitRenderRequest(camera, pedido);
                else
                    camera.Render();
                // Aquece o pipeline antes de ler pixels da primeira renderização batch.
                if (RenderPipeline.SupportsRenderRequest(camera, pedido))
                    RenderPipeline.SubmitRenderRequest(camera, pedido);
                else
                    camera.Render();
                RenderTexture.active = render;
                var imagem = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                imagem.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                imagem.Apply();
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                Object.DestroyImmediate(imagem);
            }
            finally
            {
                RenderTexture.active = anterior;
                RenderTexture.ReleaseTemporary(render);
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
