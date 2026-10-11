using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.LaboratorioVisual.Editor
{
    public sealed class JanelaLaboratorio : EditorWindow
    {
        private TelaLaboratorio _tela;
        private RecursosLaboratorio _recursos;
        private VisualElement _palco;
        private Vector2Int _resolucao = new Vector2Int(1920, 1080);
        private float _zoom = 0.5f;

        [MenuItem("Lumora/Design/Laboratório visual")]
        public static void Abrir()
        {
            var janela = GetWindow<JanelaLaboratorio>();
            janela.titleContent = new GUIContent("Lumora • Ateliê");
            janela.minSize = new Vector2(960, 600);
            janela.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            try
            {
                _recursos = new RecursosLaboratorio();
                _tela = new TelaLaboratorio(_recursos);
            }
            catch (Exception erro)
            {
                rootVisualElement.Add(
                    new Label("Não foi possível preparar o laboratório. " + erro.Message)
                );
                rootVisualElement.Add(new Button(CreateGUI) { text = "Tentar novamente" });
                Debug.LogException(erro);
                return;
            }
            var ferramentas = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingTop = 6,
                    paddingBottom = 6,
                },
            };
            rootVisualElement.Add(ferramentas);
            var resolucao = new DropdownField(
                "Área de teste",
                new System.Collections.Generic.List<string>
                {
                    "1280 × 720",
                    "1920 × 1080",
                    "2560 × 1440",
                },
                1
            );
            resolucao.RegisterValueChangedCallback(e =>
            {
                var medidas = new[]
                {
                    new Vector2Int(1280, 720),
                    new Vector2Int(1920, 1080),
                    new Vector2Int(2560, 1440),
                };
                _resolucao = medidas[resolucao.index];
                Dimensionar();
            });
            ferramentas.Add(resolucao);
            var zoom = new Slider("Zoom", 0.5f, 1f)
            {
                value = 0.5f,
                showInputField = true,
                style = { width = 230, marginLeft = 16 },
            };
            zoom.RegisterValueChangedCallback(e =>
            {
                _zoom = e.newValue;
                Dimensionar();
            });
            ferramentas.Add(zoom);
            ferramentas.Add(new Button(ExportarRevisao) { text = "Exportar ficha de revisão" });
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                style = { flexGrow = 1 },
            };
            rootVisualElement.Add(scroll);
            _palco = new VisualElement { style = { flexShrink = 0 } };
            scroll.Add(_palco);
            _palco.Add(_tela.Raiz);
            _tela.Raiz.style.position = Position.Absolute;
            _tela.Raiz.style.transformOrigin = new TransformOrigin(0, 0, 0);
            Dimensionar();
        }

        private void Dimensionar()
        {
            _palco.style.width = _resolucao.x * _zoom;
            _palco.style.height = _resolucao.y * _zoom;
            _tela.Raiz.style.width = _resolucao.x;
            _tela.Raiz.style.height = _resolucao.y;
            _tela.Raiz.style.scale = new Scale(new Vector3(_zoom, _zoom, 1));
        }

        private void ExportarRevisao()
        {
            var pasta = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../design/laboratorio-unity/revisoes")
            );
            Directory.CreateDirectory(pasta);
            var arquivo = Path.Combine(
                pasta,
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
                    + "-"
                    + Guid.NewGuid().ToString("N")
                    + ".md"
            );
            var relatorio = new StringBuilder();
            relatorio.AppendLine("# Revisão visual — pendente de aprovação humana");
            relatorio.AppendLine("\nData UTC: " + DateTime.UtcNow.ToString("O"));
            relatorio.AppendLine("\nUnity: " + Application.unityVersion);
            relatorio.AppendLine("\nExemplo: " + TelaLaboratorio.Secoes[_tela.SecaoAtual]);
            relatorio.AppendLine(
                "\nÁrea lógica: "
                    + _resolucao
                    + "; zoom: "
                    + _zoom
                    + "; texto: "
                    + _tela.TamanhoTexto
                    + " px; comparação: "
                    + _tela.Comparando
            );
            relatorio.AppendLine("\n## Verificações estruturais");
            relatorio.AppendLine("\n" + ValidacaoLaboratorio.Relatar(_tela.Raiz));
            relatorio.AppendLine(
                "\n## Avaliação humana\n\n- [ ] Tipografia e acentos\n- [ ] Sem cortes em 16, 20 e 24 px\n- [ ] Folhas e calçada proporcionais\n- [ ] Hover, foco, pressionado e desabilitado\n- [ ] Contraste e hierarquia\n- [ ] Comparação com prancha e capturas anexadas\n\nResultado: pendente.\n\nObservações:\n"
            );
            relatorio.AppendLine("\n## Recursos usados\n\n```text\n" + _recursos.Impressao + "```");
            File.WriteAllText(arquivo, relatorio.ToString());
            EditorUtility.RevealInFinder(arquivo);
        }
    }
}
