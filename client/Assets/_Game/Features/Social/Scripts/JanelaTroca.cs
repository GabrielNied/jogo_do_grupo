using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Janela da troca direta (#175): os dois lados da oferta lado a lado, a bolsa para escolher
    /// o que oferecer e os botões de aceitar e cancelar. Abre sozinha quando a troca começa e
    /// fechar a janela cancela a troca. O servidor decide tudo; a janela só mostra o que ele
    /// mandou e pede mudanças.
    /// </summary>
    internal sealed class JanelaTroca : JanelaDoJogo
    {
        public const string Chave = "social.troca";
        public const string TituloPadrao = TextosTroca.TituloDaJanela;
        public const string Tecla = "";
        public const int Ordem = 56;

        private readonly EstadoDaTrocaDoClient _estado;

        private Label _aviso;
        private Label _titulo;
        private LongField _coroasMinhas;
        private Label _tituloDele;
        private Label _coroasDele;
        private VisualElement _itensMeus;
        private VisualElement _itensDele;
        private VisualElement _bolsa;
        private Label _situacao;
        private Button _aceitar;

        public override bool MolduraSocial => true;

        public JanelaTroca()
            : this(EstadoDaTrocaDoClient.Instancia) { }

        internal JanelaTroca(EstadoDaTrocaDoClient estado)
        {
            _estado = estado;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                TituloPadrao,
                Tecla,
                Ordem,
                () => new JanelaTroca()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloSocial.Aplicar(conteudo);

            _titulo = new Label("");
            _titulo.AddToClassList(EstiloCore.Titulo);
            conteudo.Add(_titulo);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);
            conteudo.Add(_aviso);

            var colunas = new VisualElement();
            colunas.AddToClassList(EstiloSocial.TrocaColunas);

            var meu = new VisualElement();
            meu.AddToClassList(EstiloSocial.TrocaLado);
            var tituloMeu = new Label(TextosTroca.VoceOferece);
            tituloMeu.AddToClassList(EstiloCore.Subtitulo);
            meu.Add(tituloMeu);
            _coroasMinhas = new LongField(TextosTroca.Coroas) { isDelayed = true };
            _coroasMinhas.AddToClassList(EstiloCore.Campo);
            _coroasMinhas.RegisterValueChangedCallback(AoMudarCoroas);
            meu.Add(_coroasMinhas);
            _itensMeus = new VisualElement();
            meu.Add(_itensMeus);
            colunas.Add(meu);

            var dele = new VisualElement();
            dele.AddToClassList(EstiloSocial.TrocaLado);
            _tituloDele = new Label("");
            _tituloDele.AddToClassList(EstiloCore.Subtitulo);
            dele.Add(_tituloDele);
            _coroasDele = new Label("");
            _coroasDele.AddToClassList(EstiloCore.Texto);
            dele.Add(_coroasDele);
            _itensDele = new VisualElement();
            dele.Add(_itensDele);
            colunas.Add(dele);

            conteudo.Add(colunas);

            var tituloBolsa = new Label(TextosTroca.SuaBolsa);
            tituloBolsa.AddToClassList(EstiloCore.Subtitulo);
            conteudo.Add(tituloBolsa);

            var rolagem = new ScrollView(ScrollViewMode.Vertical);
            rolagem.AddToClassList(EstiloSocial.TrocaBolsa);
            _bolsa = new VisualElement();
            rolagem.Add(_bolsa);
            conteudo.Add(rolagem);

            _situacao = new Label();
            _situacao.AddToClassList(EstiloCore.Texto);
            conteudo.Add(_situacao);

            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloCore.LinhaBotoes);
            _aceitar = new Button(_estado.Confirmar) { text = TextosTroca.Aceitar };
            _aceitar.AddToClassList(EstiloCore.Botao);
            _aceitar.AddToClassList(EstiloCore.BotaoPrincipal);
            var cancelar = new Button(_estado.Cancelar) { text = TextosTroca.Cancelar };
            cancelar.AddToClassList(EstiloCore.Botao);
            acoes.Add(_aceitar);
            acoes.Add(cancelar);
            conteudo.Add(acoes);

            Redesenhar();
        }

        protected override void AoAbrir()
        {
            _estado.Mudou += Redesenhar;
            Redesenhar();
        }

        protected override void AoFechar()
        {
            _estado.Mudou -= Redesenhar;
            // Fechar a janela no meio da troca é desistir dela.
            if (_estado.EmTroca)
            {
                _estado.Cancelar();
            }
        }

        private void AoMudarCoroas(ChangeEvent<long> evento)
        {
            _estado.DefinirCoroas(evento.newValue);
        }

        private void Redesenhar()
        {
            if (_titulo == null)
            {
                return;
            }

            var troca = _estado.Troca;
            var texto = _estado.MensagemRestante > 0f ? _estado.Mensagem : "";
            _aviso.text = texto;
            EstiloSocial.Mostrar(_aviso, !string.IsNullOrEmpty(texto));
            _aviso.EnableInClassList(EstiloCore.MensagemErro, _estado.MensagemEhErro);

            if (troca == null)
            {
                _titulo.text = TextosTroca.TituloDaJanela;
                return;
            }

            DefinirTitulo($"{TextosTroca.TituloDaJanela}: {troca.NomeDoOutro}");
            _titulo.text = $"{TextosTroca.TituloDaJanela} com {troca.NomeDoOutro}";
            _tituloDele.text = TextosTroca.OfertaDoOutro(troca.NomeDoOutro);
            _coroasDele.text = $"{TextosTroca.Coroas}: {troca.Dele.Coroas}";
            _coroasMinhas.SetValueWithoutNotify(troca.Minha.Coroas);

            DesenharOferta(_itensMeus, troca.Minha.Itens, true);
            DesenharOferta(_itensDele, troca.Dele.Itens, false);
            DesenharBolsa(troca);

            _situacao.text = troca.MeuAceite
                ? (troca.AceiteDele ? "" : TextosTroca.VoceAceitou)
                : (troca.AceiteDele ? TextosTroca.OutroAceitou : TextosTroca.AguardandoOutro);
            _aceitar.SetEnabled(!troca.MeuAceite);
        }

        private void DesenharOferta(VisualElement lista, EntradaInventario[] itens, bool minha)
        {
            lista.Clear();
            if (itens == null || itens.Length == 0)
            {
                var vazio = new Label(TextosTroca.NadaOferecido);
                vazio.AddToClassList(EstiloCore.Mensagem);
                lista.Add(vazio);
                return;
            }

            foreach (var item in itens)
            {
                var id = item.InstanciaId;
                if (minha)
                {
                    var botao = new Button(() => _estado.AlternarItem(id))
                    {
                        text = ApresentacaoTroca.NomeDoItem(item),
                    };
                    botao.AddToClassList(EstiloCore.Botao);
                    botao.AddToClassList(EstiloSocial.TrocaItem);
                    lista.Add(botao);
                }
                else
                {
                    var linha = new Label(ApresentacaoTroca.NomeDoItem(item));
                    linha.AddToClassList(EstiloCore.Texto);
                    linha.AddToClassList(EstiloSocial.TrocaItem);
                    lista.Add(linha);
                }
            }
        }

        private void DesenharBolsa(EstadoDaTroca troca)
        {
            _bolsa.Clear();
            var bolsa = _estado.Bolsa;
            if (bolsa == null || bolsa.Itens == null || bolsa.Itens.Length == 0)
            {
                var vazio = new Label(TextosTroca.BolsaVazia);
                vazio.AddToClassList(EstiloCore.Mensagem);
                _bolsa.Add(vazio);
                return;
            }

            var naOferta = new HashSet<long>();
            foreach (var item in troca.Minha.Itens)
            {
                naOferta.Add(item.InstanciaId);
            }

            foreach (var item in bolsa.Itens)
            {
                var id = item.InstanciaId;
                var oferecido = naOferta.Contains(id);
                var botao = new Button(() => _estado.AlternarItem(id))
                {
                    text = ApresentacaoTroca.NomeDoItem(item) + (oferecido ? " (na oferta)" : ""),
                };
                botao.AddToClassList(EstiloCore.Botao);
                botao.AddToClassList(EstiloSocial.TrocaItem);
                botao.EnableInClassList(EstiloSocial.TrocaItemOferecido, oferecido);
                _bolsa.Add(botao);
            }
        }
    }

    /// <summary>Como um item aparece na troca: nome e, se for equipamento, o tier.</summary>
    internal static class ApresentacaoTroca
    {
        public static string NomeDoItem(EntradaInventario item)
        {
            var nome = string.IsNullOrEmpty(item.Nome) ? $"Item {item.ItemId}" : item.Nome;
            if (item.Equipamento == null)
            {
                return nome;
            }

            return $"{nome} (tier {RegrasTier.Letra(RegrasTier.TierDe(item.Equipamento))})";
        }
    }
}
