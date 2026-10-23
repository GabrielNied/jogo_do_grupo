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
    /// Janela de comércio de montarias no estábulo da cidade (#1067).
    /// Aberta ao falar com o Cavalariço Roderic na Brasa Mansa.
    /// </summary>
    public sealed class JanelaEstabulo : JanelaDoJogo
    {
        public const string Chave = "montarias.estabulo";
        public const string Titulo = "Estábulo";
        public const int Ordem = 104;

        private VisualElement _listaContainer;
        private readonly List<ItemOfertaEstabulo> _itens = new List<ItemOfertaEstabulo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, null, Ordem, () => new JanelaEstabulo());
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloMontarias.Aplicar(conteudo);
            conteudo.AddToClassList("montarias-conteudo");
            conteudo.AddToClassList("montarias-conteudo-estabulo");

            var topo = new VisualElement();
            topo.AddToClassList("montarias-estabulo-topo");

            var titulo = new Label("Estábulo da Brasa Mansa");
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.AddToClassList("montarias-estabulo-titulo");
            topo.Add(titulo);

            var subtitulo = new Label("Cavalariço Roderic · Montarias Básicas de Asterra");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.AddToClassList("montarias-estabulo-subtitulo");
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            topo.Add(subtitulo);

            conteudo.Add(topo);

            var descricao = new Label(
                "Adquira montarias leais por coroas para cavalgar pelas trilhas a 9,0 m/s autoritativos (+63% de velocidade)."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.AddToClassList("montarias-descricao");
            conteudo.Add(descricao);

            _listaContainer = new VisualElement();
            _listaContainer.AddToClassList("montarias-estabulo-lista");
            conteudo.Add(_listaContainer);

            ConstruirOfertas();

            var rodape = new Label(
                "Dica: Após a compra, a montaria é registrada na sua conta e pode ser equipada na janela [P]."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.AddToClassList("montarias-rodape");
            conteudo.Add(rodape);
        }

        private void ConstruirOfertas()
        {
            _listaContainer.Clear();
            _itens.Clear();

            foreach (var oferta in ConfigEstabulo.Ofertas)
            {
                var item = new ItemOfertaEstabulo(oferta, AoComprarOferta);
                _itens.Add(item);
                _listaContainer.Add(item.Elemento);
            }
        }

        protected override void AoAbrir()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.ColecaoMudou += AtualizarOfertas;
                ControleDeMontaria.Instancia.CompraEstabuloRecebida += AoReceberRespostaCompra;
            }

            AtualizarOfertas();
        }

        protected override void AoFechar()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.ColecaoMudou -= AtualizarOfertas;
                ControleDeMontaria.Instancia.CompraEstabuloRecebida -= AoReceberRespostaCompra;
            }
        }

        private void AoComprarOferta(int tipoMontaria)
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.ComprarMontariaEstabulo(tipoMontaria);
            }
        }

        private void AoReceberRespostaCompra(RespostaCompraEstabulo _)
        {
            AtualizarOfertas();
        }

        private void AtualizarOfertas()
        {
            var controle = ControleDeMontaria.Instancia;
            for (var i = 0; i < _itens.Count; i++)
            {
                var tipo = _itens[i].Oferta.TipoMontaria;
                var possui = controle != null && controle.PossuiMontaria(tipo);
                _itens[i].AtualizarEstado(possui);
            }
        }

        private sealed class ItemOfertaEstabulo
        {
            public MontariaEstabuloDto Oferta { get; }
            public VisualElement Elemento { get; }

            private readonly Button _botaoComprar;
            private readonly System.Action<int> _onComprar;

            public ItemOfertaEstabulo(MontariaEstabuloDto oferta, System.Action<int> onComprar)
            {
                Oferta = oferta;
                _onComprar = onComprar;

                Elemento = new VisualElement();
                Elemento.AddToClassList("montarias-oferta");

                var info = new VisualElement();
                info.AddToClassList("montarias-oferta-info");

                var linhaNome = new VisualElement();
                linhaNome.AddToClassList("montarias-oferta-linha");

                var rotuloNome = new Label(oferta.Nome);
                rotuloNome.AddToClassList(EstiloCore.Texto);
                rotuloNome.AddToClassList("montarias-oferta-nome");
                linhaNome.Add(rotuloNome);

                var rotuloPreco = new Label($" · {oferta.PrecoCoroas} Coroas");
                rotuloPreco.AddToClassList(EstiloCore.Texto);
                rotuloPreco.AddToClassList("montarias-oferta-preco");
                rotuloPreco.style.color = new Color(0.95f, 0.80f, 0.30f);
                linhaNome.Add(rotuloPreco);

                info.Add(linhaNome);

                var rotuloDesc = new Label(oferta.Descricao);
                rotuloDesc.AddToClassList(EstiloCore.Texto);
                rotuloDesc.AddToClassList("montarias-oferta-desc");
                rotuloDesc.style.color = new Color(0.65f, 0.70f, 0.78f);
                info.Add(rotuloDesc);

                Elemento.Add(info);

                _botaoComprar = new Button(() => _onComprar?.Invoke(Oferta.TipoMontaria));
                _botaoComprar.text = $"Comprar ({oferta.PrecoCoroas})";
                _botaoComprar.AddToClassList(EstiloCore.Botao);
                _botaoComprar.AddToClassList(EstiloCore.BotaoPrincipal);
                _botaoComprar.AddToClassList("montarias-oferta-comprar");
                Elemento.Add(_botaoComprar);
            }

            public void AtualizarEstado(bool possui)
            {
                if (possui)
                {
                    _botaoComprar.text = "Já Possui";
                    _botaoComprar.SetEnabled(false);
                    _botaoComprar.AddToClassList("montarias-oferta-comprar-indisponivel");
                }
                else
                {
                    _botaoComprar.text = $"Comprar ({Oferta.PrecoCoroas})";
                    _botaoComprar.SetEnabled(true);
                    _botaoComprar.RemoveFromClassList("montarias-oferta-comprar-indisponivel");
                }
            }
        }
    }
}
