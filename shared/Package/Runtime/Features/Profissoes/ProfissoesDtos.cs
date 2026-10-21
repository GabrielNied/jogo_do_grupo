using System;

namespace JogoDoGrupo.Shared.Features.Profissoes
{
    /// <summary>Client → servidor: pede <see cref="EstadoDasProfissoes"/>.</summary>
    [Serializable]
    public class ConsultarProfissoes { }

    /// <summary>Client → servidor: pede <see cref="EstadoDosNos"/>.</summary>
    [Serializable]
    public class ConsultarNos { }

    /// <summary>Um saldo de material do personagem.</summary>
    [Serializable]
    public class SaldoDeMaterial
    {
        public int MaterialId;
        public int Quantidade;

        public SaldoDeMaterial() { }

        public SaldoDeMaterial(int materialId, int quantidade)
        {
            MaterialId = materialId;
            Quantidade = quantidade;
        }
    }

    /// <summary>
    /// Servidor → client: o que o personagem aprendeu e os materiais que tem.
    /// Id: <see cref="ProfissoesMessageIds.EstadoDasProfissoes"/>.
    /// </summary>
    [Serializable]
    public class EstadoDasProfissoes
    {
        /// <summary>Valores de <see cref="Profissao"/> aprendidos.</summary>
        public int[] Aprendidas = new int[0];

        public SaldoDeMaterial[] Materiais = new SaldoDeMaterial[0];

        public EstadoDasProfissoes() { }

        public EstadoDasProfissoes(int[] aprendidas, SaldoDeMaterial[] materiais)
        {
            Aprendidas = aprendidas;
            Materiais = materiais;
        }
    }

    /// <summary>
    /// Servidor → client: os pontos de coleta esgotados e quantos segundos faltam para voltar
    /// (listas paralelas). Ponto fora da lista está disponível. O servidor decide a coleta; isto
    /// só serve para o client esconder o que já foi levado.
    /// Id: <see cref="ProfissoesMessageIds.EstadoDosNos"/>.
    /// </summary>
    [Serializable]
    public class EstadoDosNos
    {
        public int[] Esgotados = new int[0];
        public int[] SegundosParaVoltar = new int[0];

        public EstadoDosNos() { }

        public EstadoDosNos(int[] esgotados, int[] segundosParaVoltar)
        {
            Esgotados = esgotados;
            SegundosParaVoltar = segundosParaVoltar;
        }
    }

    /// <summary>Client → servidor: aprende a profissão (de graça).</summary>
    [Serializable]
    public class AprenderProfissao
    {
        public Profissao Profissao;
    }

    /// <summary>Client → servidor: coleta o ponto. Id: <see cref="ProfissoesMessageIds.ColetarNo"/>.</summary>
    [Serializable]
    public class ColetarNo
    {
        public int NoId;
    }

    /// <summary>
    /// Servidor → client: a coleta deu certo e o material já está no saldo.
    /// Id: <see cref="ProfissoesMessageIds.ColetaConcluida"/>.
    /// </summary>
    [Serializable]
    public class ColetaConcluida
    {
        public int NoId;
        public int MaterialId;
        public int Quantidade;

        public ColetaConcluida() { }

        public ColetaConcluida(int noId, int materialId, int quantidade)
        {
            NoId = noId;
            MaterialId = materialId;
            Quantidade = quantidade;
        }
    }

    /// <summary>Client → servidor: produz a receita na forja.</summary>
    [Serializable]
    public class ProduzirReceita
    {
        public int ReceitaId;
    }

    /// <summary>
    /// Servidor → client: o item produzido já está na bolsa.
    /// Id: <see cref="ProfissoesMessageIds.ProducaoConcluida"/>.
    /// </summary>
    [Serializable]
    public class ProducaoConcluida
    {
        public int ReceitaId;
        public int ModeloId;

        public ProducaoConcluida() { }

        public ProducaoConcluida(int receitaId, int modeloId)
        {
            ReceitaId = receitaId;
            ModeloId = modeloId;
        }
    }

    /// <summary>
    /// Servidor → client: o pedido foi recusado, sem efeito nenhum.
    /// Id: <see cref="ProfissoesMessageIds.ProfissoesRecusada"/>.
    /// </summary>
    [Serializable]
    public class ProfissoesRecusada
    {
        public string Codigo = "";

        public ProfissoesRecusada() { }

        public ProfissoesRecusada(string codigo)
        {
            Codigo = codigo;
        }
    }
}
