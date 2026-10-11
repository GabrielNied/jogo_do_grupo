using System;
using JogoDoGrupo.Shared.Core.Contas;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Contas
{
    /// <summary>
    /// Sessão da conta logada: token JWT e expiração, só em memória (some ao fechar o jogo).
    /// O <see cref="Api.ClienteApi"/> envia o token nas rotas protegidas.
    /// </summary>
    public static class Sessao
    {
        /// <summary>Token JWT da conta, ou <c>null</c> sem sessão.</summary>
        public static string Token { get; private set; }

        /// <summary>Quando o token expira (UTC). Sem sessão, <see cref="DateTimeOffset.MinValue"/>.</summary>
        public static DateTimeOffset ExpiraEm { get; private set; } = DateTimeOffset.MinValue;

        /// <summary>Se há token e ele ainda não expirou pelo relógio local.</summary>
        public static bool Ativa =>
            !string.IsNullOrEmpty(Token) && DateTimeOffset.UtcNow < ExpiraEm;

        /// <summary>
        /// Disparado quando o servidor recusa o token (401 em rota protegida). O Core já trata:
        /// encerra a sessão e volta para o login com um aviso. As telas só precisam parar o que
        /// estavam fazendo (veja <see cref="UI.Tela.Aberta"/>).
        /// </summary>
        public static event Action Expirou;

        /// <summary>Guarda o token recebido no login.</summary>
        public static void Iniciar(LoginResponse resposta)
        {
            if (resposta == null)
            {
                throw new ArgumentNullException(nameof(resposta));
            }

            Token = resposta.Token;
            ExpiraEm = DateTimeOffset.FromUnixTimeSeconds(resposta.ExpiraEmUnix);
        }

        /// <summary>Apaga o token (ex.: "Sair"). Não dispara <see cref="Expirou"/>.</summary>
        public static void Encerrar()
        {
            Token = null;
            ExpiraEm = DateTimeOffset.MinValue;
        }

        /// <summary>Chamado pelo <see cref="Api.ClienteApi"/> ao receber 401 em rota protegida.</summary>
        internal static void Expirar()
        {
            Encerrar();
            Expirou?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Encerrar();
            Expirou = null;
        }
    }
}
