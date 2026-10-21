using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Pets;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Pets.UI
{
    /// <summary>
    /// Janela de Companheiros (tecla U), registrada nas <see cref="JanelasDoJogo"/>:
    /// exibe o catálogo completo de pets, os pets obtidos na conta e permite chamar
    /// ou dispensar o companheiro ativo.
    /// </summary>
    public sealed class JanelaPets : JanelaDoJogo
    {
        public const string Chave = "pets.companheiros";
        public const string Titulo = "Companheiros";
        public const string Tecla = "U";
        public const int Ordem = 60;

        private VisualElement _cartoesContainer;
        private readonly List<CartaoPet> _cartoes = new List<CartaoPet>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaPets());
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 520f;
            conteudo.style.maxWidth = 640f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            // Barra de abas
            var barraAbas = new VisualElement();
            barraAbas.style.flexDirection = FlexDirection.Row;
            barraAbas.style.marginBottom = 10f;

            var abaPets = new Button();
            abaPets.text = "Pets";
            abaPets.AddToClassList(EstiloCore.Botao);
            abaPets.style.paddingLeft = 16f;
            abaPets.style.paddingRight = 16f;
            abaPets.style.paddingTop = 6f;
            abaPets.style.paddingBottom = 6f;
            abaPets.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Selecionado);
            abaPets.style.color = new Color(0.95f, 0.95f, 1f);
            abaPets.style.unityFontStyleAndWeight = FontStyle.Bold;
            barraAbas.Add(abaPets);

            conteudo.Add(barraAbas);

            // Cabeçalho descritivo
            var subtitulo = new Label("Companheiros - Coleção de Pets");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.style.fontSize = 14f;
            subtitulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            subtitulo.style.marginBottom = 4f;
            conteudo.Add(subtitulo);

            var descricao = new Label(
                "Seu pet o segue pelo mundo e recolhe automaticamente itens caídos ao chão."
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

            foreach (var especie in RegrasPets.EspeciesDoCatalogo)
            {
                CriarCartao(especie);
            }

            // Rodapé informativo
            var rodape = new Label(
                "Dica: Pressione [U] para alternar Companheiros ou use /pet no chat."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.style.fontSize = 11f;
            rodape.style.marginTop = 12f;
            rodape.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoDesabilitado);
            conteudo.Add(rodape);
        }

        protected override void AoAbrir()
        {
            ApresentadorDePets.ColecaoMudou += AtualizarTodosOsCartoes;
            AtualizarTodosOsCartoes();
        }

        protected override void AoFechar()
        {
            ApresentadorDePets.ColecaoMudou -= AtualizarTodosOsCartoes;
        }

        private void CriarCartao(EspecieDePet especie)
        {
            var cartao = new CartaoPet(especie, AoChamarPet, AoDispensarPet);
            _cartoes.Add(cartao);
            _cartoesContainer.Add(cartao.Elemento);
        }

        private void AtualizarTodosOsCartoes()
        {
            var temPetAtivo = ApresentadorDePets.TemPetProprio;
            foreach (var cartao in _cartoes)
            {
                var desbloqueado = ApresentadorDePets.Possui(cartao.Especie);
                cartao.AtualizarEstado(desbloqueado, temPetAtivo);
            }
        }

        private void AoChamarPet(EspecieDePet especie)
        {
            ApresentadorDePets.Chamar(especie);
            AtualizarTodosOsCartoes();
        }

        private void AoDispensarPet()
        {
            ApresentadorDePets.Dispensar();
            AtualizarTodosOsCartoes();
        }

        private sealed class CartaoPet
        {
            public readonly EspecieDePet Especie;
            public readonly VisualElement Elemento;
            private readonly Action<EspecieDePet> _aoChamar;
            private readonly Action _aoDispensar;
            private readonly Button _botaoChamar;
            private readonly Button _botaoDispensar;
            private readonly VisualElement _seloBloqueado;
            private readonly VisualElement _seloObtido;
            private readonly Label _rotuloOrigem;

            public CartaoPet(
                EspecieDePet especie,
                Action<EspecieDePet> aoChamar,
                Action aoDispensar
            )
            {
                Especie = especie;
                _aoChamar = aoChamar;
                _aoDispensar = aoDispensar;

                Elemento = new VisualElement();
                Elemento.style.width = 240f;
                Elemento.style.minHeight = 220f;
                Elemento.style.marginBottom = 10f;
                Elemento.style.paddingLeft = 12f;
                Elemento.style.paddingRight = 12f;
                Elemento.style.paddingTop = 10f;
                Elemento.style.paddingBottom = 10f;
                Elemento.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Campo);
                Elemento.style.borderLeftWidth = 1f;
                Elemento.style.borderRightWidth = 1f;
                Elemento.style.borderTopWidth = 1f;
                Elemento.style.borderBottomWidth = 1f;
                Elemento.style.borderLeftColor = EstiloVisual.Cor(ConfigVisual.Cores.BordaDiscreta);
                Elemento.style.borderRightColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.BordaDiscreta
                );
                Elemento.style.borderTopColor = EstiloVisual.Cor(ConfigVisual.Cores.BordaDiscreta);
                Elemento.style.borderBottomColor = EstiloVisual.Cor(
                    ConfigVisual.Cores.BordaDiscreta
                );
                Elemento.style.borderTopLeftRadius = 6f;
                Elemento.style.borderTopRightRadius = 6f;
                Elemento.style.borderBottomLeftRadius = 6f;
                Elemento.style.borderBottomRightRadius = 6f;
                Elemento.style.flexDirection = FlexDirection.Column;
                Elemento.style.justifyContent = Justify.SpaceBetween;

                // Informações superiores
                var topo = new VisualElement();

                var nome = new Label(RegrasPets.ObterNome(especie));
                nome.style.fontSize = 13.5f;
                nome.style.unityFontStyleAndWeight = FontStyle.Bold;
                nome.style.color = new Color(0.95f, 0.92f, 0.85f);
                nome.style.marginBottom = 4f;
                topo.Add(nome);

                var descricao = new Label(RegrasPets.ObterDescricao(especie));
                descricao.style.fontSize = 10.5f;
                descricao.style.color = new Color(0.72f, 0.75f, 0.82f);
                descricao.style.whiteSpace = WhiteSpace.Normal;
                descricao.style.marginBottom = 6f;
                topo.Add(descricao);

                Elemento.Add(topo);

                // Selo Obtido
                _seloObtido = new VisualElement();
                _seloObtido.style.flexDirection = FlexDirection.Row;
                _seloObtido.style.justifyContent = Justify.Center;
                _seloObtido.style.alignItems = Align.Center;
                _seloObtido.style.paddingTop = 4f;
                _seloObtido.style.paddingBottom = 4f;
                _seloObtido.style.marginBottom = 6f;
                _seloObtido.style.backgroundColor = new Color(0.18f, 0.28f, 0.16f, 0.9f);
                _seloObtido.style.borderLeftColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloObtido.style.borderRightColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloObtido.style.borderTopColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloObtido.style.borderBottomColor = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloObtido.style.borderLeftWidth = 1f;
                _seloObtido.style.borderRightWidth = 1f;
                _seloObtido.style.borderTopWidth = 1f;
                _seloObtido.style.borderBottomWidth = 1f;
                _seloObtido.style.borderTopLeftRadius = 4f;
                _seloObtido.style.borderTopRightRadius = 4f;
                _seloObtido.style.borderBottomLeftRadius = 4f;
                _seloObtido.style.borderBottomRightRadius = 4f;

                var rotuloObtido = new Label("★ NA CONTA");
                rotuloObtido.style.fontSize = 10.5f;
                rotuloObtido.style.unityFontStyleAndWeight = FontStyle.Bold;
                rotuloObtido.style.color = new Color(0.50f, 0.95f, 0.50f);
                _seloObtido.Add(rotuloObtido);
                Elemento.Add(_seloObtido);

                // Selo Bloqueado
                _seloBloqueado = new VisualElement();
                _seloBloqueado.style.flexDirection = FlexDirection.Row;
                _seloBloqueado.style.justifyContent = Justify.Center;
                _seloBloqueado.style.alignItems = Align.Center;
                _seloBloqueado.style.paddingTop = 4f;
                _seloBloqueado.style.paddingBottom = 4f;
                _seloBloqueado.style.marginBottom = 6f;
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

                var rotuloBloqueado = new Label("🔒 BLOQUEADO");
                rotuloBloqueado.style.fontSize = 10.5f;
                rotuloBloqueado.style.unityFontStyleAndWeight = FontStyle.Bold;
                rotuloBloqueado.style.color = new Color(0.60f, 0.63f, 0.70f);
                _seloBloqueado.Add(rotuloBloqueado);
                Elemento.Add(_seloBloqueado);

                // Origem do pet
                var origem = RegrasPets.ObterOrigem(especie);
                _rotuloOrigem = new Label(string.IsNullOrEmpty(origem) ? "" : $"Origem: {origem}");
                _rotuloOrigem.style.fontSize = 10f;
                _rotuloOrigem.style.color = new Color(0.68f, 0.72f, 0.80f);
                _rotuloOrigem.style.marginBottom = 6f;
                _rotuloOrigem.style.whiteSpace = WhiteSpace.Normal;
                _rotuloOrigem.style.unityFontStyleAndWeight = FontStyle.Italic;
                Elemento.Add(_rotuloOrigem);

                // Botão Chamar
                _botaoChamar = new Button(() => _aoChamar(Especie));
                _botaoChamar.text = "Chamar Pet";
                _botaoChamar.AddToClassList(EstiloCore.Botao);
                _botaoChamar.style.alignSelf = Align.Stretch;
                _botaoChamar.style.paddingTop = 6f;
                _botaoChamar.style.paddingBottom = 6f;
                _botaoChamar.style.fontSize = 11.5f;
                _botaoChamar.style.unityFontStyleAndWeight = FontStyle.Bold;
                _botaoChamar.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Hover);
                _botaoChamar.style.color = new Color(0.90f, 0.94f, 1.0f);
                Elemento.Add(_botaoChamar);

                // Botão Dispensar
                _botaoDispensar = new Button(() => _aoDispensar());
                _botaoDispensar.text = "Dispensar Pet";
                _botaoDispensar.AddToClassList(EstiloCore.Botao);
                _botaoDispensar.style.alignSelf = Align.Stretch;
                _botaoDispensar.style.paddingTop = 6f;
                _botaoDispensar.style.paddingBottom = 6f;
                _botaoDispensar.style.fontSize = 11.5f;
                _botaoDispensar.style.unityFontStyleAndWeight = FontStyle.Bold;
                _botaoDispensar.style.backgroundColor = new Color(0.30f, 0.18f, 0.18f);
                _botaoDispensar.style.color = new Color(1.0f, 0.90f, 0.90f);
                Elemento.Add(_botaoDispensar);

                AtualizarEstado(false, false);
            }

            public void AtualizarEstado(bool desbloqueado, bool petAtivo)
            {
                if (!desbloqueado)
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
                    _botaoChamar.style.display = DisplayStyle.None;
                    _botaoDispensar.style.display = DisplayStyle.None;
                    _seloObtido.style.display = DisplayStyle.None;
                    _seloBloqueado.style.display = DisplayStyle.Flex;
                    _rotuloOrigem.style.display = DisplayStyle.Flex;
                }
                else
                {
                    Elemento.style.opacity = 1f;
                    Elemento.style.backgroundColor = EstiloVisual.Cor(
                        ConfigVisual.Cores.Selecionado
                    );
                    Elemento.style.borderLeftColor = new Color(0.35f, 0.50f, 0.70f, 0.8f);
                    Elemento.style.borderRightColor = new Color(0.35f, 0.50f, 0.70f, 0.8f);
                    Elemento.style.borderTopColor = new Color(0.35f, 0.50f, 0.70f, 0.8f);
                    Elemento.style.borderBottomColor = new Color(0.35f, 0.50f, 0.70f, 0.8f);
                    _seloBloqueado.style.display = DisplayStyle.None;
                    _seloObtido.style.display = DisplayStyle.Flex;
                    _rotuloOrigem.style.display = DisplayStyle.None;

                    if (petAtivo)
                    {
                        _botaoChamar.style.display = DisplayStyle.None;
                        _botaoDispensar.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        _botaoChamar.style.display = DisplayStyle.Flex;
                        _botaoDispensar.style.display = DisplayStyle.None;
                    }
                }
            }
        }
    }
}
