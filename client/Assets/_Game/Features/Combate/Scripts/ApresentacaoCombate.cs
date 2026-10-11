using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>Uma barra de recurso pronta para mostrar: o texto e o quanto está cheia (0 a 1).</summary>
    internal readonly struct BarraRecurso
    {
        public BarraRecurso(string rotulo, int atual, int maximo)
        {
            Rotulo = rotulo;
            Atual = atual < 0 ? 0 : atual;
            Maximo = maximo < 0 ? 0 : maximo;
        }

        public string Rotulo { get; }
        public int Atual { get; }
        public int Maximo { get; }

        /// <summary>Preenchimento de 0 a 1; máximo zero (ou atual acima do máximo) não estoura.</summary>
        public float Fracao
        {
            get
            {
                if (Maximo <= 0)
                {
                    return 0f;
                }

                var fracao = (float)Atual / Maximo;
                return fracao > 1f ? 1f : fracao;
            }
        }

        public string Texto => Rotulo + " " + Atual + " / " + Maximo;
    }

    /// <summary>
    /// Transforma a resposta de <c>GET</c> <see cref="RotasCombate.Recursos"/> nas barras. Só
    /// formata o que o servidor mandou: nada é calculado ou decidido aqui.
    /// </summary>
    internal static class ApresentacaoCombate
    {
        public const string RotuloVida = "Vida";
        public const string RotuloVigor = "Vigor";
        public const string RotuloEnergia = "Energia";

        /// <summary>
        /// As três barras (vida, vigor, energia) da resposta, ou <c>false</c> quando a chamada
        /// falhou ou veio sem corpo (o painel mantém o que já mostrava).
        /// </summary>
        public static bool TentarMontar(
            bool sucesso,
            RecursosResponse corpo,
            out BarraRecurso vida,
            out BarraRecurso vigor,
            out BarraRecurso energia
        )
        {
            if (!sucesso || corpo == null)
            {
                vida = vigor = energia = default;
                return false;
            }

            vida = new BarraRecurso(RotuloVida, corpo.VidaAtual, corpo.VidaMaxima);
            vigor = new BarraRecurso(RotuloVigor, corpo.VigorAtual, corpo.VigorMaximo);
            energia = new BarraRecurso(RotuloEnergia, corpo.EnergiaAtual, corpo.EnergiaMaxima);
            return true;
        }
    }
}
