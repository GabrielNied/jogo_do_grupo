namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Regiões da tela do HUD (<c>docs/features/interface.md</c>, seção "HUD do jogo"). Cada
    /// funcionalidade preenche a sua com <see cref="Hud.Regiao"/>.
    /// </summary>
    public enum RegiaoDoHud
    {
        /// <summary>Canto inferior esquerdo: barras do personagem (Combate).</summary>
        InferiorEsquerda,

        /// <summary>Lateral esquerda, do meio para cima: lista do grupo (Social).</summary>
        LateralEsquerda,

        /// <summary>Topo, centro: barra do boss e barra do alvo (Combate).</summary>
        TopoCentro,

        /// <summary>Centro, logo abaixo da barra do boss: aviso de ataque telegrafado (Combate).</summary>
        CentroAvisos,

        /// <summary>Parte de baixo, centro: mensagens de sistema (Core, <see cref="AvisosDoJogo"/>).</summary>
        InferiorCentro,

        /// <summary>Canto superior direito: minimapa (Mundo).</summary>
        SuperiorDireita,
    }
}
