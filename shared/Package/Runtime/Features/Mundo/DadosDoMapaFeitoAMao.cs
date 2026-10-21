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

        /// <summary>
        /// Desliga os dados publicados até o escopo devolvido ser descartado, voltando ao valor
        /// anterior. Teste que liga o desvio sem desligar contamina os seguintes (#1174).
        /// </summary>
        public static System.IDisposable Desativar()
        {
            var anterior = DesativadoEmTestes;
            DesativadoEmTestes = true;
            return new EscopoDesativado(anterior);
        }

        private sealed class EscopoDesativado : System.IDisposable
        {
            private readonly bool _anterior;
            private bool _descartado;

            public EscopoDesativado(bool anterior) => _anterior = anterior;

            public void Dispose()
            {
                if (_descartado)
                    return;
                _descartado = true;
                DesativadoEmTestes = _anterior;
            }
        }

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
