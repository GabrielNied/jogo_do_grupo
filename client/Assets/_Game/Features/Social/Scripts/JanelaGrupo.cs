using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Janela Social (tecla O), com as abas de grupo e amigos. Só envia intenções e mostra o que
    /// o servidor mandou. A moldura, o atalho, o Esc e o cursor são do Core.
    /// </summary>
    internal sealed class JanelaGrupo : JanelaDoJogo
    {
        public const string Chave = "social.grupo";
        public const string Titulo = "Social";
        public const string Tecla = "O";
        public const int Ordem = 50;

        private PainelGrupo _painelGrupo;
        private PainelAmigos _painelAmigos;
        private VisualElement _conteudoGrupo;
        private VisualElement _conteudoAmigos;
        private bool _amigosAtivos;

        public override bool MolduraSocial => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaGrupo());
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloSocial.Aplicar(conteudo);
            var abas = new VisualElement();
            abas.AddToClassList(EstiloCore.LinhaBotoes);
            var grupo = new Button(() => SelecionarAba(false)) { text = "Grupo" };
            grupo.AddToClassList(EstiloCore.Botao);
            var amigos = new Button(() => SelecionarAba(true)) { text = "Amigos" };
            amigos.AddToClassList(EstiloCore.Botao);
            abas.Add(grupo);
            abas.Add(amigos);
            conteudo.Add(abas);

            _conteudoGrupo = new VisualElement();
            _conteudoAmigos = new VisualElement();
            var rolagemDeAmigos = new ScrollView(ScrollViewMode.Vertical);
            rolagemDeAmigos.style.flexGrow = 1f;
            _conteudoAmigos.style.flexGrow = 1f;
            conteudo.Add(_conteudoGrupo);
            rolagemDeAmigos.Add(_conteudoAmigos);
            conteudo.Add(rolagemDeAmigos);
            _painelGrupo = new PainelGrupo(
                _conteudoGrupo,
                GrupoDoClient.Estado,
                titulo => DefinirTitulo(_amigosAtivos ? "Amigos" : titulo)
            );
            _painelAmigos = new PainelAmigos(_conteudoAmigos, EstadoDeAmigos.DoClient);
            SelecionarAba(false);
        }

        protected override void AoFechar()
        {
            _painelGrupo?.Dispose();
            _painelGrupo = null;
            _painelAmigos?.Dispose();
            _painelAmigos = null;
        }

        private void SelecionarAba(bool amigos)
        {
            _amigosAtivos = amigos;
            EstiloSocial.Mostrar(_conteudoGrupo, !amigos);
            EstiloSocial.Mostrar(_conteudoAmigos, amigos);
            DefinirTitulo(amigos ? "Amigos" : _painelGrupo?.TituloAtual ?? Titulo);
        }
    }

    internal sealed class PainelAmigos : System.IDisposable
    {
        private const string TituloAmigos = "Amigos";
        private const string TituloPedidosRecebidos = "Pedidos recebidos";
        private const string TituloPedidosEnviados = "Pedidos enviados";
        private const string TituloIgnorados = "Ignorados";

        private readonly EstadoDeAmigos _estado;
        private readonly VisualElement _amigos;
        private readonly VisualElement _recebidos;
        private readonly VisualElement _enviados;
        private readonly VisualElement _ignorados;
        private readonly TextField _nome;

        public PainelAmigos(VisualElement conteudo, EstadoDeAmigos estado)
        {
            _estado = estado;
            EstiloSocial.Aplicar(conteudo);

            var pedir = new VisualElement();
            pedir.AddToClassList(EstiloSocial.Convidar);
            _nome = new TextField("Adicionar amigo");
            _nome.AddToClassList(EstiloCore.Campo);
            _nome.RegisterCallback<KeyDownEvent>(AoApertarTecla);
            var botaoPedir = NovoBotao("Enviar pedido", EnviarPedido);
            botaoPedir.AddToClassList(EstiloCore.BotaoPrincipal);
            pedir.Add(_nome);
            pedir.Add(botaoPedir);
            conteudo.Add(pedir);

            _amigos = NovaSecao(conteudo, TituloAmigos);
            _recebidos = NovaSecao(conteudo, TituloPedidosRecebidos);
            _enviados = NovaSecao(conteudo, TituloPedidosEnviados);
            _ignorados = NovaSecao(conteudo, TituloIgnorados);

            _estado.Mudou += Atualizar;
            Atualizar();
        }

        public void Dispose()
        {
            _estado.Mudou -= Atualizar;
        }

        private void Atualizar()
        {
            _amigos.Clear();
            _recebidos.Clear();
            _enviados.Clear();
            _ignorados.Clear();

            AdicionarTitulo(_amigos, TituloAmigos);
            if (_estado.Amigos.Count == 0)
            {
                _amigos.Add(new Label("Você ainda não tem amigos."));
            }

            foreach (var amigo in _estado.Amigos)
            {
                var estado = amigo.Online
                    ? string.IsNullOrEmpty(amigo.PersonagemOnline)
                        ? "Online"
                        : "Online — " + amigo.PersonagemOnline
                    : "Offline";
                if (amigo.Online && !string.IsNullOrEmpty(amigo.Area))
                {
                    estado += " · " + amigo.Area;
                }
                var alvo =
                    amigo.Online && !string.IsNullOrEmpty(amigo.PersonagemOnline)
                        ? amigo.PersonagemOnline
                        : amigo.Nome;
                var linha = NovaLinha(amigo.Nome + " (" + estado + ")");
                var acoes = linha.Q(className: EstiloSocial.Acoes);
                acoes.Add(NovoBotao("Sussurrar", () => Sussurrar(alvo)));
                var convidar = NovoBotao(
                    "Convidar para grupo",
                    () => GrupoDoClient.Estado.Convidar(alvo)
                );
                convidar.SetEnabled(amigo.Online && GrupoDoClient.Estado.PodeConvidar);
                acoes.Add(convidar);
                acoes.Add(NovoBotao("Remover", () => _estado.RemoverAmigo(amigo.AmigoId)));
                _amigos.Add(linha);
            }

            AdicionarTitulo(_recebidos, TituloPedidosRecebidos);
            if (_estado.PedidosRecebidos.Count == 0)
            {
                _recebidos.Add(new Label("Nenhum pedido aguardando resposta."));
            }

            foreach (var pedido in _estado.PedidosRecebidos)
            {
                var id = pedido.PedidoId;
                var linha = NovaLinha(pedido.Nome);
                var acoes = linha.Q(className: EstiloSocial.Acoes);
                acoes.Add(NovoBotao("Aceitar", () => _estado.ResponderPedido(id, true)));
                acoes.Add(NovoBotao("Recusar", () => _estado.ResponderPedido(id, false)));
                _recebidos.Add(linha);
            }

            AdicionarTitulo(_enviados, TituloPedidosEnviados);
            if (_estado.PedidosEnviados.Count == 0)
            {
                _enviados.Add(new Label("Nenhum pedido enviado aguardando resposta."));
            }

            foreach (var pedido in _estado.PedidosEnviados)
            {
                var id = pedido.PedidoId;
                var linha = NovaLinha(pedido.Nome);
                linha
                    .Q(className: EstiloSocial.Acoes)
                    .Add(NovoBotao("Cancelar", () => _estado.CancelarPedido(id)));
                _enviados.Add(linha);
            }

            AdicionarTitulo(_ignorados, TituloIgnorados);
            if (_estado.Ignorados.Count == 0)
            {
                _ignorados.Add(new Label("Nenhum jogador ignorado."));
            }

            foreach (var ignorado in _estado.Ignorados)
            {
                var id = ignorado.IgnoradoId;
                var linha = NovaLinha(ignorado.Nome);
                linha
                    .Q(className: EstiloSocial.Acoes)
                    .Add(NovoBotao("Designorar", () => _estado.DeixarDeIgnorar(id)));
                _ignorados.Add(linha);
            }
        }

        private void EnviarPedido()
        {
            if (_estado.PedirAmizade(_nome.value))
            {
                _nome.value = "";
            }
        }

        private static void Sussurrar(string nome)
        {
            JanelasDoJogo.Fechar();
            ChatDoJogo.PedirAbrir("/s " + nome + " ");
        }

        private void AoApertarTecla(KeyDownEvent evento)
        {
            if (evento.keyCode == KeyCode.Return || evento.keyCode == KeyCode.KeypadEnter)
            {
                EnviarPedido();
                evento.StopPropagation();
            }
        }

        private static VisualElement NovaSecao(VisualElement conteudo, string titulo)
        {
            var secao = new VisualElement();
            secao.AddToClassList(EstiloSocial.Secao);
            AdicionarTitulo(secao, titulo);
            conteudo.Add(secao);
            return secao;
        }

        private static void AdicionarTitulo(VisualElement secao, string titulo)
        {
            var label = new Label(titulo);
            label.AddToClassList(EstiloCore.Subtitulo);
            secao.Add(label);
        }

        private static VisualElement NovaLinha(string nome)
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaLista);
            linha.AddToClassList(EstiloSocial.Linha);
            var label = new Label(nome);
            label.AddToClassList(EstiloCore.Texto);
            label.AddToClassList(EstiloSocial.Nome);
            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloSocial.Acoes);
            linha.Add(label);
            linha.Add(acoes);
            return linha;
        }

        private static Button NovoBotao(string texto, System.Action acao)
        {
            var botao = new Button(acao) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList(EstiloSocial.Botao);
            return botao;
        }
    }
}
