namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>Uma skill da barra com o estado da recarga no instante do envio.</summary>
    [System.Serializable]
    public class SkillDaBarra
    {
        public int Espaco = 0;
        public string Id = "";
        public string Nome = "";
        public RecursoDeSkill Recurso;
        public int Custo = 0;
        public int RecargaMs = 0;

        /// <summary>Preparo em ms (0 se instantânea); skill com preparo pode ser cancelada.</summary>
        public int PreparoMs = 0;

        /// <summary>Quanto falta da recarga, em ms (0 se pronta). O client conta a partir daqui.</summary>
        public int RestanteMs = 0;
    }

    /// <summary>
    /// Servidor envia ao dono a barra de skills da arma equipada: ao entrar no mundo, ao equipar,
    /// desequipar ou trocar de conjunto. Sem arma, <see cref="Skills"/> vem vazia.
    /// Id: <see cref="SkillsMessageIds.BarraDeSkillsMudou"/>.
    /// </summary>
    [System.Serializable]
    public class BarraDeSkillsMudou
    {
        /// <summary>Id do exemplar da arma (0 sem arma); a recarga é dessa arma.</summary>
        public long ArmaId = 0;

        public SkillDaBarra[] Skills = System.Array.Empty<SkillDaBarra>();
    }
}
