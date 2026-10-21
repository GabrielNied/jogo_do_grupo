using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>Client → servidor: pede para consumir uma poção de recuperação.</summary>
    [Serializable]
    public class ConsumirPocao
    {
        public long InstanciaId = 0;
    }

    /// <summary>Servidor → cliente: poção foi consumida com sucesso.</summary>
    [Serializable]
    public class PocaoConsumida
    {
        public int VidaCurada = 0;
        public int RecargaSegundos = 0;
    }

    /// <summary>Client → servidor: pede para consumir uma refeição da bolsa.</summary>
    [Serializable]
    public class ConsumirRefeicao
    {
        public int ModeloId = 0;
    }

    /// <summary>Servidor → cliente: refeição consumida e buff aplicado.</summary>
    [Serializable]
    public class RefeicaoConsumida
    {
        public int ModeloId = 0;
        public string Nome = "";
        public int DuracaoSegundos = 0;
        public double BonusRegenVida = 0;
    }

    /// <summary>Client → servidor: intenção de sentar/descansar para acelerar recuperação.</summary>
    [Serializable]
    public class IntencaoDeDescanso
    {
        public bool Descansando = false;
    }

    /// <summary>Servidor → quem está na área: estado de descanso de um personagem mudou.</summary>
    [Serializable]
    public class DescansoMudou
    {
        public long PersonagemId = 0;
        public bool Descansando = false;
    }

    /// <summary>Client → servidor: consulta o estado atual de recuperação.</summary>
    [Serializable]
    public class ConsultarEstadoDeRecuperacao { }

    /// <summary>Servidor → cliente: estado completo de combate, poção, comida e descanso.</summary>
    [Serializable]
    public class EstadoDeRecuperacao
    {
        public bool EmCombate = false;
        public int SegundosAteSairDeCombate = 0;
        public int RecargaPocaoRestanteSegundos = 0;
        public string ComidaNome = "";
        public int ComidaDuracaoRestanteSegundos = 0;
        public double ComidaBonusRegenVida = 0;
        public bool Descansando = false;
    }

    /// <summary>Servidor → cliente: o pedido de recuperação foi recusado.</summary>
    [Serializable]
    public class RecuperacaoRecusada
    {
        public string Codigo = "";
    }
}
