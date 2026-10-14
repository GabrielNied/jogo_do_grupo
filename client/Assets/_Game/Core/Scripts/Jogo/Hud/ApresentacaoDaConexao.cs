using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Client.Core.Jogo
{
    internal enum AvisoDaConexao
    {
        Nenhum,
        Latencia,
        Reconectando,
        Perdida,
        AcessoNegado,
    }

    /// <summary>Decide o aviso a partir do estado da rede, sem depender da interface.</summary>
    internal static class ApresentacaoDaConexao
    {
        internal static AvisoDaConexao Aviso(EstadoTempoReal estado, string erro)
        {
            if (estado == EstadoTempoReal.Conectado)
            {
                return AvisoDaConexao.Latencia;
            }

            if (
                estado == EstadoTempoReal.Reconectando
                || estado == EstadoTempoReal.Conectando
                || estado == EstadoTempoReal.Autenticando
            )
            {
                return AvisoDaConexao.Reconectando;
            }

            if (erro == CodigosErro.AcessoNegado)
            {
                return AvisoDaConexao.AcessoNegado;
            }

            return erro == CodigosErroCliente.SemConexao
                ? AvisoDaConexao.Perdida
                : AvisoDaConexao.Nenhum;
        }

        internal static string TextoDaFaixa(int tentativa, int maximo) =>
            tentativa > 0 ? $"Reconectando ({tentativa}/{maximo})..." : "Conectando ao mundo...";

        internal static string TextoDaLatencia(int milissegundos) =>
            milissegundos >= 0 ? $"{milissegundos} ms" : "Latência indisponível";
    }
}
