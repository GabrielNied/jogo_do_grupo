using System;
using System.Linq;
using System.Threading.Tasks;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
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
        // Chamada explicita do teste, nunca um callback global do jogo normal.
        public static async Task EncerrarTesteNormal()
        {
            if (!Application.isPlaying || AcessoCharacterPlayground.Contexto)
                throw new InvalidOperationException(
                    "Encerre somente o teste do jogo normal em Play."
                );
            if (EntradaNoJogo.NoJogo)
                EntradaNoJogo.Sair();
            const string chave = "personagem.playground.fim-validacao";
            Navegacao.Registrar(chave, () => new TelaFimValidacao());
            // Fechar pela navegacao executa Dispose da previa antes de Stop. Objetos
            // HideAndDontSave podem sobreviver quando se interrompe a tela diretamente.
            Navegacao.Abrir(chave);
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            EditorApplication.isPlaying = false;
        }

        sealed class TelaFimValidacao : Tela
        {
            protected override void Montar(UnityEngine.UIElements.VisualElement raiz) { }
        }

        static int ContarPrevias() =>
            Resources
                .FindObjectsOfTypeAll<GameObject>()
                .Count(g => g.name == "Prévia de personagem" && !EditorUtility.IsPersistent(g));

        public static string VerificarMundo()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (
                !Application.isPlaying
                || AcessoCharacterPlayground.Contexto
                || !EntradaNoJogo.NoJogo
                || EntradaNoJogo.EntradaPendente
                || !ConexaoTempoReal.Conectado
                || jogador == null
                || Navegacao.TelaAtual != null
            )
                throw new InvalidOperationException("Entrada autenticada no mundo nao concluida.");
            var visual = jogador.GetComponentInChildren<SkinnedMeshRenderer>();
            if (visual == null || !visual.enabled || Camera.main == null || !Camera.main.enabled)
                throw new InvalidOperationException("Visual/camera original ausente.");
            if (ContarPrevias() != 0)
                throw new InvalidOperationException(
                    "Previas residuais podem interferir na luz do mundo."
                );
            return $"PASSOU: mundo autenticado, primeira posicao aplicada e visual {visual.name}. Conferir captura e input separadamente.";
        }

        public static string Verificar(bool playground, bool play)
        {
            void Exigir(bool ok, string motivo)
            {
                if (!ok)
                    throw new InvalidOperationException(motivo);
            }
            Exigir(Application.isPlaying == play, "Modo do Editor inesperado.");
            // FindObjectsByType ignora a raiz HideAndDontSave, mas suas luzes ainda
            // afetam a renderizacao. Inspecionar tambem objetos fora de cenas validas.
            var telaComPrevia =
                play
                && !playground
                && (
                    Navegacao.TelaAtual?.GetType().Name == "TelaSelecaoPersonagem"
                    || Navegacao.TelaAtual?.GetType().Name == "TelaCriacaoPersonagem"
                );
            Exigir(
                ContarPrevias() <= (telaComPrevia ? 1 : 0),
                "Previa temporaria residual entre testes."
            );
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
