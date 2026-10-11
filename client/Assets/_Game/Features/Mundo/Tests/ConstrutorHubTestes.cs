using System;
using System.Linq;
using System.Reflection;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Features.Mundo.Hub;
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

        [SetUp]
        public void Preparar()
        {
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
            Assert.AreEqual(4 * (LayoutDoHub.CelulasPorLado - 1), colisores.Length);
            Assert.GreaterOrEqual(limites.size.y, LayoutDoHub.AlturaTorre - 0.01f);
        }

        [Test]
        public void Jogador_andando_para_cada_lado_bate_nos_muros()
        {
            Construir();

            // Linhas livres de props: z = -13 para os lados e x = -10 para frente e para trás.
            var testes = new[]
            {
                (new Vector3(0f, 1f, -13f), Vector3.left),
                (new Vector3(0f, 1f, -13f), Vector3.right),
                (new Vector3(-10f, 1f, 0f), Vector3.forward),
                (new Vector3(-10f, 1f, 0f), Vector3.back),
            };

            foreach (var (origem, direcao) in testes)
            {
                var achou = Physics.Raycast(origem, direcao, out var batida, 60f);

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
    }
}
