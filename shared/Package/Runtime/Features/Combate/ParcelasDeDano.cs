using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// As parcelas do dano do golpe básico, cada uma com a sua origem (combate.md, estrutura do
    /// dano com arma): o dano-base da arma (ou dos punhos), o bônus em % do personagem (Força ou
    /// Intelecto) e o bônus em % do equipamento (Força ou Intelecto dos itens). A do equipamento
    /// nunca soma na do personagem: são fontes separadas que só se juntam na conta do dano.
    /// </summary>
    [Serializable]
    public class ParcelasDeDano
    {
        /// <summary>Dano-base: o da arma equipada, ou os punhos sem arma.</summary>
        public double DanoBase;

        /// <summary>Se o dano é mágico (arma mágica); aí vale o Intelecto, não a Força.</summary>
        public bool Magico;

        /// <summary>Bônus do personagem, em fração do dano-base (0,10 = +10%).</summary>
        public double PercentualPersonagem;

        /// <summary>Bônus dos itens, em fração do dano-base (0,10 = +10%).</summary>
        public double PercentualEquipamento;

        /// <summary>Sem arma: o dano-base dos punhos e a Força do personagem, sem item nenhum.</summary>
        public static ParcelasDeDano SemArma(int forca) =>
            new ParcelasDeDano
            {
                DanoBase = DanoSemArma,
                PercentualPersonagem = RegrasAtaque.PercentualDeDanoPorPonto * Math.Max(0, forca),
            };

        /// <summary>
        /// Dano-base de quem não tem arma (punhos). É o "dano da espada 20" da fatia; a arma
        /// equipada o substitui (ATRIB-004). Valor de teste.
        /// </summary>
        public const double DanoSemArma = 20.0;
    }
}
