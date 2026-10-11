using System;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O conteúdo da janela do grupo, em UI Toolkit montado em código: convidar por nome, os
    /// convites recebidos (aceitar ou recusar) e a lista do grupo (sair, expulsar, passar a
    /// liderança). Só mostra o <see cref="EstadoDoGrupo"/> e envia as intenções; a moldura e o
    /// botão de fechar são do Core (<c>JanelasDoJogo</c>).
    /// </summary>
    internal sealed class PainelGrupo : IDisposable
    {
        public const string TituloBase = "Grupo";
        public const string TextoConvidar = "Convidar";
        public const string RotuloDoNome = "Convidar pelo nome";
        public const string TextoAceitar = "Aceitar";
        public const string TextoRecusar = "Recusar";
        public const string TextoExpulsar = "Expulsar";
        public const string TextoPassarLideranca = "Passar liderança";
        public const string TextoSair = "Sair do grupo";
        public const string TituloConvites = "Convites";
        public const string TituloMembros = "Membros";

        private readonly EstadoDoGrupo _estado;
        private readonly Action<string> _definirTitulo;
        private readonly Label _aviso;
        private readonly VisualElement _convidar;
        private readonly TextField _nome;
        private readonly Button _botaoConvidar;
        private readonly Label _naoPodeConvidar;
        private readonly VisualElement _convites;
        private readonly VisualElement _membros;
        private readonly Button _botaoSair;

        /// <param name="conteudo">Área da janela que recebe os elementos.</param>
        /// <param name="estado">O grupo que a janela mostra.</param>
        /// <param name="definirTitulo">Troca o título da janela (ex.: "Grupo (3/5)").</param>
        public PainelGrupo(
            VisualElement conteudo,
            EstadoDoGrupo estado,
            Action<string> definirTitulo
        )
        {
            _estado = estado ?? throw new ArgumentNullException(nameof(estado));
            _definirTitulo = definirTitulo;

            EstiloSocial.Aplicar(conteudo);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);

            _nome = new TextField(RotuloDoNome);
            _nome.AddToClassList(EstiloCore.Campo);
            _nome.RegisterCallback<KeyDownEvent>(AoApertarTecla);

            _botaoConvidar = new Button(Convidar) { text = TextoConvidar };
            _botaoConvidar.AddToClassList(EstiloCore.Botao);
            _botaoConvidar.AddToClassList(EstiloCore.BotaoPrincipal);

            _convidar = new VisualElement();
            _convidar.AddToClassList(EstiloSocial.Convidar);
            _convidar.Add(_nome);
            _convidar.Add(_botaoConvidar);

            _naoPodeConvidar = new Label(TextosGrupo.NaoPodeConvidar);
            _naoPodeConvidar.AddToClassList(EstiloCore.Mensagem);

            _convites = new VisualElement();
            _convites.AddToClassList(EstiloSocial.Secao);

            _membros = new VisualElement();
            _membros.AddToClassList(EstiloSocial.Secao);

            _botaoSair = new Button(() => _estado.Sair()) { text = TextoSair };
            _botaoSair.AddToClassList(EstiloCore.Botao);
            _botaoSair.AddToClassList(EstiloSocial.Rodape);

            conteudo.Add(_aviso);
            conteudo.Add(_convidar);
            conteudo.Add(_naoPodeConvidar);
            conteudo.Add(_convites);
            conteudo.Add(_membros);
            conteudo.Add(_botaoSair);

            _estado.Mudou += Atualizar;
            Atualizar();
        }

        /// <summary>Tira a janela do estado (chamado ao fechar).</summary>
        public void Dispose()
        {
            _estado.Mudou -= Atualizar;
        }

        /// <summary>Redesenha tudo a partir do estado atual.</summary>
        public void Atualizar()
        {
            _definirTitulo?.Invoke(Titulo());
            AtualizarAviso();

            var podeConvidar = _estado.PodeConvidar;
            EstiloSocial.Mostrar(_convidar, podeConvidar);
            EstiloSocial.Mostrar(_naoPodeConvidar, !podeConvidar);

            AtualizarConvites();
            AtualizarMembros();
            EstiloSocial.Mostrar(_botaoSair, _estado.EmGrupo);
        }

        private string Titulo()
        {
            return _estado.EmGrupo
                ? TituloBase
                    + " ("
                    + _estado.Membros.Count
                    + "/"
                    + RegrasGrupo.MaximoDeMembros
                    + ")"
                : TituloBase;
        }

        private void AtualizarAviso()
        {
            var texto = _estado.MensagemRestante > 0f ? _estado.Mensagem : "";
            _aviso.text = texto;
            EstiloSocial.Mostrar(_aviso, !string.IsNullOrEmpty(texto));
            _aviso.EnableInClassList(EstiloCore.MensagemErro, _estado.MensagemEhErro);
        }

        private void AtualizarConvites()
        {
            _convites.Clear();
            EstiloSocial.Mostrar(_convites, _estado.Convites.Count > 0);
            if (_estado.Convites.Count == 0)
            {
                return;
            }

            var titulo = new Label(TituloConvites);
            titulo.AddToClassList(EstiloCore.Subtitulo);
            _convites.Add(titulo);

            foreach (var convite in _estado.Convites)
            {
                var id = convite.ConviteId;
                var linha = NovaLinha(ApresentacaoGrupo.TextoDoConvite(convite));

                var aceitar = NovoBotao(TextoAceitar, () => _estado.Responder(id, true));
                aceitar.AddToClassList(EstiloCore.BotaoPrincipal);
                var recusar = NovoBotao(TextoRecusar, () => _estado.Responder(id, false));

                var acoes = linha.Q(className: EstiloSocial.Acoes);
                acoes.Add(aceitar);
                acoes.Add(recusar);
                _convites.Add(linha);
            }
        }

        private void AtualizarMembros()
        {
            _membros.Clear();
            EstiloSocial.Mostrar(_membros, _estado.EmGrupo);
            if (!_estado.EmGrupo)
            {
                return;
            }

            var titulo = new Label(TituloMembros);
            titulo.AddToClassList(EstiloCore.Subtitulo);
            _membros.Add(titulo);

            foreach (var membro in ApresentacaoGrupo.Linhas(_estado))
            {
                var id = membro.PersonagemId;
                var linha = NovaLinha(membro.Texto);
                var nome = linha.Q<Label>(className: EstiloSocial.Nome);
                nome.EnableInClassList(EstiloSocial.Lider, membro.Lider);
                nome.EnableInClassList(EstiloSocial.Offline, !membro.Online);

                var acoes = linha.Q(className: EstiloSocial.Acoes);
                if (membro.PodePassarLideranca)
                {
                    acoes.Add(NovoBotao(TextoPassarLideranca, () => _estado.PassarLideranca(id)));
                }

                if (membro.PodeExpulsar)
                {
                    acoes.Add(NovoBotao(TextoExpulsar, () => _estado.Expulsar(id)));
                }

                _membros.Add(linha);
            }
        }

        private void Convidar()
        {
            if (_estado.Convidar(_nome.value))
            {
                _nome.value = "";
            }
        }

        private void AoApertarTecla(KeyDownEvent evento)
        {
            if (evento.keyCode == KeyCode.Return || evento.keyCode == KeyCode.KeypadEnter)
            {
                Convidar();
                evento.StopPropagation();
            }
        }

        private static VisualElement NovaLinha(string texto)
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaLista);
            linha.AddToClassList(EstiloSocial.Linha);

            var nome = new Label(texto);
            nome.AddToClassList(EstiloCore.Texto);
            nome.AddToClassList(EstiloSocial.Nome);

            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloSocial.Acoes);

            linha.Add(nome);
            linha.Add(acoes);
            return linha;
        }

        private static Button NovoBotao(string texto, Action aoClicar)
        {
            var botao = new Button(aoClicar) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList(EstiloSocial.Botao);
            return botao;
        }
    }
}
