namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Os cinco atributos de um personagem, já com o valor final (base + pontos distribuídos +
    /// equipamentos + efeitos). Regras em docs/features/fatia-jogavel.md, seção 1.
    /// </summary>
    [System.Serializable]
    public class AtributosCombate
    {
        public int Forca;
        public int Agilidade;
        public int Intelecto;
        public int Vigor;
        public int Espirito;
    }
}
