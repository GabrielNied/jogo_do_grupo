namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasPersonagem.Atributos"/> (com token) para
    /// gastar pontos livres nos atributos. O pedido vale inteiro ou não vale: se for recusado,
    /// nenhum ponto é gasto. Gastar não tem volta na fatia.
    /// Servidor responde 200 <see cref="ProgressoResponse"/>; 400
    /// <see cref="CodigosErroPersonagem.AtributoInvalido"/>; 409
    /// <see cref="CodigosErroPersonagem.PontosInsuficientes"/>; 404
    /// <see cref="CodigosErroPersonagem.PersonagemNaoEncontrado"/>; 401 do Core.
    /// O client pode conferir antes com <see cref="RegrasAtributos.ValidarDistribuicao"/>.
    /// </summary>
    [System.Serializable]
    public class DistribuirAtributosRequest
    {
        /// <summary>Quantos pontos gastar em cada atributo (zero nos que não mudam).</summary>
        public PontosDeAtributos Pontos = new PontosDeAtributos();
    }
}
