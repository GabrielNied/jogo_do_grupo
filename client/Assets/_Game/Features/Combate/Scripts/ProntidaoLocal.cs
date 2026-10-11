using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O que o client já sabe do próprio vigor e da ação em curso, para não pedir nem animar
    /// o que o servidor recusaria. Sem leitura de vigor ainda, a ação segue (o servidor
    /// continua conferindo). A recarga é a duração da ação (<see cref="RegrasAtaque.DuracaoSegundos"/>
    /// ou <see cref="RegrasEsquiva.DuracaoSegundos"/>).
    /// </summary>
    internal sealed class ProntidaoLocal
    {
        public int? Vigor { get; private set; }

        public double LivreEm { get; private set; }

        public void RegistrarVigor(int vigor) => Vigor = vigor < 0 ? 0 : vigor;

        public bool PodeAtacar(TipoDeAtaque tipo, double agora)
        {
            if (agora < LivreEm)
            {
                return false;
            }

            return Vigor is not int vigor || RegrasAtaque.TemVigor(tipo, vigor);
        }

        public bool PodeEsquivar(double agora)
        {
            if (agora < LivreEm)
            {
                return false;
            }

            return Vigor is not int vigor || RegrasEsquiva.TemVigor(vigor);
        }

        public void MarcarAtaque(TipoDeAtaque tipo, double agora)
        {
            LivreEm = agora + RegrasAtaque.DuracaoSegundos(tipo);
            if (Vigor is int vigor)
            {
                RegistrarVigor(vigor - RegrasAtaque.CustoDeVigor(tipo));
            }
        }

        public void MarcarEsquiva(double agora)
        {
            LivreEm = agora + RegrasEsquiva.DuracaoSegundos;
            if (Vigor is int vigor)
            {
                RegistrarVigor(vigor - RegrasEsquiva.CustoDeVigor);
            }
        }
    }
}
