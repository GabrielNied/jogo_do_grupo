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
    }
}
