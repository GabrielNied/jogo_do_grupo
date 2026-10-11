namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Ficha fixa de uma criatura do <see cref="CatalogoCriaturas"/>: os valores de base, antes
    /// da <see cref="VarianteDeCriatura"/>. Não muda depois de criada.
    /// </summary>
    public sealed class DefinicaoDeCriatura
    {
        public DefinicaoDeCriatura(
            int id,
            string nome,
            CategoriaDeCriatura categoria,
            int nivel,
            int vidaBase,
            int danoBase,
            int experienciaBase,
            double chanceDeAparecer,
            int intervaloDeAparicaoSegundos
        )
        {
            Id = id;
            Nome = nome;
            Categoria = categoria;
            Nivel = nivel;
            VidaBase = vidaBase;
            DanoBase = danoBase;
            ExperienciaBase = experienciaBase;
            ChanceDeAparecer = chanceDeAparecer;
            IntervaloDeAparicaoSegundos = intervaloDeAparicaoSegundos;
        }

        /// <summary>Id estável no <see cref="CatalogoCriaturas"/>.</summary>
        public int Id { get; }

        /// <summary>Nome exibido.</summary>
        public string Nome { get; }

        public CategoriaDeCriatura Categoria { get; }

        /// <summary>Nível fixo: a criatura não escala com os jogadores.</summary>
        public int Nivel { get; }

        public int VidaBase { get; }

        /// <summary>Dano do golpe básico.</summary>
        public int DanoBase { get; }

        /// <summary>Experiência que cada participante recebe ao derrotá-la.</summary>
        public int ExperienciaBase { get; }

        /// <summary>
        /// Chance, de 0 a 1, de a criatura aparecer em cada sorteio. É 1 para comuns e chefes;
        /// só o chefe raro tem menos que 1.
        /// </summary>
        public double ChanceDeAparecer { get; }

        /// <summary>
        /// Segundos até a criatura voltar depois de morta (comum) ou entre um sorteio de
        /// aparição e o próximo (chefe raro). Zero: não volta sozinha (chefe de dungeon).
        /// </summary>
        public int IntervaloDeAparicaoSegundos { get; }
    }
}
