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

        // ── Chat, em MensagemDeChatRecusada.Codigo ────────────────────────────────
        // O chat também usa NaoEstaEmGrupo (falar em Grupo sem grupo) e AlvoNaoEncontrado
        // (sussurro para nome inexistente ou personagem offline).

        /// <summary>Texto vazio depois de tirar os espaços.</summary>
        public const string MensagemVazia = "MensagemVazia";

        /// <summary>Texto maior que o tamanho máximo.</summary>
        public const string MensagemLongaDemais = "MensagemLongaDemais";

        /// <summary>O personagem passou do limite de mensagens por janela de tempo.</summary>
        public const string MensagensRapidasDemais = "MensagensRapidasDemais";

        /// <summary>A mesma mensagem no mesmo canal em pouco tempo.</summary>
        public const string MensagemRepetida = "MensagemRepetida";

        /// <summary>O personagem está em silêncio automático.</summary>
        public const string Silenciado = "Silenciado";

        /// <summary>Falar em Cidade fora da cidade, ou em canal que não existe.</summary>
        public const string CanalIndisponivel = "CanalIndisponivel";

        /// <summary>Sussurro para o próprio personagem.</summary>
        public const string SussurroParaSiMesmo = "SussurroParaSiMesmo";

        // ── Amigos e ignorados, em OperacaoDeAmizadeRecusada.Codigo ───────────────
        // Também usam AlvoNaoEncontrado: nome que não existe, ou alvo que ignora quem pede (a
        // mesma resposta para os dois).

        /// <summary>Pedir amizade a um personagem da própria conta.</summary>
        public const string AmizadeConsigoMesmo = "AmizadeConsigoMesmo";

        /// <summary>Pedir amizade a quem está na sua lista de ignorados.</summary>
        public const string AlvoIgnorado = "AlvoIgnorado";

        /// <summary>Pedir amizade a quem já é amigo.</summary>
        public const string JaSaoAmigos = "JaSaoAmigos";

        /// <summary>Já existe um pedido seu, sem resposta, para o mesmo alvo.</summary>
        public const string PedidoJaPendente = "PedidoJaPendente";

        /// <summary>A conta passou do limite de frequência de pedidos de amizade.</summary>
        public const string PedidosRapidosDemais = "PedidosRapidosDemais";

        /// <summary>Amigos mais pedidos enviados já somam o máximo (ao pedir ou ao aceitar).</summary>
        public const string ListaDeAmigosCheia = "ListaDeAmigosCheia";

        /// <summary>O alvo já tem o máximo de pedidos de amizade sem resposta.</summary>
        public const string AlvoComPedidosDemais = "AlvoComPedidosDemais";

        /// <summary>Responder ou cancelar um pedido que não existe ou não é seu.</summary>
        public const string PedidoNaoEncontrado = "PedidoNaoEncontrado";

        /// <summary>Remover um amigo que não está na sua lista.</summary>
        public const string AmigoNaoEncontrado = "AmigoNaoEncontrado";

        /// <summary>Ignorar um personagem da própria conta.</summary>
        public const string IgnorarASiMesmo = "IgnorarASiMesmo";

        /// <summary>Ignorar quem já está na sua lista de ignorados.</summary>
        public const string JaIgnorado = "JaIgnorado";

        /// <summary>A lista de ignorados já tem o máximo de jogadores.</summary>
        public const string ListaDeIgnoradosCheia = "ListaDeIgnoradosCheia";

        /// <summary>Deixar de ignorar quem não está na sua lista de ignorados.</summary>
        public const string IgnoradoNaoEncontrado = "IgnoradoNaoEncontrado";

        // ── Fila de grupo / LFG ──────────────────────────────────────────────────

        /// <summary>Tentou entrar na fila com papel não reconhecido (não é Tank, Healer ou Dps).</summary>
        public const string PapelInvalido = "PapelInvalido";

        /// <summary>O personagem já está na fila de busca de grupo.</summary>
        public const string JaEstaNaFila = "JaEstaNaFila";

        /// <summary>Tentou sair da fila sem estar nela.</summary>
        public const string NaoEstaNaFila = "NaoEstaNaFila";

        // ── Guilda ───────────────────────────────────────────────────────────────

        /// <summary>A guilda pedida não existe ou foi desfeita.</summary>
        public const string GuildaNaoEncontrada = "GuildaNaoEncontrada";

        /// <summary>O personagem já pertence a uma guilda.</summary>
        public const string JaPossuiGuilda = "JaPossuiGuilda";

        /// <summary>O personagem não pertence a nenhuma guilda.</summary>
        public const string NaoPossuiGuilda = "NaoPossuiGuilda";

        /// <summary>Sinônimo para NaoPossuiGuilda.</summary>
        public const string NaoEstaEmGuilda = NaoPossuiGuilda;

        /// <summary>O alvo não pertence a esta guilda.</summary>
        public const string AlvoNaoEstaNaGuilda = "AlvoNaoEstaNaGuilda";

        /// <summary>Resposta a convite de guilda inexistente ou expirado.</summary>
        public const string ConviteInvalido = "ConviteInvalido";

        /// <summary>Nome de guilda inválido (tamanho ou caracteres proibidos).</summary>
        public const string NomeDeGuildaInvalido = "NomeDeGuildaInvalido";

        /// <summary>Já existe uma guilda com este nome (comparação sem diferenciar maiúsculas).</summary>
        public const string NomeDeGuildaEmUso = "NomeDeGuildaEmUso";

        /// <summary>Saldo de coroas insuficiente para fundar a guilda.</summary>
        public const string CoroasInsuficientes = "CoroasInsuficientes";

        /// <summary>A guilda já atingiu o limite de 100 membros.</summary>
        public const string GuildaCheia = "GuildaCheia";

        /// <summary>O cargo do executor não tem permissão para realizar esta ação.</summary>
        public const string SemPermissaoNaGuilda = "SemPermissaoNaGuilda";

        /// <summary>O alvo convidado já pertence a uma guilda.</summary>
        public const string AlvoJaPossuiGuilda = "AlvoJaPossuiGuilda";

        /// <summary>O líder não pode sair da guilda enquanto houver outros membros sem transferir a liderança.</summary>
        public const string LiderNaoPodeSair = "LiderNaoPodeSair";

        /// <summary>Cargo inválido para a operação solicitada.</summary>
        public const string CargoInvalido = "CargoInvalido";
    }
}
