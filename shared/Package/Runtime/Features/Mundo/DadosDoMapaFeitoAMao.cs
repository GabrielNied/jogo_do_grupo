namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Dados gerados pelo comando do Unity. Sem uma publicação, os vetores vazios mantêm o mundo
    /// procedural como fonte temporária.
    /// </summary>
    public static class DadosDoMapaFeitoAMao
    {
        public static readonly short[] AlturasEmCentimetros =
            DadosPublicadosDoMapa.AlturasEmCentimetros;
        public static readonly byte[] Biomas = DadosPublicadosDoMapa.Biomas;
        public static readonly byte[] Areas = DadosPublicadosDoMapa.Areas;
        public static readonly PontoMarcadoDoMapa[] Pontos = DadosPublicadosDoMapa.Pontos;

        /// <summary>
        /// Quando verdadeiro, desativa temporariamente os dados publicados (usado em testes das fórmulas procedurais).
        /// </summary>
        public static bool DesativadoEmTestes { get; set; }

        public static bool Disponivel =>
            !DesativadoEmTestes
            && AlturasEmCentimetros.Length == MapaDoMundo.PontosPorLado * MapaDoMundo.PontosPorLado
            && Biomas.Length == AlturasEmCentimetros.Length
            && Areas.Length == AlturasEmCentimetros.Length;

        public static bool TentarAmostrar(
            int i,
            int j,
            out float altura,
            out Bioma bioma,
            out AreaEspecialDoMapa area
        )
        {
            altura = 0f;
            bioma = Bioma.Campo;
            area = AreaEspecialDoMapa.Nenhuma;
            if (!Disponivel)
            {
                return false;
            }

            var x = Limitar(i, 0, MapaDoMundo.CelulasPorLado);
            var z = Limitar(j, 0, MapaDoMundo.CelulasPorLado);
            var indice = z * MapaDoMundo.PontosPorLado + x;
            altura = AlturasEmCentimetros[indice] / 100f;
            bioma = (Bioma)Biomas[indice];
            area = (AreaEspecialDoMapa)Areas[indice];
            return true;
        }

        private static int Limitar(int valor, int minimo, int maximo)
        {
            return valor < minimo ? minimo
                : valor > maximo ? maximo
                : valor;
        }
    }
}
