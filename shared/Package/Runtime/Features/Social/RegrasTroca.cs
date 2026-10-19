using System;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Números e contas puras da troca direta entre jogadores (#175). Regras em
    /// docs/features/social.md; o servidor decide, o client só mostra (limite de itens, aviso de
    /// distância).
    /// </summary>
    public static class RegrasTroca
    {
        // ── Balanceamento ─────────────────────────────────────────────────────────

        /// <summary>Distância máxima, em metros, entre os dois jogadores para trocar.</summary>
        public const float DistanciaMaximaMetros = 10f;

        /// <summary>Itens que cada lado pode pôr na oferta.</summary>
        public const int ItensMaximosPorLado = 8;

        /// <summary>Segundos que o convite de troca vale.</summary>
        public const int ValidadeDoConviteSegundos = 30;

        // ── Contas ────────────────────────────────────────────────────────────────

        /// <summary>Se a distância (3D) entre dois pontos está dentro do limite da troca.</summary>
        public static bool DentroDaDistancia(
            float x1,
            float y1,
            float z1,
            float x2,
            float y2,
            float z2
        )
        {
            var dx = x1 - x2;
            var dy = y1 - y2;
            var dz = z1 - z2;
            return dx * dx + dy * dy + dz * dz <= DistanciaMaximaMetros * DistanciaMaximaMetros;
        }

        /// <summary>Se a oferta cabe nos limites (sem repetir item, sem coroas negativas).</summary>
        public static bool OfertaValida(long coroas, long[] instanciaIds)
        {
            if (coroas < 0 || instanciaIds == null || instanciaIds.Length > ItensMaximosPorLado)
            {
                return false;
            }

            for (var i = 0; i < instanciaIds.Length; i++)
            {
                for (var j = i + 1; j < instanciaIds.Length; j++)
                {
                    if (instanciaIds[i] == instanciaIds[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Códigos de recusa e de encerramento da troca direta, em
    /// <see cref="TrocaRecusada.Codigo"/> e <see cref="TrocaEncerrada.Motivo"/>.
    /// </summary>
    public static class CodigosErroTroca
    {
        // ── Recusas ───────────────────────────────────────────────────────────────

        /// <summary>Nome inexistente, personagem offline ou que ignora quem pede.</summary>
        public const string AlvoNaoEncontrado = "AlvoNaoEncontrado";

        public const string TrocaComSiMesmo = "TrocaComSiMesmo";

        /// <summary>Quem pede já está numa troca ou com convite de troca pendente.</summary>
        public const string VoceJaEstaEmTroca = "VoceJaEstaEmTroca";

        public const string AlvoJaEstaEmTroca = "AlvoJaEstaEmTroca";

        public const string LongeDemais = "LongeDemais";

        public const string EmCombate = "EmCombate";

        public const string TrocaNaoEncontrada = "TrocaNaoEncontrada";

        public const string OfertaInvalida = "OfertaInvalida";

        public const string SaldoInsuficiente = "SaldoInsuficiente";

        /// <summary>Item que não existe, não é do jogador, está equipado ou é favorito.</summary>
        public const string ItemIndisponivel = "ItemIndisponivel";

        /// <summary>A bolsa de quem recebe não tem casa para todos os itens.</summary>
        public const string BolsaCheia = "BolsaCheia";

        public const string ConviteExpirado = "ConviteExpirado";

        // ── Motivos de encerramento ───────────────────────────────────────────────

        public const string Concluida = "Concluida";

        public const string CanceladaPorVoce = "CanceladaPorVoce";

        public const string CanceladaPeloOutro = "CanceladaPeloOutro";

        public const string ConviteRecusado = "ConviteRecusado";

        public const string Desconectou = "Desconectou";
    }
}
