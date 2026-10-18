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

        /// <summary>Nível da corrida concluída.</summary>
        public int Nivel = 1;

        /// <summary>Tempo total decorrido da corrida em segundos.</summary>
        public int TempoTotalSegundos;

        /// <summary>Indica se a vitória foi alcançada dentro do tempo do cronômetro.</summary>
        public bool NoTempo;

        /// <summary>Próximo nível liberado para o jogador.</summary>
        public int ProximoNivelLiberado = 1;
    }

    /// <summary>Possíveis resultados de uma corrida de dungeon.</summary>
    public enum ResultadoDungeon
    {
        Vitoria,
        Derrota,
        InstanciaVazia,
    }
}
