using System.Linq;
using JogoDoGrupo.Client.Features.Itens.Visual;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    /// <summary>
    /// A arma vai ao encaixe certo (mão do conjunto ativo, costas do inativo) e some ao
    /// desequipar. Lógica sem tela: a cena é conferida jogando.
    /// </summary>
    public class ApresentadorDoVisualTestes
    {
        private GameObject _avatar;
        private Transform _mao;
        private Transform _costas;
        private Transform _cabeca;
        private Transform _tronco;

        [SetUp]
        public void Montar()
        {
            // Como o personagem do gerador: os encaixes existem com os nomes fixos.
            _avatar = new GameObject("Jogador_Teste");
            _mao = Encaixe("EncaixeMaoDireita");
            _costas = Encaixe("EncaixeCostas");
            _cabeca = Encaixe("EncaixeCabeca");
            _tronco = Encaixe("EncaixeTronco");
        }

        [TearDown]
        public void Desmontar()
        {
            Object.DestroyImmediate(_avatar);
        }

        private Transform Encaixe(string nome)
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(_avatar.transform, false);
            return objeto.transform;
        }

        private static VisualEquipado Visual(
            int conjunto,
            params (PosicaoEquipamento, TipoEquipamento)[] armas
        ) =>
            new VisualEquipado
            {
                PersonagemId = 1,
                ConjuntoAtivo = conjunto,
                Armas = armas
                    .Select(a => new ArmaVisual { Posicao = a.Item1, Tipo = a.Item2 })
                    .ToArray(),
            };

        private static VisualEquipado VisualCompleto(
            int conjunto,
            (PosicaoEquipamento, TipoEquipamento)[] armas,
            (PosicaoEquipamento, TipoEquipamento)[] armaduras
        ) =>
            new VisualEquipado
            {
                PersonagemId = 1,
                ConjuntoAtivo = conjunto,
                Armas = (armas ?? new (PosicaoEquipamento, TipoEquipamento)[0])
                    .Select(a => new ArmaVisual { Posicao = a.Item1, Tipo = a.Item2 })
                    .ToArray(),
                Armaduras = (armaduras ?? new (PosicaoEquipamento, TipoEquipamento)[0])
                    .Select(a => new ArmaduraVisual { Posicao = a.Item1, Tipo = a.Item2 })
                    .ToArray(),
            };

        private static int Armas(Transform encaixe) =>
            encaixe.GetComponentsInChildren<ArmaNoAvatar>(true).Length;

        private static int Armaduras(Transform encaixe) =>
            encaixe.GetComponentsInChildren<ArmaduraNoAvatar>(true).Length;

        [Test]
        public void ArmaDoConjuntoAtivo_VaiParaAMaoDireita()
        {
            ApresentadorDoVisual.Aplicar(
                _avatar.transform,
                Visual(1, (PosicaoEquipamento.Arma1, TipoEquipamento.Espada))
            );

            Assert.AreEqual(1, Armas(_mao));
            Assert.AreEqual(0, Armas(_costas));
        }

        [Test]
        public void ArmaDoConjuntoInativo_VaiParaAsCostas()
        {
            ApresentadorDoVisual.Aplicar(
                _avatar.transform,
                Visual(
                    1,
                    (PosicaoEquipamento.Arma1, TipoEquipamento.Espada),
                    (PosicaoEquipamento.Arma2, TipoEquipamento.Arco)
                )
            );

            Assert.AreEqual(1, Armas(_mao));
            Assert.AreEqual(1, Armas(_costas));
            Assert.AreEqual("Arma_Arco", _costas.GetComponentInChildren<ArmaNoAvatar>().name);
        }

        [Test]
        public void TrocarDeConjunto_InverteAMaoEAsCostas()
        {
            var armas = new[]
            {
                (PosicaoEquipamento.Arma1, TipoEquipamento.Espada),
                (PosicaoEquipamento.Arma2, TipoEquipamento.Martelo),
            };
            ApresentadorDoVisual.Aplicar(_avatar.transform, Visual(1, armas));
            ApresentadorDoVisual.Aplicar(_avatar.transform, Visual(2, armas));

            Assert.AreEqual("Arma_Martelo", _mao.GetComponentInChildren<ArmaNoAvatar>().name);
            Assert.AreEqual("Arma_Espada", _costas.GetComponentInChildren<ArmaNoAvatar>().name);
            Assert.AreEqual(2, _avatar.GetComponentsInChildren<ArmaNoAvatar>(true).Length);
        }

        [Test]
        public void Desequipar_RemoveOModelo()
        {
            ApresentadorDoVisual.Aplicar(
                _avatar.transform,
                Visual(1, (PosicaoEquipamento.Arma1, TipoEquipamento.Espada))
            );
            var postos = ApresentadorDoVisual.Aplicar(_avatar.transform, Visual(1));

            Assert.AreEqual(0, postos);
            Assert.AreEqual(0, _avatar.GetComponentsInChildren<ArmaNoAvatar>(true).Length);
        }

        [Test]
        public void ReaplicarOMesmoEstado_NaoDuplicaOModelo()
        {
            var visual = Visual(1, (PosicaoEquipamento.Arma1, TipoEquipamento.Cajado));
            ApresentadorDoVisual.Aplicar(_avatar.transform, visual);
            ApresentadorDoVisual.Aplicar(_avatar.transform, visual);

            Assert.AreEqual(1, Armas(_mao));
        }

        [Test]
        public void SemEncaixeNoAvatar_CriaUmPresoARaiz()
        {
            var capsula = new GameObject("Jogador_Capsula");
            try
            {
                ApresentadorDoVisual.Aplicar(
                    capsula.transform,
                    Visual(1, (PosicaoEquipamento.Arma1, TipoEquipamento.Espada))
                );

                var encaixe = capsula.transform.Find("EncaixeMaoDireita");
                Assert.IsNotNull(encaixe);
                Assert.AreEqual(1, Armas(encaixe));
                Assert.AreEqual(ApresentadorDoVisual.MaoDireitaNaRaiz, encaixe.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(capsula);
            }
        }

        [Test]
        public void SoArmaPrincipalTemModelo()
        {
            ApresentadorDoVisual.Aplicar(
                _avatar.transform,
                Visual(1, (PosicaoEquipamento.Secundaria1, TipoEquipamento.Escudo))
            );

            Assert.AreEqual(0, _avatar.GetComponentsInChildren<ArmaNoAvatar>(true).Length);
        }

        [Test]
        public void Controle_AplicaQuandoOAvatarExiste_ETrocaQuandoOEstadoMuda()
        {
            VisualEquipadoDoCliente.Reiniciar();
            var anterior = ControleDoVisualEquipado.Buscar;
            ControleDoVisualEquipado.Buscar = id => id == 1 ? _avatar : null;
            var controle = new GameObject("Controle").AddComponent<ControleDoVisualEquipado>();
            try
            {
                // A mensagem chega antes de o avatar existir: nada a fazer ainda.
                VisualEquipadoDoCliente.Receber(
                    Visual(1, (PosicaoEquipamento.Arma1, TipoEquipamento.Espada))
                );
                controle.Conferir();
                Assert.AreEqual(1, Armas(_mao));

                VisualEquipadoDoCliente.Receber(Visual(1));
                controle.Conferir();
                Assert.AreEqual(0, _avatar.GetComponentsInChildren<ArmaNoAvatar>(true).Length);
            }
            finally
            {
                ControleDoVisualEquipado.Buscar = anterior;
                VisualEquipadoDoCliente.Reiniciar();
                Object.DestroyImmediate(controle.gameObject);
            }
        }

        [Test]
        public void ObterTransformacao_MaoDireita_CentralizaEmpunhaduraEAlinhaGolpe()
        {
            var (posEspada, rotEspada) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Espada,
                EncaixeDoVisual.MaoDireita
            );
            Assert.AreEqual(75f, rotEspada.eulerAngles.x, 0.1f);
            Assert.Less(posEspada.z, 0f);

            var (posArco, rotArco) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Arco,
                EncaixeDoVisual.MaoDireita
            );
            Assert.AreEqual(270f, rotArco.eulerAngles.y, 0.1f);
            Assert.AreEqual(-0.45f, posArco.y, 0.01f);

            var (posCajado, rotCajado) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Cajado,
                EncaixeDoVisual.MaoDireita
            );
            Assert.AreEqual(15f, rotCajado.eulerAngles.x, 0.1f);
            Assert.AreEqual(-0.70f, posCajado.y, 0.05f);
        }

        [Test]
        public void ObterTransformacao_Costas_InclinaNaDiagonalEAplicaOffsetProfundidade()
        {
            var (posEspada, rotEspada) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Espada,
                EncaixeDoVisual.Costas
            );
            Assert.AreEqual(142f, rotEspada.eulerAngles.z, 0.1f);
            Assert.Less(posEspada.z, 0f); // Afastado para trás do torso

            var (posArco, rotArco) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Arco,
                EncaixeDoVisual.Costas
            );
            Assert.AreEqual(325f, rotArco.eulerAngles.z, 0.1f); // -35 graus normalizado
            Assert.Less(posArco.z, 0f);

            var (posCajado, rotCajado) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Cajado,
                EncaixeDoVisual.Costas
            );
            Assert.AreEqual(322f, rotCajado.eulerAngles.z, 0.1f); // -38 graus normalizado
            Assert.Less(posCajado.z, 0f);
        }

        [Test]
        public void Aplicar_MaoECostas_RecebemTransformsCalibrados()
        {
            ApresentadorDoVisual.Aplicar(
                _avatar.transform,
                Visual(
                    1,
                    (PosicaoEquipamento.Arma1, TipoEquipamento.Espada),
                    (PosicaoEquipamento.Arma2, TipoEquipamento.Arco)
                )
            );

            var espada = _mao.GetComponentInChildren<ArmaNoAvatar>();
            var arco = _costas.GetComponentInChildren<ArmaNoAvatar>();

            var (espadaPosEsperada, _) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Espada,
                EncaixeDoVisual.MaoDireita
            );
            var (arcoPosEsperada, _) = ApresentadorDoVisual.ObterTransformacao(
                TipoEquipamento.Arco,
                EncaixeDoVisual.Costas
            );

            Assert.AreEqual(espadaPosEsperada.x, espada.transform.localPosition.x, 0.001f);
            Assert.AreEqual(espadaPosEsperada.y, espada.transform.localPosition.y, 0.001f);
            Assert.AreEqual(espadaPosEsperada.z, espada.transform.localPosition.z, 0.001f);

            Assert.AreEqual(arcoPosEsperada.x, arco.transform.localPosition.x, 0.001f);
            Assert.AreEqual(arcoPosEsperada.y, arco.transform.localPosition.y, 0.001f);
            Assert.AreEqual(arcoPosEsperada.z, arco.transform.localPosition.z, 0.001f);
        }

        [Test]
        public void CapaceteEPeito_VaoParaCabecaETronco()
        {
            var visual = VisualCompleto(
                1,
                armas: null,
                armaduras: new[]
                {
                    (PosicaoEquipamento.Capacete, TipoEquipamento.Capacete),
                    (PosicaoEquipamento.Peito, TipoEquipamento.Peito),
                }
            );

            var postos = ApresentadorDoVisual.Aplicar(_avatar.transform, visual);

            Assert.AreEqual(2, postos);
            Assert.AreEqual(1, Armaduras(_cabeca));
            Assert.AreEqual(1, Armaduras(_tronco));
            Assert.AreEqual(
                "Armadura_Capacete",
                _cabeca.GetComponentInChildren<ArmaduraNoAvatar>().name
            );
            Assert.AreEqual(
                "Armadura_Peito",
                _tronco.GetComponentInChildren<ArmaduraNoAvatar>().name
            );
        }

        [Test]
        public void DesequiparArmadura_RemoveOModelo()
        {
            var comArmadura = VisualCompleto(
                1,
                armas: null,
                armaduras: new[]
                {
                    (PosicaoEquipamento.Capacete, TipoEquipamento.Capacete),
                    (PosicaoEquipamento.Peito, TipoEquipamento.Peito),
                }
            );
            ApresentadorDoVisual.Aplicar(_avatar.transform, comArmadura);
            Assert.AreEqual(2, _avatar.GetComponentsInChildren<ArmaduraNoAvatar>(true).Length);

            var semArmadura = VisualCompleto(1, armas: null, armaduras: null);
            var postos = ApresentadorDoVisual.Aplicar(_avatar.transform, semArmadura);

            Assert.AreEqual(0, postos);
            Assert.AreEqual(0, _avatar.GetComponentsInChildren<ArmaduraNoAvatar>(true).Length);
        }

        [Test]
        public void SemEncaixeArmaduraNoAvatar_CriaPresoARaiz()
        {
            var capsula = new GameObject("Jogador_Capsula_Armadura");
            try
            {
                var visual = VisualCompleto(
                    1,
                    armas: null,
                    armaduras: new[]
                    {
                        (PosicaoEquipamento.Capacete, TipoEquipamento.Capacete),
                        (PosicaoEquipamento.Peito, TipoEquipamento.Peito),
                    }
                );
                ApresentadorDoVisual.Aplicar(capsula.transform, visual);

                var encaixeCabeca = capsula.transform.Find("EncaixeCabeca");
                var encaixeTronco = capsula.transform.Find("EncaixeTronco");

                Assert.IsNotNull(encaixeCabeca);
                Assert.IsNotNull(encaixeTronco);
                Assert.AreEqual(1, Armaduras(encaixeCabeca));
                Assert.AreEqual(1, Armaduras(encaixeTronco));
                Assert.AreEqual(ApresentadorDoVisual.CabecaNaRaiz, encaixeCabeca.localPosition);
                Assert.AreEqual(ApresentadorDoVisual.TroncoNaRaiz, encaixeTronco.localPosition);
            }
            finally
            {
                Object.DestroyImmediate(capsula);
            }
        }

        [Test]
        public void ModelosDeArmadura_TemCapaceteEPeito()
        {
            Assert.IsTrue(ModelosDeArmadura.Tem(TipoEquipamento.Capacete));
            Assert.IsTrue(ModelosDeArmadura.Tem(TipoEquipamento.Peito));
            Assert.IsFalse(ModelosDeArmadura.Tem(TipoEquipamento.Espada));
            Assert.IsFalse(ModelosDeArmadura.Tem(TipoEquipamento.Luvas));

            var capacete = ModelosDeArmadura.Criar(TipoEquipamento.Capacete);
            var peito = ModelosDeArmadura.Criar(TipoEquipamento.Peito);
            try
            {
                Assert.IsNotNull(capacete);
                Assert.IsNotNull(peito);
                Assert.AreEqual("Armadura_Capacete", capacete.name);
                Assert.AreEqual("Armadura_Peito", peito.name);
            }
            finally
            {
                Object.DestroyImmediate(capacete);
                Object.DestroyImmediate(peito);
            }
        }
    }
}
