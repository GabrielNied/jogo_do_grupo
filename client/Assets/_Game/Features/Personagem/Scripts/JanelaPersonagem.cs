using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Janela do personagem (tecla C), registrada nas <see cref="JanelasDoJogo"/>: mostra o
    /// nome, a raça e o nível do personagem que está no mundo, como o servidor informa na
    /// lista de personagens. Atributos e equipamentos entram quando existirem.
    /// </summary>
    internal sealed class JanelaPersonagem : JanelaDoJogo
    {
        public const string Chave = "personagem.ficha";
        public const string Titulo = "Personagem";
        public const string Tecla = "C";
        public const int Ordem = 10;

        private Label _mensagem;
        private VisualElement _dados;
        private Label _nota;
        private Label _barraTexto;
        private VisualElement _barraPreenchimento;
        private VisualElement _atributos;
        private Label _gastoMensagem;
        private Button _confirmar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaPersonagem());
        }

        protected override void Montar(VisualElement conteudo)
        {
            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Mensagem);

            _dados = new VisualElement();

            _nota = new Label(TextosPersonagem.NotaDaFicha);
            _nota.AddToClassList(EstiloCore.Mensagem);

            var barra = new VisualElement();
            barra.AddToClassList(EstiloCore.BarraCarregamento);
            _barraPreenchimento = new VisualElement();
            _barraPreenchimento.AddToClassList(EstiloCore.BarraCarregamentoPreenchimento);
            barra.Add(_barraPreenchimento);
            _barraTexto = new Label();
            _barraTexto.AddToClassList(EstiloCore.Texto);

            _atributos = new VisualElement();
            _gastoMensagem = new Label();
            _gastoMensagem.AddToClassList(EstiloCore.Mensagem);
            _confirmar = new Button(ConfirmarGasto) { text = "Gastar pontos" };
            _confirmar.AddToClassList(EstiloCore.Botao);
            _confirmar.AddToClassList(EstiloCore.BotaoPrincipal);

            conteudo.Add(_mensagem);
            conteudo.Add(_dados);
            conteudo.Add(_barraTexto);
            conteudo.Add(barra);
            conteudo.Add(_atributos);
            conteudo.Add(_gastoMensagem);
            conteudo.Add(_confirmar);
            conteudo.Add(_nota);
        }

        protected override void AoAbrir()
        {
            ProgressoDoClient.Estado.Mudou += AtualizarProgresso;
            Carregar();
            AtualizarProgresso();
        }

        protected override void AoFechar()
        {
            ProgressoDoClient.Estado.Mudou -= AtualizarProgresso;
        }

        /// <summary>Pede a lista ao servidor (<c>GET</c>) e mostra o personagem que está no mundo.</summary>
        private async void Carregar()
        {
            var personagemId = EntradaNoJogo.PersonagemId;
            MostrarMensagem(TextosPersonagem.CarregandoFicha, false);
            try
            {
                var resposta = await ClienteApi.Get<ListaPersonagensResponse>(
                    RotasPersonagem.Personagens
                );

                // A janela pode ter sido fechada durante a espera (ou a sessão expirou).
                if (!Aberta)
                {
                    return;
                }

                if (!resposta.Sucesso)
                {
                    if (!TextosPersonagem.FoiCancelada(resposta.CodigoErro))
                    {
                        MostrarMensagem(TextosPersonagem.ErroAoCarregarFicha, true);
                    }

                    return;
                }

                var personagem = TextosPersonagem.ProcurarPorId(
                    resposta.Corpo?.Personagens,
                    personagemId
                );
                if (personagem == null)
                {
                    MostrarMensagem(TextosPersonagem.ErroAoCarregarFicha, true);
                    return;
                }

                MostrarFicha(personagem);
                await CarregarProgresso(personagemId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarMensagem(TextosPersonagem.ErroAoCarregarFicha, true);
                }
            }
        }

        private void MostrarFicha(PersonagemResumo personagem)
        {
            DefinirTitulo(personagem.Nome);
            _mensagem.style.display = DisplayStyle.None;
            _nota.style.display = DisplayStyle.Flex;

            _dados.Clear();
            foreach (var linha in TextosPersonagem.LinhasDaFicha(personagem))
            {
                var rotulo = new Label(linha.Key);
                rotulo.AddToClassList(EstiloCore.Texto);
                rotulo.AddToClassList(EstiloCore.RotuloDado);

                var valor = new Label(linha.Value);
                valor.AddToClassList(EstiloCore.Texto);

                var elemento = new VisualElement();
                elemento.AddToClassList(EstiloCore.LinhaDado);
                elemento.Add(rotulo);
                elemento.Add(valor);
                _dados.Add(elemento);
            }
        }

        private async Awaitable CarregarProgresso(long personagemId)
        {
            var resposta = await ClienteApi.Get<ProgressoResponse>(
                RotasPersonagem.ProgressoDe(personagemId)
            );
            if (!Aberta || !resposta.Sucesso || resposta.Corpo == null)
            {
                return;
            }

            ProgressoDoClient.Estado.Aplicar(resposta.Corpo);
        }

        private void AtualizarProgresso()
        {
            if (!Aberta)
            {
                return;
            }

            var estado = ProgressoDoClient.Estado;
            _barraTexto.text = TextosPersonagem.TextoDaBarra(estado);
            _barraPreenchimento.style.width = Length.Percent(estado.FracaoDaBarra * 100f);
            _confirmar.SetEnabled(estado.PodeConfirmar);

            _atributos.Clear();
            AdicionarAtributo(
                TextosPersonagem.RotuloForca,
                "Forca",
                TextosPersonagem.EfeitoDaForca,
                estado.Distribuidos.Forca,
                estado.Rascunho.Forca
            );
            AdicionarAtributo(
                TextosPersonagem.RotuloVigor,
                "Vigor",
                TextosPersonagem.EfeitoDoVigor,
                estado.Distribuidos.Vigor,
                estado.Rascunho.Vigor
            );
            AdicionarAtributo(
                TextosPersonagem.RotuloAgilidade,
                "Agilidade",
                TextosPersonagem.EfeitoDaAgilidade,
                estado.Distribuidos.Agilidade,
                estado.Rascunho.Agilidade
            );
            AdicionarAtributo(
                TextosPersonagem.RotuloIntelecto,
                "Intelecto",
                TextosPersonagem.EfeitoDoIntelecto,
                estado.Distribuidos.Intelecto,
                estado.Rascunho.Intelecto
            );
            AdicionarAtributo(
                TextosPersonagem.RotuloEspirito,
                "Espirito",
                TextosPersonagem.EfeitoDoEspirito,
                estado.Distribuidos.Espirito,
                estado.Rascunho.Espirito
            );

            var livres = new Label("Pontos livres: " + estado.PontosLivres);
            livres.AddToClassList(EstiloCore.Texto);
            _atributos.Add(livres);
        }

        private void AdicionarAtributo(
            string rotulo,
            string chave,
            string efeito,
            int distribuido,
            int rascunho
        )
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            var texto = new Label(
                TextosPersonagem.LinhaDoAtributo(rotulo, distribuido + rascunho, efeito)
            );
            texto.AddToClassList(EstiloCore.Texto);
            texto.tooltip = efeito;
            var mais = new Button(() => ProgressoDoClient.Estado.AumentarRascunho(chave))
            {
                text = "+",
            };
            mais.AddToClassList(EstiloCore.Botao);
            mais.SetEnabled(
                RegrasAtributos.Total(ProgressoDoClient.Estado.Rascunho)
                    < ProgressoDoClient.Estado.PontosLivres
            );
            linha.Add(texto);
            linha.Add(mais);
            _atributos.Add(linha);
        }

        private async void ConfirmarGasto()
        {
            var pedido = ProgressoDoClient.Estado.MontarPedido();
            if (pedido == null)
            {
                return;
            }

            var personagemId = EntradaNoJogo.PersonagemId;
            var resposta = await ClienteApi.Post<ProgressoResponse>(
                RotasPersonagem.AtributosDe(personagemId),
                pedido
            );
            if (!Aberta)
            {
                return;
            }

            if (!resposta.Sucesso || resposta.Corpo == null)
            {
                _gastoMensagem.text = TextosPersonagem.MensagemDoGasto(resposta.CodigoErro);
                _gastoMensagem.EnableInClassList(EstiloCore.MensagemErro, true);
                return;
            }

            _gastoMensagem.text = "";
            ProgressoDoClient.Estado.Aplicar(resposta.Corpo);
        }

        private void MostrarMensagem(string texto, bool erro)
        {
            _dados.Clear();
            _nota.style.display = DisplayStyle.None;
            _mensagem.text = texto;
            _mensagem.style.display = DisplayStyle.Flex;
            _mensagem.EnableInClassList(EstiloCore.MensagemErro, erro);
        }
    }
}
