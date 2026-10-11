using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Inicializacao;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Endereço do servidor (#198): argumento &gt; servidor.json &gt; padrão, com aviso para valor inválido.
    /// </summary>
    public class ConfiguracaoServidorTestes
    {
        private static readonly EnderecoServidor Padrao = new EnderecoServidor(
            "http://localhost:5080",
            "localhost",
            5081
        );

        private List<string> _avisos;

        [SetUp]
        public void Limpar() => _avisos = new List<string>();

        private EnderecoServidor Resolver(string[] argumentos, string arquivo) =>
            ConfiguracaoServidor.Resolver(argumentos, arquivo, Padrao, _avisos.Add);

        [Test]
        public void Sem_argumento_nem_arquivo_usa_o_padrao()
        {
            var endereco = Resolver(new[] { "Jogo.exe" }, null);

            Assert.AreEqual("http://localhost:5080", endereco.UrlApi);
            Assert.AreEqual("localhost", endereco.HostTempoReal);
            Assert.AreEqual(5081, endereco.PortaTempoReal);
            Assert.IsEmpty(_avisos);
        }

        [Test]
        public void Argumentos_definem_api_e_tempo_real()
        {
            var endereco = Resolver(
                new[]
                {
                    "Jogo.exe",
                    "-servidor",
                    "https://jogo.exemplo.org/",
                    "-tempo-real",
                    "jogo.exemplo.org:6000",
                },
                null
            );

            Assert.AreEqual("https://jogo.exemplo.org", endereco.UrlApi);
            Assert.AreEqual("jogo.exemplo.org", endereco.HostTempoReal);
            Assert.AreEqual(6000, endereco.PortaTempoReal);
        }

        [Test]
        public void Arquivo_vale_quando_nao_ha_argumento()
        {
            var endereco = Resolver(
                new[] { "Jogo.exe" },
                "{ \"servidor\": \"https://arquivo.org\", \"tempoReal\": \"arquivo.org:7000\" }"
            );

            Assert.AreEqual("https://arquivo.org", endereco.UrlApi);
            Assert.AreEqual("arquivo.org", endereco.HostTempoReal);
            Assert.AreEqual(7000, endereco.PortaTempoReal);
        }

        [Test]
        public void Argumento_ganha_do_arquivo_e_cada_endereco_e_independente()
        {
            var endereco = Resolver(
                new[] { "Jogo.exe", "-servidor", "https://argumento.org" },
                "{ \"servidor\": \"https://arquivo.org\", \"tempoReal\": \"arquivo.org:7000\" }"
            );

            Assert.AreEqual("https://argumento.org", endereco.UrlApi);
            Assert.AreEqual("arquivo.org", endereco.HostTempoReal);
        }

        [Test]
        public void Tempo_real_sem_porta_usa_a_porta_padrao()
        {
            var endereco = Resolver(new[] { "Jogo.exe", "-tempo-real", "jogo.exemplo.org" }, null);

            Assert.AreEqual("jogo.exemplo.org", endereco.HostTempoReal);
            Assert.AreEqual(5081, endereco.PortaTempoReal);
        }

        [Test]
        public void Argumento_invalido_avisa_e_cai_para_o_arquivo()
        {
            var endereco = Resolver(
                new[] { "Jogo.exe", "-servidor", "ftp://errado", "-tempo-real", "host:99999" },
                "{ \"servidor\": \"https://arquivo.org\", \"tempoReal\": \"arquivo.org:7000\" }"
            );

            Assert.AreEqual("https://arquivo.org", endereco.UrlApi);
            Assert.AreEqual(7000, endereco.PortaTempoReal);
            Assert.AreEqual(2, _avisos.Count);
        }

        [Test]
        public void Tudo_invalido_avisa_e_volta_ao_padrao()
        {
            var endereco = Resolver(
                new[] { "Jogo.exe", "-servidor", "sem-esquema" },
                "{ isto não é json"
            );

            Assert.AreEqual("http://localhost:5080", endereco.UrlApi);
            Assert.AreEqual("localhost", endereco.HostTempoReal);
            Assert.AreEqual(2, _avisos.Count, "um aviso do JSON e um do argumento");
        }

        [Test]
        public void Argumento_sem_valor_no_fim_e_ignorado()
        {
            var endereco = Resolver(new[] { "Jogo.exe", "-servidor" }, null);

            Assert.AreEqual("http://localhost:5080", endereco.UrlApi);
            Assert.IsEmpty(_avisos);
        }

        [TestCase("localhost:0")]
        [TestCase(":5081")]
        [TestCase("host/caminho:5081")]
        [TestCase("host:abc")]
        public void Host_porta_invalido_e_recusado(string valor)
        {
            Assert.IsFalse(ConfiguracaoServidor.TentarHostPorta(valor, 5081, out _, out _));
        }
    }
}
