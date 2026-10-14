using System;
using System.Collections.Generic;
using System.Text;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.TempoReal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>Formato dos pacotes de tempo real.</summary>
    public class CodecTempoRealTestes
    {
        [Test]
        public void Codificar_PoeIdLittleEndianEJsonUtf8()
        {
            var dados = CodecTempoReal.Codificar(
                0x0402,
                new AutenticarTempoReal { Token = "abc", PersonagemId = 7 }
            );

            Assert.AreEqual(0x02, dados[0]);
            Assert.AreEqual(0x04, dados[1]);
            var json = Encoding.UTF8.GetString(dados, 2, dados.Length - 2);
            Assert.AreEqual("{\"Token\":\"abc\",\"PersonagemId\":7}", json);
        }

        [Test]
        public void CodificarEDecodificar_DevolveOMesmoDto()
        {
            var dados = CodecTempoReal.Codificar(
                TempoRealIds.ResultadoAutenticacaoTempoReal,
                new ResultadoAutenticacaoTempoReal { Aceita = false, Codigo = "AcessoNegado" }
            );

            Assert.IsTrue(CodecTempoReal.TentarDecodificar(dados, out var mensagem));
            Assert.AreEqual(TempoRealIds.ResultadoAutenticacaoTempoReal, mensagem.Id);
            var lido = mensagem.Ler<ResultadoAutenticacaoTempoReal>();
            Assert.IsFalse(lido.Aceita);
            Assert.AreEqual("AcessoNegado", lido.Codigo);
        }

        [Test]
        public void SemCorpo_SoOId_ELeValoresPadrao()
        {
            var dados = CodecTempoReal.Codificar<AutenticarTempoReal>(ushort.MaxValue, null);

            Assert.AreEqual(2, dados.Length);
            Assert.IsTrue(CodecTempoReal.TentarDecodificar(dados, out var mensagem));
            Assert.AreEqual(ushort.MaxValue, mensagem.Id);
            Assert.AreEqual("", mensagem.Json);
            Assert.AreEqual(0, mensagem.Ler<AutenticarTempoReal>().PersonagemId);
        }

        [Test]
        public void PacoteCurtoNuloOuUtf8Invalido_Recusa()
        {
            Assert.IsFalse(CodecTempoReal.TentarDecodificar(null, out _));
            Assert.IsFalse(CodecTempoReal.TentarDecodificar(new byte[] { 1 }, out _));
            Assert.IsFalse(
                CodecTempoReal.TentarDecodificar(new byte[] { 1, 0, 0xFF, 0xFE }, out _)
            );
        }
    }

    /// <summary>Entrega das mensagens a quem assinou o Id ou a faixa.</summary>
    public class RoteadorTempoRealTestes
    {
        private static MensagemTempoReal Mensagem(ushort id) => new MensagemTempoReal(id, "{}");

        [Test]
        public void PorId_SoRecebeOProprioId()
        {
            var roteador = new RoteadorTempoReal();
            var recebidos = new List<ushort>();
            roteador.Assinar(1000, m => recebidos.Add(m.Id));

            roteador.Despachar(Mensagem(1000));
            roteador.Despachar(Mensagem(1001));

            CollectionAssert.AreEqual(new ushort[] { 1000 }, recebidos);
        }

        [Test]
        public void PorFaixa_RecebeDoInicioAoFimEMaisNada()
        {
            var roteador = new RoteadorTempoReal();
            var recebidos = new List<ushort>();
            roteador.AssinarFaixa(new MessageIdRange(1000, 1999), m => recebidos.Add(m.Id));

            foreach (var id in new ushort[] { 999, 1000, 1500, 1999, 2000 })
            {
                roteador.Despachar(Mensagem(id));
            }

            CollectionAssert.AreEqual(new ushort[] { 1000, 1500, 1999 }, recebidos);
        }

        [Test]
        public void Tipado_ConverteODto()
        {
            var roteador = new RoteadorTempoReal();
            ResultadoAutenticacaoTempoReal recebido = null;
            roteador.Assinar<ResultadoAutenticacaoTempoReal>(2, r => recebido = r);

            roteador.Despachar(new MensagemTempoReal(2, "{\"Aceita\":true,\"Codigo\":\"\"}"));

            Assert.IsNotNull(recebido);
            Assert.IsTrue(recebido.Aceita);
        }

        [Test]
        public void Cancelar_ParaDeReceber_EDuasVezesNaoQuebra()
        {
            var roteador = new RoteadorTempoReal();
            var vezes = 0;
            var assinatura = roteador.Assinar(5, _ => vezes++);

            roteador.Despachar(Mensagem(5));
            assinatura.Dispose();
            assinatura.Dispose();
            roteador.Despachar(Mensagem(5));

            Assert.AreEqual(1, vezes);
            Assert.AreEqual(0, roteador.Quantidade);
        }

        [Test]
        public void CancelarDuranteAEntrega_NaoPulaOsOutros()
        {
            var roteador = new RoteadorTempoReal();
            var vezes = 0;
            IDisposable primeira = null;
            primeira = roteador.Assinar(5, _ => primeira.Dispose());
            roteador.Assinar(5, _ => vezes++);

            Assert.AreEqual(2, roteador.Despachar(Mensagem(5)));
            Assert.AreEqual(1, vezes);
            Assert.AreEqual(1, roteador.Despachar(Mensagem(5)));
        }

        [Test]
        public void AssinanteQueLanca_NaoImpedeOsOutros()
        {
            var roteador = new RoteadorTempoReal();
            var vezes = 0;
            roteador.Assinar(5, _ => throw new InvalidOperationException("falha de teste"));
            roteador.Assinar(5, _ => vezes++);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: falha de teste");
            roteador.Despachar(Mensagem(5));

            Assert.AreEqual(1, vezes);
        }

        [Test]
        public void AssinarSemAcao_Lanca()
        {
            var roteador = new RoteadorTempoReal();
            Assert.Throws<ArgumentNullException>(() => roteador.Assinar(1, null));
            Assert.Throws<ArgumentNullException>(() =>
                roteador.Assinar<AutenticarTempoReal>(1, null)
            );
        }
    }

    /// <summary>Conexão, autenticação, queda e reconexão com um transporte falso.</summary>
    public class ClienteTempoRealTestes
    {
        private const string Token = "token-de-teste";

        private TransporteFalso _transporte;
        private RoteadorTempoReal _roteador;
        private ClienteTempoReal _cliente;
        private List<string> _tokensRecusados;
        private List<EstadoTempoReal> _estados;

        [SetUp]
        public void Criar()
        {
            _transporte = new TransporteFalso();
            _roteador = new RoteadorTempoReal();
            _tokensRecusados = new List<string>();
            _estados = new List<EstadoTempoReal>();
            _cliente = new ClienteTempoReal(_transporte, _roteador, _tokensRecusados.Add);
            _cliente.EstadoMudou += _estados.Add;
        }

        [Test]
        public void SemToken_NaoConecta()
        {
            Assert.IsFalse(_cliente.Conectar("localhost", 5081, "chave", "", 1));

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual(CodigosErro.NaoAutenticado, _cliente.UltimoErro);
            Assert.AreEqual(0, _transporte.Conexoes);
        }

        [Test]
        public void AoConectar_EnviaOTokenEOPersonagem()
        {
            Assert.IsTrue(_cliente.Conectar("servidor", 6000, "chave", Token, 42));
            Assert.AreEqual(EstadoTempoReal.Conectando, _cliente.Estado);
            Assert.AreEqual(("servidor", 6000, "chave"), _transporte.UltimoDestino);

            _transporte.SimularConexao();

            Assert.AreEqual(EstadoTempoReal.Autenticando, _cliente.Estado);
            var enviado = _transporte.Lido(0);
            Assert.AreEqual(TempoRealIds.AutenticarTempoReal, enviado.Id);
            var autenticar = enviado.Ler<AutenticarTempoReal>();
            Assert.AreEqual(Token, autenticar.Token);
            Assert.AreEqual(42, autenticar.PersonagemId);
            Assert.AreEqual(EntregaTempoReal.Confiavel, _transporte.Entregas[0]);
        }

        [Test]
        public void Aceita_FicaConectadoEEntregaMensagens()
        {
            var recebidos = new List<ushort>();
            _roteador.AssinarFaixa(new MessageIdRange(1000, 1999), m => recebidos.Add(m.Id));
            Autenticar();

            Assert.AreEqual(EstadoTempoReal.Conectado, _cliente.Estado);
            Assert.AreEqual("", _cliente.UltimoErro);
            CollectionAssert.AreEqual(
                new[]
                {
                    EstadoTempoReal.Conectando,
                    EstadoTempoReal.Autenticando,
                    EstadoTempoReal.Conectado,
                },
                _estados
            );

            _transporte.SimularRecebimento(1003, new AutenticarTempoReal());
            CollectionAssert.AreEqual(new ushort[] { 1003 }, recebidos);
        }

        [Test]
        public void AntesDeAutenticar_NaoEntregaNemEnvia()
        {
            var recebidos = 0;
            _roteador.AssinarFaixa(new MessageIdRange(0, 2000), _ => recebidos++);
            _cliente.Conectar("localhost", 5081, "chave", Token, 1);

            Assert.IsFalse(
                _cliente.Enviar(1000, new AutenticarTempoReal(), EntregaTempoReal.Confiavel)
            );
            _transporte.SimularConexao();
            Assert.IsFalse(
                _cliente.Enviar(1000, new AutenticarTempoReal(), EntregaTempoReal.Confiavel)
            );
            _transporte.SimularRecebimento(1000, new AutenticarTempoReal());

            Assert.AreEqual(0, recebidos);
            Assert.AreEqual(1, _transporte.Enviados.Count, "Só a autenticação.");
        }

        [Test]
        public void Conectado_EnviaComOIdEAEntrega()
        {
            Autenticar();

            Assert.IsTrue(
                _cliente.Enviar(1004, new AutenticarTempoReal(), EntregaTempoReal.Sequenciada)
            );

            Assert.AreEqual(1004, _transporte.Lido(1).Id);
            Assert.AreEqual(EntregaTempoReal.Sequenciada, _transporte.Entregas[1]);
        }

        [Test]
        public void TokenRecusado_DesconectaEAvisaComOToken()
        {
            _cliente.Conectar("localhost", 5081, "chave", Token, 1);
            _transporte.SimularConexao();
            _transporte.SimularRecebimento(
                TempoRealIds.ResultadoAutenticacaoTempoReal,
                new ResultadoAutenticacaoTempoReal
                {
                    Aceita = false,
                    Codigo = CodigosErro.NaoAutenticado,
                }
            );

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual(CodigosErro.NaoAutenticado, _cliente.UltimoErro);
            CollectionAssert.AreEqual(new[] { Token }, _tokensRecusados);
            Assert.AreEqual(1, _transporte.Desconexoes);
        }

        [Test]
        public void AcessoNegado_DesconectaSemDerrubarASessao()
        {
            _cliente.Conectar("localhost", 5081, "chave", Token, 1);
            _transporte.SimularConexao();
            _transporte.SimularRecebimento(
                TempoRealIds.ResultadoAutenticacaoTempoReal,
                new ResultadoAutenticacaoTempoReal
                {
                    Aceita = false,
                    Codigo = CodigosErro.AcessoNegado,
                }
            );

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual(CodigosErro.AcessoNegado, _cliente.UltimoErro);
            Assert.IsEmpty(_tokensRecusados);

            // Recusa não reconecta.
            _cliente.Atualizar(60f);
            Assert.AreEqual(1, _transporte.Conexoes);
        }

        [Test]
        public void ServidorForaDoAr_NaoInsiste()
        {
            _cliente.Conectar("localhost", 5081, "chave", Token, 1);
            _transporte.SimularQueda();

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual(CodigosErroCliente.SemConexao, _cliente.UltimoErro);
            _cliente.Atualizar(60f);
            Assert.AreEqual(1, _transporte.Conexoes);
        }

        [Test]
        public void SemRespostaAAutenticacao_DesisteNoTempoLimite()
        {
            _cliente.Conectar("localhost", 5081, "chave", Token, 1);
            _transporte.SimularConexao();

            _cliente.Atualizar(ClienteTempoReal.TempoLimiteAutenticacao - 0.1f);
            Assert.AreEqual(EstadoTempoReal.Autenticando, _cliente.Estado);
            _cliente.Atualizar(0.2f);

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual(CodigosErroCliente.SemConexao, _cliente.UltimoErro);
            Assert.AreEqual(1, _transporte.Desconexoes);
        }

        [Test]
        public void QuedaDepoisDeAutenticar_ReconectaEAutenticaDeNovo()
        {
            Autenticar();
            _transporte.SimularQueda();

            Assert.AreEqual(EstadoTempoReal.Reconectando, _cliente.Estado);
            Assert.IsFalse(
                _cliente.Enviar(1000, new AutenticarTempoReal(), EntregaTempoReal.Confiavel)
            );
            _cliente.Atualizar(0.5f);
            Assert.AreEqual(1, _transporte.Conexoes, "Espera antes de tentar.");
            _cliente.Atualizar(0.6f);
            Assert.AreEqual(2, _transporte.Conexoes);
            Assert.AreEqual(EstadoTempoReal.Conectando, _cliente.Estado);

            _transporte.SimularConexao();
            Assert.AreEqual(
                Token,
                _transporte.Lido(_transporte.Enviados.Count - 1).Ler<AutenticarTempoReal>().Token
            );
            Aceitar();
            Assert.AreEqual(EstadoTempoReal.Conectado, _cliente.Estado);
        }

        [Test]
        public void QuedasSeguidas_DesisteDepoisDoMaximoDeTentativas()
        {
            Autenticar();
            _transporte.SimularQueda();

            for (var i = 0; i < ClienteTempoReal.MaximoTentativas; i++)
            {
                Assert.AreEqual(EstadoTempoReal.Reconectando, _cliente.Estado);
                _cliente.Atualizar(60f);
                _transporte.SimularQueda();
            }

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual(CodigosErroCliente.SemConexao, _cliente.UltimoErro);
            Assert.AreEqual(1 + ClienteTempoReal.MaximoTentativas, _transporte.Conexoes);
        }

        [Test]
        public void ReconectarComSucesso_ZeraAsTentativas()
        {
            Autenticar();
            for (var i = 0; i < ClienteTempoReal.MaximoTentativas + 2; i++)
            {
                _transporte.SimularQueda();
                _cliente.Atualizar(60f);
                _transporte.SimularConexao();
                Aceitar();
            }

            Assert.AreEqual(EstadoTempoReal.Conectado, _cliente.Estado);
        }

        [Test]
        public void Desconectar_NaoReconectaEIgnoraEventosAtrasados()
        {
            Autenticar();
            _cliente.Desconectar();
            _transporte.SimularQueda();
            _cliente.Atualizar(60f);

            Assert.AreEqual(EstadoTempoReal.Desconectado, _cliente.Estado);
            Assert.AreEqual("", _cliente.UltimoErro);
            Assert.AreEqual(1, _transporte.Conexoes);
            Assert.IsFalse(
                _cliente.Enviar(1000, new AutenticarTempoReal(), EntregaTempoReal.Confiavel)
            );

            // Desconectar de novo não faz nada.
            _cliente.Desconectar();
            Assert.AreEqual(1, _transporte.Desconexoes);
        }

        [Test]
        public void ConectarDeNovo_FechaAAnteriorAntes()
        {
            Autenticar();
            _cliente.Conectar("localhost", 5081, "chave", "outro", 2);

            Assert.AreEqual(1, _transporte.Desconexoes);
            Assert.AreEqual(2, _transporte.Conexoes);
            Assert.AreEqual(EstadoTempoReal.Conectando, _cliente.Estado);
        }

        [Test]
        public void PacoteInvalido_EDescartado()
        {
            var recebidos = 0;
            _roteador.AssinarFaixa(new MessageIdRange(0, ushort.MaxValue), _ => recebidos++);
            Autenticar();

            _transporte.SimularBytes(new byte[] { 7 });

            Assert.AreEqual(0, recebidos);
            Assert.AreEqual(EstadoTempoReal.Conectado, _cliente.Estado);
        }

        [Test]
        public void Atualizar_ProcessaOTransporte()
        {
            _cliente.Atualizar(0.016f);
            Assert.AreEqual(1, _transporte.Processamentos);
        }

        private void Autenticar()
        {
            _cliente.Conectar("localhost", 5081, "chave", Token, 1);
            _transporte.SimularConexao();
            Aceitar();
        }

        private void Aceitar()
        {
            _transporte.SimularRecebimento(
                TempoRealIds.ResultadoAutenticacaoTempoReal,
                new ResultadoAutenticacaoTempoReal { Aceita = true }
            );
        }

        private sealed class TransporteFalso : ITransporteTempoReal
        {
            public int Latencia { get; set; } = -1;
            public readonly List<byte[]> Enviados = new List<byte[]>();
            public readonly List<EntregaTempoReal> Entregas = new List<EntregaTempoReal>();

            public int Conexoes { get; private set; }
            public int Desconexoes { get; private set; }
            public int Processamentos { get; private set; }
            public (string, int, string) UltimoDestino { get; private set; }

            public event Action Conectou;
            public event Action<string> Caiu;
            public event Action<byte[]> Recebeu;

            public void Conectar(string host, int porta, string chave)
            {
                Conexoes++;
                UltimoDestino = (host, porta, chave);
            }

            public void Enviar(byte[] dados, EntregaTempoReal entrega)
            {
                Enviados.Add(dados);
                Entregas.Add(entrega);
            }

            public void Desconectar() => Desconexoes++;

            public void Processar() => Processamentos++;

            public MensagemTempoReal Lido(int indice)
            {
                Assert.IsTrue(CodecTempoReal.TentarDecodificar(Enviados[indice], out var mensagem));
                return mensagem;
            }

            public void SimularConexao() => Conectou?.Invoke();

            public void SimularQueda() => Caiu?.Invoke("queda de teste");

            public void SimularRecebimento<T>(ushort id, T corpo) =>
                Recebeu?.Invoke(CodecTempoReal.Codificar(id, corpo));

            public void SimularBytes(byte[] dados) => Recebeu?.Invoke(dados);
        }
    }
}
