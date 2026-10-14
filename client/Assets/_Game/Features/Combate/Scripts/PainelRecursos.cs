using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// As barras de vida, stamina e mana no canto da tela (UI Toolkit montada em código).
    /// Só mostra o que recebe; o visual está em <c>EstiloCombate.uss</c>.
    /// </summary>
    internal sealed class PainelRecursos
    {
        public const string CaminhoEstilo = "UI/EstiloCombate";

        private readonly Barra _vida;
        private readonly Barra _stamina;
        private readonly Barra _mana;

        public PainelRecursos(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }

            var caixa = new VisualElement { name = "combate.recursos" };
            caixa.pickingMode = PickingMode.Ignore;
            caixa.AddToClassList("recursos");
            if (raiz.name == "CombateInterface")
            {
                caixa.AddToClassList("recursos-soltos");
            }

            _vida = new Barra(caixa, "barra-vida");
            _stamina = new Barra(caixa, "barra-stamina");
            _mana = new Barra(caixa, "barra-mana");
            raiz.Insert(0, caixa);
        }

        public void Mostrar(BarraRecurso vida, BarraRecurso stamina, BarraRecurso mana)
        {
            _vida.Mostrar(vida);
            _stamina.Mostrar(stamina);
            _mana.Mostrar(mana);
        }

        private sealed class Barra
        {
            private readonly VisualElement _preenchimento;
            private readonly Label _rotulo;
            private readonly Label _valor;

            public Barra(VisualElement pai, string classeDaCor)
            {
                var linha = new VisualElement();
                linha.pickingMode = PickingMode.Ignore;
                linha.AddToClassList("recurso-linha");

                _rotulo = new Label();
                _rotulo.pickingMode = PickingMode.Ignore;
                _rotulo.AddToClassList("recurso-rotulo");

                var barra = new VisualElement();
                barra.pickingMode = PickingMode.Ignore;
                barra.AddToClassList("recurso-barra");

                _preenchimento = new VisualElement();
                _preenchimento.pickingMode = PickingMode.Ignore;
                _preenchimento.AddToClassList("recurso-preenchimento");
                _preenchimento.AddToClassList(classeDaCor);

                _valor = new Label();
                _valor.pickingMode = PickingMode.Ignore;
                _valor.AddToClassList("recurso-valor");

                barra.Add(_preenchimento);
                linha.Add(_rotulo);
                linha.Add(barra);
                linha.Add(_valor);
                pai.Add(linha);
            }

            public void Mostrar(BarraRecurso dados)
            {
                _preenchimento.style.width = Length.Percent(dados.Fracao * 100f);
                _rotulo.text = dados.Rotulo;
                _valor.text = dados.Atual + "/" + dados.Maximo;
            }
        }
    }
}
