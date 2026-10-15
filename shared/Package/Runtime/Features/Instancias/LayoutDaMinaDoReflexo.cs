using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Desenho da Mina do Reflexo (issue #800): a porta no mundo aberto, na colina oeste, e os
    /// salões da instância. Os números são proposta a calibrar.
    /// </summary>
    /// <remarks>
    /// A instância é uma área isolada do servidor (<c>instancia:&lt;id&gt;</c>), mas as posições
    /// seguem as regras do mundo (limite e altura do chão em <c>MapaDoMundo</c>). Por isso os
    /// salões ficam num trecho plano e vazio do mundo, longe de tudo, e o chão deles é o próprio
    /// terreno: a altura dos inimigos e dos avisos do boss (<c>MapaDoMundo.AlturaEm</c>) continua
    /// certa. Paredes e teto são só do client. O eixo da dungeon corre para o leste (+X).
    /// </remarks>
    public static class LayoutDaMinaDoReflexo
    {
        /// <summary>Porta da dungeon na colina oeste, a uns 190 m do centro da cidade.</summary>
        public const float EntradaX = -190f;

        public const float EntradaZ = 30f;

        /// <summary>Raio em que o client mostra a interação de entrar (m).</summary>
        public const float RaioDeInteracaoMetros = 4f;

        /// <summary>Distância máxima da porta que o servidor aceita para pedir a entrada (m).</summary>
        public const float AlcanceDaEntradaNoServidorMetros = 6f;

        /// <summary>Quem está a até esta distância da porta entra junto com o líder (m).</summary>
        public const float DistanciaDosMembrosMetros = 10f;

        /// <summary>
        /// Início do eixo da dungeon (plano XZ), no canto sudoeste do deserto, a uns 850 m da
        /// estrada mais próxima. A planta de 3 km (#797) pôs a grande duna oeste no lugar antigo.
        /// </summary>
        public const float OrigemX = -1440f;

        public const float OrigemZ = -1420f;

        /// <summary>Onde o jogador nasce ao entrar, dentro da antecâmara (rumo ao leste).</summary>
        public const float SpawnX = OrigemX + 5f;

        public const float SpawnZ = OrigemZ;

        public const float SpawnRotacaoY = 90f;

        /// <summary>Centro do salão do boss, no fim do eixo.</summary>
        public const float BossCentroX = OrigemX + 176f;

        public const float BossCentroZ = OrigemZ;

        /// <summary>Quanto o teto fica acima do ponto mais alto do trecho (m).</summary>
        public const float AlturaDoTeto = 7f;

        /// <summary>
        /// Os trechos do eixo, da porta ao boss: antecâmara, corredor, salão 1, corredor, salão 2,
        /// corredor, salão 3, corredor e salão do boss.
        /// </summary>
        public static readonly Trecho[] Trechos =
        {
            new Trecho(TipoDeTrecho.Antecamara, 0f, 12f, 6f),
            new Trecho(TipoDeTrecho.Corredor, 12f, 32f, 2.5f),
            new Trecho(TipoDeTrecho.Salao, 32f, 54f, 10f),
            new Trecho(TipoDeTrecho.Corredor, 54f, 74f, 2.5f),
            new Trecho(TipoDeTrecho.Salao, 74f, 96f, 10f),
            new Trecho(TipoDeTrecho.Corredor, 96f, 116f, 2.5f),
            new Trecho(TipoDeTrecho.Salao, 116f, 138f, 10f),
            new Trecho(TipoDeTrecho.Corredor, 138f, 158f, 2.5f),
            new Trecho(TipoDeTrecho.SalaoDoChefe, 158f, 194f, 14f),
        };

        /// <summary>Altura do chão (o terreno) num ponto do mundo.</summary>
        public static float AlturaDoChao(float x, float z) => MapaDoMundo.AlturaEm(x, z);

        /// <summary>Altura do chão da porta no mundo aberto.</summary>
        public static float EntradaY => AlturaDoChao(EntradaX, EntradaZ);

        /// <summary>Altura em que o jogador nasce na antecâmara (pés um pouco acima do chão).</summary>
        public static float SpawnY => AlturaDoChao(SpawnX, SpawnZ) + 0.1f;

        /// <summary>Centro (X) de um trecho no mundo.</summary>
        public static float CentroX(Trecho trecho) => OrigemX + ((trecho.De + trecho.Ate) * 0.5f);

        /// <summary>Os salões com inimigos comuns, na ordem do eixo (1, 2 e 3).</summary>
        public static Trecho[] SaloesComuns()
        {
            var lista = new List<Trecho>();
            foreach (var trecho in Trechos)
            {
                if (trecho.Tipo == TipoDeTrecho.Salao)
                {
                    lista.Add(trecho);
                }
            }

            return lista.ToArray();
        }

        /// <summary>Distância horizontal entre dois pontos no plano XZ.</summary>
        public static float Distancia(float x, float z, float outroX, float outroZ)
        {
            var dx = x - outroX;
            var dz = z - outroZ;
            return (float)Math.Sqrt((dx * dx) + (dz * dz));
        }

        /// <summary>Se (x, z) está perto o bastante da porta para o client oferecer a interação.</summary>
        public static bool PertoDaEntrada(float x, float z) =>
            Distancia(x, z, EntradaX, EntradaZ) <= RaioDeInteracaoMetros;

        /// <summary>Tipo de um trecho do eixo.</summary>
        public enum TipoDeTrecho
        {
            Antecamara,
            Corredor,
            Salao,
            SalaoDoChefe,
        }

        /// <summary>Um trecho do eixo: do deslocamento <c>De</c> ao <c>Ate</c> (m) desde a origem.</summary>
        public readonly struct Trecho
        {
            public Trecho(TipoDeTrecho tipo, float de, float ate, float metadeDaLargura)
            {
                Tipo = tipo;
                De = de;
                Ate = ate;
                MetadeDaLargura = metadeDaLargura;
            }

            public TipoDeTrecho Tipo { get; }
            public float De { get; }
            public float Ate { get; }
            public float MetadeDaLargura { get; }
        }
    }
}
