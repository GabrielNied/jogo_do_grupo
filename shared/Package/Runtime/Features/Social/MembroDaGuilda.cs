namespace JogoDoGrupo.Shared.Features.Social
{
    [System.Serializable]
    public class MembroDaGuilda
    {
        public long PersonagemId;
        public string Nome = "";
        public CargoDaGuilda Cargo;
        public bool Online;
        public string Area = "";
        public long EntrouEmUnix;
    }
}
