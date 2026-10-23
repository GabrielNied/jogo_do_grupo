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
            EstiloPets.Aplicar(conteudo);
            conteudo.AddToClassList("pets-conteudo");

            // Barra de abas
            var barraAbas = new VisualElement();
            barraAbas.AddToClassList("pets-abas");

            var abaPets = new Button();
            abaPets.text = "Pets";
            abaPets.AddToClassList(EstiloCore.Botao);
            abaPets.AddToClassList("pets-aba");
            abaPets.style.color = new Color(0.95f, 0.95f, 1f);
            barraAbas.Add(abaPets);

            conteudo.Add(barraAbas);

            // Cabeçalho descritivo
            var subtitulo = new Label("Companheiros - Coleção de Pets");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.AddToClassList("pets-subtitulo");
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            conteudo.Add(subtitulo);

            var descricao = new Label(
                "Seu pet o segue pelo mundo e recolhe automaticamente itens caídos ao chão."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.AddToClassList("pets-descricao");
            conteudo.Add(descricao);

            // Contêiner de cartões
            _cartoesContainer = new VisualElement();
            _cartoesContainer.AddToClassList("pets-cartoes");
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
            rodape.AddToClassList("pets-rodape");
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
                Elemento.AddToClassList("pets-cartao");

                // Informações superiores
                var topo = new VisualElement();

                var nome = new Label(RegrasPets.ObterNome(especie));
                nome.AddToClassList("pets-nome");
                nome.style.color = new Color(0.95f, 0.92f, 0.85f);
                topo.Add(nome);

                var descricao = new Label(RegrasPets.ObterDescricao(especie));
                descricao.AddToClassList("pets-desc");
                descricao.style.color = new Color(0.72f, 0.75f, 0.82f);
                topo.Add(descricao);

                Elemento.Add(topo);

                // Selo Obtido
                _seloObtido = new VisualElement();
                _seloObtido.AddToClassList("pets-selo");
                var corSelo = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloObtido.style.backgroundColor = new Color(0.18f, 0.28f, 0.16f, 0.9f);
                _seloObtido.style.borderLeftColor = corSelo;
                _seloObtido.style.borderRightColor = corSelo;
                _seloObtido.style.borderTopColor = corSelo;
                _seloObtido.style.borderBottomColor = corSelo;

                var rotuloObtido = new Label("★ NA CONTA");
                rotuloObtido.AddToClassList("pets-selo-rotulo");
                rotuloObtido.style.color = new Color(0.50f, 0.95f, 0.50f);
                _seloObtido.Add(rotuloObtido);
                Elemento.Add(_seloObtido);

                // Selo Bloqueado
                _seloBloqueado = new VisualElement();
                _seloBloqueado.AddToClassList("pets-selo");
                _seloBloqueado.AddToClassList("pets-selo-bloqueado");

                var rotuloBloqueado = new Label("🔒 BLOQUEADO");
                rotuloBloqueado.AddToClassList("pets-selo-rotulo");
                rotuloBloqueado.style.color = new Color(0.60f, 0.63f, 0.70f);
                _seloBloqueado.Add(rotuloBloqueado);
                Elemento.Add(_seloBloqueado);

                // Origem do pet
                var origem = RegrasPets.ObterOrigem(especie);
                _rotuloOrigem = new Label(string.IsNullOrEmpty(origem) ? "" : $"Origem: {origem}");
                _rotuloOrigem.AddToClassList("pets-origem");
                _rotuloOrigem.style.color = new Color(0.68f, 0.72f, 0.80f);
                Elemento.Add(_rotuloOrigem);

                // Botão Chamar
                _botaoChamar = new Button(() => _aoChamar(Especie));
                _botaoChamar.text = "Chamar Pet";
                _botaoChamar.AddToClassList(EstiloCore.Botao);
                _botaoChamar.AddToClassList("pets-acao");
                _botaoChamar.AddToClassList("pets-chamar");
                _botaoChamar.style.color = new Color(0.90f, 0.94f, 1.0f);
                Elemento.Add(_botaoChamar);

                // Botão Dispensar
                _botaoDispensar = new Button(() => _aoDispensar());
                _botaoDispensar.text = "Dispensar Pet";
                _botaoDispensar.AddToClassList(EstiloCore.Botao);
                _botaoDispensar.AddToClassList("pets-acao");
                _botaoDispensar.style.backgroundColor = new Color(0.30f, 0.18f, 0.18f);
                _botaoDispensar.style.color = new Color(1.0f, 0.90f, 0.90f);
                Elemento.Add(_botaoDispensar);

                AtualizarEstado(false, false);
            }

            public void AtualizarEstado(bool desbloqueado, bool petAtivo)
            {
                Elemento.EnableInClassList("pets-cartao-bloqueado", !desbloqueado);
                Elemento.EnableInClassList("pets-cartao-obtido", desbloqueado);

                // Pet obtido tem borda azulada (cor sem token); bloqueado usa a borda da classe.
                if (desbloqueado)
                {
                    var azul = new Color(0.35f, 0.50f, 0.70f, 0.8f);
                    Elemento.style.borderLeftColor = azul;
                    Elemento.style.borderRightColor = azul;
                    Elemento.style.borderTopColor = azul;
                    Elemento.style.borderBottomColor = azul;
                }
                else
                {
                    Elemento.style.borderLeftColor = StyleKeyword.Null;
                    Elemento.style.borderRightColor = StyleKeyword.Null;
                    Elemento.style.borderTopColor = StyleKeyword.Null;
                    Elemento.style.borderBottomColor = StyleKeyword.Null;
                }

                _botaoChamar.style.display =
                    desbloqueado && !petAtivo ? DisplayStyle.Flex : DisplayStyle.None;
                _botaoDispensar.style.display =
                    desbloqueado && petAtivo ? DisplayStyle.Flex : DisplayStyle.None;
                _seloObtido.style.display = desbloqueado ? DisplayStyle.Flex : DisplayStyle.None;
                _seloBloqueado.style.display = desbloqueado ? DisplayStyle.None : DisplayStyle.Flex;
                _rotuloOrigem.style.display = desbloqueado ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }
}
