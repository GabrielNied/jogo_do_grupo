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

            _vida = new Barra(caixa, "barra-vida");
            _stamina = new Barra(caixa, "barra-stamina");
            _mana = new Barra(caixa, "barra-mana");
            raiz.Add(caixa);
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
            private readonly Label _texto;

            public Barra(VisualElement pai, string classeDaCor)
            {
                var barra = new VisualElement();
                barra.pickingMode = PickingMode.Ignore;
                barra.AddToClassList("recurso-barra");

                _preenchimento = new VisualElement();
                _preenchimento.pickingMode = PickingMode.Ignore;
                _preenchimento.AddToClassList("recurso-preenchimento");
                _preenchimento.AddToClassList(classeDaCor);

                _texto = new Label();
                _texto.pickingMode = PickingMode.Ignore;
                _texto.AddToClassList("recurso-texto");

                barra.Add(_preenchimento);
                barra.Add(_texto);
                pai.Add(barra);
            }

            public void Mostrar(BarraRecurso dados)
            {
                _preenchimento.style.width = Length.Percent(dados.Fracao * 100f);
                _texto.text = dados.Texto;
            }
        }
    }
}
