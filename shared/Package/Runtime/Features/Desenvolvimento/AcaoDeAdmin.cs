namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>
    /// O que a tela de admin (F8) pede ao servidor. Os números não mudam: vão na rede.
    /// Cada ação usa só os campos que a <see cref="PedirAcaoDeAdmin"/> descreve para ela.
    /// </summary>
    public enum AcaoDeAdmin
    {
        /// <summary>Gera equipamentos na bolsa (<c>ModeloId</c>, <c>Grau</c>, <c>Origem</c>, <c>Quantidade</c>).</summary>
        GerarItem = 0,

        /// <summary>Soma ou tira experiência (<c>Valor</c>).</summary>
        AjustarExperiencia = 1,

        /// <summary>Soma ou tira coroas (<c>Valor</c>).</summary>
        AjustarCoroas = 2,

        /// <summary>Soma essências de dungeon e de raid (<c>Valor</c> cada, só positivo).</summary>
        CreditarEssencias = 3,

        /// <summary>Teleporta para <c>X</c>, <c>Y</c> e <c>Z</c>.</summary>
        Teleportar = 4,

        /// <summary>Faz inimigos da espécie <c>EspecieId</c> aparecerem perto (<c>Quantidade</c>).</summary>
        SpawnarInimigo = 5,

        /// <summary>Mata os inimigos vivos da área de quem pediu.</summary>
        MatarInimigosDaArea = 6,

        /// <summary>Apaga os itens da bolsa, menos os favoritos.</summary>
        LimparBolsa = 7,

        /// <summary>Veste o último item que a tela gerou.</summary>
        EquiparUltimoItem = 8,

        /// <summary>Zera a durabilidade das peças equipadas.</summary>
        QuebrarEquipados = 9,

        /// <summary>Devolve a durabilidade das peças equipadas (sem custo).</summary>
        ConsertarEquipados = 10,
    }
}
