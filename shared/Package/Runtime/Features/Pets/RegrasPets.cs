using System;

namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Onde o pet fica depois de um passo e o que ele fez.</summary>
    public readonly struct PassoDoPet
    {
        public readonly float X;
        public readonly float Z;
        public readonly float RotacaoY;
        public readonly bool Andando;

        /// <summary>O pet estava longe demais e foi colocado perto do dono.</summary>
        public readonly bool Alcancou;

        public PassoDoPet(float x, float z, float rotacaoY, bool andando, bool alcancou)
        {
            X = x;
            Z = z;
            RotacaoY = rotacaoY;
            Andando = andando;
            Alcancou = alcancou;
        }
    }

    /// <summary>
    /// Movimento do pet no plano do chão, sem engine nem rede (o servidor decide a posição e o
    /// client só mostra). Direção em graus: 0 aponta para +Z e cresce para +X.
    /// </summary>
    public static class RegrasPets
    {
        /// <summary>
        /// Folga para dar a chegada por feita. Sem ela, o erro de arredondamento deixa o pet a
        /// 2,0000002 m e ele nunca para.
        /// </summary>
        private const float ToleranciaDeChegadaMetros = 0.01f;

        /// <summary>Direção, em graus, de (0,0) até (dx,dz).</summary>
        public static float Direcao(float dx, float dz)
        {
            return (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
        }

        public static float Distancia(float ax, float az, float bx, float bz)
        {
            var dx = bx - ax;
            var dz = bz - az;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Onde o pet nasce ao ser chamado: na distância de seguida, atrás do dono (oposto ao
        /// lado para onde ele olha).
        /// </summary>
        public static void PontoDeChegada(
            float donoX,
            float donoZ,
            float donoRotacaoY,
            out float x,
            out float z
        )
        {
            var radianos = donoRotacaoY * Math.PI / 180.0;
            x = donoX - (float)Math.Sin(radianos) * ConfigPets.DistanciaDeSeguidaMetros;
            z = donoZ - (float)Math.Cos(radianos) * ConfigPets.DistanciaDeSeguidaMetros;
        }

        /// <summary>
        /// Um passo de seguir o dono. Passando de <see cref="ConfigPets.DistanciaParaAlcancarMetros"/>
        /// o pet vai para a distância de seguida, do lado em que estava. Senão anda até parar na
        /// distância de seguida; parado, só recomeça passando da folga.
        /// </summary>
        public static PassoDoPet Seguir(
            float petX,
            float petZ,
            float petRotacaoY,
            float donoX,
            float donoZ,
            bool andando,
            float dt
        )
        {
            var distancia = Distancia(petX, petZ, donoX, donoZ);
            if (distancia > ConfigPets.DistanciaParaAlcancarMetros)
            {
                var dx = (petX - donoX) / distancia;
                var dz = (petZ - donoZ) / distancia;
                return new PassoDoPet(
                    donoX + dx * ConfigPets.DistanciaDeSeguidaMetros,
                    donoZ + dz * ConfigPets.DistanciaDeSeguidaMetros,
                    Direcao(-dx, -dz),
                    false,
                    true
                );
            }

            var limite = andando
                ? ConfigPets.DistanciaDeSeguidaMetros
                : ConfigPets.DistanciaDeSeguidaMetros + ConfigPets.FolgaDeSeguidaMetros;
            if (distancia <= limite)
            {
                return new PassoDoPet(petX, petZ, petRotacaoY, false, false);
            }

            return IrAte(
                petX,
                petZ,
                petRotacaoY,
                donoX,
                donoZ,
                ConfigPets.DistanciaDeSeguidaMetros,
                dt
            );
        }

        /// <summary>
        /// Um passo em linha reta até o ponto, parando a <paramref name="distanciaParar"/> dele.
        /// Nunca passa do ponto de parada.
        /// </summary>
        public static PassoDoPet IrAte(
            float petX,
            float petZ,
            float petRotacaoY,
            float alvoX,
            float alvoZ,
            float distanciaParar,
            float dt
        )
        {
            var distancia = Distancia(petX, petZ, alvoX, alvoZ);
            var falta = distancia - distanciaParar;
            if (falta <= ToleranciaDeChegadaMetros || distancia <= 0f)
            {
                return new PassoDoPet(petX, petZ, petRotacaoY, false, false);
            }

            var anda = Math.Min(falta, ConfigPets.VelocidadeMetrosPorSegundo * dt);
            var dx = (alvoX - petX) / distancia;
            var dz = (alvoZ - petZ) / distancia;
            return new PassoDoPet(petX + dx * anda, petZ + dz * anda, Direcao(dx, dz), true, false);
        }
    }
}
