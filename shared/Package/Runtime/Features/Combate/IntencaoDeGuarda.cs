namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Client informa ao servidor que a guarda está segurada (<see cref="Segurando"/> verdadeiro)
    /// ou solta. É só a intenção: o servidor recusa no preparo e no momento de acerto de um
    /// golpe, durante a esquiva, a reação ao acerto e a queda, e responde com
    /// <see cref="GuardaMudou"/>. Id: <see cref="CombateMessageIds.IntencaoDeGuarda"/>.
    /// </summary>
    [System.Serializable]
    public class IntencaoDeGuarda
    {
        public bool Segurando = false;
    }
}
