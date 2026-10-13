using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>O círculo de aviso do golpe no chão, enquanto o preparo não acaba.</summary>
    internal sealed class AvisoDeGolpe
    {
        public AvisoDeGolpe(float centroX, float centroZ, float raio, float duracaoSegundos)
        {
            CentroX = centroX;
            CentroZ = centroZ;
            Raio = raio;
            DuracaoSegundos = duracaoSegundos;
            RestanteSegundos = duracaoSegundos;
        }

        public float CentroX { get; }
        public float CentroZ { get; }
        public float Raio { get; }
        public float DuracaoSegundos { get; }
        public float RestanteSegundos { get; set; }

        /// <summary>De 0 (acabou de começar) a 1 (o golpe cai).</summary>
        public float Progresso =>
            DuracaoSegundos <= 0f ? 1f : 1f - Mathf.Clamp01(RestanteSegundos / DuracaoSegundos);
    }

    /// <summary>
    /// O que o client sabe de um inimigo: o último estado que o servidor mandou e a pose
    /// suavizada que se mostra. Nada aqui decide dano, alvo ou morte.
    /// </summary>
    internal sealed class InimigoNoClient
    {
        /// <summary>Velocidade da suavização da pose (maior = acompanha o servidor mais de perto).</summary>
        public const float VelocidadeDeSuavizacao = 15f;

        public InimigoNoClient(InimigoNoMundo dados)
        {
            InimigoId = dados.InimigoId;
            CriaturaId = dados.CriaturaId;
            Variante = dados.Variante;
            Vida = dados.Vida;
            VidaMaxima = dados.VidaMaxima;
            Estado = dados.Estado;
            AlvoId = dados.AlvoId;
            AlvoX = X = dados.X;
            AlvoY = Y = dados.Y;
            AlvoZ = Z = dados.Z;
            AlvoRotacaoY = RotacaoY = dados.RotacaoY;
        }

        public long InimigoId { get; }
        public int CriaturaId { get; }
        public VarianteDeCriatura Variante { get; }
        public int Vida { get; set; }
        public int VidaMaxima { get; }
        public EstadoDeInimigo Estado { get; set; }
        public long AlvoId { get; set; }

        /// <summary>Última <c>Sequencia</c> aceita de <see cref="InimigoMudou"/> (0 = nenhuma ainda).</summary>
        public int UltimaSequencia { get; set; }

        /// <summary>Pose mostrada (vai atrás da pose do servidor).</summary>
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public float RotacaoY { get; private set; }

        /// <summary>Pose mais recente do servidor.</summary>
        public float AlvoX { get; set; }
        public float AlvoY { get; set; }
        public float AlvoZ { get; set; }
        public float AlvoRotacaoY { get; set; }

        public AvisoDeGolpe Aviso { get; set; }

        /// <summary>Quantos avisos de golpe já chegaram; cada um dispara a animação uma vez.</summary>
        public int GolpesPreparados { get; set; }

        /// <summary>Duração do preparo do último golpe, em segundos (zero antes do primeiro).</summary>
        public float PreparoDoGolpeSegundos { get; set; }

        /// <summary>Decide andar e atacar do modelo.</summary>
        public AnimacaoDoInimigo Animacao { get; } = new AnimacaoDoInimigo();

        /// <summary>Vida de 0 a 1; máximo zero não estoura.</summary>
        public float FracaoDeVida
        {
            get
            {
                if (VidaMaxima <= 0)
                {
                    return 0f;
                }

                return Mathf.Clamp01((float)Vida / VidaMaxima);
            }
        }

        public bool Fortalecido => Variante == VarianteDeCriatura.Fortalecida;

        public float Escala => RegrasCriaturas.Escala(Variante);

        /// <summary>Nome e nível do <see cref="CatalogoCriaturas"/>; criatura desconhecida cai no id.</summary>
        public string Rotulo
        {
            get
            {
                var ficha = CatalogoCriaturas.Obter(CriaturaId);
                return ficha == null ? "Criatura " + CriaturaId : ficha.Nome + " Nv " + ficha.Nivel;
            }
        }

        public void Atualizar(float segundos)
        {
            var fator = 1f - Mathf.Exp(-VelocidadeDeSuavizacao * segundos);
            var antesX = X;
            var antesZ = Z;
            X = Mathf.Lerp(X, AlvoX, fator);
            Y = Mathf.Lerp(Y, AlvoY, fator);
            Z = Mathf.Lerp(Z, AlvoZ, fator);
            RotacaoY = Mathf.LerpAngle(RotacaoY, AlvoRotacaoY, fator);
            var dx = X - antesX;
            var dz = Z - antesZ;
            Animacao.Atualizar(Estado, Mathf.Sqrt(dx * dx + dz * dz), segundos, GolpesPreparados);

            if (Aviso != null)
            {
                Aviso.RestanteSegundos -= segundos;
                if (Aviso.RestanteSegundos <= 0f)
                {
                    Aviso = null;
                }
            }
        }
    }
}
