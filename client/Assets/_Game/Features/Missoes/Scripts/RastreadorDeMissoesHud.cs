using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Missoes;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Rastreador de missões do HUD (#1061), como o "objective tracker" do World of Warcraft:
    /// à direita, abaixo do minimapa, o título de cada missão acompanhada (dourado na história
    /// principal) e os objetivos com a contagem. Sem fundo pesado, só sombra no texto. Vive na
    /// região <see cref="RegiaoDoHud.Rastreador"/>, então só aparece dentro do mundo.
    /// </summary>
    internal sealed class RastreadorDeMissoesHud : MonoBehaviour
    {
        private const string CaminhoEstilo = "Missoes/EstiloMissoes";

        private static RastreadorDeMissoesHud _instancia;
        private VisualElement _regiaoAtendida;
        private VisualElement _container;
        private VisualElement _corpoMissoes;
        private Label _cabecalho;
        private Button _alternar;
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
            ControleDeMissoes.EstadoMudou += Redesenhar;
        }

        private void OnDestroy()
        {
            ControleDeMissoes.EstadoMudou -= Redesenhar;
            _container?.RemoveFromHierarchy();
        }

        private void Update()
        {
            var regiao = Hud.Regiao(RegiaoDoHud.Rastreador);
            if (regiao == _regiaoAtendida)
                return;

            _regiaoAtendida = regiao;
            _container?.RemoveFromHierarchy();
            _container = null;
            if (regiao == null)
                return;

            try
            {
                MontarInterface(regiao);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void MontarInterface(VisualElement regiao)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null && !regiao.styleSheets.Contains(estilo))
                regiao.styleSheets.Add(estilo);

            _container = new VisualElement { name = "MissoesRastreadorContainer" };
            _container.pickingMode = PickingMode.Ignore;
            _container.AddToClassList("rastreador");

            var topo = new VisualElement { pickingMode = PickingMode.Ignore };
            topo.AddToClassList("rastreador-topo");
            _cabecalho = ComponentesHud.Texto("Missões", "rastreador-cabecalho");
            topo.Add(_cabecalho);
            _alternar = new Button(Alternar) { text = "−" };
            _alternar.AddToClassList("rastreador-alternar");
            topo.Add(_alternar);
            _container.Add(topo);

            _corpoMissoes = new VisualElement { pickingMode = PickingMode.Ignore };
            _corpoMissoes.AddToClassList("rastreador-corpo");
            _container.Add(_corpoMissoes);

            regiao.Add(_container);
            Redesenhar();
        }

        private void Alternar()
        {
            _visivel = !_visivel;
            _corpoMissoes.style.display = _visivel ? DisplayStyle.Flex : DisplayStyle.None;
            _alternar.text = _visivel ? "−" : "+";
        }

        private void Redesenhar()
        {
            if (_corpoMissoes == null)
                return;

            _corpoMissoes.Clear();

            var acompanhadas = ControleDeMissoes.ObterAcompanhadas();
            _cabecalho.text =
                $"Missões  {acompanhadas.Count}/{ConfigMissoes.MaximoMissoesAcompanhadas}";

            if (acompanhadas.Count == 0)
            {
                _corpoMissoes.Add(
                    ComponentesHud.Texto(
                        "Nenhuma missão acompanhada. Abra o Diário de Missões (L) e marque as que quer seguir.",
                        "rastreador-dica"
                    )
                );
                return;
            }

            for (int i = 0; i < acompanhadas.Count; i++)
            {
                var prog = acompanhadas[i];
                var def = CatalogoDeMissoes.Obter(prog.MissaoId);
                if (def == null)
                    continue;

                var bloco = new VisualElement { pickingMode = PickingMode.Ignore };
                bloco.AddToClassList("rastreador-missao");

                var titulo = ComponentesHud.Texto(def.Titulo, "rastreador-titulo");
                titulo.EnableInClassList(
                    "rastreador-titulo-principal",
                    def.Tipo == TipoDeMissao.Principal
                );
                titulo.EnableInClassList(
                    "rastreador-titulo-pronta",
                    prog.Estado == EstadoDaMissao.ProntaParaEntregar
                );
                bloco.Add(titulo);

                if (prog.Estado == EstadoDaMissao.ProntaParaEntregar)
                {
                    var pronta = ComponentesHud.Texto(
                        "✔ Pronta: volte para entregar.",
                        "rastreador-objetivo"
                    );
                    pronta.AddToClassList("rastreador-objetivo-feito");
                    bloco.Add(pronta);
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
                        var texto =
                            objDef.QuantidadeTotal > 1
                                ? $"{(feito ? "✔" : "-")} {objDef.Descricao}: {atual}/{objDef.QuantidadeTotal}"
                                : $"{(feito ? "✔" : "-")} {objDef.Descricao}";
                        var linha = ComponentesHud.Texto(texto, "rastreador-objetivo");
                        linha.EnableInClassList("rastreador-objetivo-feito", feito);
                        bloco.Add(linha);
                    }
                }

                _corpoMissoes.Add(bloco);
            }
        }
    }
}
