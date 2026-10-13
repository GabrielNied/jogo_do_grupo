namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Pontos de atributo por atributo: os cinco da fatia jogável (seção 1). Em
    /// <see cref="DistribuirAtributosRequest"/> é quanto gastar agora; em
    /// <see cref="ProgressoResponse"/> é o total já distribuído pelo jogador (sem base,
    /// equipamentos nem efeitos, que são de Combate).
    /// </summary>
    [System.Serializable]
    public class PontosDeAtributos
    {
        public int Forca = 0;
        public int Agilidade = 0;
        public int Intelecto = 0;
        public int Vigor = 0;
        public int Espirito = 0;
    }
}
