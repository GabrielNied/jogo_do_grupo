using System;
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
    /// <remarks>
    /// Conectado, andar e correr são do servidor (modelo do WoW): quem cuida da conexão chama
    /// <see cref="AplicarVelocidadesDoServidor"/>. Mudar andar ou correr no Inspector durante o
    /// Play dispara <see cref="VelocidadesPedidasNoInspector"/> para virar um pedido ao servidor;
    /// o valor só passa a valer quando o servidor responder. Offline, vale o Inspector.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ControleTerceiraPessoa : MonoBehaviour
    {
        /// <summary>Quanto o jogador vira por segundo para a direção do movimento, em graus.</summary>
        [SerializeField]
        private float _velocidadeVirar = 720f;

        [Header("Movimento (ajustável no Play)")]
        [
            SerializeField,
            Min(0f),
            Tooltip("m/s. Conectado, vira pedido ao servidor (só em desenvolvimento).")
        ]
        private float _velocidadeAndar = MovimentoTerceiraPessoa.VelocidadeAndarPadrao;

        [
            SerializeField,
            Min(0f),
            Tooltip("m/s. Conectado, vira pedido ao servidor (só em desenvolvimento).")
        ]
        private float _velocidadeCorrer = MovimentoTerceiraPessoa.VelocidadeCorrerPadrao;

        [SerializeField, Min(0f), Tooltip("m/s²")]
        private float _aceleracao = MovimentoTerceiraPessoa.AceleracaoPadrao;

        [SerializeField, Min(0f), Tooltip("m/s²")]
        private float _frenagem = MovimentoTerceiraPessoa.FrenagemPadrao;

        [SerializeField, Min(0f), Tooltip("m/s²")]
        private float _aceleracaoNoAr = MovimentoTerceiraPessoa.AceleracaoNoArPadrao;

        [SerializeField, Min(0f), Tooltip("m/s²")]
        private float _gravidade = MovimentoTerceiraPessoa.GravidadePadrao;

        [SerializeField, Min(0f), Tooltip("m/s")]
        private float _velocidadeMaximaQueda = MovimentoTerceiraPessoa.VelocidadeMaximaQuedaPadrao;

        [SerializeField, Min(0f), Tooltip("m")]
        private float _alturaPulo = MovimentoTerceiraPessoa.AlturaPuloPadrao;

        [SerializeField, Min(0f), Tooltip("s")]
        private float _coyoteTime = MovimentoTerceiraPessoa.CoyoteTimePadrao;

        [SerializeField, Min(0f), Tooltip("s")]
        private float _bufferDePulo = MovimentoTerceiraPessoa.BufferDePuloPadrao;

        [Header("Voo (só no editor)")]
        [
            SerializeField,
            Tooltip("Sem gravidade; Espaço sobe e Ctrl desce. Ignorado fora do editor.")
        ]
        // Só o Inspector liga; o "= false" explícito evita o aviso CS0649 na compilação.
        private bool _voar = false;

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

        // Velocidades do servidor (andar, correr); null enquanto offline: vale o Inspector.
        private Vector2? _velocidadesDoServidor;

        /// <summary>
        /// Andar e correr (m/s) pedidos no Inspector durante o Play, quando diferem dos do
        /// servidor. Quem cuida da conexão transforma em pedido.
        /// </summary>
        public event Action<float, float> VelocidadesPedidasNoInspector;

        /// <summary>Se andar e correr vêm do servidor (conectado) em vez do Inspector.</summary>
        public bool VelocidadesDoServidor => _velocidadesDoServidor.HasValue;

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

        /// <summary>
        /// Aplica andar e correr que o servidor mandou (autoritativos). O Inspector passa a
        /// mostrar esses valores.
        /// </summary>
        public void AplicarVelocidadesDoServidor(float andar, float correr)
        {
            _velocidadesDoServidor = new Vector2(andar, correr);
            _velocidadeAndar = andar;
            _velocidadeCorrer = correr;
            AplicarCamposNoMovimento();
        }

        /// <summary>Volta a usar o Inspector (ao desconectar).</summary>
        public void SoltarVelocidadesDoServidor()
        {
            _velocidadesDoServidor = null;
            AplicarCamposNoMovimento();
        }

        // Chamado pelo editor a cada mudança no Inspector, também durante o Play.
        private void OnValidate()
        {
            if (
                Application.isPlaying
                && _velocidadesDoServidor is { } doServidor
                && PedidoDiferente(doServidor, _velocidadeAndar, _velocidadeCorrer)
            )
            {
                VelocidadesPedidasNoInspector?.Invoke(_velocidadeAndar, _velocidadeCorrer);
            }

            AplicarCamposNoMovimento();
        }

        /// <summary>Se o Inspector pede algo diferente do que o servidor deu.</summary>
        internal static bool PedidoDiferente(Vector2 doServidor, float andar, float correr)
        {
            return !Mathf.Approximately(doServidor.x, andar)
                || !Mathf.Approximately(doServidor.y, correr);
        }

        private void AplicarCamposNoMovimento()
        {
            // Conectado, o servidor manda; o Inspector só vira pedido (OnValidate).
            _movimento.VelocidadeAndar = _velocidadesDoServidor?.x ?? _velocidadeAndar;
            _movimento.VelocidadeCorrer = _velocidadesDoServidor?.y ?? _velocidadeCorrer;
            _movimento.Aceleracao = _aceleracao;
            _movimento.Frenagem = _frenagem;
            _movimento.AceleracaoNoAr = _aceleracaoNoAr;
            _movimento.Gravidade = _gravidade;
            _movimento.VelocidadeMaximaQueda = _velocidadeMaximaQueda;
            _movimento.AlturaPulo = _alturaPulo;
            _movimento.CoyoteTime = _coyoteTime;
            _movimento.BufferDePulo = _bufferDePulo;
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

            var noAr = !_corpo.isGrounded;
            _velocidadeHorizontal = _movimento.Acelerar(
                _velocidadeHorizontal,
                _movimento.VelocidadeAlvo(entrada, correndo),
                dt,
                noAr
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
                InputAtivo && _acoes.PularApertado,
                dt
            );

            var deslocamento =
                (_direcao * _velocidadeHorizontal + Vector3.up * _velocidadeVertical) * dt;
            var colisao = _corpo.Move(deslocamento);

            // Bateu num teto subindo: começa a cair.
            if ((colisao & CollisionFlags.Above) != 0 && _velocidadeVertical > 0f)
            {
                _velocidadeVertical = 0f;
            }

            // Resetar estado de pulo quando estiver no chão
            if (_corpo.isGrounded)
            {
                _movimento.ResetarEstadoDePulo();
            }

            // Velocidade real no plano (parede freia a animação também).
            var real = _corpo.velocity;
            real.y = 0f;
            _animador?.Atualizar(real.magnitude, _corpo.isGrounded);
        }
    }
}
