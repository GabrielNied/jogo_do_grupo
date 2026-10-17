namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Regras e constantes de balanceamento do clima no mundo (issue #902, docs/features/mundo.md).
    /// </summary>
    public static class ConfigClima
    {
        /// <summary>Velocidade normal sem penalidade de clima.</summary>
        public const float FatorVelocidadePadrao = 1.0f;

        /// <summary>Velocidade permitida sob nevasca intensa (70% da velocidade máxima de corrida).</summary>
        public const float FatorVelocidadeNevasca = 0.70f;

        /// <summary>Alcance de visão padrão sob tempo limpo (500m, limite da câmera).</summary>
        public const float DistanciaVisaoLimpo = 500f;

        /// <summary>Alcance de visão sob chuva (250m).</summary>
        public const float DistanciaVisaoChuva = 250f;

        /// <summary>Alcance de visão sob neblina densa (80m).</summary>
        public const float DistanciaVisaoNeblina = 80f;

        /// <summary>Alcance de visão sob nevasca fechada (60m).</summary>
        public const float DistanciaVisaoNevasca = 60f;

        /// <summary>Densidade da névoa exponencial sob tempo limpo.</summary>
        public const float DensidadeNevoaLimpo = 0.0016f;

        /// <summary>Densidade da névoa exponencial sob chuva.</summary>
        public const float DensidadeNevoaChuva = 0.005f;

        /// <summary>Densidade da névoa exponencial sob neblina fechada.</summary>
        public const float DensidadeNevoaNeblina = 0.020f;

        /// <summary>Densidade da névoa exponencial sob nevasca intensa.</summary>
        public const float DensidadeNevoaNevasca = 0.028f;

        /// <summary>Duração padrão de um período de clima (s): 5 minutos.</summary>
        public const int DuracaoPadraoClimaSegundos = 300;

        /// <summary>Duração mínima de sorteio de clima (s): 3 minutos.</summary>
        public const int DuracaoMinimaSegundos = 180;

        /// <summary>Duração máxima de sorteio de clima (s): 7 minutos.</summary>
        public const int DuracaoMaximaSegundos = 420;

        /// <summary>Intervalo de sincronização periódica de clima para clientes conectados (s).</summary>
        public const int IntervaloDeSincroniaSegundos = 30;

        /// <summary>Se o clima atual afeta o movimento no bioma especificado.</summary>
        public static bool AfetaMovimento(TipoDeClima clima, Bioma bioma) =>
            clima == TipoDeClima.Nevasca && bioma == Bioma.Neve;

        /// <summary>Se o clima atual afeta a visão no bioma especificado.</summary>
        public static bool AfetaVisao(TipoDeClima clima, Bioma bioma) =>
            clima switch
            {
                TipoDeClima.Chuva => bioma
                    is Bioma.Campo
                        or Bioma.Mata
                        or Bioma.Colinas
                        or Bioma.Cidade,
                TipoDeClima.Neblina => bioma is Bioma.Pantano or Bioma.Mata or Bioma.Colinas,
                TipoDeClima.Nevasca => bioma is Bioma.Neve,
                _ => false,
            };

        /// <summary>Fator de velocidade aplicado pelo clima no bioma informado.</summary>
        public static float ObterFatorVelocidade(TipoDeClima clima, Bioma bioma) =>
            AfetaMovimento(clima, bioma) ? FatorVelocidadeNevasca : FatorVelocidadePadrao;

        /// <summary>Fator de velocidade padrão do clima sem considerar bioma.</summary>
        public static float ObterFatorVelocidade(TipoDeClima clima) =>
            clima == TipoDeClima.Nevasca ? FatorVelocidadeNevasca : FatorVelocidadePadrao;

        /// <summary>Distância de visão do clima considerando o bioma informado.</summary>
        public static float ObterDistanciaVisao(TipoDeClima clima, Bioma bioma) =>
            AfetaVisao(clima, bioma) ? ObterDistanciaVisao(clima) : DistanciaVisaoLimpo;

        /// <summary>Distância de visão padrão do clima sem considerar bioma.</summary>
        public static float ObterDistanciaVisao(TipoDeClima clima) =>
            clima switch
            {
                TipoDeClima.Chuva => DistanciaVisaoChuva,
                TipoDeClima.Neblina => DistanciaVisaoNeblina,
                TipoDeClima.Nevasca => DistanciaVisaoNevasca,
                _ => DistanciaVisaoLimpo,
            };

        /// <summary>Densidade da névoa do clima considerando o bioma informado.</summary>
        public static float ObterDensidadeNevoa(TipoDeClima clima, Bioma bioma) =>
            AfetaVisao(clima, bioma) ? ObterDensidadeNevoa(clima) : DensidadeNevoaLimpo;

        /// <summary>Densidade da névoa padrão do clima sem considerar bioma.</summary>
        public static float ObterDensidadeNevoa(TipoDeClima clima) =>
            clima switch
            {
                TipoDeClima.Chuva => DensidadeNevoaChuva,
                TipoDeClima.Neblina => DensidadeNevoaNeblina,
                TipoDeClima.Nevasca => DensidadeNevoaNevasca,
                _ => DensidadeNevoaLimpo,
            };
    }
}
