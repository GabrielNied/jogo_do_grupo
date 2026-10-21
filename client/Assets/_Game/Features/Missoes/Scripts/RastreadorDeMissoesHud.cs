using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Missoes;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Rastreador de missões no HUD (#1061), exibindo as missões acompanhadas no canto da tela.
    /// </summary>
    internal sealed class RastreadorDeMissoesHud : MonoBehaviour
    {
        private static RastreadorDeMissoesHud _instancia;
        private UIDocument _documento;
        private VisualElement _container;
        private VisualElement _corpoMissoes;
        private Label _cabecalho;
        private bool _visivel = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject("Missoes.RastreadorHud");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<RastreadorDeMissoesHud>();
        }

        private void Awake()
        {
            MontarInterface();
            ControleDeMissoes.EstadoMudou += Redesenhar;
        }

        private void OnDestroy()
        {
            ControleDeMissoes.EstadoMudou -= Redesenhar;
            if (_container != null && _container.parent != null)
            {
                _container.parent.Remove(_container);
            }
        }

        private void MontarInterface()
        {
            _documento = gameObject.AddComponent<UIDocument>();
            _documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelRastreadorMissoes",
                -0.5f
            );

            var raiz = _documento.rootVisualElement;
            EstiloCore.Aplicar(raiz);

            _container = new VisualElement { name = "MissoesRastreadorContainer" };
            _container.AddToClassList(EstiloCore.RaizInterface);
            _container.pickingMode = PickingMode.Ignore;
            _container.style.position = Position.Absolute;
            _container.style.top = 100;
            _container.style.right = 16;
            _container.style.width = 240;
            _container.style.paddingLeft = 8;
            _container.style.paddingRight = 8;
            _container.style.paddingTop = 6;
            _container.style.paddingBottom = 6;
            _container.style.backgroundColor = new StyleColor(
                new Color(0.05f, 0.05f, 0.07f, 0.65f)
            );
            _container.style.borderTopLeftRadius = 6;
            _container.style.borderTopRightRadius = 6;
            _container.style.borderBottomLeftRadius = 6;
            _container.style.borderBottomRightRadius = 6;
            _container.style.borderLeftWidth = 1;
            _container.style.borderRightWidth = 1;
            _container.style.borderTopWidth = 1;
            _container.style.borderBottomWidth = 1;
            _container.style.borderLeftColor = new StyleColor(new Color(0.3f, 0.35f, 0.45f, 0.4f));
            _container.style.borderRightColor = new StyleColor(new Color(0.3f, 0.35f, 0.45f, 0.4f));
            _container.style.borderTopColor = new StyleColor(new Color(0.3f, 0.35f, 0.45f, 0.4f));
            _container.style.borderBottomColor = new StyleColor(
                new Color(0.3f, 0.35f, 0.45f, 0.4f)
            );

            var topo = new VisualElement();
            topo.style.flexDirection = FlexDirection.Row;
            topo.style.justifyContent = Justify.SpaceBetween;
            topo.style.alignItems = Align.Center;
            topo.style.marginBottom = 6;

            _cabecalho = ComponentesVisuais.Texto("MISSÕES", subtitulo: true);
            _cabecalho.style.fontSize = 12;
            _cabecalho.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.9f));
            topo.Add(_cabecalho);

            var btnAlternar = new Button(() =>
            {
                _visivel = !_visivel;
                _corpoMissoes.style.display = _visivel ? DisplayStyle.Flex : DisplayStyle.None;
            })
            {
                text = "−",
            };
            btnAlternar.style.fontSize = 11;
            btnAlternar.style.backgroundColor = new StyleColor(Color.clear);
            btnAlternar.style.borderLeftWidth = 0;
            btnAlternar.style.borderRightWidth = 0;
            btnAlternar.style.borderTopWidth = 0;
            btnAlternar.style.borderBottomWidth = 0;
            btnAlternar.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            topo.Add(btnAlternar);

            _container.Add(topo);

            _corpoMissoes = new VisualElement();
            _corpoMissoes.pickingMode = PickingMode.Ignore;
            _container.Add(_corpoMissoes);

            raiz.Add(_container);
            Redesenhar();
        }

        private void Redesenhar()
        {
            if (_corpoMissoes == null)
                return;

            _corpoMissoes.Clear();

            var acompanhadas = ControleDeMissoes.ObterAcompanhadas();
            _cabecalho.text =
                $"MISSÕES ({acompanhadas.Count}/{ConfigMissoes.MaximoMissoesAcompanhadas})";

            if (acompanhadas.Count == 0)
            {
                var dica = ComponentesVisuais.Texto("Pressione L para abrir o Diário de Missões.");
                dica.style.fontSize = 11;
                dica.style.opacity = 0.6f;
                dica.style.whiteSpace = WhiteSpace.Normal;
                _corpoMissoes.Add(dica);
                return;
            }

            for (int i = 0; i < acompanhadas.Count; i++)
            {
                var prog = acompanhadas[i];
                var def = CatalogoDeMissoes.Obter(prog.MissaoId);
                if (def == null)
                    continue;

                var bloco = new VisualElement();
                bloco.pickingMode = PickingMode.Ignore;
                bloco.style.marginBottom = 8;

                // Título
                var titulo = ComponentesVisuais.Texto(def.Titulo, subtitulo: true);
                titulo.style.fontSize = 12;
                titulo.style.whiteSpace = WhiteSpace.Normal;
                if (prog.Estado == EstadoDaMissao.ProntaParaEntregar)
                {
                    titulo.style.color = new StyleColor(new Color(0.4f, 0.95f, 0.4f));
                }
                else if (def.Tipo == TipoDeMissao.Principal)
                {
                    titulo.style.color = new StyleColor(new Color(1f, 0.85f, 0.3f));
                }
                else
                {
                    titulo.style.color = new StyleColor(Color.white);
                }
                bloco.Add(titulo);

                // Objetivos
                if (prog.Estado == EstadoDaMissao.ProntaParaEntregar)
                {
                    var pronto = ComponentesVisuais.Texto("✔ Pronta! Entregue ao NPC.");
                    pronto.style.fontSize = 11;
                    pronto.style.color = new StyleColor(new Color(0.4f, 0.95f, 0.4f));
                    bloco.Add(pronto);
                }
                else
                {
                    for (int j = 0; j < def.Objetivos.Length; j++)
                    {
                        var objDef = def.Objetivos[j];
                        var progObj =
                            prog.Objetivos != null
                                ? Array.Find(prog.Objetivos, o => o.ObjetivoId == objDef.Id)
                                : null;

                        var atual = progObj != null ? progObj.QuantidadeAtual : 0;
                        var feito = progObj != null && progObj.Concluido;

                        var objLinha = ComponentesVisuais.Texto(
                            $" {(feito ? "[✔]" : "•")} {objDef.Descricao} ({atual}/{objDef.QuantidadeTotal})"
                        );
                        objLinha.style.fontSize = 11;
                        objLinha.style.whiteSpace = WhiteSpace.Normal;
                        objLinha.style.color = feito
                            ? new StyleColor(new Color(0.5f, 0.85f, 0.5f))
                            : new StyleColor(new Color(0.8f, 0.8f, 0.8f));
                        bloco.Add(objLinha);
                    }
                }

                _corpoMissoes.Add(bloco);
            }
        }
    }
}
