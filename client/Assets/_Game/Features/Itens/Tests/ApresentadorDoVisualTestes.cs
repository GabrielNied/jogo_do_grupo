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

        [SetUp]
        public void Montar()
        {
            // Como o personagem do gerador: os encaixes existem com os nomes fixos.
            _avatar = new GameObject("Jogador_Teste");
            _mao = Encaixe("EncaixeMaoDireita");
            _costas = Encaixe("EncaixeCostas");
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

        private static int Armas(Transform encaixe) =>
            encaixe.GetComponentsInChildren<ArmaNoAvatar>(true).Length;

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
    }
}
