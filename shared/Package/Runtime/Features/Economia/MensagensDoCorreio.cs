using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>Resumo de uma carta na caixa de entrada do correio.</summary>
    [Serializable]
    public class ResumoCarta
    {
        public long Id;
        public string Remetente = "";
        public string Assunto = "";
        public bool Lida;
        public bool TemAnexos;
        public long Coroas;
        public int TotalItens;
        public long EnviadaEm;
        public long ExpiraEm;
        public bool DoSistema;
    }

    /// <summary>Um item anexado a uma carta de correio.</summary>
    [Serializable]
    public class AnexoItemCorreio
    {
        public long AnexoId;
        public int ItemId;
        public EntradaInventario? Item;
    }

    /// <summary>Conteúdo e anexos completos de uma carta aberta.</summary>
    [Serializable]
    public class DetalhesDaCarta
    {
        public long Id;
        public string Remetente = "";
        public string Assunto = "";
        public string Mensagem = "";
        public bool Lida;
        public long Coroas;
        public AnexoItemCorreio[] Itens = Array.Empty<AnexoItemCorreio>();
        public long EnviadaEm;
        public long ExpiraEm;
        public bool AnexosColetados;
        public bool DoSistema;
    }

    /// <summary>Client → servidor: consulta as cartas da caixa de entrada.</summary>
    [Serializable]
    public class ConsultarCorreio { }

    /// <summary>Servidor → client: lista de cartas na caixa de correio.</summary>
    [Serializable]
    public class EstadoDoCorreio
    {
        public ResumoCarta[] Cartas = Array.Empty<ResumoCarta>();
        public int NaoLidas;
    }

    /// <summary>Client → servidor: pede a leitura detalhada de uma carta.</summary>
    [Serializable]
    public class LerCarta
    {
        public long CartaId;
    }

    /// <summary>Client → servidor: envia uma carta com mensagem, coroas e itens anexados.</summary>
    [Serializable]
    public class EnviarCarta
    {
        public string Destinatario = "";
        public string Assunto = "";
        public string Mensagem = "";
        public long Coroas;
        public long[] InstanciasItens = Array.Empty<long>();
    }

    /// <summary>Servidor → client: confirmação de carta enviada.</summary>
    [Serializable]
    public class CartaEnviada
    {
        public long CartaId;
    }

    /// <summary>Client → servidor: pede a coleta de anexos (coroas e itens) de uma carta.</summary>
    [Serializable]
    public class ColetarAnexos
    {
        public long CartaId;
    }

    /// <summary>Servidor → client: confirmação de anexos coletados.</summary>
    [Serializable]
    public class AnexosColetados
    {
        public long CartaId;
        public long Coroas;
        public int TotalItens;
    }

    /// <summary>Client → servidor: pede a exclusão de uma carta sem anexos.</summary>
    [Serializable]
    public class ExcluirCarta
    {
        public long CartaId;
    }

    /// <summary>Servidor → client: confirmação de carta excluída.</summary>
    [Serializable]
    public class CartaExcluida
    {
        public long CartaId;
    }

    /// <summary>Servidor → client: aviso de que uma nova carta chegou no correio.</summary>
    [Serializable]
    public class AvisoNovoCorreio
    {
        public long CartaId;
        public string Remetente = "";
        public string Assunto = "";
    }
}
