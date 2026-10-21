namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>
    /// Servidor → participantes da raid (12006): informa que uma etapa/portão da raid foi concluída.
    /// </summary>
    [System.Serializable]
    public class EtapaRaidConcluida
    {
        public string IdInstancia = string.Empty;
        public int EtapaConcluida;
        public int TotalEtapas = RegrasRaid.TotalEtapas;
        public int ProximaEtapa;
        public int CriaturaIdDerrotada;
        public bool EtapaFinal;
        public bool GanhouLootSemanal;
    }
}
