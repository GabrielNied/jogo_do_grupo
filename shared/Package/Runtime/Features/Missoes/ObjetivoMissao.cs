namespace JogoDoGrupo.Shared.Features.Missoes
{
    public class ObjetivoMissao
    {
        public int Id;
        public string Descricao = "";
        public TipoDeObjetivo Tipo;
        public int AlvoId;
        public int QuantidadeTotal;
        public string RegiaoAlvo = "";
        public float CoordenadaX;
        public float CoordenadaZ;
        public float RaioAreaMetros;
    }
}
