namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Papel funcional ou social do NPC no mundo (#1063).</summary>
    public enum PapelNpc
    {
        Nenhum = 0,
        Vendedor = 1,
        Ferreiro = 2,
        Banqueiro = 3,
        Leiloeiro = 4,
        DadorDeMissao = 5,
        Taverneiro = 6,
        Armeiro = 7,
        Armadureiro = 8,
        Merceeiro = 9,
        Estalajadeiro = 10,
        CuidadorDePets = 11,
        Guarda = 12,
        Morador = 13,
        Cavalarico = 14,

        /// <summary>NPC de cena do prólogo (#327); não aparece no catálogo da cidade.</summary>
        Mago = 15,

        /// <summary>Mestres de ofício: um por profissão (#1257), cada um abre só a sua.</summary>
        MestreLenhador = 16,
        MestreMinerador = 17,
        MestreCurtidor = 18,
        MestreFerreiro = 19,
        MestreAlquimista = 20,
        MestreCozinheiro = 21,
    }
}
