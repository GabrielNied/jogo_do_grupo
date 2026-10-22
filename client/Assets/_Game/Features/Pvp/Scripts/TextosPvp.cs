using JogoDoGrupo.Shared.Features.Pvp;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>Textos formatados para notificações e interface de PvP.</summary>
    public static class TextosPvp
    {
        public static string MensagemConviteDuelo(string desafianteNome) =>
            $"{desafianteNome} desafiou você para um duelo! ({TeclasDeConviteDeDuelo.RotuloAceitar} aceita, {TeclasDeConviteDeDuelo.RotuloRecusar} recusa)";

        public static string ConviteNaTela(string desafianteNome) =>
            $"{desafianteNome} desafiou você para um duelo.";

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

        // ── Janela de PvP ────────────────────────────────────────────────────

        public const string TituloDaJanela = "PvP";
        public const string SecaoBandeira = "Bandeira PvP";
        public const string SecaoArena = "Arena 3v3";
        public const string SecaoDuelo = "Duelo";
        public const string LigarBandeira = "Ligar bandeira";
        public const string DesligarBandeira = "Desligar bandeira";
        public const string EntrarNaFila = "Entrar na fila";
        public const string SairDaFila = "Sair da fila";
        public const string AtualizarRanking = "Ver ranking";
        public const string DesafiarProximo = "Desafiar jogador próximo";
        public const string CancelarDuelo = "Cancelar duelo";
        public const string SemAlvoProximo = "Nenhum jogador a até 15 m para desafiar.";
        public const string EmDueloAgora = "Você está em duelo.";
        public const string SemDuelo = "Sem duelo em andamento.";
        public const string SemRanking = "Toque em Ver ranking para consultar seus pontos.";
        public const string SemConexao = "PvP indisponível: entre no mundo primeiro.";

        public static string EstadoDaBandeira(bool ativa) =>
            ativa
                ? "Bandeira ligada: você pode lutar com quem também ligou."
                : "Bandeira desligada. Ligue na cidade para lutar com quem também ligou.";

        public static string EstadoDaArena(bool naFila, bool emArena, int segundos)
        {
            if (emArena)
                return "Partida de arena em andamento.";
            return naFila ? MensagemFilaArena(true, segundos) : "Fora da fila de arena.";
        }

        public static string Ranking(RespostaRankingArena r) =>
            $"Temporada {r.Temporada}: {r.Pontos} pontos, {r.Vitorias} vitórias, {r.Derrotas} derrotas.";

        public static string AlvoProximo(string nome) => $"Alvo próximo: {nome}.";

        public static string DesafioEnviado(string nome) => $"Desafio enviado a {nome}.";

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
