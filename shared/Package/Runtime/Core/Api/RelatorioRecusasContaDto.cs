using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Core.Api
{
    /// <summary>
    /// Relatório de recusas repetidas de tempo real por conta para ferramentas de GM (#523, #211).
    /// </summary>
    [System.Serializable]
    public class RelatorioRecusasContaDto
    {
        public long ContaId;
        public long PersonagemId;
        public int TotalRecusas;
        public string UltimoMotivo = "";
        public int UltimoMensagemId;
        public bool AlertaAtivo;
        public string UltimaRecusaUtc = "";
        public Dictionary<string, int> RecusasPorMotivo = new Dictionary<string, int>();
    }
}
