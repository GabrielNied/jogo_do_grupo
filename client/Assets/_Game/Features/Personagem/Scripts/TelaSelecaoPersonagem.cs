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
    /// Seleção de personagem (aberta depois do login): lista os personagens da conta,
    /// deixa escolher um e leva para a criação. Quem decide a lista é o servidor.
    /// </summary>
    internal sealed class TelaSelecaoPersonagem : Tela
    {
        private readonly List<PersonagemResumo> _personagens = new List<PersonagemResumo>();
        private readonly Dictionary<long, Button> _itens = new Dictionary<long, Button>();

        private Label _subtitulo;
        private Label _carregando;
        private VisualElement _erro;
        private VisualElement _lista;
        private Label _vazio;
        private Button _criar;
        private Button _entrar;
        private Label _avisoLimite;
        private Label _mensagem;

        private long? _idSelecionado;
        private bool _carregandoLista;

        protected override void Montar(VisualElement raiz)
        {
            EstiloPersonagem.Aplicar(raiz);

            var painel = new VisualElement();
            painel.AddToClassList(EstiloCore.Painel);
            painel.AddToClassList(EstiloPersonagem.PainelSelecao);

            var titulo = new Label("Personagens");
            titulo.AddToClassList(EstiloCore.Titulo);

            _subtitulo = new Label();
            _subtitulo.AddToClassList(EstiloCore.Subtitulo);

            _carregando = new Label("Carregando personagens...");
            _carregando.AddToClassList(EstiloCore.Mensagem);

            _erro = MontarErro();

            _lista = new VisualElement();
            _lista.AddToClassList(EstiloPersonagem.Lista);

            _vazio = new Label("Você ainda não tem personagens. Crie o primeiro!");
            _vazio.AddToClassList(EstiloCore.Texto);

            _criar = new Button(AoClicarCriar) { text = "Criar" };
            _criar.AddToClassList(EstiloCore.Botao);

            _entrar = new Button(AoClicarEntrar) { text = "Entrar" };
            _entrar.AddToClassList(EstiloCore.Botao);
            _entrar.AddToClassList(EstiloCore.BotaoPrincipal);

            var sair = new Button(AoClicarSair) { text = "Sair" };
            sair.AddToClassList(EstiloCore.Botao);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(_criar);
            linha.Add(_entrar);
            linha.Add(sair);

            // Aviso fixo enquanto a conta estiver cheia; separado de _mensagem para não sumir
            // quando outra mensagem (ex.: Entrar) é mostrada ou limpa.
            _avisoLimite = new Label(TextosPersonagem.LimiteAtingido);
            _avisoLimite.AddToClassList(EstiloCore.Mensagem);
            EstiloPersonagem.Mostrar(_avisoLimite, false);

            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Mensagem);

            painel.Add(titulo);
            painel.Add(_subtitulo);
            painel.Add(_carregando);
            painel.Add(_erro);
            painel.Add(_lista);
            painel.Add(_vazio);
            painel.Add(linha);
            painel.Add(_avisoLimite);
            painel.Add(_mensagem);
            raiz.Add(painel);
        }

        protected override void AoAbrir()
        {
            Carregar();
        }

        private VisualElement MontarErro()
        {
            var erro = new VisualElement();

            var texto = new Label(TextosPersonagem.ErroAoCarregar);
            texto.AddToClassList(EstiloCore.Mensagem);
            texto.AddToClassList(EstiloCore.MensagemErro);

            var tentar = new Button(Carregar) { text = "Tentar de novo" };
            tentar.AddToClassList(EstiloCore.Botao);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(tentar);

            erro.Add(texto);
            erro.Add(linha);
            return erro;
        }

        /// <summary>Pede a lista ao servidor (<c>GET</c>) e mostra o resultado.</summary>
        private async void Carregar()
        {
            if (_carregandoLista)
            {
                return;
            }

            _carregandoLista = true;
            MostrarCarregando();
            try
            {
                var resposta = await ClienteApi.Get<ListaPersonagensResponse>(
                    RotasPersonagem.Personagens
                );

                // 401 já encerrou a sessão e levou para o login: esta tela fechou.
                if (!Aberta)
                {
                    return;
                }

                if (!resposta.Sucesso)
                {
                    if (TextosPersonagem.FoiCancelada(resposta.CodigoErro))
                    {
                        return;
                    }

                    // Sem conexão, HTTP bloqueado ou erro do servidor: a mesma mensagem.
                    MostrarErro();
                    return;
                }

                MostrarLista(resposta.Corpo.Personagens);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarErro();
                }
            }
            finally
            {
                _carregandoLista = false;
            }
        }

        private void MostrarCarregando()
        {
            _subtitulo.text = "";
            EstiloPersonagem.Mostrar(_carregando, true);
            EstiloPersonagem.Mostrar(_erro, false);
            EstiloPersonagem.Mostrar(_lista, false);
            EstiloPersonagem.Mostrar(_vazio, false);
            EstiloPersonagem.Mostrar(_criar, false);
            EstiloPersonagem.Mostrar(_entrar, false);
            EstiloPersonagem.Mostrar(_avisoLimite, false);
            MostrarMensagem(null);
        }

        private void MostrarErro()
        {
            _subtitulo.text = "";
            EstiloPersonagem.Mostrar(_carregando, false);
            EstiloPersonagem.Mostrar(_erro, true);
            EstiloPersonagem.Mostrar(_lista, false);
            EstiloPersonagem.Mostrar(_vazio, false);
            EstiloPersonagem.Mostrar(_criar, false);
            EstiloPersonagem.Mostrar(_entrar, false);
            EstiloPersonagem.Mostrar(_avisoLimite, false);
            MostrarMensagem(null);
        }

        private void MostrarLista(PersonagemResumo[] recebidos)
        {
            _personagens.Clear();
            if (recebidos != null)
            {
                foreach (var personagem in recebidos)
                {
                    if (personagem != null)
                    {
                        _personagens.Add(personagem);
                    }
                }
            }

            // A ordem é a do servidor (ordem de criação).
            _lista.Clear();
            _itens.Clear();
            foreach (var personagem in _personagens)
            {
                var item = CriarItem(personagem);
                _itens[personagem.Id] = item;
                _lista.Add(item);
            }

            var vazia = _personagens.Count == 0;
            _subtitulo.text = vazia ? "" : "Escolha um personagem";
            EstiloPersonagem.Mostrar(_carregando, false);
            EstiloPersonagem.Mostrar(_erro, false);
            EstiloPersonagem.Mostrar(_lista, !vazia);
            EstiloPersonagem.Mostrar(_vazio, vazia);
            EstiloPersonagem.Mostrar(_criar, true);
            EstiloPersonagem.Mostrar(_entrar, !vazia);

            // O servidor garante o limite; aqui só evita um clique que seria recusado.
            var cheia = _personagens.Count >= RegrasPersonagem.MaximoPorConta;
            _criar.SetEnabled(!cheia);
            EstiloPersonagem.Mostrar(_avisoLimite, cheia);
            MostrarMensagem(null);

            Selecionar(EscolherSelecaoInicial());
        }

        /// <summary>O recém-criado (se veio da criação) ou o primeiro da lista.</summary>
        private long? EscolherSelecaoInicial()
        {
            var pedido = TelasPersonagem.IdParaSelecionar;
            TelasPersonagem.IdParaSelecionar = null;

            if (pedido.HasValue && _itens.ContainsKey(pedido.Value))
            {
                return pedido.Value;
            }

            return _personagens.Count > 0 ? _personagens[0].Id : (long?)null;
        }

        private Button CriarItem(PersonagemResumo personagem)
        {
            var id = personagem.Id;
            var item = new Button(() => AoClicarItem(id));
            item.AddToClassList(EstiloPersonagem.Item);

            var nome = new Label(personagem.Nome);
            nome.AddToClassList(EstiloPersonagem.ItemNome);

            var detalhes = new Label(
                $"{TextosPersonagem.NomeDaRaca(personagem.Raca)} · Nível {personagem.Nivel}"
            );
            detalhes.AddToClassList(EstiloPersonagem.ItemDetalhes);

            item.Add(nome);
            item.Add(detalhes);
            return item;
        }

        private void AoClicarItem(long id)
        {
            if (_idSelecionado != id)
            {
                MostrarMensagem(null);
            }

            Selecionar(id);
        }

        private void Selecionar(long? id)
        {
            _idSelecionado = id;
            foreach (var par in _itens)
            {
                par.Value.EnableInClassList(
                    EstiloPersonagem.ItemSelecionado,
                    id.HasValue && par.Key == id.Value
                );
            }

            _entrar.SetEnabled(id.HasValue);
        }

        private void AoClicarCriar()
        {
            Navegacao.Abrir(TelasPersonagem.Criacao);
        }

        private void AoClicarEntrar()
        {
            if (!_idSelecionado.HasValue)
            {
                return;
            }

            // Ainda não existe mundo: avisa e continua na seleção, sem chamar o servidor.
            MostrarMensagem(TextosPersonagem.JogoNaoImplementado, EstiloCore.MensagemAviso);
        }

        private void AoClicarSair()
        {
            Navegacao.VoltarParaLogin();
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
