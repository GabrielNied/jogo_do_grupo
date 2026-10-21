namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Client → Servidor: solicita a compra de uma montaria vendida no estábulo (#1067).
    /// </summary>
    [System.Serializable]
    public sealed class ComprarMontariaEstabulo
    {
        public int TipoMontaria;

        public ComprarMontariaEstabulo() { }

        public ComprarMontariaEstabulo(int tipoMontaria)
        {
            TipoMontaria = tipoMontaria;
        }
    }
}
