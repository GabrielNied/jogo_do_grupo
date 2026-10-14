namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasPersonagem.Personagens"/> (com token).
    /// Servidor responde 201 com o <see cref="PersonagemResumo"/> criado; 400 <c>NomeInvalido</c>
    /// (regras de <see cref="RegrasPersonagem"/>) ou <c>RacaInvalida</c>; 401 <c>NaoAutenticado</c>;
    /// 409 <c>NomeJaExiste</c> ou <c>LimitePersonagens</c>. Erros com corpo <c>ErroApi</c>; os
    /// códigos estão em <see cref="CodigosErroPersonagem"/>, exceto <c>NaoAutenticado</c> (Core).
    /// </summary>
    [System.Serializable]
    public class CriarPersonagemRequest
    {
        public string Nome = "";
        public Raca Raca;
        public AparenciaPersonagem Aparencia = new AparenciaPersonagem();
    }
}
