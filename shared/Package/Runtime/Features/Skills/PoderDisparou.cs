using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>
    /// Servidor avisa o dono que um poder lendário ou um bônus de set disparou, para o jogador ver
    /// o que mudou na skill. Id: <see cref="SkillsMessageIds.PoderDisparou"/>.
    /// </summary>
    [System.Serializable]
    public class PoderDisparou
    {
        /// <summary>Nome do poder ou do set (com o marco) que disparou.</summary>
        public string Nome = "";

        /// <summary>O que aconteceu: marca, carga, dano extra ou recarga menor.</summary>
        public AcaoDePoder Acao;

        /// <summary>
        /// O número do efeito: o dano extra em % (<see cref="AcaoDePoder.AmplificarDano"/>), os ms
        /// tirados (<see cref="AcaoDePoder.ReduzirRecarga"/>) ou as cargas que o personagem tem.
        /// </summary>
        public int Valor = 0;
    }
}
