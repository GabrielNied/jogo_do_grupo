using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Client.Core.Api
{
    /// <summary>Resultado de uma chamada do <see cref="ClienteApi"/> sem corpo de resposta.</summary>
    public class RespostaApi
    {
        /// <summary>Se o servidor respondeu com status 2xx (e, se esperado, um corpo válido).</summary>
        public bool Sucesso { get; internal set; }

        /// <summary>Status HTTP da resposta, ou 0 se não houve resposta (falha de rede).</summary>
        public long Status { get; internal set; }

        /// <summary>
        /// Erro quando <see cref="Sucesso"/> é <c>false</c>; <c>null</c> no sucesso.
        /// <see cref="ErroApi.Codigo"/> é um código do servidor (<see cref="CodigosErro"/> ou da
        /// funcionalidade) ou de <see cref="CodigosErroCliente"/>. Todo 401 em rota protegida
        /// chega como <see cref="CodigosErro.NaoAutenticado"/>.
        /// </summary>
        public ErroApi Erro { get; internal set; }

        /// <summary>Atalho para <c>Erro?.Codigo</c> (<c>null</c> no sucesso).</summary>
        public string CodigoErro => Erro?.Codigo;
    }

    /// <summary>Resultado de uma chamada do <see cref="ClienteApi"/> que devolve um corpo JSON.</summary>
    /// <typeparam name="T">Tipo do corpo (DTO <c>[Serializable]</c> do shared).</typeparam>
    public class RespostaApi<T> : RespostaApi
    {
        /// <summary>Corpo lido do JSON quando <see cref="RespostaApi.Sucesso"/> é <c>true</c>.</summary>
        public T Corpo { get; internal set; }
    }
}
