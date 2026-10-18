using System.Net;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>Seção <c>TempoReal</c> da configuração (canal UDP, ADR 0015).</summary>
public sealed class ConfiguracaoTempoReal
{
    public const string Secao = "TempoReal";

    /// <summary>Endereço IPv4 em que o servidor escuta. <c>0.0.0.0</c>: todas as interfaces.</summary>
    public string Endereco { get; set; } = "0.0.0.0";

    /// <summary>Porta UDP. <c>0</c> deixa o sistema escolher (usado nos testes).</summary>
    public int Porta { get; set; } = 5081;

    /// <summary>Chave que o client manda ao conectar. Conexão com outra chave é recusada.</summary>
    public string ChaveConexao { get; set; } = "JogoDoGrupo";

    /// <summary>Prazo para a conexão se autenticar; depois disso ela é desconectada.</summary>
    public int TempoParaAutenticarMs { get; set; } = 10_000;

    /// <summary>Sem pacotes do client por este tempo, a conexão é considerada caída.</summary>
    public int TempoQuedaMs { get; set; } = 5_000;

    /// <summary>
    /// Pacotes por segundo que uma conexão pode mandar, em média (issue #211). Acima disso a
    /// conexão é derrubada. O client manda 10 posições por segundo mais ataques e pedidos.
    /// </summary>
    public int MensagensPorSegundo { get; set; } = 60;

    /// <summary>Pacotes que podem chegar de uma vez acima da média (rajada após um engasgo da rede).</summary>
    public int RajadaMensagens { get; set; } = 120;

    /// <summary>Tamanho total máximo em bytes, incluindo cabeçalho.</summary>
    public int TamanhoMaximoPacote { get; set; } = 16_384;
    public int ProfundidadeMaximaJson { get; set; } = 32;
    public int MaximoRecusasPorSessao { get; set; } = 10;
    public int MaximoExcecoesPorSessao { get; set; } = 3;
    public int MaximoTentativasAutenticacao { get; set; } = 3;
    public int MaximoConexoesPendentesPorIp { get; set; } = 32;

    /// <summary>Janela em minutos para agregação de recusas de tempo real por conta para o GM (#523).</summary>
    public int JanelaRecusasMinutos { get; set; } = 10;

    /// <summary>Quantidade de recusas na janela para disparar alerta de suspeita para o GM (#523).</summary>
    public int LimiteAlertaRecusasConta { get; set; } = 5;

    /// <summary>Lê e valida a seção; configuração inválida impede o servidor de subir.</summary>
    public static ConfiguracaoTempoReal Ler(IConfiguration configuracao)
    {
        var tempoReal =
            configuracao.GetSection(Secao).Get<ConfiguracaoTempoReal>()
            ?? new ConfiguracaoTempoReal();

        if (tempoReal.Porta is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"TempoReal:Porta precisa estar entre 0 e {ushort.MaxValue}."
            );
        }

        if (
            !IPAddress.TryParse(tempoReal.Endereco, out var ip)
            || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
        )
        {
            throw new InvalidOperationException(
                "TempoReal:Endereco precisa ser um endereço IPv4 (ex.: 0.0.0.0 ou 127.0.0.1)."
            );
        }

        if (string.IsNullOrEmpty(tempoReal.ChaveConexao))
        {
            throw new InvalidOperationException("TempoReal:ChaveConexao não pode ficar vazia.");
        }

        if (tempoReal.TempoParaAutenticarMs <= 0 || tempoReal.TempoQuedaMs <= 0)
        {
            throw new InvalidOperationException(
                "TempoReal:TempoParaAutenticarMs e TempoReal:TempoQuedaMs precisam ser maiores que zero."
            );
        }

        if (tempoReal.MensagensPorSegundo <= 0 || tempoReal.RajadaMensagens <= 0)
        {
            throw new InvalidOperationException(
                "TempoReal:MensagensPorSegundo e TempoReal:RajadaMensagens precisam ser maiores que zero."
            );
        }

        if (
            tempoReal.TamanhoMaximoPacote < ProtocoloTempoReal.TamanhoCabecalho
            || tempoReal.ProfundidadeMaximaJson <= 0
            || tempoReal.MaximoRecusasPorSessao <= 0
            || tempoReal.MaximoExcecoesPorSessao <= 0
            || tempoReal.MaximoTentativasAutenticacao <= 0
            || tempoReal.MaximoConexoesPendentesPorIp <= 0
            || tempoReal.JanelaRecusasMinutos <= 0
            || tempoReal.LimiteAlertaRecusasConta <= 0
        )
            throw new InvalidOperationException(
                "TempoReal: limites do protocolo precisam ser positivos e o pacote precisa comportar o cabe�alho."
            );
        return tempoReal;
    }
}
