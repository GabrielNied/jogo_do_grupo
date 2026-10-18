using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Os textos do grupo que a pessoa lê: avisos do servidor, recusas e mensagens do próprio
    /// client. Só traduz o que o servidor mandou; não decide nada.
    /// </summary>
    internal static class TextosGrupo
    {
        public const string SemConexao = "Sem conexão com o servidor.";
        public const string NomeVazio = "Digite o nome do personagem.";
        public const string RecusaGenerica = "Não foi possível completar o pedido do grupo.";
        public const string AvisoSemGrupo = "Você não está em um grupo.";
        public const string NaoPodeConvidar = "Só o líder do grupo convida.";

        public static string Recusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroSocial.AlvoNaoEncontrado:
                    return "Personagem não encontrado ou offline.";
                case CodigosErroSocial.AlvoJaEmGrupo:
                    return "Esse personagem já está em um grupo.";
                case CodigosErroSocial.ConviteParaSiMesmo:
                    return "Você não pode convidar a si mesmo.";
                case CodigosErroSocial.GrupoCheio:
                    return "O grupo está cheio.";
                case CodigosErroSocial.NaoELider:
                    return "Só o líder do grupo pode fazer isso.";
                case CodigosErroSocial.ConviteJaPendente:
                    return "Esse personagem já tem um convite seu pendente.";
                case CodigosErroSocial.AlvoComConvitesDemais:
                    return "Esse personagem tem convites demais pendentes.";
                case CodigosErroSocial.ConvitesRapidosDemais:
                    return "Convites rápidos demais. Espere um instante.";
                case CodigosErroSocial.ConviteNaoEncontrado:
                    return "Esse convite não existe mais.";
                case CodigosErroSocial.ConviteExpirado:
                    return "Esse convite expirou.";
                case CodigosErroSocial.ConvidanteOffline:
                    return "Quem convidou você está offline.";
                case CodigosErroSocial.NaoEstaEmGrupo:
                    return AvisoSemGrupo;
                case CodigosErroSocial.AlvoNaoEstaNoGrupo:
                    return "Esse personagem não está no seu grupo.";
                case CodigosErroSocial.AlvoInvalido:
                    return "Escolha outro membro do grupo.";
                case CodigosErroSocial.NaoPodeExpulsarLider:
                    return "O líder não pode ser expulso. Use Sair do grupo.";
                case CodigosErroSocial.ReconviteBloqueado:
                    return "Esse personagem foi expulso há pouco. Espere para convidar de novo.";
                case CodigosErroSocial.PapelInvalido:
                    return "Papel de grupo inválido.";
                case CodigosErroSocial.JaEstaNaFila:
                    return "Você já está na fila de grupo.";
                case CodigosErroSocial.NaoEstaNaFila:
                    return "Você não está na fila de grupo.";
                default:
                    return RecusaGenerica;
            }
        }

        /// <summary>O texto de um aviso do grupo. <paramref name="euMesmo"/> se o aviso é sobre o personagem local.</summary>
        public static string Aviso(AvisoDoGrupo aviso, bool euMesmo)
        {
            var nome = string.IsNullOrWhiteSpace(aviso.Nome) ? "Alguém" : aviso.Nome;
            switch (aviso.Tipo)
            {
                case TipoDeAvisoDoGrupo.ConviteRecusado:
                    return nome + " recusou o convite.";
                case TipoDeAvisoDoGrupo.ConviteExpirou:
                    return string.IsNullOrWhiteSpace(aviso.Nome)
                        ? "Um convite expirou."
                        : "O convite de " + aviso.Nome + " expirou.";
                case TipoDeAvisoDoGrupo.EntrouNoGrupo:
                    return euMesmo ? "Você entrou no grupo." : nome + " entrou no grupo.";
                case TipoDeAvisoDoGrupo.SaiuDoGrupo:
                    return euMesmo ? "Você saiu do grupo." : nome + " saiu do grupo.";
                case TipoDeAvisoDoGrupo.LiderMudou:
                    return euMesmo ? "Você é o novo líder." : nome + " é o novo líder.";
                case TipoDeAvisoDoGrupo.ExpulsoDoGrupo:
                    return euMesmo || string.IsNullOrWhiteSpace(aviso.Nome)
                        ? "Você foi expulso do grupo."
                        : nome + " foi expulso do grupo.";
                case TipoDeAvisoDoGrupo.GrupoDesfeito:
                    return "O grupo foi desfeito.";
                case TipoDeAvisoDoGrupo.MembroOffline:
                    return nome + " ficou offline.";
                case TipoDeAvisoDoGrupo.MembroVoltou:
                    return nome + " voltou.";
                default:
                    return null;
            }
        }
    }
}
