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
            EstiloPets.Aplicar(conteudo);
            conteudo.AddToClassList("pets-conteudo");
            conteudo.AddToClassList("pets-conteudo-cuidador");

            var topo = new VisualElement();
            topo.AddToClassList("pets-cuidador-topo");

            var titulo = new Label("Cercado da Brasa Mansa");
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.AddToClassList("pets-cuidador-titulo");
            topo.Add(titulo);

            var subtitulo = new Label("Cuidadora Mirna · Pets Comuns e Companheiros");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.AddToClassList("pets-cuidador-subtitulo");
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            topo.Add(subtitulo);

            conteudo.Add(topo);

            var descricao = new Label(
                "Adquira companheiros fiéis por coroas para segui-lo pelo mundo e recolher itens caídos ao chão."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.AddToClassList("pets-descricao");
            conteudo.Add(descricao);

            _listaContainer = new VisualElement();
            _listaContainer.AddToClassList("pets-cuidador-lista");
            conteudo.Add(_listaContainer);

            ConstruirOfertas();

            var rodape = new Label(
                "Dica: Após a compra, o pet é registrado em sua conta e pode ser chamado na janela [U] (Companheiros)."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.AddToClassList("pets-rodape");
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
                Elemento.AddToClassList("pets-oferta");

                var info = new VisualElement();
                info.AddToClassList("pets-oferta-info");

                var linhaNome = new VisualElement();
                linhaNome.AddToClassList("pets-oferta-linha");

                var rotuloNome = new Label(oferta.Nome);
                rotuloNome.AddToClassList(EstiloCore.Texto);
                rotuloNome.AddToClassList("pets-oferta-nome");
                rotuloNome.style.color = new Color(0.94f, 0.94f, 0.98f);
                linhaNome.Add(rotuloNome);

                var badgePreco = new Label($" {oferta.PrecoCoroas} coroas");
                badgePreco.AddToClassList(EstiloCore.Texto);
                badgePreco.AddToClassList("pets-oferta-preco");
                badgePreco.style.color = new Color(0.95f, 0.78f, 0.25f);
                linhaNome.Add(badgePreco);

                info.Add(linhaNome);

                var rotuloDesc = new Label(oferta.Descricao);
                rotuloDesc.AddToClassList(EstiloCore.Texto);
                rotuloDesc.AddToClassList("pets-oferta-desc");
                rotuloDesc.style.color = new Color(0.65f, 0.70f, 0.78f);
                info.Add(rotuloDesc);

                Elemento.Add(info);

                _botaoComprar = new Button(() => _onComprar?.Invoke(Oferta.Especie));
                _botaoComprar.text = $"Comprar ({oferta.PrecoCoroas})";
                _botaoComprar.AddToClassList(EstiloCore.Botao);
                _botaoComprar.AddToClassList(EstiloCore.BotaoPrincipal);
                _botaoComprar.AddToClassList("pets-oferta-comprar");
                Elemento.Add(_botaoComprar);
            }

            public void AtualizarEstado(bool possui)
            {
                if (possui)
                {
                    _botaoComprar.text = "Já Possui";
                    _botaoComprar.SetEnabled(false);
                    _botaoComprar.AddToClassList("pets-oferta-comprar-indisponivel");
                }
                else
                {
                    _botaoComprar.text = $"Comprar ({Oferta.PrecoCoroas})";
                    _botaoComprar.SetEnabled(true);
                    _botaoComprar.RemoveFromClassList("pets-oferta-comprar-indisponivel");
                }
            }
        }
    }
}
