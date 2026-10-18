namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Motivo de recusa para a operação de marcação tática de alvo.
    /// </summary>
    public enum CodigoErroMarcador : byte
    {
        NaoEstaEmGrupo = 1,
        AlvoInvalido = 2,
        ApenasLiderPodeMarcar = 3,
    }
}
