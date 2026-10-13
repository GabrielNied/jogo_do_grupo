using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// O aviso do golpe do boss como malha no mundo: uma marca por área, que acompanha a altura
    /// do terreno (<see cref="MapaDoMundo.AlturaEm"/>), com cor pelo tipo do ataque. O contorno
    /// mostra a área inteira e o miolo cresce do centro até a borda: o golpe cai quando enche.
    /// </summary>
    internal sealed class AvisosNoChao
    {
        // Balanceamento visual (constantes nomeadas).
        public const int Lados = 40;
        public const int Aneis = 6;
        public const float ElevacaoDoContorno = 0.06f;
        public const float ElevacaoDoMiolo = 0.05f;
        public const float LarguraDoContornoMetros = 0.25f;

        public static readonly Color CorDaInvestida = new Color(0.91f, 0.58f, 0.51f, 0.9f);
        public static readonly Color CorDaPisada = new Color(0.95f, 0.65f, 0.25f, 0.9f);
        public static readonly Color CorDosRaios = new Color(0.91f, 0.71f, 0.84f, 0.9f);
        public const float OpacidadeDoMiolo = 0.4f;

        private readonly Transform _raiz;
        private readonly Material _material;
        private readonly Dictionary<long, List<Marca>> _marcas =
            new Dictionary<long, List<Marca>>();
        private readonly List<long> _paraRemover = new List<long>();

        public AvisosNoChao(Transform pai)
        {
            _raiz = new GameObject("Mobs.AvisosNoChao").transform;
            _raiz.SetParent(pai, false);
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            _material = new Material(shader) { renderQueue = 3000 };
            if (_material.HasProperty("_Cull"))
            {
                _material.SetFloat("_Cull", 0f);
            }
        }

        public static Color CorDoTipo(TipoDeAtaqueDeInimigo tipo)
        {
            switch (tipo)
            {
                case TipoDeAtaqueDeInimigo.Pisada:
                    return CorDaPisada;
                case TipoDeAtaqueDeInimigo.Raios:
                    return CorDosRaios;
                default:
                    return CorDaInvestida;
            }
        }

        /// <summary>Atualiza as marcas deste quadro: cria, anima e remove conforme os avisos.</summary>
        public void Mostrar(IReadOnlyCollection<InimigoNoClient> inimigos)
        {
            _paraRemover.Clear();
            foreach (var par in _marcas)
            {
                _paraRemover.Add(par.Key);
            }

            foreach (var inimigo in inimigos)
            {
                var aviso = inimigo.Aviso;
                if (aviso == null || aviso.Areas.Length == 0)
                {
                    continue;
                }

                _paraRemover.Remove(inimigo.InimigoId);
                if (!_marcas.TryGetValue(inimigo.InimigoId, out var lista))
                {
                    lista = new List<Marca>();
                    _marcas[inimigo.InimigoId] = lista;
                }

                while (lista.Count < aviso.Areas.Length)
                {
                    lista.Add(new Marca(_raiz, _material));
                }

                while (lista.Count > aviso.Areas.Length)
                {
                    lista[lista.Count - 1].Destruir();
                    lista.RemoveAt(lista.Count - 1);
                }

                var cor = CorDoTipo(aviso.Tipo);
                for (var i = 0; i < lista.Count; i++)
                {
                    lista[i].Desenhar(aviso.Areas[i], aviso.Progresso, cor);
                }
            }

            foreach (var id in _paraRemover)
            {
                foreach (var marca in _marcas[id])
                {
                    marca.Destruir();
                }

                _marcas.Remove(id);
            }
        }

        public void Destruir()
        {
            if (_raiz != null)
            {
                Object.Destroy(_raiz.gameObject);
            }

            if (_material != null)
            {
                Object.Destroy(_material);
            }
        }

        /// <summary>Anéis concêntricos de raio crescente, cada vértice na altura do terreno.</summary>
        public static void MontarDisco(
            Mesh malha,
            AreaDoAviso area,
            float raioInterno,
            float raioExterno,
            float elevacao
        )
        {
            var vertices = new Vector3[(Aneis + 1) * Lados];
            for (var a = 0; a <= Aneis; a++)
            {
                var raio = Mathf.Lerp(raioInterno, raioExterno, a / (float)Aneis);
                for (var l = 0; l < Lados; l++)
                {
                    var angulo = l * Mathf.PI * 2f / Lados;
                    var x = area.CentroX + Mathf.Sin(angulo) * raio;
                    var z = area.CentroZ + Mathf.Cos(angulo) * raio;
                    vertices[a * Lados + l] = new Vector3(
                        x,
                        MapaDoMundo.AlturaEm(x, z) + elevacao,
                        z
                    );
                }
            }

            var triangulos = new int[Aneis * Lados * 6];
            var t = 0;
            for (var a = 0; a < Aneis; a++)
            {
                for (var l = 0; l < Lados; l++)
                {
                    var proximo = (l + 1) % Lados;
                    var i0 = a * Lados + l;
                    var i1 = a * Lados + proximo;
                    var i2 = (a + 1) * Lados + l;
                    var i3 = (a + 1) * Lados + proximo;
                    triangulos[t++] = i0;
                    triangulos[t++] = i2;
                    triangulos[t++] = i1;
                    triangulos[t++] = i1;
                    triangulos[t++] = i2;
                    triangulos[t++] = i3;
                }
            }

            malha.Clear();
            malha.vertices = vertices;
            malha.triangles = triangulos;
        }

        private sealed class Marca
        {
            private readonly Mesh _contorno = new Mesh();
            private readonly Mesh _miolo = new Mesh();
            private readonly MeshRenderer _renderizadorContorno;
            private readonly MeshRenderer _renderizadorMiolo;
            private readonly GameObject _objeto;

            public Marca(Transform pai, Material material)
            {
                _objeto = new GameObject("Aviso");
                _objeto.transform.SetParent(pai, false);
                _renderizadorContorno = CriarParte("Contorno", _contorno, material);
                _renderizadorMiolo = CriarParte("Miolo", _miolo, material);
            }

            private MeshRenderer CriarParte(string nome, Mesh malha, Material material)
            {
                var parte = new GameObject(nome);
                parte.transform.SetParent(_objeto.transform, false);
                parte.AddComponent<MeshFilter>().sharedMesh = malha;
                var renderizador = parte.AddComponent<MeshRenderer>();
                renderizador.sharedMaterial = material;
                renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderizador.receiveShadows = false;
                return renderizador;
            }

            public void Desenhar(AreaDoAviso area, float progresso, Color cor)
            {
                var largura = Mathf.Min(LarguraDoContornoMetros, area.Raio);
                MontarDisco(_contorno, area, area.Raio - largura, area.Raio, ElevacaoDoContorno);
                Pintar(_contorno, cor);

                var raioDoMiolo = Mathf.Max(0.01f, (area.Raio - largura) * progresso);
                MontarDisco(_miolo, area, 0f, raioDoMiolo, ElevacaoDoMiolo);
                Pintar(_miolo, new Color(cor.r, cor.g, cor.b, OpacidadeDoMiolo));
            }

            private static void Pintar(Mesh malha, Color cor)
            {
                var cores = new Color[malha.vertexCount];
                for (var i = 0; i < cores.Length; i++)
                {
                    cores[i] = cor;
                }

                malha.colors = cores;
            }

            public void Destruir()
            {
                Object.Destroy(_contorno);
                Object.Destroy(_miolo);
                if (_objeto != null)
                {
                    Object.Destroy(_objeto);
                }
            }
        }
    }
}
