namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Servidor avisa o dono que o personagem ganhou experiência. Traz o estado inteiro depois
    /// do ganho, então o client só mostra (não soma nem calcula nível). Se o ganho subiu de
    /// nível, <see cref="Nivel"/> e <see cref="PontosLivres"/> já vêm atualizados e quem está na
    /// área recebe também <see cref="SubiuDeNivel"/>.
    /// Id: <see cref="PersonagemMessageIds.ExperienciaGanha"/>.
    /// </summary>
    [System.Serializable]
    public class ExperienciaGanha
    {
        /// <summary>
        /// Quanto a experiência mudou agora. Positivo ao ganhar de um inimigo; no comando de
        /// teste pode ser negativo. O client usa os campos de estado e não soma este valor.
        /// </summary>
        public long Quantidade = 0;

        /// <summary>Experiência acumulada dentro do nível atual, depois do ganho.</summary>
        public long XpAtual = 0;

        /// <summary>
        /// Experiência que o nível atual pede para subir
        /// (<see cref="RegrasExperiencia.XpParaOProximoNivel"/>). No nível máximo é só
        /// informativo: <see cref="XpAtual"/> pode passar dele.
        /// </summary>
        public long XpDoNivel = 0;

        /// <summary>Nível depois do ganho.</summary>
        public int Nivel = 1;

        /// <summary>Pontos de atributo ainda não distribuídos, depois do ganho.</summary>
        public int PontosLivres = 0;
    }
}
