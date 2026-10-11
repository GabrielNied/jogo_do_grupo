using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Move o jogador local com o <see cref="CharacterController"/>: anda na direção da câmera,
    /// corre com Shift, pula com Espaço (só no chão), vira para onde anda, cai com gravidade e alimenta o Animator do visual.
    /// O cálculo fica em <see cref="MovimentoTerceiraPessoa"/>. Criado pela
    /// <see cref="EntradaNoJogo"/>; não precisa ser montado em cena.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ControleTerceiraPessoa : MonoBehaviour
    {
        /// <summary>Quanto o jogador vira por segundo para a direção do movimento, em graus.</summary>
        [SerializeField]
        private float _velocidadeVirar = 720f;

        private readonly MovimentoTerceiraPessoa _movimento = new MovimentoTerceiraPessoa();

        private CharacterController _corpo;
        private AcoesJogador _acoes;
        private CameraTerceiraPessoa _camera;
        private AnimadorDoVisual _animador;
        private Vector3 _direcao;
        private float _velocidadeHorizontal;
        private float _velocidadeVertical;

        /// <summary>Se lê o input (falso com o menu aberto: o jogador para).</summary>
        public bool InputAtivo { get; set; } = true;

        /// <summary>Velocidade horizontal atual, em m/s.</summary>
        public float VelocidadeHorizontal => _velocidadeHorizontal;

        /// <summary>Se o jogador está no chão.</summary>
        public bool NoChao => _corpo != null && _corpo.isGrounded;

        /// <summary>Valores de velocidade e gravidade (ajustáveis por quem criar o jogador).</summary>
        public MovimentoTerceiraPessoa Movimento => _movimento;

        internal void Configurar(
            AcoesJogador acoes,
            CameraTerceiraPessoa camera,
            AnimadorDoVisual animador
        )
        {
            _acoes = acoes;
            _camera = camera;
            _animador = animador;
            _direcao = transform.forward;
        }

        private void Awake()
        {
            _corpo = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_acoes == null || _corpo == null || !_corpo.enabled)
            {
                return;
            }

            var dt = Time.deltaTime;
            var entrada = InputAtivo ? _acoes.Mover : Vector2.zero;
            var correndo = InputAtivo && _acoes.Correr;
            var anguloCamera = _camera != null ? _camera.Horizontal : transform.eulerAngles.y;

            var direcao = MovimentoTerceiraPessoa.DirecaoNoMundo(entrada, anguloCamera);
            if (direcao != Vector3.zero)
            {
                _direcao = direcao;
                var alvo = Quaternion.LookRotation(direcao, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    alvo,
                    _velocidadeVirar * dt
                );
            }

            _velocidadeHorizontal = _movimento.Acelerar(
                _velocidadeHorizontal,
                _movimento.VelocidadeAlvo(entrada, correndo),
                dt
            );
            _velocidadeVertical = _movimento.VelocidadeVertical(
                _velocidadeVertical,
                _corpo.isGrounded,
                dt
            );
            _velocidadeVertical = _movimento.Pular(
                _velocidadeVertical,
                _corpo.isGrounded,
                InputAtivo && _acoes.PularApertado
            );

            var deslocamento =
                (_direcao * _velocidadeHorizontal + Vector3.up * _velocidadeVertical) * dt;
            var colisao = _corpo.Move(deslocamento);

            // Bateu num teto subindo: começa a cair.
            if ((colisao & CollisionFlags.Above) != 0 && _velocidadeVertical > 0f)
            {
                _velocidadeVertical = 0f;
            }

            // Velocidade real no plano (parede freia a animação também).
            var real = _corpo.velocity;
            real.y = 0f;
            _animador?.Atualizar(real.magnitude, _corpo.isGrounded);
        }
    }
}
