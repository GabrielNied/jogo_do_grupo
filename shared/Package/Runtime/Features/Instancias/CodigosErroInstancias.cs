namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Códigos de recusa de Instancias que o servidor coloca em
    /// <see cref="EntradaRecusada.Codigo"/>.
    /// </summary>
    public static class CodigosErroInstancias
    {
        /// <summary>Um membro que não lidera tentou iniciar a corrida.</summary>
        public const string NaoELider = "NaoELider";

        /// <summary>O solicitante ou seu grupo já pertence a outra corrida ativa.</summary>
        public const string GrupoEmOutraDungeon = "GrupoEmOutraDungeon";

        /// <summary>O personagem não está numa posição válida para entrar.</summary>
        public const string LongeDemais = "LongeDemais";

        /// <summary>O nível requisitado ainda não foi desbloqueado pelo líder.</summary>
        public const string NivelNaoDesbloqueado = "NivelNaoDesbloqueado";
    }
}
