using JogoDoGrupo.Client.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Entrada explicita no sandbox, sem mudar bootstrap ou cenas de build.</summary>
    [InitializeOnLoad]
    public static class AcessoCharacterPlayground
    {
        const string ChaveCena = "CharacterPlayground.CenaInicialAnterior";
        const string ChaveRestaurar = "CharacterPlayground.RestaurarCenaInicial";
        const string ChaveRetorno = "CharacterPlayground.CenaDeRetorno";
        const string ChaveSair = "CharacterPlayground.VoltarAposStop";
        const string TelaLocal = "personagem.playground.editor";
        static GameObject _interface;
        static string _telaAnterior;
        static bool _isolado;

        static AcessoCharacterPlayground()
        {
            EditorApplication.playModeStateChanged += AoMudarModo;
            EditorApplication.update += Atualizar;
            EditorApplication.delayCall += RecuperarPreferencia;
            EditorApplication.quitting += RecuperarPreferencia;
        }

        public static bool Contexto =>
            SceneManager.GetActiveScene().path == CharacterPlayground.Cena;

        // Recupera tambem o estado legado: nunca conservar o Playground como cena inicial.
        static void RecuperarPreferencia()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var atual = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            if (SessionState.GetBool(ChaveRestaurar, false))
            {
                var path = SessionState.GetString(ChaveCena, "");
                EditorSceneManager.playModeStartScene =
                    path == CharacterPlayground.Cena
                        ? null
                        : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }
            else if (atual == CharacterPlayground.Cena)
                EditorSceneManager.playModeStartScene = null;
            SessionState.EraseBool(ChaveRestaurar);
            SessionState.EraseString(ChaveCena);
        }

        [MenuItem("Jogo do Grupo/Personagem/Character Playground/Abrir Playground", false, 0)]
        public static void Abrir()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (SceneManager.GetActiveScene().path != CharacterPlayground.Cena)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                SessionState.SetString(ChaveRetorno, SceneManager.GetActiveScene().path);
                EditorSceneManager.OpenScene(CharacterPlayground.Cena);
            }
            JanelaCharacterPlayground.Abrir();
        }

        [MenuItem("Jogo do Grupo/Personagem/Character Playground/Voltar ao Jogo", false, 1)]
        public static void VoltarAoJogo()
        {
            if (!Contexto)
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(ChaveSair, true);
                EditorApplication.isPlaying = false;
                return;
            }
            RecuperarPreferencia();
            if (!Contexto)
                return;
            var destino = SessionState.GetString(ChaveRetorno, "");
            if (!CenaNormalValida(destino))
            {
                destino = null;
                foreach (var cena in EditorBuildSettings.scenes)
                    if (cena.enabled && CenaNormalValida(cena.path))
                    {
                        destino = cena.path;
                        break;
                    }
            }
            if (!CenaNormalValida(destino))
                throw new System.InvalidOperationException(
                    "Nenhuma cena normal disponível para retorno."
                );
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(destino);
            SessionState.EraseString(ChaveRetorno);
            SessionState.EraseInt("CharacterPlayground.Indice");
        }

        static bool CenaNormalValida(string path) =>
            !string.IsNullOrEmpty(path)
            && path != CharacterPlayground.Cena
            && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;

        static void AoMudarModo(PlayModeStateChange estado)
        {
            if (
                estado == PlayModeStateChange.ExitingEditMode
                && SceneManager.GetActiveScene().path == CharacterPlayground.Cena
            )
            {
                var anterior = EditorSceneManager.playModeStartScene;
                // Abrir a cena basta. Suspender somente uma preferencia externa existente,
                // sem nunca apontar a preferencia global para o Playground.
                if (anterior != null)
                {
                    SessionState.SetString(ChaveCena, AssetDatabase.GetAssetPath(anterior));
                    SessionState.SetBool(ChaveRestaurar, true);
                    EditorSceneManager.playModeStartScene = null;
                }
            }
            if (estado == PlayModeStateChange.EnteredEditMode)
            {
                RecuperarPreferencia();
                _isolado = false;
                _interface = null;
                _telaAnterior = null;
                if (SessionState.GetBool(ChaveSair, false))
                {
                    SessionState.EraseBool(ChaveSair);
                    EditorApplication.delayCall += VoltarAoJogo;
                }
            }
        }

        static void Atualizar()
        {
            if (!RuntimeCharacterPlayground.Ativo)
            {
                if (_isolado && Application.isPlaying)
                {
                    if (_interface != null)
                        _interface.SetActive(true);
                    if (!string.IsNullOrEmpty(_telaAnterior))
                        Navegacao.Abrir(_telaAnterior);
                    _isolado = false;
                }
                return;
            }
            if (_isolado || Navegacao.TelaAtual == null)
                return;
            // O bootstrap global cria a abertura em qualquer cena. Trocar pela tela
            // vazia fecha a anterior (Aberta=false), impedindo sua continuacao async.
            // Nao alterar sessao gravada, bootstrap, Core ou fluxo de outras cenas.
            _telaAnterior = Navegacao.ChaveAtual;
            if (!Navegacao.EstaRegistrada(TelaLocal))
                Navegacao.Registrar(TelaLocal, () => new TelaPlayground());
            Navegacao.Abrir(TelaLocal);
            _interface = GameObject.Find("JogoDoGrupo.Interface");
            if (_interface != null)
                _interface.SetActive(false);
            _isolado = true;
        }

        sealed class TelaPlayground : Tela
        {
            protected override void Montar(VisualElement raiz) { }
        }
    }
}
