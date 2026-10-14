using System;
using System.Text;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Shared.Core.Api;
using UnityEngine;
using UnityEngine.Networking;

namespace JogoDoGrupo.Client.Core.Api
{
    /// <summary>
    /// Chamadas à API HTTP do servidor (JSON com <c>JsonUtility</c>), para conta e personagens
    /// (ADR 0009). As funcionalidades usam este cliente em vez de falar com a rede direto.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Os métodos nunca lançam exceção por erro de rede ou HTTP: o resultado vem em
    /// <see cref="RespostaApi"/> (sucesso, status, corpo ou <see cref="ErroApi"/>).
    /// Resposta de erro sem corpo ou com JSON inválido vira
    /// <see cref="CodigosErroCliente.RespostaInvalida"/>; falha de rede vira
    /// <see cref="CodigosErroCliente.SemConexao"/>; HTTP bloqueado pelo Unity vira
    /// <see cref="CodigosErroCliente.HttpBloqueado"/>; sair do Play no meio vira
    /// <see cref="CodigosErroCliente.Cancelada"/>. Só um <c>corpo</c> que o
    /// <c>JsonUtility</c> não consegue serializar (erro de programação) lança exceção.
    /// </para>
    /// <para>
    /// Rota protegida (<c>autenticar = true</c>, o padrão): envia
    /// <c>Authorization: Bearer &lt;token&gt;</c> se houver <see cref="Sessao"/>. Qualquer 401
    /// nela vira <see cref="CodigosErro.NaoAutenticado"/>. Se um token foi enviado e ele ainda
    /// for o da sessão atual, a sessão é encerrada e o Core volta para o login
    /// (<see cref="Sessao.Expirou"/>) antes de o <c>await</c> retornar. Por isso, depois
    /// do <c>await</c>, confira <c>Tela.Aberta</c> antes de mexer na interface.
    /// </para>
    /// <para>Rotas de conta (registro e login) não são protegidas: passe <c>autenticar: false</c>.</para>
    /// <code>
    /// var resposta = await ClienteApi.Get&lt;ListaPersonagensResponse&gt;(RotasPersonagem.Listar);
    /// if (!Aberta) return;
    /// if (resposta.Sucesso) Mostrar(resposta.Corpo);
    /// else MostrarErro(resposta.CodigoErro);
    /// </code>
    /// </remarks>
    public static class ClienteApi
    {
        /// <summary>URL do servidor local de desenvolvimento.</summary>
        public const string UrlBasePadrao = "http://localhost:5080";

        /// <summary>Tempo limite padrão de cada chamada, em segundos.</summary>
        public const int TempoLimitePadraoSegundos = 10;

        private static string _urlBase = UrlBasePadrao;

        /// <summary>
        /// Endereço do servidor, sem barra no fim (ex.: <c>http://localhost:5080</c>).
        /// Valor vazio volta para <see cref="UrlBasePadrao"/>.
        /// </summary>
        public static string UrlBase
        {
            get => _urlBase;
            set =>
                _urlBase = string.IsNullOrWhiteSpace(value)
                    ? UrlBasePadrao
                    : value.Trim().TrimEnd('/');
        }

        /// <summary>Tempo limite de cada chamada, em segundos. Esgotado, vira <see cref="CodigosErroCliente.SemConexao"/>.</summary>
        public static int TempoLimiteSegundos { get; set; } = TempoLimitePadraoSegundos;

        /// <summary><c>GET</c> que devolve um corpo JSON do tipo <typeparamref name="T"/>.</summary>
        /// <param name="rota">Rota do shared, começando com "/" (ex.: <c>RotasContas.Login</c>).</param>
        /// <param name="autenticar">Se a rota é protegida (envia o token e trata 401).</param>
        public static async Awaitable<RespostaApi<T>> Get<T>(string rota, bool autenticar = true)
        {
            var resposta = new RespostaApi<T>();
            await Executar(
                UnityWebRequest.kHttpVerbGET,
                rota,
                null,
                autenticar,
                resposta,
                requisicao => LerCorpo(resposta, requisicao)
            );
            return resposta;
        }

