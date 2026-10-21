using System;

namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>O que um <see cref="VolumeMarcado"/> significa para o movimento.</summary>
    public enum TipoDeVolume : byte
    {
        /// <summary>Corpo de água (lago, poça de dungeon): o volume vai do fundo à superfície.</summary>
        Agua = 1,

        /// <summary>Via de escalada: o volume cobre a parede e o espaço de quem escala.</summary>
        ViaDeEscalada = 2,
    }

    /// <summary>
    /// Um volume marcado no mundo para natação ou escalada: uma caixa com centro, tamanho e giro
    /// em Y. Nasce de um marcador na cena (<c>MarcadorDeVolume</c>, no client) e vai para
    /// <see cref="VolumesMarcados"/>, que é o que o servidor e o client conferem. Na via de
    /// escalada, a frente do volume (+Z local) aponta para a parede.
    /// </summary>
    [Serializable]
    public sealed class VolumeMarcado
    {
        public string Id = "";
        public TipoDeVolume Tipo = TipoDeVolume.Agua;
        public float CentroX = 0f;
        public float CentroY = 0f;
        public float CentroZ = 0f;
        public float TamanhoX = 1f;
        public float TamanhoY = 1f;
        public float TamanhoZ = 1f;

        /// <summary>Giro do volume em graus em torno de Y (convenção do Unity).</summary>
        public float RotacaoY = 0f;

        public VolumeMarcado() { }

        public VolumeMarcado(
            string id,
            TipoDeVolume tipo,
            float centroX,
            float centroY,
            float centroZ,
            float tamanhoX,
            float tamanhoY,
            float tamanhoZ,
            float rotacaoY = 0f
        )
        {
            Id = id;
            Tipo = tipo;
            CentroX = centroX;
            CentroY = centroY;
            CentroZ = centroZ;
            TamanhoX = tamanhoX;
            TamanhoY = tamanhoY;
            TamanhoZ = tamanhoZ;
            RotacaoY = rotacaoY;
        }

        /// <summary>Altura (Y) do fundo do volume.</summary>
        public float Fundo => CentroY - (TamanhoY / 2f);

        /// <summary>Altura (Y) do topo do volume (a superfície, na água).</summary>
        public float Topo => CentroY + (TamanhoY / 2f);

        /// <summary>
        /// Posição de um ponto no sistema do volume (lado = X local, frente = Z local), em metros
        /// a partir do centro, já desfeito o giro.
        /// </summary>
        public void ParaLocal(float x, float z, out float lado, out float frente)
        {
            var dx = x - CentroX;
            var dz = z - CentroZ;
            var rad = RotacaoY * (float)Math.PI / 180f;
            var cos = (float)Math.Cos(rad);
            var sen = (float)Math.Sin(rad);
            lado = (dx * cos) - (dz * sen);
            frente = (dx * sen) + (dz * cos);
        }

        /// <summary>Se o ponto está no volume, com uma folga (m) em todas as direções.</summary>
        public bool Contem(float x, float y, float z, float folga = 0f)
        {
            if (y < Fundo - folga || y > Topo + folga)
            {
                return false;
            }

            return ContemNoPlano(x, z, folga);
        }

        /// <summary>Se o ponto, visto de cima, está no volume (com folga em m).</summary>
        public bool ContemNoPlano(float x, float z, float folga = 0f)
        {
            ParaLocal(x, z, out var lado, out var frente);
            return Math.Abs(lado) <= (TamanhoX / 2f) + folga
                && Math.Abs(frente) <= (TamanhoZ / 2f) + folga;
        }

        /// <summary>Direção horizontal (X, Z) da frente do volume: para onde a parede está.</summary>
        public void Frente(out float x, out float z)
        {
            var rad = RotacaoY * (float)Math.PI / 180f;
            x = (float)Math.Sin(rad);
            z = (float)Math.Cos(rad);
        }
    }
}
