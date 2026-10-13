using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Monta o terreno do mundo em tempo de execução, a partir do <see cref="MapaDoMundo"/>: o
    /// chão com relevo (uma malha por bloco, com colisor), a água do pântano, a lava e as
    /// barreiras invisíveis da borda. Nada aqui decide regra: a altura e os biomas vêm do mapa
    /// compartilhado, o mesmo que o servidor usa.
    /// </summary>
    public static class ConstrutorDoTerreno
    {
        /// <summary>Células da grade por lado de cada bloco do chão (bloco de 200 m).</summary>
        public const int CelulasPorBloco = 20;

        /// <summary>Blocos por lado do mundo (15, ou seja, 225 blocos).</summary>
        public const int BlocosPorLado = MapaDoMundo.CelulasPorLado / CelulasPorBloco;

        /// <summary>Altura das barreiras da borda, acima do pico mais alto do mapa.</summary>
        public const float TopoDaBarreira = MapaDoMundo.AlturaMaxima + 40f;

        /// <summary>As barreiras descem abaixo do ponto mais baixo do mapa.</summary>
        public const float BaseDaBarreira = MapaDoMundo.AlturaMinima - 18f;

        public const string NomeBlocos = "Blocos";
        public const string NomeAgua = "Agua";
        public const string NomeLava = "Lava";
        public const string NomeFuroDaCaverna = "FuroDaCaverna";

        /// <summary>Lado dos quadrados do remendo que preenche o furo da caverna (m).</summary>
        public const float PassoDoFuro = 1f;

        private const int PontosPorBloco = CelulasPorBloco + 1;

        /// <summary>
        /// Cria o chão, a água, a lava e as barreiras como filhos de <paramref name="terreno"/>.
        /// O <paramref name="modelo"/> é o material copiado para o chão e para os líquidos (pode
        /// ser nulo: usa o material padrão do URP).
        /// </summary>
        public static void Construir(Transform terreno, Material modelo)
        {
            var amostras = AmostrasDoTerreno.Gerar();

            var chao = CriarMaterialDoChao(modelo);
            if (chao != null)
            {
                var textura = CriarTextura(amostras);
                chao.mainTexture = textura;
                if (chao.HasProperty("_BaseMap"))
                {
                    chao.SetTexture("_BaseMap", textura);
                }
            }

            var blocos = new GameObject(NomeBlocos).transform;
            blocos.SetParent(terreno, false);
            for (var bj = 0; bj < BlocosPorLado; bj++)
            {
                for (var bi = 0; bi < BlocosPorLado; bi++)
                {
                    var malha = CriarMalhaDoBloco(amostras, bi, bj);
                    var bloco = CriarObjetoComMalha($"Bloco_{bi}_{bj}", blocos, malha, chao);
                    bloco.AddComponent<MeshCollider>().sharedMesh = malha;
                }
            }

            // As células que a boca da caverna atravessa saem dos blocos e voltam num remendo
            // mais fino, com o túnel aberto (o mapa de alturas não tem como furar o chão).
            var remendo = CriarMalhaDoFuro();
            if (remendo != null)
            {
                var furo = CriarObjetoComMalha(NomeFuroDaCaverna, blocos, remendo, chao);
                furo.AddComponent<MeshCollider>().sharedMesh = remendo;
            }

            var agua = CriarMaterial(modelo, CoresDosBiomas.Agua, 0.85f);
            CriarLiquido(NomeAgua, terreno, CriarMalhaDoLiquido(amostras, false), agua);

            var lava = CriarMaterial(modelo, CoresDosBiomas.LavaViva, 0.3f);
            if (lava != null && lava.HasProperty("_EmissionColor"))
            {
                // A lava brilha no escuro.
                lava.EnableKeyword("_EMISSION");
                lava.SetColor("_EmissionColor", CoresDosBiomas.LavaViva * 1.6f);
            }

            CriarLiquido(NomeLava, terreno, CriarMalhaDoLiquido(amostras, true), lava);

            CriarBarreiras(terreno);
        }

        /// <summary>
        /// Malha do bloco (<paramref name="blocoI"/>, <paramref name="blocoJ"/>) do chão, já nas
        /// coordenadas do mundo. Cada célula são dois triângulos, divididos pela mesma diagonal
        /// que o <see cref="MapaDoMundo.AlturaEm"/> usa. As células sob a cidade ficam vazias: o
        /// chão de lá é o da própria cidade.
        /// </summary>
        public static Mesh CriarMalhaDoBloco(AmostrasDoTerreno amostras, int blocoI, int blocoJ)
        {
            var pontos = amostras.PontosPorLado;
            var vertices = new Vector3[PontosPorBloco * PontosPorBloco];
            var normais = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (var lj = 0; lj < PontosPorBloco; lj++)
            {
                for (var li = 0; li < PontosPorBloco; li++)
                {
                    var i = blocoI * CelulasPorBloco + li;
                    var j = blocoJ * CelulasPorBloco + lj;
                    var indice = lj * PontosPorBloco + li;
                    vertices[indice] = new Vector3(
                        MapaDoMundo.CoordenadaDaGrade(i),
                        amostras.Altura(i, j),
                        MapaDoMundo.CoordenadaDaGrade(j)
                    );
                    normais[indice] = amostras.Normal(i, j);

                    // O centro de cada pixel da textura cai em cima de um ponto da grade.
                    uvs[indice] = new Vector2((i + 0.5f) / pontos, (j + 0.5f) / pontos);
                }
            }

            var triangulos = new List<int>(CelulasPorBloco * CelulasPorBloco * 6);
            for (var lj = 0; lj < CelulasPorBloco; lj++)
            {
                for (var li = 0; li < CelulasPorBloco; li++)
                {
                    var i = blocoI * CelulasPorBloco + li;
                    var j = blocoJ * CelulasPorBloco + lj;
                    if (CelulaSobACidade(i, j) || CelulaComFuro(i, j))
                    {
                        continue;
                    }

                    var origem = lj * PontosPorBloco + li;
                    var leste = origem + 1;
                    var norte = origem + PontosPorBloco;
                    var oposto = norte + 1;

                    triangulos.Add(origem);
                    triangulos.Add(norte);
                    triangulos.Add(leste);

                    triangulos.Add(leste);
                    triangulos.Add(norte);
                    triangulos.Add(oposto);
                }
            }

            var malha = new Mesh { name = $"Chao_{blocoI}_{blocoJ}" };
            malha.vertices = vertices;
            malha.normals = normais;
            malha.uv = uvs;
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateBounds();
            return malha;
        }

        /// <summary>
        /// Malha plana da água do pântano ou, com <paramref name="lava"/>, da lava: um quadrado
        /// no nível do líquido em cada célula da grade que tem chão abaixo dele. Onde os dois
        /// biomas se misturam, vale o de maior peso.
        /// </summary>
        public static Mesh CriarMalhaDoLiquido(AmostrasDoTerreno amostras, bool lava)
        {
            var nivel = lava ? MapaDoMundo.NivelDaLava : MapaDoMundo.NivelDaAgua;
            var maisAlto = Mathf.Max(MapaDoMundo.NivelDaAgua, MapaDoMundo.NivelDaLava);
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();

            for (var j = 0; j < MapaDoMundo.CelulasPorLado; j++)
            {
                for (var i = 0; i < MapaDoMundo.CelulasPorLado; i++)
                {
                    var fundo = Mathf.Min(
                        Mathf.Min(amostras.Altura(i, j), amostras.Altura(i + 1, j)),
                        Mathf.Min(amostras.Altura(i, j + 1), amostras.Altura(i + 1, j + 1))
                    );
                    if (fundo >= maisAlto)
                    {
                        continue;
                    }

                    var x = MapaDoMundo.CoordenadaDaGrade(i);
                    var z = MapaDoMundo.CoordenadaDaGrade(j);
                    var meio = MapaDoMundo.PassoDaGrade / 2f;
                    var pesos = MapaDoMundo.PesosEm(x + meio, z + meio);
                    var ehLava = pesos.Lava >= 0.35f && pesos.Lava >= pesos.Pantano;
                    if (ehLava != lava || fundo >= nivel)
                    {
                        continue;
                    }

                    var primeiro = vertices.Count;
                    var passo = MapaDoMundo.PassoDaGrade;
                    vertices.Add(new Vector3(x, nivel, z));
                    vertices.Add(new Vector3(x, nivel, z + passo));
                    vertices.Add(new Vector3(x + passo, nivel, z));
                    vertices.Add(new Vector3(x + passo, nivel, z + passo));

                    triangulos.Add(primeiro);
                    triangulos.Add(primeiro + 1);
                    triangulos.Add(primeiro + 2);

                    triangulos.Add(primeiro + 2);
                    triangulos.Add(primeiro + 1);
                    triangulos.Add(primeiro + 3);
                }
            }

            var normais = new Vector3[vertices.Count];
            for (var k = 0; k < normais.Length; k++)
            {
                normais[k] = Vector3.up;
            }

            var malha = new Mesh { name = lava ? NomeLava : NomeAgua };
            malha.indexFormat = IndexFormat.UInt32;
            malha.SetVertices(vertices);
            malha.normals = normais;
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateBounds();
            return malha;
        }

        /// <summary>
        /// Textura com a cor do chão: um pixel por ponto da grade. Entre os pontos a placa de
        /// vídeo mistura as cores, e é isso que deixa as fronteiras dos biomas suaves.
        /// </summary>
        public static Texture2D CriarTextura(AmostrasDoTerreno amostras)
        {
            var lado = amostras.PontosPorLado;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                name = "CoresDoTerreno",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            textura.SetPixels32(amostras.Cores);
            textura.Apply(false, false);
            return textura;
        }

        /// <summary>Se a célula (<paramref name="i"/>, <paramref name="j"/>) fica sob o chão da cidade.</summary>
        public static bool CelulaSobACidade(int i, int j)
        {
            var meio = MapaDoMundo.PassoDaGrade / 2f;
            var x = MapaDoMundo.CoordenadaDaGrade(i) + meio;
            var z = MapaDoMundo.CoordenadaDaGrade(j) + meio;
            return Mathf.Abs(x) < MapaDoMundo.MetadeDaCidade
                && Mathf.Abs(z) < MapaDoMundo.MetadeDaCidade;
        }

        /// <summary>
        /// Se a célula (<paramref name="i"/>, <paramref name="j"/>) da grade tem algum ponto onde
        /// o terreno é furado pela caverna (<see cref="PrimeiraCaverna.FuraOTerreno"/>).
        /// </summary>
        public static bool CelulaComFuro(int i, int j)
        {
            var x0 = MapaDoMundo.CoordenadaDaGrade(i);
            var z0 = MapaDoMundo.CoordenadaDaGrade(j);
            PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
            var passo = MapaDoMundo.PassoDaGrade;
            if (x0 + passo < minX || x0 > maxX || z0 + passo < minZ || z0 > maxZ)
            {
                return false;
            }

            var quadrados = Mathf.RoundToInt(passo / PassoDoFuro);
            for (var qj = 0; qj < quadrados; qj++)
            {
                for (var qi = 0; qi < quadrados; qi++)
                {
                    if (
                        PrimeiraCaverna.FuraOTerreno(
                            x0 + (qi + 0.5f) * PassoDoFuro,
                            z0 + (qj + 0.5f) * PassoDoFuro
                        )
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Remendo do chão nas células furadas pela caverna: a mesma superfície do mapa, em
        /// quadrados de <see cref="PassoDoFuro"/>, sem os quadrados onde o túnel abre. Devolve
        /// <c>null</c> se nenhuma célula é furada.
        /// </summary>
        public static Mesh CriarMalhaDoFuro()
        {
            PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
            var passo = MapaDoMundo.PassoDaGrade;
            var primeiroI = Mathf.Max(0, Mathf.FloorToInt((minX + MapaDoMundo.Metade) / passo));
            var ultimoI = Mathf.Min(
                MapaDoMundo.CelulasPorLado - 1,
                Mathf.FloorToInt((maxX + MapaDoMundo.Metade) / passo)
            );
            var primeiroJ = Mathf.Max(0, Mathf.FloorToInt((minZ + MapaDoMundo.Metade) / passo));
            var ultimoJ = Mathf.Min(
                MapaDoMundo.CelulasPorLado - 1,
                Mathf.FloorToInt((maxZ + MapaDoMundo.Metade) / passo)
            );

            var quadrados = Mathf.RoundToInt(passo / PassoDoFuro);
            var pontos = quadrados + 1;
            var vertices = new List<Vector3>();
            var normais = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangulos = new List<int>();
            for (var j = primeiroJ; j <= ultimoJ; j++)
            {
                for (var i = primeiroI; i <= ultimoI; i++)
                {
                    if (!CelulaComFuro(i, j))
                    {
                        continue;
                    }

                    var x0 = MapaDoMundo.CoordenadaDaGrade(i);
                    var z0 = MapaDoMundo.CoordenadaDaGrade(j);
                    var primeiro = vertices.Count;
                    for (var lj = 0; lj < pontos; lj++)
                    {
                        for (var li = 0; li < pontos; li++)
                        {
                            var x = x0 + li * PassoDoFuro;
                            var z = z0 + lj * PassoDoFuro;
                            vertices.Add(new Vector3(x, MapaDoMundo.AlturaEm(x, z), z));
                            normais.Add(NormalDoRelevo(x, z));
                            uvs.Add(
                                new Vector2(
                                    ((x + MapaDoMundo.Metade) / passo + 0.5f)
                                        / MapaDoMundo.PontosPorLado,
                                    ((z + MapaDoMundo.Metade) / passo + 0.5f)
                                        / MapaDoMundo.PontosPorLado
                                )
                            );
                        }
                    }

                    for (var qj = 0; qj < quadrados; qj++)
                    {
                        for (var qi = 0; qi < quadrados; qi++)
                        {
                            if (
                                PrimeiraCaverna.FuraOTerreno(
                                    x0 + (qi + 0.5f) * PassoDoFuro,
                                    z0 + (qj + 0.5f) * PassoDoFuro
                                )
                            )
                            {
                                continue;
                            }

                            var origem = primeiro + qj * pontos + qi;
                            var leste = origem + 1;
                            var norte = origem + pontos;
                            var oposto = norte + 1;
                            triangulos.Add(origem);
                            triangulos.Add(norte);
                            triangulos.Add(leste);
                            triangulos.Add(leste);
                            triangulos.Add(norte);
                            triangulos.Add(oposto);
                        }
                    }
                }
            }

            if (vertices.Count == 0)
            {
                return null;
            }

            var malha = new Mesh { name = NomeFuroDaCaverna };
            malha.indexFormat = IndexFormat.UInt32;
            malha.SetVertices(vertices);
            malha.SetNormals(normais);
            malha.SetUVs(0, uvs);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateBounds();
            return malha;
        }

        // Normal do relevo por diferenças finitas em volta do ponto.
        private static Vector3 NormalDoRelevo(float x, float z)
        {
            var passo = PassoDoFuro;
            var dx = MapaDoMundo.AlturaEm(x - passo, z) - MapaDoMundo.AlturaEm(x + passo, z);
            var dz = MapaDoMundo.AlturaEm(x, z - passo) - MapaDoMundo.AlturaEm(x, z + passo);
            return new Vector3(dx, 2f * passo, dz).normalized;
        }

        private static void CriarLiquido(string nome, Transform pai, Mesh malha, Material material)
        {
            var liquido = CriarObjetoComMalha(nome, pai, malha, material);
            var renderizador = liquido.GetComponent<MeshRenderer>();
            renderizador.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static GameObject CriarObjetoComMalha(
            string nome,
            Transform pai,
            Mesh malha,
            Material material
        )
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(pai, false);
            objeto.AddComponent<MeshFilter>().sharedMesh = malha;
            var renderizador = objeto.AddComponent<MeshRenderer>();
            if (material != null)
            {
                renderizador.sharedMaterial = material;
            }

            return objeto;
        }

        // Cria o material do chão: se o shader dedicado TerrenoMundo existir, usa-o com
        // amostragem de alta definição em espaço mundial e projeção triplanar em paredões.
        // Se não existir, usa o Lit nativo do URP com o mapa de micro-detalhe 1024x1024.
        private static Material CriarMaterialDoChao(Material modelo)
        {
            var shaderTerreno = Shader.Find("JogoDoGrupo/TerrenoMundo");
            Material material;
            if (shaderTerreno != null)
            {
                material = new Material(shaderTerreno);
                material.SetTexture("_GrassTex", GeradorDeTexturasDoTerreno.ObterGrama());
                material.SetTexture("_DirtTex", GeradorDeTexturasDoTerreno.ObterTerra());
                material.SetTexture("_SandTex", GeradorDeTexturasDoTerreno.ObterAreia());
                material.SetTexture("_RockTex", GeradorDeTexturasDoTerreno.ObterRocha());
                material.SetTexture("_SnowTex", GeradorDeTexturasDoTerreno.ObterNeve());
                material.SetTexture("_DetailAlbedoMap", GeradorDeTexturasDoTerreno.ObterDetalhes());
                material.SetFloat("_DetailTiling", 0.2f);
            }
            else
            {
                material = CriarMaterial(modelo, Color.white, 0.05f);
                if (material != null && material.HasProperty("_DetailAlbedoMap"))
                {
                    material.EnableKeyword("_DETAIL_MULX2");
                    material.EnableKeyword("_DETAIL_SCALED");
                    material.SetTexture(
                        "_DetailAlbedoMap",
                        GeradorDeTexturasDoTerreno.ObterDetalhes()
                    );
                    material.SetFloat("_DetailAlbedoMapScale", 1.0f);
                    material.SetVector("_DetailAlbedoMap_ST", new Vector4(600f, 600f, 0f, 0f));
                }
            }

            return material;
        }

        // Copia o modelo (ou cria um material do URP, se não houver modelo) com a cor e o brilho
        // pedidos e sem textura.
        private static Material CriarMaterial(Material modelo, Color cor, float brilho)
        {
            Material material;
            if (modelo != null)
            {
                material = new Material(modelo);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    return null;
                }

                material = new Material(shader);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", cor);
            }

            material.color = cor;
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", brilho);
            }

            return material;
        }

        // Paredes invisíveis na borda do mundo, do fundo até acima do pico mais alto.
        private static void CriarBarreiras(Transform pai)
        {
            const float espessura = 1f;
            var lado = MapaDoMundo.Lado;
            var deslocamento = MapaDoMundo.Metade + espessura / 2f;
            var altura = TopoDaBarreira - BaseDaBarreira;
            var meio = (TopoDaBarreira + BaseDaBarreira) / 2f;
            var aoLongoDeX = new Vector3(lado + 2f * espessura, altura, espessura);
            var aoLongoDeZ = new Vector3(espessura, altura, lado);

            CriarBarreira(pai, "Barreira_Norte", new Vector3(0f, meio, deslocamento), aoLongoDeX);
            CriarBarreira(pai, "Barreira_Sul", new Vector3(0f, meio, -deslocamento), aoLongoDeX);
            CriarBarreira(pai, "Barreira_Oeste", new Vector3(-deslocamento, meio, 0f), aoLongoDeZ);
            CriarBarreira(pai, "Barreira_Leste", new Vector3(deslocamento, meio, 0f), aoLongoDeZ);
        }

        private static void CriarBarreira(
            Transform pai,
            string nome,
            Vector3 centro,
            Vector3 tamanho
        )
        {
            var barreira = new GameObject(nome);
            barreira.transform.SetParent(pai, false);
            barreira.transform.localPosition = centro;
            barreira.AddComponent<BoxCollider>().size = tamanho;
        }
    }
}
