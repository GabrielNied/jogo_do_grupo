namespace JogoDoGrupo.Shared.Features.Combate
{
    public enum CategoriaDeDano
    {
        Fisica,
        Magica,
        Ambas,
    }

    public enum TipoDeEfeito
    {
        Dano,
        Cura,
        Escudo,
        Controle,
        Mobilidade,
        Utilidade,
        RecuperacaoDeRecurso,
    }

    /// <summary>
    /// As propriedades mínimas que uma skill declara (docs/features/talentos.md, "Efeitos
    /// condicionais por skill"). Crítico, penetração e velocidade só valem com a flag
    /// correspondente: a categoria do item não decide nada.
    /// </summary>
    [System.Serializable]
    public class PropriedadesDaSkill
    {
        public string Id = "";
        public CategoriaDeDano Categoria;
        public TipoDeEfeito Tipo;
        public bool PodeCritar;
        public bool RecebePenetracao;
        public bool PodeSerAcelerada;

        /// <summary>
        /// Só para <see cref="CategoriaDeDano.Ambas"/>: que parte (0 a 1) do dano é física; o resto
        /// é mágico. Cada parte recebe só o que é do seu tipo, então nada vale duas vezes.
        /// </summary>
        public double FracaoFisica = ConfigAtributosSecundarios.FracaoFisicaPadraoDaHibrida;
    }
}