        /// <summary><c>POST</c> com corpo JSON que devolve um corpo JSON do tipo <typeparamref name="T"/>.</summary>
        /// <param name="rota">Rota do shared, começando com "/".</param>
        /// <param name="corpo">DTO <c>[Serializable]</c> enviado como JSON, ou <c>null</c> para sem corpo.</param>
        /// <param name="autenticar">Se a rota é protegida (envia o token e trata 401).</param>
        public static async Awaitable<RespostaApi<T>> Post<T>(
            string rota,
            object corpo,
            bool autenticar = true
        )
        {
            var resposta = new RespostaApi<T>();
            await Executar(
                UnityWebRequest.kHttpVerbPOST,
                rota,
                corpo,
                autenticar,
                resposta,
                requisicao => LerCorpo(resposta, requisicao)
            );
            return resposta;
        }

        /// <summary><c>POST</c> com corpo JSON cuja resposta de sucesso não tem corpo (ex.: 201).</summary>
        /// <param name="rota">Rota do shared, começando com "/".</param>
        /// <param name="corpo">DTO <c>[Serializable]</c> enviado como JSON, ou <c>null</c> para sem corpo.</param>
        /// <param name="autenticar">Se a rota é protegida (envia o token e trata 401).</param>
        public static async Awaitable<RespostaApi> Post(
            string rota,
            object corpo,
            bool autenticar = true
        )
        {
            var resposta = new RespostaApi();
            await Executar(UnityWebRequest.kHttpVerbPOST, rota, corpo, autenticar, resposta, null);
            return resposta;
        }

