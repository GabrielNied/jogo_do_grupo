using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Como as mensagens do ataque viram texto e decisões de tela. Só formata o que o servidor
    /// mandou: dano, acerto e custo nunca são calculados aqui.
    /// </summary>
    internal static class ApresentacaoAtaque
    {
        public const string TextoVigorInsuficiente = "Vigor insuficiente";
        public const string TextoAcaoEmAndamento = "Aguarde o golpe terminar";
        public const string TextoVoceEstaCaido = "Você está caído";
        public const string TextoRecusaGenerica = "Ataque recusado";

        /// <summary>O número do dano como aparece na tela (nunca negativo).</summary>
        public static string TextoDoDano(int dano) => (dano < 0 ? 0 : dano).ToString();

        /// <summary>O aviso para o motivo de recusa do servidor; código desconhecido tem texto genérico.</summary>
        public static string TextoDaRecusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroCombate.VigorInsuficiente:
                    return TextoVigorInsuficiente;
                case CodigosErroCombate.AcaoEmAndamento:
                    return TextoAcaoEmAndamento;
                case CodigosErroCombate.VoceEstaCaido:
                    return TextoVoceEstaCaido;
                default:
                    return TextoRecusaGenerica;
            }
        }

        /// <summary>
        /// Se o dano de um golpe aparece na tela: só o que o próprio jogador causou (dano entre
        /// outras pessoas e inimigos não envolve você e não aparece).
        /// </summary>
        public static bool MostraODano(long atacanteId, long meuPersonagemId) =>
            meuPersonagemId != 0 && atacanteId == meuPersonagemId;
    }

    /// <summary>
    /// Monta os pedidos de ataque: numera em ordem crescente (a recusa do servidor devolve o
    /// número) e normaliza a direção da mira para 0 a 360 graus.
    /// </summary>
    internal sealed class GeradorDeIntencoes
    {
        private int _sequencia;

        public IntencaoDeAtaque Criar(TipoDeAtaque tipo, float direcaoYGraus)
        {
            _sequencia++;
            return new IntencaoDeAtaque
            {
                Tipo = tipo,
                DirecaoY = NormalizarDirecao(direcaoYGraus),
                Sequencia = _sequencia,
            };
        }

        /// <summary>Leva o ângulo para 0 a 360 (0 aponta para +Z e cresce para +X). Valor inválido vira 0.</summary>
        public static float NormalizarDirecao(float graus)
        {
            if (float.IsNaN(graus) || float.IsInfinity(graus))
            {
                return 0f;
            }

            var resto = graus % 360f;
            return resto < 0f ? resto + 360f : resto;
        }
    }

    /// <summary>Um aviso curto (ex.: ataque recusado) que some sozinho.</summary>
    internal sealed class AvisoTemporario
    {
        public const float DuracaoSegundos = 2f;

        private float _restante;

        public string Texto { get; private set; } = "";

        public bool Visivel => _restante > 0f;

        public void Mostrar(string texto)
        {
            Texto = texto ?? "";
            _restante = DuracaoSegundos;
        }

        public void Atualizar(float segundos)
        {
            if (_restante <= 0f)
            {
                return;
            }

            _restante -= segundos;
            if (_restante <= 0f)
            {
                _restante = 0f;
                Texto = "";
            }
        }
    }
}
