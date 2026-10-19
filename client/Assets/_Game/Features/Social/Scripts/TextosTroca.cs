using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>Os textos da troca direta, num lugar só (recusas, encerramentos, rótulos).</summary>
    internal static class TextosTroca
    {
        public const string TituloDaJanela = "Troca";
        public const string VoceOferece = "Você oferece";
        public const string SuaBolsa = "Sua bolsa (clique para oferecer)";
        public const string Coroas = "Coroas";
        public const string Aceitar = "Aceitar troca";
        public const string Cancelar = "Cancelar troca";
        public const string AguardandoOutro = "Aguardando o outro jogador aceitar...";
        public const string VoceAceitou = "Você aceitou. Qualquer mudança desfaz o aceite.";
        public const string OutroAceitou = "O outro jogador aceitou. Confira a oferta e aceite.";
        public const string BolsaVazia = "Nada na bolsa que possa ser negociado.";
        public const string NadaOferecido = "Nada oferecido ainda.";

        public static string ConviteEnviado(string nome) => $"Convite de troca enviado a {nome}.";

        public static string ConviteRecebido(string nome) => $"{nome} quer trocar itens com você.";

        public static string OfertaDoOutro(string nome) => $"{nome} oferece";

        public static string Codigo(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroTroca.AlvoNaoEncontrado:
                    return "Jogador não encontrado ou offline.";
                case CodigosErroTroca.TrocaComSiMesmo:
                    return "Você não pode trocar consigo mesmo.";
                case CodigosErroTroca.VoceJaEstaEmTroca:
                    return "Você já está em uma troca.";
                case CodigosErroTroca.AlvoJaEstaEmTroca:
                    return "Esse jogador já está em uma troca.";
                case CodigosErroTroca.LongeDemais:
                    return $"Chegue a {RegrasTroca.DistanciaMaximaMetros:0} m do outro jogador.";
                case CodigosErroTroca.EmCombate:
                    return "Não dá para trocar em combate.";
                case CodigosErroTroca.TrocaNaoEncontrada:
                    return "Essa troca não existe mais.";
                case CodigosErroTroca.OfertaInvalida:
                    return $"Oferta inválida (até {RegrasTroca.ItensMaximosPorLado} itens, sem repetir).";
                case CodigosErroTroca.SaldoInsuficiente:
                    return "Coroas insuficientes para essa oferta.";
                case CodigosErroTroca.ItemIndisponivel:
                    return "Um item da oferta não está mais disponível.";
                case CodigosErroTroca.BolsaCheia:
                    return "A bolsa de quem recebe está cheia.";
                case CodigosErroTroca.ConviteExpirado:
                    return "O convite de troca venceu.";
                default:
                    return "Não foi possível trocar.";
            }
        }

        public static string Encerrada(string motivo, string nomeDoOutro)
        {
            switch (motivo)
            {
                case CodigosErroTroca.Concluida:
                    return $"Troca com {nomeDoOutro} concluída.";
                case CodigosErroTroca.CanceladaPorVoce:
                    return "Troca cancelada.";
                case CodigosErroTroca.CanceladaPeloOutro:
                    return $"{nomeDoOutro} cancelou a troca.";
                case CodigosErroTroca.ConviteRecusado:
                    return $"{nomeDoOutro} recusou a troca.";
                case CodigosErroTroca.Desconectou:
                    return $"{nomeDoOutro} saiu do jogo. Nada foi trocado.";
                default:
                    return Codigo(motivo) + " Nada foi trocado.";
            }
        }
    }
}
