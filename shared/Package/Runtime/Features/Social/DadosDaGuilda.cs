namespace JogoDoGrupo.Shared.Features.Social
{
    [System.Serializable]
    public class DadosDaGuilda
    {
        public long GuildaId;
        public string Nome = "";
        public int Nivel = 1;
        public long Experiencia;
        public CargoDaGuilda MeuCargo;
        public MembroDaGuilda[] Membros = new MembroDaGuilda[0];
    }
}
