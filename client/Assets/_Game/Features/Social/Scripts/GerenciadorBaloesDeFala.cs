using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Gerencia o ciclo de vida dos balões de fala (Speech Bubbles) exibidos no mundo 3D
    /// sobre a cabeça dos avatares ao receber mensagens nos canais públicos (Cidade e Grupo).
    /// </summary>
    public sealed class GerenciadorBaloesDeFala : MonoBehaviour
    {
        public const string NomeObjeto = "Social.BaloesDeFala";

        private readonly Dictionary<long, BalaoDeFalaVisual> _baloes =
            new Dictionary<long, BalaoDeFalaVisual>();

        private readonly List<long> _expirados = new List<long>();

        private VisualElement _camada;
        private Camera _camera;
        private ServicoDeChat _servico;

        public IReadOnlyDictionary<long, BalaoDeFalaVisual> BaloesAtivos => _baloes;

        /// <summary>
        /// Configura a camada de interface e câmera para projeção dos balões.
        /// </summary>
        public void Configurar(VisualElement camada, Camera camera = null)
        {
            _camada = camada;
            _camera = camera;
        }

        /// <summary>
        /// Conecta o gerenciador aos eventos de mensagens do serviço de chat.
        /// </summary>
        internal void ConectarServico(ServicoDeChat servico)
        {
            if (_servico != null)
            {
                _servico.MensagemRecebida -= AoReceberMensagem;
            }

            _servico = servico;
            if (_servico != null)
            {
                _servico.MensagemRecebida += AoReceberMensagem;
            }
        }

        private void OnDestroy()
        {
            if (_servico != null)
            {
                _servico.MensagemRecebida -= AoReceberMensagem;
                _servico = null;
            }

            DescartarTudo();
        }

        private void LateUpdate()
        {
            Atualizar(Time.deltaTime);
        }

        /// <summary>
        /// Atualiza todos os balões de fala ativos, descartando os que expiraram.
        /// </summary>
        public void Atualizar(float deltaTime)
        {
            if (_camada == null)
            {
                return;
            }

            var cam = _camera != null ? _camera : Camera.main;
            _expirados.Clear();

            foreach (var par in _baloes)
            {
                var balao = par.Value;

                // Tenta resolver o avatar caso ainda não tenha transform ou tenha sido recriado
                if (balao.TransformAlvo == null)
                {
                    balao.TransformAlvo = ResolverAvatar(balao.PersonagemId);
                }

                if (balao.Atualizar(deltaTime, cam, _camada))
                {
                    _expirados.Add(par.Key);
                }
            }

            foreach (var id in _expirados)
            {
                if (_baloes.TryGetValue(id, out var balao))
                {
                    balao.Descartar();
                    _baloes.Remove(id);
                }
            }
        }

        private void AoReceberMensagem(MensagemDeChat mensagem)
        {
            ProcessarMensagem(mensagem);
        }

        /// <summary>
        /// Processa uma mensagem de chat. Cria um novo balão ou atualiza o existente para o emissor.
        /// </summary>
        public void ProcessarMensagem(
            MensagemDeChat mensagem,
            Func<long, Transform> localizadorAvatar = null
        )
        {
            if (mensagem == null || !RegrasBaloesDeFala.CanalPermiteBalao(mensagem.Canal))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(mensagem.Texto))
            {
                return;
            }

            var transformAlvo =
                localizadorAvatar != null
                    ? localizadorAvatar(mensagem.RemetenteId)
                    : ResolverAvatar(mensagem.RemetenteId);

            if (_baloes.TryGetValue(mensagem.RemetenteId, out var existente))
            {
                existente.DefinirMensagem(mensagem.NomeDoRemetente, mensagem.Texto, mensagem.Canal);
                if (transformAlvo != null)
                {
                    existente.TransformAlvo = transformAlvo;
                }
            }
            else
            {
                if (_camada == null)
                {
                    return;
                }

                var novo = new BalaoDeFalaVisual(
                    mensagem.RemetenteId,
                    mensagem.NomeDoRemetente,
                    mensagem.Texto,
                    mensagem.Canal,
                    transformAlvo,
                    _camada
                );
                _baloes[mensagem.RemetenteId] = novo;
            }
        }

        /// <summary>
        /// Localiza o Transform do avatar do jogador no mundo (jogador local ou remoto).
        /// </summary>
        public static Transform ResolverAvatar(long personagemId)
        {
            if (
                EntradaNoJogo.NoJogo
                && EntradaNoJogo.Jogador != null
                && personagemId == EntradaNoJogo.PersonagemId
            )
            {
                return EntradaNoJogo.Jogador.transform;
            }

            var objeto = GameObject.Find($"Jogador_{personagemId}");
            return objeto != null ? objeto.transform : null;
        }

        /// <summary>
        /// Limpa e descarta todos os balões de fala da interface.
        /// </summary>
        public void DescartarTudo()
        {
            foreach (var balao in _baloes.Values)
            {
                balao.Descartar();
            }

            _baloes.Clear();
            _expirados.Clear();
        }

        /// <summary>
        /// Cria e instancia a infraestrutura visual dos balões de fala na cena especificada.
        /// </summary>
        internal static GerenciadorBaloesDeFala Criar(Scene cena, ServicoDeChat servico)
        {
            var raiz = new GameObject(NomeObjeto);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelBaloesDeFala",
                -2.5f
            );
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            EstiloSocial.Aplicar(documento.rootVisualElement);

            var camada = new VisualElement { name = "SocialBaloesCamada" };
            camada.pickingMode = PickingMode.Ignore;
            camada.AddToClassList(EstiloSocial.BalaoCamada);
            documento.rootVisualElement.Add(camada);

            var gerenciador = raiz.AddComponent<GerenciadorBaloesDeFala>();
            gerenciador.Configurar(camada);
            gerenciador.ConectarServico(servico);

            return gerenciador;
        }
    }
}
