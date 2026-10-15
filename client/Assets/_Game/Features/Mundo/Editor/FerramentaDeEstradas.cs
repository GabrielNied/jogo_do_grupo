using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Autoria da estrada do Passo na vista 3D, com previa sem salvar objetos na cena.</summary>
    public sealed class FerramentaDeEstradas : EditorWindow
    {
        [SerializeField]
        private List<Vector2> _pontos = new List<Vector2>();

        [SerializeField]
        private float _largura = 7f;

        [SerializeField]
        private float _borda = 8f;

        [SerializeField]
        private float _suavizacao = 18f;

        private GameObject _raiz;
        private MeshCollider _colisor;
        private Material _materialTerreno;
        private Material _materialEstrada;
        private bool _adicionar;
        private string _erro;

        [MenuItem("Jogo do Grupo/Mundo/Autoria de estradas")]
        public static void Abrir()
        {
            GetWindow<FerramentaDeEstradas>("Estradas do mundo").Show();
        }

        /// <summary>Verifica a montagem da prévia em modo batch, sem gravar a cena.</summary>
        public static void VerificarPrevia()
        {
            var janela = GetWindow<FerramentaDeEstradas>();
            janela.Reverter();
            if (janela._colisor == null || janela._raiz == null)
            {
                throw new InvalidOperationException("A prévia da estrada não foi montada.");
            }

            janela.Close();
        }

        private void OnEnable()
        {
            _pontos ??= new List<Vector2>();
            if (_pontos.Count < 2)
            {
                Reverter();
            }
            else
            {
                EditorApplication.delayCall += RecriarPrevia;
            }

            SceneView.duringSceneGui += DesenharNaCena;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DesenharNaCena;
            EditorApplication.delayCall -= RecriarPrevia;
            LimparPrevia();
        }

        private void OnGUI()
        {
            EditorGUILayout.Popup("Elemento", 0, new[] { "Estrada" });
            EditorGUILayout.LabelField("Rota", "Cidade central → Passo");
            EditorGUILayout.HelpBox(
                "Clique em 'Adicionar ponto' e depois no mapa 3D. Arraste as alças dos pontos "
                    + "intermediários para ajustar o caminho. A prévia não altera a cena salva.",
                MessageType.Info
            );

            _adicionar = GUILayout.Toggle(_adicionar, "Adicionar ponto por clique", "Button");
            EditorGUI.BeginChangeCheck();
            _largura = EditorGUILayout.Slider("Largura da rua (m)", _largura, 2f, 20f);
            _borda = EditorGUILayout.Slider("Acomodação lateral (m)", _borda, 2f, 30f);
            _suavizacao = EditorGUILayout.Slider("Suavização do relevo (m)", _suavizacao, 0f, 40f);
            if (EditorGUI.EndChangeCheck())
            {
                RecriarPrevia();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pontos do traçado", EditorStyles.boldLabel);
            for (var i = 0; i < _pontos.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(25f));
                    if (i == 0 || i == _pontos.Count - 1)
                    {
                        EditorGUILayout.Vector2Field(GUIContent.none, _pontos[i]);
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();
                        var valor = EditorGUILayout.Vector2Field(GUIContent.none, _pontos[i]);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _pontos[i] = LimitarAoMundo(valor);
                            RecriarPrevia();
                        }

                        if (GUILayout.Button("×", GUILayout.Width(24f)))
                        {
                            _pontos.RemoveAt(i);
                            RecriarPrevia();
                            break;
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enquadrar estrada"))
                {
                    Enquadrar();
                }

                if (GUILayout.Button("Reverter"))
                {
                    Reverter();
                }
            }

            if (GUILayout.Button("Salvar traçado compartilhado"))
            {
                Salvar();
            }

            if (!string.IsNullOrEmpty(_erro))
            {
                EditorGUILayout.HelpBox(_erro, MessageType.Error);
            }
        }

        private void DesenharNaCena(SceneView vista)
        {
            if (_pontos.Count < 2)
            {
                return;
            }

            var evento = Event.current;
            if (_adicionar && evento.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            if (
                _adicionar
                && evento.type == EventType.MouseDown
                && evento.button == 0
                && !evento.alt
                && _colisor != null
                && _colisor.Raycast(
                    HandleUtility.GUIPointToWorldRay(evento.mousePosition),
                    out var toque,
                    10000f
                )
            )
            {
                var ponto = new Vector2(toque.point.x, toque.point.z);
                _pontos.Insert(SegmentoMaisProximo(ponto) + 1, LimitarAoMundo(ponto));
                RecriarPrevia();
                evento.Use();
            }

            for (var i = 0; i < _pontos.Count; i++)
            {
                var ponto = _pontos[i];
                var posicao = new Vector3(ponto.x, AlturaDaPrevia(ponto.x, ponto.y) + 2f, ponto.y);
                Handles.color = i == 0 || i == _pontos.Count - 1 ? Color.cyan : Color.yellow;
                if (i == 0 || i == _pontos.Count - 1 || _adicionar)
                {
                    Handles.SphereHandleCap(0, posicao, Quaternion.identity, 6f, EventType.Repaint);
                    continue;
                }

                EditorGUI.BeginChangeCheck();
                var nova = Handles.PositionHandle(posicao, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    _pontos[i] = LimitarAoMundo(new Vector2(nova.x, nova.z));
                    RecriarPrevia();
                }
            }
        }

        private void Reverter()
        {
            _pontos.Clear();
            var salvo = EstradaDoPasso.Tracado;
            foreach (var ponto in salvo.Pontos)
            {
                _pontos.Add(new Vector2(ponto.X, ponto.Z));
            }

            _largura = salvo.Largura;
            _borda = salvo.Borda;
            _suavizacao = salvo.Suavizacao;
            _erro = null;
            RecriarPrevia();
        }

        private TracadoDoMundo TracadoAtual()
        {
            var pontos = new TracadoDoMundo.Ponto[_pontos.Count];
            for (var i = 0; i < _pontos.Count; i++)
            {
                pontos[i] = new TracadoDoMundo.Ponto(_pontos[i].x, _pontos[i].y);
            }

            return new TracadoDoMundo(pontos, _largura, _borda, _suavizacao);
        }

        private float AlturaDaPrevia(float x, float z)
        {
            return AlturaDaPrevia(x, z, TracadoAtual());
        }

        private static float AlturaDaPrevia(float x, float z, TracadoDoMundo tracado)
        {
            var baseY = MapaDoMundo.AlturaBaseDaAutoriaEm(x, z);
            return tracado.AplicarEstrada(x, z, baseY, MapaDoMundo.AlturaBaseDaAutoriaEm);
        }

        private void RecriarPrevia()
        {
            if (_pontos == null || _pontos.Count < 2)
            {
                return;
            }

            LimparPrevia();
            var tracado = TracadoAtual();
            _raiz = new GameObject("Prévia da estrada do Passo")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _materialTerreno = MaterialDaPrevia(new Color(0.30f, 0.39f, 0.25f));
            _materialEstrada = MontadorDaEstrada.CriarMaterial();
            _materialEstrada.hideFlags = HideFlags.HideAndDontSave;

            var minimo = new Vector2(float.MaxValue, float.MaxValue);
            var maximo = new Vector2(float.MinValue, float.MinValue);
            foreach (var ponto in _pontos)
            {
                minimo = Vector2.Min(minimo, ponto);
                maximo = Vector2.Max(maximo, ponto);
            }

            var inicioX = Mathf.Max(-MapaDoMundo.Metade, Mathf.Floor((minimo.x - 70f) / 10f) * 10f);
            var inicioZ = Mathf.Max(-MapaDoMundo.Metade, Mathf.Floor((minimo.y - 70f) / 10f) * 10f);
            var fimX = Mathf.Min(MapaDoMundo.Metade, Mathf.Ceil((maximo.x + 70f) / 10f) * 10f);
            var fimZ = Mathf.Min(MapaDoMundo.Metade, Mathf.Ceil((maximo.y + 70f) / 10f) * 10f);
            var colunas = Mathf.RoundToInt((fimX - inicioX) / 10f) + 1;
            var linhas = Mathf.RoundToInt((fimZ - inicioZ) / 10f) + 1;
            var vertices = new Vector3[colunas * linhas];
            var triangulos = new int[(colunas - 1) * (linhas - 1) * 6];
            for (var j = 0; j < linhas; j++)
            {
                for (var i = 0; i < colunas; i++)
                {
                    var x = inicioX + i * 10f;
                    var z = inicioZ + j * 10f;
                    var altura = MapaDoMundo.AlturaBaseDaAutoriaEm(x, z);
                    vertices[j * colunas + i] = new Vector3(
                        x,
                        tracado.AplicarEstrada(x, z, altura, MapaDoMundo.AlturaBaseDaAutoriaEm),
                        z
                    );
                }
            }

            var k = 0;
            for (var j = 0; j + 1 < linhas; j++)
            {
                for (var i = 0; i + 1 < colunas; i++)
                {
                    var a = j * colunas + i;
                    triangulos[k++] = a;
                    triangulos[k++] = a + colunas;
                    triangulos[k++] = a + 1;
                    triangulos[k++] = a + 1;
                    triangulos[k++] = a + colunas;
                    triangulos[k++] = a + colunas + 1;
                }
            }

            var malha = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            malha.vertices = vertices;
            malha.triangles = triangulos;
            malha.RecalculateNormals();
            var chao = CriarObjeto("Terreno", malha, _materialTerreno);
            _colisor = chao.AddComponent<MeshCollider>();
            _colisor.sharedMesh = malha;
            CriarObjeto(
                "Terra batida",
                MontadorDaEstrada.CriarMalha(tracado, (x, z) => AlturaDaPrevia(x, z, tracado)),
                _materialEstrada
            );
            SceneView.RepaintAll();
        }

        private GameObject CriarObjeto(string nome, Mesh malha, Material material)
        {
            var objeto = new GameObject(nome) { hideFlags = HideFlags.HideAndDontSave };
            objeto.transform.SetParent(_raiz.transform, false);
            objeto.AddComponent<MeshFilter>().sharedMesh = malha;
            objeto.AddComponent<MeshRenderer>().sharedMaterial = material;
            return objeto;
        }

        private static Material MaterialDaPrevia(Color cor)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            return new Material(shader) { color = cor, hideFlags = HideFlags.HideAndDontSave };
        }

        private void LimparPrevia()
        {
            if (_raiz != null)
            {
                foreach (var filtro in _raiz.GetComponentsInChildren<MeshFilter>())
                {
                    DestroyImmediate(filtro.sharedMesh);
                }

                DestroyImmediate(_raiz);
            }

            DestroyImmediate(_materialTerreno);
            DestroyImmediate(_materialEstrada);
            _raiz = null;
            _colisor = null;
        }

        private int SegmentoMaisProximo(Vector2 ponto)
        {
            var melhor = float.MaxValue;
            var indice = 0;
            for (var i = 0; i + 1 < _pontos.Count; i++)
            {
                var a = _pontos[i];
                var b = _pontos[i + 1];
                var t = Mathf.Clamp01(Vector2.Dot(ponto - a, b - a) / (b - a).sqrMagnitude);
                var distancia = (ponto - Vector2.Lerp(a, b, t)).sqrMagnitude;
                if (distancia < melhor)
                {
                    melhor = distancia;
                    indice = i;
                }
            }

            return indice;
        }

        private static Vector2 LimitarAoMundo(Vector2 ponto)
        {
            return new Vector2(
                Mathf.Clamp(ponto.x, -MapaDoMundo.Metade, MapaDoMundo.Metade),
                Mathf.Clamp(ponto.y, -MapaDoMundo.Metade, MapaDoMundo.Metade)
            );
        }

        private void Enquadrar()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                return;
            }

            var limites = new Bounds(new Vector3(_pontos[0].x, 0f, _pontos[0].y), Vector3.one);
            foreach (var ponto in _pontos)
            {
                limites.Encapsulate(new Vector3(ponto.x, 0f, ponto.y));
            }

            limites.Expand(120f);
            SceneView.lastActiveSceneView.Frame(limites, false);
        }

        private void Salvar()
        {
            if (_pontos.Count < 2)
            {
                _erro = "A estrada precisa de pelo menos dois pontos.";
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
                    "Mundo",
                    "EstradaDoPasso.cs"
                )
            );
            if (!File.Exists(caminho))
            {
                _erro = "Arquivo compartilhado da estrada não encontrado: " + caminho;
                return;
            }

            var texto = new StringBuilder();
            texto.AppendLine("namespace JogoDoGrupo.Shared.Features.Mundo");
            texto.AppendLine("{");
            texto.AppendLine(
                "    /// <summary>Traçado autoral salvo pelo editor para a ligação da cidade central ao Passo.</summary>"
            );
            texto.AppendLine("    public static class EstradaDoPasso");
            texto.AppendLine("    {");
            texto.AppendLine(
                "        public static readonly TracadoDoMundo Tracado = new TracadoDoMundo("
            );
            texto.AppendLine("            new[]");
            texto.AppendLine("            {");
            foreach (var ponto in _pontos)
            {
                texto.Append("                new TracadoDoMundo.Ponto(");
                texto.Append(Numero(ponto.x)).Append(", ").Append(Numero(ponto.y));
                texto.AppendLine("),");
            }

            texto.AppendLine("            },");
            texto.Append("            ").Append(Numero(_largura)).AppendLine(",");
            texto.Append("            ").Append(Numero(_borda)).AppendLine(",");
            texto.Append("            ").Append(Numero(_suavizacao)).AppendLine();
            texto.AppendLine("        );");
            texto.AppendLine("    }");
            texto.AppendLine("}");
            File.WriteAllText(caminho, texto.ToString(), new UTF8Encoding(false));
            _erro = null;
            CompilationPipeline.RequestScriptCompilation();
            Debug.Log("[FerramentaDeEstradas] Traçado salvo em " + caminho);
        }

        private static string Numero(float valor)
        {
            return valor.ToString("0.###", CultureInfo.InvariantCulture) + "f";
        }
    }
}
