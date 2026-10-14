namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// Reserva temporária do nome de um personagem excluído por 7 dias.
/// Durante o período, apenas a conta dona pode reutilizar o nome.
/// </summary>
public sealed class NomeReservado
{
    public long Id { get; set; }

    /// <summary>Conta que possuía o personagem excluído.</summary>
    public long ContaId { get; set; }

    /// <summary>Nome do personagem excluído.</summary>
    public string Nome { get; set; } = "";

    /// <summary>Nome em minúsculas para consulta rápida.</summary>
    public string NomeNormalizado { get; set; } = "";

    /// <summary>Momento em que o personagem foi excluído.</summary>
    public DateTime ExcluidoEm { get; set; }

    /// <summary>Prazo até o qual o nome fica reservado para a conta (7 dias).</summary>
    public DateTime ReservadoAte { get; set; }
}
