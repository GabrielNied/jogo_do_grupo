using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Mantém o chat vivo enquanto o jogo está aberto: assina as mensagens do servidor desde o
    /// começo (o histórico da Cidade chega ao entrar no mundo) e, quando o jogador entra no mundo,
    /// monta o painel na região inferior esquerda do HUD. O histórico de 100 linhas e a lista de
    /// ignorados sobrevivem a sair e voltar ao mundo. Mesmo padrão do observador de Combate.
    /// </summary>
    internal sealed class ObservadorDoChat : MonoBehaviour
    {
        private const string NomeObservador = "Social.ChatObservador";
        private const string NomeRaiz = "Social.Chat";

        private static ObservadorDoChat _instancia;

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private ServicoDeChat _servico;
        private GameObject _jogadorAtendido;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDoChat>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Awake()
        {
            _servico = new ServicoDeChat(
                mensagem =>
                    ConexaoTempoReal.Enviar(SocialMessageIds.EnviarMensagemDeChat, mensagem),
                () => EntradaNoJogo.PersonagemId,
                () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new ListaDeIgnorados(new ArmazenamentoPlayerPrefs())
            );

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<MensagemDeChat>(
                    SocialMessageIds.MensagemDeChat,
                    _servico.AoReceberMensagem
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<MensagemDeChatRecusada>(
                    SocialMessageIds.MensagemDeChatRecusada,
                    _servico.AoReceberRecusa
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<HistoricoDaCidade>(
                    SocialMessageIds.HistoricoDaCidade,
                    _servico.AoReceberHistorico
                )
            );
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null || jogador == _jogadorAtendido)
            {
                return;
            }

            _jogadorAtendido = jogador;
            try
            {
                Montar(jogador);
            }
            catch (Exception e)
            {
                // O chat com defeito não pode derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private void Montar(GameObject jogador)
        {
            var regiao = Hud.Regiao(RegiaoDoHud.InferiorEsquerda);
            if (regiao == null)
            {
                Debug.LogWarning("[Social] O HUD não está montado; o chat não foi criado.");
                return;
            }

            var raiz = new GameObject(NomeRaiz);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);
            var controle = raiz.AddComponent<ControleDeChat>();
            controle.Configurar(_servico, new PainelDeChat(regiao));
        }
    }
}
