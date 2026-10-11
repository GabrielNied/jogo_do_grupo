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

            conteudo.Add(_mensagem);
            conteudo.Add(_dados);
            conteudo.Add(_nota);
        }

        protected override void AoAbrir()
        {
            Carregar();
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
