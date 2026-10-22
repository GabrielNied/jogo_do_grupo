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

        // O nascimento fica sempre na altura do chão (a mesma que o servidor usa para validar o
        // movimento). O Y do marcador não vale: um marcador acima do relevo (28,13 sobre um
        // chão de 25,18) fazia o personagem nascer no ar e cair 3 m (#1186).
        private static PontoMarcadoDoMapa SobreOChao(PontoMarcadoDoMapa ponto) =>
            new PontoMarcadoDoMapa(
                ponto.Id,
                ponto.Tipo,
                ponto.X,
                ponto.Z,
                MapaDoMundo.AlturaEm(ponto.X, ponto.Z)
            );
    }
}
