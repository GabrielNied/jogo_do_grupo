using System;
using System.Linq;
using JogoDoGrupo.Client.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Verificacoes de regressao chamadas via MCP entre transicoes reais do Editor.</summary>
    public static class ValidacaoFluxosPlayground
    {
        public static string Verificar(bool playground, bool play)
        {
            void Exigir(bool ok, string motivo)
            {
                if (!ok)
                    throw new InvalidOperationException(motivo);
            }
            Exigir(Application.isPlaying == play, "Modo do Editor inesperado.");
            Exigir(AcessoCharacterPlayground.Contexto == playground, "Cena/contexto inesperado.");
            Exigir(
                RuntimeCharacterPlayground.Ativo == (playground && play),
                "Contexto runtime indevido."
            );
            Exigir(EditorSceneManager.playModeStartScene == null, "Override residual de Play.");
            Exigir(
                !EditorBuildSettings.scenes.Any(s => s.path == CharacterPlayground.Cena),
                "Playground nos Build Settings."
            );
            Exigir(
                !InputSystem.devices.Any(d => d.name.StartsWith("Playground") || d.name == "Probe"),
                "Dispositivo de teste residual."
            );
            if (!playground)
            {
                Exigir(
                    CharacterPlayground.Atual == null
                        && RuntimeCharacterPlayground.Controle == null,
                    "Personagem/controlador do Playground fora de contexto."
                );
                Exigir(GameObject.Find("CameraPlaygroundGameplay") == null, "Camera residual.");
                if (play)
                {
                    Exigir(
                        GameObject.Find("JogoDoGrupo.Interface")?.activeInHierarchy == true,
                        "Bootstrap/interface normal indisponivel."
                    );
                    Exigir(
                        Navegacao.TelaAtual != null
                            && Navegacao.ChaveAtual != "personagem.playground.editor",
                        "Navegacao normal sequestrada."
                    );
                    Exigir(
                        InputSystem.devices.Any(d => d is Keyboard && d.enabled),
                        "Teclado normal desativado."
                    );
                }
            }
            else
            {
                Exigir(
                    CharacterPlayground.Lista.Personagens.Count > 0
                        && CharacterPlayground.Atual != null,
                    "Catalogo/personagem ausente."
                );
                Exigir(
                    CharacterPlayground.Raiz("PersonagemAtual").childCount == 1,
                    "Personagem duplicado."
                );
                var animator = CharacterPlayground.Atual.GetComponentInChildren<Animator>();
                Exigir(animator.avatar.isValid && animator.isHuman, "Avatar invalido.");
                Exigir(
                    CharacterPlayground.Sockets.All(s =>
                        CharacterPlayground.Socket(CharacterPlayground.Atual, s) != null
                    ),
                    "Socket ausente."
                );
                if (play)
                {
                    Exigir(
                        RuntimeCharacterPlayground.Controle != null
                            && RuntimeCharacterPlayground.Camera != null
                            && RuntimeCharacterPlayground.Erro == null,
                        "Contexto runtime incompleto."
                    );
                    Exigir(
                        animator.runtimeAnimatorController != null
                            && animator.avatar.name == "HumanMaleLocomotionAvatar",
                        "Calibracao aprovada nao aplicada."
                    );
                    Exigir(
                        Navegacao.ChaveAtual == "personagem.playground.editor",
                        "Isolamento da interface local ausente."
                    );
                    Exigir(
                        UnityEngine
                            .Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                            .Count(t => t.name == "CameraPlaygroundGameplay") == 1,
                        "Camera duplicada."
                    );
                }
                else
                    Exigir(
                        animator.runtimeAnimatorController == null
                            && CharacterPlayground.Atual.GetComponent<CharacterController>()
                                == null,
                        "Runtime persistido na cena."
                    );
            }
            return $"PASSOU: {(playground ? "Playground" : "Jogo normal")} / {(play ? "Play" : "Edit")} / {SceneManager.GetActiveScene().path}";
        }
    }
}
