namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>Servidor informa aos participantes que a corrida terminou.</summary>
    [System.Serializable]
    public class DungeonEncerrada
    {
        /// <summary>Resultado da corrida.</summary>
        public ResultadoDungeon Resultado;

        /// <summary>Identificador da corrida encerrada.</summary>
        public string IdInstancia = string.Empty;
    }

    /// <summary>Possíveis resultados de uma corrida de dungeon.</summary>
    public enum ResultadoDungeon
    {
        Vitoria,
        Derrota,
        InstanciaVazia,
    }
}
