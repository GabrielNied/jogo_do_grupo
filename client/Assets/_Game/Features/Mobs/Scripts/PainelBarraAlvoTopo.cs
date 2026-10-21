using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Quadro do alvo e barra do chefe no topo, ao centro (<see cref="RegiaoDoHud.TopoCentro"/>),
    /// como o "target frame" do World of Warcraft: placa com o nome (vermelho para hostil,
    /// dourado para chefe), o nível e a vida com números e porcentagem. O chefe ganha a placa
    /// larga com caveira. O alvo é o inimigo sob a mira ou o último atingido (sem tab target).
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

            _boss = new Bloco(ClasseBarraBoss, true);
            _alvo = new Bloco(ClasseBarraAlvo, false);

            _coluna.Add(_boss.Raiz);
            _coluna.Add(_alvo.Raiz);
            regiao.Insert(0, _coluna);

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
            private readonly Label _nivel;
            private readonly Label _percentual;
            private readonly BarraHud _barra;
            private readonly bool _chefe;
            private long _ultimoId;

            public Bloco(string classeBarra, bool chefe)
            {
                _chefe = chefe;
                Raiz = new VisualElement();
                Raiz.pickingMode = PickingMode.Ignore;
                Raiz.AddToClassList(classeBarra);

                var cabecalho = new VisualElement { pickingMode = PickingMode.Ignore };
                cabecalho.AddToClassList("hud-alvo-cabecalho");
                if (chefe)
                {
                    cabecalho.Add(
                        ComponentesHud.Imagem(
                            ComponentesHud.Icone("Icones", "chefe"),
                            "hud-alvo-caveira"
                        )
                    );
                }

                _nome = ComponentesHud.Texto(string.Empty, ClasseNome);
                _nivel = ComponentesHud.Texto(string.Empty, "hud-alvo-nivel");
                cabecalho.Add(_nome);
                cabecalho.Add(_nivel);

                _barra = new BarraHud(chefe ? "hud-barra-inimigo" : "hud-barra-inimigo");
                _barra.Raiz.AddToClassList(ClasseTrilho);
                _percentual = ComponentesHud.Texto(string.Empty, ClassePercentual);
                _barra.Raiz.Add(_percentual);

                Raiz.Add(cabecalho);
                Raiz.Add(_barra.Raiz);
            }

            public VisualElement Raiz { get; }

            public void Esconder() => Raiz.style.display = DisplayStyle.None;

            public void Mostrar(InimigoNoClient inimigo)
            {
                Raiz.style.display = DisplayStyle.Flex;
                var ficha = CatalogoCriaturas.Obter(inimigo.CriaturaId);
                _nome.text = ficha != null ? ficha.Nome : inimigo.Rotulo;
                var nivel = inimigo.Nivel > 0 ? inimigo.Nivel : ficha?.Nivel ?? 0;
                _nivel.text = nivel > 0 ? "Nv " + nivel : string.Empty;
                Raiz.EnableInClassList("hud-alvo-elite", inimigo.Elite && !_chefe);
                var fracao = inimigo.FracaoDeVida;
                _barra.Definir(
                    inimigo.Vida,
                    inimigo.VidaMaxima,
                    inimigo.Vida + " / " + inimigo.VidaMaxima
                );
                _percentual.text = Mathf.RoundToInt(fracao * 100f) + "%";
                if (_ultimoId != inimigo.InimigoId)
                {
                    _ultimoId = inimigo.InimigoId;
                }
            }
        }
    }
}
