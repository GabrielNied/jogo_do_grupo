using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Exibição dos efeitos de status ativos e tempo de imunidade no HUD (UI Toolkit).
    /// Oculta automaticamente quando não há nenhum efeito ativo (#171, combate.md).
    /// </summary>
    internal sealed class PainelEfeitosDeStatus
    {
        private readonly VisualElement _raiz;
        private readonly VisualElement _pai;

        public VisualElement Raiz => _raiz;

        public PainelEfeitosDeStatus(VisualElement pai)
        {
            _pai = pai;
            var estilo = Resources.Load<StyleSheet>(PainelRecursos.CaminhoEstilo);
            if (estilo != null && !pai.styleSheets.Contains(estilo))
            {
                pai.styleSheets.Add(estilo);
            }

            _raiz = new VisualElement { name = "combate.status" };
            _raiz.pickingMode = PickingMode.Ignore;
            _raiz.AddToClassList("painel-status");
            _raiz.style.display = DisplayStyle.None;

            pai.Add(_raiz);
        }

        public void Atualizar(IReadOnlyList<ItemDeStatus> efeitos, float imunidadeRestante)
        {
            var temEfeitos = efeitos != null && efeitos.Count > 0;
            var temImunidade = imunidadeRestante > 0f;

            if (!temEfeitos && !temImunidade)
            {
                _raiz.style.display = DisplayStyle.None;
                _raiz.Clear();
                return;
            }

            _raiz.style.display = DisplayStyle.Flex;
            _raiz.Clear();

            if (temEfeitos)
            {
                for (int i = 0; i < efeitos.Count; i++)
                {
                    var e = efeitos[i];
                    var item = new VisualElement();
                    item.pickingMode = PickingMode.Ignore;
                    item.AddToClassList("item-status");

                    var rotulo = new Label(e.Nome + (e.Acumulos > 1 ? " x" + e.Acumulos : ""));
                    rotulo.pickingMode = PickingMode.Ignore;
                    rotulo.AddToClassList("status-rotulo");

                    var tempo = new Label(
                        e.DuracaoRestante.ToString(
                            "0.0",
                            System.Globalization.CultureInfo.InvariantCulture
                        ) + "s"
                    );
                    tempo.pickingMode = PickingMode.Ignore;
                    tempo.AddToClassList("status-tempo");

                    item.Add(rotulo);
                    item.Add(tempo);
                    _raiz.Add(item);
                }
            }

            if (temImunidade)
            {
                var itemImune = new VisualElement();
                itemImune.pickingMode = PickingMode.Ignore;
                itemImune.AddToClassList("item-status");

                var rotuloImune = new Label("Imune a Atordoado");
                rotuloImune.pickingMode = PickingMode.Ignore;
                rotuloImune.AddToClassList("status-rotulo");

                var tempoImune = new Label(
                    imunidadeRestante.ToString(
                        "0.0",
                        System.Globalization.CultureInfo.InvariantCulture
                    ) + "s"
                );
                tempoImune.pickingMode = PickingMode.Ignore;
                tempoImune.AddToClassList("status-tempo");

                itemImune.Add(rotuloImune);
                itemImune.Add(tempoImune);
                _raiz.Add(itemImune);
            }
        }

        public void Descartar()
        {
            _raiz.RemoveFromHierarchy();
        }
    }
}
