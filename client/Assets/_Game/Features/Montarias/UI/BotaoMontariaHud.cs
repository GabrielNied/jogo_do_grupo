using System;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Montarias;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Montarias.UI
{
    /// <summary>
    /// A montaria no HUD: um slot com a ferradura e a tecla [T] ao lado da barra de ação (como
    /// a montaria na barra do World of Warcraft) e, enquanto ela é chamada, a barra de
    /// canalização logo acima. Clicar (com o cursor livre) monta ou desmonta. A coleção abre
    /// pelo menu rápido ou pela tecla da janela.
    /// </summary>
    public sealed class BotaoMontariaHud : IDisposable
    {
        private readonly VisualElement _raiz;
        private readonly SlotHud _slot;

        // Barra de canalização da chamada.
        private readonly VisualElement _painelInvocacao;
        private readonly Label _rotuloInvocacao;
        private readonly VisualElement _preenchimentoInvocacao;

        /// <param name="regiao">Ao lado da barra de ação (<c>RegiaoDoHud.BarraDeAcao</c>).</param>
        /// <param name="regiaoDaCanalizacao">Onde a barra de chamada aparece (acima da barra de ação); nula usa <paramref name="regiao"/>.</param>
        public BotaoMontariaHud(VisualElement regiao, VisualElement regiaoDaCanalizacao = null)
        {
            _raiz = new VisualElement { name = "montarias.hud-raiz" };
            _raiz.AddToClassList("hud-acao-extra");

            _painelInvocacao = new VisualElement { name = "montarias.hud-invocacao" };
            _painelInvocacao.AddToClassList("hud-canalizacao");
            _painelInvocacao.style.display = DisplayStyle.None;
            _painelInvocacao.pickingMode = PickingMode.Ignore;
            _preenchimentoInvocacao = new VisualElement { pickingMode = PickingMode.Ignore };
            _preenchimentoInvocacao.AddToClassList("hud-canalizacao-preenchimento");
            _rotuloInvocacao = ComponentesHud.Texto(
                "Invocando montaria...",
                "hud-canalizacao-texto"
            );
            _painelInvocacao.Add(_preenchimentoInvocacao);
            _painelInvocacao.Add(_rotuloInvocacao);

            _slot = new SlotHud(pequeno: true);
            _slot.Raiz.name = "montarias.hud-botao";
            _slot.Raiz.AddToClassList("montaria-hud-slot");
            _slot.DefinirVazio(false);
            _slot.DefinirIcone(ComponentesHud.Icone("Icones", "montar"));
            _slot.DefinirCorDoIcone("hud-icone-neutro");
            _slot.DefinirTecla("T");
            _slot.Raiz.RegisterCallback<ClickEvent>(_ => AoClicarMontar());
            ComponentesHud.ComDica(
                _slot.Raiz,
                () =>
                {
                    var controle = ControleDeMontaria.Instancia;
                    var montado = controle != null && controle.MontadoLocal;
                    var tipo =
                        controle != null ? controle.MontariaEquipada : RegrasMontaria.TipoPadrao;
                    return new ConteudoDaDica
                    {
                        Titulo = montado ? "Desmontar" : "Montar",
                        Subtitulo = RegrasMontaria.ObterNome(tipo),
                        Texto =
                            "Chama a montaria equipada para viajar mais rápido. Em combate não dá para montar.",
                        Rodape = "Atalho: T",
                    };
                }
            );

            _raiz.Add(_slot.Raiz);
            regiao.Add(_raiz);
            (regiaoDaCanalizacao ?? regiao).Add(_painelInvocacao);

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
                _slot.DefinirAtivo(true);
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

            _slot.DefinirAtivo(montado);
            _slot.DefinirCorDoIcone(montado ? "hud-icone-fisico" : "hud-icone-neutro");
        }

        public void Dispose()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EstadoMudou -= AtualizarEstado;
                ControleDeMontaria.Instancia.InvocacaoProgresso -= AtualizarInvocacao;
            }

            _raiz.RemoveFromHierarchy();
            _painelInvocacao.RemoveFromHierarchy();
        }
    }
}
