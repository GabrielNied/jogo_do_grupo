namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>Um membro em <see cref="ListaDoGrupo.Membros"/>.</summary>
    [System.Serializable]
    public class MembroDoGrupo
    {
        public long PersonagemId = 0;
        public string Nome = "";

        /// <summary>Falso para quem desconectou e ainda tem a vaga guardada.</summary>
        public bool Online = false;
    }
}
