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

        /// <summary>Identificador do Lobo de Presa.</summary>
        public const int TipoLoboDePresa = 3;

        /// <summary>Identificador do Corcel de Cristal.</summary>
        public const int TipoCorcelDeCristal = 4;

        /// <summary>Identificador do Corcel da Geada.</summary>
        public const int TipoCorcelDaGeada = 5;

        /// <summary>Identificador do Basalto Vivente.</summary>
        public const int TipoBasaltoVivente = 6;

        /// <summary>Identificador do Tempestário.</summary>
        public const int TipoTempestario = 7;

        /// <summary>Identificador da Mula de Carga.</summary>
        public const int TipoMulaDeCarga = 8;

        /// <summary>Tempo de espera / canalização para subir na montaria em segundos.</summary>
        public const float TempoInvocacaoSegundos = 1.5f;

        /// <summary>Todos os tipos exibidos no catálogo (tecla U), em ordem.</summary>
        public static readonly int[] TiposDoCatalogo =
        {
            TipoCastanho,
            TipoNoturno,
            TipoLoboDePresa,
            TipoCorcelDeCristal,
            TipoCorcelDaGeada,
            TipoBasaltoVivente,
            TipoTempestario,
            TipoMulaDeCarga,
        };

        public static bool TipoValido(int tipo)
        {
            for (var i = 0; i < TiposDoCatalogo.Length; i++)
            {
                if (TiposDoCatalogo[i] == tipo)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Retorna o tipo de montaria desbloqueado pelo item, ou 0 se não for item de montaria.</summary>
        public static int TipoMontariaDoItem(int itemId) =>
            itemId switch
            {
                CatalogoItens.RedeasDoCorcelNoturno => TipoNoturno,
                CatalogoItens.RedeasDoLoboDePresa => TipoLoboDePresa,
                CatalogoItens.RedeasDoCorcelDeCristal => TipoCorcelDeCristal,
                CatalogoItens.RedeasDoCorcelDaGeada => TipoCorcelDaGeada,
                CatalogoItens.RedeasDoBasaltoVivente => TipoBasaltoVivente,
                CatalogoItens.RedeasDoTempestario => TipoTempestario,
                CatalogoItens.RedeasDaMulaDeCarga => TipoMulaDeCarga,
                _ => 0,
            };

        /// <summary>Indica se a montaria já vem desbloqueada por padrão na conta.</summary>
        public static bool DesbloqueadaPorPadrao(int tipo) => tipo == TipoCastanho;

        /// <summary>Origem de obtenção descrita para exibição na coleção / catálogo.</summary>
        public static string ObterOrigem(int tipo) =>
            tipo switch
            {
                TipoNoturno => "Rédeas antigas de chefes (legado)",
                TipoLoboDePresa => "Drop raro da Presa-Velha (5%)",
                TipoCorcelDeCristal => "Drop raro do Guardião do Reflexo (5%)",
                TipoCorcelDaGeada => "Drop raro de chefe de neve (5%) — em breve",
                TipoBasaltoVivente => "Drop raro do Colosso Basáltico (5%) — em breve",
                TipoTempestario => "Drop raro do Ancião Trovejante (5%) — em breve",
                TipoMulaDeCarga => "Comerciante de Asterra",
                _ => "Montaria inicial de Asterra",
            };

        public static string ObterNome(int tipo) =>
            tipo switch
            {
                TipoNoturno => "Corcel Noturno",
                TipoLoboDePresa => "Lobo de Presa",
                TipoCorcelDeCristal => "Corcel de Cristal",
                TipoCorcelDaGeada => "Corcel da Geada",
                TipoBasaltoVivente => "Basalto Vivente",
                TipoTempestario => "Tempestário",
                TipoMulaDeCarga => "Mula de Carga",
                _ => "Corcel Castanho",
            };

        public static string ObterDescricao(int tipo) =>
            tipo switch
            {
                TipoNoturno =>
                    "Uma montaria lendária imbuída pelas energias místicas da noite. Pelagem ébano com detalhes luminescentes em azul e sela prateada.",
                TipoLoboDePresa =>
                    "Lobo envelhecido selado para cavalgar; pelagem grisalha e arreios reforçados para longas caçadas.",
                TipoCorcelDeCristal =>
                    "Corcel imbuído pelos cristais da Mina do Reflexo; detalhes translúcidos e sela escura.",
                TipoCorcelDaGeada =>
                    "Corcel de crina gelada e arreios grossos, feito para as neves de Asterra.",
                TipoBasaltoVivente =>
                    "Forma quadrúpede de rocha viva; lento no visual, mesma corrida das demais montarias.",
                TipoTempestario =>
                    "Montaria escura com detalhes elétricos; sem habilidade, só aparência.",
                TipoMulaDeCarga =>
                    "Companheiro rústico e barato para viagens curtas; mesma velocidade das demais.",
                _ =>
                    "Companheiro rústico e veloz para longas viagens pelas planícies e trilhas de Asterra. Pelagem castanha aconchegante com arreios de couro.",
            };

        public static string ObterRaridade(int tipo) =>
            tipo switch
            {
                TipoNoturno
                or TipoLoboDePresa
                or TipoCorcelDeCristal
                or TipoCorcelDaGeada
                or TipoBasaltoVivente
                or TipoTempestario => "Raro",
                TipoMulaDeCarga => "Comum",
                _ => "Comum",
            };
    }
}
