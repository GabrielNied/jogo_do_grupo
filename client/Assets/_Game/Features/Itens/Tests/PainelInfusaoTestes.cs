using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class PainelInfusaoTestes
    {
        [Test]
        public void EstadoVazioExplicaComoObterEquipamentos()
        {
            var raiz = new VisualElement();
            new PainelInfusao(raiz, (_, __) => { }).Mostrar(
                new EquipamentosResponse { ModoInfusao = true }
            );
            Assert.IsTrue(
                raiz.Query<Label>().ToList().Exists(l => l.text.Contains("precisa de equipamentos"))
            );
            Assert.IsNull(raiz.Q<DropdownField>());
        }

        [Test]
        public void PreviaMostraValoresCustoSlotsEDestruicaoSemModificarItem()
        {
            var texto = PainelInfusao.DescreverPrevia(
                new PreviaInfusao
                {
                    Receptor = "Martelo Ressonante",
                    Doador = "Martelo da Serra",
                    DoadorId = 2,
                    Status = StatusItem.Forca,
                    Antes = 20,
                    Depois = 20.4,
                    Custo = 10,
                    SlotsDepois = 1,
                    Limite = 10,
                }
            );
            StringAssert.Contains("Força: 20", texto);
            StringAssert.Contains("1/10", texto);
            StringAssert.Contains("10 coroas", texto);
            StringAssert.Contains("Será consumido: Martelo da Serra (#2)", texto);
        }

        [Test]
        public void TrocarEscolhaRemovePreviaEConfirmacaoESemSaldoDesabilitaConfirmar()
        {
            var raiz = new VisualElement();
            var receptor = CatalogoEquipamentos.Obter(100);
            var doador = CatalogoEquipamentos.Obter(100);
            var pedidos = 0;
            var estado = new EquipamentosResponse
            {
                ModoInfusao = true,
                Coroas = 0,
                Inventario = new[]
                {
                    new EntradaInventario { InstanciaId = 1, Equipamento = receptor },
                    new EntradaInventario { InstanciaId = 2, Equipamento = doador },
                },
                Previa = new PreviaInfusao
                {
                    ReceptorId = 1,
                    DoadorId = 2,
                    Custo = 10,
                },
            };
            new PainelInfusao(raiz, (_, __) => pedidos++).Mostrar(estado);
            var confirmar = raiz.Query<Button>()
                .ToList()
                .Find(b => b.text.StartsWith("Confirmar infusão"));
            Assert.IsNotNull(confirmar);
            Assert.IsFalse(confirmar.enabledSelf);
            raiz.Q<DropdownField>("receptor-infusao").index = 1;
            Assert.IsFalse(
                raiz.Query<Button>().ToList().Exists(b => b.text.StartsWith("Confirmar infusão"))
            );
            Assert.AreEqual(0, pedidos);
            Assert.AreEqual(0, receptor.InfusoesUsadas);
            Assert.AreEqual(2, estado.Inventario.Length);
        }

        [Test]
        public void FavoritoNuncaApareceComoDoador()
        {
            var raiz = new VisualElement();
            var protegido = CatalogoEquipamentos.Obter(100);
            protegido.Favorito = true;
            new PainelInfusao(raiz, (_, __) => { }).Mostrar(
                new EquipamentosResponse
                {
                    Inventario = new[]
                    {
                        new EntradaInventario
                        {
                            InstanciaId = 1,
                            Equipamento = CatalogoEquipamentos.Obter(100),
                        },
                        new EntradaInventario { InstanciaId = 2, Equipamento = protegido },
                    },
                }
            );
            Assert.IsEmpty(raiz.Q<DropdownField>("doador-infusao").choices);
        }
    }
}
