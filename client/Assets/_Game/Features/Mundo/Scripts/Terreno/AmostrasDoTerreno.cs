using System.Collections.Generic;
using System.Threading;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// A grade do <see cref="MapaDoMundo"/> calculada uma vez: a altura e a cor do chão em cada
    /// ponto. O <see cref="ConstrutorDoTerreno"/> monta a malha e a textura a partir daqui, sem
    /// perguntar de novo ao mapa.
    /// </summary>
    public sealed class AmostrasDoTerreno
    {
        private readonly float[] _alturas;
        private readonly Color32[] _cores;
        private readonly Color32[] _pesosDoChao;

        private AmostrasDoTerreno(float[] alturas, Color32[] cores, Color32[] pesosDoChao)
        {
            _alturas = alturas;
            _cores = cores;
            _pesosDoChao = pesosDoChao;
        }

        /// <summary>Número de pontos por lado da grade.</summary>
        public int PontosPorLado => MapaDoMundo.PontosPorLado;

        /// <summary>
        /// Cores do chão, uma por ponto da grade, linha a linha a partir do sul (o formato que a
        /// textura espera).
        /// </summary>
        public Color32[] Cores => _cores;

        /// <summary>
        /// Pesos dos materiais do chão (<see cref="PesosDoTerreno.ParaTextura"/>), um por ponto
        /// da grade, no mesmo formato de <see cref="Cores"/>.
        /// </summary>
        public Color32[] PesosDoChao => _pesosDoChao;

        /// <summary>Calcula a grade inteira do mapa.</summary>
        public static AmostrasDoTerreno Gerar()
        {
            AmostrasDoTerreno pronta = null;
            foreach (var amostras in GerarEmEtapas())
            {
                pronta = amostras;
            }

            return pronta;
        }

        /// <summary>Calcula altura e cor em lotes para a entrada no mundo não congelar a tela.</summary>
        public static async Awaitable<AmostrasDoTerreno> GerarAsync(CancellationToken cancelar)
        {
            AmostrasDoTerreno pronta = null;
            foreach (var amostras in GerarEmEtapas())
            {
                pronta = amostras;
                cancelar.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(cancelar);
            }

            return pronta;
        }

        private static IEnumerable<AmostrasDoTerreno> GerarEmEtapas()
        {
            var lado = MapaDoMundo.PontosPorLado;
            var alturas = new float[lado * lado];
            var pesos = new PesosDosBiomas[lado * lado];
            for (var j = 0; j < lado; j++)
            {
                for (var i = 0; i < lado; i++)
                {
                    var indice = j * lado + i;
                    alturas[indice] = MapaDoMundo.AmostrarNaGrade(i, j, out pesos[indice]);
                }

                if ((j + 1) % 8 == 0)
                {
                    yield return null;
                }
            }

            var amostras = new AmostrasDoTerreno(
                alturas,
                new Color32[lado * lado],
                new Color32[lado * lado]
            );
            for (var j = 0; j < lado; j++)
            {
                var z = MapaDoMundo.CoordenadaDaGrade(j);
                for (var i = 0; i < lado; i++)
                {
                    var x = MapaDoMundo.CoordenadaDaGrade(i);
                    var indice = j * lado + i;
                    amostras._cores[indice] = CoresDosBiomas.DoChao(
                        pesos[indice],
                        alturas[indice],
                        amostras.Declive(i, j),
                        x,
                        z
                    );
                    amostras._pesosDoChao[indice] = PesosDoTerreno
                        .Calcular(pesos[indice], alturas[indice], x, z)
                        .ParaTextura();
                }

                if ((j + 1) % 8 == 0)
                {
                    yield return null;
                }
            }

            yield return amostras;
        }

        /// <summary>
        /// Altura do chão no ponto (<paramref name="i"/>, <paramref name="j"/>) da grade. Índices
        /// fora da grade valem os da borda.
        /// </summary>
        public float Altura(int i, int j)
        {
            var ultimo = PontosPorLado - 1;
            return _alturas[Mathf.Clamp(j, 0, ultimo) * PontosPorLado + Mathf.Clamp(i, 0, ultimo)];
        }

        /// <summary>Cor do chão no ponto (<paramref name="i"/>, <paramref name="j"/>) da grade.</summary>
        public Color32 Cor(int i, int j)
        {
            var ultimo = PontosPorLado - 1;
            return _cores[Mathf.Clamp(j, 0, ultimo) * PontosPorLado + Mathf.Clamp(i, 0, ultimo)];
        }

        /// <summary>
        /// Vetor que aponta para fora do chão no ponto da grade, medido pelos pontos vizinhos.
        /// Como não depende de qual bloco usa o ponto, não aparece emenda entre os blocos.
        /// </summary>
        public Vector3 Normal(int i, int j)
        {
            var dx = Altura(i + 1, j) - Altura(i - 1, j);
            var dz = Altura(i, j + 1) - Altura(i, j - 1);
            return new Vector3(-dx, 2f * MapaDoMundo.PassoDaGrade, -dz).normalized;
        }

        /// <summary>Inclinação no ponto da grade, em metros de subida por metro andado.</summary>
        public float Declive(int i, int j)
        {
            var dx = Altura(i + 1, j) - Altura(i - 1, j);
            var dz = Altura(i, j + 1) - Altura(i, j - 1);
            return Mathf.Sqrt(dx * dx + dz * dz) / (2f * MapaDoMundo.PassoDaGrade);
        }
    }
}
