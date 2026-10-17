using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Valores de balanceamento e regras puras de montarias (ADR 0017).
    /// </summary>
    public static class RegrasMontaria
    {
        /// <summary>Velocidade de corrida padrão quando montado (m/s).</summary>
        public const float VelocidadeMontariaPadrao = 9.0f;

        /// <summary>Identificador do tipo padrão da primeira montaria.</summary>
        public const int TipoPadrao = 1;

        /// <summary>Identificador do Corcel Castanho.</summary>
        public const int TipoCastanho = 1;

        /// <summary>Identificador do Corcel Noturno.</summary>
        public const int TipoNoturno = 2;

        /// <summary>Tempo de espera / canalização para subir na montaria em segundos.</summary>
        public const float TempoInvocacaoSegundos = 1.5f;

        public static bool TipoValido(int tipo) => tipo == TipoCastanho || tipo == TipoNoturno;

        /// <summary>Retorna o tipo de montaria desbloqueado pelo item, ou 0 se não for item de montaria.</summary>
        public static int TipoMontariaDoItem(int itemId) =>
            itemId switch
            {
                CatalogoItens.RedeasDoCorcelNoturno => TipoNoturno,
                _ => 0,
            };

        /// <summary>Indica se a montaria já vem desbloqueada por padrão na conta.</summary>
        public static bool DesbloqueadaPorPadrao(int tipo) => tipo == TipoCastanho;

        /// <summary>Origem de obtenção descrita para exibição na coleção / catálogo.</summary>
        public static string ObterOrigem(int tipo) =>
            tipo switch
            {
                TipoNoturno => "Drop raro de chefes (5%)",
                _ => "Montaria inicial de Asterra",
            };

        public static string ObterNome(int tipo) =>
            tipo switch
            {
                TipoNoturno => "Corcel Noturno",
                _ => "Corcel Castanho",
            };

        public static string ObterDescricao(int tipo) =>
            tipo switch
            {
                TipoNoturno =>
                    "Uma montaria lendária imbuída pelas energias místicas da noite. Pelagem ébano com detalhes luminescentes em azul e sela prateada.",
                _ =>
                    "Companheiro rústico e veloz para longas viagens pelas planícies e trilhas de Asterra. Pelagem castanha aconchegante com arreios de couro.",
            };

        public static string ObterRaridade(int tipo) =>
            tipo switch
            {
                TipoNoturno => "Raro",
                _ => "Comum",
            };
    }
}
