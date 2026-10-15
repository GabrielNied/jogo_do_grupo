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

        /// <summary>
        /// As etiquetas que a skill declara (<see cref="EtiquetasDeSkill"/>): é por elas que os
        /// poderes e sets dizem, de forma explícita, quais skills afetam (PODER-003).
        /// </summary>
        public string[] Etiquetas = System.Array.Empty<string>();
    }

    /// <summary>
    /// As etiquetas de skill que os poderes usam. A de dano vem da categoria da skill; as de
    /// comportamento são declaradas no catálogo. Itens escreve o mesmo texto em
    /// <c>CatalogoDePoderes</c> e um teste confere que concordam.
    /// </summary>
    public static class EtiquetasDeSkill
    {
        public const string Fisica = "fisica";
        public const string Magica = "magica";

        /// <summary>A skill que leva o personagem até o alvo ao atacar.</summary>
        public const string Investida = "investida";
    }
}
