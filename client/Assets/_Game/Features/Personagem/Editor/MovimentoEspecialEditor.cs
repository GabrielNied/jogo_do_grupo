using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem.Editor
{
    /// <summary>
    /// Ferramentas dos marcadores de natação e escalada (#349): cria a cena de validação e exporta
    /// os marcadores da cena para o catálogo que o servidor confere (<c>VolumesMarcados</c>).
    /// Com o editor fechado, a cena sai de
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.MovimentoEspecialEditor.CriarCenaDeValidacao</c>.
    /// </summary>
    public static class MovimentoEspecialEditor
    {
        private const string PastaDaCena = "Assets/_Game/Features/Personagem/Dev/Movimento";
        private const string CaminhoDaCena = PastaDaCena + "/ValidacaoMovimentoEspecial.unity";
        private const string InicioDaLista = "// INICIO-DOS-VOLUMES";
        private const string FimDaLista = "// FIM-DOS-VOLUMES";

        [MenuItem("Jogo do Grupo/Personagem/Criar cena de validação de movimento")]
        public static void CriarCenaDeValidacao()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", PastaDaCena));

            var chao = Material("Chao", new Color(0.35f, 0.5f, 0.3f));
            var pedra = Material("Pedra", new Color(0.45f, 0.45f, 0.5f));
            var agua = Material("Agua", new Color(0.15f, 0.4f, 0.8f));

            var luz = new GameObject("Sol");
            var componenteLuz = luz.AddComponent<Light>();
            componenteLuz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var camera = new GameObject("CameraDeInspecao") { tag = "MainCamera" };
            camera.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(
                new Vector3(0f, 14f, -26f),
                Quaternion.Euler(20f, 0f, 0f)
            );

            var raiz = new GameObject("Cenario").transform;

            // Chão em volta de um buraco de 12 x 12 m (a piscina).
            Bloco(raiz, "ChaoNorte", chao, new Vector3(0f, -0.5f, 23f), new Vector3(80f, 1f, 34f));
            Bloco(raiz, "ChaoSul", chao, new Vector3(0f, -0.5f, -23f), new Vector3(80f, 1f, 34f));
            Bloco(raiz, "ChaoOeste", chao, new Vector3(-23f, -0.5f, 0f), new Vector3(34f, 1f, 12f));
            Bloco(raiz, "ChaoLeste", chao, new Vector3(23f, -0.5f, 0f), new Vector3(34f, 1f, 12f));

            // Piscina de 3 m de água: fundo em y = -3, superfície em y = 0.
            Bloco(
                raiz,
                "FundoDaPiscina",
                pedra,
                new Vector3(0f, -3.5f, 0f),
                new Vector3(12f, 1f, 12f)
            );
            var superficie = Bloco(
                raiz,
                "SuperficieDaAgua",
                agua,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(12f, 0.02f, 12f)
            );
            DestruirColisor(superficie);
            Marcador(
                raiz,
                "Agua-Piscina",
                TipoDeVolume.Agua,
                "validacao-piscina",
                new Vector3(0f, -1.5f, 0f),
                new Vector3(12f, 3f, 12f),
                0f
            );

            // Torre de 12 m com uma via de escalada na face sul (a frente da via aponta para +Z).
            Bloco(raiz, "Torre", pedra, new Vector3(0f, 6f, 25f), new Vector3(10f, 12f, 10f));
            Marcador(
                raiz,
                "Via-Torre",
                TipoDeVolume.ViaDeEscalada,
                "validacao-torre",
                new Vector3(0f, 6f, 18.5f),
                new Vector3(6f, 12f, 3f),
                0f
            );

            // Plataformas para o dano de queda: 3 m (sem dano), 7 m e 12 m.
            Bloco(raiz, "Degrau3m", pedra, new Vector3(-20f, 1.5f, -12f), new Vector3(6f, 3f, 6f));
            Bloco(
                raiz,
                "Plataforma7m",
                pedra,
                new Vector3(-30f, 3.5f, -12f),
                new Vector3(6f, 7f, 6f)
            );
            Bloco(
                raiz,
                "Plataforma12m",
                pedra,
                new Vector3(-40f, 6f, -12f),
                new Vector3(6f, 12f, 6f)
            );
            Bloco(raiz, "Escada", pedra, new Vector3(-24f, 6f, -20f), new Vector3(2f, 12f, 2f));

            new GameObject("Inicio").transform.position = new Vector3(0f, 0f, -10f);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            AssetDatabase.SaveAssets();
            Debug.Log("[MovimentoEspecial] Cena de validação criada em " + CaminhoDaCena);
        }

        [MenuItem("Jogo do Grupo/Personagem/Exportar volumes marcados")]
        public static void ExportarVolumesMarcados()
        {
            var marcadores = UnityEngine
                .Object.FindObjectsByType<MarcadorDeVolume>(FindObjectsInactive.Include)
                .Where(m => m.Exportar)
                .OrderBy(m => m.Id, StringComparer.Ordinal)
                .ToArray();

            var repetidos = marcadores.GroupBy(m => m.Id).Where(g => g.Count() > 1).ToArray();
            if (marcadores.Any(m => string.IsNullOrWhiteSpace(m.Id)) || repetidos.Length > 0)
            {
                Debug.LogError(
                    "[MovimentoEspecial] Cada marcador exportado precisa de um Id único e preenchido."
                );
                return;
            }

            var caminho = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    "shared",
                    "Package",
                    "Runtime",
                    "Features",
                    "Personagem",
                    "VolumesMarcados.cs"
                )
            );
            var texto = File.ReadAllText(caminho);
            var novo = SubstituirLista(texto, marcadores.Select(m => m.Medir()).ToArray());
            File.WriteAllText(caminho, novo);
            Debug.Log(
                $"[MovimentoEspecial] {marcadores.Length} volume(s) exportado(s) para VolumesMarcados.cs. Rode o dotnet csharpier format e inclua o arquivo no commit."
            );
        }

        /// <summary>Troca o trecho entre as marcas de <c>VolumesMarcados</c> pelos volumes dados.</summary>
        internal static string SubstituirLista(string arquivo, VolumeMarcado[] volumes)
        {
            var inicio = arquivo.IndexOf(InicioDaLista, StringComparison.Ordinal);
            var fim = arquivo.IndexOf(FimDaLista, StringComparison.Ordinal);
            if (inicio < 0 || fim < inicio)
            {
                throw new InvalidOperationException(
                    "VolumesMarcados.cs perdeu as marcas INICIO-DOS-VOLUMES e FIM-DOS-VOLUMES."
                );
            }

            var fimDaLinhaDeInicio = arquivo.IndexOf('\n', inicio) + 1;
            var corpo = new StringBuilder();
            foreach (var v in volumes)
            {
                corpo
                    .Append("            new VolumeMarcado(")
                    .Append('"')
                    .Append(v.Id)
                    .Append("\", TipoDeVolume.")
                    .Append(v.Tipo)
                    .Append(", ")
                    .Append(Numero(v.CentroX))
                    .Append(", ")
                    .Append(Numero(v.CentroY))
                    .Append(", ")
                    .Append(Numero(v.CentroZ))
                    .Append(", ")
                    .Append(Numero(v.TamanhoX))
                    .Append(", ")
                    .Append(Numero(v.TamanhoY))
                    .Append(", ")
                    .Append(Numero(v.TamanhoZ))
                    .Append(", ")
                    .Append(Numero(v.RotacaoY))
                    .Append("),\n");
            }

            var inicioDaLinhaDeFim = arquivo.LastIndexOf('\n', fim) + 1;
            return arquivo.Substring(0, fimDaLinhaDeInicio)
                + corpo
                + arquivo.Substring(inicioDaLinhaDeFim);
        }

        private static string Numero(float valor) =>
            Math.Round(valor, 3).ToString("0.###", CultureInfo.InvariantCulture) + "f";

        private static Material Material(string nome, Color cor)
        {
            var caminho = PastaDaCena + "/Materiais/" + nome + ".mat";
            Directory.CreateDirectory(
                Path.Combine(Application.dataPath, "..", PastaDaCena, "Materiais")
            );
            var existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (existente != null)
            {
                return existente;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = cor };
            AssetDatabase.CreateAsset(material, caminho);
            return material;
        }

        private static GameObject Bloco(
            Transform pai,
            string nome,
            Material material,
            Vector3 centro,
            Vector3 tamanho
        )
        {
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nome;
            cubo.transform.SetParent(pai, false);
            cubo.transform.position = centro;
            cubo.transform.localScale = tamanho;
            cubo.GetComponent<Renderer>().sharedMaterial = material;
            return cubo;
        }

        private static void DestruirColisor(GameObject objeto)
        {
            UnityEngine.Object.DestroyImmediate(objeto.GetComponent<Collider>());
        }

        private static void Marcador(
            Transform pai,
            string nome,
            TipoDeVolume tipo,
            string id,
            Vector3 centro,
            Vector3 tamanho,
            float giroY
        )
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(pai, false);
            objeto.transform.SetPositionAndRotation(centro, Quaternion.Euler(0f, giroY, 0f));
            objeto.transform.localScale = tamanho;
            // Cena de validação: vale só no client, não vai para o catálogo do servidor.
            objeto.AddComponent<MarcadorDeVolume>().Definir(tipo, id, false);
        }
    }
}
