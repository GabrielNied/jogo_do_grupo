using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Decisões puras da esquiva no client: tecla, direção do pedido e texto da recusa. Custo,
    /// distância e duração vêm de <see cref="RegrasEsquiva"/>; quem decide é o servidor.
    /// </summary>
    internal static class ApresentacaoEsquiva
    {
        /// <summary>Tecla padrão da esquiva (docs/features/interface.md, Teclas e atalhos).</summary>
        public const string TeclaPadrao = "<Keyboard>/leftAlt";

        public const string TextoVigorInsuficiente = "Vigor insuficiente";
        public const string TextoAcaoEmAndamento = "Aguarde o golpe terminar";
        public const string TextoVoceEstaCaido = "Você está caído";
        public const string TextoRecusaGenerica = "Esquiva recusada";

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
        /// Direção do pedido, em graus (0 = +Z, cresce para +X): a do movimento atual em relação
        /// à câmera; parado, para onde o personagem está virado.
        /// </summary>
        public static float DirecaoDoPedido(
            Vector2 entrada,
            float anguloCamera,
            float rotacaoDoJogador
        )
        {
            if (entrada.sqrMagnitude < 0.01f)
            {
                return GeradorDeIntencoes.NormalizarDirecao(rotacaoDoJogador);
            }

            var local = Mathf.Atan2(entrada.x, entrada.y) * Mathf.Rad2Deg;
            return GeradorDeIntencoes.NormalizarDirecao(anguloCamera + local);
        }

        /// <summary>Se o jogo está em condição de pedir a esquiva agora.</summary>
        public static bool PodePedir(bool conectado, bool cursorPreso, bool predicaoAtiva) =>
            conectado && cursorPreso && !predicaoAtiva;

        /// <summary>Se a mensagem do servidor é sobre o meu personagem.</summary>
        public static bool EhMinha(long personagemId, long meuPersonagemId) =>
            meuPersonagemId != 0 && personagemId == meuPersonagemId;
    }

    /// <summary>Monta os pedidos de esquiva: numera em ordem crescente e normaliza a direção.</summary>
    internal sealed class GeradorDeEsquivas
    {
        private int _sequencia;

        public IntencaoDeEsquiva Criar(float direcaoYGraus)
        {
            _sequencia++;
            return new IntencaoDeEsquiva
            {
                DirecaoY = GeradorDeIntencoes.NormalizarDirecao(direcaoYGraus),
                Sequencia = _sequencia,
            };
        }
    }

    /// <summary>
    /// Antecipa o deslocamento da esquiva enquanto o servidor não responde, no mesmo ritmo de
    /// <see cref="RegrasEsquiva"/> (reta, <see cref="RegrasEsquiva.DistanciaMetros"/> em
    /// <see cref="RegrasEsquiva.DuracaoSegundos"/>). Se o servidor recusar durante o movimento,
    /// a esquiva para onde está (ADR 0024); o ponto de partida ainda é devolvido para quem
    /// precisar dele, mas o client não volta no tempo.
    /// </summary>
    internal sealed class PredicaoDeEsquiva
    {
        private double _decorrido;
        private double _inicioX;
        private double _inicioZ;
        private double _dirX;
        private double _dirZ;

        public bool Ativa { get; private set; }

        public int Sequencia { get; private set; }

        public void Iniciar(int sequencia, double x, double z, float direcaoYGraus)
        {
            var radianos = direcaoYGraus * System.Math.PI / 180.0;
            _dirX = System.Math.Sin(radianos);
            _dirZ = System.Math.Cos(radianos);
            _inicioX = x;
            _inicioZ = z;
            _decorrido = 0.0;
            Sequencia = sequencia;
            Ativa = true;
        }

        /// <summary>
        /// Avança o tempo e devolve quanto o personagem anda neste passo. Sem esquiva ativa, zero.
        /// Termina sozinha ao completar a duração.
        /// </summary>
        public void Avancar(double segundos, out double passoX, out double passoZ)
        {
            passoX = 0.0;
            passoZ = 0.0;
            if (!Ativa || segundos <= 0.0)
            {
                return;
            }

            var antes = RegrasEsquiva.DistanciaPermitidaAte(_decorrido);
            _decorrido += segundos;
            var depois = RegrasEsquiva.DistanciaPermitidaAte(_decorrido);
            passoX = _dirX * (depois - antes);
            passoZ = _dirZ * (depois - antes);
            if (_decorrido >= RegrasEsquiva.DuracaoSegundos)
            {
                Ativa = false;
            }
        }

        /// <summary>
        /// Servidor recusou o pedido <paramref name="sequencia"/>. Se é o que está em andamento,
        /// cancela e devolve o ponto de partida para voltar; senão não há nada a desfazer.
        /// </summary>
        public bool Recusar(int sequencia, out double voltarX, out double voltarZ)
        {
            voltarX = _inicioX;
            voltarZ = _inicioZ;
            if (!Ativa || sequencia != Sequencia)
            {
                return false;
            }

            Ativa = false;
            return true;
        }

        /// <summary>Cancela sem desfazer (ex.: saiu do mundo).</summary>
        public void Cancelar()
        {
            Ativa = false;
        }
    }
}
