using System;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    [CustomEditor(typeof(EstradaNativa))]
    public sealed class InspectorDaEstradaNativa : UnityEditor.Editor
    {
        private bool _previa = true;
        private string _erro;

        [MenuItem("Jogo do Grupo/Mundo/Estradas/Criar estrada de terra")]
        public static void Criar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var terreno =
                Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponent<Terrain>()
                    : null;
            var cena = SceneManager.GetActiveScene();
            if (terreno != null && terreno.gameObject.scene != cena)
                terreno = null;
            if (terreno == null)
                terreno = Terrain.activeTerrains.FirstOrDefault(t => t.gameObject.scene == cena);
            var hub = cena.GetRootGameObjects()
                .FirstOrDefault(o => o.name == ConstrutorHub.NomeRaiz);
            Transform pai = hub != null ? hub.transform : null;
            var objeto = new GameObject("Estrada de terra");
            Undo.RegisterCreatedObjectUndo(objeto, "Criar estrada");
            if (pai != null)
                objeto.transform.SetParent(pai, true);
            var centro =
                terreno != null
                    ? terreno.transform.position + terreno.terrainData.size * 0.5f
                    : Vector3.zero;
            if (SceneView.lastActiveSceneView != null && terreno != null)
            {
                var foco = SceneView.lastActiveSceneView.pivot;
                var local = foco - terreno.transform.position;
                if (
                    local.x >= 8f
                    && local.x <= terreno.terrainData.size.x - 8f
                    && local.z >= 8f
                    && local.z <= terreno.terrainData.size.z - 8f
                )
                    centro = foco;
            }
            if (terreno != null)
                centro.y = terreno.SampleHeight(centro) + terreno.transform.position.y;
            objeto.transform.position = centro;
            var spline = objeto.AddComponent<SplineContainer>();
            var comprimento =
                terreno != null ? Mathf.Min(12f, terreno.terrainData.size.x * 0.2f) : 12f;
            spline.Spline = new Spline();
            spline.Spline.Add(
                new BezierKnot(new float3(-comprimento * 0.5f, 0f, 0f)),
                TangentMode.AutoSmooth
            );
            spline.Spline.Add(
                new BezierKnot(new float3(comprimento * 0.5f, 0f, 0f)),
                TangentMode.AutoSmooth
            );
            var estrada = objeto.AddComponent<EstradaNativa>();
            estrada.Id = Guid.NewGuid().ToString("N");
            Selection.activeGameObject = objeto;
            EditorSceneManager.MarkSceneDirty(objeto.scene);
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_largura"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_transicaoVisual"),
                new GUIContent("Transição visual (m)")
            );
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_transicao"),
                new GUIContent("Suavização do relevo (m)")
            );
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_rebaixo"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_pedregulhos"),
                new GUIContent("Cascalhos")
            );
            using (
                new EditorGUI.DisabledScope(
                    !serializedObject.FindProperty("_pedregulhos").boolValue
                )
            )
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("_densidadeCascalhos"),
                    new GUIContent("Cascalhos por m²")
                );
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("_corCascalhos"),
                    new GUIContent("Cor dos cascalhos")
                );
            }
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "_id",
                "_registroGuid",
                "_largura",
                "_transicao",
                "_transicaoVisual",
                "_rebaixo",
                "_pedregulhos",
                "_densidadeCascalhos",
                "_corCascalhos"
            );
            serializedObject.ApplyModifiedProperties();
            var estrada = (EstradaNativa)target;
            var registro = estrada.Registro as RegistroDaEstrada;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Edite os pontos com as ferramentas oficiais do Spline Container (Edit Spline). A altura da spline é uma guia: a estrada acompanha o Terrain. O rebaixo forma uma depressão suave pelo uso de pessoas e cavalos.",
                MessageType.Info
            );
            EditorGUILayout.HelpBox(
                "Transição visual prolonga a transparência da estrada sobre o solo, sem alargar o rebaixo. Suavização do relevo controla só a depressão física. Após mudar os valores, clique em Aplicar/Reaplicar e salvar para atualizar o resultado. A linha azul mostra o limite da mistura; a verde, a limpeza de vegetação.",
                MessageType.Info
            );
            if (estrada.Material != null)
                EditorGUILayout.HelpBox(
                    "Material próprio: a transição visual exige que o shader respeite o alpha da cor dos vértices. Um material opaco não fará a mistura com o Terrain.",
                    MessageType.Warning
                );
            _previa = EditorGUILayout.Toggle("Prévia do corredor na cena", _previa);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (
                    GUILayout.Button(
                        registro != null && registro.Aplicada
                            ? "Reaplicar e salvar"
                            : "Aplicar e salvar"
                    )
                )
                    Executar(() => AplicadorDeEstradas.Aplicar(estrada));
                using (new EditorGUI.DisabledScope(registro == null || !registro.Aplicada))
                    if (GUILayout.Button("Restaurar terreno e vegetação"))
                        Executar(() => AplicadorDeEstradas.Restaurar(estrada));
            }
            if (registro != null)
                EditorGUILayout.LabelField(
                    "Estado",
                    registro.EmAplicacao ? "Aplicação interrompida: restaure antes de continuar"
                        : registro.Aplicada ? "Aplicada (backup persistente)"
                        : "Restaurada"
                );
            if (!string.IsNullOrEmpty(_erro))
                EditorGUILayout.HelpBox(_erro, MessageType.Error);
            foreach (var terreno in AplicadorDeEstradas.TerrenosDa(estrada))
            {
                if (terreno.terrainData == null)
                    continue;
                var dados = terreno.terrainData;
                var passo =
                    Mathf.Max(dados.size.x, dados.size.z) / (dados.heightmapResolution - 1f);
                if (passo > estrada.Largura * 0.25f)
                    EditorGUILayout.HelpBox(
                        terreno.name
                            + ": a grade de altura tem "
                            + passo.ToString("0.##")
                            + " m por célula. Estradas estreitas terão relevo físico suave/limitado; use blocos de maior resolução para detalhe. A publicação do servidor usa a grade de 10 m do mapa.",
                        MessageType.Warning
                    );
            }
            EditorGUILayout.HelpBox(
                "Aplicar salva esta cena e os TerrainData. A limpeza alcança árvores e todas as camadas nativas de detalhes dentro da faixa + margem. Objetos plantados manualmente são independentes. Restaure antes de editar o terreno dentro da faixa ou remover o objeto. Corredores de estradas diferentes precisam ficar separados nesta versão. O mapa principal só muda quando você publicar pelo fluxo de autoria existente.",
                MessageType.Info
            );
            EditorGUILayout.HelpBox(
                "Os blocos tocados recebem maior precisão de relevo para a estrada não ser atravessada pelo Terrain à distância. Isso pode aumentar a quantidade de triângulos renderizados. A última estrada restaurada no bloco repõe a configuração anterior, preservando um ajuste manual posterior.",
                MessageType.Info
            );
        }

        private void Executar(Action acao)
        {
            try
            {
                acao();
                _erro = null;
            }
            catch (Exception e)
            {
                _erro = e.Message;
                Debug.LogException(e, target);
            }
            SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            if (!_previa)
                return;
            var estrada = (EstradaNativa)target;
            try
            {
                var pontos = PerfilDaEstrada.Amostrar(estrada);
                var terrenos = AplicadorDeEstradas.TerrenosDa(estrada);
                var centro = new Vector3[pontos.Count];
                var esquerda = new Vector3[pontos.Count];
                var direita = new Vector3[pontos.Count];
                var visualEsquerda = new Vector3[pontos.Count];
                var visualDireita = new Vector3[pontos.Count];
                for (var i = 0; i < pontos.Count; i++)
                {
                    var p = pontos[i];
                    var direcao =
                        pontos[Mathf.Min(i + 1, pontos.Count - 1)] - pontos[Mathf.Max(i - 1, 0)];
                    var lado = new Vector3(-direcao.z, 0f, direcao.x).normalized;
                    var raio =
                        PerfilDaEstrada.MeiaLargura(estrada, p)
                        + estrada.TransicaoDoCorredor
                        + estrada.MargemVegetacao;
                    centro[i] = Apoiar(p, terrenos);
                    esquerda[i] = Apoiar(p + lado * raio, terrenos);
                    direita[i] = Apoiar(p - lado * raio, terrenos);
                    var raioVisual =
                        PerfilDaEstrada.MeiaLargura(estrada, p) + estrada.TransicaoVisual;
                    visualEsquerda[i] = Apoiar(p + lado * raioVisual, terrenos);
                    visualDireita[i] = Apoiar(p - lado * raioVisual, terrenos);
                }
                Handles.color = new Color(1f, 0.7f, 0.2f);
                Handles.DrawAAPolyLine(2f, centro);
                Handles.color = new Color(0.3f, 0.65f, 1f);
                Handles.DrawAAPolyLine(2f, visualEsquerda);
                Handles.DrawAAPolyLine(2f, visualDireita);
                Handles.color = new Color(0.3f, 0.9f, 0.6f);
                Handles.DrawAAPolyLine(2f, esquerda);
                Handles.DrawAAPolyLine(2f, direita);
                Handles.DrawDottedLine(esquerda[0], direita[0], 3f);
                Handles.DrawDottedLine(
                    esquerda[esquerda.Length - 1],
                    direita[direita.Length - 1],
                    3f
                );
            }
            catch (InvalidOperationException) { }
        }

        private static Vector3 Apoiar(Vector3 p, Terrain[] terrenos)
        {
            try
            {
                p.y = AplicadorDeEstradas.AlturaEm(terrenos, p) + 0.08f;
            }
            catch (InvalidOperationException) { }
            return p;
        }
    }
}
