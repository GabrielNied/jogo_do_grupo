namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Tipo de ponto colocado manualmente na cena de autoria do mundo.</summary>
    public enum TipoDePontoMarcadoDoMapa : byte
    {
        Nascimento,
        EntradaDeDungeon,
        Retorno,
        Coleta,
        Inimigos,
    }

    /// <summary>Um ponto de jogabilidade exportado pelo Unity para cliente e servidor.</summary>
    public readonly struct PontoMarcadoDoMapa
    {
        public PontoMarcadoDoMapa(string id, TipoDePontoMarcadoDoMapa tipo, float x, float z)
        {
            Id = id;
            Tipo = tipo;
            X = x;
            Z = z;
        }

        public string Id { get; }
        public TipoDePontoMarcadoDoMapa Tipo { get; }
        public float X { get; }
        public float Z { get; }
    }
}
