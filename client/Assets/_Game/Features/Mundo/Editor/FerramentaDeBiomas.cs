using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Desenha regiões sobre o mapa completo sem gravar a prévia na cena.</summary>
    public sealed class FerramentaDeBiomas : EditorWindow
    {
        [Serializable]
        private sealed class Rascunho
        {
            public string Nome = "Nova região";
            public Bioma Bioma = Bioma.Mata;
            public List<Vector2> Pontos = new List<Vector2>();
            public float Borda = 30f;
            public float Relevo = 1f;
            public float Vegetacao = 1f;
            public float EnterroDaVegetacao = 0.15f;

            public Rascunho() { }

            public Rascunho(RegiaoDeBioma regiao)
            {
                Nome = regiao.Nome;
                Bioma = regiao.Bioma;
                Borda = regiao.LarguraDaBorda;
                Relevo = regiao.IntensidadeDoRelevo;
                Vegetacao = regiao.DensidadeDaVegetacao;
                EnterroDaVegetacao = regiao.EnterroDaVegetacao;
                foreach (var ponto in regiao.Pontos)
                {
                    Pontos.Add(new Vector2(ponto.X, ponto.Z));
                }
            }

            public RegiaoDeBioma Criar()
            {
                var pontos = new RegiaoDeBioma.Ponto[Pontos.Count];
                for (var i = 0; i < pontos.Length; i++)
                {
                    pontos[i] = new RegiaoDeBioma.Ponto(Pontos[i].x, Pontos[i].y);
                }

                return new RegiaoDeBioma(
                    Nome,
                    Bioma,
                    pontos,
                    Borda,
                    Relevo,
                    Vegetacao,
                    EnterroDaVegetacao
                );
            }
        }

        [SerializeField]
        private List<Rascunho> _regioes = new List<Rascunho>();

        [SerializeField]
        private int _selecionada;

        [SerializeField]
        private bool _adicionar;

        private GameObject _raiz;
        private RegiaoDeBioma[] _regioesMontadas = Array.Empty<RegiaoDeBioma>();
        private Vector2 _rolagem;
        private string _erro;
        private bool _montando;
        private double _atualizarDepoisDe;
        private long _ultimaMontagemMs;

        [MenuItem("Jogo do Grupo/Mundo/Autoria de biomas")]
        public static void Abrir()
        {
            GetWindow<FerramentaDeBiomas>("Biomas do mundo").Show();
        }

        /// <summary>Confere a prévia do mapa em execução batch no Unity.</summary>
        public static void VerificarPrevia()
        {
            var janela = GetWindow<FerramentaDeBiomas>();
            janela.AtualizarPrevia();
            if (janela._raiz == null)
            {
                throw new InvalidOperationException("A prévia do mapa não foi montada.");
            }

            janela.Close();
        }

        /// <summary>Confere uma edição da prévia sem alterar o mapa salvo.</summary>
        public static void VerificarAtualizacao()
        {
            var janela = GetWindow<FerramentaDeBiomas>();
            try
            {
                janela.AtualizarPrevia();
                if (janela._raiz == null || janela._regioes.Count == 0)
                {
                    throw new InvalidOperationException("A região de exemplo não foi montada.");
                }

                janela._regioes[0].Borda += 1f;
                janela.AtualizarPrevia();
                if (janela._raiz == null || janela._erro != null)
                {
                    throw new InvalidOperationException(
                        janela._erro ?? "Prévia removida após edição."
                    );
                }

                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "terreno-biomas-atualizacao-tempo.txt"),
                    janela._ultimaMontagemMs.ToString(CultureInfo.InvariantCulture)
                );
            }
            finally
            {
                janela.Close();
            }
        }

        /// <summary>Fotografa a região selecionada sobre o mapa inteiro para revisão visual.</summary>
        public static void CapturarPrevia()
        {
            var janela = GetWindow<FerramentaDeBiomas>();
            janela.AtualizarPrevia();
            if (janela._raiz == null)
            {
                throw new InvalidOperationException("A prévia do mapa não foi montada.");
            }

            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "terreno-biomas-previa-tempo.txt"),
                janela._ultimaMontagemMs.ToString(CultureInfo.InvariantCulture)
            );

            var objeto = new GameObject("Camera da prévia de biomas");
            var camera = objeto.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = MapaDoMundo.Metade;
            camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            objeto.transform.position = new Vector3(0f, 1000f, 0f);
            objeto.transform.LookAt(Vector3.zero);
            var nevoa = RenderSettings.fog;
            RenderSettings.fog = false;
            var render = RenderTexture.GetTemporary(1024, 1024, 24);
            var imagem = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                janela.DesenharVegetacao(camera);
                camera.Render();
                RenderTexture.active = render;
                imagem.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                imagem.Apply();
                var caminho = Path.Combine(Path.GetTempPath(), "terreno-biomas-previa-mapa.png");
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                Debug.Log("[FerramentaDeBiomas] prévia salva em " + caminho);

                camera.orthographicSize = 150f;
                objeto.transform.position = new Vector3(250f, 350f, -190f);
                objeto.transform.LookAt(new Vector3(250f, 0f, -190f));
                janela.DesenharVegetacao(camera);
                camera.Render();
                RenderTexture.active = render;
                imagem.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                imagem.Apply();
                caminho = Path.Combine(Path.GetTempPath(), "terreno-biomas-previa-copas.png");
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                Debug.Log("[FerramentaDeBiomas] copas da mata salvas em " + caminho);

                camera.orthographic = false;
                camera.fieldOfView = 55f;
                camera.farClipPlane = 1000f;
                camera.backgroundColor = new Color(0.48f, 0.63f, 0.74f);
                objeto.transform.position = new Vector3(
                    100f,
                    MapaDoMundo.AlturaEm(100f, -100f) + 28f,
                    -100f
                );
                objeto.transform.LookAt(
                    new Vector3(250f, MapaDoMundo.AlturaEm(250f, -190f) + 5f, -190f)
                );
                janela.DesenharVegetacao(camera);
                camera.Render();
                RenderTexture.active = render;
                imagem.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                imagem.Apply();
                caminho = Path.Combine(Path.GetTempPath(), "terreno-biomas-previa-perto.png");
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                Debug.Log("[FerramentaDeBiomas] vista próxima salva em " + caminho);
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(render);
                RenderSettings.fog = nevoa;
                DestroyImmediate(imagem);
                DestroyImmediate(objeto);
                janela.Close();
            }
        }

        private void OnEnable()
        {
            _regioes ??= new List<Rascunho>();
            if (_regioes.Count == 0)
            {
                Reverter();
            }

            SceneView.duringSceneGui += DesenharNaCena;
            Undo.undoRedoPerformed += AgendarPrevia;
            EditorApplication.playModeStateChanged += AoMudarModoDeJogo;
            AgendarPrevia();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DesenharNaCena;
            Undo.undoRedoPerformed -= AgendarPrevia;
            EditorApplication.playModeStateChanged -= AoMudarModoDeJogo;
            EditorApplication.update -= AtualizarQuandoPararDeEditar;
            MapaDoMundo.DefinirRegioesDePrevia(null);
            LimparPrevia();
            PecasDosBiomas.LimparCache();
        }

        private void AoMudarModoDeJogo(PlayModeStateChange estado)
        {
            if (estado == PlayModeStateChange.ExitingEditMode)
            {
                MapaDoMundo.DefinirRegioesDePrevia(null);
                LimparPrevia();
                PecasDosBiomas.LimparCache();
            }
            else if (estado == PlayModeStateChange.EnteredEditMode)
            {
                AgendarPrevia();
            }
        }

        private void OnGUI()
        {
            _rolagem = EditorGUILayout.BeginScrollView(_rolagem);
            EditorGUILayout.HelpBox(
                "Escolha um bioma, clique no chão do mapa 3D para formar o contorno e arraste "
                    + "os pontos para ajustar. A região mais recente cobre as anteriores. "
                    + "A prévia não muda a cena nem o mapa salvo.",
                MessageType.Info
            );
            if (_ultimaMontagemMs > 0)
            {
                EditorGUILayout.LabelField(
                    "Última atualização da prévia",
                    (_ultimaMontagemMs / 1000f).ToString("0.0", CultureInfo.CurrentCulture) + " s"
                );
            }

            if (_regioes.Count > 0)
            {
                var nomes = new string[_regioes.Count];
                for (var i = 0; i < nomes.Length; i++)
                {
                    nomes[i] = (i + 1) + ". " + _regioes[i].Nome;
                }

                _selecionada = EditorGUILayout.Popup(
                    "Região",
                    Mathf.Clamp(_selecionada, 0, nomes.Length - 1),
                    nomes
                );
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nova região"))
                {
                    Undo.RecordObject(this, "Criar região de bioma");
                    _regioes.Add(new Rascunho { Nome = "Região " + (_regioes.Count + 1) });
                    _selecionada = _regioes.Count - 1;
                    _adicionar = true;
                    _erro = null;
                    AgendarPrevia();
                }

                using (new EditorGUI.DisabledScope(_regioes.Count == 0))
                {
                    if (GUILayout.Button("Remover região"))
                    {
                        Undo.RecordObject(this, "Remover região de bioma");
                        _regioes.RemoveAt(_selecionada);
                        _selecionada = Mathf.Clamp(_selecionada, 0, _regioes.Count - 1);
                        _erro = null;
                        AgendarPrevia();
                    }
                }
            }

            if (_regioes.Count > 0)
            {
                MostrarRegiao(_regioes[_selecionada]);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enquadrar região"))
                {
                    Enquadrar();
                }

                if (GUILayout.Button("Reverter"))
                {
                    Reverter();
                }
            }

            if (GUILayout.Button("Salvar regiões no mapa compartilhado"))
            {
                Salvar();
            }

            if (!string.IsNullOrEmpty(_erro))
            {
                EditorGUILayout.HelpBox(_erro, MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }

        private void MostrarRegiao(Rascunho regiao)
        {
            EditorGUI.BeginChangeCheck();
            RegistrarUndoSeEntrada("Editar região de bioma");
            regiao.Nome = EditorGUILayout.TextField("Nome", regiao.Nome);
            var opcoes = new[] { "Campo", "Mata", "Colinas", "Deserto", "Neve", "Lava", "Pântano" };
            regiao.Bioma = (Bioma)(
                EditorGUILayout.Popup("Bioma", (int)regiao.Bioma - 1, opcoes) + 1
            );
            regiao.Borda = EditorGUILayout.Slider("Mistura da borda (m)", regiao.Borda, 0f, 100f);
            regiao.Relevo = EditorGUILayout.Slider("Intensidade do relevo", regiao.Relevo, 0f, 2f);
            regiao.Vegetacao = EditorGUILayout.Slider(
                "Densidade da vegetação",
                regiao.Vegetacao,
                0f,
                2f
            );
            regiao.EnterroDaVegetacao = EditorGUILayout.Slider(
                "Enterrar árvores (m)",
                regiao.EnterroDaVegetacao,
                0f,
                0.5f
            );
            if (EditorGUI.EndChangeCheck())
            {
                AgendarPrevia();
            }

            _adicionar = GUILayout.Toggle(_adicionar, "Adicionar ponto por clique", "Button");
            EditorGUILayout.LabelField("Pontos do contorno: " + regiao.Pontos.Count);
            for (var i = 0; i < regiao.Pontos.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    RegistrarUndoSeEntrada("Mover ponto do bioma");
                    var ponto = EditorGUILayout.Vector2Field("Ponto " + (i + 1), regiao.Pontos[i]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        regiao.Pontos[i] = Limitar(ponto);
                        AgendarPrevia();
                    }

                    if (GUILayout.Button("×", GUILayout.Width(25f)))
                    {
                        Undo.RecordObject(this, "Remover ponto do bioma");
                        regiao.Pontos.RemoveAt(i);
                        AgendarPrevia();
                        break;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Trazer para frente") && _selecionada + 1 < _regioes.Count)
                {
                    Undo.RecordObject(this, "Alterar ordem dos biomas");
                    _regioes.RemoveAt(_selecionada);
                    _regioes.Insert(++_selecionada, regiao);
                    AgendarPrevia();
                }

                if (GUILayout.Button("Enviar para trás") && _selecionada > 0)
                {
                    Undo.RecordObject(this, "Alterar ordem dos biomas");
                    _regioes.RemoveAt(_selecionada);
                    _regioes.Insert(--_selecionada, regiao);
                    AgendarPrevia();
                }
            }

            if (regiao.Pontos.Count < 3)
            {
                EditorGUILayout.HelpBox(
                    "Clique em pelo menos três pontos para fechar a região.",
                    MessageType.Warning
                );
            }
        }

        private void DesenharNaCena(SceneView vista)
        {
            if (Event.current.type == EventType.Repaint && _raiz != null)
            {
                DesenharVegetacao(vista.camera);
            }

            if (_regioes.Count == 0 || _raiz == null)
            {
                return;
            }

            var regiao = _regioes[_selecionada];
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
            )
            {
                var raio = HandleUtility.GUIPointToWorldRay(evento.mousePosition);
                if (TentarAtingirTerreno(raio, out var toque))
                {
                    Undo.RecordObject(this, "Adicionar ponto ao bioma");
                    regiao.Pontos.Add(Limitar(new Vector2(toque.x, toque.z)));
                    _erro = null;
                    AgendarPrevia();
                    evento.Use();
                }
            }

            var posicoes = new Vector3[regiao.Pontos.Count];
            for (var i = 0; i < regiao.Pontos.Count; i++)
            {
                var ponto = regiao.Pontos[i];
                posicoes[i] = new Vector3(
                    ponto.x,
                    MapaDoMundo.AlturaEm(ponto.x, ponto.y) + 2f,
                    ponto.y
                );
            }

            Handles.color = Color.cyan;
            for (var i = 0; i + 1 < posicoes.Length; i++)
            {
                Handles.DrawAAPolyLine(4f, posicoes[i], posicoes[i + 1]);
            }

            if (posicoes.Length >= 3)
            {
                Handles.DrawAAPolyLine(4f, posicoes[posicoes.Length - 1], posicoes[0]);
            }

            for (var i = 0; i < posicoes.Length; i++)
            {
                Handles.color = Color.yellow;
                if (_adicionar)
                {
                    Handles.SphereHandleCap(
                        0,
                        posicoes[i],
                        Quaternion.identity,
                        8f,
                        EventType.Repaint
                    );
                    continue;
                }

                EditorGUI.BeginChangeCheck();
                var nova = Handles.PositionHandle(posicoes[i], Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Mover ponto do bioma");
                    regiao.Pontos[i] = Limitar(new Vector2(nova.x, nova.z));
                    AgendarPrevia();
                }
            }
        }

        private void DesenharVegetacao(Camera camera)
        {
            if (_raiz == null || camera == null)
            {
                return;
            }

            foreach (var blocos in _raiz.GetComponentsInChildren<BlocosDeVegetacao>(true))
            {
                blocos.Desenhar(camera.transform.position, camera);
            }
        }

        private bool TentarAtingirTerreno(Ray raio, out Vector3 ponto)
        {
            var menor = float.MaxValue;
            ponto = default;
            foreach (var colisor in _raiz.GetComponentsInChildren<MeshCollider>())
            {
                if (!colisor.name.StartsWith("Bloco_", StringComparison.Ordinal))
                {
                    continue;
                }

                if (colisor.Raycast(raio, out var toque, menor))
                {
                    menor = toque.distance;
                    ponto = toque.point;
                }
            }

            return menor < float.MaxValue;
        }

        private void AgendarPrevia()
        {
            _atualizarDepoisDe = EditorApplication.timeSinceStartup + 0.25d;
            EditorApplication.update -= AtualizarQuandoPararDeEditar;
            EditorApplication.update += AtualizarQuandoPararDeEditar;
            Repaint();
        }

        private void AtualizarQuandoPararDeEditar()
        {
            if (EditorApplication.timeSinceStartup < _atualizarDepoisDe)
            {
                return;
            }

            EditorApplication.update -= AtualizarQuandoPararDeEditar;
            AtualizarPrevia();
        }

        private void AtualizarPrevia()
        {
            EditorApplication.update -= AtualizarQuandoPararDeEditar;
            if (_montando || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            _montando = true;
            try
            {
                var regioes = new List<RegiaoDeBioma>();
                foreach (var rascunho in _regioes)
                {
                    if (rascunho.Pontos.Count >= 3)
                    {
                        regioes.Add(rascunho.Criar());
                    }
                }

                // A lista original de peças precisa nascer antes da primeira região temporária.
                if (_raiz == null)
                {
                    _ = LayoutDoHub.PropsFora;
                }

                var novasRegioes = regioes.ToArray();
                var areaAlterada = AreaDasRegioes(_regioesMontadas, novasRegioes);
                var atualizarLiquidos = _raiz != null && AreaContemLiquido(areaAlterada);
                MapaDoMundo.DefinirRegioesDePrevia(novasRegioes);
                atualizarLiquidos |= _raiz != null && AreaContemLiquido(areaAlterada);
                PecasDosBiomas.LimparCache();
                var relogio = System.Diagnostics.Stopwatch.StartNew();
                if (_raiz != null)
                {
                    ConstrutorDoTerreno.AtualizarPrevia(
                        _raiz.transform.Find(ConstrutorHub.NomeTerreno),
                        areaAlterada,
                        atualizarLiquidos
                    );
                    ConstrutorHub.AtualizarVegetacaoDaPrevia(_raiz.transform);
                    _regioesMontadas = novasRegioes;
                    relogio.Stop();
                    _ultimaMontagemMs = relogio.ElapsedMilliseconds;
                    _erro = null;
                    SceneView.RepaintAll();
                    return;
                }

                var modo = RenderSettings.ambientMode;
                var ceu = RenderSettings.ambientSkyColor;
                var horizonte = RenderSettings.ambientEquatorColor;
                var solo = RenderSettings.ambientGroundColor;
                var nevoa = RenderSettings.fog;
                var corDaNevoa = RenderSettings.fogColor;
                var densidadeDaNevoa = RenderSettings.fogDensity;
                try
                {
                    _raiz = ConstrutorHub.Montar();
                    _regioesMontadas = novasRegioes;
                    relogio.Stop();
                    _ultimaMontagemMs = relogio.ElapsedMilliseconds;
                    _raiz.name = "Prévia dos biomas — não salvar";
                    _raiz.hideFlags = HideFlags.HideAndDontSave;
                }
                finally
                {
                    RenderSettings.ambientMode = modo;
                    RenderSettings.ambientSkyColor = ceu;
                    RenderSettings.ambientEquatorColor = horizonte;
                    RenderSettings.ambientGroundColor = solo;
                    RenderSettings.fog = nevoa;
                    RenderSettings.fogColor = corDaNevoa;
                    RenderSettings.fogDensity = densidadeDaNevoa;
                }

                _erro = null;
                SceneView.RepaintAll();
            }
            catch (Exception erro)
            {
                _erro = "Não foi possível montar a prévia: " + erro.Message;
                Debug.LogException(erro);
                MapaDoMundo.DefinirRegioesDePrevia(null);
                LimparPrevia();
                _regioesMontadas = Array.Empty<RegiaoDeBioma>();
            }
            finally
            {
                _montando = false;
                Repaint();
            }
        }

        private static Rect AreaDasRegioes(RegiaoDeBioma[] anteriores, RegiaoDeBioma[] novas)
        {
            var minimoX = float.MaxValue;
            var maximoX = float.MinValue;
            var minimoZ = float.MaxValue;
            var maximoZ = float.MinValue;
            foreach (var conjunto in new[] { anteriores, novas })
            {
                foreach (var regiao in conjunto)
                {
                    var margem = regiao.LarguraDaBorda / 2f + MapaDoMundo.PassoDaGrade * 2f;
                    foreach (var ponto in regiao.Pontos)
                    {
                        minimoX = Mathf.Min(minimoX, ponto.X - margem);
                        maximoX = Mathf.Max(maximoX, ponto.X + margem);
                        minimoZ = Mathf.Min(minimoZ, ponto.Z - margem);
                        maximoZ = Mathf.Max(maximoZ, ponto.Z + margem);
                    }
                }
            }

            return minimoX == float.MaxValue
                ? new Rect(0f, 0f, 0f, 0f)
                : Rect.MinMaxRect(minimoX, minimoZ, maximoX, maximoZ);
        }

        private static bool AreaContemLiquido(Rect area)
        {
            var passo = MapaDoMundo.PassoDaGrade;
            var minimoX = Mathf.Max(
                -MapaDoMundo.Metade,
                Mathf.Floor(area.xMin / passo) * passo - passo
            );
            var maximoX = Mathf.Min(
                MapaDoMundo.Metade,
                Mathf.Ceil(area.xMax / passo) * passo + passo
            );
            var minimoZ = Mathf.Max(
                -MapaDoMundo.Metade,
                Mathf.Floor(area.yMin / passo) * passo - passo
            );
            var maximoZ = Mathf.Min(
                MapaDoMundo.Metade,
                Mathf.Ceil(area.yMax / passo) * passo + passo
            );
            for (var z = minimoZ; z <= maximoZ; z += passo)
            {
                for (var x = minimoX; x <= maximoX; x += passo)
                {
                    if (
                        MapaDoMundo.AlturaEm(x, z)
                        < Mathf.Max(MapaDoMundo.NivelDaAgua, MapaDoMundo.NivelDaLava)
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void LimparPrevia()
        {
            if (_raiz == null)
            {
                return;
            }

            var temporarios = new HashSet<Object>();
            foreach (var filtro in _raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                temporarios.Add(filtro.sharedMesh);
            }

            foreach (var renderer in _raiz.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    temporarios.Add(material);
                    foreach (var propriedade in material.GetTexturePropertyNames())
                    {
                        temporarios.Add(material.GetTexture(propriedade));
                    }
                }
            }

            DestroyImmediate(_raiz);
            _raiz = null;
            MontadorDaVegetacao.Limpar();
            foreach (var recurso in temporarios)
            {
                if (recurso != null && !EditorUtility.IsPersistent(recurso))
                {
                    DestroyImmediate(recurso);
                }
            }
        }

        private void Reverter()
        {
            Undo.RecordObject(this, "Reverter biomas");
            _regioes.Clear();
            foreach (var regiao in RegioesDoMundo.Todas)
            {
                _regioes.Add(new Rascunho(regiao));
            }

            if (_regioes.Count == 0)
            {
                _regioes.Add(
                    new Rascunho
                    {
                        Nome = "Mata de exemplo",
                        Bioma = Bioma.Mata,
                        Pontos = new List<Vector2>
                        {
                            new Vector2(170f, -130f),
                            new Vector2(310f, -100f),
                            new Vector2(330f, -250f),
                            new Vector2(160f, -270f),
                        },
                        Borda = 70f,
                        Relevo = 1.1f,
                        Vegetacao = 2f,
                        EnterroDaVegetacao = 0.15f,
                    }
                );
            }

            _selecionada = 0;
            _erro = null;
            AgendarPrevia();
        }

        private void Enquadrar()
        {
            if (SceneView.lastActiveSceneView == null || _regioes.Count == 0)
            {
                return;
            }

            var pontos = _regioes[_selecionada].Pontos;
            if (pontos.Count == 0)
            {
                SceneView.lastActiveSceneView.Frame(
                    new Bounds(Vector3.zero, new Vector3(200f, 100f, 200f)),
                    false
                );
                return;
            }

            var limite = new Bounds(new Vector3(pontos[0].x, 0f, pontos[0].y), Vector3.one);
            foreach (var ponto in pontos)
            {
                limite.Encapsulate(new Vector3(ponto.x, 0f, ponto.y));
            }

            limite.Expand(100f);
            SceneView.lastActiveSceneView.Frame(limite, false);
        }

        private void Salvar()
        {
            try
            {
                var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var regiao in _regioes)
                {
                    if (!nomes.Add(regiao.Nome.Trim()))
                    {
                        throw new InvalidOperationException(
                            "Use nomes diferentes para as regiões."
                        );
                    }

                    regiao.Criar();
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
                        "RegioesDoMundo.cs"
                    )
                );
                if (!File.Exists(caminho))
                {
                    throw new FileNotFoundException(
                        "Arquivo compartilhado das regiões não encontrado.",
                        caminho
                    );
                }

                var texto = new StringBuilder();
                texto.AppendLine("namespace JogoDoGrupo.Shared.Features.Mundo");
                texto.AppendLine("{");
                texto.AppendLine(
                    "    /// <summary>Regiões salvas pelo criador de mapas. A ordem define a sobreposição.</summary>"
                );
                texto.AppendLine("    public static class RegioesDoMundo");
                texto.AppendLine("    {");
                texto.AppendLine("        public static readonly RegiaoDeBioma[] Todas =");
                texto.AppendLine("        {");
                foreach (var regiao in _regioes)
                {
                    texto.Append("            new RegiaoDeBioma(\"");
                    texto.Append(
                        regiao
                            .Nome.Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\r", "\\r")
                            .Replace("\n", "\\n")
                    );
                    texto.Append("\", Bioma.").Append(regiao.Bioma).AppendLine(",");
                    texto.AppendLine("                new[]");
                    texto.AppendLine("                {");
                    foreach (var ponto in regiao.Pontos)
                    {
                        texto.Append("                    new RegiaoDeBioma.Ponto(");
                        texto.Append(Numero(ponto.x)).Append(", ").Append(Numero(ponto.y));
                        texto.AppendLine("),");
                    }

                    texto.AppendLine("                },");
                    texto.Append("                ").Append(Numero(regiao.Borda)).AppendLine(",");
                    texto.Append("                ").Append(Numero(regiao.Relevo)).AppendLine(",");
                    texto
                        .Append("                ")
                        .Append(Numero(regiao.Vegetacao))
                        .AppendLine(",");
                    texto
                        .Append("                ")
                        .Append(Numero(regiao.EnterroDaVegetacao))
                        .AppendLine("),");
                }

                texto.AppendLine("        };");
                texto.AppendLine("    }");
                texto.AppendLine("}");
                File.WriteAllText(caminho, texto.ToString(), new UTF8Encoding(false));
                _erro = null;
                CompilationPipeline.RequestScriptCompilation();
                Debug.Log("[FerramentaDeBiomas] Regiões salvas em " + caminho);
            }
            catch (Exception erro)
            {
                _erro = erro.Message;
            }
        }

        private static Vector2 Limitar(Vector2 ponto)
        {
            return new Vector2(
                Mathf.Clamp(ponto.x, -MapaDoMundo.Metade, MapaDoMundo.Metade),
                Mathf.Clamp(ponto.y, -MapaDoMundo.Metade, MapaDoMundo.Metade)
            );
        }

        private static string Numero(float valor)
        {
            return valor.ToString("0.###", CultureInfo.InvariantCulture) + "f";
        }

        private void RegistrarUndoSeEntrada(string descricao)
        {
            if (
                Event.current.type == EventType.MouseDown
                || Event.current.type == EventType.KeyDown
            )
            {
                Undo.RecordObject(this, descricao);
            }
        }
    }
}
