using System;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Montarias;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Montarias.UI
{
    /// <summary>
    /// Componente de montaria no HUD (padrão Lumora): exibe o botão de invocação [T],
    /// a barra de progresso / espera durante a canalização e o botão de acesso à coleção [B].
    /// </summary>
    public sealed class BotaoMontariaHud : IDisposable
    {
        private readonly VisualElement _raiz;
        private readonly VisualElement _linhaBotoes;
        private readonly Button _botaoMontar;
        private readonly Label _rotuloMontar;
        private readonly Label _atalhoMontar;
        private readonly Button _botaoColecao;

        // Barra de espera / canalização de montaria
        private readonly VisualElement _painelInvocacao;
        private readonly Label _rotuloInvocacao;
        private readonly VisualElement _trilhoInvocacao;
        private readonly VisualElement _preenchimentoInvocacao;

        public BotaoMontariaHud(VisualElement regiao)
        {
            _raiz = new VisualElement { name = "montarias.hud-raiz" };
            _raiz.style.flexDirection = FlexDirection.Column;
            _raiz.style.alignItems = Align.FlexStart;
            _raiz.style.marginBottom = 8f;
            _raiz.style.marginRight = 8f;

            // Painel da Barra de Invocação (acima dos botões)
            _painelInvocacao = new VisualElement { name = "montarias.hud-invocacao" };
            _painelInvocacao.style.display = DisplayStyle.None;
            _painelInvocacao.style.marginBottom = 6f;
            _painelInvocacao.style.paddingLeft = 8f;
            _painelInvocacao.style.paddingRight = 8f;
            _painelInvocacao.style.paddingTop = 5f;
            _painelInvocacao.style.paddingBottom = 5f;
            _painelInvocacao.style.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            _painelInvocacao.style.borderLeftColor = new Color(0.85f, 0.65f, 0.25f, 0.8f);
            _painelInvocacao.style.borderRightColor = new Color(0.85f, 0.65f, 0.25f, 0.8f);
            _painelInvocacao.style.borderTopColor = new Color(0.85f, 0.65f, 0.25f, 0.8f);
            _painelInvocacao.style.borderBottomColor = new Color(0.85f, 0.65f, 0.25f, 0.8f);
            _painelInvocacao.style.borderLeftWidth = 1f;
            _painelInvocacao.style.borderRightWidth = 1f;
            _painelInvocacao.style.borderTopWidth = 1f;
            _painelInvocacao.style.borderBottomWidth = 1f;
            _painelInvocacao.style.borderTopLeftRadius = 6f;
            _painelInvocacao.style.borderTopRightRadius = 6f;
            _painelInvocacao.style.borderBottomLeftRadius = 6f;
            _painelInvocacao.style.borderBottomRightRadius = 6f;

            _rotuloInvocacao = new Label("Invocando montaria...");
            _rotuloInvocacao.style.fontSize = 11f;
            _rotuloInvocacao.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rotuloInvocacao.style.color = new Color(0.95f, 0.90f, 0.70f);
            _rotuloInvocacao.style.marginBottom = 4f;
            _painelInvocacao.Add(_rotuloInvocacao);

            _trilhoInvocacao = new VisualElement { name = "TrilhoInvocacao" };
            _trilhoInvocacao.AddToClassList(EstiloCore.BarraCarregamento);
            _trilhoInvocacao.style.width = 170f;
            _trilhoInvocacao.style.height = 8f;
            _trilhoInvocacao.style.backgroundColor = new Color(0.06f, 0.08f, 0.10f, 0.9f);

            _preenchimentoInvocacao = new VisualElement { name = "PreenchimentoInvocacao" };
            _preenchimentoInvocacao.AddToClassList(EstiloCore.BarraCarregamentoPreenchimento);
            _preenchimentoInvocacao.style.width = Length.Percent(0f);
            _preenchimentoInvocacao.style.backgroundColor = new Color(0.90f, 0.70f, 0.25f);
            _trilhoInvocacao.Add(_preenchimentoInvocacao);
            _painelInvocacao.Add(_trilhoInvocacao);

            _raiz.Add(_painelInvocacao);

            // Linha com os botões de ação
            _linhaBotoes = new VisualElement();
            _linhaBotoes.style.flexDirection = FlexDirection.Row;
            _linhaBotoes.style.alignItems = Align.Center;

            // Botão principal de montar/desmontar
            _botaoMontar = new Button();
            _botaoMontar.style.flexDirection = FlexDirection.Row;
            _botaoMontar.style.alignItems = Align.Center;
            _botaoMontar.style.paddingLeft = 10f;
            _botaoMontar.style.paddingRight = 10f;
            _botaoMontar.style.paddingTop = 6f;
            _botaoMontar.style.paddingBottom = 6f;
            _botaoMontar.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.88f);
            _botaoMontar.style.borderLeftColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botaoMontar.style.borderRightColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botaoMontar.style.borderTopColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botaoMontar.style.borderBottomColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            _botaoMontar.style.borderTopLeftRadius = 6f;
            _botaoMontar.style.borderTopRightRadius = 6f;
            _botaoMontar.style.borderBottomLeftRadius = 6f;
            _botaoMontar.style.borderBottomRightRadius = 6f;
            _botaoMontar.style.borderLeftWidth = 1f;
            _botaoMontar.style.borderRightWidth = 1f;
            _botaoMontar.style.borderTopWidth = 1f;
            _botaoMontar.style.borderBottomWidth = 1f;

            _rotuloMontar = new Label("Montar");
            _rotuloMontar.style.color = new Color(0.92f, 0.94f, 0.96f);
            _rotuloMontar.style.fontSize = 12f;
            _rotuloMontar.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rotuloMontar.style.marginRight = 6f;

            _atalhoMontar = new Label("[T]");
            _atalhoMontar.style.color = new Color(0.65f, 0.75f, 0.88f);
            _atalhoMontar.style.fontSize = 11f;

            _botaoMontar.Add(_rotuloMontar);
            _botaoMontar.Add(_atalhoMontar);
            _botaoMontar.clicked += AoClicarMontar;
            _linhaBotoes.Add(_botaoMontar);

            // Botão secundário para abrir a coleção de montarias
            _botaoColecao = new Button();
            _botaoColecao.style.flexDirection = FlexDirection.Row;
            _botaoColecao.style.alignItems = Align.Center;
            _botaoColecao.style.marginLeft = 6f;
            _botaoColecao.style.paddingLeft = 8f;
            _botaoColecao.style.paddingRight = 8f;
            _botaoColecao.style.paddingTop = 6f;
            _botaoColecao.style.paddingBottom = 6f;
            _botaoColecao.style.backgroundColor = new Color(0.14f, 0.16f, 0.22f, 0.85f);
            _botaoColecao.style.borderLeftColor = new Color(0.30f, 0.35f, 0.46f, 0.6f);
            _botaoColecao.style.borderRightColor = new Color(0.30f, 0.35f, 0.46f, 0.6f);
            _botaoColecao.style.borderTopColor = new Color(0.30f, 0.35f, 0.46f, 0.6f);
            _botaoColecao.style.borderBottomColor = new Color(0.30f, 0.35f, 0.46f, 0.6f);
            _botaoColecao.style.borderTopLeftRadius = 6f;
            _botaoColecao.style.borderTopRightRadius = 6f;
            _botaoColecao.style.borderBottomLeftRadius = 6f;
            _botaoColecao.style.borderBottomRightRadius = 6f;
            _botaoColecao.style.borderLeftWidth = 1f;
            _botaoColecao.style.borderRightWidth = 1f;
            _botaoColecao.style.borderTopWidth = 1f;
            _botaoColecao.style.borderBottomWidth = 1f;

            var rotuloColecao = new Label("Coleção");
            rotuloColecao.style.color = new Color(0.85f, 0.88f, 0.94f);
            rotuloColecao.style.fontSize = 11.5f;
            rotuloColecao.style.marginRight = 4f;

            var atalhoColecao = new Label("[B]");
            atalhoColecao.style.color = new Color(0.85f, 0.70f, 0.30f);
            atalhoColecao.style.fontSize = 11f;

            _botaoColecao.Add(rotuloColecao);
            _botaoColecao.Add(atalhoColecao);
            _botaoColecao.clicked += AoClicarColecao;
            _linhaBotoes.Add(_botaoColecao);

            _raiz.Add(_linhaBotoes);
            regiao.Add(_raiz);

            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EstadoMudou += AtualizarEstado;
                ControleDeMontaria.Instancia.InvocacaoProgresso += AtualizarInvocacao;
                AtualizarEstado(ControleDeMontaria.Instancia.MontadoLocal);
            }
        }

        private void AoClicarMontar()
        {
            ControleDeMontaria.Instancia?.AlternarMontaria();
        }

        private void AoClicarColecao()
        {
            if (JanelasDoJogo.EstaRegistrada(JanelaMontarias.Chave))
            {
                if (JanelasDoJogo.ChaveAberta == JanelaMontarias.Chave)
                {
                    JanelasDoJogo.Fechar();
                }
                else
                {
                    JanelasDoJogo.Abrir(JanelaMontarias.Chave);
                }
            }
        }

        public void AtualizarInvocacao(bool invocando, float tempoAtual, float duracao)
        {
            if (invocando)
            {
                var tipo =
                    ControleDeMontaria.Instancia != null
                        ? ControleDeMontaria.Instancia.MontariaEquipada
                        : RegrasMontaria.TipoPadrao;
                var nome = RegrasMontaria.ObterNome(tipo);

                _painelInvocacao.style.display = DisplayStyle.Flex;
                _rotuloInvocacao.text = $"Invocando {nome}...";

                var progresso = Mathf.Clamp01(tempoAtual / Mathf.Max(0.01f, duracao));
                _preenchimentoInvocacao.style.width = Length.Percent(progresso * 100f);

                _rotuloMontar.text = "Cancelar";
                _botaoMontar.style.backgroundColor = new Color(0.28f, 0.14f, 0.14f, 0.95f);
                _botaoMontar.style.borderLeftColor = new Color(0.85f, 0.35f, 0.35f, 0.9f);
                _botaoMontar.style.borderRightColor = new Color(0.85f, 0.35f, 0.35f, 0.9f);
                _botaoMontar.style.borderTopColor = new Color(0.85f, 0.35f, 0.35f, 0.9f);
                _botaoMontar.style.borderBottomColor = new Color(0.85f, 0.35f, 0.35f, 0.9f);
            }
            else
            {
                _painelInvocacao.style.display = DisplayStyle.None;
                _preenchimentoInvocacao.style.width = Length.Percent(0f);
                AtualizarEstado(
                    ControleDeMontaria.Instancia != null
                        && ControleDeMontaria.Instancia.MontadoLocal
                );
            }
        }

        public void AtualizarEstado(bool montado)
        {
            if (ControleDeMontaria.Instancia != null && ControleDeMontaria.Instancia.Invocando)
            {
                return;
            }

            if (montado)
            {
                _rotuloMontar.text = "Desmontar";
                _botaoMontar.style.backgroundColor = new Color(0.24f, 0.18f, 0.12f, 0.95f);
                _botaoMontar.style.borderLeftColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                _botaoMontar.style.borderRightColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                _botaoMontar.style.borderTopColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
                _botaoMontar.style.borderBottomColor = new Color(0.85f, 0.65f, 0.25f, 0.9f);
            }
            else
            {
                _rotuloMontar.text = "Montar";
                _botaoMontar.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.88f);
                _botaoMontar.style.borderLeftColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
                _botaoMontar.style.borderRightColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
                _botaoMontar.style.borderTopColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
                _botaoMontar.style.borderBottomColor = new Color(0.35f, 0.40f, 0.50f, 0.6f);
            }
        }

        public void Dispose()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EstadoMudou -= AtualizarEstado;
                ControleDeMontaria.Instancia.InvocacaoProgresso -= AtualizarInvocacao;
            }

            _raiz.RemoveFromHierarchy();
        }
    }
}
