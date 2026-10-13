using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Fórmulas puras da esquiva: custo, duração e deslocamento. Valores da proposta em
    /// docs/features/fatia-jogavel.md, seção 3. Nos primeiros 0,3 s o dano direto some;
    /// depois, a esquiva evita dano por tirar o personagem da área do golpe. O servidor é quem decide; o client usa para
    /// antecipar o movimento.
    /// </summary>
    public static class RegrasEsquiva
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Metros que a esquiva desloca, no máximo.</summary>
        public const double DistanciaMetros = 5.0;

        /// <summary>Segundos que a esquiva dura.</summary>
        public const double DuracaoSegundos = 0.6;

        /// <summary>Stamina gasta por esquiva; sem essa stamina ela é recusada.</summary>
        public const int CustoDeStamina = 15;

        /// <summary>
        /// Segundos, desde o começo da esquiva, em que o dano direto some. Depois disso o golpe
        /// acerta se a pessoa ainda está no volume.
        /// </summary>
        public const double JanelaDeInvulnerabilidadeSegundos = 0.3;

        // ── Regras ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Para onde a esquiva sai, em graus (0 aponta para +Z e cresce para +X): andando, na
        /// direção do movimento; parado, para trás da mira. O resultado fica entre 0 e 360.
        /// </summary>
        public static double DirecaoDaEsquiva(
            bool andando,
            double direcaoDoMovimentoGraus,
            double miraGraus
        )
        {
            double graus = (andando ? direcaoDoMovimentoGraus : miraGraus + 180.0) % 360.0;
            return graus < 0.0 ? graus + 360.0 : graus;
        }

        /// <summary>
        /// Se o dano some neste instante: nos primeiros
        /// <see cref="JanelaDeInvulnerabilidadeSegundos"/> da esquiva (o instante final da
        /// janela já leva dano).
        /// </summary>
        public static bool EstaInvulneravel(double segundosDesdeOInicio) =>
            segundosDesdeOInicio >= 0.0 && segundosDesdeOInicio < JanelaDeInvulnerabilidadeSegundos;

        /// <summary>Duração em milissegundos, como vai em <see cref="EsquivaIniciada.DuracaoMs"/>.</summary>
        public static int DuracaoMs => (int)Math.Round(DuracaoSegundos * 1000.0);

        /// <summary>Velocidade média durante a esquiva, em metros por segundo.</summary>
        public static double VelocidadeMetrosPorSegundo => DistanciaMetros / DuracaoSegundos;

        /// <summary>Se a stamina atual paga a esquiva.</summary>
        public static bool TemStamina(int staminaAtual) => staminaAtual >= CustoDeStamina;

        /// <summary>
        /// Se o instante ainda está dentro da esquiva. Recebe os segundos desde que ela começou;
        /// o último instante (<see cref="DuracaoSegundos"/>) ainda conta.
        /// </summary>
        public static bool EmAndamento(double segundosDesdeOInicio) =>
            segundosDesdeOInicio >= 0.0 && segundosDesdeOInicio <= DuracaoSegundos;

        /// <summary>
        /// Distância máxima, em metros, que o personagem pode ter percorrido por conta da esquiva
        /// depois de <paramref name="segundosDesdeOInicio"/>: cresce em linha reta até
        /// <see cref="DistanciaMetros"/> e para aí. Antes do início é zero.
        /// </summary>
        public static double DistanciaPermitidaAte(double segundosDesdeOInicio)
        {
            if (segundosDesdeOInicio <= 0.0)
            {
                return 0.0;
            }
            double percorrida = VelocidadeMetrosPorSegundo * segundosDesdeOInicio;
            return Math.Min(DistanciaMetros, percorrida);
        }

        /// <summary>
        /// Se um deslocamento total de <paramref name="distanciaMetros"/> cabe em uma esquiva
        /// (5 m cabe; 5,1 m não).
        /// </summary>
        public static bool DeslocamentoCabe(double distanciaMetros) =>
            distanciaMetros >= 0.0 && distanciaMetros <= DistanciaMetros;

        /// <summary>
        /// Onde a esquiva completa termina, no plano do chão, saindo de (x, z) na direção dada
        /// (0 grau aponta para +Z e cresce para +X). O servidor ainda confere os limites da área.
        /// </summary>
        public static void Destino(
            double x,
            double z,
            double direcaoYGraus,
            out double destinoX,
            out double destinoZ
        )
        {
            double radianos = direcaoYGraus * Math.PI / 180.0;
            destinoX = x + Math.Sin(radianos) * DistanciaMetros;
            destinoZ = z + Math.Cos(radianos) * DistanciaMetros;
        }
    }
}
