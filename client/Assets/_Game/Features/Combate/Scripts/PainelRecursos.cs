using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Quadro do personagem no alto à esquerda, como o "player frame" do World of Warcraft:
    /// retrato com o nível, nome e as barras de vida, mana e stamina com os números. Só mostra
    /// o que recebe; nome e nível vêm de <see cref="PerfilDoJogador"/>. Visual em
    /// <c>EstiloCombate.uss</c> e nos componentes do HUD do Core.
    /// </summary>
    internal sealed class PainelRecursos
    {
        public const string CaminhoEstilo = "UI/EstiloCombate";

        private readonly VisualElement _quadro;
        private readonly RetratoHud _retrato;
        private readonly Label _nome;
        private readonly BarraHud _vida;
        private readonly BarraHud _mana;
        private readonly BarraHud _stamina;

        public PainelRecursos(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }

            _quadro = new VisualElement { name = "combate.recursos" };
            _quadro.pickingMode = PickingMode.Ignore;
            _quadro.AddToClassList("recursos");
            if (raiz.name == "CombateInterface")
            {
                _quadro.AddToClassList("recursos-soltos");
            }

            _retrato = new RetratoHud();
            _quadro.Add(_retrato.Raiz);

            var coluna = new VisualElement { pickingMode = PickingMode.Ignore };
            coluna.AddToClassList("recursos-coluna");
            _nome = ComponentesHud.Texto(string.Empty, "recursos-nome");
            coluna.Add(_nome);

            _vida = new BarraHud("hud-barra-vida");
            _vida.Raiz.AddToClassList("recursos-barra-vida");
            _mana = new BarraHud("hud-barra-mana");
            _mana.Raiz.AddToClassList("recursos-barra-mana");
            _stamina = new BarraHud("hud-barra-stamina", fina: true);
            _stamina.Raiz.AddToClassList("recursos-barra-stamina");
            coluna.Add(_vida.Raiz);
            coluna.Add(_mana.Raiz);
            coluna.Add(_stamina.Raiz);
            _quadro.Add(coluna);

            raiz.Insert(0, _quadro);

            PerfilDoJogador.Mudou += AtualizarPerfil;
            _quadro.RegisterCallback<DetachFromPanelEvent>(_ =>
                PerfilDoJogador.Mudou -= AtualizarPerfil
            );
            AtualizarPerfil();
        }

        public void Mostrar(BarraRecurso vida, BarraRecurso stamina, BarraRecurso mana)
        {
            _vida.Definir(vida.Atual, vida.Maximo);
            _vida.Raiz.EnableInClassList(
                "hud-barra-vida-baixa",
                vida.Maximo > 0 && vida.Fracao <= ConfigCombateHud.FracaoVidaBaixa
            );
            _mana.Definir(mana.Atual, mana.Maximo);
            _stamina.Definir(stamina.Atual, stamina.Maximo, null);
            _quadro.EnableInClassList(
                "recursos-vida-baixa",
                vida.Maximo > 0 && vida.Fracao <= ConfigCombateHud.FracaoVidaBaixa
            );
        }

        private void AtualizarPerfil()
        {
            _nome.text = PerfilDoJogador.Nome;
            _retrato.Definir(
                PerfilDoJogador.Nome,
                PerfilDoJogador.Nivel,
                PerfilDoJogador.CorDaRaca(PerfilDoJogador.Raca)
            );
        }
    }

    /// <summary>Valores de leitura do quadro do personagem.</summary>
    internal static class ConfigCombateHud
    {
        /// <summary>Abaixo desta fração a vida fica em alerta (barra e moldura avermelhadas).</summary>
        public const float FracaoVidaBaixa = 0.3f;
    }
}
