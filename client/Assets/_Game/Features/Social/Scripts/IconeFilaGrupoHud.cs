using System;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Ícone no HUD perto do minimapa que indica que o jogador está na fila do Dungeon Finder.
    /// Fica visível apenas enquanto <see cref="EstadoDaFilaDeGrupo.EmFila"/> for verdadeiro.
    /// Exibe animação giratória, tempo de fila e permite cancelar a busca ou abrir a janela social.
    /// </summary>
    internal sealed class IconeFilaGrupoHud : IDisposable
    {
        private readonly VisualElement _raiz;
        private readonly EstadoDaFilaDeGrupo _estado;
        private readonly VisualElement _corpo;
        private readonly Label _iconeGiro;
        private readonly Label _rotuloTempo;
        private readonly Button _botaoCancelar;
        private float _angulo;

        public VisualElement Elemento => _raiz;

        public IconeFilaGrupoHud(VisualElement pai, EstadoDaFilaDeGrupo estado = null)
        {
            if (pai == null)
            {
                throw new ArgumentNullException(nameof(pai));
            }

            _estado = estado ?? FilaDeGrupoDoClient.Estado;

            _raiz = new VisualElement { name = "social.fila-hud" };
            _raiz.AddToClassList("social-fila-hud");
            _raiz.pickingMode = PickingMode.Position;
            _raiz.style.display = DisplayStyle.None;

            _corpo = new VisualElement { name = "social.fila-hud-corpo" };
            _corpo.AddToClassList("social-fila-hud-corpo");
            _corpo.pickingMode = PickingMode.Position;
            _corpo.RegisterCallback<ClickEvent>(AoClicarCorpo);

            _iconeGiro = new Label("🌀") { name = "social.fila-hud-icone" };
            _iconeGiro.AddToClassList("social-fila-hud-icone");
            _iconeGiro.pickingMode = PickingMode.Ignore;
            _corpo.Add(_iconeGiro);

            _rotuloTempo = new Label("00:00") { name = "social.fila-hud-tempo" };
            _rotuloTempo.AddToClassList("social-fila-hud-tempo");
            _rotuloTempo.pickingMode = PickingMode.Ignore;
            _corpo.Add(_rotuloTempo);

            _raiz.Add(_corpo);

            _botaoCancelar = new Button(AoClicarCancelar)
            {
                text = "✕",
                name = "social.fila-hud-cancelar",
            };
            _botaoCancelar.AddToClassList("social-fila-hud-cancelar");
            _botaoCancelar.pickingMode = PickingMode.Position;
            _botaoCancelar.tooltip = "Sair da fila";
            _raiz.Add(_botaoCancelar);

            pai.Add(_raiz);

            _estado.Mudou += Atualizar;
            Atualizar();
        }

        public void AtualizarRotacao(float deltaTempo)
        {
            if (!_estado.EmFila || _raiz.style.display == DisplayStyle.None)
            {
                return;
            }

            _angulo = (_angulo + deltaTempo * 180f) % 360f;
            _iconeGiro.style.rotate = new Rotate(Angle.Degrees(_angulo));
        }

        public void Dispose()
        {
            _estado.Mudou -= Atualizar;
            _raiz?.RemoveFromHierarchy();
        }

        private void AoClicarCorpo(ClickEvent evt)
        {
            JanelaGrupo.AbrirNaFila();
        }

        private void AoClicarCancelar()
        {
            _estado.Sair();
        }

        public void Atualizar()
        {
            if (_estado.EmFila)
            {
                _raiz.style.display = DisplayStyle.Flex;
                var minutos = _estado.SegundosNaFila / 60;
                var segundos = _estado.SegundosNaFila % 60;
                _rotuloTempo.text = $"{minutos:D2}:{segundos:D2}";

                var papelStr = _estado.Papel switch
                {
                    PapelNoGrupo.Tank => "Tanque",
                    PapelNoGrupo.Healer => "Healer",
                    PapelNoGrupo.Dps => "DPS",
                    _ => "DPS",
                };

                var dica =
                    $"Dungeon Finder ({papelStr})\nTanque: {_estado.Tanques}/1 | Healer: {_estado.Healers}/1 | DPS: {_estado.Dps}/3\nClique para abrir a janela.";
                _corpo.tooltip = dica;
                _raiz.tooltip = dica;
            }
            else
            {
                _raiz.style.display = DisplayStyle.None;
            }
        }
    }
}
