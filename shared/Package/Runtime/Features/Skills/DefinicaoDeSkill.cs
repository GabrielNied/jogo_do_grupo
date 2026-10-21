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
    /// O apoio que a skill dá ao grupo no lugar do dano (#170): em área ao redor de quem usa, sem
    /// alvo escolhido. <see cref="Nenhum"/> é a skill de dano.
    /// </summary>
    public enum TipoDeApoio
    {
        Nenhum = 0,

        /// <summary>Escudo de absorção em quem usa e no grupo.</summary>
        Escudo = 1,

        /// <summary>Buff Fortalecido (+dano causado) no grupo.</summary>
        Fortalecer = 2,

        /// <summary>Buff Protegido (-dano recebido) no grupo.</summary>
        Proteger = 3,
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

        /// <summary>Preparo em ms antes do efeito; 0 é instantânea. Só skill forte tem (cancelável).</summary>
        public int PreparoMs;

        /// <summary>Vezes o dano de base (dano da arma mais o flat dos itens) que a skill causa.</summary>
        public double Multiplicador = 1.0;

        public PropriedadesDaSkill Propriedades = new PropriedadesDaSkill();

        /// <summary>O apoio de grupo da skill; <see cref="TipoDeApoio.Nenhum"/> se ela causa dano.</summary>
        public TipoDeApoio Apoio = TipoDeApoio.Nenhum;

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
