namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Dados de um personagem para a tela de seleção. Servidor envia dentro de
    /// <see cref="ListaPersonagensResponse"/> e como resposta (201) de
    /// <see cref="CriarPersonagemRequest"/>.
    /// </summary>
    [System.Serializable]
    public class PersonagemResumo
    {
        /// <summary>Id gerado pelo servidor.</summary>
        public long Id;

        /// <summary>Nome com as maiúsculas/minúsculas que o jogador escolheu.</summary>
        public string Nome = "";

        public Raca Raca;

        /// <summary>Nível atual. Personagem recém-criado tem nível 1.</summary>
        public int Nivel;

        /// <summary>Aparência básica customizada do personagem.</summary>
        public AparenciaPersonagem Aparencia = new AparenciaPersonagem();
    }
}
