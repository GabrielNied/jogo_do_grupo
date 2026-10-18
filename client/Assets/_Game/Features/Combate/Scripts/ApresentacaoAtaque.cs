using System;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Como as mensagens do ataque viram texto e decisões de tela. Só formata o que o servidor
    /// mandou: dano, acerto e custo nunca são calculados aqui.
    /// </summary>
    internal static class ApresentacaoAtaque
    {
        public const string TextoStaminaInsuficiente = "Stamina insuficiente";
        public const string TextoAcaoEmAndamento = "Aguarde o golpe terminar";
        public const string TextoVoceEstaCaido = "Você está caído";
        public const string TextoVoceEstaAtordoado = "Você está atordoado";
        public const string TextoRecusaGenerica = "Ataque recusado";

        /// <summary>O número do dano como aparece na tela (nunca negativo).</summary>
        public static string TextoDoDano(int dano) => (dano < 0 ? 0 : dano).ToString();

        /// <summary>O aviso para o motivo de recusa do servidor; código desconhecido tem texto genérico.</summary>
        public static string TextoDaRecusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroCombate.StaminaInsuficiente:
                    return TextoStaminaInsuficiente;
                case CodigosErroCombate.AcaoEmAndamento:
                    return TextoAcaoEmAndamento;
                case CodigosErroCombate.VoceEstaCaido:
                    return TextoVoceEstaCaido;
                case CodigosErroCombate.VoceEstaAtordoado:
                    return TextoVoceEstaAtordoado;
                default:
                    return TextoRecusaGenerica;
            }
        }

        /// <summary>
        /// Se o dano de um golpe aparece na tela: o próprio jogador ou um aliado
        /// (docs/features/interface.md, "Texto flutuante de dano").
        /// </summary>
        public static bool MostraODano(
            long atacanteId,
            long meuPersonagemId,
            System.Func<long, bool> ehAliado = null
        )
        {
            if (meuPersonagemId == 0 || atacanteId == 0)
            {
                return false;
            }

            if (atacanteId == meuPersonagemId)
            {
                return true;
            }

            return ehAliado != null && ehAliado(atacanteId);
        }

        /// <summary>
        /// Onde o número de dano está no mundo: sobe de <paramref name="origem"/> (sobre a cabeça do
        /// alvo) até <paramref name="subidaMetros"/> acima, conforme o <paramref name="progresso"/> (0 a 1).
        /// </summary>
        public static UnityEngine.Vector3 PosicaoDoNumero(
            UnityEngine.Vector3 origem,
            float progresso,
            float subidaMetros
        ) => origem + UnityEngine.Vector3.up * (subidaMetros * progresso);

        /// <summary>
        /// Gira o corpo de <paramref name="atual"/> em direção à mira sem passar do teto do
        /// ataque (<see cref="RegrasAtaque.GiroMaximoGrausPorSegundo"/>), como o servidor mede o
        /// cone no acerto. O resultado fica entre 0 e 360 graus.
        /// </summary>
        public static float GirarParaAMira(
            float atual,
            float mira,
            TipoDeAtaque tipo,
            float segundos
        )
        {
            var teto = (float)(RegrasAtaque.GiroMaximoGrausPorSegundo(tipo) * segundos);
            var diferenca = (float)RegrasAtaque.DiferencaDeAngulo(mira, atual);
            var passo = diferenca < -teto ? -teto : (diferenca > teto ? teto : diferenca);
            return GeradorDeIntencoes.NormalizarDirecao(atual + passo);
        }

        /// <summary>A fração da corrida permitida com o compromisso do golpe (0,4).</summary>
        public static float FatorDoGolpe => (float)RegrasAtaque.VelocidadeDuranteGolpe;

        /// <summary>A velocidade com o compromisso do golpe (40% da corrida, por exemplo).</summary>
        public static float VelocidadeDuranteGolpe(float velocidade) => velocidade * FatorDoGolpe;

        /// <summary>Se o número do dano é do golpe mais forte da cadeia ou do pesado (aparece maior).</summary>
        public static bool GolpeForte(TipoDeAtaque tipo, int passo) =>
            tipo == TipoDeAtaque.Pesado || passo == RegrasAtaque.PassosDaCadeiaLeve - 1;
    }

    /// <summary>
    /// Monta os pedidos de ataque: numera em ordem crescente (a recusa do servidor devolve o
    /// número) e normaliza a direção da mira para 0 a 360 graus.
    /// </summary>
    internal sealed class GeradorDeIntencoes
    {
        private int _sequencia;

        public IntencaoDeAtaque Criar(
            TipoDeAtaque tipo,
            float direcaoYGraus,
            float elevacaoGraus = 0f
        )
        {
            _sequencia++;
            return new IntencaoDeAtaque
            {
                Tipo = tipo,
                DirecaoY = NormalizarDirecao(direcaoYGraus),
                ElevacaoGraus = NormalizarElevacao(elevacaoGraus),
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

        /// <summary>Clampeia a elevação vertical da mira entre os limites permitidos. Valor inválido vira 0.</summary>
        public static float NormalizarElevacao(float graus)
        {
            if (float.IsNaN(graus) || float.IsInfinity(graus))
            {
                return 0f;
            }

            return Math.Max(
                RegrasProjetil.ElevacaoMinimaGraus,
                Math.Min(RegrasProjetil.ElevacaoMaximaGraus, graus)
            );
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
