using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Cores do chão, da água, da lava e das peças em cada bioma. Contas puras (sem criar
    /// objetos): o <see cref="ConstrutorDoTerreno"/> pinta a textura do terreno com elas.
    /// </summary>
    public static class CoresDosBiomas
    {
        public static readonly Color Campo = new Color(0.55f, 0.68f, 0.34f);
        public static readonly Color Mata = new Color(0.2f, 0.38f, 0.18f);
        public static readonly Color Colinas = new Color(0.52f, 0.44f, 0.28f);
        public static readonly Color Deserto = new Color(0.86f, 0.74f, 0.48f);
        public static readonly Color Neve = new Color(0.93f, 0.95f, 0.98f);
        public static readonly Color Lava = new Color(0.2f, 0.16f, 0.16f);
        public static readonly Color Pantano = new Color(0.27f, 0.33f, 0.2f);

        /// <summary>Pedra exposta nas encostas íngremes e paredões rochosos.</summary>
        public static readonly Color Rocha = new Color(0.42f, 0.42f, 0.45f);

        /// <summary>Areia dourada das praias e margens calmas.</summary>
        public static readonly Color Praia = new Color(0.86f, 0.78f, 0.56f);

        /// <summary>Cascalho e seixos das margens de rio.</summary>
        public static readonly Color MargemDeRio = new Color(0.44f, 0.46f, 0.43f);

        /// <summary>Terra batida e cascalho da estrada conectando a cidade ao mundo.</summary>
        public static readonly Color Estrada = new Color(0.66f, 0.58f, 0.44f);

        /// <summary>Lama do fundo das poças do pântano.</summary>
        public static readonly Color Lodo = new Color(0.16f, 0.2f, 0.13f);

        /// <summary>Pedra em brasa na beira dos rios de lava.</summary>
        public static readonly Color Brasa = new Color(0.5f, 0.17f, 0.06f);

        /// <summary>Superfície da água do pântano.</summary>
        public static readonly Color Agua = new Color(0.13f, 0.22f, 0.17f);

        /// <summary>Superfície da lava.</summary>
        public static readonly Color LavaViva = new Color(1f, 0.42f, 0.08f);

        /// <summary>A partir desta inclinação (metros de subida por metro) a pedra começa a aparecer.</summary>
        public const float DecliveComPedra = 0.7f;

        /// <summary>A partir desta inclinação o chão é só pedra.</summary>
        public const float DecliveSoDePedra = 1.3f;

        /// <summary>Cor de base de <paramref name="bioma"/> (a cidade usa a do campo).</summary>
        public static Color Do(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Mata:
                    return Mata;
                case Bioma.Colinas:
                    return Colinas;
                case Bioma.Deserto:
                    return Deserto;
                case Bioma.Neve:
                    return Neve;
                case Bioma.Lava:
                    return Lava;
                case Bioma.Pantano:
                    return Pantano;
                default:
                    return Campo;
            }
        }

        /// <summary>
        /// Cor do chão num ponto: a mistura das cores dos biomas pelos pesos, com lodo no fundo
        /// das poças, brasa na beira da lava, pedra nas encostas, praias e conexão com a estrada.
        /// </summary>
        /// <param name="pesos">Quanto de cada bioma existe no ponto.</param>
        /// <param name="altura">Altura do chão, em metros.</param>
        /// <param name="declive">Inclinação, em metros de subida por metro andado.</param>
        /// <param name="x">Coordenada X do mundo (opcional).</param>
        /// <param name="z">Coordenada Z do mundo (opcional).</param>
        public static Color DoChao(
            PesosDosBiomas pesos,
            float altura,
            float declive,
            float x = 0f,
            float z = 0f
        )
        {
            var cor =
                Campo * pesos.Campo
                + Mata * pesos.Mata
                + Colinas * pesos.Colinas
                + Deserto * pesos.Deserto
                + Neve * pesos.Neve
                + Lava * pesos.Lava
                + Pantano * pesos.Pantano;

            // Variação micro-tonal sutil para profundidade orgânica
            if (x != 0f || z != 0f)
            {
                var micro =
                    (Mathf.Sin(x * 0.35f + z * 0.17f) + Mathf.Cos(x * 0.21f - z * 0.43f)) * 0.025f;
                cor = new Color(
                    Mathf.Clamp01(cor.r + micro),
                    Mathf.Clamp01(cor.g + micro * 0.8f),
                    Mathf.Clamp01(cor.b + micro * 0.5f),
                    1f
                );
            }

            // Praia nas margens de águas calmas (pântano entre nível da água e +1.2 m com declive suave)
            var praia =
                Mathf.InverseLerp(
                    MapaDoMundo.NivelDaAgua + 1.2f,
                    MapaDoMundo.NivelDaAgua + 0.1f,
                    altura
                ) * Mathf.InverseLerp(0.7f, 0.25f, declive);
            cor = Color.Lerp(cor, Praia, praia * pesos.Pantano * 0.85f);

            // Fundo das poças: escurece de 30 cm acima da água até 20 cm abaixo.
            var molhado = Mathf.InverseLerp(
                MapaDoMundo.NivelDaAgua + 0.3f,
                MapaDoMundo.NivelDaAgua - 0.2f,
                altura
            );
            cor = Color.Lerp(cor, Lodo, molhado * pesos.Pantano);

            // Beira dos rios de lava: em brasa até 2 m acima da lava.
            var quente = Mathf.InverseLerp(
                MapaDoMundo.NivelDaLava + 2f,
                MapaDoMundo.NivelDaLava,
                altura
            );
            cor = Color.Lerp(cor, Brasa, quente * pesos.Lava);

            // Encostas e paredões rochosos: a pedra aparece. Na lava o chão já é pedra escura.
            var pedra = Mathf.InverseLerp(DecliveComPedra, DecliveSoDePedra, declive);
            cor = Color.Lerp(cor, Rocha, pedra * (1f - pesos.Lava));

            // Estrada norte que sai do portão da cidade pelas transições 1 e 2
            if (
                Mathf.Abs(x) <= 8f
                && z >= MapaDoMundo.MetadeDaCidade
                && z <= MapaDoMundo.RaioTransicao2
            )
            {
                var lateral = 1f - Mathf.InverseLerp(4f, 8f, Mathf.Abs(x));
                var longitudinal =
                    1f
                    - Mathf.InverseLerp(MapaDoMundo.MetadeDaCidade, MapaDoMundo.RaioTransicao2, z)
                        * 0.4f;
                cor = Color.Lerp(cor, Estrada, lateral * longitudinal * 0.7f);
            }

            cor.a = 1f;
            return cor;
        }

        /// <summary>
        /// Cor que multiplica o material das peças (árvores e pedras) em <paramref name="bioma"/>:
        /// seca no deserto, fria na neve, queimada na lava e musgosa no pântano. Branco deixa a
        /// peça como ela é.
        /// </summary>
        public static Color DasPecas(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Deserto:
                    return new Color(1f, 0.88f, 0.66f);
                case Bioma.Neve:
                    return new Color(0.8f, 0.9f, 1f);
                case Bioma.Lava:
                    return new Color(0.34f, 0.29f, 0.29f);
                case Bioma.Pantano:
                    return new Color(0.62f, 0.74f, 0.52f);
                default:
                    return Color.white;
            }
        }
    }
}
