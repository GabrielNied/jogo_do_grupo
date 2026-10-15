using System;
using System.Linq;
using System.Reflection;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class ConstrutorHubTestes
    {
        private GameObject _hub;
        private AmbientMode _modo;
        private Color _ceu;
        private Color _horizonte;
        private Color _solo;
        private bool _nevoa;
        private FogMode _modoDaNevoa;
        private Color _corDaNevoa;
        private float _densidadeDaNevoa;

        [SetUp]
        public void Preparar()
        {
            _nevoa = RenderSettings.fog;
            _modoDaNevoa = RenderSettings.fogMode;
            _corDaNevoa = RenderSettings.fogColor;
            _densidadeDaNevoa = RenderSettings.fogDensity;
            _modo = RenderSettings.ambientMode;
            _ceu = RenderSettings.ambientSkyColor;
            _horizonte = RenderSettings.ambientEquatorColor;
            _solo = RenderSettings.ambientGroundColor;
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

        private PosicaoInicial Construir()
        {
            var inicio = ConstrutorHub.Construir();
            _hub = GameObject.Find(ConstrutorHub.NomeRaiz);
            Assert.IsNotNull(_hub, "O construtor deveria criar o objeto raiz do hub.");
            Physics.SyncTransforms();
            return inicio;
        }

        [Test]
        public void Nascimento_fica_dentro_dos_limites_do_chao_e_nao_na_origem()
        {
            var inicio = Construir();

            Assert.Greater(
                new Vector2(inicio.Posicao.x, inicio.Posicao.z).magnitude,
                1f,
                "O nascimento não pode ser a origem por acaso (a fonte fica lá)."
            );
            Assert.IsTrue(
                LayoutDoHub.DentroDaAreaAndavel(inicio.Posicao, EntradaNoJogo.RaioJogador),
                $"Nascimento {inicio.Posicao} fora da área andável."
            );

            var chao = _hub
                .transform.Find(ConstrutorHub.NomeChao)
                .GetComponentInChildren<Collider>();
            Assert.AreEqual(LayoutDoHub.Lado, chao.bounds.size.x, 0.01f);
            Assert.AreEqual(LayoutDoHub.Lado, chao.bounds.size.z, 0.01f);
            Assert.LessOrEqual(Mathf.Abs(inicio.Posicao.x), chao.bounds.extents.x);
            Assert.LessOrEqual(Mathf.Abs(inicio.Posicao.z), chao.bounds.extents.z);
        }

        [Test]
        public void Nascimento_tem_os_pes_na_superficie_do_chao()
        {
            var inicio = Construir();

            var achou = Physics.Raycast(
                inicio.Posicao + Vector3.up * 5f,
                Vector3.down,
                out var batida,
                10f
            );

            Assert.IsTrue(achou, "Nada abaixo do nascimento.");
            Assert.AreEqual(inicio.Posicao.y, batida.point.y, 0.01f);
        }

        [Test]
        public void Nascimento_nao_fica_dentro_de_nenhum_colisor()
        {
            var inicio = Construir();

            // Cápsula do CharacterController, 5 cm acima do chão (o chão não conta como batida).
            var baixo = inicio.Posicao + Vector3.up * (EntradaNoJogo.RaioJogador + 0.05f);
            var alto =
                inicio.Posicao
                + Vector3.up * (EntradaNoJogo.AlturaJogador - EntradaNoJogo.RaioJogador);
            var colisores = Physics.OverlapCapsule(baixo, alto, EntradaNoJogo.RaioJogador);

            Assert.IsEmpty(
                colisores.Select(c => c.name).ToArray(),
                "O jogador nasceria dentro de um colisor."
            );
        }

        [Test]
        public void Nascimento_olha_para_o_centro_do_hub()
        {
            var inicio = Construir();

            var frente = inicio.Rotacao * Vector3.forward;
            var paraCentro = (Vector3.zero - inicio.Posicao).normalized;

            Assert.Greater(Vector3.Dot(frente, paraCentro), 0.99f);
        }

        [Test]
        public void Muros_com_colisor_cercam_toda_a_area_andavel()
        {
            Construir();

            var colisores = _hub
                .transform.Find(ConstrutorHub.NomeMuros)
                .GetComponentsInChildren<Collider>();
            var limites = colisores[0].bounds;
            foreach (var colisor in colisores)
            {
                limites.Encapsulate(colisor.bounds);
            }

            Assert.AreEqual(LayoutDoHub.Lado, limites.size.x, 0.01f);
            Assert.AreEqual(LayoutDoHub.Lado, limites.size.z, 0.01f);
            Assert.AreEqual(0f, limites.center.x, 0.01f);
            Assert.AreEqual(0f, limites.center.z, 0.01f);
            // 76 células de borda, menos as 2 do vão do portão.
            Assert.AreEqual(4 * (LayoutDoHub.CelulasPorLado - 1) - 2, colisores.Length);
            Assert.GreaterOrEqual(limites.size.y, LayoutDoHub.AlturaTorre - 0.01f);
        }

        [Test]
        public void Jogador_andando_para_cada_lado_bate_nos_muros()
        {
            Construir();

            // Linhas livres de props (a rua em anel, de pedra e sem colisor).
            var anel = -LayoutDoHub.CentroDoAnel;
            var testes = new[]
            {
                (new Vector3(0f, 1f, anel), Vector3.left),
                (new Vector3(0f, 1f, anel), Vector3.right),
                (new Vector3(anel, 1f, 0f), Vector3.forward),
                (new Vector3(anel, 1f, 0f), Vector3.back),
            };

            foreach (var (origem, direcao) in testes)
            {
                var achou = Physics.Raycast(origem, direcao, out var batida, LayoutDoHub.Lado);

                Assert.IsTrue(achou, $"Sem muro na direção {direcao}.");
                // Só o eixo do raio importa: a batida tem de ser na face interna do muro.
                var distanciaDoCentro = Mathf.Abs(Vector3.Dot(batida.point, direcao));
                Assert.AreEqual(LayoutDoHub.MetadeAndavel, distanciaDoCentro, 0.05f);
            }
        }

        [Test]
        public void Hub_tem_uma_luz_direcional_props_e_estradas()
        {
            Construir();

            var luzes = _hub.GetComponentsInChildren<Light>();

            Assert.IsTrue(luzes.Any(l => l.type == LightType.Directional));
            var props = _hub.transform.Find(ConstrutorHub.NomeProps);
            Assert.AreEqual(LayoutDoHub.Props.Count, props.childCount);
            var estradas = _hub.transform.Find(ConstrutorHub.NomeEstradas);
            Assert.IsNotEmpty(estradas.GetComponentsInChildren<Renderer>());
        }

        [Test]
        public void Sol_e_a_luz_principal_com_sombra_e_o_preenchimento_e_mais_fraco_e_sem_sombra()
        {
            Construir();

            var sol = _hub.transform.Find(ConstrutorHub.NomeLuz).GetComponent<Light>();
            var preenchimento = _hub
                .transform.Find(ConstrutorHub.NomePreenchimento)
                .GetComponent<Light>();

            Assert.AreEqual(LightType.Directional, sol.type);
            Assert.AreEqual(LightType.Directional, preenchimento.type);
            Assert.AreSame(sol, RenderSettings.sun, "O sol deve ser a luz principal da cena.");
            Assert.AreNotEqual(LightShadows.None, sol.shadows);
            Assert.Greater(sol.shadowStrength, 0f);
            Assert.Less(sol.shadowStrength, 1f, "A sombra não pode ser preta.");
            Assert.AreEqual(LightShadows.None, preenchimento.shadows);
            Assert.Greater(preenchimento.intensity, 0f);
            Assert.Less(preenchimento.intensity, sol.intensity);

            // O preenchimento vem do lado oposto ao do sol (as luzes apontam em sentidos opostos
            // no plano horizontal), para clarear o lado do personagem fora do sol.
            var deSol = Vector3.ProjectOnPlane(sol.transform.forward, Vector3.up);
            var dePreenchimento = Vector3.ProjectOnPlane(
                preenchimento.transform.forward,
                Vector3.up
            );
            Assert.Less(Vector3.Dot(deSol.normalized, dePreenchimento.normalized), -0.9f);
        }

        [Test]
        public void O_ambiente_tem_o_ciclo_do_dia()
        {
            Construir();

            var ambiente = _hub.transform.Find(ConstrutorHub.NomeAmbiente);
            Assert.IsNotNull(ambiente);
            Assert.IsNotNull(ambiente.GetComponent<DiaENoite.CicloDoDia>());
        }

        [Test]
        public void Lanternas_da_praca_e_do_portao_tem_luz_de_ponto_com_alcance()
        {
            Construir();

            var props = _hub.transform.Find(ConstrutorHub.NomeProps);
            var lanternas = props
                .Cast<Transform>()
                .Where(t => t.name == ConstrutorHub.PrefabDaLanterna)
                .ToList();

            var esperadas = LayoutDoHub.Props.Count(p =>
                p.Prefab == ConstrutorHub.PrefabDaLanterna
            );
            Assert.Greater(esperadas, 0);
            Assert.AreEqual(esperadas, lanternas.Count, "Faltam lanternas no hub.");

            var daPraca = lanternas.Count(t =>
                Mathf.Abs(t.position.x) < 12f && Mathf.Abs(t.position.z) < 12f
            );
            var doPortao = lanternas.Count(t => t.position.z > 30f && Mathf.Abs(t.position.x) < 8f);
            Assert.AreEqual(4, daPraca, "A praça tem uma lanterna em cada canto.");
            Assert.AreEqual(2, doPortao, "O portão tem uma lanterna de cada lado.");

            foreach (var lanterna in lanternas)
            {
                var luz = lanterna.GetComponentInChildren<Light>();
                Assert.IsNotNull(luz, $"Lanterna em {lanterna.position} sem luz.");
                // Acende e apaga pela hora do mundo (#382); de dia, apagada.
                Assert.IsNotNull(luz.GetComponent<DiaENoite.LuzDaLanterna>());
                Assert.AreEqual(DiaENoite.LuzDaLanterna.Acesa(AmbienteDosBiomas.Hora), luz.enabled);
                Assert.AreEqual(LightType.Point, luz.type);
                Assert.Greater(luz.range, 0f);
                Assert.AreEqual(ConfigIluminacao.AlcanceDaLanterna, luz.range, 1e-4f);
                Assert.AreEqual(ConfigIluminacao.IntensidadeDaLanterna, luz.intensity, 1e-4f);
                // Chama quente: mais vermelho que azul.
                Assert.Greater(luz.color.r, luz.color.b);
            }
        }

        [Test]
        public void Trocar_de_bioma_muda_a_cor_do_ambiente_e_o_preenchimento_acompanha()
        {
            Construir();

            var sol = _hub.transform.Find(ConstrutorHub.NomeLuz).GetComponent<Light>();
            var preenchimento = _hub
                .transform.Find(ConstrutorHub.NomePreenchimento)
                .GetComponent<Light>();

            AmbienteDosBiomas.Aplicar(AmbienteDosBiomas.Do(Bioma.Campo), sol, preenchimento);
            var ceuDoCampo = RenderSettings.ambientSkyColor;
            var corDoPreenchimento = preenchimento.color;

            AmbienteDosBiomas.Aplicar(AmbienteDosBiomas.Do(Bioma.Lava), sol, preenchimento);

            Assert.AreNotEqual(ceuDoCampo, RenderSettings.ambientSkyColor);
            Assert.AreNotEqual(corDoPreenchimento, preenchimento.color);
            Assert.AreEqual(AmbienteDosBiomas.Do(Bioma.Lava).ForcaDoSol, sol.intensity, 1e-4f);
            Assert.Less(preenchimento.intensity, sol.intensity);
        }

        [Test]
        public void Props_ficam_na_area_andavel_e_longe_do_nascimento()
        {
            foreach (var peca in LayoutDoHub.Props)
            {
                var posicao = new Vector3(peca.X, 0f, peca.Z);

                Assert.IsTrue(
                    LayoutDoHub.DentroDaAreaAndavel(posicao, 1f),
                    $"{peca.Prefab} em {posicao} encosta nos muros ou sai do hub."
                );
                Assert.Greater(
                    Vector3.Distance(posicao, LayoutDoHub.PontoDeNascimento),
                    2.5f,
                    $"{peca.Prefab} em {posicao} está colado no nascimento."
                );
            }
        }

        // Se uma cápsula do CharacterController (a 5 cm do chão) passa de "pe" andando "distancia"
        // metros na direção horizontal "direcao" sem bater em nada.
        private static bool JogadorPassa(Vector3 pe, Vector3 direcao, float distancia)
        {
            var baixo = pe + Vector3.up * (EntradaNoJogo.RaioJogador + 0.05f);
            var alto = pe + Vector3.up * (EntradaNoJogo.AlturaJogador - EntradaNoJogo.RaioJogador);
            return !Physics.CapsuleCast(baixo, alto, EntradaNoJogo.RaioJogador, direcao, distancia);
        }

        [Test]
        public void Nascimento_fica_dentro_da_cidade_murada_olhando_para_a_fonte()
        {
            var inicio = Construir();

            Assert.AreEqual(new Vector3(0f, 0f, -24f), inicio.Posicao);
            Assert.Less(Mathf.Abs(inicio.Posicao.x), LayoutDoHub.MetadeAndavel);
            Assert.Less(Mathf.Abs(inicio.Posicao.z), LayoutDoHub.MetadeAndavel);
            Assert.AreEqual(Vector3.forward, inicio.Rotacao * Vector3.forward);
        }

        [Test]
        public void Terreno_de_fora_tem_chao_andavel_com_a_face_de_cima_em_zero()
        {
            Construir();
            var terreno = _hub.transform.Find(ConstrutorHub.NomeTerreno);
            Assert.IsNotNull(terreno, "O hub deveria ter o terreno de fora.");

            // Até 2026-10-11 era um colisor só na raiz, de 1 km, com a face de cima em zero. Agora
            // o chão é uma malha com relevo em blocos, cada um com o seu colisor: juntos cobrem
            // o mundo de 3 km. Logo fora dos muros ele sobe devagar (a faixa inicial, até 5 m):
            // o laço abaixo confere a face de cima contra MapaDoMundo.AlturaEm.
            var blocos = terreno.Find(ConstrutorDoTerreno.NomeBlocos);
            Assert.IsNotNull(blocos, "O terreno precisa dos blocos do chão.");
            // O remendo da boca da caverna também mora em "blocos" e tem colisor, mas não é bloco.
            var colisores = blocos
                .GetComponentsInChildren<MeshCollider>()
                .Where(c => c.name != ConstrutorDoTerreno.NomeFuroDaCaverna)
                .ToArray();
            var total = ConstrutorDoTerreno.BlocosPorLado * ConstrutorDoTerreno.BlocosPorLado;
            Assert.AreEqual(total, colisores.Length);
            var chao = colisores[0].bounds;
            foreach (var colisor in colisores)
            {
                chao.Encapsulate(colisor.bounds);
            }

            Assert.GreaterOrEqual(chao.size.x, MapaDoMundo.Lado - 0.01f);
            Assert.GreaterOrEqual(chao.size.z, MapaDoMundo.Lado - 0.01f);
            Assert.GreaterOrEqual(chao.size.x, LayoutDoHub.LadoDoTerreno - 0.01f);

            // Faixa livre logo fora dos muros (os props começam mais longe), em passo de 5 m.
            var testados = 0;
            var limite = LayoutDoHub.Metade + 20f;
            for (var x = -limite; x <= limite; x += 5f)
            {
                for (var z = -limite; z <= limite; z += 5f)
                {
                    var foraDoQuadradoDaCidade =
                        Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) > LayoutDoHub.Metade;
                    var ponto = new Vector3(x, MapaDoMundo.AlturaEm(x, z), z);
                    if (!foraDoQuadradoDaCidade || ArvoreOuPedraPerto(ponto, 2.5f))
                    {
                        continue;
                    }

                    testados++;
                    Assert.IsTrue(
                        Physics.Raycast(ponto + Vector3.up * 5f, Vector3.down, out var batida, 10f),
                        $"Sem chão em {ponto}."
                    );
                    Assert.AreEqual(
                        ponto.y,
                        batida.point.y,
                        0.2f,
                        $"Chão fora do relevo em {ponto}."
                    );
                    Assert.IsTrue(batida.collider.transform.IsChildOf(terreno));
                    Assert.IsTrue(
                        JogadorPassa(ponto + Vector3.up * 0.3f, Vector3.forward, 0.1f),
                        $"Colisor na altura do jogador em {ponto}."
                    );
                }
            }

            Assert.Greater(testados, 50, "Poucos pontos de fora para conferir.");
        }

        private static bool ArvoreOuPedraPerto(Vector3 ponto, float distancia)
        {
            foreach (var peca in LayoutDoHub.PropsFora)
            {
                var dx = peca.X - ponto.x;
                var dz = peca.Z - ponto.z;
                if (dx * dx + dz * dz < distancia * distancia)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void Barreiras_invisiveis_seguram_o_jogador_na_borda_do_terreno()
        {
            Construir();

            // A borda era a do terreno de 1 km (LayoutDoHub.MetadeDoTerreno) até 2026-10-11; agora
            // é a do mundo de 3 km. Os raios saem acima do pico mais alto, para o relevo não ficar
            // na frente: a barreira sobe até lá.
            var pertoDaBorda = MapaDoMundo.Metade - 5f;
            var alto = MapaDoMundo.AlturaMaxima + 10f;
            var testes = new[]
            {
                (new Vector3(pertoDaBorda, alto, 0f), Vector3.right),
                (new Vector3(-pertoDaBorda, alto, 0f), Vector3.left),
                (new Vector3(0f, alto, -pertoDaBorda), Vector3.back),
                (new Vector3(30f, alto, pertoDaBorda), Vector3.forward),
                (new Vector3(pertoDaBorda, 1f, 0f), Vector3.right),
                (new Vector3(0f, 1f, -pertoDaBorda), Vector3.back),
            };

            foreach (var (origem, direcao) in testes)
            {
                var batidas = Physics.RaycastAll(origem, direcao, 20f);
                var naBarreira = batidas.Where(b => b.collider.name.StartsWith("Barreira_"));
                Assert.IsNotEmpty(naBarreira, $"Sem barreira saindo de {origem}.");
                Assert.AreEqual(
                    MapaDoMundo.Metade,
                    Mathf.Abs(Vector3.Dot(naBarreira.First().point, direcao)),
                    0.05f
                );
            }
        }

        [Test]
        public void Vao_do_portao_nao_e_muro_solido()
        {
            Construir();
            var muros = _hub.transform.Find(ConstrutorHub.NomeMuros);
            var z = LayoutDoHub.CentroDaCelula(LayoutDoHub.CelulasPorLado - 1);

            // Nenhum colisor ocupa o vão (4 m de largura, na altura do jogador e acima).
            var noVao = Physics.OverlapBox(
                new Vector3(0f, 2f, z),
                new Vector3(LayoutDoHub.MetadeDoPortao - 0.05f, 1.9f, LayoutDoHub.Modulo / 2f),
                Quaternion.identity
            );
            Assert.IsEmpty(noVao.Select(c => c.name).ToArray(), "Há colisor no vão do portão.");

            foreach (var colisor in muros.GetComponentsInChildren<Collider>())
            {
                Assert.IsFalse(
                    colisor.bounds.Contains(new Vector3(0f, 1f, z)),
                    $"{colisor.name} cobre o centro do portão."
                );
            }

            // O vão tem 4 m: as torres dos dois lados são sólidas.
            foreach (var direcao in new[] { Vector3.left, Vector3.right })
            {
                Assert.IsTrue(
                    Physics.Raycast(new Vector3(0f, 1f, z), direcao, out var batida, 10f)
                );
                Assert.AreEqual(LayoutDoHub.MetadeDoPortao, Mathf.Abs(batida.point.x), 0.05f);
                Assert.IsTrue(batida.collider.transform.IsChildOf(muros));
            }
        }

        [Test]
        public void Jogador_sai_da_cidade_pelo_portao_ate_o_fim_da_estrada()
        {
            Construir();

            // De dentro da cidade (norte da fonte) até a ponta da estrada, por todo o vão. A
            // estrada sobe devagar fora dos muros: o jogador anda em passos de 1 m, com os pés
            // um pouco acima do chão (o CharacterController sobe degrau).
            var fim = LayoutDoHub.Metade + LayoutDoHub.ComprimentoDaEstradaFora;
            foreach (var x in new[] { -1.5f, 0f, 1.5f })
            {
                for (var z = 10f; z < fim; z += 1f)
                {
                    var y = Mathf.Max(MapaDoMundo.AlturaEm(x, z), MapaDoMundo.AlturaEm(x, z + 1f));
                    Assert.IsTrue(
                        JogadorPassa(new Vector3(x, y + 0.3f, z), Vector3.forward, 1f),
                        $"Algo bloqueia a saída em x = {x}, z = {z}."
                    );
                }
            }
        }

        [Test]
        public void Estrada_atravessa_o_portao_e_segue_pelo_terreno_de_fora()
        {
            Construir();
            var estradas = _hub.transform.Find(ConstrutorHub.NomeEstradas);
            var pontas = estradas
                .Cast<Transform>()
                .Select(t => t.position)
                .Where(p => Mathf.Abs(p.x) < LayoutDoHub.MetadeDoPortao)
                .ToArray();

            var zDoPortao = LayoutDoHub.CentroDaCelula(LayoutDoHub.CelulasPorLado - 1);
            Assert.IsTrue(
                pontas.Any(p => Mathf.Abs(p.z - zDoPortao) < 0.01f),
                "Sem estrada no portão."
            );
            Assert.IsTrue(
                pontas.Any(p =>
                    p.z
                    > LayoutDoHub.Metade + LayoutDoHub.ComprimentoDaEstradaFora - LayoutDoHub.Modulo
                ),
                "A estrada de fora não chega ao fim."
            );
            Assert.IsEmpty(
                estradas.GetComponentsInChildren<Collider>(),
                "Estrada não pode ter colisor."
            );
        }

        [Test]
        public void Cidade_tem_ruas_em_anel_ligadas_as_estradas_em_cruz()
        {
            // O anel passa por ±22 e encontra a cruz.
            Assert.IsTrue(LayoutDoHub.EhEstradaDaCidade(22f, 18f));
            Assert.IsTrue(LayoutDoHub.EhEstradaDaCidade(-18f, -22f));
            Assert.IsTrue(LayoutDoHub.EhEstradaDaCidade(22f, 2f));
            Assert.IsTrue(LayoutDoHub.EhEstradaDaCidade(14f, 18f));
            Assert.IsTrue(LayoutDoHub.EhEstradaDaCidade(18f, 8f));
            // Quarteirão entre o anel e a praça, e fora do anel, não são rua.
            Assert.IsFalse(LayoutDoHub.EhEstradaDaCidade(16f, 16f));
            Assert.IsFalse(LayoutDoHub.EhEstradaDaCidade(30f, 14f));
        }

        [Test]
        public void Props_de_fora_ficam_longe_da_cidade_e_do_corredor_do_portao()
        {
            Assert.GreaterOrEqual(LayoutDoHub.PropsFora.Count, 30);

            foreach (var peca in LayoutDoHub.PropsFora)
            {
                var posicao = new Vector3(peca.X, 0f, peca.Z);
                Assert.Greater(
                    Mathf.Max(Mathf.Abs(peca.X), Mathf.Abs(peca.Z)),
                    LayoutDoHub.Metade + 3f,
                    $"{peca.Prefab} em {posicao} encosta nos muros."
                );
                Assert.LessOrEqual(Mathf.Abs(peca.X), LayoutDoHub.MetadeDoTerreno - 1f);
                Assert.LessOrEqual(Mathf.Abs(peca.Z), LayoutDoHub.MetadeDoTerreno - 1f);
                Assert.IsFalse(
                    Mathf.Abs(peca.X) < LayoutDoHub.MetadeDoPortao + 1f && peca.Z > 0f,
                    $"{peca.Prefab} em {posicao} está na estrada do portão."
                );
            }
        }

        [Test]
        public void Props_da_cidade_nao_se_sobrepoem()
        {
            Construir();
            var props = _hub.transform.Find(ConstrutorHub.NomeProps);
            var caixas = props
                .Cast<Transform>()
                .Select(t => (nome: t.name, colisores: t.GetComponentsInChildren<Collider>()))
                .Where(p => p.colisores.Length > 0)
                .Select(p =>
                {
                    var limites = p.colisores[0].bounds;
                    foreach (var colisor in p.colisores)
                    {
                        limites.Encapsulate(colisor.bounds);
                    }

                    // Encostar não conta (casas coladas): só sobreposição de mais de 2 cm.
                    limites.Expand(-0.04f);
                    return (p.nome, limites);
                })
                .ToArray();

            for (var i = 0; i < caixas.Length; i++)
            {
                for (var j = i + 1; j < caixas.Length; j++)
                {
                    Assert.IsFalse(
                        caixas[i].limites.Intersects(caixas[j].limites),
                        $"{caixas[i].nome} em {caixas[i].limites.center} sobrepõe {caixas[j].nome} em {caixas[j].limites.center}."
                    );
                }
            }
        }

        [Test]
        public void Todos_os_prefabs_do_layout_existem_em_Resources()
        {
            var nomes = LayoutDoHub
                .Props.Concat(LayoutDoHub.PropsFora)
                .Select(p => p.Prefab)
                .Concat(PecasDosBiomas.Todas.Select(p => p.Prefab))
                // Cacto, pinheiro, rocha vulcânica etc. nascem em código, sem prefab.
                .Where(nome => !VegetacaoProcedural.ConsegueCriar(nome))
                .Concat(
                    new[]
                    {
                        LayoutDoHub.PrefabChao,
                        LayoutDoHub.PrefabTerreno,
                        LayoutDoHub.PrefabMuro,
                        LayoutDoHub.PrefabTorre,
                        LayoutDoHub.PrefabEstrada,
                    }
                )
                .Distinct();

            foreach (var nome in nomes)
            {
                Assert.IsNotNull(
                    Resources.Load<GameObject>("Mundo/" + nome),
                    $"Prefab 'Mundo/{nome}' não existe: rode Jogo do Grupo > Mundo > Gerar peças do hub."
                );
            }
        }

        [Test]
        public void Construtor_fica_registrado_no_Core_ao_iniciar()
        {
            var registrar = typeof(ConstrutorHub).GetMethod(
                "Registrar",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            var campo = typeof(EntradaNoJogo).GetField(
                "_construirMundo",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.IsNotNull(registrar, "ConstrutorHub.Registrar não existe.");
            Assert.IsNotNull(
                registrar.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>(),
                "Registrar precisa de [RuntimeInitializeOnLoadMethod]."
            );
            Assert.IsNotNull(campo, "O Core mudou: o campo do construtor de mundo sumiu.");

            var anterior = campo.GetValue(null);
            try
            {
                EntradaNoJogo.DefinirConstrutorDeMundo(null);
                registrar.Invoke(null, null);

                var registrado = campo.GetValue(null) as Func<PosicaoInicial>;
                Assert.IsNotNull(registrado, "O construtor do hub não foi registrado.");
                Assert.AreEqual(nameof(ConstrutorHub.Construir), registrado.Method.Name);
            }
            finally
            {
                campo.SetValue(null, anterior);
            }
        }

        [Test]
        public void Terreno_de_fora_separa_campo_mata_e_colinas()
        {
            Assert.AreEqual(AreaDoTerreno.Campo, LayoutDoHub.AreaEm(0f, LayoutDoHub.Metade + 30f));
            Assert.AreEqual(AreaDoTerreno.Mata, LayoutDoHub.AreaEm(LayoutDoHub.Metade + 30f, 0f));
            Assert.AreEqual(
                AreaDoTerreno.Colinas,
                LayoutDoHub.AreaEm(-LayoutDoHub.Metade - 30f, 0f)
            );

            var campo = 0;
            var mata = 0;
            var colinas = 0;
            var pedrasNasColinas = 0;
            foreach (var peca in LayoutDoHub.PropsFora)
            {
                switch (LayoutDoHub.AreaEm(peca.X, peca.Z))
                {
                    case AreaDoTerreno.Campo:
                        campo++;
                        break;
                    case AreaDoTerreno.Mata:
                        mata++;
                        break;
                    default:
                        colinas++;
                        if (peca.Prefab.StartsWith("Pedra"))
                        {
                            pedrasNasColinas++;
                        }

                        break;
                }
            }

            Assert.GreaterOrEqual(campo, 40);
            Assert.GreaterOrEqual(mata, 100);
            Assert.Greater(mata, campo, "A mata fica mais cheia que o campo.");
            Assert.GreaterOrEqual(colinas, 50);
            Assert.Greater(pedrasNasColinas, colinas / 2, "As colinas têm mais pedra que árvore.");

            // Até 2026-10-11 cada área era uma placa colorida com o nome dela. Agora o chão é uma
            // malha só, pintada pelo mapa do mundo: as três áreas continuam nos mesmos lados.
            Assert.AreEqual(Bioma.Campo, MapaDoMundo.BiomaEm(0f, 250f));
            Assert.AreEqual(Bioma.Mata, MapaDoMundo.BiomaEm(250f, -100f));
            Assert.AreEqual(Bioma.Colinas, MapaDoMundo.BiomaEm(-250f, -100f));

            Construir();
            var terreno = _hub.transform.Find(ConstrutorHub.NomeTerreno);
            Assert.IsNotNull(terreno.Find(ConstrutorDoTerreno.NomeBlocos));
        }

        [Test]
        public void Montagem_do_mundo_com_orcamento_pequeno_divide_em_varios_quadros_e_o_mundo_e_o_mesmo()
        {
            ConstrutorHub.LimparCacheParaTeste();
            var inteiro = ConstrutorHub.MontarEtapasParaTeste(float.MaxValue);
            ConstrutorHub.LimparCacheParaTeste();
            var picado = ConstrutorHub.MontarEtapasParaTeste(0f);

            Assert.Greater(
                picado.Etapas,
                inteiro.Etapas + 100,
                "Muros, props, estruturas e caverna deveriam ceder o quadro várias vezes."
            );
            Assert.AreEqual(
                inteiro.Objetos,
                picado.Objetos,
                "Dividir em quadros não pode mudar o mundo montado."
            );
        }
    }
}
