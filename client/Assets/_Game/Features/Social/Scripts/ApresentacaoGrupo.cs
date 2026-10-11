using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O que a lista do grupo mostra, sem Unity: a ordem é a do servidor e cada linha diz se é
    /// o líder, se está offline e quais botões valem. Quem decide se o pedido vale é o servidor;
    /// os botões só evitam pedidos que ele recusaria.
    /// </summary>
    internal static class ApresentacaoGrupo
    {
        public const string SufixoLider = " (líder)";
        public const string SufixoOffline = " (offline)";
        public const string SufixoVoce = " (você)";

        public static IReadOnlyList<LinhaDoMembro> Linhas(EstadoDoGrupo estado)
        {
            var linhas = new List<LinhaDoMembro>();
            foreach (var membro in estado.Membros)
            {
                if (membro == null)
                {
                    continue;
                }

                var lider = membro.PersonagemId == estado.LiderId;
                var voce = membro.PersonagemId == estado.PersonagemLocalId;
                var texto = membro.Nome;
                if (voce)
                {
                    texto += SufixoVoce;
                }

                if (lider)
                {
                    texto += SufixoLider;
                }

                if (!membro.Online)
                {
                    texto += SufixoOffline;
                }

                linhas.Add(
                    new LinhaDoMembro
                    {
                        PersonagemId = membro.PersonagemId,
                        Nome = membro.Nome,
                        Texto = texto,
                        Lider = lider,
                        Online = membro.Online,
                        Voce = voce,

                        // O líder expulsa e passa a liderança a outro membro; só passa a quem está online.
                        PodeExpulsar = estado.SouLider && !voce && !lider,
                        PodePassarLideranca = estado.SouLider && !voce && !lider && membro.Online,
                    }
                );
            }

            return linhas;
        }

        public static string TextoDoConvite(ConviteDoGrupo convite)
        {
            var segundos = (int)System.Math.Ceiling(System.Math.Max(0f, convite.Restante));
            return convite.NomeDoConvidante + " convidou você para o grupo (" + segundos + " s)";
        }

        /// <summary>
        /// A linha que aparece por cima do jogo com a janela fechada: a mensagem mais recente ou,
        /// sem ela, o convite que espera resposta. Vazio se não há nada a dizer.
        /// </summary>
        public static string TextoNaTela(EstadoDoGrupo estado, string teclaDaJanela)
        {
            if (estado.MensagemRestante > 0f && !string.IsNullOrEmpty(estado.Mensagem))
            {
                return estado.Mensagem;
            }

            if (estado.Convites.Count == 0)
            {
                return "";
            }

            var convite = estado.Convites[estado.Convites.Count - 1];
            var texto =
                convite.NomeDoConvidante
                + " convidou você para o grupo. "
                + teclaDaJanela
                + ": responder";
            return estado.Convites.Count > 1
                ? texto + " (+" + (estado.Convites.Count - 1) + ")"
                : texto;
        }
    }

    /// <summary>Uma linha da lista do grupo.</summary>
    internal sealed class LinhaDoMembro
    {
        public long PersonagemId;
        public string Nome = "";

        /// <summary>Nome com as marcas (você, líder, offline).</summary>
        public string Texto = "";
        public bool Lider;
        public bool Online;
        public bool Voce;
        public bool PodeExpulsar;
        public bool PodePassarLideranca;
    }
}
