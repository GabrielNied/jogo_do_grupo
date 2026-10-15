using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Desenha a vegetação do mundo sem um objeto por peça: as peças ficam agrupadas em blocos
    /// de <see cref="ConfigVegetacao.LadoDoBloco"/> m, e cada bloco desenha, por instancing, uma
    /// chamada por (malha, material). Blocos longe da câmera não desenham, em duas faixas: as
    /// peças pequenas somem mais perto que as grandes. O colisor de cada peça é um objeto à
    /// parte (ver <see cref="MontadorDaVegetacao"/>).
    /// </summary>
    public sealed class BlocosDeVegetacao : MonoBehaviour
    {
        private sealed class Grupo
        {
            public Mesh Malha;
            public int Submalha;
            public Material Material;
            public bool Pequena;
            public readonly List<Matrix4x4> Matrizes = new List<Matrix4x4>();
            public Bounds Limites;
            public bool TemLimites;
        }

        private sealed class Bloco
        {
            public Bounds Limites;
            public bool TemLimites;
            public readonly Dictionary<(Mesh, int, Material, bool), Grupo> Grupos =
                new Dictionary<(Mesh, int, Material, bool), Grupo>();
        }

        private readonly Dictionary<(int, int), Bloco> _blocos =
            new Dictionary<(int, int), Bloco>();
        private readonly HashSet<Material> _materiais = new HashSet<Material>();
        private Matrix4x4[] _lote;

        /// <summary>Quantas instâncias (peça × parte) estão registradas.</summary>
        public int Instancias { get; private set; }

        /// <summary>Blocos com alguma peça.</summary>
        public int Blocos => _blocos.Count;

        /// <summary>Materiais distintos em uso.</summary>
        public int MateriaisDistintos => _materiais.Count;

        /// <summary>Chamadas de desenho por quadro se todos os blocos estivessem visíveis.</summary>
        public int GruposTotais
        {
            get
            {
                var total = 0;
                foreach (var bloco in _blocos.Values)
                {
                    total += bloco.Grupos.Count;
                }

                return total;
            }
        }

        /// <summary>
        /// Registra uma peça de <paramref name="modelo"/> em <paramref name="mundo"/> (do espaço
        /// da raiz da peça para o mundo), no bioma <paramref name="bioma"/>.
        /// </summary>
        public void Adicionar(ModeloDePeca modelo, Matrix4x4 mundo, Bioma bioma)
        {
            var posicao = mundo.GetColumn(3);
            var i = Mathf.FloorToInt(
                (posicao.x + MapaDoMundo.Metade) / ConfigVegetacao.LadoDoBloco
            );
            var j = Mathf.FloorToInt(
                (posicao.z + MapaDoMundo.Metade) / ConfigVegetacao.LadoDoBloco
            );
            if (!_blocos.TryGetValue((i, j), out var bloco))
            {
                bloco = new Bloco();
                _blocos[(i, j)] = bloco;
            }

            foreach (var parte in modelo.Partes)
            {
                var material = MateriaisDosBiomas.Para(parte.Material, bioma);
                if (material == null || parte.Malha == null)
                {
                    continue;
                }

                var chave = (parte.Malha, parte.Submalha, material, modelo.Pequena);
                if (!bloco.Grupos.TryGetValue(chave, out var grupo))
                {
                    grupo = new Grupo
                    {
                        Malha = parte.Malha,
                        Submalha = parte.Submalha,
                        Material = material,
                        Pequena = modelo.Pequena,
                    };
                    bloco.Grupos[chave] = grupo;
                    _materiais.Add(material);
                }

                var matriz = mundo * parte.Local;
                grupo.Matrizes.Add(matriz);
                Instancias++;

                var limites = parte.Malha.bounds;
                var centro = matriz.MultiplyPoint3x4(limites.center);
                var raio = limites.extents.magnitude * MaiorEscala(matriz) + 0.1f;
                var caixa = new Bounds(centro, Vector3.one * raio * 2f);
                if (grupo.TemLimites)
                {
                    grupo.Limites.Encapsulate(caixa);
                }
                else
                {
                    grupo.Limites = caixa;
                    grupo.TemLimites = true;
                }

                if (bloco.TemLimites)
                {
                    bloco.Limites.Encapsulate(caixa);
                }
                else
                {
                    bloco.Limites = caixa;
                    bloco.TemLimites = true;
                }
            }
        }

        /// <summary>Esvazia tudo (usado ao refazer o mundo).</summary>
        public void Limpar()
        {
            _blocos.Clear();
            _materiais.Clear();
            Instancias = 0;
        }

        /// <summary>
        /// Quantas chamadas de desenho saem a partir de <paramref name="origem"/> (a câmera), com
        /// o corte por distância; é o custo do quadro, sem desenhar nada.
        /// </summary>
        public int ContarChamadas(Vector3 origem)
        {
            var total = 0;
            foreach (var bloco in _blocos.Values)
            {
                foreach (var grupo in bloco.Grupos.Values)
                {
                    if (Visivel(bloco, grupo, origem))
                    {
                        total +=
                            (grupo.Matrizes.Count + ConfigVegetacao.InstanciasPorChamada - 1)
                            / ConfigVegetacao.InstanciasPorChamada;
                    }
                }
            }

            return total;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                Desenhar(camera.transform.position, null);
            }
        }

        /// <summary>
        /// Desenha os blocos perto de <paramref name="origem"/>. Com <paramref name="camera"/>
        /// nula vale para todas as câmeras (e para as sombras); com uma câmera, só para ela.
        /// </summary>
        public void Desenhar(Vector3 origem, Camera camera)
        {
            foreach (var bloco in _blocos.Values)
            {
                foreach (var grupo in bloco.Grupos.Values)
                {
                    if (!Visivel(bloco, grupo, origem))
                    {
                        continue;
                    }

                    var parametros = new RenderParams(grupo.Material)
                    {
                        camera = camera,
                        shadowCastingMode = ShadowCastingMode.On,
                        receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off,
                        worldBounds = grupo.Limites,
                    };
                    DesenharGrupo(grupo, parametros);
                }
            }
        }

        private void DesenharGrupo(Grupo grupo, RenderParams parametros)
        {
            var total = grupo.Matrizes.Count;
            if (_lote == null)
            {
                _lote = new Matrix4x4[ConfigVegetacao.InstanciasPorChamada];
            }

            for (var inicio = 0; inicio < total; inicio += ConfigVegetacao.InstanciasPorChamada)
            {
                var quantas = Mathf.Min(ConfigVegetacao.InstanciasPorChamada, total - inicio);
                grupo.Matrizes.CopyTo(inicio, _lote, 0, quantas);
                Graphics.RenderMeshInstanced(
                    parametros,
                    grupo.Malha,
                    grupo.Submalha,
                    _lote,
                    quantas
                );
            }
        }

        // Distância horizontal da origem até a caixa do bloco, contra o alcance da faixa do grupo.
        private static bool Visivel(Bloco bloco, Grupo grupo, Vector3 origem)
        {
            var alcance = grupo.Pequena
                ? ConfigVegetacao.DistanciaDasPecasPequenas
                : ConfigVegetacao.DistanciaDasPecasGrandes;
            var caixa = bloco.Limites;
            var dx = Mathf.Max(0f, Mathf.Max(caixa.min.x - origem.x, origem.x - caixa.max.x));
            var dz = Mathf.Max(0f, Mathf.Max(caixa.min.z - origem.z, origem.z - caixa.max.z));
            return dx * dx + dz * dz <= alcance * alcance;
        }

        private static float MaiorEscala(Matrix4x4 m)
        {
            var x = ((Vector3)m.GetColumn(0)).magnitude;
            var y = ((Vector3)m.GetColumn(1)).magnitude;
            var z = ((Vector3)m.GetColumn(2)).magnitude;
            return Mathf.Max(x, Mathf.Max(y, z));
        }
    }
}
