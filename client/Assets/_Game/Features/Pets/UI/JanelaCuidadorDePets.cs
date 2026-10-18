using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Pets;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Pets.UI
{
    /// <summary>
    /// Janela de comércio de pets no cercado do cuidador de pets (#1068).
    /// Aberta ao falar com a Cuidadora Mirna na Brasa Mansa.
    /// </summary>
    public sealed class JanelaCuidadorDePets : JanelaDoJogo
    {
        public const string Chave = "pets.cuidador";
        public const string Titulo = "Cuidador de Pets";
        public const int Ordem = 105;

        private VisualElement _listaContainer;
        private readonly List<ItemOfertaPet> _itens = new List<ItemOfertaPet>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                Titulo,
                null,
                Ordem,
                () => new JanelaCuidadorDePets()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 520f;
            conteudo.style.maxWidth = 620f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            var topo = new VisualElement();
            topo.style.marginBottom = 10f;
            topo.style.paddingBottom = 6f;
            topo.style.borderBottomWidth = 1f;
            topo.style.borderBottomColor = new Color(0.25f, 0.28f, 0.35f, 0.8f);

            var titulo = new Label("Cercado da Brasa Mansa");
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.style.fontSize = 16f;
            titulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            titulo.style.color = new Color(0.92f, 0.94f, 0.98f);
            topo.Add(titulo);

            var subtitulo = new Label("Cuidadora Mirna · Pets Comuns e Companheiros");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.style.fontSize = 12f;
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            subtitulo.style.marginTop = 2f;
            topo.Add(subtitulo);

            conteudo.Add(topo);

            var descricao = new Label(
                "Adquira companheiros fiéis por coroas para segui-lo pelo mundo e recolher itens caídos ao chão."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.style.fontSize = 11.5f;
            descricao.style.color = new Color(0.72f, 0.76f, 0.84f);
            descricao.style.marginBottom = 12f;
            conteudo.Add(descricao);

            _listaContainer = new VisualElement();
            _listaContainer.style.flexDirection = FlexDirection.Column;
            conteudo.Add(_listaContainer);

            ConstruirOfertas();

            var rodape = new Label(
                "Dica: Após a compra, o pet é registrado em sua conta e pode ser chamado na janela [U] (Companheiros)."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.style.fontSize = 11f;
            rodape.style.marginTop = 12f;
            rodape.style.color = new Color(0.60f, 0.66f, 0.75f);
            conteudo.Add(rodape);
        }

        private void ConstruirOfertas()
        {
            _listaContainer.Clear();
            _itens.Clear();

            foreach (var oferta in ConfigCuidadorDePets.Ofertas)
            {
                var item = new ItemOfertaPet(oferta, AoComprarOferta);
                _itens.Add(item);
                _listaContainer.Add(item.Elemento);
            }
        }

        protected override void AoAbrir()
        {
            ApresentadorDePets.ColecaoMudou += AtualizarOfertas;
            ApresentadorDePets.CompraPetResultado += AoReceberRespostaCompra;
            AtualizarOfertas();
        }

        protected override void AoFechar()
        {
            ApresentadorDePets.ColecaoMudou -= AtualizarOfertas;
            ApresentadorDePets.CompraPetResultado -= AoReceberRespostaCompra;
        }

        private void AtualizarOfertas()
        {
            foreach (var item in _itens)
            {
                var possui = ApresentadorDePets.Possui((EspecieDePet)item.Oferta.Especie);
                item.AtualizarEstado(possui);
            }
        }

        private void AoComprarOferta(int especie)
        {
            ApresentadorDePets.ComprarPetCuidador(especie);
        }

        private void AoReceberRespostaCompra(RespostaCompraPet resposta)
        {
            AtualizarOfertas();
        }

        private sealed class ItemOfertaPet
        {
            public readonly PetCuidadorDto Oferta;
            public readonly VisualElement Elemento;
            private readonly Button _botaoComprar;
            private readonly System.Action<int> _onComprar;

            public ItemOfertaPet(PetCuidadorDto oferta, System.Action<int> onComprar)
            {
                Oferta = oferta;
                _onComprar = onComprar;

                Elemento = new VisualElement();
                Elemento.style.flexDirection = FlexDirection.Row;
                Elemento.style.alignItems = Align.Center;
                Elemento.style.justifyContent = Justify.SpaceBetween;
                Elemento.style.paddingLeft = 12f;
                Elemento.style.paddingRight = 12f;
                Elemento.style.paddingTop = 8f;
                Elemento.style.paddingBottom = 8f;
                Elemento.style.marginBottom = 6f;
                Elemento.style.backgroundColor = new Color(0.12f, 0.14f, 0.20f, 0.75f);
                Elemento.style.borderTopWidth = 1f;
                Elemento.style.borderBottomWidth = 1f;
                Elemento.style.borderLeftWidth = 1f;
                Elemento.style.borderRightWidth = 1f;
                Elemento.style.borderTopColor = new Color(0.24f, 0.28f, 0.38f, 0.6f);
                Elemento.style.borderBottomColor = new Color(0.24f, 0.28f, 0.38f, 0.6f);
                Elemento.style.borderLeftColor = new Color(0.24f, 0.28f, 0.38f, 0.6f);
                Elemento.style.borderRightColor = new Color(0.24f, 0.28f, 0.38f, 0.6f);
                Elemento.style.borderTopLeftRadius = 4f;
                Elemento.style.borderTopRightRadius = 4f;
                Elemento.style.borderBottomLeftRadius = 4f;
                Elemento.style.borderBottomRightRadius = 4f;

                var info = new VisualElement();
                info.style.flexDirection = FlexDirection.Column;
                info.style.flexGrow = 1f;
                info.style.marginRight = 12f;

                var linhaNome = new VisualElement();
                linhaNome.style.flexDirection = FlexDirection.Row;
                linhaNome.style.alignItems = Align.Center;

                var rotuloNome = new Label(oferta.Nome);
                rotuloNome.AddToClassList(EstiloCore.Texto);
                rotuloNome.style.fontSize = 13f;
                rotuloNome.style.unityFontStyleAndWeight = FontStyle.Bold;
                rotuloNome.style.color = new Color(0.94f, 0.94f, 0.98f);
                linhaNome.Add(rotuloNome);

                var badgePreco = new Label($" {oferta.PrecoCoroas} coroas");
                badgePreco.AddToClassList(EstiloCore.Texto);
                badgePreco.style.fontSize = 11.5f;
                badgePreco.style.unityFontStyleAndWeight = FontStyle.Bold;
                badgePreco.style.color = new Color(0.95f, 0.78f, 0.25f);
                badgePreco.style.marginLeft = 8f;
                linhaNome.Add(badgePreco);

                info.Add(linhaNome);

                var rotuloDesc = new Label(oferta.Descricao);
                rotuloDesc.AddToClassList(EstiloCore.Texto);
                rotuloDesc.style.fontSize = 11f;
                rotuloDesc.style.color = new Color(0.65f, 0.70f, 0.78f);
                rotuloDesc.style.marginTop = 2f;
                info.Add(rotuloDesc);

                Elemento.Add(info);

                _botaoComprar = new Button(() => _onComprar?.Invoke(Oferta.Especie));
                _botaoComprar.text = $"Comprar ({oferta.PrecoCoroas})";
                _botaoComprar.AddToClassList(EstiloCore.Botao);
                _botaoComprar.AddToClassList(EstiloCore.BotaoPrincipal);
                _botaoComprar.style.minWidth = 130f;
                _botaoComprar.style.height = 30f;
                Elemento.Add(_botaoComprar);
            }

            public void AtualizarEstado(bool possui)
            {
                if (possui)
                {
                    _botaoComprar.text = "Já Possui";
                    _botaoComprar.SetEnabled(false);
                    _botaoComprar.style.opacity = 0.5f;
                }
                else
                {
                    _botaoComprar.text = $"Comprar ({Oferta.PrecoCoroas})";
                    _botaoComprar.SetEnabled(true);
                    _botaoComprar.style.opacity = 1f;
                }
            }
        }
    }
}
