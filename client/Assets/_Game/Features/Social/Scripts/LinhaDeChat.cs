namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>Canal de uma linha do chat na tela. Sistema só existe no client.</summary>
    internal enum CanalDaLinha
    {
        Cidade,
        Grupo,
        Sussurro,
        Sistema,
    }

    /// <summary>Uma linha do histórico do chat, já pronta para mostrar como texto puro.</summary>
    internal sealed class LinhaDeChat
    {
        public LinhaDeChat(
            CanalDaLinha canal,
            long remetenteId,
            string nome,
            string nomeDoAlvo,
            string texto,
            long horaUnixMs,
            bool propria
        )
        {
            Canal = canal;
            RemetenteId = remetenteId;
            Nome = nome ?? "";
            NomeDoAlvo = nomeDoAlvo ?? "";
            Texto = texto ?? "";
            HoraUnixMs = horaUnixMs;
            Propria = propria;
        }

        public CanalDaLinha Canal { get; }
        public long RemetenteId { get; }

        /// <summary>Quem falou (vazio no Sistema).</summary>
        public string Nome { get; }

        /// <summary>Só no sussurro: quem recebeu.</summary>
        public string NomeDoAlvo { get; }

        public string Texto { get; }
        public long HoraUnixMs { get; }

        /// <summary>Se foi o próprio jogador quem falou (nunca é escondida por ignorar).</summary>
        public bool Propria { get; }

        public static LinhaDeChat Sistema(string texto, long horaUnixMs) =>
            new LinhaDeChat(CanalDaLinha.Sistema, 0, "", "", texto, horaUnixMs, true);
    }
}
