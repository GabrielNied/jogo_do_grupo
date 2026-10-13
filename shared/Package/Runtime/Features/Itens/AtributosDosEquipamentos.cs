using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Uma linha do que o equipamento dá ao combate, para a janela do personagem.</summary>
    [Serializable]
    public class LinhaDeEquipamento
    {
        public StatusItem Status;

        /// <summary>O número de combate somado de todas as peças ativas (unidade em <see cref="ConfigAtributosItem"/>).</summary>
        public double Valor;

        /// <summary>A linha pronta: "Força dos itens: +7% de dano físico".</summary>
        public string Texto = "";
    }

    /// <summary>
    /// O que as peças ativas (vestidas e a arma do conjunto ativo) dão ao combate, <b>já
    /// convertido</b> pela conversão linear de cada atributo e categoria (ATRIB-003). É uma fonte
    /// própria: nada aqui soma em Força, Vigor, Agilidade, Intelecto ou Espírito do personagem
    /// (DEC-001, ATRIB-002). Os nove secundários não vêm aqui; chegam pela pipeline do Combate
    /// (fonte Equipamento, uma parcela por peça).
    /// </summary>
    [Serializable]
    public class AtributosDosEquipamentos
    {
        /// <summary>Se há arma na mão principal do conjunto ativo (senão vale o dano sem arma).</summary>
        public bool TemArma;

        /// <summary>Se o dano da arma é mágico (cajado); aí o Intelecto aumenta o dano, não a Força.</summary>
        public bool ArmaMagica;

        /// <summary>Dano-base da arma já convertido; 0 sem arma.</summary>
        public double DanoDaArma;

        /// <summary>Bônus de dano físico da Força dos itens, em fração (0,10 = +10%). Não é a Força do personagem.</summary>
        public double PercentualForca;

        /// <summary>Bônus de dano mágico do Intelecto dos itens, em fração.</summary>
        public double PercentualIntelecto;

        /// <summary>Vida máxima somada (do Vigor dos itens).</summary>
        public int VidaMaxima;

        /// <summary>Stamina máxima somada (da Agilidade dos itens).</summary>
        public int StaminaMaxima;

        /// <summary>Mana máxima somada (do Espírito dos itens).</summary>
        public int ManaMaxima;

        public double DefesaFisica;
        public double DefesaMagica;

        /// <summary>As linhas para mostrar, uma por atributo, na ordem de <see cref="StatusItem"/>.</summary>
        public LinhaDeEquipamento[] Linhas = new LinhaDeEquipamento[0];
    }
}
