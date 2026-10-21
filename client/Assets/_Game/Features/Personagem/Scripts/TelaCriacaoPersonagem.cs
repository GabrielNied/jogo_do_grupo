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
        private Label _erroNome;
        private readonly List<Button> _botoesRaca = new List<Button>();
        private VisualElement _escolhaDeRaca;
        private VisualElement _cartaoDaRaca;
        private Image _imagem;
        private Label _avisoPrevia;
        private PreviaVisualPersonagem _previsao;
        private Label _racaNome;
        private Label _racaPovo;
        private Label _racaAparencia;
        private Label _racaTraco;
        private Label _racaSkill;
        private Label _racaTexto;
        private int _indiceRaca;
        private readonly AparenciaPersonagem _aparencia = new AparenciaPersonagem();
        private Label _rotuloTom;
        private Label _rotuloCabelo;
        private Label _rotuloCor;
        private Button _confirmar;
        private Button _voltar;
        private Label _mensagem;
        private bool _esperando;

        protected override void Montar(VisualElement raiz)
        {
            EstiloPersonagem.Aplicar(raiz);

            // Tela cheia, como a criação do WoW: raças à esquerda, o personagem no centro com a
            // aparência embaixo e o cartão da raça com o Criar à direita.
            var painel = new VisualElement();
            painel.AddToClassList("personagem-tela");
            painel.AddToClassList("personagem-painel-criacao-visual");

            var titulo = new Label("Criar personagem");
            titulo.AddToClassList(EstiloCore.Titulo);

            var subtitulo = new Label("Escolha o nome e a raça");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);

            _nome = new TextField("Nome") { maxLength = RegrasPersonagem.NomeMaximo };
            _nome.AddToClassList(EstiloCore.Campo);
            _nome.RegisterValueChangedCallback(AoMudarNome);
            _nome.RegisterCallback<KeyDownEvent>(AoTeclarNoNome, TrickleDown.TrickleDown);
            _erroNome = new Label();
            _erroNome.AddToClassList(EstiloCore.Mensagem);
            _erroNome.AddToClassList(EstiloCore.MensagemErro);
            _erroNome.AddToClassList("personagem-criacao-erro-nome");
            EstiloPersonagem.Mostrar(_erroNome, false);

            _escolhaDeRaca = MontarRacas();

            _confirmar = new Button(AoClicarConfirmar) { text = "Criar personagem" };
            _confirmar.AddToClassList(EstiloCore.Botao);
            _confirmar.AddToClassList(EstiloCore.BotaoPrincipal);

            _voltar = new Button(AoClicarVoltar) { text = "Voltar" };
            _voltar.AddToClassList(EstiloCore.Botao);

            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Mensagem);
            _cartaoDaRaca.Add(_mensagem);
            _cartaoDaRaca.Add(_confirmar);

            var rodape = new VisualElement();
            rodape.AddToClassList("personagem-selecao-rodape");
            rodape.Add(_voltar);

            painel.Add(titulo);
            painel.Add(subtitulo);
            painel.Add(_escolhaDeRaca);
            painel.Add(rodape);
            raiz.Add(painel);
        }

        protected override void AoAbrir()
        {
            // Dá o foco depois do primeiro layout, quando o campo já pode receber foco.
            _nome.schedule.Execute(() => _nome.Focus());
        }

        protected override void AoFechar()
        {
            _imagem.image = null;
            _previsao?.Dispose();
            _previsao = null;
        }

        /// <summary>
        /// Uma opção por valor do enum <see cref="Raca"/> (na ordem do enum), com o cartão da
        /// raça escolhida ao lado. A primeira já vem escolhida.
        /// </summary>
        private VisualElement MontarRacas()
        {
            foreach (Raca raca in Enum.GetValues(typeof(Raca)))
            {
                _racas.Add(raca);
            }

            var linha = new VisualElement();
            linha.AddToClassList(EstiloPersonagem.EscolhaDeRaca);
            linha.AddToClassList("personagem-criacao-corpo");

            var coluna = new VisualElement();
            coluna.AddToClassList(EstiloPersonagem.ListaDeRacas);
            coluna.AddToClassList("personagem-criacao-escolhas");
            coluna.Add(_nome);
            coluna.Add(_erroNome);
            var tituloRacas = new Label("Escolha sua raça");
            tituloRacas.AddToClassList(EstiloCore.Subtitulo);
            coluna.Add(tituloRacas);
            for (var i = 0; i < _racas.Count; i++)
            {
                var indice = i;
                var botao = new Button(() => EscolherRaca(indice))
                {
                    text = TextosPersonagem.NomeDaRaca(_racas[i]),
                };
                botao.AddToClassList(EstiloPersonagem.Item);
                botao.AddToClassList(EstiloPersonagem.RacaBotao);
                _botoesRaca.Add(botao);
                coluna.Add(botao);
            }

            var centro = new VisualElement();
            centro.AddToClassList("personagem-criacao-centro");
            _imagem = new Image { scaleMode = ScaleMode.ScaleToFit };
            _imagem.AddToClassList("personagem-selecao-imagem");
            PreviaVisualPersonagem.LigarArrasto(_imagem, () => _previsao);
            centro.Add(PreviaVisualPersonagem.CriarPalco(_imagem));
            _avisoPrevia = new Label();
            _avisoPrevia.AddToClassList(EstiloCore.Mensagem);
            centro.Add(_avisoPrevia);
            centro.Add(MontarSecaoAparencia());

            _cartaoDaRaca = new VisualElement();
            _cartaoDaRaca.AddToClassList(EstiloPersonagem.CartaoDaRaca);
            _cartaoDaRaca.AddToClassList("personagem-criacao-informacoes");
            _racaNome = NovoRotulo(_cartaoDaRaca, EstiloPersonagem.ItemNome);
            _racaPovo = NovoRotulo(_cartaoDaRaca, EstiloPersonagem.ItemDetalhes);
            _racaAparencia = NovoRotulo(_cartaoDaRaca, EstiloPersonagem.CartaoTexto);
            _racaTraco = NovoRotulo(_cartaoDaRaca, EstiloPersonagem.CartaoTexto);
            _racaSkill = NovoRotulo(_cartaoDaRaca, EstiloPersonagem.CartaoTexto);
            _racaTexto = NovoRotulo(_cartaoDaRaca, EstiloPersonagem.CartaoCitacao);

            linha.Add(coluna);
            linha.Add(centro);
            linha.Add(_cartaoDaRaca);
            EscolherRaca(0);
            return linha;
        }

        private VisualElement MontarSecaoAparencia()
        {
            var secao = new VisualElement();
            secao.AddToClassList("personagem-criacao-aparencia");

            var titulo = new Label("Personalização");
            titulo.AddToClassList(EstiloCore.Subtitulo);
            secao.Add(titulo);

            secao.Add(
                CriarLinhaAparencia(
                    "Tom de pele",
                    () => TextosPersonagem.NomeTomDePele(_aparencia.TomDePele),
                    () => MudarTom(-1),
                    () => MudarTom(1),
                    out _rotuloTom
                )
            );

            secao.Add(
                CriarLinhaAparencia(
                    "Cabelo",
                    () => TextosPersonagem.NomeCabelo(_aparencia.Cabelo),
                    () => MudarCabelo(-1),
                    () => MudarCabelo(1),
                    out _rotuloCabelo
                )
            );

            secao.Add(
                CriarLinhaAparencia(
                    "Cor do cabelo",
                    () => TextosPersonagem.NomeCorDeCabelo(_aparencia.CorDeCabelo),
                    () => MudarCor(-1),
                    () => MudarCor(1),
                    out _rotuloCor
                )
            );

            return secao;
        }

        private static VisualElement CriarLinhaAparencia(
            string nome,
            Func<string> obterTexto,
            Action aoVoltar,
            Action aoAvancar,
            out Label rotuloValor
        )
        {
            var item = new VisualElement();
            item.AddToClassList("personagem-stepper-item");

            var titulo = new Label(nome);
            titulo.AddToClassList(EstiloCore.Texto);
            item.Add(titulo);

            var linha = new VisualElement();
            linha.AddToClassList("personagem-stepper-linha");

            var anterior = new Button(aoVoltar) { text = "◀" };
            anterior.AddToClassList(EstiloCore.Botao);
            anterior.AddToClassList("personagem-stepper-botao");

            rotuloValor = new Label(obterTexto());
            rotuloValor.AddToClassList("personagem-stepper-valor");

            var proximo = new Button(aoAvancar) { text = "▶" };
            proximo.AddToClassList(EstiloCore.Botao);
            proximo.AddToClassList("personagem-stepper-botao");

            linha.Add(anterior);
            linha.Add(rotuloValor);
            linha.Add(proximo);
            item.Add(linha);
            return item;
        }

        private void MudarTom(int delta)
        {
            if (_esperando)
            {
                return;
            }

            _aparencia.TomDePele =
                (_aparencia.TomDePele + delta + RegrasPersonagem.TotalTonsDePele)
                % RegrasPersonagem.TotalTonsDePele;
            _rotuloTom.text = TextosPersonagem.NomeTomDePele(_aparencia.TomDePele);
            AtualizarPrevia();
        }

        private void MudarCabelo(int delta)
        {
            if (_esperando)
            {
                return;
            }

            _aparencia.Cabelo =
                (_aparencia.Cabelo + delta + RegrasPersonagem.TotalCabelos)
                % RegrasPersonagem.TotalCabelos;
            _rotuloCabelo.text = TextosPersonagem.NomeCabelo(_aparencia.Cabelo);
            AtualizarPrevia();
        }

        private void MudarCor(int delta)
        {
            if (_esperando)
            {
                return;
            }

            _aparencia.CorDeCabelo =
                (_aparencia.CorDeCabelo + delta + RegrasPersonagem.TotalCoresDeCabelo)
                % RegrasPersonagem.TotalCoresDeCabelo;
            _rotuloCor.text = TextosPersonagem.NomeCorDeCabelo(_aparencia.CorDeCabelo);
            AtualizarPrevia();
        }

        private void AtualizarPrevia()
        {
            if (_indiceRaca >= 0 && _indiceRaca < _racas.Count)
            {
                _previsao ??= new PreviaVisualPersonagem();
                var disponivel = _previsao.Mostrar(_racas[_indiceRaca], _aparencia);
                _imagem.image = disponivel ? _previsao.Textura : null;
                _avisoPrevia.text = disponivel ? "" : "Visual indisponível neste momento";
                EstiloPersonagem.Mostrar(_avisoPrevia, !disponivel);
            }
        }

        private static Label NovoRotulo(VisualElement pai, string classe)
        {
            var rotulo = new Label();
            rotulo.AddToClassList(classe);
            pai.Add(rotulo);
            return rotulo;
        }

        private void EscolherRaca(int indice)
        {
            if (_esperando || indice < 0 || indice >= _racas.Count)
            {
                return;
            }

            _indiceRaca = indice;
            for (var i = 0; i < _botoesRaca.Count; i++)
            {
                if (i == indice)
                {
                    _botoesRaca[i].AddToClassList(EstiloPersonagem.ItemSelecionado);
                }
                else
                {
                    _botoesRaca[i].RemoveFromClassList(EstiloPersonagem.ItemSelecionado);
                }
            }

            var dados = DadosDasRacas.Procurar(_racas[indice]);
            _racaNome.text = TextosPersonagem.NomeDaRaca(_racas[indice]);
            _racaPovo.text = dados?.Povo ?? "";
            _racaAparencia.text = dados?.Aparencia ?? "";
            _racaTraco.text = dados?.Traco ?? "";
            _racaSkill.text =
                dados == null ? "" : "Habilidade racial em desenvolvimento: " + dados.SkillRacial;
            _racaTexto.text = dados != null ? "“" + dados.Texto + "”" : "";

            AtualizarPrevia();
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
                _nome.RemoveFromClassList("campo-erro");
                EstiloPersonagem.Mostrar(_erroNome, false);
                MostrarMensagem(null);
            }
            else
            {
                _nome.AddToClassList("campo-erro");
                _erroNome.text = TextosPersonagem.NomeInvalido;
                EstiloPersonagem.Mostrar(_erroNome, true);
                MostrarMensagem(null);
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
                _nome.AddToClassList("campo-erro");
                _erroNome.text = TextosPersonagem.NomeInvalido;
                EstiloPersonagem.Mostrar(_erroNome, true);
                return;
            }

            _nome.RemoveFromClassList("campo-erro");
            EstiloPersonagem.Mostrar(_erroNome, false);

            var indice = _indiceRaca;
            if (indice < 0 || indice >= _racas.Count)
            {
                MostrarMensagem("Escolha uma raça.", EstiloCore.MensagemErro);
                return;
            }

            var pedido = new CriarPersonagemRequest
            {
                Nome = nome,
                Raca = _racas[indice],
                Aparencia = _aparencia,
            };

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

            if (
                codigo == CodigosErroPersonagem.NomeInvalido
                || codigo == CodigosErroPersonagem.NomeJaExiste
            )
            {
                _nome.AddToClassList("campo-erro");
                _erroNome.text = TextosPersonagem.MensagemDaRecusa(codigo);
                EstiloPersonagem.Mostrar(_erroNome, true);
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
            _escolhaDeRaca.SetEnabled(!esperando);
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
