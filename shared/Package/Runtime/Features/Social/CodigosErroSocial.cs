namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Códigos de recusa de Social que o servidor coloca em
    /// <see cref="OperacaoDoGrupoRecusada.Codigo"/>. A tabela com o "quando" de cada um está em
    /// docs/features/social.md.
    /// </summary>
    public static class CodigosErroSocial
    {
        /// <summary>Nome inexistente ou personagem offline (a mesma resposta para os dois).</summary>
        public const string AlvoNaoEncontrado = "AlvoNaoEncontrado";

        /// <summary>O alvo (ou quem aceita) já está em um grupo.</summary>
        public const string AlvoJaEmGrupo = "AlvoJaEmGrupo";

        /// <summary>O convidante é o próprio alvo.</summary>
        public const string ConviteParaSiMesmo = "ConviteParaSiMesmo";

        /// <summary>Já tem o máximo de membros (contando vagas prometidas em convites).</summary>
        public const string GrupoCheio = "GrupoCheio";

        /// <summary>Operação do líder feita por membro comum.</summary>
        public const string NaoELider = "NaoELider";

        /// <summary>O mesmo convidante já tem convite pendente para o alvo.</summary>
        public const string ConviteJaPendente = "ConviteJaPendente";

        /// <summary>O alvo já tem o máximo de convites pendentes.</summary>
        public const string AlvoComConvitesDemais = "AlvoComConvitesDemais";

        /// <summary>O convidante passou do limite de frequência de convites.</summary>
        public const string ConvitesRapidosDemais = "ConvitesRapidosDemais";

        /// <summary>Resposta a um convite que não existe ou que não é de quem respondeu.</summary>
        public const string ConviteNaoEncontrado = "ConviteNaoEncontrado";

        /// <summary>Aceitar depois da validade do convite.</summary>
        public const string ConviteExpirado = "ConviteExpirado";

        /// <summary>Aceitar quando o convidante já saiu do jogo.</summary>
        public const string ConvidanteOffline = "ConvidanteOffline";

        /// <summary>Sair, expulsar ou passar a liderança sem estar em grupo.</summary>
        public const string NaoEstaEmGrupo = "NaoEstaEmGrupo";

        /// <summary>Expulsar ou passar a liderança para quem não é membro.</summary>
        public const string AlvoNaoEstaNoGrupo = "AlvoNaoEstaNoGrupo";

        /// <summary>Passar a liderança para si mesmo ou para membro offline.</summary>
        public const string AlvoInvalido = "AlvoInvalido";

        /// <summary>Tentar expulsar o líder.</summary>
        public const string NaoPodeExpulsarLider = "NaoPodeExpulsarLider";

        /// <summary>Reconvidar quem foi expulso há pouco tempo.</summary>
        public const string ReconviteBloqueado = "ReconviteBloqueado";
    }
}
