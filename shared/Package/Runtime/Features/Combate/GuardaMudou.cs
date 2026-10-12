namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área (inclusive o dono) que a guarda de um personagem subiu
    /// ou desceu. <see cref="Segurando"/> falso com <see cref="Quebrou"/> verdadeiro: o golpe
    /// custou mais vigor do que havia. Também é a resposta ao dono quando a guarda é recusada
    /// (<see cref="Segurando"/> falso). Id: <see cref="CombateMessageIds.GuardaMudou"/>.
    /// </summary>
    [System.Serializable]
    public class GuardaMudou
    {
        public long PersonagemId = 0;
        public bool Segurando = false;
        public bool Quebrou = false;
    }
}
