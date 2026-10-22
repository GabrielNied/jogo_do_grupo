using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    public static class ValidacaoDoBackupLegado
    {
        [MenuItem("Jogo do Grupo/Mundo/Estradas/Validar migração do backup anterior")]
        public static void Executar()
        {
            var cenaAnterior = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!Application.isBatchMode && cenaAnterior.isDirty)
                throw new InvalidOperationException(
                    "Salve a cena atual antes da validação isolada."
                );
            var retorno = "Assets/_Game/Features/Mundo/DemoCruzamentos/DemoCruzamentos.unity";
            var pasta = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/_Game/Features/Mundo/ValidacaoBackupLegado"
            );
            AplicadorDeEstradas.GarantirPasta(pasta);
            var cena = EditorSceneManager.OpenScene(
                "Assets/_Game/Features/Mundo/DemoEstradas/DemoEstradas.unity",
                OpenSceneMode.Single
            );
            var terrenos = Object.FindObjectsByType<Terrain>();
            var copias = new Dictionary<TerrainData, TerrainData>();
            foreach (var terreno in terrenos)
            {
                var original = terreno.terrainData;
                var copia = Object.Instantiate(original);
                AssetDatabase.CreateAsset(copia, pasta + "/" + original.name + ".asset");
                terreno.terrainData = copia;
                terreno.GetComponent<TerrainCollider>().terrainData = copia;
                copias.Add(original, copia);
            }
            var vias = Object.FindObjectsByType<EstradaNativa>().OrderBy(e => e.Tipo).ToArray();
            var perfis = new List<AlteracaoDeTerreno>();
            var ids = new List<string>();
            foreach (var via in vias)
            {
                var original = (RegistroDaEstrada)via.Registro;
                Verificar(
                    original.Rede == null && string.IsNullOrEmpty(original.ConfiguracaoAplicada),
                    "A receita de migração precisa dos registros anteriores reais."
                );
                var copia = Object.Instantiate(original);
                AssetDatabase.CreateAsset(copia, pasta + "/Registro" + via.Tipo + ".asset");
                foreach (var a in copia.Alteracoes)
                    a.Dados = copias[a.Dados];
                perfis.AddRange(copia.Alteracoes);
                via.Id = Guid.NewGuid().ToString("N");
                ids.Add(via.Id);
                via.Registro = copia;
            }
            EditorSceneManager.SaveScene(cena, pasta + "/Legado.unity");
            var terra = vias[0];
            var lajotas = vias[1];
            var larguraOriginal = lajotas.Largura;
            var alturasAntes = terrenos.ToDictionary(
                t => t,
                t => t.terrainData.GetHeights(0, 0, 129, 129)
            );
            // A versão anterior não guardava todos os parâmetros. Uma mudança pendente não pode
            // recalcular o perfil do vizinho; suas células legadas continuam exatas até reaplicá-lo.
            var so = new SerializedObject(lajotas);
            so.FindProperty("_largura").floatValue = 8f;
            so.FindProperty("_rebaixo").floatValue = 0.3f;
            so.FindProperty("_tipo").enumValueIndex = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            AplicadorDeEstradas.Aplicar(terra);
            foreach (var terreno in terrenos)
                Verificar(
                    alturasAntes[terreno]
                        .Cast<float>()
                        .SequenceEqual(
                            terreno.terrainData.GetHeights(0, 0, 129, 129).Cast<float>()
                        ),
                    "Migrar alterou o perfil aplicado de uma vizinha."
                );
            Verificar(
                Mathf.Approximately(lajotas.Largura, 8f) && lajotas.Tipo == TipoDeEstrada.Terra,
                "Migração perdeu ajustes pendentes do Inspector."
            );
            Verificar(
                Mathf.Approximately(((RegistroDaEstrada)lajotas.Registro).Largura, larguraOriginal),
                "Migração aplicou largura pendente."
            );
            Verificar(
                lajotas
                    .transform.Find("SuperficieEstrada")
                    .GetComponent<Renderer>()
                    .sharedMaterial.GetFloat("_Tipo") == 1f,
                "Restauração/migração perdeu o material padrão de lajotas."
            );
            foreach (var terreno in terrenos)
                terreno.heightmapPixelError = 0.15f;
            terra.Spline.Spline = new Spline();
            terra.Spline.Spline.Add(new BezierKnot(new float3(32, 3, 6)), TangentMode.Linear);
            terra.Spline.Spline.Add(new BezierKnot(new float3(32, 3, 26)), TangentMode.Linear);
            AplicadorDeEstradas.Aplicar(terra);
            Verificar(
                vias.SelectMany(e => e.GetComponentsInChildren<MeshFilter>())
                    .Count(f => f.name == "SuperficieEstrada") == 1,
                "Via nova não conectou ao perfil legado."
            );
            AplicadorDeEstradas.Restaurar(terra);
            Verificar(
                lajotas
                    .transform.Find("SuperficieEstrada")
                    .GetComponent<Renderer>()
                    .sharedMaterial.GetFloat("_Tipo") == 1f,
                "Restauração parcial perdeu lajotas."
            );
            var perfilLegado = ((RegistroDaEstrada)lajotas.Registro).PerfilLegado;
            foreach (var a in perfilLegado)
                for (var i = 0; i < a.IndicesAltura.Length; i++)
                {
                    var indice = a.IndicesAltura[i];
                    var atual = a.Dados.GetHeights(
                        indice % a.ResolucaoAltura,
                        indice / a.ResolucaoAltura,
                        1,
                        1
                    )[0, 0];
                    Verificar(
                        Mathf.Abs(atual - a.AlturasDepois[i]) < 0.000001f,
                        "Restauração alterou perfil legado restante."
                    );
                }
            EditorSceneManager.SaveScene(cena);
            AssetDatabase.SaveAssets();
            cena = EditorSceneManager.OpenScene(cena.path, OpenSceneMode.Single);
            lajotas = Object.FindObjectsByType<EstradaNativa>().First(e => e.Id == ids[1]);
            AplicadorDeEstradas.Restaurar(lajotas);
            foreach (var a in perfis)
                for (var i = 0; i < a.IndicesAltura.Length; i++)
                {
                    var indice = a.IndicesAltura[i];
                    Verificar(
                        Mathf.Abs(
                            a.Dados.GetHeights(
                                indice % a.ResolucaoAltura,
                                indice / a.ResolucaoAltura,
                                1,
                                1
                            )[0, 0] - a.AlturasAntes[i]
                        ) < 0.000001f,
                        "Backup anterior não restaurou a base após recarga."
                    );
                }
            Verificar(
                Object
                    .FindObjectsByType<Terrain>()
                    .All(t => Mathf.Approximately(t.heightmapPixelError, 0.15f)),
                "Restauração apagou ajuste manual de qualidade."
            );
            // Reabre o cenário final já aprovado para atualizar a captura com upload gráfico completo.
            EditorSceneManager.OpenScene(retorno, OpenSceneMode.Single);
            ValidacaoDosCruzamentos.Capturar(
                Path.GetFullPath("../cruzamentos-validacao.png"),
                new Vector3(44, 3.2f, 16)
            );
            ValidacaoDosCruzamentos.Capturar(
                Path.GetFullPath("../cruzamentos-validacao-panorama.png"),
                new Vector3(32, 3.2f, 16),
                new Vector3(20, 42, -38)
            );
            AssetDatabase.DeleteAsset(pasta);
            foreach (var id in ids)
                AssetDatabase.DeleteAsset(AplicadorDeEstradas.PastaGerada + "/" + id);
            Debug.Log(
                "[Cruzamentos] BACKUP LEGADO APROVADO: perfil original, ajustes pendentes, material de lajotas, conexão, restauração parcial/total e recarga."
            );
        }

        private static void Verificar(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException("[Cruzamentos legado] " + mensagem);
        }
    }
}
