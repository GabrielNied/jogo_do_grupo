using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Barra de experiência na borda de baixo da tela, de ponta a ponta, como a do World of
    /// Warcraft: 20 marcas, preenchimento roxo e o texto "Nível N · atual / total (x%)" no
    /// meio. Mantém o nível do <see cref="PerfilDoJogador"/> em dia para o quadro do personagem.
    /// </summary>
    internal sealed class PainelBarraXp
    {
        public const string CaminhoEstilo = EstiloPersonagem.Caminho;
        private const int Marcas = 20;

        private readonly VisualElement _raiz;
        private readonly Label _rotulo;
        private readonly VisualElement _preenchimento;

        public PainelBarraXp(VisualElement regiao)
        {
            _raiz = new VisualElement { name = "personagem.barra-xp" };
            EstiloPersonagem.Aplicar(_raiz);
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList(EstiloPersonagem.HudBarraXp);

            var trilho = new VisualElement();
            trilho.pickingMode = PickingMode.Ignore;
            trilho.AddToClassList(EstiloPersonagem.HudBarraXpTrilho);

            _preenchimento = new VisualElement();
            _preenchimento.pickingMode = PickingMode.Ignore;
            _preenchimento.AddToClassList(EstiloPersonagem.HudBarraXpPreenchimento);
            trilho.Add(_preenchimento);

            for (var i = 1; i < Marcas; i++)
            {
                var marca = new VisualElement { pickingMode = PickingMode.Ignore };
                marca.AddToClassList("personagem-hud-barra-xp-marca");
                marca.style.left = Length.Percent(i * 100f / Marcas);
                trilho.Add(marca);
            }

            _rotulo = new Label();
            _rotulo.pickingMode = PickingMode.Ignore;
            _rotulo.AddToClassList(EstiloPersonagem.HudBarraXpRotulo);
            trilho.Add(_rotulo);

            _raiz.Add(trilho);
            regiao.Add(_raiz);

            ProgressoDoClient.Estado.Mudou += Atualizar;
            Atualizar();
        }

        public void Descartar()
        {
            ProgressoDoClient.Estado.Mudou -= Atualizar;
            _raiz.RemoveFromHierarchy();
        }

        private void Atualizar()
        {
            var progresso = ProgressoDoClient.Estado;
            var noMaximo = progresso.Nivel >= RegrasExperiencia.NivelMaximo;
            var fracao = noMaximo ? 1f : progresso.FracaoDaBarra;
            var texto = TextosPersonagem.TextoDaBarraNoHud(progresso, noMaximo);
            if (!noMaximo && progresso.XpDoNivel > 0)
                texto += "  (" + Mathf.FloorToInt(fracao * 100f) + "%)";
            _rotulo.text = texto;
            _preenchimento.style.width = Length.Percent(fracao * 100f);
            // Sem dados do servidor ainda (XpDoNivel 0), o nível do perfil (da seleção) vale.
            if (progresso.XpDoNivel > 0 || noMaximo)
                PerfilDoJogador.DefinirNivel(progresso.Nivel);
        }
    }
}
