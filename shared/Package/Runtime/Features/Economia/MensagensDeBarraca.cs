using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Resumo de uma barraca ativa na praça para exibição no mapa ou lista.
    /// </summary>
    [Serializable]
    public class BarracaResumo
    {
        public long PersonagemId;
        public string NomeDono = "";
        public string TituloBarraca = "";
        public float PosicaoX;
        public float PosicaoZ;
        public bool EhVendedorOffline;
        public int QuantidadeItens;
    }

    /// <summary>
    /// Client → servidor: monta a barraca presencial na praça.
    /// Id: EconomiaMessageIds.MontarBarraca (9120).
    /// </summary>
    [Serializable]
    public class MontarBarraca
    {
        public string Titulo = "";
        public float PosicaoX;
        public float PosicaoZ;
    }

    /// <summary>
    /// Client → servidor: desmonta a barraca presencial.
    /// Id: EconomiaMessageIds.DesmontarBarraca (9121).
    /// </summary>
    [Serializable]
    public class DesmontarBarraca { }

    /// <summary>
    /// Client → servidor: contrata manequim vendedor offline por N dias mediante diária em coroas.
    /// Id: EconomiaMessageIds.ContratarVendedorOffline (9122).
    /// </summary>
    [Serializable]
    public class ContratarVendedorOffline
    {
        public string Titulo = "";
        public int Dias = 1;
        public float PosicaoX;
        public float PosicaoZ;
    }

    /// <summary>
    /// Client → servidor: encerra o vendedor offline antes do prazo.
    /// Id: EconomiaMessageIds.EncerrarVendedorOffline (9123).
    /// </summary>
    [Serializable]
    public class EncerrarVendedorOffline { }

    /// <summary>
    /// Client → servidor: consulta as ofertas e dados da barraca de um jogador.
    /// Id: EconomiaMessageIds.ConsultarBarraca (9124).
    /// </summary>
    [Serializable]
    public class ConsultarBarraca
    {
        public long PersonagemId;
    }

    /// <summary>
    /// Servidor → client: vitrine e ofertas ativas na barraca consultada.
    /// Id: EconomiaMessageIds.DadosDaBarraca (9125).
    /// </summary>
    [Serializable]
    public class DadosDaBarraca
    {
        public long PersonagemId;
        public string NomeDono = "";
        public string Titulo = "";
        public bool EhVendedorOffline;
        public int TaxaPercentual = ConfigBarraca.TaxaVendaBarracaPercentual;
        public OrdemDeMercado[] Itens = Array.Empty<OrdemDeMercado>();
    }

    /// <summary>
    /// Servidor → client: lista de barracas ativas na praça da cidade.
    /// Id: EconomiaMessageIds.BarracasNaArea (9126).
    /// </summary>
    [Serializable]
    public class BarracasNaArea
    {
        public BarracaResumo[] Barracas = Array.Empty<BarracaResumo>();
    }

    /// <summary>
    /// Client → servidor: compra diretamente um item da barraca de outro jogador com taxa de 3%.
    /// Id: EconomiaMessageIds.ComprarDaBarraca (9127).
    /// </summary>
    [Serializable]
    public class ComprarDaBarraca
    {
        public long OrdemId;
        public long VendedorId;
    }
}
