using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Uma arma que aparece no corpo do personagem.</summary>
    [Serializable]
    public class ArmaVisual
    {
        /// <summary><see cref="PosicaoEquipamento.Arma1"/> ou <see cref="PosicaoEquipamento.Arma2"/>.</summary>
        public PosicaoEquipamento Posicao;

        public TipoEquipamento Tipo;

        /// <summary>Modelo-base da peça (escolhe o desenho quando houver um por modelo).</summary>
        public int ModeloId;

        /// <summary>Tier da arma equipada (D a S+), determina aura no mundo 3D para B+.</summary>
        public TierItem Tier = TierItem.D;
    }

    /// <summary>
    /// Servidor avisa a área do que um personagem veste e se vê: o <b>estado inteiro</b>, não a
    /// diferença. Sai quando ele equipa, desequipa ou troca de conjunto, e quando alguém entra na
    /// área. Lista vazia = sem arma nenhuma. Id: <see cref="ItensMessageIds.VisualEquipado"/>.
    /// </summary>
    [Serializable]
    public class VisualEquipado
    {
        public long PersonagemId;

        /// <summary>Conjunto de armas ativo (1 ou 2): a arma dele vai à mão, a do outro às costas.</summary>
        public int ConjuntoAtivo = 1;

        public ArmaVisual[] Armas = new ArmaVisual[0];
    }
}
