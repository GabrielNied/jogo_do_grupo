using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Criação de personagem: nome e raça. O client só confere o formato do nome
    /// (<see cref="RegrasPersonagem.ValidarNome"/>) para dar retorno rápido; o servidor valida
    /// tudo de novo e decide unicidade e limite.
    /// </summary>
    internal sealed class TelaCriacaoPersonagem : Tela
    {
        private readonly List<Raca> _racas = new List<Raca>();

        private TextField _nome;
        private RadioButtonGroup _raca;
        private Button _confirmar;
        private Button _voltar;
        private Label _mensagem;
        private bool _esperando;

        protected override void Montar(VisualElement raiz)
        {
            EstiloPersonagem.Aplicar(raiz);

            var painel = new VisualElement();
            painel.AddToClassList(EstiloCore.Painel);

            var titulo = new Label("Novo personagem");
            titulo.AddToClassList(EstiloCore.Titulo);

            var subtitulo = new Label("Escolha o nome e a raça");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);

            _nome = new TextField("Nome") { maxLength = RegrasPersonagem.NomeMaximo };
            _nome.AddToClassList(EstiloCore.Campo);
            _nome.RegisterValueChangedCallback(AoMudarNome);
            _nome.RegisterCallback<KeyDownEvent>(AoTeclarNoNome, TrickleDown.TrickleDown);

            _raca = MontarRacas();

            _confirmar = new Button(AoClicarConfirmar) { text = "Confirmar" };
            _confirmar.AddToClassList(EstiloCore.Botao);
            _confirmar.AddToClassList(EstiloCore.BotaoPrincipal);

            _voltar = new Button(AoClicarVoltar) { text = "Voltar" };
            _voltar.AddToClassList(EstiloCore.Botao);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(_confirmar);
            linha.Add(_voltar);

            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Mensagem);

            painel.Add(titulo);
            painel.Add(subtitulo);
            painel.Add(_nome);
            painel.Add(_raca);
            painel.Add(linha);
            painel.Add(_mensagem);
            raiz.Add(painel);
        }

        protected override void AoAbrir()
        {
            // Dá o foco depois do primeiro layout, quando o campo já pode receber foco.
            _nome.schedule.Execute(() => _nome.Focus());
        }

        /// <summary>Uma opção por valor do enum <see cref="Raca"/>; a primeira já vem escolhida.</summary>
        private RadioButtonGroup MontarRacas()
        {
            var nomes = new List<string>();
            foreach (Raca raca in Enum.GetValues(typeof(Raca)))
            {
                _racas.Add(raca);
                nomes.Add(TextosPersonagem.NomeDaRaca(raca));
            }

            var grupo = new RadioButtonGroup("Raça", nomes);
            grupo.AddToClassList(EstiloCore.Campo);
            grupo.AddToClassList(EstiloPersonagem.Racas);
            grupo.value = _racas.Count > 0 ? 0 : -1;
            return grupo;
        }

        private void AoMudarNome(ChangeEvent<string> evt)
        {
            if (_esperando)
            {
                return;
            }

            // Campo vazio não é erro ainda: a pessoa só não começou a digitar.
            var nome = evt.newValue;
            if (string.IsNullOrEmpty(nome) || RegrasPersonagem.ValidarNome(nome) == null)
            {
                MostrarMensagem(null);
            }
            else
            {
                MostrarMensagem(TextosPersonagem.NomeInvalido, EstiloCore.MensagemErro);
            }
        }

        private void AoTeclarNoNome(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.StopPropagation();
                AoClicarConfirmar();
            }
        }

        private async void AoClicarConfirmar()
        {
            if (_esperando)
            {
                return;
            }

            var nome = _nome.value;
            if (RegrasPersonagem.ValidarNome(nome) != null)
            {
                MostrarMensagem(TextosPersonagem.NomeInvalido, EstiloCore.MensagemErro);
                return;
            }

            var indice = _raca.value;
            if (indice < 0 || indice >= _racas.Count)
            {
                MostrarMensagem("Escolha uma raça.", EstiloCore.MensagemErro);
                return;
            }

            var pedido = new CriarPersonagemRequest { Nome = nome, Raca = _racas[indice] };

            DefinirEsperando(true);
            MostrarMensagem("Criando o personagem...");
            try
            {
                var resposta = await ClienteApi.Post<PersonagemResumo>(
                    RotasPersonagem.Personagens,
                    pedido
                );

                // 401 já encerrou a sessão e levou para o login: esta tela fechou.
                if (!Aberta)
                {
                    return;
                }

                if (!resposta.Sucesso)
                {
                    MostrarRecusa(resposta.CodigoErro);
                    return;
                }

                // Volta para a seleção com o novo personagem selecionado.
                TelasPersonagem.IdParaSelecionar = resposta.Corpo.Id;
                Navegacao.Abrir(TelasPersonagem.Selecao);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarMensagem(TextosPersonagem.Generica, EstiloCore.MensagemErro);
                }
            }
            finally
            {
                if (Aberta)
                {
                    DefinirEsperando(false);
                }
            }
        }

        private void MostrarRecusa(string codigo)
        {
            if (TextosPersonagem.FoiCancelada(codigo))
            {
                MostrarMensagem(null);
                return;
            }

            MostrarMensagem(TextosPersonagem.MensagemDaRecusa(codigo), EstiloCore.MensagemErro);
        }

        private void AoClicarVoltar()
        {
            if (_esperando)
            {
                return;
            }

            Navegacao.Abrir(TelasPersonagem.Selecao);
        }

        private void DefinirEsperando(bool esperando)
        {
            _esperando = esperando;
            _nome.SetEnabled(!esperando);
            _raca.SetEnabled(!esperando);
            _confirmar.SetEnabled(!esperando);
            _voltar.SetEnabled(!esperando);
        }

        private void MostrarMensagem(string texto, string classe = null)
        {
            _mensagem.text = texto ?? "";
            _mensagem.RemoveFromClassList(EstiloCore.MensagemErro);
            _mensagem.RemoveFromClassList(EstiloCore.MensagemAviso);
            if (classe != null)
            {
                _mensagem.AddToClassList(classe);
            }
        }
    }
}
