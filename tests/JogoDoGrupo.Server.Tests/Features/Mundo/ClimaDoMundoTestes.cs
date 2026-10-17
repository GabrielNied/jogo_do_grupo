using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public class ClimaDoMundoTestes
{
    private sealed class Conexao;

    private readonly Conexao _conexao = new();
    private readonly CidadeDoMundo<Conexao> _cidade;
    private readonly ClimaDoServidor _clima;
    private readonly ServicoDeClimaDoMundo _servico;
    private long _agora = 1_000_000;

    public ClimaDoMundoTestes()
    {
        _cidade = new CidadeDoMundo<Conexao>(() => _agora);
        _clima = new ClimaDoServidor(() => _agora);
        _servico = new ServicoDeClimaDoMundo(
            _clima,
            null!,
            () =>
                _cidade
                    .Presentes()
                    .Select(a => new AvatarNaCidade<SessaoTempoReal>(
                        a.PersonagemId,
                        null!,
                        a.Posicao,
                        a.SequenciaReplica,
                        a.UltimoMs
                    ))
                    .ToArray(),
            new LimiteDeVelocidadeDaCidadeFake(_cidade),
            () => _agora
        );
    }

    private sealed class LimiteDeVelocidadeDaCidadeFake(CidadeDoMundo<Conexao> cidade)
        : ILimiteDeVelocidadeDoMundo
    {
        public bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos) =>
            cidade.LimitarVelocidade(personagemId, fator, duracaoSegundos);
    }

    private static PosicaoNaCidade Pos(float x, float z) =>
        new(x, MapaDoMundo.AlturaEm(x, z), z, 0f);

    [Fact]
    public void Clima_do_servidor_comeca_limpo_e_atualiza_apos_duracao()
    {
        Assert.Equal(TipoDeClima.Limpo, _clima.ClimaAtual);

        // Menos da duração: clima continua o mesmo
        var mudouAntes = _clima.Atualizar(_agora + 10_000);
        Assert.False(mudouAntes);
        Assert.Equal(TipoDeClima.Limpo, _clima.ClimaAtual);

        // Após a duração do clima atual: sorteia novo clima
        _agora += (_clima.DuracaoSegundos + 1) * 1000L;
        var mudouDepois = _clima.Atualizar(_agora);
        Assert.True(mudouDepois);
    }

    [Fact]
    public void Mensagem_de_clima_contem_valores_das_constantes()
    {
        _clima.DefinirClima(TipoDeClima.Nevasca, 240);
        var msg = _clima.Mensagem();

        Assert.Equal(TipoDeClima.Nevasca, msg.Clima);
        Assert.Equal(ConfigClima.FatorVelocidadeNevasca, msg.FatorVelocidade);
        Assert.Equal(ConfigClima.DistanciaVisaoNevasca, msg.DistanciaVisao);
        Assert.Equal(ConfigClima.DensidadeNevoaNevasca, msg.DensidadeNevoa);
        Assert.Equal(240, msg.DuracaoSegundos);
    }

    [Fact]
    public void Na_nevasca_personagem_na_neve_tem_velocidade_reduzida_e_no_campo_nao()
    {
        const long idNeve = 101;
        const long idCampo = 102;

        // Posição nas montanhas de neve (-920, 1030) e no campo (0, 100)
        _cidade.Entrar(idNeve, _conexao, Pos(-920f, 1030f));
        _cidade.Entrar(idCampo, _conexao, Pos(0f, 100f));

        _clima.DefinirClima(TipoDeClima.Nevasca, 300);
        _servico.AplicarEfeitosDeMovimento();

        _agora += 500;
        var fatorNevasca = ConfigClima.FatorVelocidadeNevasca;
        var alcancePermitidoNaNeve = ValidadorDeMovimento.AlcanceNoTeto(
            ValidadorDeMovimento.VelocidadeMaxima * fatorNevasca,
            500
        );

        // Na neve: deslocamento acima do teto de nevasca é recusado
        var excessoNeve = _cidade
            .Mover(idNeve, _conexao, Pos(-920f, 1030f + (float)alcancePermitidoNaNeve + 0.5f))
            .Resultado;
        Assert.Equal(ResultadoMovimento.Recusado, excessoNeve);

        // Na neve: deslocamento dentro do teto de nevasca é aceito
        var validoNeve = _cidade
            .Mover(idNeve, _conexao, Pos(-920f, 1030f + (float)(alcancePermitidoNaNeve * 0.8)))
            .Resultado;
        Assert.Equal(ResultadoMovimento.Aceito, validoNeve);

        // No campo: deslocamento normal a 100% da velocidade máxima é aceito mesmo sob nevasca global
        var alcanceCheio = ValidadorDeMovimento.VelocidadeMaxima * 0.5f;
        var validoCampo = _cidade
            .Mover(idCampo, _conexao, Pos(0f, 100f + (float)(alcanceCheio * 0.8)))
            .Resultado;
        Assert.Equal(ResultadoMovimento.Aceito, validoCampo);
    }

    [Fact]
    public void Quando_clima_muda_para_limpo_velocidade_na_neve_volta_ao_normal()
    {
        const long id = 201;
        _cidade.Entrar(id, _conexao, Pos(-920f, 1030f));

        _clima.DefinirClima(TipoDeClima.Nevasca, 300);
        _servico.AplicarEfeitosDeMovimento();

        // Clima muda para Limpo
        _clima.DefinirClima(TipoDeClima.Limpo, 300);
        _servico.AplicarEfeitosDeMovimento();

        _agora += 500;

        // Agora pode andar a 100% da velocidade máxima
        var alcanceCheio = ValidadorDeMovimento.VelocidadeMaxima * 0.5f;
        var resultado = _cidade
            .Mover(id, _conexao, Pos(-920f, 1030f + (float)(alcanceCheio * 0.8)))
            .Resultado;

        Assert.Equal(ResultadoMovimento.Aceito, resultado);
    }

    [Fact]
    public void Sair_do_bioma_de_neve_sob_nevasca_remove_teto_de_velocidade()
    {
        const long id = 301;
        _cidade.Entrar(id, _conexao, Pos(-920f, 1030f));

        _clima.DefinirClima(TipoDeClima.Nevasca, 300);
        _servico.AplicarEfeitosDeMovimento();

        // Move para o campo: VerificarPasso restaura a velocidade máxima
        _servico.VerificarPasso(id, 0f, 100f);
        var seq = _cidade.ForcarPosicao(id, Pos(0f, 100f));
        _cidade.ConfirmarPosicaoForcada(id, _conexao, seq!.Value);
        _agora += 500;

        var alcanceCheio = ValidadorDeMovimento.VelocidadeMaxima * 0.5f;
        var resultado = _cidade
            .Mover(id, _conexao, Pos(0f, 100f + (float)(alcanceCheio * 0.8)))
            .Resultado;

        Assert.Equal(ResultadoMovimento.Aceito, resultado);
    }
}
