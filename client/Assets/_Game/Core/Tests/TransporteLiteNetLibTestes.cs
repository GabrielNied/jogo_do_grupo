using System;
using System.Diagnostics;
using System.Threading;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core.TempoReal;
using LiteNetLib;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Transporte de verdade contra um NetManager local configurado como o servidor (#819, #820).
    /// </summary>
    public class TransporteLiteNetLibTestes
    {
        private const string Chave = "teste";

        [Test]
        public void Sequenciada_NoUltimoCanalDoServidor_Chega()
        {
            var ouvinte = new EventBasedNetListener();
            var servidor = new NetManager(ouvinte)
            {
                ChannelsCount = TransporteTempoReal.Canais,
                IPv6Enabled = false,
            };
            var canal = (byte)(TransporteTempoReal.Canais - 1);
            var enviado = new byte[] { 7, 0, 42 };
            ouvinte.ConnectionRequestEvent += pedido => pedido.AcceptIfKey(Chave);
            ouvinte.PeerConnectedEvent += peer =>
                peer.Send(enviado, canal, DeliveryMethod.Sequenced);

            var transporte = new TransporteLiteNetLib();
            byte[] recebido = null;
            string caiu = null;
            transporte.Recebeu += segmento =>
            {
                recebido = new byte[segmento.Count];
                Array.Copy(segmento.Array, segmento.Offset, recebido, 0, segmento.Count);
            };
            transporte.Caiu += motivo => caiu = motivo;

            Assert.IsTrue(servidor.Start(0), "o servidor de teste não abriu o socket");
            try
            {
                transporte.Conectar("127.0.0.1", servidor.LocalPort, Chave);
                var relogio = Stopwatch.StartNew();
                while (recebido == null && caiu == null && relogio.ElapsedMilliseconds < 5000)
                {
                    servidor.PollEvents();
                    transporte.Processar();
                    Thread.Sleep(5);
                }
            }
            finally
            {
                transporte.Desconectar();
                servidor.Stop();
            }

            Assert.IsNull(caiu, "a conexão caiu");
            Assert.IsNotNull(recebido, $"a mensagem do canal {canal} não chegou");
            CollectionAssert.AreEqual(enviado, recebido);
        }
    }
}
