using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    public class FerramentaDeEncostas : EditorWindow
    {
        [SerializeField]
        private TerrainLayer _rocha;

        [SerializeField]
        private float _inicio = PinturaDeEncostas.InicioPadrao;

        [SerializeField]
        private float _coberturaPlena = PinturaDeEncostas.CoberturaPlenaPadrao;

        [SerializeField]
        private float _intensidade = PinturaDeEncostas.IntensidadePadrao;
        private Vector2 _rolagem;
        private string _resultado;

        [MenuItem("Jogo do Grupo/Mundo/Texturas do terreno/Pintar encostas")]
        public static void Abrir() => GetWindow<FerramentaDeEncostas>("Pintar encostas");

        private void OnSelectionChange() => Repaint();

        private void OnGUI()
        {
            _rolagem = EditorGUILayout.BeginScrollView(_rolagem);
            var terreno =
                Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponent<Terrain>()
                    : null;
            EditorGUILayout.HelpBox(
                "Selecione um Terrain na Hierarchy. A pintura afeta somente esse terreno. "
                    + "Depois de aplicar, retoque normalmente com Paint Texture. Ctrl+Z desfaz.",
                MessageType.Info
            );
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Terreno selecionado", terreno, typeof(Terrain), true);
            _rocha = (TerrainLayer)
                EditorGUILayout.ObjectField("Camada de rocha", _rocha, typeof(TerrainLayer), false);
            _inicio = EditorGUILayout.Slider("Comecar em (graus)", _inicio, 0f, 89f);
            _coberturaPlena = EditorGUILayout.Slider(
                "Cobertura plena (graus)",
                _coberturaPlena,
                _inicio + 1f,
                90f
            );
            _intensidade = EditorGUILayout.Slider("Intensidade", _intensidade, 0.01f, 1f);
            EditorGUILayout.HelpBox(
                "A rocha aumenta gradualmente entre os dois angulos. O solo abaixo do primeiro "
                    + "angulo permanece intacto. Aplicar novamente so acrescenta rocha: reduzir os "
                    + "valores nao remove a pintura anterior. Use Undo para experimentar outra configuracao. "
                    + "Nas encostas, estradas e outros retoques podem ser cobertos; finalize-os depois.",
                MessageType.Info
            );
            EditorGUILayout.HelpBox(
                "Se houver um Terrain Painter controlando este terreno, desative o componente "
                    + "e Auto Repaint antes dos retoques. Uma nova aplicacao procedural pode sobrescreve-los.",
                MessageType.Warning
            );
            var erro = Validar(terreno);
            if (erro != null)
                EditorGUILayout.HelpBox(erro, MessageType.Warning);
            using (new EditorGUI.DisabledScope(erro != null))
                if (GUILayout.Button("Aplicar rocha nas encostas"))
                    Aplicar(terreno);
            if (!string.IsNullOrEmpty(_resultado))
                EditorGUILayout.HelpBox(_resultado, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private string Validar(Terrain terreno)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Saia do Play para pintar o mapa de autoria.";
            if (terreno == null || terreno.terrainData == null)
                return "Selecione um objeto com componente Terrain na Hierarchy.";
            if (!EditorUtility.IsPersistent(terreno.terrainData))
                return "O TerrainData precisa estar salvo como asset do projeto.";
            if (
                terreno.transform.lossyScale != Vector3.one
                || Quaternion.Angle(terreno.transform.rotation, Quaternion.identity) > 0.01f
            )
                return "Use Terrain com escala 1 e rotacao 0 para medir a inclinacao corretamente.";
            if (
                terreno.terrainData.terrainLayers.Length == 0
                || terreno.terrainData.terrainLayers.Any(c => c == null)
            )
                return "Configure primeiro as camadas do solo em Paint Texture, sem referencias vazias.";
            if (_rocha == null || _rocha.diffuseTexture == null)
                return "Arraste uma TerrainLayer com textura para Camada de rocha.";
            return null;
        }

        private void Aplicar(Terrain terreno)
        {
            var dados = terreno.terrainData;
            try
            {
                PinturaDeEncostas.AplicarComUndo(
                    dados,
                    _rocha,
                    _inicio,
                    _coberturaPlena,
                    _intensidade,
                    progresso =>
                        EditorUtility.DisplayCancelableProgressBar(
                            "Pintar encostas",
                            terreno.name,
                            progresso
                        )
                );
                _resultado =
                    "Pintura aplicada. Use Ctrl+Z para desfazer ou Paint Texture para retocar. Salve o projeto para gravar.";
            }
            catch (Exception erro)
            {
                _resultado =
                    erro is OperationCanceledException
                        ? "Pintura cancelada; o terreno foi restaurado."
                        : "Nao foi possivel pintar: " + erro.Message;
                if (!(erro is OperationCanceledException))
                    Debug.LogException(erro);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                terreno.Flush();
                SceneView.RepaintAll();
            }
        }
    }
}
