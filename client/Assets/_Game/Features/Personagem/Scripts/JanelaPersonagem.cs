using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Combate;
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
        private Label _equipamentoTitulo;
        private VisualElement _equipamento;
        private Label _secundariosTitulo;
        private VisualElement _secundarios;

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
            _equipamentoTitulo = new Label(TextosSecundarios.TituloEquipamento);
            _equipamentoTitulo.AddToClassList(EstiloCore.Texto);
            _equipamento = new VisualElement();
            _secundariosTitulo = new Label(TextosSecundarios.Titulo);
            _secundariosTitulo.AddToClassList(EstiloCore.Texto);
            _secundarios = new VisualElement();

            conteudo.Add(_mensagem);
            conteudo.Add(_dados);
            conteudo.Add(_barraTexto);
            conteudo.Add(barra);
            conteudo.Add(_atributos);
            conteudo.Add(_equipamentoTitulo);
            conteudo.Add(_equipamento);
            conteudo.Add(_secundariosTitulo);
            conteudo.Add(_secundarios);
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
                await CarregarSecundarios(personagemId);
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

        /// <summary>
        /// Pede os nove secundários ao servidor (rota de recursos) e mostra o valor e a origem de
        /// cada um. Se falhar, só esta seção avisa; o resto da janela segue.
        /// </summary>
        private async Awaitable CarregarSecundarios(long personagemId)
        {
            MostrarTextoDosSecundarios(TextosSecundarios.Carregando, false);
            try
            {
                var resposta = await ClienteApi.Get<RecursosResponse>(
                    RotasCombate.RecursosDe(personagemId)
                );
                if (!Aberta)
                {
                    return;
                }

                if (
                    !resposta.Sucesso
                    || resposta.Corpo?.Secundarios == null
                    || resposta.Corpo.Secundarios.Length == 0
                )
                {
                    MostrarTextoDosSecundarios(TextosSecundarios.Erro, true);
                    return;
                }

                MostrarEquipamento(resposta.Corpo.DoEquipamento);
                _secundarios.Clear();
                foreach (var calculado in resposta.Corpo.Secundarios)
                {
                    AdicionarSecundario(calculado);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarTextoDosSecundarios(TextosSecundarios.Erro, true);
                }
            }
        }

        // O que as peças dão ao combate (dano da arma, bônus de dano, vida, defesa...), separado dos
        // atributos do personagem: o equipamento nunca soma neles.
        private void MostrarEquipamento(string[] linhas)
        {
            _equipamento.Clear();
            if (linhas == null || linhas.Length == 0)
            {
                var vazio = new Label(TextosSecundarios.SemEquipamento);
                vazio.AddToClassList(EstiloCore.Mensagem);
                _equipamento.Add(vazio);
                return;
            }

            foreach (var linha in linhas)
            {
                var texto = new Label(linha);
                texto.AddToClassList(EstiloCore.Texto);
                _equipamento.Add(texto);
            }
        }

        private void AdicionarSecundario(SecundarioCalculado calculado)
        {
            var rotulo = new Label(TextosSecundarios.Rotulo(calculado.Atributo));
            rotulo.AddToClassList(EstiloCore.Texto);
            rotulo.AddToClassList(EstiloCore.RotuloDado);

            var valor = new Label(TextosSecundarios.Valor(calculado.Atributo, calculado.Valor));
            valor.AddToClassList(EstiloCore.Texto);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaDado);
            linha.Add(rotulo);
            linha.Add(valor);
            _secundarios.Add(linha);

            var origem = new Label(TextosSecundarios.Origem(calculado));
            origem.AddToClassList(EstiloCore.Mensagem);
            _secundarios.Add(origem);
        }

        private void MostrarTextoDosSecundarios(string texto, bool erro)
        {
            _secundarios.Clear();
            var mensagem = new Label(texto);
            mensagem.AddToClassList(EstiloCore.Mensagem);
            mensagem.EnableInClassList(EstiloCore.MensagemErro, erro);
            _secundarios.Add(mensagem);
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
            _atributos.Clear();
            var livres = new Label(TextosPersonagem.PontosDaArvore + estado.PontosLivres);
            livres.AddToClassList(EstiloCore.Texto);
            _atributos.Add(livres);
            var dica = new Label(TextosPersonagem.DicaDaArvore);
            dica.AddToClassList(EstiloCore.Mensagem);
            _atributos.Add(dica);
        }

        private void MostrarMensagem(string texto, bool erro)
        {
            _dados.Clear();
            _secundarios.Clear();
            _nota.style.display = DisplayStyle.None;
            _mensagem.text = texto;
            _mensagem.style.display = DisplayStyle.Flex;
            _mensagem.EnableInClassList(EstiloCore.MensagemErro, erro);
        }
    }
}
