using System;
using LiteNetLib;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Transporte UDP com a LiteNetLib (ADR 0015). Um <see cref="NetManager"/> por conexão; os
    /// eventos só saem em <see cref="Processar"/>, na thread principal.
    /// </summary>
    internal sealed class TransporteLiteNetLib : ITransporteTempoReal
    {
        /// <summary>Sem pacote do servidor por este tempo, a conexão é dada como caída.</summary>
        public const int TempoLimiteMs = 5000;

        private NetManager _manager;
        private NetPeer _servidor;
        private NetManager _pararDepois;
        private bool _processando;

        public int Latencia => _servidor?.Ping ?? -1;

        public event Action Conectou;
        public event Action<string> Caiu;
        public event Action<ArraySegment<byte>> Recebeu;

        public void Conectar(string host, int porta, string chave)
        {
            Encerrar();

            var ouvinte = new EventBasedNetListener();
            var manager = new NetManager(ouvinte) { DisconnectTimeout = TempoLimiteMs };

            ouvinte.PeerConnectedEvent += peer =>
            {
                if (_manager == manager)
                {
                    Conectou?.Invoke();
                }
            };
            ouvinte.PeerDisconnectedEvent += (peer, info) =>
            {
                if (_manager == manager)
                {
                    Encerrar();
                    Caiu?.Invoke($"{info.Reason} ({info.SocketErrorCode})");
                }
            };
            ouvinte.NetworkReceiveEvent += (peer, leitor, canal, entrega) =>
            {
                var segmento = new ArraySegment<byte>(
                    leitor.RawData,
                    leitor.Position,
                    leitor.AvailableBytes
                );
                if (_manager == manager)
                {
                    Recebeu?.Invoke(segmento);
                }
                leitor.Recycle();
            };

            _manager = manager;
            if (!manager.Start())
            {
                Encerrar();
                Caiu?.Invoke("não abriu o socket UDP");
                return;
            }

            _servidor = manager.Connect(host, porta, chave);
            if (_servidor == null)
            {
                Encerrar();
                Caiu?.Invoke($"não conectou em {host}:{porta}");
            }
        }

        public void Enviar(byte[] dados, EntregaTempoReal entrega)
        {
            if (dados != null)
            {
                Enviar(new ArraySegment<byte>(dados, 0, dados.Length), entrega);
            }
        }

        public void Enviar(ArraySegment<byte> dados, EntregaTempoReal entrega)
        {
            if (dados.Array != null && dados.Count > 0)
            {
                _servidor?.Send(dados.Array, dados.Offset, dados.Count, ParaLiteNetLib(entrega));
            }
        }

        public void Desconectar() => Encerrar();

        public void Processar()
        {
            if (_manager == null)
            {
                return;
            }

            _processando = true;
            try
            {
                _manager.PollEvents();
            }
            finally
            {
                _processando = false;
                var parar = _pararDepois;
                _pararDepois = null;
                parar?.Stop();
            }
        }

        internal static DeliveryMethod ParaLiteNetLib(EntregaTempoReal entrega)
        {
            switch (entrega)
            {
                case EntregaTempoReal.Sequenciada:
                    return DeliveryMethod.Sequenced;
                case EntregaTempoReal.NaoConfiavel:
                    return DeliveryMethod.Unreliable;
                default:
                    return DeliveryMethod.ReliableOrdered;
            }
        }

        private void Encerrar()
        {
            var manager = _manager;
            _manager = null;
            _servidor = null;
            if (manager == null)
            {
                return;
            }

            // Stop avisa o servidor; os eventos que ainda vierem desse manager são ignorados.
            // Dentro do PollEvents, só para no fim dele.
            if (_processando)
            {
                _pararDepois?.Stop();
                _pararDepois = manager;
            }
            else
            {
                manager.Stop();
            }
        }
    }
}
