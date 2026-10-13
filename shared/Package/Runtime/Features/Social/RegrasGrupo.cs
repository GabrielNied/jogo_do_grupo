using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Números e fórmulas puras do grupo de jogadores. Regras em docs/features/social.md; o
    /// servidor é quem decide, o client usa só para mostrar (vagas, contagem do convite).
    /// </summary>
    public static class RegrasGrupo
    {
        // ── Balanceamento ─────────────────────────────────────────────────────────

        /// <summary>Tamanho máximo do grupo, contando o líder.</summary>
        public const int MaximoDeMembros = 5;

        /// <summary>Com menos membros que isto o grupo se desfaz.</summary>
        public const int MinimoDeMembros = 2;

        /// <summary>Segundos que um convite vale; a partir daí, expirou.</summary>
        public const int ValidadeDoConviteSegundos = 60;

        /// <summary>Segundos que a vaga de quem desconectou fica guardada.</summary>
        public const int VagaGuardadaSegundos = 60;

        /// <summary>Segundos em que o grupo não pode reconvidar quem foi expulso.</summary>
        public const int BloqueioDeReconviteSegundos = 30;

        /// <summary>Segundos mínimos entre dois convites do mesmo convidante.</summary>
        public const double IntervaloEntreConvitesSegundos = 1.0;

        /// <summary>Convites pendentes que um mesmo alvo pode ter, de convidantes diferentes.</summary>
        public const int MaximoDeConvitesPorAlvo = 5;

        /// <summary>
        /// Convites pendentes que um convidante pode ter ao mesmo tempo: as vagas de um grupo só
        /// com ele.
        /// </summary>
        public const int MaximoDeConvitesPorConvidante = MaximoDeMembros - 1;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Vagas que ainda dá para prometer: o máximo menos os membros e os convites pendentes.
        /// Quem convida sem grupo conta como 1 membro. Nunca é negativo.
        /// </summary>
        public static int VagasLivres(int membros, int convitesPendentes)
        {
            long ocupadas = (long)Math.Max(0, membros) + Math.Max(0, convitesPendentes);
            return (int)Math.Max(0L, MaximoDeMembros - ocupadas);
        }

        /// <summary>Se cabe mais um convite (senão é <c>GrupoCheio</c>).</summary>
        public static bool CabeMaisUmConvite(int membros, int convitesPendentes) =>
            VagasLivres(membros, convitesPendentes) > 0;

        /// <summary>Se cabe mais um membro agora (conferido de novo ao aceitar).</summary>
        public static bool CabeMaisUmMembro(int membros) => membros < MaximoDeMembros;

        /// <summary>Se o alvo já tem convites pendentes demais para receber outro.</summary>
        public static bool AlvoComConvitesDemais(int convitesPendentesDoAlvo) =>
            convitesPendentesDoAlvo >= MaximoDeConvitesPorAlvo;

        /// <summary>Convite com 59 s ainda vale; com 60 s ou mais, expirou.</summary>
        public static bool ConviteExpirou(double segundosDesdeOConvite) =>
            segundosDesdeOConvite >= ValidadeDoConviteSegundos;

        /// <summary>Segundos que faltam para o convite expirar (nunca negativo).</summary>
        public static int ValidadeRestanteSegundos(double segundosDesdeOConvite)
        {
            double restante = Math.Ceiling(ValidadeDoConviteSegundos - segundosDesdeOConvite);
            return (int)Math.Max(0.0, Math.Min(ValidadeDoConviteSegundos, restante));
        }

        /// <summary>Voltar com até 60 s mantém a vaga; passando disso, o membro é removido.</summary>
        public static bool VagaGuardadaVenceu(double segundosOffline) =>
            segundosOffline > VagaGuardadaSegundos;

        /// <summary>Se o expulso ainda não pode ser reconvidado pelo mesmo grupo.</summary>
        public static bool ReconviteBloqueado(double segundosDesdeAExpulsao) =>
            segundosDesdeAExpulsao < BloqueioDeReconviteSegundos;

        /// <summary>Se o convidante está convidando rápido demais.</summary>
        public static bool ConviteRapidoDemais(double segundosDesdeOUltimoConvite) =>
            segundosDesdeOUltimoConvite < IntervaloEntreConvitesSegundos;

        /// <summary>Se o grupo se desfaz com essa quantidade de membros.</summary>
        public static bool DeveDesfazer(int membros) => membros < MinimoDeMembros;

        /// <summary>
        /// Quem herda a liderança quando o líder sai ou perde a vaga. Recebe, na ordem de entrada
        /// no grupo, se cada membro está online, e a posição do líder atual. Devolve a posição do
        /// membro mais antigo que esteja online; se ninguém mais estiver online, a do mais antigo
        /// entre os offline; -1 se o líder está sozinho.
        /// </summary>
        public static int PosicaoDoHerdeiro(IReadOnlyList<bool> onlineEmOrdem, int posicaoDoLider)
        {
            if (onlineEmOrdem == null)
            {
                return -1;
            }

            int maisAntigoOffline = -1;
            for (int i = 0; i < onlineEmOrdem.Count; i++)
            {
                if (i == posicaoDoLider)
                {
                    continue;
                }
                if (onlineEmOrdem[i])
                {
                    return i;
                }
                if (maisAntigoOffline < 0)
                {
                    maisAntigoOffline = i;
                }
            }
            return maisAntigoOffline;
        }
    }
}
