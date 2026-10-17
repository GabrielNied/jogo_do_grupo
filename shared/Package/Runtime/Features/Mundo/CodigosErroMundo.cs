namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Códigos de erro de operações do mundo persistente e seus canais.</summary>
    public static class CodigosErroMundo
    {
        /// <summary>Ação recusada pois o personagem está em combate.</summary>
        public const string EmCombate = "EmCombate";

        /// <summary>O canal solicitado atingiu a capacidade máxima de jogadores.</summary>
        public const string CanalLotado = "CanalLotado";

        /// <summary>O canal solicitado não existe ou está fora da faixa válida.</summary>
        public const string CanalInvalido = "CanalInvalido";

        /// <summary>O personagem já se encontra no canal solicitado.</summary>
        public const string MesmoCanal = "MesmoCanal";
    }
}
