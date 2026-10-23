using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.Contas;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Contas
{
    /// <summary>
    /// "Esqueci a senha" (#1167): pede um código ao e-mail de recuperação e redefine a senha com
    /// ele. Com sucesso, volta ao login; as sessões antigas da conta caem no servidor.
    /// </summary>
    internal sealed class TelaRecuperarSenha : Tela
    {
        private TextField _email;
        private Button _enviar;
        private VisualElement _etapaCodigo;
        private TextField _codigo;
        private TextField _novaSenha;
        private TextField _confirmacao;
        private Button _redefinir;
        private Label _mensagem;
        private bool _enviando;

        protected override void Montar(VisualElement raiz)
        {
            var composicao = new VisualElement();
            composicao.AddToClassList("login-composicao");

            var marca = ComponentesVisuais.Marca();
            marca.AddToClassList("login-marca");

            var painel = EstiloCore.CriarPainel(social: true);
            painel.AddToClassList("login-painel");

            var subtitulo = new Label("Recuperar a senha");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);

            var explicacao = new Label(
                "Digite o e-mail de recuperação da conta. Se ele estiver cadastrado, chega um "
                    + $"código de {RegrasConta.CodigoRecuperacaoDigitos} dígitos que vale "
                    + $"{RegrasConta.CodigoRecuperacaoMinutos} minutos."
            );
            explicacao.AddToClassList(EstiloCore.Texto);

            _email = new TextField("E-mail") { maxLength = RegrasConta.EmailMaximo };
            _email.AddToClassList(EstiloCore.Campo);

            _enviar = new Button(AoClicarEnviar) { text = "Enviar código" };
            _enviar.AddToClassList(EstiloCore.Botao);
            _enviar.AddToClassList(EstiloCore.BotaoPrincipal);

            _etapaCodigo = new VisualElement();
            _etapaCodigo.style.display = DisplayStyle.None;
            _codigo = new TextField("Código") { maxLength = RegrasConta.CodigoRecuperacaoDigitos };
            _codigo.AddToClassList(EstiloCore.Campo);
            _etapaCodigo.Add(_codigo);
            _novaSenha = PainelConta.CriarCampoSenha(_etapaCodigo, "Nova senha");
            _confirmacao = PainelConta.CriarCampoSenha(_etapaCodigo, "Repita a nova senha");
            _redefinir = new Button(AoClicarRedefinir) { text = "Redefinir senha" };
            _redefinir.AddToClassList(EstiloCore.Botao);
            _redefinir.AddToClassList(EstiloCore.BotaoPrincipal);
            _etapaCodigo.Add(_redefinir);

            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Mensagem);
            _mensagem.AddToClassList("login-mensagem");

            var voltar = new Button(() => Navegacao.Abrir(TelasCore.Login))
            {
                text = "Voltar ao login",
            };
            voltar.AddToClassList(EstiloCore.Botao);
            voltar.AddToClassList("login-esqueci");

            painel.Add(subtitulo);
            painel.Add(explicacao);
            painel.Add(_email);
            painel.Add(_enviar);
            painel.Add(_etapaCodigo);
            painel.Add(_mensagem);
            painel.Add(voltar);
            composicao.Add(marca);
            composicao.Add(painel);
            raiz.Add(composicao);
        }

        protected override void AoAbrir()
        {
            _email.schedule.Execute(() => _email.Focus());
        }

        private async void AoClicarEnviar()
        {
            var email = (_email.value ?? "").Trim();
            if (RegrasConta.ValidarEmail(email) is { } erro)
            {
                PainelConta.Mostrar(
                    _mensagem,
                    TelaLogin.MensagemDoErro(erro),
                    EstiloCore.MensagemErro
                );
                return;
            }

            if (!Iniciar())
            {
                return;
            }

            PainelConta.Mostrar(_mensagem, "Enviando...");
            try
            {
                var resposta = await ClienteApi.Post(
                    RotasContas.PedirCodigoRecuperacao,
                    new PedirCodigoRecuperacaoRequest { Email = email },
                    autenticar: false
                );
                if (!Aberta || resposta.CodigoErro == CodigosErroCliente.Cancelada)
                {
                    return;
                }

                if (!resposta.Sucesso)
                {
                    PainelConta.Mostrar(
                        _mensagem,
                        TelaLogin.MensagemDoErro(resposta.CodigoErro),
                        EstiloCore.MensagemErro
                    );
                    return;
                }

                // A resposta é a mesma exista ou não a conta: o texto não afirma que enviou.
                _etapaCodigo.style.display = DisplayStyle.Flex;
                _enviar.text = "Enviar outro código";
                PainelConta.Mostrar(
                    _mensagem,
                    "Se o e-mail estiver cadastrado, o código chega em instantes. "
                        + "Confira também a caixa de spam."
                );
                _codigo.schedule.Execute(() => _codigo.Focus());
            }
            catch (Exception e)
            {
                MostrarFalha(e);
            }
            finally
            {
                Encerrar();
            }
        }

        private async void AoClicarRedefinir()
        {
            var email = (_email.value ?? "").Trim();
            var codigo = (_codigo.value ?? "").Trim();
            var nova = _novaSenha.value;
            var formato = !RegrasConta.CodigoRecuperacaoNoFormato(codigo)
                ? $"O código tem {RegrasConta.CodigoRecuperacaoDigitos} dígitos."
                : PainelConta.MensagemDeSenhaNova(nova, _confirmacao.value);
            if (formato != null)
            {
                PainelConta.Mostrar(_mensagem, formato, EstiloCore.MensagemErro);
                return;
            }

            if (!Iniciar())
            {
                return;
            }

            PainelConta.Mostrar(_mensagem, "Redefinindo a senha...");
            try
            {
                var resposta = await ClienteApi.Post(
                    RotasContas.RedefinirSenha,
                    new RedefinirSenhaRequest
                    {
                        Email = email,
                        Codigo = codigo,
                        NovaSenha = nova,
                    },
                    autenticar: false
                );
                if (!Aberta || resposta.CodigoErro == CodigosErroCliente.Cancelada)
                {
                    return;
                }

                if (!resposta.Sucesso)
                {
                    PainelConta.Mostrar(
                        _mensagem,
                        TelaLogin.MensagemDoErro(resposta.CodigoErro),
                        EstiloCore.MensagemErro
                    );
                    return;
                }

                Navegacao.VoltarParaLogin("Senha redefinida. Entre com a nova senha.");
            }
            catch (Exception e)
            {
                MostrarFalha(e);
            }
            finally
            {
                Encerrar();
            }
        }

        private bool Iniciar()
        {
            if (_enviando)
            {
                return false;
            }

            _enviando = true;
            _enviar.SetEnabled(false);
            _redefinir.SetEnabled(false);
            return true;
        }

        private void Encerrar()
        {
            _enviando = false;
            if (Aberta)
            {
                _enviar.SetEnabled(true);
                _redefinir.SetEnabled(true);
            }
        }

        private void MostrarFalha(Exception e)
        {
            Debug.LogException(e);
            if (Aberta)
            {
                PainelConta.Mostrar(
                    _mensagem,
                    TelaLogin.MensagemDoErro(null),
                    EstiloCore.MensagemErro
                );
            }
        }
    }
}
