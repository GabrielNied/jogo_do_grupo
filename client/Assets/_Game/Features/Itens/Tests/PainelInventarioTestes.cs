using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class PainelInventarioTestes
    {
        // Os botões só recebem eventos presos a um painel de verdade.
        private PanelSettings _configuracao;
        private GameObject _objeto;
        private VisualElement _documento;

        [SetUp]
        public void Preparar()
        {
            _configuracao = ScriptableObject.CreateInstance<PanelSettings>();
            _objeto = new GameObject("PainelInventarioTestes");
            var documento = _objeto.AddComponent<UIDocument>();
            documento.panelSettings = _configuracao;
            _documento = documento.rootVisualElement;
        }

        [TearDown]
        public void Limpar()
        {
            Object.DestroyImmediate(_objeto);
            Object.DestroyImmediate(_configuracao);
        }

        private VisualElement NovaRaiz()
        {
            var raiz = new VisualElement();
            _documento.Add(raiz);
            return raiz;
        }

        private static EntradaInventario Entrada(long id, int modelo, int casa) =>
            new EntradaInventario
            {
                InstanciaId = id,
                ItemId = modelo,
                Equipamento = CatalogoEquipamentos.Obter(modelo),
                Nome = "item" + id,
                Aba = 0,
                Casa = casa,
            };

        private static PainelInventario Montar(
            VisualElement raiz,
            EquipamentosResponse estado,
            HashSet<long> novos = null,
            HashSet<long> pendentes = null,
            bool ocupado = false
        )
        {
            var painel = new PainelInventario(raiz);
            painel.Mostrar(estado, novos, pendentes, ocupado);
            return painel;
        }

        [Test]
        public void DesenhaUmaCasaPorCapacidadeDoServidorEOContador()
        {
            var raiz = NovaRaiz();
            var estado = new EquipamentosResponse
            {
                Capacidade = new CapacidadeBolsa { Abas = 1, CasasPorAba = 24 },
                Inventario = new[] { Entrada(1, 107, 0), Entrada(2, 101, 5) },
            };
            Montar(raiz, estado);

            Assert.AreEqual(
                24,
                raiz.Query<VisualElement>(className: EstiloItens.Slot)
                    .Where(e => e.name.StartsWith("casa-"))
                    .ToList()
                    .Count
            );
            Assert.AreEqual("2/24", raiz.Q<Label>("itens.contador").text);
            Assert.IsFalse(
                raiz.Q<Label>("itens.contador").ClassListContains(EstiloItens.ContadorCheio)
            );
        }

        [Test]
        public void ContadorFicaEmDestaqueComABolsaCheia()
        {
            var raiz = NovaRaiz();
            var itens = new List<EntradaInventario>();
            for (var i = 0; i < 4; i++)
                itens.Add(Entrada(i + 1, 107, i));
            Montar(
                raiz,
                new EquipamentosResponse
                {
                    Capacidade = new CapacidadeBolsa { Abas = 1, CasasPorAba = 4 },
                    Inventario = itens.ToArray(),
                }
            );
            Assert.IsTrue(
                raiz.Q<Label>("itens.contador").ClassListContains(EstiloItens.ContadorCheio)
            );
        }

        [Test]
        public void MostraAbaDoEstadoComOIconeDoTipo()
        {
            var raiz = NovaRaiz();
            Montar(raiz, new EquipamentosResponse { Inventario = new[] { Entrada(1, 107, 3) } });
            var casa = raiz.Q<VisualElement>("casa-0-3");
            Assert.AreEqual("", casa.Q<Label>().text);
            Assert.IsNotNull(IconeDe(casa));
            // O anel 107 do catálogo tem Vitalidade 6 na faixa de 0 a 100: tier D, letra no canto.
            Assert.IsTrue(casa.ClassListContains(EstiloItens.Tier(TierItem.D)));
            Assert.AreEqual("D", casa.Q<Label>(className: EstiloItens.LetraTier).text);
        }

        [Test]
        public void EncaixesVaziosMostramASilhuetaDoTipo()
        {
            var raiz = NovaRaiz();
            Montar(raiz, new EquipamentosResponse());
            var capacete = raiz.Q<VisualElement>("encaixe-Capacete");
            Assert.IsTrue(capacete.ClassListContains(EstiloItens.SlotVazio));
            Assert.IsNotNull(IconeDe(capacete));
            Assert.NotNull(raiz.Q<VisualElement>("encaixe-Secundaria2"));
        }

        [Test]
        public void ItemEquipadoApareceNoEncaixeEFicaForaDaBolsa()
        {
            var raiz = NovaRaiz();
            var vestido = Entrada(7, 101, 0);
            vestido.Posicao = PosicaoEquipamento.Capacete;
            vestido.Aba = RegrasItens.SemCasa;
            vestido.Casa = RegrasItens.SemCasa;
            Montar(raiz, new EquipamentosResponse { Equipados = new[] { vestido } });
            Assert.IsFalse(
                raiz.Q<VisualElement>("encaixe-Capacete").ClassListContains(EstiloItens.SlotVazio)
            );
            Assert.AreEqual("0/40", raiz.Q<Label>("itens.contador").text);
        }

        [Test]
        public void ItemNovoTemSeloEPendenteFicaSemitransparente()
        {
            var raiz = NovaRaiz();
            Montar(
                raiz,
                new EquipamentosResponse
                {
                    Inventario = new[] { Entrada(1, 107, 0), Entrada(2, 101, 1) },
                },
                novos: new HashSet<long> { 1 },
                pendentes: new HashSet<long> { 2 }
            );
            Assert.NotNull(
                raiz.Q<VisualElement>("casa-0-0").Q<Label>(className: EstiloItens.SeloNovo)
            );
            Assert.IsNull(
                raiz.Q<VisualElement>("casa-0-1").Q<Label>(className: EstiloItens.SeloNovo)
            );
            Assert.IsTrue(
                raiz.Q<VisualElement>("casa-0-1").ClassListContains(EstiloItens.SlotPendente)
            );
            Assert.IsFalse(
                raiz.Q<VisualElement>("casa-0-0").ClassListContains(EstiloItens.SlotPendente)
            );
        }

        [Test]
        public void ResumoListaOsBonusDoEstado()
        {
            var raiz = NovaRaiz();
            Montar(
                raiz,
                new EquipamentosResponse
                {
                    Bonus = new[]
                    {
                        new AtributoItem { Status = StatusItem.Forca, Valor = 12 },
                    },
                }
            );
            var textos = raiz.Query<Label>(className: EstiloItens.ResumoBonus).ToList();
            Assert.AreEqual(1, textos.Count);
            StringAssert.Contains("Força +12", textos[0].text);
        }

        [Test]
        public void ConfirmacaoDeDescarteSoEnviaDepoisDoConfirmar()
        {
            var raiz = NovaRaiz();
            var painel = Montar(raiz, new EquipamentosResponse());
            var confirmou = false;
            painel.PedirConfirmacaoDescarte(Entrada(1, 107, 0), () => confirmou = true, null);

            var confirmar = raiz.Query<Button>().Where(b => b.text == "Confirmar descarte").First();
            Assert.NotNull(confirmar);
            Assert.IsFalse(confirmou);
            Click(confirmar);
            Assert.IsTrue(confirmou);
            Assert.AreEqual(
                0,
                raiz.Query<Button>().Where(b => b.text == "Confirmar descarte").ToList().Count
            );
        }

        [Test]
        public void CancelarADescarteNaoConfirma()
        {
            var raiz = NovaRaiz();
            var painel = Montar(raiz, new EquipamentosResponse());
            var confirmou = false;
            var cancelou = false;
            painel.PedirConfirmacaoDescarte(
                Entrada(1, 107, 0),
                () => confirmou = true,
                () => cancelou = true
            );
            Click(raiz.Query<Button>().Where(b => b.text == "Cancelar").First());
            Assert.IsFalse(confirmou);
            Assert.IsTrue(cancelou);
        }

        [Test]
        public void ConjuntoAtivoFicaDestacadoEOOutroPodeSerEscolhido()
        {
            var raiz = NovaRaiz();
            var painel = Montar(raiz, new EquipamentosResponse { ConjuntoAtivo = 2 });
            var escolhido = 0;
            painel.ConjuntoPedido += n => escolhido = n;
            var botoes = raiz.Query<Button>(className: EstiloItens.ConjuntoRotulo).ToList();
            Assert.AreEqual(2, botoes.Count);
            Assert.IsFalse(botoes[0].ClassListContains(EstiloItens.AbaAtiva));
            Assert.IsTrue(botoes[1].ClassListContains(EstiloItens.AbaAtiva));
            Assert.IsTrue(botoes[0].enabledSelf);
            Assert.IsFalse(botoes[1].enabledSelf);
            Click(botoes[0]);
            Assert.AreEqual(1, escolhido);
        }

        [Test]
        public void ComPedidoEmAndamentoNadaDeInteracaoEstaHabilitado()
        {
            var raiz = NovaRaiz();
            Montar(raiz, new EquipamentosResponse(), ocupado: true);
            foreach (
                var botao in raiz.Query<Button>(className: EstiloItens.ConjuntoRotulo).ToList()
            )
                Assert.IsFalse(botao.enabledSelf);
            Assert.IsFalse(
                raiz.Query<Button>().Where(b => b.text == "Bancada").First().enabledSelf
            );
        }

        private static Sprite IconeDe(VisualElement slot) =>
            slot.Q<VisualElement>(className: EstiloItens.Icone).style.backgroundImage.value.sprite;

        private static void Click(Button botao)
        {
            using (var evento = NavigationSubmitEvent.GetPooled())
            {
                evento.target = botao;
                botao.SendEvent(evento);
            }
        }
    }
}
