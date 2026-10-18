using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public class ControleDeMarcadoresDeGrupoTestes
{
    private sealed class SaidaFalsa : ISaidaDeMarcadores
    {
        public readonly List<(
            long PersonagemId,
            MarcadoresDeAlvoMudaram Mensagem
        )> MarcadoresEnviados = new();
        public readonly List<(long PersonagemId, DefinirMarcadorRecusado Recusa)> RecusasEnviadas =
            new();

        public void Marcadores(long personagemId, MarcadoresDeAlvoMudaram mensagem) =>
            MarcadoresEnviados.Add((personagemId, mensagem));

        public void Recusada(long personagemId, DefinirMarcadorRecusado recusa) =>
            RecusasEnviadas.Add((personagemId, recusa));
    }

    private sealed class ConsultaGrupoFalsa : IConsultaDeGrupo
    {
        public readonly Dictionary<long, GrupoAtual> Grupos = new();

        public GrupoAtual? Consultar(long personagemId) =>
            Grupos.TryGetValue(personagemId, out var g) ? g : null;
    }

    [Fact]
    public void DefinirMarcador_JogadorSemGrupo_EnviaRecusaNaoEstaEmGrupo()
    {
        var saida = new SaidaFalsa();
        var consulta = new ConsultaGrupoFalsa();
        var controle = new ControleDeMarcadoresDeGrupo(
            consulta,
            saida,
            NullLogger<ControleDeMarcadoresDeGrupo>.Instance
        );

        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 50, Marcador = TipoMarcadorDeAlvo.Caveira }
        );

        Assert.Single(saida.RecusasEnviadas);
        Assert.Equal(100, saida.RecusasEnviadas[0].PersonagemId);
        Assert.Equal(CodigoErroMarcador.NaoEstaEmGrupo, saida.RecusasEnviadas[0].Recusa.Motivo);
        Assert.Empty(saida.MarcadoresEnviados);
    }

    [Fact]
    public void DefinirMarcador_AlvoInvalido_EnviaRecusaAlvoInvalido()
    {
        var saida = new SaidaFalsa();
        var consulta = new ConsultaGrupoFalsa();
        var grupo = new GrupoAtual(1, 100, new long[] { 100, 101 });
        consulta.Grupos[100] = grupo;

        var controle = new ControleDeMarcadoresDeGrupo(
            consulta,
            saida,
            NullLogger<ControleDeMarcadoresDeGrupo>.Instance
        );

        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 0, Marcador = TipoMarcadorDeAlvo.Caveira }
        );

        Assert.Single(saida.RecusasEnviadas);
        Assert.Equal(CodigoErroMarcador.AlvoInvalido, saida.RecusasEnviadas[0].Recusa.Motivo);
        Assert.Empty(saida.MarcadoresEnviados);
    }

    [Fact]
    public void DefinirMarcador_MembroDoGrupo_TransmiteMarcadorParaTodosOsMembros()
    {
        var saida = new SaidaFalsa();
        var consulta = new ConsultaGrupoFalsa();
        var grupo = new GrupoAtual(1, 100, new long[] { 100, 101, 102 });
        consulta.Grupos[100] = grupo;
        consulta.Grupos[101] = grupo;
        consulta.Grupos[102] = grupo;

        var controle = new ControleDeMarcadoresDeGrupo(
            consulta,
            saida,
            NullLogger<ControleDeMarcadoresDeGrupo>.Instance
        );

        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 42, Marcador = TipoMarcadorDeAlvo.Caveira }
        );

        Assert.Empty(saida.RecusasEnviadas);
        Assert.Equal(3, saida.MarcadoresEnviados.Count);

        foreach (var (_, mensagem) in saida.MarcadoresEnviados)
        {
            Assert.Single(mensagem.Marcadores);
            Assert.Equal(42, mensagem.Marcadores[0].AlvoId);
            Assert.Equal(TipoMarcadorDeAlvo.Caveira, mensagem.Marcadores[0].Marcador);
        }
    }

    [Fact]
    public void DefinirMarcador_MarcadorReatribuido_TransfereParaNovoAlvo()
    {
        var saida = new SaidaFalsa();
        var consulta = new ConsultaGrupoFalsa();
        var grupo = new GrupoAtual(1, 100, new long[] { 100 });
        consulta.Grupos[100] = grupo;

        var controle = new ControleDeMarcadoresDeGrupo(
            consulta,
            saida,
            NullLogger<ControleDeMarcadoresDeGrupo>.Instance
        );

        // Primeiro marca Alvo 42 como Caveira
        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 42, Marcador = TipoMarcadorDeAlvo.Caveira }
        );

        // Depois marca Alvo 43 como Caveira
        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 43, Marcador = TipoMarcadorDeAlvo.Caveira }
        );

        var snapshot = controle.ObterMarcadores(1);
        Assert.Single(snapshot);
        Assert.Equal(43, snapshot[0].AlvoId);
        Assert.Equal(TipoMarcadorDeAlvo.Caveira, snapshot[0].Marcador);
    }

    [Fact]
    public void DefinirMarcador_ComNenhum_RemoveMarcadorDoAlvo()
    {
        var saida = new SaidaFalsa();
        var consulta = new ConsultaGrupoFalsa();
        var grupo = new GrupoAtual(1, 100, new long[] { 100 });
        consulta.Grupos[100] = grupo;

        var controle = new ControleDeMarcadoresDeGrupo(
            consulta,
            saida,
            NullLogger<ControleDeMarcadoresDeGrupo>.Instance
        );

        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 42, Marcador = TipoMarcadorDeAlvo.Cruz }
        );
        Assert.Single(controle.ObterMarcadores(1));

        controle.DefinirMarcador(
            100,
            new DefinirMarcadorDeAlvo { AlvoId = 42, Marcador = TipoMarcadorDeAlvo.Nenhum }
        );
        Assert.Empty(controle.ObterMarcadores(1));
    }

    [Fact]
    public void DefinirMarcador_MembroNaoLider_EnviaRecusaApenasLiderPodeMarcar()
    {
        var saida = new SaidaFalsa();
        var consulta = new ConsultaGrupoFalsa();
        var grupo = new GrupoAtual(1, 100, new long[] { 100, 101 });
        consulta.Grupos[100] = grupo;
        consulta.Grupos[101] = grupo;

        var controle = new ControleDeMarcadoresDeGrupo(
            consulta,
            saida,
            NullLogger<ControleDeMarcadoresDeGrupo>.Instance
        );

        controle.DefinirMarcador(
            101, // membro comum, líder é 100
            new DefinirMarcadorDeAlvo { AlvoId = 50, Marcador = TipoMarcadorDeAlvo.Caveira }
        );

        Assert.Single(saida.RecusasEnviadas);
        Assert.Equal(101, saida.RecusasEnviadas[0].PersonagemId);
        Assert.Equal(
            CodigoErroMarcador.ApenasLiderPodeMarcar,
            saida.RecusasEnviadas[0].Recusa.Motivo
        );
        Assert.Empty(saida.MarcadoresEnviados);
    }
}
