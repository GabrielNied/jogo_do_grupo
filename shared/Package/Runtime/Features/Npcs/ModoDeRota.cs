namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Como o NPC repete a rota (#1069).</summary>
    public enum ModoDeRota
    {
        /// <summary>Do último ponto volta direto ao primeiro.</summary>
        Ciclo = 0,

        /// <summary>Vai até o último ponto e volta pelo mesmo caminho.</summary>
        VaiEVolta = 1,
    }
}
