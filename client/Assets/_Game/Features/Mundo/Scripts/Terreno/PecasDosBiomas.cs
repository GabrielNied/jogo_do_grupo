using System.Collections.Generic;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>Uma árvore ou pedra do mundo: qual prefab, onde fica e de que tamanho.</summary>
    public readonly struct PecaDoBioma
    {
        public PecaDoBioma(
            string prefab,
            Bioma bioma,
            float x,
            float y,
            float z,
            float guinada,
            float escala
        )
        {
            Prefab = prefab;
            Bioma = bioma;
            X = x;
            Y = y;
            Z = z;
            Guinada = guinada;
            Escala = escala;
        }

        /// <summary>Nome do prefab em <c>Resources/Mundo/</c> (sem extensão).</summary>
        public string Prefab { get; }

        /// <summary>Bioma em que a peça está: decide a cor dela.</summary>
        public Bioma Bioma { get; }

        public float X { get; }

        /// <summary>Altura do chão no ponto, em metros.</summary>
        public float Y { get; }

        public float Z { get; }

        /// <summary>Rotação em torno do eixo Y, em graus.</summary>
        public float Guinada { get; }

        /// <summary>Tamanho em relação ao prefab (1 = como ele é).</summary>
        public float Escala { get; }
    }

    /// <summary>
    /// Árvores e pedras do mundo além do miolo de 1 km (o miolo continua com
    /// <see cref="LayoutDoHub.PropsFora"/>). Dados puros, espalhados por uma semente fixa: o
    /// resultado é sempre o mesmo. Cacto, pinheiro, rocha vulcânica, árvore de pântano e arbusto
    /// são malhas do próprio jogo (<see cref="VegetacaoProcedural"/>); campo, mata e colinas
    /// continuam com as árvores do Kenney, tingidas com <see cref="CoresDosBiomas.DasPecas"/>.
    /// </summary>
    public static class PecasDosBiomas
    {
        /// <summary>Lado da célula do sorteio, em metros: no máximo uma peça por célula.</summary>
        public const float Celula = 12f;

        /// <summary>O miolo vai até aqui (a maior entre |x| e |z|); daqui para fora é esta lista.</summary>
        public const float InicioDeFora = LayoutDoHub.MetadeDoTerreno - 12f;

        /// <summary>Folga até a borda do mundo, em metros.</summary>
        public const float FolgaDaBorda = 12f;

        /// <summary>Acima desta altura não nasce árvore na neve (só pedra).</summary>
        public const float LimiteDasArvoresNaNeve = 85f;

        /// <summary>Encosta mais íngreme em que cabe uma árvore (metros de subida por metro).</summary>
        public const float DecliveMaximoDeArvore = 0.6f;

        /// <summary>Encosta mais íngreme em que cabe uma pedra.</summary>
        public const float DecliveMaximoDePedra = 1.1f;

        private const uint Semente = 20261012u;

        // Lado das manchas de vegetação, em metros: cada mancha é mais cheia ou mais vazia.
        private const float Mancha = 150f;

        private static readonly string[] PecasDoCampo =
        {
            "Arvore_Redonda",
            "Arvore",
            "Arbusto",
            "Arbusto",
            "Pedra_Pequena",
        };

        private static readonly string[] PecasDaMata =
        {
            "Arvore",
            "Arvore_Alta",
            "Arvore_Alta",
            "Arvore_Torta",
            "Arvore_Redonda",
            "Arbusto",
        };

        private static readonly string[] PecasDasColinas =
        {
            "Pedra_Grande",
            "Pedra_Grande",
            "Pedra_Larga",
            "Pedra_Pequena",
            "Arvore_Torta",
            "Arvore",
        };

        // Deserto: pedra, cacto e árvore seca. Sem pinheiro.
        private static readonly string[] PecasDoDeserto =
        {
            "Pedra_Grande",
            "Pedra_Larga",
            "Pedra_Larga",
            "Pedra_Pequena",
            "Cacto",
            "Arvore_Seca",
        };

        // Neve: pinheiro até o limite de altitude, e pedra. Sem cacto.
        private static readonly string[] PecasDaNeve =
        {
            "Pinheiro",
            "Pinheiro",
            "Pinheiro",
            "Pedra_Grande",
            "Pedra_Larga",
        };

        // Lava: rocha vulcânica, sem árvore.
        private static readonly string[] PecasDaLava =
        {
            "Rocha_Vulcanica",
            "Rocha_Vulcanica",
            "Rocha_Vulcanica",
            "Pedra_Pequena",
        };

        // Pântano: árvore própria nas ilhas.
        private static readonly string[] PecasDoPantano =
        {
            "Arvore_Pantano",
            "Arvore_Pantano",
            "Arvore_Pantano",
            "Arvore_Torta",
            "Pedra_Pequena",
        };

        private static IReadOnlyList<PecaDoBioma> _todas;

        /// <summary>Todas as peças de fora do miolo. Calculadas na primeira vez que são pedidas.</summary>
        public static IReadOnlyList<PecaDoBioma> Todas
        {
            get
            {
                if (_todas == null)
                {
                    _todas = Gerar();
                }

                return _todas;
            }
        }

        /// <summary>Se o prefab é uma pedra (o resto é planta).</summary>
        public static bool EhPedra(string prefab)
        {
            return prefab.StartsWith("Pedra") || prefab.StartsWith("Rocha");
        }

        /// <summary>
        /// Chance de uma célula de <see cref="Celula"/> metros ter uma peça em
        /// <paramref name="bioma"/>, antes de contar as manchas: a mata e o pântano são cheios, o
        /// deserto e a lava são quase vazios.
        /// </summary>
        public static float Densidade(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Campo:
                    return 0.03f;
                case Bioma.Mata:
                    return 0.2f;
                case Bioma.Colinas:
                    return 0.08f;
                case Bioma.Deserto:
                    return 0.04f;
                case Bioma.Neve:
                    return 0.07f;
                case Bioma.Lava:
                    return 0.045f;
                case Bioma.Pantano:
                    return 0.13f;
                default:
                    return 0f;
            }
        }

        private static string[] PecasDe(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Campo:
                    return PecasDoCampo;
                case Bioma.Mata:
                    return PecasDaMata;
                case Bioma.Colinas:
                    return PecasDasColinas;
                case Bioma.Deserto:
                    return PecasDoDeserto;
                case Bioma.Neve:
                    return PecasDaNeve;
                case Bioma.Lava:
                    return PecasDaLava;
                default:
                    return PecasDoPantano;
            }
        }

        /// <summary>
        /// Espalha as peças de novo, do zero. O resultado é sempre igual ao de
        /// <see cref="Todas"/>; quem só quer a lista deve usar <see cref="Todas"/>.
        /// </summary>
        public static IReadOnlyList<PecaDoBioma> Gerar()
        {
            var lista = new List<PecaDoBioma>();
            var limite = MapaDoMundo.Metade - FolgaDaBorda;
            var celulas = Mathf.FloorToInt(2f * limite / Celula);

            for (var cj = 0; cj < celulas; cj++)
            {
                for (var ci = 0; ci < celulas; ci++)
                {
                    // Cada célula tem o seu sorteio: mudar um bioma não embaralha os outros.
                    var sorteio = new Sorteio(Semente, ci, cj);

                    // A peça fica nos 70% do meio da célula: duas vizinhas nunca se encostam.
                    var x = -limite + (ci + 0.15f + 0.7f * sorteio.Proximo()) * Celula;
                    var z = -limite + (cj + 0.15f + 0.7f * sorteio.Proximo()) * Celula;
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < InicioDeFora)
                    {
                        continue;
                    }

                    // O teste barato vem antes: a maioria das células fica vazia sem consultar
                    // o mapa.
                    var chance = sorteio.Proximo();
                    if (chance >= 0.2f * 1.8f)
                    {
                        continue;
                    }

                    var bioma = MapaDoMundo.BiomaEm(x, z);
                    if (chance >= Densidade(bioma) * DensidadeDaMancha(x, z))
                    {
                        continue;
                    }

                    var nomes = PecasDe(bioma);
                    var prefab = nomes[(int)(sorteio.Proximo() * nomes.Length) % nomes.Length];
                    var guinada = sorteio.Proximo() * 360f;
                    var tamanho = sorteio.Proximo();

                    var y = MapaDoMundo.AlturaEm(x, z);
                    if (!Cabe(prefab, bioma, x, y, z))
                    {
                        continue;
                    }

                    lista.Add(
                        new PecaDoBioma(prefab, bioma, x, y, z, guinada, Escala(prefab, tamanho))
                    );
                }
            }

            return lista;
        }

        // Fora da água e da lava, em chão que não seja íngreme demais, e sem árvore nos picos.
        private static bool Cabe(string prefab, Bioma bioma, float x, float y, float z)
        {
            if (y < MapaDoMundo.NivelDaAgua + 0.25f)
            {
                return false;
            }

            if (bioma == Bioma.Lava && y < MapaDoMundo.NivelDaLava + 2f)
            {
                return false;
            }

            var pedra = EhPedra(prefab);
            if (!pedra && bioma == Bioma.Neve && y > LimiteDasArvoresNaNeve)
            {
                return false;
            }

            return Declive(x, z) <= (pedra ? DecliveMaximoDePedra : DecliveMaximoDeArvore);
        }

        // Pedras variam mais de tamanho (até 2,6 vezes); árvores, de 0,9 a 1,5.
        private static float Escala(string prefab, float sorteado)
        {
            return EhPedra(prefab) ? 1f + 1.6f * sorteado * sorteado : 0.9f + 0.6f * sorteado;
        }

        // Inclinação do chão em volta do ponto, em metros de subida por metro andado.
        private static float Declive(float x, float z)
        {
            const float passo = 3f;
            var dx = MapaDoMundo.AlturaEm(x + passo, z) - MapaDoMundo.AlturaEm(x - passo, z);
            var dz = MapaDoMundo.AlturaEm(x, z + passo) - MapaDoMundo.AlturaEm(x, z - passo);
            return Mathf.Sqrt(dx * dx + dz * dz) / (2f * passo);
        }

        // De 0,2 a 1,8, igual em toda a mancha de 150 m: cria clareiras e bosques.
        private static float DensidadeDaMancha(float x, float z)
        {
            var mi = Mathf.FloorToInt(x / Mancha);
            var mj = Mathf.FloorToInt(z / Mancha);
            return 0.2f + 1.6f * new Sorteio(Semente + 7u, mi, mj).Proximo();
        }

        // Gerador congruencial linear próprio: o resultado não depende da plataforma.
        private struct Sorteio
        {
            private uint _estado;

            public Sorteio(uint semente, int a, int b)
            {
                unchecked
                {
                    _estado = semente + (uint)a * 374761393u + (uint)b * 668265263u;
                    _estado = (_estado ^ (_estado >> 13)) * 1274126177u;
                }
            }

            public float Proximo()
            {
                _estado = unchecked(_estado * 1664525u + 1013904223u);
                return (_estado >> 8) / 16777216f;
            }
        }
    }
}
