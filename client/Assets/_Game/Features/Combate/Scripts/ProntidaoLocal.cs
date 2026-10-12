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

        /// <summary>
        /// Quando o golpe em curso acerta (fim do preparo). Até lá o personagem está
        /// comprometido: anda devagar e não esquiva. Depois, na recuperação, a esquiva vale.
        /// </summary>
        public double AcertoEm { get; private set; }

        /// <summary>Se o personagem está no preparo ou no momento de acerto de um golpe.</summary>
        public bool Comprometido(double agora) => agora < AcertoEm;

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
            // No preparo e no acerto não esquiva; na recuperação ela corta o resto do golpe.
            if (Comprometido(agora) || (agora < LivreEm && !EmRecuperacao(agora)))
            {
                return false;
            }

            return Vigor is not int vigor || RegrasEsquiva.TemVigor(vigor);
        }

        // Dentro de uma ação de ataque, depois do acerto (a esquiva de antes ocupa até LivreEm).
        private bool EmRecuperacao(double agora) => _ataqueEmCurso && agora >= AcertoEm;

        private bool _ataqueEmCurso;

        public void MarcarAtaque(TipoDeAtaque tipo, double agora)
        {
            _ataqueEmCurso = true;
            AcertoEm = agora + RegrasAtaque.PreparoSegundos(tipo);
            LivreEm = agora + RegrasAtaque.DuracaoSegundos(tipo);
            if (Vigor is int vigor)
            {
                RegistrarVigor(vigor - RegrasAtaque.CustoDeVigor(tipo));
            }
        }

        public void MarcarEsquiva(double agora)
        {
            // A esquiva corta o resto do golpe: a recuperação acaba aqui.
            _ataqueEmCurso = false;
            AcertoEm = 0;
            LivreEm = agora + RegrasEsquiva.DuracaoSegundos;
            if (Vigor is int vigor)
            {
                RegistrarVigor(vigor - RegrasEsquiva.CustoDeVigor);
            }
        }
    }
}
