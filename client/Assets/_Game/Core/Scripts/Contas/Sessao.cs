using System;
using JogoDoGrupo.Shared.Core.Contas;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Contas
{
    /// <summary>
    /// Sessão da conta logada: token JWT e expiração. Fica em memória e também gravada neste
    /// computador, para o próximo Play entrar direto até a pessoa sair da conta.
    /// Não grava a senha. O <see cref="Api.ClienteApi"/> envia o token nas rotas protegidas.
    /// </summary>
    public static class Sessao
    {
        private const string ChaveToken = "jogodogrupo.sessao.token";
        private const string ChaveExpira = "jogodogrupo.sessao.expira";

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

        /// <summary>
        /// Disparado sempre que a sessão é encerrada (<see cref="Encerrar"/>, inclusive na
        /// expiração). O Core usa para fechar a conexão de tempo real.
        /// </summary>
        internal static event Action Encerrada;

        /// <summary>Guarda o token recebido no login.</summary>
        public static void Iniciar(LoginResponse resposta)
        {
            if (resposta == null)
            {
                throw new ArgumentNullException(nameof(resposta));
            }

            Token = resposta.Token;
            ExpiraEm = DateTimeOffset.FromUnixTimeSeconds(resposta.ExpiraEmUnix);
            Gravar();
        }

        /// <summary>
        /// Traz de volta o token gravado, se ainda não expirou. Chamado na abertura do jogo.
        /// Token vencido é apagado.
        /// </summary>
        public static void Restaurar()
        {
            if (Ativa)
            {
                return;
            }

            var token = PlayerPrefs.GetString(ChaveToken, "");
            var textoExpira = PlayerPrefs.GetString(ChaveExpira, "");
            if (string.IsNullOrEmpty(token) || !long.TryParse(textoExpira, out var unix))
            {
                return;
            }

            Token = token;
            ExpiraEm = DateTimeOffset.FromUnixTimeSeconds(unix);
            if (!Ativa)
            {
                Encerrar();
            }
        }

        /// <summary>Apaga o token da memória e do computador (ex.: "Sair"). Não dispara <see cref="Expirou"/>.</summary>
        public static void Encerrar()
        {
            EsquecerMemoria();
            PlayerPrefs.DeleteKey(ChaveToken);
            PlayerPrefs.DeleteKey(ChaveExpira);
            PlayerPrefs.Save();
            Encerrada?.Invoke();
        }

        /// <summary>Limpa só a memória. O login gravado continua, para o próximo Play.</summary>
        internal static void EsquecerMemoria()
        {
            Token = null;
            ExpiraEm = DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Chamado pelo <see cref="Api.ClienteApi"/> ao receber 401 em rota protegida.
        /// Só encerra a sessão e dispara <see cref="Expirou"/> se <paramref name="tokenEnviado"/>
        /// ainda for o <see cref="Token"/> atual: um 401 repetido ou atrasado (de uma sessão
        /// anterior) não derruba a sessão nova nem dispara a expiração de novo.
        /// </summary>
        internal static void ExpirarSeAtual(string tokenEnviado)
        {
            // Sem token enviado não havia sessão para expirar.
            if (
                string.IsNullOrEmpty(tokenEnviado)
                || !string.Equals(tokenEnviado, Token, StringComparison.Ordinal)
            )
            {
                return;
            }

            Encerrar();
            Expirou?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            EsquecerMemoria();
            Expirou = null;
            Encerrada = null;
        }

        private static void Gravar()
        {
            PlayerPrefs.SetString(ChaveToken, Token ?? "");
            PlayerPrefs.SetString(ChaveExpira, ExpiraEm.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();
        }
    }
}
