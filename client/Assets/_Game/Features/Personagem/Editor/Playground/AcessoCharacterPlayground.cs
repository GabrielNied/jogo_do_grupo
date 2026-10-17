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
        const string TelaLocal = "personagem.playground.editor";
        static GameObject _interface;
        static string _telaAnterior;
        static bool _isolado;

        static AcessoCharacterPlayground()
        {
            EditorApplication.playModeStateChanged += AoMudarModo;
            EditorApplication.update += Atualizar;
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
                EditorSceneManager.OpenScene(CharacterPlayground.Cena);
            }
            JanelaCharacterPlayground.Abrir();
        }

        static void AoMudarModo(PlayModeStateChange estado)
        {
            if (
                estado == PlayModeStateChange.ExitingEditMode
                && SceneManager.GetActiveScene().path == CharacterPlayground.Cena
            )
            {
                var anterior = EditorSceneManager.playModeStartScene;
                SessionState.SetString(
                    ChaveCena,
                    anterior == null ? "" : AssetDatabase.GetAssetPath(anterior)
                );
                SessionState.SetBool(ChaveRestaurar, true);
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    CharacterPlayground.Cena
                );
            }
            if (estado == PlayModeStateChange.EnteredEditMode)
            {
                if (SessionState.GetBool(ChaveRestaurar, false))
                {
                    var path = SessionState.GetString(ChaveCena, "");
                    EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path)
                        ? null
                        : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    SessionState.SetBool(ChaveRestaurar, false);
                }
                _isolado = false;
                _interface = null;
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
