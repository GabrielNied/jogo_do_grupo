using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// O conteúdo da janela do inventário: paperdoll à esquerda, bolsa em grade à direita e o
    /// resumo de bônus embaixo. Só desenha o estado que o servidor mandou e devolve intenções
    /// (<see cref="AcaoPedida"/> e as outras); quem fala com o servidor é a
    /// <see cref="JanelaInventario"/>. Arrastar e tooltip ficam em Itens até haver segundo uso.
    /// </summary>
    public sealed class PainelInventario
    {
        public const int Colunas = 8;

        // Espaços do USS do Core e deste painel que entram na conta da altura (unidades do painel).
        private const float MargemTelaPx = 16f; // padding da tela
        private const float PercentualAlturaJanela = 0.9f; // max-height do painel (Core)
        private const float FolgaAlturaPx = 8f;
        private const float MargemColunaPx = 8f; // espacamento-pequeno em cima e embaixo
        private const float AlturaMolduraPadraoPx = 170f; // até a janela existir

        private int _casa = MedidasInventario.CasaMinima + 8;

        private readonly VisualElement _conteudo;
        private readonly Label _aviso;
        private readonly VisualElement _corpo;
        private readonly VisualElement _resumo;
        private readonly VisualElement _confirmacao;
        private readonly VisualElement _rodape;
        private readonly Dictionary<PosicaoEquipamento, VisualElement> _encaixes =
            new Dictionary<PosicaoEquipamento, VisualElement>();
        private readonly Dictionary<(int, int), VisualElement> _casas =
            new Dictionary<(int, int), VisualElement>();

        private readonly ControleDeArrasto _arrasto;
        private readonly TooltipDoInventario _tooltip;
        private VisualElement _raizInterface;
        private VisualElement _janela;
        private ScrollView _grade;
        private VisualElement _alvoAceso;
        private EquipamentosResponse _estado;
        private HashSet<long> _novos = new HashSet<long>();
        private HashSet<long> _pendentes = new HashSet<long>();
        private bool _ocupado;
        private int _aba;

        public event Action<AcaoDoInventario> AcaoPedida;
        public event Action<int> ConjuntoPedido;
        public event Action BancadaPedida;
#pragma warning disable CS0067 // só é disparado em editor/build de desenvolvimento
        public event Action<int> ItemDeTestePedido;
#pragma warning restore CS0067
        public event Action<long> ItemVisto;

        public PainelInventario(VisualElement conteudo)
        {
            _conteudo = conteudo;
            _raizInterface = AcharRaiz(conteudo);
            EstiloItens.Aplicar(_raizInterface);
            _janela = AcharJanela(conteudo);
            _janela?.AddToClassList(EstiloItens.PainelLargo);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);
            _corpo = new VisualElement();
            _corpo.AddToClassList(EstiloItens.Corpo);
            _resumo = new VisualElement();
            _resumo.AddToClassList(EstiloItens.Resumo);
            _confirmacao = new VisualElement();
            _confirmacao.AddToClassList(EstiloItens.Confirmacao);
            _confirmacao.style.display = DisplayStyle.None;
            _rodape = new VisualElement();
            _rodape.AddToClassList(EstiloCore.LinhaBotoes);
            _rodape.AddToClassList(EstiloItens.RodapeBotoes);

            conteudo.Add(_aviso);
            conteudo.Add(_corpo);
            conteudo.Add(_resumo);
            conteudo.Add(_confirmacao);
            conteudo.Add(_rodape);
            MostrarAviso(null, false);

            _arrasto = new ControleDeArrasto(
                _raizInterface,
                AlvoEm,
                (origem, alvo) =>
                    AcaoPedida?.Invoke(LogicaInventario.ResolverSoltura(origem, alvo)),
                AoMudarArrasto,
                AoMoverArrasto
            );
            _tooltip = new TooltipDoInventario(
                _raizInterface,
                () => _arrasto.Arrastando,
                () => _resumo
            );
            _raizInterface.RegisterCallback<GeometryChangedEvent>(_ => AjustarTamanho());
            _conteudo.RegisterCallback<GeometryChangedEvent>(_ => AjustarTamanho());
        }

        // ---- tamanho pela tela ----

        // A janela tem de caber na tela sem rolar: a casa é o maior tamanho que deixa o corpo
        // (paperdoll e bolsa) caber no que sobra da janela. Mede a moldura e o resto do conteúdo
        // em vez de supor, então acompanha mudança de resolução, de escala e de rodapé.
        private void AjustarTamanho()
        {
            var paperdoll = _corpo.Q("itens.paperdoll");
            if (paperdoll == null || float.IsNaN(_raizInterface.layout.height))
                return;

            var alturaTela = _raizInterface.layout.height;
            var maximaJanela =
                (alturaTela - 2 * MargemTelaPx) * PercentualAlturaJanela - FolgaAlturaPx;
            var ocupado = MolduraDaJanela() + RestoDoConteudo();
            var alturaCorpo = maximaJanela - ocupado;
            var fixaPaperdoll =
                paperdoll.layout.height
                - MedidasInventario.LinhasPaperdoll * (_casa + MedidasInventario.MargemCasa)
                + 2 * MargemColunaPx;
            var largura = _conteudo.layout.width;
            if (float.IsNaN(largura) || float.IsNaN(fixaPaperdoll) || largura <= 0)
                return;

            var nova = MedidasInventario.TamanhoDaCasa(largura, alturaCorpo, fixaPaperdoll);
            if (nova == _casa)
                return;
            _casa = nova;
            AplicarTamanho();
        }

        // Altura do que a janela do Core põe em volta do conteúdo (padding, borda, título, dica).
        private float MolduraDaJanela()
        {
            if (_janela == null)
                return AlturaMolduraPadraoPx;
            var estilo = _janela.resolvedStyle;
            var total =
                estilo.paddingTop
                + estilo.paddingBottom
                + estilo.borderTopWidth
                + estilo.borderBottomWidth;
            foreach (var filho in _janela.Children())
            {
                if (filho.Contains(_conteudo))
                    continue;
                total += AlturaComMargens(filho);
            }
            return total;
        }

        private float RestoDoConteudo()
        {
            var total = 0f;
            foreach (var filho in _conteudo.Children())
            {
                if (filho == _corpo)
                    continue;
                total += AlturaComMargens(filho);
            }
            return total;
        }

        private static float AlturaComMargens(VisualElement elemento)
        {
            if (elemento.resolvedStyle.display == DisplayStyle.None)
                return 0f;
            var altura = elemento.layout.height;
            if (float.IsNaN(altura))
                return 0f;
            return altura + elemento.resolvedStyle.marginTop + elemento.resolvedStyle.marginBottom;
        }

        // O tamanho da casa é do layout calculado agora, então vai inline; o resto fica no USS.
        private void AplicarTamanho()
        {
            foreach (var slot in _corpo.Query(className: EstiloItens.Slot).ToList())
                DimensionarCasa(slot);
            foreach (var vazio in _corpo.Query(className: EstiloItens.PaperdollVazio).ToList())
                DimensionarCasa(vazio);
            DimensionarGrade();
        }

        private void DimensionarCasa(VisualElement casa)
        {
            casa.style.width = _casa;
            casa.style.height = _casa;
        }

        private void DimensionarGrade()
        {
            if (_grade == null || _estado == null)
                return;
            var linhas = (_estado.Capacidade.CasasPorAba + Colunas - 1) / Colunas;
            var rola = linhas > MedidasInventario.LinhasVisiveisBolsa;
            _grade.style.width = MedidasInventario.LarguraDaGrade(_casa, Colunas, rola);
            _grade.style.maxHeight = MedidasInventario.AlturaDaGrade(_casa, linhas);
        }

        public bool Arrastando => _arrasto.Arrastando;

        /// <summary>Cancela um arraste em andamento e esconde o tooltip (janela fechando).</summary>
        public void Encerrar()
        {
            _arrasto.Cancelar();
            _tooltip.Remover();
        }

        public void MostrarCarregando()
        {
            _corpo.Clear();
            _corpo.Add(Texto("Carregando..."));
        }

        public void MostrarAviso(string texto, bool erro)
        {
            _aviso.text = texto ?? "";
            _aviso.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _aviso.EnableInClassList(EstiloCore.MensagemErro, erro);
        }

        /// <summary>
        /// Desenha o estado. <paramref name="novos"/> ganham o selo "novo";
        /// <paramref name="pendentes"/> ficam semitransparentes e travados;
        /// <paramref name="ocupado"/> trava todo arraste e clique enquanto o servidor responde.
        /// </summary>
        public void Mostrar(
            EquipamentosResponse estado,
            HashSet<long> novos,
            HashSet<long> pendentes,
            bool ocupado
        )
        {
            _estado = estado;
            _novos = novos ?? new HashSet<long>();
            _pendentes = pendentes ?? new HashSet<long>();
            _ocupado = ocupado;
            _tooltip.Esconder();
            if (_aba >= estado.Capacidade.Abas)
                _aba = 0;

            _encaixes.Clear();
            _casas.Clear();
            _corpo.Clear();
            _corpo.Add(MontarPaperdoll());
            _corpo.Add(MontarBolsa());
            MontarResumo();
            MontarRodape();
            AplicarTamanho();
        }

        /// <summary>Pede a confirmação de descarte (o item só sai depois do "Confirmar").</summary>
        public void PedirConfirmacaoDescarte(
            EntradaInventario item,
            Action confirmar,
            Action cancelar
        )
        {
            _confirmacao.Clear();
            var dadosItem = RegrasEquipamentos.DadosDe(item);
            var nome = dadosItem != null ? dadosItem.Nome : item.Nome;
            _confirmacao.Add(Texto("Descartar " + nome + " destrói o item de vez. Confirmar?"));
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(
                Botao(
                    "Confirmar descarte",
                    () =>
                    {
                        EsconderConfirmacao();
                        confirmar();
                    }
                )
            );
            linha.Add(
                Botao(
                    "Cancelar",
                    () =>
                    {
                        EsconderConfirmacao();
                        cancelar?.Invoke();
                    }
                )
            );
            _confirmacao.Add(linha);
            _confirmacao.style.display = DisplayStyle.Flex;
        }

        public void EsconderConfirmacao()
        {
            _confirmacao.Clear();
            _confirmacao.style.display = DisplayStyle.None;
        }

        // ---- montagem ----

        private VisualElement MontarPaperdoll()
        {
            var coluna = new VisualElement { name = "itens.paperdoll" };
            coluna.AddToClassList(EstiloItens.Coluna);
            coluna.AddToClassList(EstiloItens.Paperdoll);
            coluna.Add(Cabecalho("Equipamentos", null, false));

            // Três colunas: bijuterias dos lados, armadura no meio.
            var disposicao = new[]
            {
                new[]
                {
                    PosicaoEquipamento.Brinco1,
                    PosicaoEquipamento.Capacete,
                    PosicaoEquipamento.Brinco2,
                },
                new[]
                {
                    PosicaoEquipamento.Luvas,
                    PosicaoEquipamento.Peito,
                    PosicaoEquipamento.Adorno,
                },
                new[]
                {
                    PosicaoEquipamento.Anel1,
                    PosicaoEquipamento.Cinto,
                    PosicaoEquipamento.Anel2,
                },
                new[]
                {
                    PosicaoEquipamento.Inventario,
                    PosicaoEquipamento.Calca,
                    PosicaoEquipamento.Inventario,
                },
                new[]
                {
                    PosicaoEquipamento.Inventario,
                    PosicaoEquipamento.Botas,
                    PosicaoEquipamento.Inventario,
                },
            };
            foreach (var linhaDePosicoes in disposicao)
            {
                var linha = new VisualElement();
                linha.AddToClassList(EstiloItens.PaperdollLinha);
                foreach (var posicao in linhaDePosicoes)
                {
                    if (posicao == PosicaoEquipamento.Inventario)
                    {
                        var vazio = new VisualElement();
                        vazio.AddToClassList(EstiloItens.PaperdollVazio);
                        linha.Add(vazio);
                    }
                    else
                    {
                        linha.Add(CriarEncaixe(posicao));
                    }
                }
                coluna.Add(linha);
            }

            var conjuntos = new VisualElement();
            conjuntos.AddToClassList(EstiloItens.Conjuntos);
            for (var numero = 1; numero <= 2; numero++)
            {
                var n = numero;
                var linha = new VisualElement();
                linha.AddToClassList(EstiloItens.Conjunto);
                var rotulo = Botao("Conj. " + n, () => ConjuntoPedido?.Invoke(n));
                rotulo.AddToClassList(EstiloItens.ConjuntoRotulo);
                rotulo.EnableInClassList(EstiloItens.AbaAtiva, _estado.ConjuntoAtivo == n);
                rotulo.tooltip =
                    _estado.ConjuntoAtivo == n
                        ? "Conjunto de armas ativo"
                        : "Usar este conjunto de armas";
                rotulo.SetEnabled(!_ocupado && _estado.ConjuntoAtivo != n);
                linha.Add(rotulo);
                linha.Add(CriarEncaixe(RegrasEquipamentos.Principal(n)));
                linha.Add(CriarEncaixe(RegrasEquipamentos.Secundaria(n)));
                conjuntos.Add(linha);
            }
            coluna.Add(conjuntos);
            return coluna;
        }

        private VisualElement CriarEncaixe(PosicaoEquipamento posicao)
        {
            var entrada = LogicaInventario.NoEncaixe(_estado, posicao);
            var slot = CriarSlot(
                entrada,
                TextosEquipamento.TipoDoEncaixe(posicao),
                TextosEquipamento.NomePosicao(posicao)
            );
            slot.name = "encaixe-" + posicao;
            _encaixes[posicao] = slot;
            return slot;
        }

        private VisualElement MontarBolsa()
        {
            var coluna = new VisualElement { name = "itens.bolsa" };
            coluna.AddToClassList(EstiloItens.Coluna);
            var capacidade = _estado.Capacidade;
            var usados = _estado.Inventario.Length;
            coluna.Add(
                Cabecalho("Bolsa", usados + "/" + capacidade.Total, usados >= capacidade.Total)
            );

            var abas = new VisualElement();
            abas.AddToClassList(EstiloItens.Abas);
            for (var i = 0; i < capacidade.Abas; i++)
            {
                var indice = i;
                var aba = Botao(
                    capacidade.Abas == 1 ? "Bolsa" : "Aba " + (i + 1),
                    () =>
                    {
                        _aba = indice;
                        Mostrar(_estado, _novos, _pendentes, _ocupado);
                    }
                );
                aba.AddToClassList(EstiloItens.Aba);
                aba.EnableInClassList(EstiloItens.AbaAtiva, i == _aba);
                abas.Add(aba);
            }
            coluna.Add(abas);

            _grade = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            _grade.AddToClassList(EstiloItens.Grade);
            for (var casa = 0; casa < capacidade.CasasPorAba; casa++)
            {
                var entrada = LogicaInventario.NaCasa(_estado, _aba, casa);
                var slot = CriarSlot(entrada, null, null);
                slot.name = "casa-" + _aba + "-" + casa;
                _casas[(_aba, casa)] = slot;
                _grade.Add(slot);
            }
            coluna.Add(_grade);
            return coluna;
        }

        private void MontarResumo()
        {
            _resumo.Clear();
            _resumo.Add(Texto("Bônus das peças e das armas ativas", EstiloItens.ResumoTitulo));
            var linha = new VisualElement();
            linha.AddToClassList(EstiloItens.ResumoLinha);
            if (_estado.Bonus.Length == 0)
                linha.Add(Texto("Nenhum bônus ainda.", EstiloItens.ResumoBonus));
            foreach (var bonus in _estado.Bonus)
            {
                linha.Add(
                    Texto(
                        TextosEquipamento.NomeStatus(bonus.Status)
                            + " +"
                            + TextosEquipamento.Numero(bonus.Valor),
                        EstiloItens.ResumoBonus
                    )
                );
            }
            _resumo.Add(linha);
        }

        private void MontarRodape()
        {
            _rodape.Clear();
            var bancada = Botao("Bancada", () => BancadaPedida?.Invoke());
            bancada.tooltip = "Infundir equipamentos";
            bancada.SetEnabled(!_ocupado);
            _rodape.Add(bancada);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var grau = new DropdownField(
                "Grau de teste",
                new List<string> { "Inicial", "Médio", "Forte" },
                0
            );
            grau.AddToClassList(EstiloCore.Campo);
            _rodape.Add(grau);
            var obter = Botao(
                "Obter equipamento de teste",
                () => ItemDeTestePedido?.Invoke(grau.index)
            );
            obter.SetEnabled(!_ocupado);
            _rodape.Add(obter);
#endif
        }

        // ---- slots ----

        /// <summary>Uma casa da bolsa ou um encaixe, com ou sem item.</summary>
        private VisualElement CriarSlot(
            EntradaInventario entrada,
            TipoEquipamento? silhueta,
            string nomeDoEncaixe
        )
        {
            var slot = new VisualElement();
            slot.AddToClassList(EstiloItens.Slot);
            var icone = new VisualElement { pickingMode = PickingMode.Ignore };
            icone.AddToClassList(EstiloItens.Icone);
            var sigla = new Label { pickingMode = PickingMode.Ignore };
            sigla.AddToClassList(EstiloCore.Texto);
            sigla.AddToClassList(EstiloItens.Sigla);
            slot.Add(icone);
            slot.Add(sigla);

            if (entrada == null)
            {
                slot.AddToClassList(EstiloItens.SlotVazio);
                if (silhueta != null)
                {
                    PreencherIcone(icone, sigla, silhueta.Value, entrada: null);
                    slot.tooltip = nomeDoEncaixe;
                }
                return slot;
            }

            var tipo = LogicaInventario.TipoDe(entrada);
            if (tipo != null)
                PreencherIcone(icone, sigla, tipo.Value, entrada);
            else
                sigla.text = Abreviar(entrada.Nome);
            AdicionarTier(slot, entrada);

            var pendente = _pendentes.Contains(entrada.InstanciaId);
            slot.EnableInClassList(EstiloItens.SlotPendente, pendente);

            if (_novos.Contains(entrada.InstanciaId))
            {
                var selo = new Label("novo") { pickingMode = PickingMode.Ignore };
                selo.AddToClassList(EstiloItens.SeloNovo);
                slot.Add(selo);
                slot.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    selo.RemoveFromHierarchy();
                    ItemVisto?.Invoke(entrada.InstanciaId);
                });
            }

            _tooltip.Registrar(slot, () => TextosTooltip.Montar(entrada, _estado));
            if (_ocupado || pendente)
                return slot;

            _arrasto.Registrar(
                slot,
                LogicaInventario.OrigemDe(entrada),
                () => CriarFantasma(entrada, tipo)
            );
            slot.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 1)
                    return;
                _tooltip.Esconder();
                AcaoPedida?.Invoke(LogicaInventario.CliqueDireito(entrada, _estado));
                e.StopPropagation();
            });
            return slot;
        }

        private VisualElement CriarFantasma(EntradaInventario entrada, TipoEquipamento? tipo)
        {
            var fantasma = new VisualElement();
            fantasma.AddToClassList(EstiloItens.Slot);
            DimensionarCasa(fantasma);
            var icone = new VisualElement { pickingMode = PickingMode.Ignore };
            icone.AddToClassList(EstiloItens.Icone);
            var sigla = new Label { pickingMode = PickingMode.Ignore };
            sigla.AddToClassList(EstiloCore.Texto);
            sigla.AddToClassList(EstiloItens.Sigla);
            fantasma.Add(icone);
            fantasma.Add(sigla);
            if (tipo != null)
                PreencherIcone(icone, sigla, tipo.Value, entrada);
            else
                sigla.text = Abreviar(entrada.Nome);
            AdicionarTier(fantasma, entrada);
            return fantasma;
        }

        /// <summary>
        /// Tier calculado da peça: a cor da borda e do ícone (classe da casa) e a letra no canto.
        /// Item sem dados de equipamento não tem tier.
        /// </summary>
        private static void AdicionarTier(VisualElement casa, EntradaInventario entrada)
        {
            var dados = RegrasEquipamentos.DadosDe(entrada);
            if (dados == null)
                return;
            var tier = RegrasTier.TierDe(dados);
            casa.AddToClassList(EstiloItens.Tier(tier));
            var letra = new Label(RegrasTier.Letra(tier)) { pickingMode = PickingMode.Ignore };
            letra.AddToClassList(EstiloCore.Texto);
            letra.AddToClassList(EstiloItens.LetraTier);
            casa.Add(letra);
        }

        /// <summary>
        /// Ponto único do ícone: pelo tipo. Sem sprite, mostra a sigla do tipo.
        /// </summary>
        private static void PreencherIcone(
            VisualElement icone,
            Label sigla,
            TipoEquipamento tipo,
            EntradaInventario entrada
        )
        {
            var sprite = IconeDoTipo(tipo);
            if (sprite != null)
            {
                icone.style.backgroundImage = new StyleBackground(sprite);
                sigla.text = "";
            }
            else
            {
                sigla.text = TextosEquipamento.SiglaDoTipo(tipo);
            }
        }

        /// <summary>Silhueta branca do tipo (tingida pelo tier no USS), ou <c>null</c> sem ícone.</summary>
        public static Sprite IconeDoTipo(TipoEquipamento tipo)
        {
            return IconesEquipamento.Carregar(tipo);
        }

        private static string Abreviar(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return "?";
            return nome.Length <= 3 ? nome : nome.Substring(0, 3);
        }

        // ---- arraste ----

        private AlvoSoltura AlvoEm(Vector2 posicao)
        {
            foreach (var par in _encaixes)
            {
                if (par.Value.worldBound.Contains(posicao))
                    return AlvoSoltura.EmEncaixe(par.Key);
            }

            if (_grade != null && _grade.contentViewport.worldBound.Contains(posicao))
            {
                foreach (var par in _casas)
                {
                    if (par.Value.worldBound.Contains(posicao))
                        return AlvoSoltura.EmCasa(par.Key.Item1, par.Key.Item2);
                }
            }

            if (_janela != null && _janela.worldBound.Contains(posicao))
                return AlvoSoltura.Em(TipoAlvo.Nenhum);
            return AlvoSoltura.Em(TipoAlvo.Fora);
        }

        private void AoMudarArrasto(OrigemArrasto origem, bool comecou)
        {
            _tooltip.Esconder();
            foreach (var par in _encaixes)
            {
                par.Value.RemoveFromClassList(EstiloItens.SlotValido);
                par.Value.RemoveFromClassList(EstiloItens.SlotInvalido);
            }

            LimparAlvo();
            if (!comecou || origem.Tipo == null)
                return;
            var validos = LogicaInventario.EncaixesValidos(origem.Tipo.Value);
            foreach (var par in _encaixes)
            {
                par.Value.AddToClassList(
                    validos.Contains(par.Key) ? EstiloItens.SlotValido : EstiloItens.SlotInvalido
                );
            }
        }

        private void AoMoverArrasto(OrigemArrasto origem, Vector2 posicao)
        {
            var alvo = AlvoEm(posicao);
            VisualElement elemento = null;
            if (alvo.Tipo == TipoAlvo.Encaixe)
                _encaixes.TryGetValue(alvo.Posicao, out elemento);
            else if (alvo.Tipo == TipoAlvo.Casa)
                _casas.TryGetValue((alvo.Aba, alvo.Casa), out elemento);
            if (elemento == _alvoAceso)
                return;
            LimparAlvo();
            _alvoAceso = elemento;
            _alvoAceso?.AddToClassList(EstiloItens.SlotAlvo);
        }

        private void LimparAlvo()
        {
            _alvoAceso?.RemoveFromClassList(EstiloItens.SlotAlvo);
            _alvoAceso = null;
        }

        // ---- ajudantes ----

        private static VisualElement Cabecalho(string titulo, string contador, bool cheio)
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloItens.CabecalhoSecao);
            var rotulo = new Label(titulo);
            rotulo.AddToClassList(EstiloCore.Subtitulo);
            rotulo.AddToClassList(EstiloItens.TituloSecao);
            linha.Add(rotulo);
            if (contador != null)
            {
                var numero = new Label(contador) { name = "itens.contador" };
                numero.AddToClassList(EstiloCore.Texto);
                numero.AddToClassList(EstiloItens.Contador);
                numero.EnableInClassList(EstiloItens.ContadorCheio, cheio);
                linha.Add(numero);
            }
            return linha;
        }

        private static Label Texto(string texto, string classe = null)
        {
            var label = new Label(texto);
            label.AddToClassList(EstiloCore.Texto);
            if (classe != null)
                label.AddToClassList(classe);
            return label;
        }

        private static Button Botao(string texto, Action acao)
        {
            var botao = new Button(acao) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            return botao;
        }

        // A raiz da interface é o elemento com a classe lumora-ui (tem os tokens e recebe o
        // fantasma do arraste e o tooltip).
        private static VisualElement AcharRaiz(VisualElement inicio)
        {
            VisualElement atual = inicio;
            VisualElement topo = inicio;
            while (atual != null)
            {
                if (atual.ClassListContains("lumora-ui"))
                    return atual;
                topo = atual;
                atual = atual.parent;
            }

            return topo;
        }

        private static VisualElement AcharJanela(VisualElement inicio)
        {
            var atual = inicio.parent;
            while (atual != null)
            {
                if (atual.ClassListContains(EstiloCore.Janela))
                    return atual;
                atual = atual.parent;
            }

            return null;
        }
    }
}
