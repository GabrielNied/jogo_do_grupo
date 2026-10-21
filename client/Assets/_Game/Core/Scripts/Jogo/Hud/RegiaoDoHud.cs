namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Regiões da tela do HUD (<c>docs/features/interface.md</c>, seção "HUD do jogo"). Cada
    /// funcionalidade preenche a sua com <see cref="Hud.Regiao"/>. O desenho segue o MMO de PC
    /// clássico (World of Warcraft) com a bússola e o combate de ação do New World: quadro do
    /// personagem no alto à esquerda, alvo no alto ao centro, barra de ação embaixo ao centro,
    /// minimapa e missões à direita, chat embaixo à esquerda e menu rápido embaixo à direita.
    /// </summary>
    public enum RegiaoDoHud
    {
        /// <summary>
        /// Embaixo à esquerda, acima do chat: espaço livre para avisos de uma funcionalidade
        /// (ex.: montaria). Os recursos do personagem foram para <see cref="QuadroJogador"/>.
        /// </summary>
        InferiorEsquerda,

        /// <summary>Embaixo à esquerda, acima da barra de experiência: chat (Social).</summary>
        Chat,

        /// <summary>Lateral esquerda, abaixo do quadro do personagem: lista do grupo (Social).</summary>
        LateralEsquerda,

        /// <summary>Topo, centro, abaixo da bússola: quadro do alvo e barra do chefe (Mobs, Combate).</summary>
        TopoCentro,

        /// <summary>Centro, abaixo do alvo: aviso de ataque telegrafado (Combate).</summary>
        CentroAvisos,

        /// <summary>Acima da barra de ação: mensagens de sistema (Core, <see cref="AvisosDoJogo"/>).</summary>
        InferiorCentro,

        /// <summary>Canto superior direito: minimapa (Mundo).</summary>
        SuperiorDireita,

        /// <summary>Canto superior esquerdo: retrato, nome, nível, vida, mana e stamina (Combate).</summary>
        QuadroJogador,

        /// <summary>Embaixo, centro: barra de ação com as skills e atalhos de combate (Skills, Combate, Montarias).</summary>
        BarraDeAcao,

        /// <summary>Faixa fina na borda de baixo da tela: experiência (Personagem).</summary>
        Experiencia,

        /// <summary>Embaixo à direita: menu rápido com os ícones das janelas (Core).</summary>
        MenuRapido,

        /// <summary>Direita, abaixo do minimapa: rastreador de missões (Missões).</summary>
        Rastreador,

        /// <summary>Topo, centro: bússola (Core).</summary>
        Bussola,
    }
}
