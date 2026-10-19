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
        public PontoMarcadoDoMapa(
            string id,
            TipoDePontoMarcadoDoMapa tipo,
            float x,
            float z,
            float? y = null
        )
        {
            Id = id;
            Tipo = tipo;
            X = x;
            Z = z;
            Y = y;
        }

        public string Id { get; }
        public TipoDePontoMarcadoDoMapa Tipo { get; }
        public float X { get; }
        public float Z { get; }
        public float? Y { get; }
    }

    public static class NascimentoDoMapa
    {
        public static PontoMarcadoDoMapa Atual => Resolver(DadosDoMapaFeitoAMao.Pontos);

        public static PontoMarcadoDoMapa Resolver(
            System.Collections.Generic.IReadOnlyList<PontoMarcadoDoMapa> pontos
        )
        {
            foreach (var ponto in pontos)
            {
                if (ponto.Tipo == TipoDePontoMarcadoDoMapa.Nascimento && ponto.Id == "nascimento")
                    return SobreOChao(ponto);
            }
            return SobreOChao(
                new PontoMarcadoDoMapa("nascimento", TipoDePontoMarcadoDoMapa.Nascimento, 0f, -24f)
            );
        }

        private static PontoMarcadoDoMapa SobreOChao(PontoMarcadoDoMapa ponto) =>
            new PontoMarcadoDoMapa(
                ponto.Id,
                ponto.Tipo,
                ponto.X,
                ponto.Z,
                System.Math.Max(ponto.Y ?? float.MinValue, MapaDoMundo.AlturaEm(ponto.X, ponto.Z))
            );
    }
}
