namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>
    /// Constantes e valores de balanceamento das missões (#179, #1061).
    /// </summary>
    public static class ConfigMissoes
    {
        public const int LimiteMissoesEmAndamento = 25;
        public const int MaximoMissoesAcompanhadas = 4;
        public const float RaioCompartilhamentoMetros = 30f;
        public const float RaioObjetivoEmGrupoMetros = 40f;
        public const int MissaoPrincipalInicialId = 1;
        public const float DistanciaInteracaoNpcMetros = 4f;

        /// <summary>Diferença de nível para considerar missão muito acima (indicador vermelho).</summary>
        public const int DiferencaNivelAlta = 3;

        /// <summary>Diferença de nível para considerar missão trivial (indicador cinza).</summary>
        public const int DiferencaNivelBaixa = 5;

        /// <summary>Quantidade de missões diárias sorteadas na rotação a cada ciclo (#1062).</summary>
        public const int QuantidadeDiariasPorDia = 3;

        /// <summary>Quantidade de missões semanais sorteadas na rotação a cada ciclo (#1062).</summary>
        public const int QuantidadeSemanaisPorSemana = 1;

        /// <summary>Deslocamento do fuso de Brasília em relação a UTC (-03:00).</summary>
        public static readonly System.TimeSpan FusoHorarioBrasilia = System.TimeSpan.FromHours(-3);

        /// <summary>Hora do reset diário no horário de Brasília (00:00).</summary>
        public const int HoraResetBrasilia = 0;

        /// <summary>Dia da semana do reset semanal (terça-feira, alinhado às raids e MMOs).</summary>
        public const System.DayOfWeek DiaResetSemanal = System.DayOfWeek.Tuesday;
    }
}
