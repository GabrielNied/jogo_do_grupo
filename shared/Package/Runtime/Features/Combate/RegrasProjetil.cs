using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Fórmulas puras do sistema autoritativo de projéteis: velocidades, raio de colisão,
    /// tempo de vida e avanço de trajetória linear com detecção de impacto.
    /// </summary>
    public static class RegrasProjetil
    {
        // ── Balanceamento da Flecha ──────────────────────────────────────────────

        /// <summary>Velocidade linear da flecha disparada em ataque leve (m/s).</summary>
        public const float VelocidadeFlechaLeve = 35.0f;

        /// <summary>Velocidade linear da flecha disparada em ataque pesado (m/s).</summary>
        public const float VelocidadeFlechaPesada = 50.0f;

        /// <summary>Raio da cápsula/esfera de colisão da flecha em metros.</summary>
        public const float RaioFlecha = 0.25f;

        /// <summary>Alcance máximo do disparo de flecha leve (metros).</summary>
        public const float AlcanceMaximoFlechaLeve = 35.0f;

        /// <summary>Alcance máximo do disparo de flecha pesada (metros).</summary>
        public const float AlcanceMaximoFlechaPesada = 40.0f;

        // ── Balanceamento dos Projéteis Mágicos (Cajados) ─────────────────────────

        /// <summary>Velocidade linear da bola de fogo disparada em ataque leve (m/s).</summary>
        public const float VelocidadeBolaDeFogoLeve = 22.0f;

        /// <summary>Velocidade linear da bola de fogo disparada em ataque pesado (m/s).</summary>
        public const float VelocidadeBolaDeFogoPesada = 30.0f;

        /// <summary>Raio da esfera de colisão da bola de fogo em metros.</summary>
        public const float RaioBolaDeFogo = 0.45f;

        /// <summary>Alcance máximo da bola de fogo em ataque leve (metros).</summary>
        public const float AlcanceMaximoBolaDeFogoLeve = 28.0f;

        /// <summary>Alcance máximo da bola de fogo em ataque pesado (metros).</summary>
        public const float AlcanceMaximoBolaDeFogoPesada = 34.0f;

        /// <summary>Velocidade linear da esfera de gelo disparada em ataque leve (m/s).</summary>
        public const float VelocidadeEsferaDeGeloLeve = 25.0f;

        /// <summary>Velocidade linear da esfera de gelo disparada em ataque pesado (m/s).</summary>
        public const float VelocidadeEsferaDeGeloPesada = 32.0f;

        /// <summary>Raio da esfera de colisão do gelo em metros.</summary>
        public const float RaioEsferaDeGelo = 0.35f;

        /// <summary>Alcance máximo da esfera de gelo em ataque leve (metros).</summary>
        public const float AlcanceMaximoEsferaDeGeloLeve = 30.0f;

        /// <summary>Alcance máximo da esfera de gelo em ataque pesado (metros).</summary>
        public const float AlcanceMaximoEsferaDeGeloPesada = 36.0f;

        /// <summary>Velocidade linear do orbe de cura disparado em ataque leve (m/s).</summary>
        public const float VelocidadeOrbeDeCuraLeve = 20.0f;

        /// <summary>Velocidade linear do orbe de cura disparado em ataque pesado (m/s).</summary>
        public const float VelocidadeOrbeDeCuraPesada = 26.0f;

        /// <summary>Raio da esfera de colisão do orbe de cura em metros (mais permissivo para aliados).</summary>
        public const float RaioOrbeDeCura = 0.50f;

        /// <summary>Alcance máximo do orbe de cura em ataque leve (metros).</summary>
        public const float AlcanceMaximoOrbeDeCuraLeve = 26.0f;

        /// <summary>Alcance máximo do orbe de cura em ataque pesado (metros).</summary>
        public const float AlcanceMaximoOrbeDeCuraPesada = 32.0f;

        /// <summary>Altura padrão de spawn do projétil a partir do chão/pés do atacante (metros).</summary>
        public const float AlturaOrigemDisparo = 1.2f;

        /// <summary>Devolve a velocidade linear de acordo com o projétil e o tipo de ataque.</summary>
        public static float Velocidade(TipoDeProjetil tipoProjetil, TipoDeAtaque tipoAtaque)
        {
            switch (tipoProjetil)
            {
                case TipoDeProjetil.Flecha:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? VelocidadeFlechaPesada
                        : VelocidadeFlechaLeve;
                case TipoDeProjetil.BolaDeFogo:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? VelocidadeBolaDeFogoPesada
                        : VelocidadeBolaDeFogoLeve;
                case TipoDeProjetil.EsferaDeGelo:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? VelocidadeEsferaDeGeloPesada
                        : VelocidadeEsferaDeGeloLeve;
                case TipoDeProjetil.OrbeDeCura:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? VelocidadeOrbeDeCuraPesada
                        : VelocidadeOrbeDeCuraLeve;
                default:
                    return tipoAtaque == TipoDeAtaque.Pesado ? 30.0f : 20.0f;
            }
        }

        /// <summary>Devolve o raio de colisão do projétil em metros.</summary>
        public static float Raio(TipoDeProjetil tipoProjetil)
        {
            switch (tipoProjetil)
            {
                case TipoDeProjetil.Flecha:
                    return RaioFlecha;
                case TipoDeProjetil.BolaDeFogo:
                    return RaioBolaDeFogo;
                case TipoDeProjetil.EsferaDeGelo:
                    return RaioEsferaDeGelo;
                case TipoDeProjetil.OrbeDeCura:
                    return RaioOrbeDeCura;
                default:
                    return 0.35f;
            }
        }

        /// <summary>Devolve o alcance máximo em metros.</summary>
        public static float AlcanceMaximo(TipoDeProjetil tipoProjetil, TipoDeAtaque tipoAtaque)
        {
            switch (tipoProjetil)
            {
                case TipoDeProjetil.Flecha:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? AlcanceMaximoFlechaPesada
                        : AlcanceMaximoFlechaLeve;
                case TipoDeProjetil.BolaDeFogo:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? AlcanceMaximoBolaDeFogoPesada
                        : AlcanceMaximoBolaDeFogoLeve;
                case TipoDeProjetil.EsferaDeGelo:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? AlcanceMaximoEsferaDeGeloPesada
                        : AlcanceMaximoEsferaDeGeloLeve;
                case TipoDeProjetil.OrbeDeCura:
                    return tipoAtaque == TipoDeAtaque.Pesado
                        ? AlcanceMaximoOrbeDeCuraPesada
                        : AlcanceMaximoOrbeDeCuraLeve;
                default:
                    return 30.0f;
            }
        }

        /// <summary>Tempo máximo de vida do projétil em milissegundos antes de expirar.</summary>
        public static int TempoDeVidaMaximoMs(TipoDeProjetil tipoProjetil, TipoDeAtaque tipoAtaque)
        {
            float vel = Velocidade(tipoProjetil, tipoAtaque);
            if (vel <= 0.001f)
            {
                return 1000;
            }

            float alcance = AlcanceMaximo(tipoProjetil, tipoAtaque);
            return (int)Math.Ceiling((alcance / vel) * 1000.0f);
        }

        /// <summary>
        /// Converte a rotação horizontal DirecaoY em graus (0 = +Z, 90 = +X) em vetor unitário (dirX, dirY, dirZ).
        /// </summary>
        public static void DirecaoVetorial(
            float direcaoYGraus,
            out float dirX,
            out float dirY,
            out float dirZ
        )
        {
            double radianos = direcaoYGraus * Math.PI / 180.0;
            dirX = (float)Math.Sin(radianos);
            dirY = 0f;
            dirZ = (float)Math.Cos(radianos);
        }

        /// <summary>
        /// Avança a posição linear do projétil dado o deslocamento pelo delta time.
        /// </summary>
        public static void AvancarPosicao(
            float x,
            float y,
            float z,
            float dirX,
            float dirY,
            float dirZ,
            float velocidade,
            float dt,
            out float novoX,
            out float novoY,
            out float novoZ
        )
        {
            float passo = velocidade * dt;
            novoX = x + dirX * passo;
            novoY = y + dirY * passo;
            novoZ = z + dirZ * passo;
        }

        /// <summary>
        /// Testa se o segmento de linha entre (x0, z0) e (x1, z1) colide com um círculo centrado em (centroX, centroZ) com raioTotal.
        /// Evita tunelamento em altas velocidades ao testar o trajeto contínuo do projétil no quadro/tick.
        /// </summary>
        public static bool SegmentoColideComCirculo(
            double x0,
            double z0,
            double x1,
            double z1,
            double centroX,
            double centroZ,
            double raioTotal
        )
        {
            if (raioTotal <= 0.0)
            {
                return false;
            }

            double dx = x1 - x0;
            double dz = z1 - z0;
            double compQuadrado = dx * dx + dz * dz;

            if (compQuadrado <= 0.000001)
            {
                double distSq = (centroX - x0) * (centroX - x0) + (centroZ - z0) * (centroZ - z0);
                return distSq <= raioTotal * raioTotal;
            }

            double t = ((centroX - x0) * dx + (centroZ - z0) * dz) / compQuadrado;
            t = Math.Max(0.0, Math.Min(1.0, t));

            double maisProximoX = x0 + t * dx;
            double maisProximoZ = z0 + t * dz;

            double dCentroX = centroX - maisProximoX;
            double dCentroZ = centroZ - maisProximoZ;
            return (dCentroX * dCentroX + dCentroZ * dCentroZ) <= raioTotal * raioTotal;
        }

        /// <summary>
        /// Determina se o deslocamento do projétil colide com a cápsula/cilindro do alvo no chão (alvoY = 0).
        /// </summary>
        public static bool ColideComAlvo(
            double x0,
            double y0,
            double z0,
            double x1,
            double y1,
            double z1,
            double raioProjetil,
            double alvoX,
            double alvoZ,
            double raioAlvo,
            double alturaAlvo
        ) =>
            ColideComAlvo(
                x0,
                y0,
                z0,
                x1,
                y1,
                z1,
                raioProjetil,
                alvoX,
                0.0,
                alvoZ,
                raioAlvo,
                alturaAlvo
            );

        /// <summary>
        /// Determina se o deslocamento do projétil colide com a cápsula/cilindro do alvo (posição base no chão e altura).
        /// </summary>
        public static bool ColideComAlvo(
            double x0,
            double y0,
            double z0,
            double x1,
            double y1,
            double z1,
            double raioProjetil,
            double alvoX,
            double alvoY,
            double alvoZ,
            double raioAlvo,
            double alturaAlvo
        )
        {
            double projMinY = Math.Min(y0, y1) - raioProjetil;
            double projMaxY = Math.Max(y0, y1) + raioProjetil;
            double alvoMinY = alvoY;
            double alvoMaxY = alvoY + Math.Max(0.5, alturaAlvo);

            if (projMaxY < alvoMinY || projMinY > alvoMaxY)
            {
                return false;
            }

            return SegmentoColideComCirculo(
                x0,
                z0,
                x1,
                z1,
                alvoX,
                alvoZ,
                raioProjetil + Math.Max(0.0, raioAlvo)
            );
        }
    }
}
