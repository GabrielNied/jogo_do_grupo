using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Barras de vida do boss e do alvo comum no topo do HUD (<see cref="RegiaoDoHud.TopoCentro"/>).
    /// </summary>
    internal sealed class PainelBarraAlvoTopo
    {
        public const string ClasseColuna = "hud-alvo-coluna";
        public const string ClasseBarraBoss = "hud-barra-boss";
        public const string ClasseBarraAlvo = "hud-barra-alvo";
        public const string ClasseNome = "hud-alvo-nome";
        public const string ClasseTrilho = "hud-alvo-trilho";
        public const string ClassePreenchimento = "hud-alvo-preenchimento";
        public const string ClassePercentual = "hud-alvo-percentual";

        private readonly VisualElement _coluna;
        private readonly Bloco _boss;
        private readonly Bloco _alvo;

        public PainelBarraAlvoTopo(VisualElement regiao)
        {
            var estilo = Resources.Load<StyleSheet>(PainelDeInimigos.CaminhoEstilo);
            if (estilo != null)
            {
                regiao.styleSheets.Add(estilo);
            }

            _coluna = new VisualElement { name = "mobs.barra-alvo-topo" };
            _coluna.pickingMode = PickingMode.Ignore;
            _coluna.AddToClassList(ClasseColuna);

            _boss = new Bloco(ClasseBarraBoss);
            _alvo = new Bloco(ClasseBarraAlvo);

            _coluna.Add(_boss.Raiz);
            _coluna.Add(_alvo.Raiz);
            regiao.Add(_coluna);

            EsconderTudo();
        }

        public void Descartar()
        {
            _coluna.RemoveFromHierarchy();
        }

        public void Mostrar(InimigoNoClient boss, InimigoNoClient alvo)
        {
            if (boss != null)
            {
                _boss.Mostrar(boss);
            }
            else
            {
                _boss.Esconder();
            }

            if (alvo != null)
            {
                _alvo.Mostrar(alvo);
            }
            else
            {
                _alvo.Esconder();
            }

            _coluna.style.display =
                boss != null || alvo != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void EsconderTudo()
        {
            _boss.Esconder();
            _alvo.Esconder();
            _coluna.style.display = DisplayStyle.None;
        }

        private sealed class Bloco
        {
            private readonly Label _nome;
            private readonly Label _percentual;
            private readonly VisualElement _preenchimento;

            public Bloco(string classeBarra)
            {
                Raiz = new VisualElement();
                Raiz.pickingMode = PickingMode.Ignore;
                Raiz.AddToClassList(classeBarra);

                _nome = new Label();
                _nome.pickingMode = PickingMode.Ignore;
                _nome.AddToClassList(ClasseNome);

                var trilho = new VisualElement();
                trilho.pickingMode = PickingMode.Ignore;
                trilho.AddToClassList(ClasseTrilho);

                _preenchimento = new VisualElement();
                _preenchimento.pickingMode = PickingMode.Ignore;
                _preenchimento.AddToClassList(ClassePreenchimento);

                _percentual = new Label();
                _percentual.pickingMode = PickingMode.Ignore;
                _percentual.AddToClassList(ClassePercentual);

                trilho.Add(_preenchimento);
                Raiz.Add(_nome);
                Raiz.Add(trilho);
                Raiz.Add(_percentual);
            }

            public VisualElement Raiz { get; }

            public void Esconder() => Raiz.style.display = DisplayStyle.None;

            public void Mostrar(InimigoNoClient inimigo)
            {
                Raiz.style.display = DisplayStyle.Flex;
                _nome.text = inimigo.Rotulo;
                var fracao = inimigo.FracaoDeVida;
                _preenchimento.style.width = Length.Percent(fracao * 100f);
                _percentual.text = Mathf.RoundToInt(fracao * 100f) + "%";
            }
        }
    }
}
