using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.TempoReal;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Estado da conexão de tempo real, sem Unity nem rede (o transporte vem de fora): conecta,
    /// autentica com o token, entrega as mensagens ao <see cref="RoteadorTempoReal"/> e tenta
    /// reconectar quando a conexão cai depois de autenticada. Usado pelo
    /// <see cref="ConexaoTempoReal"/>; o tempo avança em <see cref="Atualizar"/>.
    /// </summary>
    internal sealed class ClienteTempoReal
    {
        /// <summary>Quantas vezes tenta reconectar depois de uma queda.</summary>
        public const int MaximoTentativas = 3;

        /// <summary>Sem resposta à autenticação por este tempo, a conexão é dada como caída.</summary>
        public const float TempoLimiteAutenticacao = 10f;

        // Espera antes de cada tentativa de reconexão, em segundos.
        private static readonly float[] EsperasReconexao = { 1f, 2f, 4f };

        private readonly ITransporteTempoReal _transporte;
        private readonly RoteadorTempoReal _roteador;
        private readonly Action<string> _aoRecusarToken;

        private string _host;
        private int _porta;
        private string _chave;
        private string _token;
        private long _personagemId;
        private bool _jaAutenticou;
        private int _tentativas;
        private float _espera;
        private float _tempoAutenticando;

        /// <param name="transporte">Transporte UDP.</param>
        /// <param name="roteador">Quem entrega as mensagens recebidas.</param>
        /// <param name="aoRecusarToken">
        /// Chamado com o token enviado quando o servidor responde
        /// <see cref="CodigosErro.NaoAutenticado"/>.
        /// </param>
        public ClienteTempoReal(
            ITransporteTempoReal transporte,
            RoteadorTempoReal roteador,
            Action<string> aoRecusarToken = null
        )
        {
            _transporte = transporte ?? throw new ArgumentNullException(nameof(transporte));
            _roteador = roteador ?? throw new ArgumentNullException(nameof(roteador));
            _aoRecusarToken = aoRecusarToken;
            _transporte.Conectou += AoConectar;
            _transporte.Caiu += AoCair;
            _transporte.Recebeu += AoReceber;
        }

        public EstadoTempoReal Estado { get; private set; } = EstadoTempoReal.Desconectado;

        /// <summary>
        /// Por que a última conexão terminou sem ser pedida: <see cref="CodigosErroCliente.SemConexao"/>,
        /// <see cref="CodigosErro.NaoAutenticado"/> ou <see cref="CodigosErro.AcessoNegado"/>.
        /// Vazio se não houve erro.
        /// </summary>
        public string UltimoErro { get; private set; } = "";

        public event Action<EstadoTempoReal> EstadoMudou;

        /// <summary>
        /// Conecta e autentica com <paramref name="token"/>. Se já estiver conectado, desconecta
        /// antes. Sem token, não conecta e devolve <c>false</c>.
        /// </summary>
        public bool Conectar(string host, int porta, string chave, string token, long personagemId)
        {
            Desconectar();
            if (string.IsNullOrEmpty(token))
            {
                UltimoErro = CodigosErro.NaoAutenticado;
                return false;
            }

            _host = host;
            _porta = porta;
            _chave = chave;
            _token = token;
            _personagemId = personagemId;
            _jaAutenticou = false;
            _tentativas = 0;
            UltimoErro = "";
            AbrirConexao();
            return true;
        }

        /// <summary>Fecha a conexão sem tentar reconectar. Sem conexão, não faz nada.</summary>
        public void Desconectar()
        {
            if (Estado == EstadoTempoReal.Desconectado)
            {
                return;
            }

            _transporte.Desconectar();
            _token = null;
            UltimoErro = "";
            MudarEstado(EstadoTempoReal.Desconectado);
        }

        /// <summary>
        /// Envia uma mensagem ao servidor. Só envia com <see cref="EstadoTempoReal.Conectado"/>;
        /// fora dele, descarta e devolve <c>false</c>.
        /// </summary>
        public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega)
        {
            if (Estado != EstadoTempoReal.Conectado)
            {
                return false;
            }

            _transporte.Enviar(CodecTempoReal.Codificar(id, mensagem), entrega);
            return true;
        }

        /// <summary>Processa a rede e conta o tempo das esperas. Chamado a cada frame.</summary>
        public void Atualizar(float segundos)
        {
            _transporte.Processar();

            switch (Estado)
            {
                case EstadoTempoReal.Reconectando:
                    _espera -= segundos;
                    if (_espera <= 0f)
                    {
                        AbrirConexao();
                    }

                    break;
                case EstadoTempoReal.Autenticando:
                    _tempoAutenticando += segundos;
                    if (_tempoAutenticando >= TempoLimiteAutenticacao)
                    {
                        _transporte.Desconectar();
                        AoCair("o servidor não respondeu à autenticação");
                    }

                    break;
            }
        }

        private void AbrirConexao()
        {
            MudarEstado(EstadoTempoReal.Conectando);
            _transporte.Conectar(_host, _porta, _chave);
        }

        private void AoConectar()
        {
            if (Estado != EstadoTempoReal.Conectando)
            {
                return;
            }

            _tempoAutenticando = 0f;
            MudarEstado(EstadoTempoReal.Autenticando);
            var autenticar = new AutenticarTempoReal
            {
                Token = _token,
                PersonagemId = _personagemId,
            };
            _transporte.Enviar(
                CodecTempoReal.Codificar(TempoRealIds.AutenticarTempoReal, autenticar),
                EntregaTempoReal.Confiavel
            );
        }

        private void AoCair(string motivo)
        {
            if (Estado == EstadoTempoReal.Desconectado)
            {
                return;
            }

            Debug.LogWarning($"[TempoReal] Conexão caiu: {motivo}");

            // Sem nunca ter autenticado, o servidor não está lá: não insiste.
            if (!_jaAutenticou || _tentativas >= MaximoTentativas)
            {
                _token = null;
                UltimoErro = CodigosErroCliente.SemConexao;
                MudarEstado(EstadoTempoReal.Desconectado);
                return;
            }

            _espera = EsperasReconexao[Math.Min(_tentativas, EsperasReconexao.Length - 1)];
            _tentativas++;
            MudarEstado(EstadoTempoReal.Reconectando);
        }

        private void AoReceber(byte[] dados)
        {
            if (!CodecTempoReal.TentarDecodificar(dados, out var mensagem))
            {
                Debug.LogWarning("[TempoReal] Pacote inválido descartado.");
                return;
            }

            if (Estado == EstadoTempoReal.Autenticando)
            {
                if (mensagem.Id == TempoRealIds.ResultadoAutenticacaoTempoReal)
                {
                    AoAutenticar(mensagem);
                }

                return;
            }

            if (Estado == EstadoTempoReal.Conectado)
            {
                _roteador.Despachar(mensagem);
            }
        }

        private void AoAutenticar(MensagemTempoReal mensagem)
        {
            ResultadoAutenticacaoTempoReal resultado;
            try
            {
                resultado = mensagem.Ler<ResultadoAutenticacaoTempoReal>();
            }
            catch (ArgumentException)
            {
                resultado = null;
            }

            if (resultado != null && resultado.Aceita)
            {
                _jaAutenticou = true;
                _tentativas = 0;
                UltimoErro = "";
                MudarEstado(EstadoTempoReal.Conectado);
                return;
            }

            var codigo =
                resultado == null || string.IsNullOrEmpty(resultado.Codigo)
                    ? CodigosErro.NaoAutenticado
                    : resultado.Codigo;
            var token = _token;
            _transporte.Desconectar();
            _token = null;
            UltimoErro = codigo;
            MudarEstado(EstadoTempoReal.Desconectado);

            if (codigo == CodigosErro.NaoAutenticado)
            {
                _aoRecusarToken?.Invoke(token);
            }
        }

        private void MudarEstado(EstadoTempoReal estado)
        {
            if (Estado == estado)
            {
                return;
            }

            Estado = estado;
            try
            {
                EstadoMudou?.Invoke(estado);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
