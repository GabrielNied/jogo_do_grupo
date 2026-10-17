using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class TerrenoDoMundoTestes
    {
        private static AmostrasDoTerreno _amostras;

        private GameObject _hub;
        private AmbientMode _modo;
        private Color _ceu;
        private Color _horizonte;
        private Color _solo;
        private bool _nevoa;
        private FogMode _modoDaNevoa;
        private Color _corDaNevoa;
        private float _densidadeDaNevoa;

        // A grade inteira leva um instante para calcular: uma vez só para todos os testes.
        private static AmostrasDoTerreno Amostras
        {
            get
            {
                if (_amostras == null)
                {
                    _amostras = AmostrasDoTerreno.Gerar();
                }

                return _amostras;
            }
        }

        [SetUp]
        public void Preparar()
        {
            _modo = RenderSettings.ambientMode;
            _ceu = RenderSettings.ambientSkyColor;
            _horizonte = RenderSettings.ambientEquatorColor;
            _solo = RenderSettings.ambientGroundColor;
            _nevoa = RenderSettings.fog;
            _modoDaNevoa = RenderSettings.fogMode;
            _corDaNevoa = RenderSettings.fogColor;
            _densidadeDaNevoa = RenderSettings.fogDensity;
        }

        [TearDown]
        public void Limpar()
        {
            if (_hub != null)
            {
                Object.DestroyImmediate(_hub);
            }

            RenderSettings.ambientMode = _modo;
            RenderSettings.ambientSkyColor = _ceu;
            RenderSettings.ambientEquatorColor = _horizonte;
            RenderSettings.ambientGroundColor = _solo;
            RenderSettings.fog = _nevoa;
            RenderSettings.fogMode = _modoDaNevoa;
            RenderSettings.fogColor = _corDaNevoa;
            RenderSettings.fogDensity = _densidadeDaNevoa;
        }

        private Transform ConstruirTerreno()
        {
            _hub = ConstrutorHub.Montar();
            Physics.SyncTransforms();
            var terreno = _hub.transform.Find(ConstrutorHub.NomeTerreno);
            Assert.IsNotNull(terreno, "O hub deveria ter o terreno.");
            return terreno;
        }

        [Test]
        public void Mapa_do_mundo_e_layout_do_hub_concordam_nas_medidas()
        {
            Assert.AreEqual(LayoutDoHub.Metade, MapaDoMundo.MetadeDaCidade);
            Assert.Less(LayoutDoHub.MetadeDoTerreno, MapaDoMundo.Metade);

            // A estrada do portão termina dentro da faixa plana.
            var fimDaEstrada = LayoutDoHub.Metade + LayoutDoHub.ComprimentoDaEstradaFora;
            Assert.Less(fimDaEstrada, MapaDoMundo.RaioPlano);

            // A grade se divide em blocos inteiros, e a borda da cidade cai em pontos da grade.
            Assert.AreEqual(
                MapaDoMundo.CelulasPorLado,
                ConstrutorDoTerreno.BlocosPorLado * ConstrutorDoTerreno.CelulasPorBloco
            );
            Assert.AreEqual(0f, MapaDoMundo.MetadeDaCidade % MapaDoMundo.PassoDaGrade);
        }

        [Test]
        public void Amostras_trazem_as_alturas_do_mapa()
        {
            foreach (var (i, j) in new[] { (0, 0), (150, 150), (150, 270), (30, 150), (300, 300) })
            {
                Assert.AreEqual(MapaDoMundo.AlturaNaGrade(i, j), Amostras.Altura(i, j), 0.0001f);
            }

            // Fora da grade vale a borda.
            Assert.AreEqual(Amostras.Altura(0, 40), Amostras.Altura(-3, 40));
            Assert.AreEqual(Amostras.Altura(300, 40), Amostras.Altura(999, 40));

            // No plano em volta da cidade o chão aponta para cima e não tem inclinação.
            Assert.Less(Vector3.Distance(Vector3.up, Amostras.Normal(150, 150)), 0.0001f);
            Assert.AreEqual(0f, Amostras.Declive(150, 150));

            // Nas montanhas de neve há encosta.
            var maior = 0f;
            for (var i = 100; i <= 200; i += 5)
            {
                maior = Mathf.Max(maior, Amostras.Declive(i, 270));
            }

            Assert.Greater(maior, 0.2f);
        }

        [Test]
        public void Malha_do_bloco_segue_a_grade_do_mapa()
        {
            var malha = ConstrutorDoTerreno.CriarMalhaDoBloco(Amostras, 2, 12);
            try
            {
                var pontos = ConstrutorDoTerreno.CelulasPorBloco + 1;
                var vertices = malha.vertices;
                Assert.AreEqual(pontos * pontos, vertices.Length);
                Assert.AreEqual(
                    ConstrutorDoTerreno.CelulasPorBloco * ConstrutorDoTerreno.CelulasPorBloco * 6,
                    malha.triangles.Length
                );

                // O primeiro vértice é o canto sudoeste do bloco; o último, o nordeste.
                var i0 = 2 * ConstrutorDoTerreno.CelulasPorBloco;
                var j0 = 12 * ConstrutorDoTerreno.CelulasPorBloco;
                var primeiro = new Vector3(
                    MapaDoMundo.CoordenadaDaGrade(i0),
                    MapaDoMundo.AlturaNaGrade(i0, j0),
                    MapaDoMundo.CoordenadaDaGrade(j0)
                );
                var i1 = i0 + ConstrutorDoTerreno.CelulasPorBloco;
                var j1 = j0 + ConstrutorDoTerreno.CelulasPorBloco;
                var ultimo = new Vector3(
                    MapaDoMundo.CoordenadaDaGrade(i1),
                    MapaDoMundo.AlturaNaGrade(i1, j1),
                    MapaDoMundo.CoordenadaDaGrade(j1)
                );
                Assert.Less(Vector3.Distance(primeiro, vertices[0]), 0.001f);
                Assert.Less(Vector3.Distance(ultimo, vertices[vertices.Length - 1]), 0.001f);

                // Todos os triângulos olham para cima (senão o chão ficaria invisível de cima).
                var indices = malha.triangles;
                for (var t = 0; t < indices.Length; t += 3)
                {
                    var a = vertices[indices[t]];
                    var b = vertices[indices[t + 1]];
                    var c = vertices[indices[t + 2]];
                    Assert.Greater(Vector3.Cross(b - a, c - a).y, 0f, $"Triângulo {t / 3} virado.");
                }

                foreach (var normal in malha.normals)
                {
                    Assert.Greater(normal.y, 0f);
                    Assert.AreEqual(1f, normal.magnitude, 0.001f);
                }
            }
            finally
            {
                Object.DestroyImmediate(malha);
            }
        }

        [Test]
        public void Bloco_do_centro_fica_vazio_sob_a_cidade()
        {
            var centro = ConstrutorDoTerreno.BlocosPorLado / 2;
            var malha = ConstrutorDoTerreno.CriarMalhaDoBloco(Amostras, centro, centro);
            try
            {
                // A cidade tem 80 m: 8 x 8 células de 10 m a menos.
                var lado = ConstrutorDoTerreno.CelulasPorBloco;
                Assert.AreEqual((lado * lado - 64) * 6, malha.triangles.Length);
            }
            finally
            {
                Object.DestroyImmediate(malha);
            }

            Assert.IsTrue(ConstrutorDoTerreno.CelulaSobACidade(150, 150));
            Assert.IsTrue(ConstrutorDoTerreno.CelulaSobACidade(146, 153));
            Assert.IsFalse(ConstrutorDoTerreno.CelulaSobACidade(145, 150));
            Assert.IsFalse(ConstrutorDoTerreno.CelulaSobACidade(150, 154));
        }

        [Test]
        public void Chao_colide_na_altura_que_o_mapa_diz_em_todos_os_biomas()
        {
            var terreno = ConstruirTerreno();
            var chaoNativo = terreno.Find(TerrenoNativoDoMundo.Nome);
            Assert.IsNotNull(chaoNativo, "O Terrain nativo deveria existir.");
            var pontos = new[]
            {
                new Vector2(0f, 250f),
                new Vector2(250f, -120f),
                new Vector2(-250f, -120f),
                new Vector2(3.3f, 1203.7f),
                new Vector2(-411f, 1322f),
                new Vector2(-1204.5f, 8f),
                new Vector2(-905f, -333f),
                new Vector2(1201f, 302.5f),
                new Vector2(777.7f, 55.5f),
                new Vector2(1f, -1202f),
                new Vector2(444f, -1111f),
                new Vector2(1495f, 1495f),
                new Vector2(-1495f, -1495f),
            };

            foreach (var ponto in pontos)
            {
                var esperado = MapaDoMundo.AlturaEm(ponto.x, ponto.y);
                var origem = new Vector3(ponto.x, MapaDoMundo.AlturaMaxima + 20f, ponto.y);
                var noChao = Physics
                    .RaycastAll(origem, Vector3.down, MapaDoMundo.AlturaMaxima + 60f)
                    .Where(b => b.collider.transform.IsChildOf(chaoNativo))
                    .ToArray();

                Assert.AreEqual(1, noChao.Length, $"Chão em {ponto}: esperava uma batida só.");
                Assert.AreEqual(esperado, noChao[0].point.y, 0.05f, $"Altura do chão em {ponto}.");
            }
        }

        [Test]
        public void Agua_e_lava_ficam_no_nivel_e_so_onde_o_chao_afunda()
        {
            var terreno = ConstruirTerreno();
            ConferirLiquido(terreno, ConstrutorDoTerreno.NomeAgua, MapaDoMundo.NivelDaAgua, false);
            ConferirLiquido(terreno, ConstrutorDoTerreno.NomeLava, MapaDoMundo.NivelDaLava, true);
        }

        private static void ConferirLiquido(Transform terreno, string nome, float nivel, bool lava)
        {
            var liquido = terreno.Find(nome);
            Assert.IsNotNull(liquido, $"O terreno deveria ter '{nome}'.");
            Assert.IsEmpty(liquido.GetComponentsInChildren<Collider>(), "Líquido não tem colisor.");

            var vertices = liquido.GetComponent<MeshFilter>().sharedMesh.vertices;
            Assert.Greater(vertices.Length, 400, $"Pouco de '{nome}' no mundo.");
            Assert.AreEqual(0, vertices.Length % 4);

            var subPasso = MapaDoMundo.PassoDaGrade / 4f;
            var subMeio = subPasso / 2f;

            // Cada quadrado cobre uma sub-célula com chão abaixo do nível, no bioma certo.
            for (var k = 0; k < vertices.Length; k += 4 * 37)
            {
                var canto = vertices[k];
                Assert.AreEqual(nivel, canto.y, 0.0001f);

                var fundo = Mathf.Min(
                    Mathf.Min(
                        MapaDoMundo.AlturaEm(canto.x, canto.z),
                        MapaDoMundo.AlturaEm(canto.x + subPasso, canto.z)
                    ),
                    Mathf.Min(
                        MapaDoMundo.AlturaEm(canto.x, canto.z + subPasso),
                        MapaDoMundo.AlturaEm(canto.x + subPasso, canto.z + subPasso)
                    )
                );
                var centroChao = MapaDoMundo.AlturaEm(canto.x + subMeio, canto.z + subMeio);
                Assert.IsTrue(
                    fundo < nivel || centroChao < nivel,
                    $"'{nome}' em {canto} sem chão abaixo do nível."
                );

                var pesos = MapaDoMundo.PesosEm(canto.x + subMeio, canto.z + subMeio);
                var ehLava = pesos.Lava >= 0.35f && pesos.Lava >= pesos.Pantano;
                Assert.AreEqual(lava, ehLava, $"'{nome}' em {canto}.");
            }
        }

        [Test]
        public void Textura_do_chao_tem_a_cor_de_cada_bioma()
        {
            var textura = ConstrutorDoTerreno.CriarTextura(Amostras);
            try
            {
                Assert.AreEqual(MapaDoMundo.PontosPorLado, textura.width);
                Assert.AreEqual(MapaDoMundo.PontosPorLado, textura.height);

                // Coluna = X, linha = Z. Referências estáveis da planta com clareiras.
                ConferirCor(CoresDosBiomas.Campo, textura.GetPixel(160, 132), 0.1f);
                ConferirCor(CoresDosBiomas.Mata, textura.GetPixel(185, 125), 0.1f);
                ConferirCor(CoresDosBiomas.Colinas, textura.GetPixel(120, 168), 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        // Compara as cores com folga: contas com frações não dão o número exato.
        private static void ConferirCor(Color esperada, Color obtida, float folga = 0.002f)
        {
            Assert.AreEqual(esperada.r, obtida.r, folga, $"Vermelho de {obtida}.");
            Assert.AreEqual(esperada.g, obtida.g, folga, $"Verde de {obtida}.");
            Assert.AreEqual(esperada.b, obtida.b, folga, $"Azul de {obtida}.");
        }

        [Test]
        public void Cor_do_chao_mistura_os_biomas_e_mostra_pedra_lodo_e_brasa()
        {
            var neve = new PesosDosBiomas(0f, 0f, 0f, 0f, 1f, 0f, 0f);
            var lava = new PesosDosBiomas(0f, 0f, 0f, 0f, 0f, 1f, 0f);
            var pantano = new PesosDosBiomas(0f, 0f, 0f, 0f, 0f, 0f, 1f);
            var meioAMeio = new PesosDosBiomas(0.5f, 0f, 0f, 0.5f, 0f, 0f, 0f);

            ConferirCor(CoresDosBiomas.Neve, CoresDosBiomas.DoChao(neve, 50f, 0f));
            ConferirCor(CoresDosBiomas.Lava, CoresDosBiomas.DoChao(lava, 50f, 0f));
            ConferirCor(CoresDosBiomas.Pantano, CoresDosBiomas.DoChao(pantano, 1f, 0f));
            ConferirCor(
                Color.Lerp(CoresDosBiomas.Campo, CoresDosBiomas.Deserto, 0.5f),
                CoresDosBiomas.DoChao(meioAMeio, 5f, 0f)
            );

            // Paredão na neve é pedra; fundo de poça é lodo; beira da lava é brasa.
            var paredao = CoresDosBiomas.DecliveSoDePedra + 0.1f;
            var fundoDaPoca = MapaDoMundo.NivelDaAgua - 0.3f;
            var dentroDaLava = MapaDoMundo.NivelDaLava - 0.2f;
            ConferirCor(CoresDosBiomas.Rocha, CoresDosBiomas.DoChao(neve, 50f, paredao));
            ConferirCor(CoresDosBiomas.Lodo, CoresDosBiomas.DoChao(pantano, fundoDaPoca, 0f));
            ConferirCor(CoresDosBiomas.Brasa, CoresDosBiomas.DoChao(lava, dentroDaLava, 0f));

            // Paredão na lava continua escuro (não vira pedra cinza).
            ConferirCor(CoresDosBiomas.Lava, CoresDosBiomas.DoChao(lava, 50f, paredao));

            foreach (Bioma bioma in System.Enum.GetValues(typeof(Bioma)))
            {
                Assert.AreEqual(1f, CoresDosBiomas.Do(bioma).a);
                Assert.AreEqual(1f, CoresDosBiomas.DasPecas(bioma).a);
            }
        }

        [Test]
        public void Pecas_dos_biomas_ficam_fora_do_miolo_apoiadas_no_chao()
        {
            var pecas = PecasDosBiomas.Todas;
            Assert.That(pecas.Count, Is.InRange(1500, 6000));

            var limite = MapaDoMundo.Metade - PecasDosBiomas.FolgaDaBorda;
            foreach (var peca in pecas)
            {
                var alcance = Mathf.Max(Mathf.Abs(peca.X), Mathf.Abs(peca.Z));
                if (alcance < PecasDosBiomas.InicioDeFora)
                {
                    Assert.Greater(MapaDoMundo.CoberturaDaAutoriaEm(peca.X, peca.Z), 0f);
                }
                Assert.LessOrEqual(alcance, limite);
                Assert.AreEqual(MapaDoMundo.AlturaEm(peca.X, peca.Z), peca.Y, 0.001f);
                Assert.AreEqual(MapaDoMundo.BiomaEm(peca.X, peca.Z), peca.Bioma);
                Assert.AreNotEqual(Bioma.Cidade, peca.Bioma);
                Assert.GreaterOrEqual(peca.Y, MapaDoMundo.NivelDaAgua, "Peça dentro da água.");
                var escalaMaxima =
                    peca.Bioma == Bioma.Mata
                    && MapaDoMundo.CoberturaDaAutoriaEm(peca.X, peca.Z) > 0f
                        ? 1.5f * ConfigVegetacao.EscalaDaArvoreNaMataAutoral
                    : peca.Prefab == "Cacto" ? 2.7f
                    : 2.6f;
                Assert.That(peca.Escala, Is.InRange(0.9f, escalaMaxima));

                if (peca.Bioma == Bioma.Lava)
                {
                    Assert.Greater(peca.Y, MapaDoMundo.NivelDaLava + 1f, "Peça dentro da lava.");
                }

                if (peca.Bioma == Bioma.Neve && !PecasDosBiomas.EhPedra(peca.Prefab))
                {
                    Assert.LessOrEqual(peca.Y, PecasDosBiomas.LimiteDasArvoresNaNeve);
                }
            }
        }

        [Test]
        public void Cada_bioma_de_fora_tem_as_suas_pecas()
        {
            var pecas = PecasDosBiomas.Todas;
            foreach (var bioma in new[] { Bioma.Deserto, Bioma.Neve, Bioma.Lava, Bioma.Pantano })
            {
                var quantas = pecas.Count(p => p.Bioma == bioma);
                Assert.GreaterOrEqual(quantas, 200, $"Poucas em {bioma}.");
            }

            // O deserto agora tem cactos; lava fica rochosa e o pântano tem árvores.
            Assert.Greater(Pedras(Bioma.Lava), Arvores(Bioma.Lava));
            Assert.Greater(Arvores(Bioma.Pantano), Pedras(Bioma.Pantano));
            Assert.Greater(Arvores(Bioma.Neve), 100);

            Assert.Greater(pecas.Count(p => p.Bioma == Bioma.Deserto && p.Prefab == "Cacto"), 0);
            Assert.AreEqual(0, pecas.Count(p => p.Bioma == Bioma.Neve && p.Prefab == "Cacto"));
            Assert.Greater(pecas.Count(p => p.Bioma == Bioma.Neve && p.Prefab == "Pinheiro"), 0);
            Assert.AreEqual(
                0,
                pecas.Count(p => p.Bioma == Bioma.Deserto && p.Prefab == "Pinheiro")
            );
        }

        private static int Pedras(Bioma bioma)
        {
            return PecasDosBiomas.Todas.Count(p =>
                p.Bioma == bioma && PecasDosBiomas.EhPedra(p.Prefab)
            );
        }

        private static int Arvores(Bioma bioma)
        {
            return PecasDosBiomas.Todas.Count(p =>
                p.Bioma == bioma && !PecasDosBiomas.EhPedra(p.Prefab)
            );
        }

        [Test]
        public void Pecas_saem_sempre_iguais_e_nao_se_encostam()
        {
            var pecas = PecasDosBiomas.Todas;
            var deNovo = PecasDosBiomas.Gerar();
            Assert.AreEqual(pecas.Count, deNovo.Count);
            for (var k = 0; k < pecas.Count; k += 53)
            {
                Assert.AreEqual(pecas[k].Prefab, deNovo[k].Prefab);
                Assert.AreEqual(pecas[k].X, deNovo[k].X);
                Assert.AreEqual(pecas[k].Z, deNovo[k].Z);
                Assert.AreEqual(pecas[k].Guinada, deNovo[k].Guinada);
            }

            // A camada adicional da mata mantém distância entre troncos e demais peças.
            var minima = 0.3f * PecasDosBiomas.Celula - 0.01f;
            for (var a = 0; a < pecas.Count; a++)
            {
                for (var b = a + 1; b < pecas.Count; b++)
                {
                    var dx = pecas[a].X - pecas[b].X;
                    var dz = pecas[a].Z - pecas[b].Z;
                    if (dx * dx + dz * dz < minima * minima)
                    {
                        Assert.Fail($"Peças {a} e {b} encostadas em ({pecas[a].X}, {pecas[a].Z}).");
                    }
                }
            }
        }

        private static readonly string[] TiposComTronco =
        {
            "Cacto",
            "Arvore_Seca",
            "Pinheiro",
            "Arvore_Pantano",
        };

        [Test]
        public void Hub_desenha_toda_peca_dos_biomas_por_instancias_com_poucos_materiais()
        {
            ConstruirTerreno();
            var raiz = _hub.transform.Find(ConstrutorHub.NomePecasDosBiomas);
            Assert.IsNotNull(raiz, "O hub deveria ter as peças dos biomas.");
            var blocos = raiz.GetComponent<BlocosDeVegetacao>();
            Assert.IsNotNull(blocos);

            // Sem um objeto (nem um Renderer) por peça: o desenho é por instancing.
            Assert.AreEqual(0, raiz.GetComponentsInChildren<Renderer>(true).Length);
            Assert.GreaterOrEqual(blocos.Instancias, PecasDosBiomas.Todas.Count);

            // Materiais compartilhados por tipo: poucos, em vez de um por cubo.
            Assert.LessOrEqual(
                blocos.MateriaisDistintos,
                ConfigVegetacao.LimiteDeMateriais,
                "Materiais demais na vegetação."
            );
            Assert.LessOrEqual(MateriaisDosBiomas.Quantidade, ConfigVegetacao.LimiteDeMateriais);
        }

        [Test]
        public void Todo_tronco_gerado_tem_colisor_de_capsula_no_lugar_da_peca()
        {
            ConstruirTerreno();
            foreach (var nome in TiposComTronco)
            {
                var modelo = VegetacaoProcedural.Modelo(nome);
                Assert.IsTrue(modelo.Colisor.HasValue, $"{nome} sem colisor.");
                Assert.IsFalse(modelo.Colisor.Value.EhCaixa);
                Assert.Greater(modelo.Colisor.Value.Raio, 0.1f);
            }

            var verificadas = 0;
            foreach (var peca in PecasDosBiomas.Todas)
            {
                if (!TiposComTronco.Contains(peca.Prefab))
                {
                    continue;
                }

                var ponto = new Vector3(peca.X, peca.Y + 0.4f, peca.Z);
                var achou = false;
                foreach (var colisor in Physics.OverlapSphere(ponto, 0.05f))
                {
                    achou |= colisor is CapsuleCollider;
                }

                Assert.IsTrue(achou, $"{peca.Prefab} em ({peca.X}, {peca.Z}) atravessável.");
                verificadas++;
            }

            Assert.Greater(verificadas, 100);

            // Arbusto continua atravessável.
            Assert.IsFalse(VegetacaoProcedural.Modelo("Arbusto").Colisor.HasValue);
        }

        [TestCase("Cacto")]
        [TestCase("Arvore_Seca")]
        [TestCase("Pinheiro")]
        [TestCase("Rocha_Vulcanica")]
        [TestCase("Arvore_Pantano")]
        [TestCase("Arbusto")]
        public void Base_de_cada_peca_gerada_encosta_no_chao(string nome)
        {
            VegetacaoProcedural.Limpar();
            var modelo = VegetacaoProcedural.Modelo(nome);
            var maisBaixo = float.MaxValue;
            foreach (var parte in modelo.Partes)
            {
                var limites = parte.Malha.bounds;
                maisBaixo = Mathf.Min(maisBaixo, parte.Local.MultiplyPoint3x4(limites.min).y);
            }

            Assert.AreEqual(0f, maisBaixo, 0.02f, $"{nome} flutua ou afunda.");
            if (modelo.Colisor.HasValue && !modelo.Colisor.Value.EhCaixa)
            {
                var colisor = modelo.Colisor.Value;
                Assert.AreEqual(
                    0f,
                    colisor.Centro.y - (colisor.Altura * 0.5f),
                    0.02f,
                    $"Colisor de {nome} não parte do chão."
                );
            }
        }

        [Test]
        public void Montagem_da_vegetacao_cabe_em_quadros_curtos_e_espalha_o_trabalho()
        {
            var raiz = new GameObject("TesteDaVegetacao").transform;
            _hub = raiz.gameObject;
            var maior = 0.0;
            var etapas = 0;
            var relogio = System.Diagnostics.Stopwatch.StartNew();
            foreach (var _ in MontadorDaVegetacao.Montar(raiz, 1f, OrcamentoDeTesteMs))
            {
                maior = System.Math.Max(maior, relogio.Elapsed.TotalMilliseconds);
                etapas++;
                relogio.Restart();
            }

            Assert.Greater(etapas, 2, "A montagem deveria se espalhar por vários quadros.");
            Assert.Less(
                maior,
                LimiteDoQuadroDeMontagemMs,
                $"Uma etapa levou {maior:0.0} ms (orçamento {OrcamentoDeTesteMs} ms)."
            );
        }

        // Folga sobre o orçamento: o carregamento dos prefabs na primeira peça de cada tipo e o
        // ruído da máquina de teste cabem aqui.
        private const double LimiteDoQuadroDeMontagemMs = 60.0;

        // Orçamento pequeno de propósito: numa máquina rápida a montagem inteira cabe em poucos ms,
        // e o teste precisa ver a divisão em etapas.
        private const float OrcamentoDeTesteMs = 0.25f;

        [Test]
        public void Corte_por_distancia_tira_blocos_longe_e_pecas_pequenas_mais_perto()
        {
            ConstruirTerreno();
            var blocos = _hub
                .transform.Find(ConstrutorHub.NomePecasDosBiomas)
                .GetComponent<BlocosDeVegetacao>();

            var peca = PecasDosBiomas.Todas[0];
            var junto = new Vector3(peca.X, peca.Y, peca.Z);
            var perto = blocos.ContarChamadas(junto);
            var longe = blocos.ContarChamadas(new Vector3(0f, 0f, 0f));
            Assert.Greater(perto, 0);
            Assert.Less(perto, blocos.GruposTotais, "O corte deveria esconder o resto do mundo.");
            Assert.LessOrEqual(longe, blocos.GruposTotais);
        }

        [Test]
        public void Ambiente_muda_com_o_bioma_e_mistura_nas_fronteiras()
        {
            var padrao = AmbienteDosBiomas.Padrao;
            var campo = new PesosDosBiomas(1f, 0f, 0f, 0f, 0f, 0f, 0f);
            var neve = new PesosDosBiomas(0f, 0f, 0f, 0f, 1f, 0f, 0f);
            var campoENeve = new PesosDosBiomas(0.5f, 0f, 0f, 0f, 0.5f, 0f, 0f);
            var lavaEPantano = new PesosDosBiomas(0f, 0f, 0f, 0f, 0f, 0.25f, 0.75f);

            ConferirCor(padrao.Nevoa, AmbienteDosBiomas.Misturar(campo).Nevoa);
            Assert.AreEqual(padrao.ForcaDoSol, AmbienteDosBiomas.Misturar(campo).ForcaDoSol, 1e-6f);

            var naNeve = AmbienteDosBiomas.Do(Bioma.Neve);
            ConferirCor(naNeve.Nevoa, AmbienteDosBiomas.Misturar(neve).Nevoa);
            Assert.AreEqual(naNeve.Densidade, AmbienteDosBiomas.Misturar(neve).Densidade, 1e-6f);

            var meio = AmbienteDosBiomas.Misturar(campoENeve);
            Assert.AreEqual((padrao.Densidade + naNeve.Densidade) / 2f, meio.Densidade, 1e-6f);
            ConferirCor(Color.Lerp(padrao.Ceu, naNeve.Ceu, 0.5f), meio.Ceu);

            var naLava = AmbienteDosBiomas.Do(Bioma.Lava);
            var noPantano = AmbienteDosBiomas.Do(Bioma.Pantano);
            var misto = AmbienteDosBiomas.Misturar(lavaEPantano);
            Assert.AreEqual(
                0.25f * naLava.Densidade + 0.75f * noPantano.Densidade,
                misto.Densidade,
                1e-6f
            );

            // Os quatro biomas de fora têm, cada um, a sua névoa.
            var nevoas = new[] { Bioma.Deserto, Bioma.Neve, Bioma.Lava, Bioma.Pantano }
                .Select(b => AmbienteDosBiomas.Do(b).Nevoa)
                .Append(padrao.Nevoa)
                .Distinct();
            Assert.AreEqual(5, nevoas.Count());

            // No mapa: perto da cidade é o padrão; no meio das montanhas de neve, o da neve.
            ConferirCor(padrao.Nevoa, AmbienteDosBiomas.Em(0f, -24f).Nevoa);
            ConferirCor(naNeve.Nevoa, AmbienteDosBiomas.Em(0f, 1200f).Nevoa);
        }

        [Test]
        public void Hub_liga_a_nevoa_e_o_ambiente_do_nascimento()
        {
            ConstruirTerreno();

            var esperado = AmbienteDosBiomas.Padrao;
            Assert.IsTrue(RenderSettings.fog);
            ConferirCor(esperado.Nevoa, RenderSettings.fogColor);
            Assert.AreEqual(esperado.Densidade, RenderSettings.fogDensity, 1e-6f);
            ConferirCor(esperado.Ceu, RenderSettings.ambientSkyColor);

            var ambiente = _hub.transform.Find(ConstrutorHub.NomeAmbiente);
            Assert.IsNotNull(ambiente, "O hub deveria ter o objeto do ambiente.");
            Assert.IsNotNull(ambiente.GetComponent<AmbienteDosBiomas>());

            var sol = _hub.transform.Find(ConstrutorHub.NomeLuz).GetComponent<Light>();
            Assert.AreEqual(esperado.ForcaDoSol, sol.intensity, 1e-4f);
        }

        [Test]
        public void Cada_bioma_tem_o_seu_ceu_e_o_efeito_nao_vaza()
        {
            Assert.AreEqual(EfeitoDoClima.Poeira, AmbienteDosBiomas.Do(Bioma.Deserto).Efeito);
            Assert.AreEqual(EfeitoDoClima.NeveCaindo, AmbienteDosBiomas.Do(Bioma.Neve).Efeito);
            Assert.AreNotEqual(
                AmbienteDosBiomas.Do(Bioma.Deserto).Efeito,
                AmbienteDosBiomas.Do(Bioma.Neve).Efeito
            );
            Assert.Greater(
                AmbienteDosBiomas.Do(Bioma.Pantano).Densidade,
                AmbienteDosBiomas.Padrao.Densidade
            );

            var dia = AmbienteDosBiomas.ComHora(AmbienteDosBiomas.Padrao, 12f);
            var noite = AmbienteDosBiomas.ComHora(AmbienteDosBiomas.Padrao, 22f);
            Assert.Less(noite.Ceu.grayscale, dia.Ceu.grayscale);
            Assert.Less(noite.Nevoa.grayscale, dia.Nevoa.grayscale);

            AmbienteDosBiomas.Hora = 22f;
            try
            {
                Assert.AreEqual(EfeitoDoClima.Vagalume, AmbienteDosBiomas.Em(350f, -250f).Efeito);
            }
            finally
            {
                AmbienteDosBiomas.Hora = 12f;
            }

            Assert.AreNotEqual(EfeitoDoClima.Vagalume, AmbienteDosBiomas.Em(350f, -250f).Efeito);
        }

        [Test]
        public void Boca_da_caverna_fura_a_malha_do_bloco_e_o_remendo_preenche()
        {
            var passo = MapaDoMundo.PassoDaGrade;
            var i = Mathf.FloorToInt((PrimeiraCaverna.EntradaX + MapaDoMundo.Metade) / passo);
            var j = Mathf.FloorToInt((PrimeiraCaverna.EntradaZ + MapaDoMundo.Metade) / passo);
            Assert.IsTrue(ConstrutorDoTerreno.CelulaComFuro(i, j), "a célula da boca é furada");
            Assert.IsFalse(
                ConstrutorDoTerreno.CelulaComFuro(150, 150),
                "longe da caverna não há furo"
            );

            var porBloco = ConstrutorDoTerreno.CelulasPorBloco;
            var bi = i / porBloco;
            var bj = j / porBloco;
            var malha = ConstrutorDoTerreno.CriarMalhaDoBloco(Amostras, bi, bj);
            var remendo = ConstrutorDoTerreno.CriarMalhaDoFuro();
            try
            {
                var furadas = 0;
                for (var lj = 0; lj < porBloco; lj++)
                {
                    for (var li = 0; li < porBloco; li++)
                    {
                        if (
                            ConstrutorDoTerreno.CelulaComFuro(
                                bi * porBloco + li,
                                bj * porBloco + lj
                            )
                        )
                        {
                            furadas++;
                        }
                    }
                }

                Assert.Greater(furadas, 0);
                Assert.AreEqual((porBloco * porBloco - furadas) * 6, malha.triangles.Length);
                Assert.IsNotNull(remendo);
                Assert.Greater(remendo.triangles.Length, 0);

                // O remendo segue o relevo e nenhum triângulo dele fica sobre o túnel.
                var vertices = remendo.vertices;
                var indices = remendo.triangles;
                foreach (var v in vertices)
                {
                    Assert.AreEqual(MapaDoMundo.AlturaEm(v.x, v.z), v.y, 0.001f);
                }

                // Cada quadrado do remendo são dois triângulos (seis índices) e o centro do quadrado
                // é que decide se ele abre o túnel: o centro de um triângulo solto não conta.
                for (var t = 0; t < indices.Length; t += 6)
                {
                    var centro = Vector3.zero;
                    for (var k = 0; k < 6; k++)
                    {
                        centro += vertices[indices[t + k]];
                    }

                    centro /= 6f;
                    Assert.IsFalse(
                        PrimeiraCaverna.FuraOTerreno(centro.x, centro.z),
                        "triângulo sobre o túnel"
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(malha);
                if (remendo != null)
                {
                    Object.DestroyImmediate(remendo);
                }
            }
        }

        [Test]
        public void Ambiente_da_caverna_nao_tem_sol_nem_clima()
        {
            var fora = AmbienteDosBiomas.Em(250f, -100f);
            var dentro = AmbienteDosBiomas.NaCaverna(fora);

            Assert.AreEqual(0f, dentro.ForcaDoSol);
            Assert.AreEqual(EfeitoDoClima.Nenhum, dentro.Efeito);
            Assert.Less(dentro.Ceu.grayscale, fora.Ceu.grayscale);
            Assert.Greater(dentro.Densidade, 0f);
        }
    }
}
