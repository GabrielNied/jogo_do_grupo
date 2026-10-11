using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Move o jogador local com o <see cref="CharacterController"/>: anda na direção da câmera,
    /// corre com Shift, pula com Espaço (só no chão), vira para onde anda, cai com gravidade e alimenta o Animator do visual.
    /// Os valores do movimento e o voo de desenvolvimento aparecem no Inspector e valem na hora,
    /// também com o Play aberto. O cálculo fica em <see cref="MovimentoTerceiraPessoa"/>. Criado pela
    /// <see cref="EntradaNoJogo"/>; não precisa ser montado em cena.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ControleTerceiraPessoa : MonoBehaviour
    {
        /// <summary>Quanto o jogador vira por segundo para a direção do movimento, em graus.</summary>
        [SerializeField]
        private float _velocidadeVirar = 720f;

        [Header("Movimento (ajustável no Play)")]
        [SerializeField, Min(0f), Tooltip("m/s")]
        private float _velocidadeAndar = MovimentoTerceiraPessoa.VelocidadeAndarPadrao;

        [SerializeField, Min(0f), Tooltip("m/s. Conectado, o servidor puxa de volta acima de 5,5.")]
        private float _velocidadeCorrer = MovimentoTerceiraPessoa.VelocidadeCorrerPadrao;

        [SerializeField, Min(0f), Tooltip("m/s²")]
        private float _aceleracao = MovimentoTerceiraPessoa.AceleracaoPadrao;

        [SerializeField, Min(0f), Tooltip("m/s²")]
        private float _gravidade = MovimentoTerceiraPessoa.GravidadePadrao;

        [SerializeField, Min(0f), Tooltip("m/s")]
        private float _velocidadeMaximaQueda = MovimentoTerceiraPessoa.VelocidadeMaximaQuedaPadrao;

        [SerializeField, Min(0f), Tooltip("m")]
        private float _alturaPulo = MovimentoTerceiraPessoa.AlturaPuloPadrao;

        [Header("Voo (só no editor)")]
        [
            SerializeField,
            Tooltip("Sem gravidade; Espaço sobe e Ctrl desce. Ignorado fora do editor.")
        ]
        private bool _voar;

        [SerializeField, Min(0f), Tooltip("m/s")]
        private float _velocidadeVerticalVoo = MovimentoTerceiraPessoa.VelocidadeVerticalVooPadrao;

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

        /// <summary>
        /// Valores de velocidade e gravidade (ajustáveis por quem criar o jogador). Mudar um
        /// campo no Inspector reaplica todos os campos do Inspector aqui.
        /// </summary>
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
            AplicarCamposNoMovimento();
        }

        // Chamado pelo editor a cada mudança no Inspector, também durante o Play.
        private void OnValidate()
        {
            AplicarCamposNoMovimento();
        }

        private void AplicarCamposNoMovimento()
        {
            _movimento.VelocidadeAndar = _velocidadeAndar;
            _movimento.VelocidadeCorrer = _velocidadeCorrer;
            _movimento.Aceleracao = _aceleracao;
            _movimento.Gravidade = _gravidade;
            _movimento.VelocidadeMaximaQueda = _velocidadeMaximaQueda;
            _movimento.AlturaPulo = _alturaPulo;
            _movimento.Voando = _voar;
            _movimento.VelocidadeVerticalVoo = _velocidadeVerticalVoo;
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
                dt,
                InputAtivo ? _acoes.Subir : 0f
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
