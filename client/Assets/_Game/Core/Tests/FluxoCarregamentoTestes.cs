using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Core.Api;
using NUnit.Framework;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class FluxoCarregamentoTestes
    {
        [Test]
        public void TelaDeEntrada_MostraEtapaRealEFalhaRecuperavel()
        {
            var tela = new TelaCarregamento();
            var raiz = new VisualElement();
            tela.Abrir(raiz);
            tela.MostrarEtapa(EtapaDoCarregamento.Conexao);

            Assert.AreEqual(
                "Conectando ao mundo...",
                raiz.Q<Label>(className: "entrada-mundo-estado").text
            );
            Assert.IsTrue(
                raiz.Query<Label>(className: "entrada-mundo-etapa-ativa").First().text == "Conexão"
            );

            tela.MostrarFalha("Não foi possível entrar", "A conexão não foi concluída.");
            Assert.AreEqual(
                "Não foi possível entrar",
                raiz.Q<Label>(className: "entrada-mundo-estado").text
            );
            Assert.AreEqual(2, raiz.Query<Button>().ToList().Count);
            tela.Fechar();
        }

        [Test]
        public void EsperaDaPosicao_ExpiraNoPrazo()
        {
            Assert.IsFalse(EntradaNoJogo.PrazoEsgotado(10f, 29.9f));
            Assert.IsTrue(EntradaNoJogo.PrazoEsgotado(10f, 30f));
        }

        [Test]
        public void Abertura_BloqueiaOfflineEversaoIncompativel()
        {
            Assert.IsTrue(TelaAbertura.FalhaDaSaude(false, null).Contains("fora do ar"));
            Assert.IsTrue(
                TelaAbertura
                    .FalhaDaSaude(true, new EstadoDaSaude { Estado = "ok", Versao = 2 })
                    .Contains("incompatível")
            );
            Assert.IsNull(
                TelaAbertura.FalhaDaSaude(
                    true,
                    new EstadoDaSaude { Estado = "ok", Versao = EstadoDaSaude.VersaoAtual }
                )
            );
        }
    }

    public class TelaLoginTestes
    {
        [Test]
        public void Formato_CamposAusentesMostramOrientacao()
        {
            Assert.AreEqual(
                "Preencha o usuário e a senha.",
                TelaLogin.MensagemDeFormato("", "senha1234")
            );
            Assert.AreEqual(
                "Preencha o usuário e a senha.",
                TelaLogin.MensagemDeFormato("usuario", null)
            );
        }

        [Test]
        public void Formato_UsuarioInvalidoMostraOrientacao()
        {
            StringAssert.Contains(
                "só com letras sem acento",
                TelaLogin.MensagemDeFormato("João", "senha1234")
            );
        }

        [Test]
        public void Formato_SenhaInvalidaMostraOrientacao()
        {
            StringAssert.Contains(
                "A senha deve ter",
                TelaLogin.MensagemDeFormato("usuario", "curta")
            );
        }

        [Test]
        public void Formato_CredenciaisValidasNaoMostramErro()
        {
            Assert.IsNull(TelaLogin.MensagemDeFormato("usuario_1", "senha1234"));
        }

        [Test]
        public void Envio_DuploCliqueIgnoraSegundaAcaoEnquantoAPrimeiraEstaAtiva()
        {
            var estado = new TelaLogin.EstadoDeEnvioLogin();
            var validacoes = 0;
            Func<bool> validar = () =>
            {
                validacoes++;
                return true;
            };

            Assert.IsTrue(estado.TentarIniciar(validar));
            Assert.IsFalse(estado.TentarIniciar(validar));
            Assert.AreEqual(1, validacoes);

            estado.Encerrar();

            Assert.IsTrue(estado.TentarIniciar(validar));
            Assert.AreEqual(2, validacoes);
        }

        [Test]
        public void Api_TodosCodigosConhecidosViraramMensagemEsperada()
        {
            Assert.AreEqual(
                "Usuário ou senha incorretos.",
                TelaLogin.MensagemDoErro(CodigosErro.CredenciaisInvalidas)
            );
            Assert.AreEqual(
                "Esse usuário já existe. Escolha outro.",
                TelaLogin.MensagemDoErro(CodigosErro.UsuarioJaExiste)
            );
            Assert.AreEqual(
                "Não foi possível conectar ao servidor. Verifique a conexão e tente de novo.",
                TelaLogin.MensagemDoErro(CodigosErroCliente.SemConexao)
            );
            Assert.AreEqual(
                "Não foi possível conectar ao servidor (HTTP bloqueado nas configurações do projeto).",
                TelaLogin.MensagemDoErro(CodigosErroCliente.HttpBloqueado)
            );

            var mensagemGenerica = "Algo deu errado. Tente de novo.";
            var codigosGenericos = new[]
            {
                CodigosErro.UsuarioInvalido,
                CodigosErro.SenhaInvalida,
                CodigosErro.NaoAutenticado,
                CodigosErro.RequisicaoInvalida,
                CodigosErro.AcessoNegado,
                CodigosErro.RotaNaoEncontrada,
                CodigosErro.MetodoNaoPermitido,
                CodigosErro.ServidorOcupado,
                CodigosErro.ErroInterno,
                CodigosErroCliente.RespostaInvalida,
                CodigosErroCliente.Cancelada,
                "Outro",
            };
            foreach (var codigo in codigosGenericos)
            {
                Assert.AreEqual(mensagemGenerica, TelaLogin.MensagemDoErro(codigo), codigo);
            }

            StringAssert.Contains(
                "O usuário deve ter",
                TelaLogin.MensagemDoErro(CodigosErro.UsuarioInvalido)
            );
            StringAssert.Contains(
                "A senha deve ter",
                TelaLogin.MensagemDoErro(CodigosErro.SenhaInvalida)
            );
        }
    }

    public class ClienteApiTestes
    {
        [Test]
        public void TimeoutSemResposta_MapeiaParaSemConexao()
        {
            Assert.AreEqual(
                CodigosErroCliente.SemConexao,
                ClienteApi.CodigoErroDeResposta(UnityWebRequest.Result.ConnectionError, 0, false)
            );
        }

        [Test]
        public void RespostaSemStatus_MapeiaParaSemConexao()
        {
            Assert.AreEqual(
                CodigosErroCliente.SemConexao,
                ClienteApi.CodigoErroDeResposta(UnityWebRequest.Result.DataProcessingError, 0, true)
            );
        }

        [Test]
        public void Http401_SoRotaAutenticadaMapeiaParaNaoAutenticado()
        {
            Assert.AreEqual(
                CodigosErro.NaoAutenticado,
                ClienteApi.CodigoErroDeResposta(UnityWebRequest.Result.ProtocolError, 401, true)
            );
            Assert.IsNull(
                ClienteApi.CodigoErroDeResposta(UnityWebRequest.Result.ProtocolError, 401, false)
            );
        }

        [Test]
        public void FalhaAoEnviar_MapeiaHttpBloqueadoOuSemConexao()
        {
            Assert.AreEqual(
                CodigosErroCliente.HttpBloqueado,
                ClienteApi.CodigoErroAntesDeEnviar(
                    "http://localhost:5080",
                    new InvalidOperationException("Insecure connection")
                )
            );
            Assert.AreEqual(
                CodigosErroCliente.SemConexao,
                ClienteApi.CodigoErroAntesDeEnviar(
                    "https://localhost:5080",
                    new InvalidOperationException("Falha")
                )
            );
        }

        [Test]
        public void LerErro_UsaErroApiValidoOuRespostaInvalida()
        {
            var erro = ClienteApi.LerErro(
                "{\"Codigo\":\"CredenciaisInvalidas\",\"Mensagem\":\"falhou\"}"
            );

            Assert.AreEqual(CodigosErro.CredenciaisInvalidas, erro.Codigo);
            Assert.AreEqual("falhou", erro.Mensagem);
            Assert.AreEqual(CodigosErroCliente.RespostaInvalida, ClienteApi.LerErro("{}").Codigo);
            Assert.AreEqual(
                CodigosErroCliente.RespostaInvalida,
                ClienteApi.LerErro("não é JSON").Codigo
            );
        }
    }
}
