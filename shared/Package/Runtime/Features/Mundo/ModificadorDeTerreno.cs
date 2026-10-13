using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Tipo de intervenção ou adaptação de relevo no terreno.
    /// </summary>
    public enum TipoModificadorTerreno
    {
        Planar = 0,
        Cavar = 1,
        Elevar = 2,
    }

    /// <summary>
    /// Modificador de área que adapta o terreno para monumentos, cidades ou terraplanagem
    /// pelo jogador (estradas, praças, bases). Puro em C# e compartilhado entre Client e Servidor.
    /// </summary>
    public sealed class ModificadorDeTerreno
    {
        public string Id { get; }
        public TipoModificadorTerreno Tipo { get; }
        public float CentroX { get; }
        public float CentroZ { get; }
        public float RaioPlano { get; }
        public float RaioSuavizacao { get; }
        public float AlturaAlvo { get; }

        public ModificadorDeTerreno(
            string id,
            TipoModificadorTerreno tipo,
            float centroX,
            float centroZ,
            float raioPlano,
            float raioSuavizacao,
            float alturaAlvo
        )
        {
            Id = id ?? string.Empty;
            Tipo = tipo;
            CentroX = centroX;
            CentroZ = centroZ;
            RaioPlano = Math.Max(0f, raioPlano);
            RaioSuavizacao = Math.Max(0.01f, raioSuavizacao);
            AlturaAlvo = alturaAlvo;
        }

        public float RaioTotal => RaioPlano + RaioSuavizacao;

        /// <summary>Verifica se a coordenada está dentro da zona de influência deste modificador.</summary>
        public bool Afeta(float x, float z)
        {
            var dx = x - CentroX;
            var dz = z - CentroZ;
            var raioTotal = RaioTotal;
            return (dx * dx + dz * dz) <= (raioTotal * raioTotal);
        }

        /// <summary>
        /// Aplica a modificação de altura com transição suave Hermite (smoothstep).
        /// </summary>
        public float Aplicar(float x, float z, float alturaOriginal)
        {
            var dx = x - CentroX;
            var dz = z - CentroZ;
            var dist = (float)Math.Sqrt(dx * dx + dz * dz);
            var raioTotal = RaioTotal;

            if (dist > raioTotal)
            {
                return alturaOriginal;
            }

            if (dist <= RaioPlano)
            {
                return Tipo switch
                {
                    TipoModificadorTerreno.Planar => AlturaAlvo,
                    TipoModificadorTerreno.Cavar => Math.Min(alturaOriginal, AlturaAlvo),
                    TipoModificadorTerreno.Elevar => Math.Max(alturaOriginal, AlturaAlvo),
                    _ => AlturaAlvo,
                };
            }

            // Transição suave cúbica (Hermite) na borda externa
            var t = (dist - RaioPlano) / RaioSuavizacao;
            var pesoBorda = t * t * (3f - 2f * t); // 0 no RaioPlano, 1 no RaioTotal
            var pesoAlvo = 1f - pesoBorda;

            var alturaFinal = Tipo switch
            {
                TipoModificadorTerreno.Planar => AlturaAlvo * pesoAlvo + alturaOriginal * pesoBorda,
                TipoModificadorTerreno.Cavar => Math.Min(
                    alturaOriginal,
                    AlturaAlvo * pesoAlvo + alturaOriginal * pesoBorda
                ),
                TipoModificadorTerreno.Elevar => Math.Max(
                    alturaOriginal,
                    AlturaAlvo * pesoAlvo + alturaOriginal * pesoBorda
                ),
                _ => AlturaAlvo * pesoAlvo + alturaOriginal * pesoBorda,
            };

            return alturaFinal;
        }
    }
}
