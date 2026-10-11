namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// O que aconteceu em um <see cref="AvisoDoGrupo"/>. Os valores são estáveis: nunca mude nem
    /// reaproveite um número já publicado. Regras em docs/features/social.md.
    /// </summary>
    public enum TipoDeAvisoDoGrupo
    {
        /// <summary>Para o convidante: o convidado recusou.</summary>
        ConviteRecusado = 0,

        /// <summary>Para os dois lados: o convite venceu ou foi cancelado sem resposta.</summary>
        ConviteExpirou = 1,

        /// <summary>Para o grupo: um personagem entrou.</summary>
        EntrouNoGrupo = 2,

        /// <summary>Para o grupo: um membro saiu (por vontade ou porque a vaga guardada venceu).</summary>
        SaiuDoGrupo = 3,

        /// <summary>Para o grupo: o líder agora é o personagem do aviso.</summary>
        LiderMudou = 4,

        /// <summary>Para o grupo e para o expulso: o líder expulsou o personagem do aviso.</summary>
        ExpulsoDoGrupo = 5,

        /// <summary>Para quem sobrou: o grupo ficou com uma pessoa só e acabou.</summary>
        GrupoDesfeito = 6,

        /// <summary>Para o grupo: um membro desconectou e a vaga dele está guardada.</summary>
        MembroOffline = 7,

        /// <summary>Para o grupo: um membro offline voltou a tempo.</summary>
        MembroVoltou = 8,
    }
}
