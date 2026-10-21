using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Montarias;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Montarias.UI
{
    /// <summary>
    /// Janela de coleção de montarias (tecla P), registrada nas <see cref="JanelasDoJogo"/>:
    /// exibe os companheiros disponíveis, detalhes visuais e permite equipar a montaria
    /// ativa a ser invocada com [T].
    /// </summary>
    public sealed class JanelaMontarias : JanelaDoJogo
    {
        public const string Chave = "montarias.colecao";
        public const string Titulo = "Montarias";
        public const string Tecla = "P";
        public const int Ordem = 50;

        private VisualElement _cartoesContainer;
        private readonly List<CartaoMontaria> _cartoes = new List<CartaoMontaria>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaMontarias());
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 520f;
            conteudo.style.maxWidth = 640f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            // Cabeçalho descritivo
            var subtitulo = new Label("Coleção de Montarias de Asterra");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.style.fontSize = 14f;
            subtitulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            subtitulo.style.marginBottom = 4f;
            conteudo.Add(subtitulo);

            var descricao = new Label(
                "Selecione e equipe sua montaria. Ao cavalgar, sua velocidade autoritativa aumenta para 9,0 m/s (+63% de corrida)."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.style.fontSize = 11.5f;
            descricao.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
            descricao.style.marginBottom = 12f;
            conteudo.Add(descricao);

            // Contêiner de cartões
            _cartoesContainer = new VisualElement();
            _cartoesContainer.style.flexDirection = FlexDirection.Row;
            _cartoesContainer.style.justifyContent = Justify.SpaceBetween;
            _cartoesContainer.style.flexWrap = Wrap.Wrap;
            conteudo.Add(_cartoesContainer);

            foreach (var tipo in RegrasMontaria.TiposDoCatalogo)
            {
                CriarCartao(tipo);
            }

            // Rodapé informativo
            var rodape = new Label(
                "Dica: Pressione [T] para subir na montaria equipada (1,5s parado) ou descer na hora."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.style.fontSize = 11f;
            rodape.style.marginTop = 12f;
            rodape.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoDesabilitado);
            conteudo.Add(rodape);
        }

        protected override void AoAbrir()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.MontariaEquipadaMudou += AtualizarCartoesPorEquipada;
                ControleDeMontaria.Instancia.ColecaoMudou += AtualizarTodosOsCartoes;
            }

            AtualizarTodosOsCartoes();
        }

        protected override void AoFechar()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.MontariaEquipadaMudou -= AtualizarCartoesPorEquipada;
                ControleDeMontaria.Instancia.ColecaoMudou -= AtualizarTodosOsCartoes;
            }
        }

        private void CriarCartao(int tipo)
        {
            var cartao = new CartaoMontaria(tipo, AoSolicitarEquipar);
            _cartoes.Add(cartao);
            _cartoesContainer.Add(cartao.Elemento);
        }

        private void AoSolicitarEquipar(int tipo)
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EquiparMontaria(tipo);
            }
            AtualizarTodosOsCartoes();
        }

        private void AtualizarCartoesPorEquipada(int _)
        {
            AtualizarTodosOsCartoes();
        }

        private void AtualizarTodosOsCartoes()
        {
            var controle = ControleDeMontaria.Instancia;
            var equipada = controle != null ? controle.MontariaEquipada : RegrasMontaria.TipoPadrao;

            for (var i = 0; i < _cartoes.Count; i++)
            {
                var tipo = _cartoes[i].Tipo;
                var desbloqueada = controle == null || controle.PossuiMontaria(tipo);
                var ehEquipada = desbloqueada && tipo == equipada;
                _cartoes[i].AtualizarEstado(desbloqueada, ehEquipada);
            }
        }

        /// <summary>
        /// Representa o cartão visual de uma montaria na coleção.
        /// </summary>
        private sealed class CartaoMontaria
        {
            public int Tipo { get; }
            public VisualElement Elemento { get; }

            private readonly Button _botaoEquipar;
            private readonly VisualElement _seloEquipado;
            private readonly VisualElement _seloBloqueado;
            private readonly Label _rotuloOrigem;
            private readonly Action<int> _aoEquipar;

            public CartaoMontaria(int tipo, Action<int> aoEquipar)
            {
                Tipo = tipo;
                _aoEquipar = aoEquipar;

                Elemento = new VisualElement();
                Elemento.style.flexGrow = 1f;
                Elemento.style.flexBasis = 230f;
                Elemento.style.marginRight = 6f;
                Elemento.style.marginLeft = 6f;
                Elemento.style.marginBottom = 10f;
                Elemento.style.paddingLeft = 12f;
                Elemento.style.paddingRight = 12f;
                Elemento.style.paddingTop = 10f;
                Elemento.style.paddingBottom = 12f;
                Elemento.style.borderTopLeftRadius = 8f;
                Elemento.style.borderTopRightRadius = 8f;
                Elemento.style.borderBottomLeftRadius = 8f;
                Elemento.style.borderBottomRightRadius = 8f;
                Elemento.style.borderLeftWidth = 1.5f;
                Elemento.style.borderRightWidth = 1.5f;
                Elemento.style.borderTopWidth = 1.5f;
                Elemento.style.borderBottomWidth = 1.5f;

                // Faixa de destaque visual no topo do cartão
                var faixaVisual = new VisualElement();
                faixaVisual.style.height = 40f;
                faixaVisual.style.borderTopLeftRadius = 6f;
                faixaVisual.style.borderTopRightRadius = 6f;
                faixaVisual.style.borderBottomLeftRadius = 6f;
                faixaVisual.style.borderBottomRightRadius = 6f;
                faixaVisual.style.marginBottom = 8f;
                faixaVisual.style.justifyContent = Justify.Center;
                faixaVisual.style.alignItems = Align.Center;

                if (tipo == RegrasMontaria.TipoNoturno)
                {
                    faixaVisual.style.backgroundColor = new Color(0.12f, 0.16f, 0.26f, 0.95f);
                    faixaVisual.style.borderLeftColor = new Color(0.28f, 0.70f, 0.95f, 0.6f);
                    faixaVisual.style.borderRightColor = new Color(0.28f, 0.70f, 0.95f, 0.6f);
                    faixaVisual.style.borderTopColor = new Color(0.28f, 0.70f, 0.95f, 0.6f);
                    faixaVisual.style.borderBottomColor = new Color(0.28f, 0.70f, 0.95f, 0.6f);
                    faixaVisual.style.borderLeftWidth = 1f;
                    faixaVisual.style.borderRightWidth = 1f;
                    faixaVisual.style.borderTopWidth = 1f;
                    faixaVisual.style.borderBottomWidth = 1f;
                }
                else
                {
                    faixaVisual.style.backgroundColor = new Color(0.26f, 0.18f, 0.12f, 0.95f);
                    faixaVisual.style.borderLeftColor = new Color(0.75f, 0.55f, 0.25f, 0.6f);
                    faixaVisual.style.borderRightColor = new Color(0.75f, 0.55f, 0.25f, 0.6f);
                    faixaVisual.style.borderTopColor = new Color(0.75f, 0.55f, 0.25f, 0.6f);
                    faixaVisual.style.borderBottomColor = new Color(0.75f, 0.55f, 0.25f, 0.6f);
                    faixaVisual.style.borderLeftWidth = 1f;
                    faixaVisual.style.borderRightWidth = 1f;
                    faixaVisual.style.borderTopWidth = 1f;
                    faixaVisual.style.borderBottomWidth = 1f;
                }

                var insignia = new Label(
                    tipo == RegrasMontaria.TipoNoturno
                        ? "✦ CORCEL NOTURNO ✦"
                        : "❖ CORCEL CASTANHO ❖"
                );
                insignia.style.fontSize = 11f;
                insignia.style.unityFontStyleAndWeight = FontStyle.Bold;
                insignia.style.color =
                    tipo == RegrasMontaria.TipoNoturno
                        ? new Color(0.50f, 0.88f, 1.0f)
                        : new Color(0.95f, 0.80f, 0.40f);
                faixaVisual.Add(insignia);
                Elemento.Add(faixaVisual);

                // Nome da montaria
                var nome = new Label(RegrasMontaria.ObterNome(tipo));
                nome.AddToClassList(EstiloCore.Titulo);
                nome.style.fontSize = 15f;
                nome.style.marginBottom = 2f;
                nome.style.color = new Color(0.95f, 0.96f, 0.98f);
                Elemento.Add(nome);

                // Categoria e raridade
                var raridade = new Label($"{RegrasMontaria.ObterRaridade(tipo)} • Terrestre");
                raridade.style.fontSize = 10.5f;
                raridade.style.color =
                    tipo == RegrasMontaria.TipoNoturno
                        ? new Color(0.40f, 0.80f, 1.0f)
                        : new Color(0.80f, 0.70f, 0.45f);
                raridade.style.marginBottom = 8f;
                Elemento.Add(raridade);

                // Descrição
                var desc = new Label(RegrasMontaria.ObterDescricao(tipo));
                desc.AddToClassList(EstiloCore.Texto);
                desc.style.fontSize = 11f;
                desc.style.color = new Color(0.74f, 0.78f, 0.85f);
                desc.style.marginBottom = 10f;
                Elemento.Add(desc);

                // Atributos / Estatísticas
                var atributos = new VisualElement();
                atributos.style.marginBottom = 10f;
                atributos.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Trilho);
                atributos.style.paddingLeft = 8f;
                atributos.style.paddingRight = 8f;
                atributos.style.paddingTop = 6f;
                atributos.style.paddingBottom = 6f;
                atributos.style.borderTopLeftRadius = 4f;
                atributos.style.borderTopRightRadius = 4f;
                atributos.style.borderBottomLeftRadius = 4f;
                atributos.style.borderBottomRightRadius = 4f;

                var linhaVelocidade = new Label("⚡ Velocidade: 9,0 m/s (+63%)");
                linhaVelocidade.style.fontSize = 11f;
                linhaVelocidade.style.color = new Color(0.40f, 0.90f, 0.60f);
                linhaVelocidade.style.marginBottom = 2f;

                var linhaInvocacao = new Label("⏳ Invocação: 1,5 segundos");
                linhaInvocacao.style.fontSize = 11f;
                linhaInvocacao.style.color = new Color(0.85f, 0.85f, 0.90f);

                atributos.Add(linhaVelocidade);
                atributos.Add(linhaInvocacao);
                Elemento.Add(atributos);

                // Botão de equipar
                _botaoEquipar = new Button(() => _aoEquipar(Tipo));
                _botaoEquipar.text = "Equipar Montaria";
                _botaoEquipar.AddToClassList(EstiloCore.Botao);
                _botaoEquipar.style.alignSelf = Align.Stretch;
                _botaoEquipar.style.paddingTop = 7f;
                _botaoEquipar.style.paddingBottom = 7f;
                _botaoEquipar.style.marginTop = 4f;
                _botaoEquipar.style.fontSize = 12f;
                _botaoEquipar.style.unityFontStyleAndWeight = FontStyle.Bold;
                _botaoEquipar.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Hover);
                _botaoEquipar.style.color = new Color(0.90f, 0.94f, 1.0f);
                Elemento.Add(_botaoEquipar);

                // Selo de montaria já equipada
                _seloEquipado = new VisualElement();
                _seloEquipado.style.flexDirection = FlexDirection.Row;
                _seloEquipado.style.justifyContent = Justify.Center;
                _seloEquipado.style.alignItems = Align.Center;
                _seloEquipado.style.paddingTop = 6f;
                _seloEquipado.style.paddingBottom = 6f;
                _seloEquipado.style.marginTop = 4f;
                _seloEquipado.style.backgroundColor = new Color(0.18f, 0.28f, 0.16f, 0.9f);
                _seloEquipado.style.borderLeftColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloEquipado.style.borderRightColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloEquipado.style.borderTopColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloEquipado.style.borderBottomColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloEquipado.style.borderLeftWidth = 1f;
                _seloEquipado.style.borderRightWidth = 1f;
                _seloEquipado.style.borderTopWidth = 1f;
                _seloEquipado.style.borderBottomWidth = 1f;
                _seloEquipado.style.borderTopLeftRadius = 4f;
                _seloEquipado.style.borderTopRightRadius = 4f;
                _seloEquipado.style.borderBottomLeftRadius = 4f;
                _seloEquipado.style.borderBottomRightRadius = 4f;

                var rotuloEquipado = new Label("★ EQUIPADA");
                rotuloEquipado.style.fontSize = 11.5f;
                rotuloEquipado.style.unityFontStyleAndWeight = FontStyle.Bold;
                rotuloEquipado.style.color = new Color(0.50f, 0.95f, 0.50f);
                _seloEquipado.Add(rotuloEquipado);
                Elemento.Add(_seloEquipado);

                // Selo de montaria bloqueada
                _seloBloqueado = new VisualElement();
                _seloBloqueado.style.flexDirection = FlexDirection.Row;
                _seloBloqueado.style.justifyContent = Justify.Center;
                _seloBloqueado.style.alignItems = Align.Center;
                _seloBloqueado.style.paddingTop = 6f;
                _seloBloqueado.style.paddingBottom = 6f;
                _seloBloqueado.style.marginTop = 4f;
                _seloBloqueado.style.backgroundColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.Desabilitado
                );
                _seloBloqueado.style.borderLeftColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.BordaDiscreta
                );
                _seloBloqueado.style.borderRightColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.BordaDiscreta
                );
                _seloBloqueado.style.borderTopColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.BordaDiscreta
                );
                _seloBloqueado.style.borderBottomColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.BordaDiscreta
                );
                _seloBloqueado.style.borderLeftWidth = 1f;
                _seloBloqueado.style.borderRightWidth = 1f;
                _seloBloqueado.style.borderTopWidth = 1f;
                _seloBloqueado.style.borderBottomWidth = 1f;
                _seloBloqueado.style.borderTopLeftRadius = 4f;
                _seloBloqueado.style.borderTopRightRadius = 4f;
                _seloBloqueado.style.borderBottomLeftRadius = 4f;
                _seloBloqueado.style.borderBottomRightRadius = 4f;

                var rotuloBloqueado = new Label("🔒 BLOQUEADA");
                rotuloBloqueado.style.fontSize = 11.5f;
                rotuloBloqueado.style.unityFontStyleAndWeight = FontStyle.Bold;
                rotuloBloqueado.style.color = new Color(0.60f, 0.63f, 0.70f);
                _seloBloqueado.Add(rotuloBloqueado);
                Elemento.Add(_seloBloqueado);

                // Origem da montaria
                var origem = RegrasMontaria.ObterOrigem(tipo);
                _rotuloOrigem = new Label(string.IsNullOrEmpty(origem) ? "" : $"Origem: {origem}");
                _rotuloOrigem.style.fontSize = 10f;
                _rotuloOrigem.style.color = new Color(0.68f, 0.72f, 0.80f);
                _rotuloOrigem.style.marginTop = 6f;
                _rotuloOrigem.style.unityFontStyleAndWeight = FontStyle.Italic;
                Elemento.Add(_rotuloOrigem);

                AtualizarEstado(true, false);
            }

            public void AtualizarEstado(bool desbloqueada, bool equipado)
            {
                if (!desbloqueada)
                {
                    Elemento.style.opacity = 0.55f;
                    Elemento.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Trilho);
                    Elemento.style.borderLeftColor = EstiloVisual.Cor(
                        ConfigVisual.Cores.BordaDiscreta
                    );
                    Elemento.style.borderRightColor = EstiloVisual.Cor(
                        ConfigVisual.Cores.BordaDiscreta
                    );
                    Elemento.style.borderTopColor = EstiloVisual.Cor(
                        ConfigVisual.Cores.BordaDiscreta
                    );
                    Elemento.style.borderBottomColor = EstiloVisual.Cor(
                        ConfigVisual.Cores.BordaDiscreta
                    );
                    _botaoEquipar.style.display = DisplayStyle.None;
                    _seloEquipado.style.display = DisplayStyle.None;
                    _seloBloqueado.style.display = DisplayStyle.Flex;
                    _rotuloOrigem.style.display = DisplayStyle.Flex;
                }
                else
                {
                    Elemento.style.opacity = 1f;
                    _seloBloqueado.style.display = DisplayStyle.None;

                    if (equipado)
                    {
                        Elemento.style.backgroundColor = EstiloVisual.Cor(
                            ConfigVisual.Cores.Selecionado
                        );
                        Elemento.style.borderLeftColor = new Color(0.85f, 0.70f, 0.28f, 0.9f);
                        Elemento.style.borderRightColor = new Color(0.85f, 0.70f, 0.28f, 0.9f);
                        Elemento.style.borderTopColor = new Color(0.85f, 0.70f, 0.28f, 0.9f);
                        Elemento.style.borderBottomColor = new Color(0.85f, 0.70f, 0.28f, 0.9f);
                        _botaoEquipar.style.display = DisplayStyle.None;
                        _seloEquipado.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        Elemento.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Campo);
                        Elemento.style.borderLeftColor = EstiloVisual.Cor(
                            ConfigVisual.Cores.BordaDiscreta
                        );
                        Elemento.style.borderRightColor = EstiloVisual.Cor(
                            ConfigVisual.Cores.BordaDiscreta
                        );
                        Elemento.style.borderTopColor = EstiloVisual.Cor(
                            ConfigVisual.Cores.BordaDiscreta
                        );
                        Elemento.style.borderBottomColor = EstiloVisual.Cor(
                            ConfigVisual.Cores.BordaDiscreta
                        );
                        _botaoEquipar.style.display = DisplayStyle.Flex;
                        _seloEquipado.style.display = DisplayStyle.None;
                    }
                }
            }

            public void DefinirEquipado(bool equipado)
            {
                AtualizarEstado(true, equipado);
            }
        }
    }
}
