using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Contas
{
    /// <summary>
    /// Aba "Conta" das Opções do menu do Esc: troca de senha logado (#183). Com sucesso, a
    /// <see cref="Sessao"/> passa para o token novo; os tokens antigos (outros PCs) deixam de valer.
    /// </summary>
    internal sealed class PainelConta
    {
        private TextField _senhaAtual;
        private TextField _novaSenha;
        private TextField _confirmacao;
        private Button _trocar;
        private Label _mensagemSenha;
        private bool _enviando;
        private VisualElement _raiz;

        /// <summary>Monta a aba dentro de <paramref name="conteudo"/>.</summary>
        public void Montar(VisualElement conteudo)
        {
            _raiz = conteudo;
            var subtitulo = new Label("Trocar senha");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            conteudo.Add(subtitulo);

            var aviso = new Label("Trocar a senha encerra a conta nos outros computadores.");
            aviso.AddToClassList(EstiloCore.Texto);
            conteudo.Add(aviso);

            _senhaAtual = CriarCampoSenha(conteudo, "Senha atual");
            _novaSenha = CriarCampoSenha(conteudo, "Nova senha");
            _confirmacao = CriarCampoSenha(conteudo, "Repita a nova senha");

            _mensagemSenha = new Label();
            _mensagemSenha.AddToClassList(EstiloCore.Mensagem);
            conteudo.Add(_mensagemSenha);

            _trocar = new Button(AoClicarTrocar) { text = "Trocar senha" };
            _trocar.AddToClassList(EstiloCore.Botao);
            _trocar.AddToClassList(EstiloCore.BotaoPrincipal);
            conteudo.Add(_trocar);
        }

        internal static TextField CriarCampoSenha(VisualElement conteudo, string rotulo)
        {
            var campo = new TextField(rotulo)
            {
                maxLength = RegrasConta.SenhaMaximo,
                isPasswordField = true,
            };
            campo.AddToClassList(EstiloCore.Campo);
            conteudo.Add(campo);
            return campo;
        }

        private bool Aberto => _raiz != null && _raiz.panel != null;

        private async void AoClicarTrocar()
        {
            if (_enviando)
            {
                return;
            }

            var atual = _senhaAtual.value;
            var nova = _novaSenha.value;
            var formato = MensagemDeFormatoTroca(atual, nova, _confirmacao.value);
            if (formato != null)
            {
                Mostrar(_mensagemSenha, formato, EstiloCore.MensagemErro);
                return;
            }

            _enviando = true;
            _trocar.SetEnabled(false);
            Mostrar(_mensagemSenha, "Trocando a senha...");
            try
            {
                var resposta = await ClienteApi.Post<LoginResponse>(
                    RotasContas.TrocarSenha,
                    new TrocarSenhaRequest { SenhaAtual = atual, NovaSenha = nova }
                );
                if (!Aberto || resposta.CodigoErro == CodigosErroCliente.Cancelada)
                {
                    return;
                }

                if (!resposta.Sucesso || string.IsNullOrEmpty(resposta.Corpo?.Token))
                {
                    Mostrar(
                        _mensagemSenha,
                        MensagemDoErro(resposta.CodigoErro),
                        EstiloCore.MensagemErro
                    );
                    return;
                }

                Sessao.Iniciar(resposta.Corpo);
                _senhaAtual.value = "";
                _novaSenha.value = "";
                _confirmacao.value = "";
                Mostrar(_mensagemSenha, "Senha trocada.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberto)
                {
                    Mostrar(_mensagemSenha, MensagemDoErro(null), EstiloCore.MensagemErro);
                }
            }
            finally
            {
                _enviando = false;
                if (Aberto)
                {
                    _trocar.SetEnabled(true);
                }
            }
        }

        /// <summary>Confere os campos antes de chamar o servidor; <c>null</c> se estão certos.</summary>
        internal static string MensagemDeFormatoTroca(string atual, string nova, string confirmacao)
        {
            if (string.IsNullOrEmpty(atual) || string.IsNullOrEmpty(nova))
            {
                return "Preencha a senha atual e a nova.";
            }

            if (!string.Equals(nova, confirmacao, StringComparison.Ordinal))
            {
                return "A nova senha e a repetição não conferem.";
            }

            var codigo = RegrasConta.ValidarSenha(nova);
            return codigo == null ? null : MensagemDoErro(codigo);
        }

        internal static string MensagemDoErro(string codigo)
        {
            return codigo switch
            {
                CodigosErro.SenhaAtualIncorreta => "A senha atual está incorreta.",
                CodigosErro.MuitasTentativas =>
                    "Muitas tentativas. Aguarde alguns minutos e tente de novo.",
                _ => TelaLogin.MensagemDoErro(codigo),
            };
        }

        internal static void Mostrar(Label rotulo, string texto, string classe = null)
        {
            rotulo.text = texto ?? "";
            rotulo.RemoveFromClassList(EstiloCore.MensagemErro);
            rotulo.RemoveFromClassList(EstiloCore.MensagemAviso);
            if (classe != null)
            {
                rotulo.AddToClassList(classe);
            }
        }
    }
}
