using System;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Shared.Core.Contas;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// O login gravado sobrevive ao fechar o jogo e some no "Sair" ou quando o token vence.
    /// </summary>
    public class SessaoTestes
    {
        private const string ChaveToken = "jogodogrupo.sessao.token";
        private const string ChaveExpira = "jogodogrupo.sessao.expira";

        private string _tokenAntes;
        private string _expiraAntes;

        [SetUp]
        public void GuardarPrefs()
        {
            _tokenAntes = PlayerPrefs.GetString(ChaveToken, "");
            _expiraAntes = PlayerPrefs.GetString(ChaveExpira, "");
            Sessao.Encerrar();
        }

        [TearDown]
        public void DevolverPrefs()
        {
            Sessao.EsquecerMemoria();
            if (string.IsNullOrEmpty(_tokenAntes))
            {
                PlayerPrefs.DeleteKey(ChaveToken);
            }
            else
            {
                PlayerPrefs.SetString(ChaveToken, _tokenAntes);
            }

            if (string.IsNullOrEmpty(_expiraAntes))
            {
                PlayerPrefs.DeleteKey(ChaveExpira);
            }
            else
            {
                PlayerPrefs.SetString(ChaveExpira, _expiraAntes);
            }

            PlayerPrefs.Save();
        }

        [Test]
        public void Iniciar_GravaOToken_ERestaurarTrazDeVoltaDepoisDeFechar()
        {
            var expira = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
            Sessao.Iniciar(new LoginResponse { Token = "abc", ExpiraEmUnix = expira });

            Sessao.EsquecerMemoria();
            Assert.IsFalse(Sessao.Ativa);

            Sessao.Restaurar();

            Assert.IsTrue(Sessao.Ativa);
            Assert.AreEqual("abc", Sessao.Token);
        }

        [Test]
        public void Encerrar_ApagaOLoginGravado()
        {
            Sessao.Iniciar(
                new LoginResponse
                {
                    Token = "abc",
                    ExpiraEmUnix = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                }
            );

            Sessao.Encerrar();
            Sessao.Restaurar();

            Assert.IsFalse(Sessao.Ativa);
            Assert.IsNull(Sessao.Token);
        }

        [Test]
        public void Restaurar_ApagaTokenVencido()
        {
            Sessao.Iniciar(
                new LoginResponse
                {
                    Token = "velho",
                    ExpiraEmUnix = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
                }
            );
            Sessao.EsquecerMemoria();

            Sessao.Restaurar();

            Assert.IsFalse(Sessao.Ativa);
            Assert.AreEqual("", PlayerPrefs.GetString(ChaveToken, ""));
        }
    }
}
