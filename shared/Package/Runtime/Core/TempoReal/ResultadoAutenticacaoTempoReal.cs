using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Shared.Core.TempoReal
{
    /// <summary>
    /// Servidor responde à tentativa de autenticação UDP.
    /// Quando <see cref="Aceita"/> é <c>true</c>, <see cref="Codigo"/> fica vazio.
    /// Quando <see cref="Aceita"/> é <c>false</c>, <see cref="Codigo"/> é
    /// <see cref="CodigosErro.NaoAutenticado"/> (token inválido/expirado) ou
    /// <see cref="CodigosErro.AcessoNegado"/> (token válido, acesso negado ao personagem).
    /// Id: <see cref="TempoRealIds.ResultadoAutenticacaoTempoReal"/>.
    /// </summary>
    [System.Serializable]
    public class ResultadoAutenticacaoTempoReal
    {
        public bool Aceita = false;
        public string Codigo = "";
    }
}
