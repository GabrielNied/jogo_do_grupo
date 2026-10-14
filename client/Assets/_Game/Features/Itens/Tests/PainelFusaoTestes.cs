using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class PainelFusaoTestes
    {
        private static EntradaInventario Entrada(long id, Equipamento dados) =>
            new EntradaInventario { InstanciaId = id, Equipamento = dados };

        [Test]
        public void EstadoVazioExplicaComoObterEquipamentos()
        {
            var raiz = new VisualElement();
            new PainelFusao(raiz, (_, __) => { }).Mostrar(
                new EquipamentosResponse { ModoFusao = true }
            );
            Assert.IsTrue(
                raiz.Query<Label>().ToList().Exists(l => l.text.Contains("precisa de equipamentos"))
            );
            Assert.IsNull(raiz.Q<DropdownField>());
        }

        [Test]
        public void PreviaMostraValoresNivelTierCustoMateriaisEConsumo()
        {
            var texto = PainelFusao.DescreverPrevia(
                new PreviaFusao
                {
                    Principal = "Martelo Ressonante",
                    PrincipalId = 1,
                    Material = "Martelo da Serra",
                    MaterialId = 2,
                    Categoria = TipoEquipamento.Martelo,
                    Status = StatusItem.DanoFisico,
                    ValorAtual = 40,
                    ValorMaterial = 20,
                    ValorDepois = 30,
                    NivelAtual = 5,
                    NivelDepois = 6,
                    TierAntes = TierItem.C,
                    TierDepois = TierItem.D,
                    Custo = 1200,
                    MateriaisDungeon = 1,
                }
            );
            StringAssert.Contains("Dano físico: 40 → 30 (-10; material 20)", texto);
            StringAssert.Contains("+5 → +6", texto);
            StringAssert.Contains("Tier C → D", texto);
            StringAssert.Contains("1200 coroas", texto);
            StringAssert.Contains("materiais: 1 de dungeon", texto);
            StringAssert.Contains("mesma categoria (Martelo)", texto);
            StringAssert.Contains("Martelo da Serra (#2) será consumido", texto);
        }

        [Test]
        public void MotivoDizOQueFazerQuandoNaoHaEscolha()
        {
            StringAssert.Contains("+10", PainelFusao.MotivoSemEscolha(true, 3, 3, 10));
            StringAssert.Contains(
                "material compatível",
                PainelFusao.MotivoSemEscolha(false, 0, 0, 2)
            );
            StringAssert.Contains(
                "atributo em comum",
                PainelFusao.MotivoSemEscolha(false, 1, 0, 2)
            );
            Assert.AreEqual("", PainelFusao.MotivoSemEscolha(false, 1, 2, 2));
        }

        [Test]
        public void TrocarEscolhaRemovePreviaEConfirmacaoESemSaldoDesabilitaConfirmar()
        {
            // Trocar a escolha dispara ChangeEvent, que só chega a elemento preso a um painel
            // de verdade (sem painel, o callback não roda e a prévia nunca some).
            var configuracao = ScriptableObject.CreateInstance<PanelSettings>();
            var objeto = new GameObject("PainelFusaoTestes");
            try
            {
                var documento = objeto.AddComponent<UIDocument>();
                documento.panelSettings = configuracao;
                var raiz = new VisualElement();
                documento.rootVisualElement.Add(raiz);
                TrocarEscolha(raiz);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objeto);
                UnityEngine.Object.DestroyImmediate(configuracao);
            }
        }

        private static void TrocarEscolha(VisualElement raiz)
        {
            var principal = CatalogoEquipamentos.Obter(100);
            var material = CatalogoEquipamentos.Obter(100);
            var pedidos = 0;
            var estado = new EquipamentosResponse
            {
                ModoFusao = true,
                Coroas = 0,
                Inventario = new[] { Entrada(1, principal), Entrada(2, material) },
                Previa = new PreviaFusao
                {
                    PrincipalId = 1,
                    MaterialId = 2,
                    Status = StatusItem.DanoFisico,
                    Custo = 100,
                },
            };
            new PainelFusao(raiz, (_, __) => pedidos++).Mostrar(estado);
            var confirmar = raiz.Q<Button>("confirmar-fusao");
            Assert.IsNotNull(confirmar);
            Assert.IsFalse(confirmar.enabledSelf);
            raiz.Q<DropdownField>("principal-fusao").index = 1;
            Assert.IsNull(raiz.Q<Button>("confirmar-fusao"));
            Assert.AreEqual(0, pedidos);
            Assert.AreEqual(0, principal.NivelFusao);
            Assert.AreEqual(2, estado.Inventario.Length);
        }

        [Test]
        public void ConfirmarSoFicaHabilitadoComSaldoEMateriais()
        {
            var raiz = new VisualElement();
            Equipamento A() => CatalogoEquipamentos.Obter(100);
            EquipamentosResponse Estado(long coroas, bool materiais) =>
                new EquipamentosResponse
                {
                    ModoFusao = true,
                    Coroas = coroas,
                    Inventario = new[] { Entrada(1, A()), Entrada(2, A()) },
                    Previa = new PreviaFusao
                    {
                        PrincipalId = 1,
                        MaterialId = 2,
                        Custo = 100,
                        MateriaisSuficientes = materiais,
                    },
                };

            new PainelFusao(raiz, (_, __) => { }).Mostrar(Estado(100, true));
            Assert.IsTrue(raiz.Q<Button>("confirmar-fusao").enabledSelf);

            new PainelFusao(raiz, (_, __) => { }).Mostrar(Estado(99, true));
            Assert.IsFalse(raiz.Q<Button>("confirmar-fusao").enabledSelf);

            new PainelFusao(raiz, (_, __) => { }).Mostrar(Estado(1000, false));
            Assert.IsFalse(raiz.Q<Button>("confirmar-fusao").enabledSelf);
        }

        [Test]
        public void FavoritoECategoriaDiferenteNuncaAparecemComoMaterial()
        {
            var raiz = new VisualElement();
            var protegido = CatalogoEquipamentos.Obter(100);
            protegido.Favorito = true;
            var outraCategoria = CatalogoEquipamentos.Obter(100);
            outraCategoria.Tipo = TipoEquipamento.Espada;
            new PainelFusao(raiz, (_, __) => { }).Mostrar(
                new EquipamentosResponse
                {
                    Inventario = new[]
                    {
                        Entrada(1, CatalogoEquipamentos.Obter(100)),
                        Entrada(2, protegido),
                        Entrada(3, outraCategoria),
                    },
                }
            );
            Assert.IsEmpty(raiz.Q<DropdownField>("material-fusao").choices);
            Assert.IsFalse(raiz.Q<Button>("previa-fusao").enabledSelf);
            StringAssert.Contains("material compatível", raiz.Q<Label>("motivo-fusao").text);
        }
    }
}
