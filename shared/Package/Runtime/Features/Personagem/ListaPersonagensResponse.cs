namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Servidor responde (200) a <c>GET</c> <see cref="RotasPersonagem.Personagens"/> com os
    /// personagens da conta do token, em ordem de criação (Id crescente). Conta sem
    /// personagens recebe array vazio, nunca <c>null</c>. Sem token válido: 401 <c>NaoAutenticado</c>.
    /// </summary>
    [System.Serializable]
    public class ListaPersonagensResponse
    {
        public PersonagemResumo[] Personagens = System.Array.Empty<PersonagemResumo>();
    }
}
