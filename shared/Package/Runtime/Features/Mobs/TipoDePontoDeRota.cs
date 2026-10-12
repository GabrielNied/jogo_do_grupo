namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>Para que serve um ponto da rota de uma manada (docs/features/mobs.md, "Manadas").</summary>
    public enum TipoDePontoDeRota
    {
        /// <summary>Onde a manada pasta.</summary>
        Pasto = 0,

        /// <summary>Onde a manada bebe.</summary>
        Rio = 1,

        /// <summary>Onde a manada descansa.</summary>
        Repouso = 2,

        /// <summary>Volta do circuito, antes de recomeçar pelo pasto.</summary>
        Retorno = 3,
    }
}
