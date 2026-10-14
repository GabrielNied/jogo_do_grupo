using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>De qual recurso a skill gasta o custo.</summary>
    public enum RecursoDeSkill
    {
        Stamina,
        Mana,
    }

    /// <summary>
    /// Uma skill do catálogo: o que o jogador vê (nome, custo, recarga) e o que o servidor usa
    /// para decidir (multiplicador do dano). <see cref="Propriedades"/> são as categorias e
    /// flags que os poderes e a árvore usam para se ligar à skill (SKILL-006).
    /// </summary>
    [System.Serializable]
    public class DefinicaoDeSkill
    {
        public string Id = "";
        public string Nome = "";
        public RecursoDeSkill Recurso;
        public int Custo;
        public int RecargaMs;

        /// <summary>Vezes o dano de base (dano da arma mais o flat dos itens) que a skill causa.</summary>
        public double Multiplicador = 1.0;

        public PropriedadesDaSkill Propriedades = new PropriedadesDaSkill();
    }
}
