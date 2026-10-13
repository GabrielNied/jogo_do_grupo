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

        /// <summary>A linha pronta: "Dano físico dos itens: +12".</summary>
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
        public bool TemArma;

        public bool ArmaMagica;

        /// <summary>Dano-base da arma já convertido; 0 sem arma.</summary>
        public double DanoDaArma;

        /// <summary>Dano físico flat somado de todas as peças ativas.</summary>
        public double BonusDanoFisico;

        /// <summary>Dano mágico flat somado de todas as peças ativas.</summary>
        public double BonusDanoMagico;

        /// <summary>Vida máxima somada das peças.</summary>
        public int VidaMaxima;

        public double DefesaFisica;
        public double DefesaMagica;

        public LinhaDeEquipamento[] Linhas = new LinhaDeEquipamento[0];
    }
}
