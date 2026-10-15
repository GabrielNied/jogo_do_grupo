using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Câmera que orbita e segue o jogador: o mouse gira, a roda dá zoom, e ela se aproxima quando
    /// há cenário entre ela e o jogador (mobs e outros jogadores não contam). O cálculo fica em
    /// <see cref="OrbitaCamera"/>. Quando encosta no jogador, o modelo local some (fica só a
    /// sombra). Criada pela <see cref="EntradaNoJogo"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraTerceiraPessoa : MonoBehaviour
    {
        /// <summary>Raio usado para detectar paredes atrás do jogador, em metros.</summary>
        private const float RaioColisao = 0.2f;

        /// <summary>Distância mínima até o pivô quando encosta numa parede, em metros.</summary>
        private const float DistanciaMinima = 0.5f;

        /// <summary>Abaixo desta distância até o pivô, em metros, o modelo local some.</summary>
        private const float DistanciaEsconderModelo = 1.1f;

        /// <summary>Quantos acertos o teste de obstáculo examina por frame.</summary>
        private const int MaximoAcertos = 16;

        /// <summary>Graus por pixel de movimento do mouse.</summary>
        [SerializeField]
        private float _sensibilidadeMouse = 0.12f;

        /// <summary>Graus por segundo com o analógico no máximo.</summary>
        [SerializeField]
        private float _velocidadeControle = 150f;

        private readonly OrbitaCamera _orbita = new OrbitaCamera();
        private readonly RaycastHit[] _acertos = new RaycastHit[MaximoAcertos];
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly Dictionary<Renderer, ShadowCastingMode> _sombrasOriginais =
            new Dictionary<Renderer, ShadowCastingMode>();

        private AcoesJogador _acoes;
        private Transform _alvo;
        private bool _modeloEscondido;

        /// <summary>Se lê o input (falso com o menu aberto).</summary>
        public bool InputAtivo { get; set; } = true;

        /// <summary>Ângulo horizontal da câmera, em graus: a "frente" do movimento.</summary>
        public float Horizontal => _orbita.Horizontal;

        /// <summary>Ângulos, distância e altura do pivô.</summary>
        public OrbitaCamera Orbita => _orbita;

        internal void Configurar(AcoesJogador acoes, Transform alvo)
        {
            MostrarModelo();
            _acoes = acoes;
            _alvo = alvo;
            if (alvo != null)
            {
                _orbita.Definir(alvo.eulerAngles.y, _orbita.Vertical);
                _orbita.Reiniciar(alvo.position);
                Posicionar(0f);
            }
        }

        private void LateUpdate()
        {
            if (_alvo == null)
            {
                return;
            }

            if (InputAtivo && _acoes != null)
            {
                var delta =
                    _acoes.OlharMouse * _sensibilidadeMouse * OpcoesLocais.Atual.Sensibilidade
                    + _acoes.OlharControle * (_velocidadeControle * Time.deltaTime);
                _orbita.Girar(delta);
                _orbita.Zoom(_acoes.ZoomCliques);
            }

            Posicionar(Time.deltaTime);
        }

        private void OnDisable()
        {
            MostrarModelo();
        }

        private void Posicionar(float dt)
        {
            var pivo = _orbita.PivoFiltrado(_alvo.position, dt);
            var livre = DistanciaLivre(pivo, _orbita.Rotacao * Vector3.back, _orbita.Distancia);
            var distancia = _orbita.AtualizarDistancia(Mathf.Max(DistanciaMinima, livre), dt);

            transform.SetPositionAndRotation(_orbita.Posicao(pivo, distancia), _orbita.Rotacao);
            AtualizarModelo(distancia < DistanciaEsconderModelo);
        }

        /// <summary>
        /// Até onde a câmera pode ir atrás do pivô sem atravessar cenário. Ignora o próprio
        /// jogador, mobs, outros jogadores e triggers: só o que não é personagem bloqueia.
        /// </summary>
        private float DistanciaLivre(Vector3 pivo, Vector3 paraTras, float distancia)
        {
            var quantidade = Physics.SphereCastNonAlloc(
                pivo,
                RaioColisao,
                paraTras,
                _acertos,
                distancia,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            var livre = float.PositiveInfinity;
            for (var i = 0; i < quantidade; i++)
            {
                var acerto = _acertos[i];
                // Distância 0 é colisor em que a esfera já começa dentro (o próprio jogador).
                if (acerto.distance <= 0f || acerto.distance >= livre)
                {
                    continue;
                }

                if (EhPersonagem(acerto.collider))
                {
                    continue;
                }

                livre = acerto.distance;
            }

            return livre;
        }

        /// <summary>
        /// Jogador local, outro jogador ou mob: CharacterController, corpo físico que se move ou
        /// algo com Animator acima (o visual animado dos personagens). Cenário não tem nenhum.
        /// </summary>
        private bool EhPersonagem(Collider colisor)
        {
            if (colisor.transform.IsChildOf(_alvo))
            {
                return true;
            }

            if (colisor is CharacterController)
            {
                return true;
            }

            var corpo = colisor.attachedRigidbody;
            if (corpo != null && !corpo.isKinematic)
            {
                return true;
            }

            // Animator acima (colisor num osso) ou abaixo (colisor na raiz do prefab).
            return colisor.GetComponentInParent<Animator>() != null
                || colisor.GetComponentInChildren<Animator>() != null;
        }

        /// <summary>
        /// Esconde o modelo local (deixa só a sombra) quando a câmera encosta, para ele não tapar a
        /// tela. Relê os renderers a cada frame escondido, para pegar equipamento trocado.
        /// </summary>
        private void AtualizarModelo(bool esconder)
        {
            if (!esconder)
            {
                MostrarModelo();
                return;
            }

            _modeloEscondido = true;
            _alvo.GetComponentsInChildren(_renderers);
            foreach (var renderer in _renderers)
            {
                if (_sombrasOriginais.ContainsKey(renderer))
                {
                    continue;
                }

                _sombrasOriginais[renderer] = renderer.shadowCastingMode;
                if (renderer.shadowCastingMode == ShadowCastingMode.Off)
                {
                    // Sem sombra antes: some de vez, em vez de ganhar uma sombra.
                    renderer.forceRenderingOff = true;
                }
                else
                {
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void MostrarModelo()
        {
            if (!_modeloEscondido)
            {
                return;
            }

            _modeloEscondido = false;
            foreach (var par in _sombrasOriginais)
            {
                if (par.Key != null)
                {
                    par.Key.shadowCastingMode = par.Value;
                    par.Key.forceRenderingOff = false;
                }
            }

            _sombrasOriginais.Clear();
        }
    }
}
