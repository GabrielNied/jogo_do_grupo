namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Servidor avisa que o chefe da dungeon foi derrotado e quanto tempo resta para coletar o loot
    /// antes do retorno automático à cidade.
    /// </summary>
    [System.Serializable]
    public class ChefeDerrotadoNaDungeon
    {
        /// <summary>Identificador da corrida.</summary>
        public string IdInstancia = string.Empty;

        /// <summary>Quantos segundos os jogadores têm para coletar o loot antes de encerrar.</summary>
        public int SegundosParaColeta;
    }
}
