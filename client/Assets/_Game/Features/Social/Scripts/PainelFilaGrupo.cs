using System;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Painel de interface para a fila automática de dungeon (Dungeon Finder).
    /// Permite escolher o papel (Tank, Healer, DPS), entrar na fila e ver a composição.
    /// </summary>
    internal sealed class PainelFilaGrupo : IDisposable
    {
        private readonly VisualElement _raiz;
        private readonly EstadoDaFilaDeGrupo _estado;
        private readonly Label _titulo;
        private readonly Label _aviso;
        private readonly VisualElement _secaoSelecao;
        private readonly VisualElement _secaoStatus;
        private readonly Label _tempoFila;
        private readonly Label _contadores;
        private readonly Button _botaoEntrar;
        private readonly Button _botaoSair;
        private readonly Button _botaoTanque;
        private readonly Button _botaoHealer;
        private readonly Button _botaoDps;

        private PapelNoGrupo _papelSelecionado = PapelNoGrupo.Dps;

        public PainelFilaGrupo(VisualElement raiz, EstadoDaFilaDeGrupo estado)
        {
            _raiz = raiz ?? throw new ArgumentNullException(nameof(raiz));
            _estado = estado ?? throw new ArgumentNullException(nameof(estado));

            _titulo = new Label("Procurar Grupo de Dungeon") { name = "lfg.titulo" };
            _titulo.AddToClassList(EstiloCore.Titulo);
            _raiz.Add(_titulo);

            var explicacao = new Label(
                "A fila automática emparelha 1 Tanque, 1 Healer e 3 DPS para formar um grupo de 5 jogadores."
            );
            explicacao.AddToClassList(EstiloCore.Texto);
            explicacao.style.marginBottom = 12;
            _raiz.Add(explicacao);

            // ── Seleção de papel ──────────────────────────────────────────────
            _secaoSelecao = new VisualElement { name = "lfg.secao-selecao" };
            var rotuloPapel = new Label("Selecione seu papel:");
            rotuloPapel.AddToClassList(EstiloCore.Subtitulo);
            _secaoSelecao.Add(rotuloPapel);

            var linhaPapeis = new VisualElement();
            linhaPapeis.AddToClassList(EstiloCore.LinhaBotoes);
            linhaPapeis.style.marginBottom = 12;

            _botaoTanque = new Button(() => SelecionarPapel(PapelNoGrupo.Tank))
            {
                text = "🛡️ Tanque",
            };
            _botaoTanque.AddToClassList(EstiloCore.Botao);
            linhaPapeis.Add(_botaoTanque);

            _botaoHealer = new Button(() => SelecionarPapel(PapelNoGrupo.Healer))
            {
                text = "✨ Healer",
            };
            _botaoHealer.AddToClassList(EstiloCore.Botao);
            linhaPapeis.Add(_botaoHealer);

            _botaoDps = new Button(() => SelecionarPapel(PapelNoGrupo.Dps)) { text = "⚔️ DPS" };
            _botaoDps.AddToClassList(EstiloCore.Botao);
            linhaPapeis.Add(_botaoDps);

            _secaoSelecao.Add(linhaPapeis);

            _botaoEntrar = new Button(AoClicarEntrar) { text = "Entrar na Fila" };
            _botaoEntrar.AddToClassList(EstiloCore.Botao);
            _botaoEntrar.AddToClassList(EstiloCore.BotaoPrincipal);
            _secaoSelecao.Add(_botaoEntrar);
            _raiz.Add(_secaoSelecao);

            // ── Status da fila ────────────────────────────────────────────────
            _secaoStatus = new VisualElement { name = "lfg.secao-status" };
            _secaoStatus.style.display = DisplayStyle.None;

            var statusHeader = new Label("Buscando grupo de dungeon...");
            statusHeader.AddToClassList(EstiloCore.Subtitulo);
            _secaoStatus.Add(statusHeader);

            _tempoFila = new Label("Tempo na fila: 00:00");
            _tempoFila.AddToClassList(EstiloCore.Texto);
            _secaoStatus.Add(_tempoFila);

            _contadores = new Label("Tanque: 0/1 | Healer: 0/1 | DPS: 0/3");
            _contadores.AddToClassList(EstiloCore.Texto);
            _contadores.style.marginTop = 6;
            _contadores.style.marginBottom = 12;
            _secaoStatus.Add(_contadores);

            _botaoSair = new Button(AoClicarSair) { text = "Sair da Fila" };
            _botaoSair.AddToClassList(EstiloCore.Botao);
            _secaoStatus.Add(_botaoSair);
            _raiz.Add(_secaoStatus);

            // ── Mensagens de aviso ────────────────────────────────────────────
            _aviso = new Label { name = "lfg.aviso" };
            _aviso.AddToClassList(EstiloCore.MensagemAviso);
            _aviso.style.marginTop = 12;
            _aviso.style.display = DisplayStyle.None;
            _raiz.Add(_aviso);

            _estado.Mudou += AtualizarVisual;
            SelecionarPapel(_estado.EmFila ? _estado.Papel : PapelNoGrupo.Dps);
            AtualizarVisual();
        }

        public void Dispose()
        {
            _estado.Mudou -= AtualizarVisual;
        }

        private void SelecionarPapel(PapelNoGrupo papel)
        {
            _papelSelecionado = papel;
            _botaoTanque.EnableInClassList("selecionado", papel == PapelNoGrupo.Tank);
            _botaoHealer.EnableInClassList("selecionado", papel == PapelNoGrupo.Healer);
            _botaoDps.EnableInClassList("selecionado", papel == PapelNoGrupo.Dps);
        }

        private void AoClicarEntrar()
        {
            _estado.Entrar(_papelSelecionado);
        }

        private void AoClicarSair()
        {
            _estado.Sair();
        }

        private void AtualizarVisual()
        {
            if (_estado.EmFila)
            {
                _secaoSelecao.style.display = DisplayStyle.None;
                _secaoStatus.style.display = DisplayStyle.Flex;

                var minutos = _estado.SegundosNaFila / 60;
                var segundos = _estado.SegundosNaFila % 60;
                _tempoFila.text =
                    $"Tempo na fila: {minutos:D2}:{segundos:D2} ({NomeDoPapel(_estado.Papel)})";
                _contadores.text =
                    $"Tanque: {_estado.Tanques}/1  |  Healer: {_estado.Healers}/1  |  DPS: {_estado.Dps}/3";
            }
            else
            {
                _secaoSelecao.style.display = DisplayStyle.Flex;
                _secaoStatus.style.display = DisplayStyle.None;
            }

            if (!string.IsNullOrEmpty(_estado.UltimoAviso))
            {
                _aviso.text = _estado.UltimoAviso;
                _aviso.style.display = DisplayStyle.Flex;
            }
            else
            {
                _aviso.style.display = DisplayStyle.None;
            }
        }

        private static string NomeDoPapel(PapelNoGrupo papel)
        {
            switch (papel)
            {
                case PapelNoGrupo.Tank:
                    return "Tanque";
                case PapelNoGrupo.Healer:
                    return "Healer";
                case PapelNoGrupo.Dps:
                    return "DPS";
                default:
                    return "Indefinido";
            }
        }
    }
}
