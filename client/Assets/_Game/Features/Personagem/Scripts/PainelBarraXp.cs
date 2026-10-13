using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Barra compacta de XP no HUD (região inferior esquerda). Só reflete
    /// <see cref="ProgressoDoClient.Estado"/>; o servidor decide os números.
    /// </summary>
    internal sealed class PainelBarraXp
    {
        public const string CaminhoEstilo = EstiloPersonagem.Caminho;

        private readonly VisualElement _raiz;
        private readonly Label _rotulo;
        private readonly VisualElement _preenchimento;

        public PainelBarraXp(VisualElement regiao)
        {
            _raiz = new VisualElement { name = "personagem.barra-xp" };
            EstiloPersonagem.Aplicar(_raiz);
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList(EstiloPersonagem.HudBarraXp);

            _rotulo = new Label();
            _rotulo.pickingMode = PickingMode.Ignore;
            _rotulo.AddToClassList(EstiloPersonagem.HudBarraXpRotulo);

            var trilho = new VisualElement();
            trilho.pickingMode = PickingMode.Ignore;
            trilho.AddToClassList(EstiloPersonagem.HudBarraXpTrilho);

            _preenchimento = new VisualElement();
            _preenchimento.pickingMode = PickingMode.Ignore;
            _preenchimento.AddToClassList(EstiloPersonagem.HudBarraXpPreenchimento);

            trilho.Add(_preenchimento);
            _raiz.Add(_rotulo);
            _raiz.Add(trilho);
            regiao.Insert(0, _raiz);

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
            _rotulo.text = TextosPersonagem.TextoDaBarraNoHud(progresso, noMaximo);
            _preenchimento.style.width = Length.Percent(
                noMaximo ? 100f : progresso.FracaoDaBarra * 100f
            );
        }
    }
}