        /// <summary>
        /// Envia a requisição e preenche <paramref name="resposta"/>. <paramref name="lerCorpo"/>
        /// (opcional) roda só no sucesso, antes do descarte da requisição.
        /// </summary>
        private static async Awaitable Executar(
            string metodo,
            string rota,
            object corpo,
            bool autenticar,
            RespostaApi resposta,
            Action<UnityWebRequest> lerCorpo
        )
        {
            // Monta o JSON antes de criar a requisição: se lançar, não sobra nada sem descarte.
            var json = corpo != null ? Encoding.UTF8.GetBytes(JsonUtility.ToJson(corpo)) : null;

            // Guarda o token enviado: um 401 só encerra a sessão se ela ainda for a mesma.
            var tokenEnviado = autenticar ? Sessao.Token : null;
            var url = UrlBase + rota;

            UnityWebRequest requisicao;
            UnityWebRequestAsyncOperation operacao;
            try
            {
                requisicao = CriarRequisicao(metodo, url, json, tokenEnviado);
            }
            catch (Exception e)
            {
                FalharAntesDeEnviar(resposta, metodo, url, e);
                return;
            }

            using (requisicao)
            {
                try
                {
                    operacao = requisicao.SendWebRequest();
                }
                catch (Exception e)
                {
                    FalharAntesDeEnviar(resposta, metodo, url, e);
                    return;
                }

                try
                {
                    while (!operacao.isDone)
                    {
                        await Awaitable.NextFrameAsync(Application.exitCancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    requisicao.Abort();
                    resposta.Erro = CriarErro(
                        CodigosErroCliente.Cancelada,
                        "O jogo foi fechado durante a chamada."
                    );
                    return;
                }

                PreencherStatus(resposta, requisicao, autenticar, tokenEnviado);
                if (resposta.Sucesso)
                {
                    lerCorpo?.Invoke(requisicao);
                }
            }
        }

        private static UnityWebRequest CriarRequisicao(
            string metodo,
            string url,
            byte[] json,
            string token
        )
        {
            var requisicao = new UnityWebRequest(url, metodo);
            try
            {
                requisicao.downloadHandler = new DownloadHandlerBuffer();
                requisicao.timeout = TempoLimiteSegundos;

                if (json != null)
                {
                    requisicao.uploadHandler = new UploadHandlerRaw(json);
                    requisicao.SetRequestHeader("Content-Type", "application/json");
                }

                requisicao.SetRequestHeader("Accept", "application/json");

                // Quem decide se o token vale é o servidor; o client só manda o que tem.
                if (!string.IsNullOrEmpty(token))
                {
                    requisicao.SetRequestHeader("Authorization", "Bearer " + token);
                }

                return requisicao;
            }
            catch
            {
                requisicao.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Converte em erro uma exceção ao criar ou enviar a requisição (ex.: URL inválida ou
        /// HTTP inseguro bloqueado pelo Unity, que lança <c>InvalidOperationException</c>).
        /// </summary>
        private static void FalharAntesDeEnviar(
            RespostaApi resposta,
            string metodo,
            string url,
            Exception e
        )
        {
            resposta.Status = 0;

            resposta.Erro = CriarErro(CodigoErroAntesDeEnviar(url, e), e.Message);
            if (resposta.CodigoErro == CodigosErroCliente.HttpBloqueado)
            {
                Debug.LogError(
                    $"[ClienteApi] {metodo} {url}: o Unity bloqueou HTTP sem criptografia ({e.Message}). "
                        + "Libere em Project Settings > Player > Other Settings > "
                        + "Allow downloads over HTTP (\"Allowed in development builds\")."
                );
                return;
            }

            Debug.LogWarning(
                $"[ClienteApi] {metodo} {url}: não foi possível enviar ({e.GetType().Name}: {e.Message})."
            );
        }

        internal static string CodigoErroAntesDeEnviar(string url, Exception e)
        {
            if (
                e is InvalidOperationException
                && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && e.Message != null
                && e.Message.IndexOf("Insecure connection", StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                return CodigosErroCliente.HttpBloqueado;
            }

            return CodigosErroCliente.SemConexao;
        }

        private static void PreencherStatus(
            RespostaApi resposta,
            UnityWebRequest requisicao,
            bool autenticar,
            string tokenEnviado
        )
        {
            var status = requisicao.responseCode;
            var codigoFalha = CodigoErroDeResposta(requisicao.result, status, autenticar);

            if (codigoFalha == CodigosErroCliente.SemConexao)
            {
                resposta.Status = 0;
                resposta.Erro = CriarErro(CodigosErroCliente.SemConexao, requisicao.error);
                Debug.LogWarning(
                    $"[ClienteApi] {requisicao.method} {requisicao.url}: sem conexão ({requisicao.error})."
                );
                return;
            }

            resposta.Status = status;
            if (requisicao.result == UnityWebRequest.Result.Success)
            {
                resposta.Sucesso = true;
                return;
            }

            if (codigoFalha == CodigosErro.NaoAutenticado)
            {
                resposta.Erro = CriarErro(
                    CodigosErro.NaoAutenticado,
                    "Token ausente, inválido ou expirado."
                );
                Debug.LogWarning(
                    $"[ClienteApi] {requisicao.method} {requisicao.url}: 401, não autenticado."
                );
                Sessao.ExpirarSeAtual(tokenEnviado);
                return;
            }

            resposta.Erro = LerErro(requisicao.downloadHandler?.text);
            Debug.LogWarning(
                $"[ClienteApi] {requisicao.method} {requisicao.url}: {status} {resposta.Erro.Codigo} ({resposta.Erro.Mensagem})."
            );
        }

        internal static string CodigoErroDeResposta(
            UnityWebRequest.Result resultado,
            long status,
            bool autenticar
        )
        {
            if (resultado == UnityWebRequest.Result.ConnectionError || status == 0)
            {
                return CodigosErroCliente.SemConexao;
            }

            return autenticar && status == 401 ? CodigosErro.NaoAutenticado : null;
        }

        internal static ErroApi LerErro(string texto)
        {
            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    var erro = JsonUtility.FromJson<ErroApi>(texto);
                    if (erro != null && !string.IsNullOrEmpty(erro.Codigo))
                    {
                        return erro;
                    }
                }
                catch (Exception)
                {
                    // Corpo que não é JSON: cai no erro genérico abaixo.
                }
            }

            return CriarErro(
                CodigosErroCliente.RespostaInvalida,
                "Resposta de erro sem ErroApi válido."
            );
        }

        private static void LerCorpo<T>(RespostaApi<T> resposta, UnityWebRequest requisicao)
        {
            var texto = requisicao.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    resposta.Corpo = JsonUtility.FromJson<T>(texto);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[ClienteApi] {requisicao.url}: corpo inválido ({e.Message})."
                    );
                }
            }

            if (resposta.Corpo == null)
            {
                resposta.Sucesso = false;
                resposta.Erro = CriarErro(
                    CodigosErroCliente.RespostaInvalida,
                    "Resposta de sucesso sem o corpo esperado."
                );
            }
        }

        private static ErroApi CriarErro(string codigo, string mensagem)
        {
            return new ErroApi { Codigo = codigo, Mensagem = mensagem ?? "" };
        }
    }
}
