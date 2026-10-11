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
    /// Login e registro de conta. Com sucesso, guarda a <see cref="Sessao"/> e abre a tela
    /// definida em <see cref="Navegacao.DefinirTelaAposLogin"/>.
    /// </summary>
    internal sealed class TelaLogin : Tela
    {
        private const string MensagemGenerica = "Algo deu errado. Tente de novo.";
        private const string MensagemContaCriadaSemLogin =
            "Conta criada, mas não foi possível entrar. Use Entrar.";

        private TextField _usuario;
        private TextField _senha;
        private Button _entrar;
        private Button _criarConta;
        private Label _mensagem;
        private bool _esperando;

        protected override void Montar(VisualElement raiz)
        {
            var painel = EstiloCore.CriarPainel(social: true);

            var titulo = ComponentesVisuais.Marca();

            var subtitulo = new Label("Entre com a sua conta");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);

            _usuario = new TextField("Usuário") { maxLength = RegrasConta.UsuarioMaximo };
            _usuario.AddToClassList(EstiloCore.Campo);
            _usuario.RegisterCallback<KeyDownEvent>(AoTeclarNoUsuario, TrickleDown.TrickleDown);

            _senha = new TextField("Senha")
            {
                maxLength = RegrasConta.SenhaMaximo,
                isPasswordField = true,
            };
            _senha.AddToClassList(EstiloCore.Campo);
            _senha.RegisterCallback<KeyDownEvent>(AoTeclarNaSenha, TrickleDown.TrickleDown);

            _entrar = new Button(AoClicarEntrar) { text = "Entrar" };
            _entrar.AddToClassList(EstiloCore.Botao);
            _entrar.AddToClassList(EstiloCore.BotaoPrincipal);

            _criarConta = new Button(AoClicarCriarConta) { text = "Criar conta" };
            _criarConta.AddToClassList(EstiloCore.Botao);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(_entrar);
            linha.Add(_criarConta);

            _mensagem = new Label();
            _mensagem.AddToClassList(EstiloCore.Mensagem);

            painel.Add(titulo);
            painel.Add(subtitulo);
            painel.Add(_usuario);
            painel.Add(_senha);
            painel.Add(linha);
            painel.Add(_mensagem);
            raiz.Add(painel);
        }

        protected override void AoAbrir()
        {
            var aviso = Navegacao.ConsumirAvisoLogin();
            if (!string.IsNullOrEmpty(aviso))
            {
                MostrarMensagem(aviso, EstiloCore.MensagemAviso);
            }

            // Dá o foco depois do primeiro layout, quando o campo já pode receber foco.
            _usuario.schedule.Execute(() => _usuario.Focus());
        }

        private void AoTeclarNoUsuario(KeyDownEvent evt)
        {
            if (EhEnter(evt))
            {
                evt.StopPropagation();
                _senha.Focus();
            }
        }

        private void AoTeclarNaSenha(KeyDownEvent evt)
        {
            if (EhEnter(evt))
            {
                evt.StopPropagation();
                AoClicarEntrar();
            }
        }

        private static bool EhEnter(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter;
        }

        private async void AoClicarEntrar()
        {
            var usuario = _usuario.value;
            var senha = _senha.value;
            if (_esperando || !ValidarFormato(usuario, senha))
            {
                return;
            }

            DefinirEsperando(true);
            MostrarMensagem("Entrando...");
            try
            {
                await Entrar(usuario, senha, contaRecemCriada: false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarMensagem(MensagemGenerica, EstiloCore.MensagemErro);
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

        private async void AoClicarCriarConta()
        {
            var usuario = _usuario.value;
            var senha = _senha.value;
            if (_esperando || !ValidarFormato(usuario, senha))
            {
                return;
            }

            DefinirEsperando(true);
            MostrarMensagem("Criando a conta...");
            var contaCriada = false;
            try
            {
                var registro = await ClienteApi.Post(
                    RotasContas.Registrar,
                    new RegistrarContaRequest { Usuario = usuario, Senha = senha },
                    autenticar: false
                );
                // Cancelada: o jogo está fechando; não há o que mostrar.
                if (!Aberta || registro.CodigoErro == CodigosErroCliente.Cancelada)
                {
                    return;
                }

                if (!registro.Sucesso)
                {
                    MostrarMensagem(MensagemDoErro(registro.CodigoErro), EstiloCore.MensagemErro);
                    return;
                }

                contaCriada = true;
                MostrarMensagem("Conta criada. Entrando...");
                await Entrar(usuario, senha, contaRecemCriada: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarMensagem(
                        contaCriada ? MensagemContaCriadaSemLogin : MensagemGenerica,
                        EstiloCore.MensagemErro
                    );
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

        /// <summary>
        /// Faz o login; com sucesso, guarda a sessão e sai desta tela. Se falhar logo depois do
        /// registro (<paramref name="contaRecemCriada"/>), avisa que a conta foi criada e que é
        /// só usar Entrar.
        /// </summary>
        private async Awaitable Entrar(string usuario, string senha, bool contaRecemCriada)
        {
            var resposta = await ClienteApi.Post<LoginResponse>(
                RotasContas.Login,
                new LoginRequest { Usuario = usuario, Senha = senha },
                autenticar: false
            );
            // Cancelada: o jogo está fechando; não há o que mostrar.
            if (!Aberta || resposta.CodigoErro == CodigosErroCliente.Cancelada)
            {
                return;
            }

            if (!resposta.Sucesso || string.IsNullOrEmpty(resposta.Corpo.Token))
            {
                if (resposta.Sucesso)
                {
                    Debug.LogWarning("[TelaLogin] O login respondeu sem token.");
                }

                var mensagem =
                    contaRecemCriada ? MensagemContaCriadaSemLogin
                    : resposta.Sucesso ? MensagemGenerica
                    : MensagemDoErro(resposta.CodigoErro);
                MostrarMensagem(mensagem, EstiloCore.MensagemErro);
                return;
            }

            Sessao.Iniciar(resposta.Corpo);
            Navegacao.AbrirTelaAposLogin();
        }

        /// <summary>Confere o formato com <see cref="RegrasConta"/> antes de chamar o servidor.</summary>
        private bool ValidarFormato(string usuario, string senha)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
            {
                MostrarMensagem("Preencha o usuário e a senha.", EstiloCore.MensagemErro);
                return false;
            }

            var codigo = RegrasConta.ValidarUsuario(usuario) ?? RegrasConta.ValidarSenha(senha);
            if (codigo != null)
            {
                MostrarMensagem(MensagemDoErro(codigo), EstiloCore.MensagemErro);
                return false;
            }

            return true;
        }

        private static string MensagemDoErro(string codigo)
        {
            return codigo switch
            {
                CodigosErro.CredenciaisInvalidas => "Usuário ou senha incorretos.",
                CodigosErro.UsuarioJaExiste => "Esse usuário já existe. Escolha outro.",
                CodigosErro.UsuarioInvalido =>
                    $"O usuário deve ter de {RegrasConta.UsuarioMinimo} a {RegrasConta.UsuarioMaximo} "
                        + "caracteres, só com letras sem acento, números e _.",
                CodigosErro.SenhaInvalida =>
                    $"A senha deve ter de {RegrasConta.SenhaMinimo} a {RegrasConta.SenhaMaximo} caracteres.",
                CodigosErroCliente.SemConexao =>
                    "Não foi possível conectar ao servidor. Verifique a conexão e tente de novo.",
                CodigosErroCliente.HttpBloqueado =>
                    "Não foi possível conectar ao servidor (HTTP bloqueado nas configurações do projeto).",
                _ => MensagemGenerica,
            };
        }

        private void DefinirEsperando(bool esperando)
        {
            _esperando = esperando;
            _usuario.SetEnabled(!esperando);
            _senha.SetEnabled(!esperando);
            _entrar.SetEnabled(!esperando);
            _criarConta.SetEnabled(!esperando);
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
