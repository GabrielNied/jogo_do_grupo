using JogoDoGrupo.Shared.Features.Pvp;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>Textos formatados para notificações e interface de PvP.</summary>
    public static class TextosPvp
    {
        public static string MensagemConviteDuelo(string desafianteNome) =>
            $"{desafianteNome} desafiou você para um duelo! (Pressione Y para aceitar, N para recusar)";

        public static string MensagemDueloIniciado(int contagem) =>
            $"⚔️ Duelo iniciando em {contagem}s!";

        public static string MensagemDueloEncerrado(bool vitoria, MotivoDueloEncerramento motivo)
        {
            var resultado = vitoria ? "Vitória no duelo!" : "Derrota no duelo!";
            var detalhe = motivo switch
            {
                MotivoDueloEncerramento.VitoriaPorDano => "Adversário derrotado.",
                MotivoDueloEncerramento.Desistencia => "Oponente desistiu.",
                MotivoDueloEncerramento.FugiuDoRaio => "Oponente fugiu da área do duelo.",
                MotivoDueloEncerramento.Desconexao => "Oponente desconectou.",
                _ => "Duelo cancelado.",
            };
            return $"{resultado} ({detalhe})";
        }

        public static string MensagemFilaArena(bool naFila, int segundos) =>
            naFila
                ? $"⚔️ Na fila de Arena 3v3 ({segundos / 60:D2}:{segundos % 60:D2})"
                : "Fora da fila de arena.";

        public static string MensagemResultadoArena(
            bool vitoria,
            int variacaoPontos,
            int novoRanking
        )
        {
            var resultado = vitoria ? "🏆 Vitória na Arena 3v3!" : "💀 Derrota na Arena 3v3!";
            var sinal = variacaoPontos >= 0 ? "+" : "";
            return $"{resultado} ({sinal}{variacaoPontos} pontos | Ranking: {novoRanking})";
        }

        public static string MensagemBandeira(bool ativa) =>
            ativa
                ? "🚩 Bandeira PvP Ativada! Cuidado fora da cidade."
                : "🏳️ Bandeira PvP Desativada.";

        public static string TraduzirRecusa(string motivo) =>
            motivo switch
            {
                CodigosErroPvp.AlvoInvalido => "Alvo inválido para duelo.",
                CodigosErroPvp.LongeDemais => "Alvo está longe demais.",
                CodigosErroPvp.AlvoJaEmDuelo => "Alvo já está em um duelo.",
                CodigosErroPvp.VoceJaEmDuelo => "Você já está em um duelo.",
                CodigosErroPvp.RecusadoPeloAlvo => "O alvo recusou o convite de duelo.",
                CodigosErroPvp.ConviteExpirado => "O convite de duelo expirou.",
                CodigosErroPvp.ApenasNaCidade => "Ação permitida apenas dentro da cidade.",
                _ => "Não foi possível realizar a ação de PvP.",
            };
    }
}
