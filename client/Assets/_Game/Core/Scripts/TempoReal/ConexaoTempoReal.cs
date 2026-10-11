using System;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Shared.Core;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Canal de tempo real com o servidor (UDP com a LiteNetLib, ADR 0015). As funcionalidades
    /// usam esta classe em vez de falar com a rede direto: conectam com o personagem escolhido,
    /// enviam os DTOs do shared pelo Id e assinam os Ids (ou a faixa inteira) que querem receber.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ao conectar, o Core autentica com o token da <see cref="Sessao"/> (mensagens
    /// <c>AutenticarTempoReal</c> e <c>ResultadoAutenticacaoTempoReal</c>). Só depois de
    /// <see cref="EstadoTempoReal.Conectado"/> as mensagens vão e vêm. Se a conexão cair, o Core
    /// tenta reconectar sozinho (<see cref="EstadoTempoReal.Reconectando"/>) e, ao voltar, dispara
    /// <see cref="EstadoMudou"/> com <see cref="EstadoTempoReal.Conectado"/> de novo: reenvie ali o
    /// que o servidor precisa saber (ex.: entrar na cidade). Token recusado encerra a sessão e
    /// volta para o login, como na API HTTP.
    /// </para>
    /// <para>
    /// A conexão fecha sozinha ao sair do mundo, ao sair da conta e ao fechar o jogo. As
    /// assinaturas continuam valendo entre conexões; descarte o retorno de <see cref="Assinar{T}"/>
    /// para cancelar. Tudo roda na thread principal.
    /// </para>
    /// <code>
    /// ConexaoTempoReal.Assinar&lt;JogadorEntrou&gt;(MundoMessageIds.JogadorEntrou, Mostrar);
    /// ConexaoTempoReal.EstadoMudou += estado =>
    /// {
    ///     if (estado == EstadoTempoReal.Conectado)
    ///         ConexaoTempoReal.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
    /// };
    /// ConexaoTempoReal.Conectar(personagemId);
    /// </code>
    /// </remarks>
    public static class ConexaoTempoReal
    {
        /// <summary>Endereço do servidor local de desenvolvimento.</summary>
        public const string HostPadrao = "localhost";

        /// <summary>Porta UDP padrão do servidor de tempo real.</summary>
        public const int PortaPadrao = 5081;

        /// <summary>Chave que o servidor confere ao aceitar a conexão UDP (não é segredo; quem autentica é o token).</summary>
        public const string ChaveConexao = "JogoDoGrupo";

        private static readonly RoteadorTempoReal _roteador = new RoteadorTempoReal();
        private static ClienteTempoReal _cliente;
        private static ExecutorTempoReal _executor;

        /// <summary>Endereço do servidor. Vazio volta para <see cref="HostPadrao"/>.</summary>
        public static string Host
        {
            get => _host;
            set => _host = string.IsNullOrWhiteSpace(value) ? HostPadrao : value.Trim();
        }

        private static string _host = HostPadrao;

        /// <summary>Porta UDP do servidor.</summary>
        public static int Porta { get; set; } = PortaPadrao;

        public static EstadoTempoReal Estado => _cliente?.Estado ?? EstadoTempoReal.Desconectado;

        /// <summary>Se está autenticado e pronto para enviar e receber.</summary>
        public static bool Conectado => Estado == EstadoTempoReal.Conectado;

        /// <summary>
        /// Por que a última conexão terminou sem ser pedida:
        /// <see cref="Api.CodigosErroCliente.SemConexao"/>,
        /// <c>CodigosErro.NaoAutenticado</c> ou <c>CodigosErro.AcessoNegado</c>. Vazio sem erro.
        /// </summary>
        public static string UltimoErro => _cliente?.UltimoErro ?? "";

        /// <summary>Disparado a cada mudança de <see cref="Estado"/>.</summary>
        public static event Action<EstadoTempoReal> EstadoMudou;

        /// <summary>
        /// Conecta ao servidor e autentica o <paramref name="personagemId"/> com o token da
        /// <see cref="Sessao"/>. Se já estiver conectado, reconecta. Sem sessão, devolve
        /// <c>false</c> e não conecta.
        /// </summary>
        public static bool Conectar(long personagemId)
        {
            GarantirCliente();
            return _cliente.Conectar(Host, Porta, ChaveConexao, Sessao.Token, personagemId);
        }

        /// <summary>Fecha a conexão sem tentar reconectar.</summary>
        public static void Desconectar()
        {
            _cliente?.Desconectar();
        }

        /// <summary>
        /// Envia um DTO do shared com o <paramref name="id"/> da faixa da funcionalidade. Fora de
        /// <see cref="EstadoTempoReal.Conectado"/>, descarta e devolve <c>false</c>.
        /// </summary>
        public static bool Enviar<T>(
            ushort id,
            T mensagem,
            EntregaTempoReal entrega = EntregaTempoReal.Confiavel
        ) => _cliente != null && _cliente.Enviar(id, mensagem, entrega);

        /// <summary>
        /// Assina as mensagens de um Id, já convertidas no DTO <typeparamref name="T"/>.
        /// Descarte o retorno para cancelar.
        /// </summary>
        public static IDisposable Assinar<T>(ushort id, Action<T> aoReceber) =>
            _roteador.Assinar(id, aoReceber);

        /// <summary>
        /// Assina todos os Ids de uma faixa (ex.: <c>MundoMessageIds.Faixa</c>); leia cada uma com
        /// <see cref="MensagemTempoReal.Ler{T}"/> conforme o <see cref="MensagemTempoReal.Id"/>.
        /// </summary>
        public static IDisposable AssinarFaixa(
            MessageIdRange faixa,
            Action<MensagemTempoReal> aoReceber
        ) => _roteador.AssinarFaixa(faixa, aoReceber);

        private static void GarantirCliente()
        {
            if (_cliente == null)
            {
                _cliente = new ClienteTempoReal(
                    new TransporteLiteNetLib(),
                    _roteador,
                    Sessao.ExpirarSeAtual
                );
                _cliente.EstadoMudou += estado => EstadoMudou?.Invoke(estado);
            }

            // Sair da conta fecha a conexão. Tira antes para não assinar duas vezes.
            Sessao.Encerrada -= Desconectar;
            Sessao.Encerrada += Desconectar;

            if (_executor == null)
            {
                // Fica na cena DontDestroyOnLoad, que some ao sair do Play no editor.
                var objeto = new GameObject("JogoDoGrupo.TempoReal");
                UnityEngine.Object.DontDestroyOnLoad(objeto);
                _executor = objeto.AddComponent<ExecutorTempoReal>();
            }
        }

        internal static void Atualizar(float segundos) => _cliente?.Atualizar(segundos);

        internal static void AoFecharExecutor(ExecutorTempoReal executor)
        {
            if (_executor != executor)
            {
                return;
            }

            _executor = null;
            Desconectar();
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções; limpa antes de qualquer registro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _cliente?.Desconectar();
            _cliente = null;
            _executor = null;
            _roteador.Limpar();
            EstadoMudou = null;
            _host = HostPadrao;
            Porta = PortaPadrao;
        }
    }
}
