using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Montarias.UI
{
    /// <summary>
    /// Botão de montaria no HUD (padrão Lumora): exibe o estado atual (Montar / Desmontar),
    /// a tecla de atalho [T] e permite alternar com um clique.
    /// </summary>
    public sealed class BotaoMontariaHud : IDisposable
    {
        private readonly VisualElement _raiz;
        private readonly Button _botao;
        private readonly Label _rotulo;
        private readonly Label _atalho;

        public BotaoMontariaHud(VisualElement regiao)
        {
            _raiz = new VisualElement { name = "montarias.hud-botao" };
            _raiz.style.flexDirection = FlexDirection.Row;
            _raiz.style.alignItems = Align.Center;
            _raiz.style.marginBottom = 8f;
            _raiz.style.marginRight = 8f;

            _botao = new Button();
            _botao.style.flexDirection = FlexDirection.Row;
            _botao.style.alignItems = Align.Center;
            _botao.style.paddingLeft = 10f;
            _botao.style.paddingRight = 10f;
            _botao.style.paddingTop = 6f;
            _botao.style.paddingBottom = 6f;
            _botao.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.88f);
            _botao.style.borderLeftColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botao.style.borderRightColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botao.style.borderTopColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botao.style.borderBottomColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botao.style.borderTopLeftRadius = 6f;
            _botao.style.borderTopRightRadius = 6f;
            _botao.style.borderBottomLeftRadius = 6f;
            _botao.style.borderBottomRightRadius = 6f;
            _botao.style.borderLeftWidth = 1f;
            _botao.style.borderRightWidth = 1f;
            _botao.style.borderTopWidth = 1f;
            _botao.style.borderBottomWidth = 1f;

            _rotulo = new Label("Montar");
            _rotulo.style.color = new Color(0.92f, 0.94f, 0.96f);
            _rotulo.style.fontSize = 12f;
            _rotulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rotulo.style.marginRight = 6f;

            _atalho = new Label("[T]");
            _atalho.style.color = new Color(0.65f, 0.75f, 0.88f);
            _atalho.style.fontSize = 11f;

            _botao.Add(_rotulo);
            _botao.Add(_atalho);
            _botao.clicked += AoClicar;

            _raiz.Add(_botao);
            regiao.Add(_raiz);

            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EstadoMudou += AtualizarEstado;
                AtualizarEstado(ControleDeMontaria.Instancia.MontadoLocal);
            }
        }

        private void AoClicar()
        {
            ControleDeMontaria.Instancia?.AlternarMontaria();
        }

        public void AtualizarEstado(bool montado)
        {
            if (montado)
            {
                _rotulo.text = "Desmontar";
                _botao.style.backgroundColor = new Color(0.24f, 0.18f, 0.12f, 0.95f);
                _botao.style.borderLeftColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                _botao.style.borderRightColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                _botao.style.borderTopColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                _botao.style.borderBottomColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
            }
            else
            {
                _rotulo.text = "Montar";
                _botao.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.88f);
                _botao.style.borderLeftColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
                _botao.style.borderRightColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
                _botao.style.borderTopColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
                _botao.style.borderBottomColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            }
        }

        public void Dispose()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EstadoMudou -= AtualizarEstado;
            }

            _raiz.RemoveFromHierarchy();
        }
    }
}
