namespace JogoDoGrupo.Server.Features.Social;

public sealed class PedidoAmizadeSalvo
{
    public long PedidoId { get; set; }

    public long DeContaId { get; set; }

    public long ParaContaId { get; set; }

    public string NomeAlvo { get; set; } = "";
}
