namespace JogoDoGrupo.Shared.Features.Missoes
{
    public class DefinicaoDeMissao
    {
        public int Id;
        public string Titulo = "";
        public string Descricao = "";
        public TipoDeMissao Tipo;
        public string Regiao = "";
        public int NivelRecomendado;
        public int NivelMinimo;
        public int NpcDoadorId;
        public int NpcEntregadorId;
        public int[] RequisitosMissaoIds = System.Array.Empty<int>();
        public ObjetivoMissao[] Objetivos = System.Array.Empty<ObjetivoMissao>();
        public int RecompensaXp;
        public long RecompensaCoroas;
        public int[] ItensParaEscolha = System.Array.Empty<int>();
        public string ReputacaoFaccao = "";
        public int ReputacaoValor;
        public int ProximaMissaoPrincipalId;
    }
}
