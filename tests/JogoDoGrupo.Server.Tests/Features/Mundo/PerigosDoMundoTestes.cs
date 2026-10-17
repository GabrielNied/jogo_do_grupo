using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public class PerigosDoMundoTestes
{
    private sealed class Conexao;

    private sealed class DanoAoPersonagemGravador : IDanoAoPersonagem
    {
        public List<(long PersonagemId, int Dano, string Origem, string Area)> Chamadas { get; } =
        [];

        public void AplicarDanoDeAmbiente(long personagemId, int dano, string origem, string area)
        {
            Chamadas.Add((personagemId, dano, origem, area));
        }
    }

    private readonly Conexao _conexao = new();
    private readonly CidadeDoMundo<Conexao> _cidade;
    private readonly DanoAoPersonagemGravador _dano = new();
    private readonly EventosDoServidor _eventos = new();
    private readonly ServicoDePerigosDoMundo _perigos;
    private long _agora = 1_000_000;

    public PerigosDoMundoTestes()
    {
        _cidade = new CidadeDoMundo<Conexao>(() => _agora);
        _perigos = new ServicoDePerigosDoMundo(
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
            _dano,
            _eventos,
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
    public void Personagem_sobre_lava_leva_dano_no_intervalo_certo_e_fora_dela_nao()
    {
        const long id = 10;
        // Ponto de rio de lava confirmado no mapa
        float lavaX = 0f,
            lavaZ = 0f;
        var achou = false;
        for (var x = 500f; x <= 1200f && !achou; x += 15f)
        {
            for (var z = -1400f; z <= -800f && !achou; z += 15f)
            {
                if (ConfigPerigosDoMundo.EstaNaLava(x, z))
                {
                    lavaX = x;
                    lavaZ = z;
                    achou = true;
                }
            }
        }
        Assert.True(achou);

        _cidade.Entrar(id, _conexao, Pos(lavaX, lavaZ));

        // Primeiro tick: leva dano
        _perigos.Passo(_agora);
        Assert.Single(_dano.Chamadas);
        Assert.Equal(id, _dano.Chamadas[0].PersonagemId);
        Assert.Equal(ConfigPerigosDoMundo.DanoPorSegundoLava, _dano.Chamadas[0].Dano);
        Assert.Equal("Lava", _dano.Chamadas[0].Origem);

        // Meio segundo depois: ainda não passou 1s, sem novo dano
        _agora += 500;
        _perigos.Passo(_agora);
        Assert.Single(_dano.Chamadas);

        // 1 segundo completo após o primeiro dano: novo dano aplicado
        _agora += 500;
        _perigos.Passo(_agora);
        Assert.Equal(2, _dano.Chamadas.Count);

        // Move para a cidade (fora da lava): dano cessa
        _cidade.Mover(id, _conexao, Pos(0f, 0f));
        _agora += 1000;
        _perigos.Passo(_agora);
        Assert.Equal(2, _dano.Chamadas.Count);
    }

    [Fact]
    public void No_pantano_a_velocidade_validada_cai_para_a_fracao_escolhida_e_volta_ao_sair()
    {
        const long id = 20;
        float pantanoX = 0f,
            pantanoZ = 0f;
        var achou = false;
        for (var x = 800f; x <= 1300f && !achou; x += 15f)
        {
            for (var z = 0f; z <= 500f && !achou; z += 15f)
            {
                if (ConfigPerigosDoMundo.EstaNoPantano(x, z))
                {
                    pantanoX = x;
                    pantanoZ = z;
                    achou = true;
                }
            }
        }
        Assert.True(achou);

        _cidade.Entrar(id, _conexao, Pos(pantanoX, pantanoZ));
        _perigos.Passo(_agora);

        // No pântano: deslocamento acima da velocidade reduzida é recusado
        _agora += 500;
        var fator = ConfigPerigosDoMundo.FatorVelocidadePantano;
        var alcancePermitido = ValidadorDeMovimento.AlcanceNoTeto(
            ValidadorDeMovimento.VelocidadeMaxima * fator,
            500
        );

        // Movimento além do permitido no teto deve ser recusado
        var resultadoExcessivo = _cidade
            .Mover(id, _conexao, Pos(pantanoX, pantanoZ + (float)alcancePermitido + 0.5f))
            .Resultado;
        Assert.Equal(ResultadoMovimento.Recusado, resultadoExcessivo);

        // Movimento dentro do teto reduzido é aceito
        var resultadoValido = _cidade
            .Mover(id, _conexao, Pos(pantanoX, pantanoZ + (float)(alcancePermitido * 0.8)))
            .Resultado;
        Assert.Equal(ResultadoMovimento.Aceito, resultadoValido);

        // Sai do pântano para o campo: teto é removido
        _perigos.VerificarPasso(id, 160f, -180f);
        var seq = _cidade.ForcarPosicao(id, Pos(160f, -180f));
        _cidade.ConfirmarPosicaoForcada(id, _conexao, seq!.Value);
        _agora += 500;

        // Fora do pântano, pode se mover a 100% da velocidade máxima
        var alcanceCheio = ValidadorDeMovimento.VelocidadeMaxima * 0.5f;
        var resultadoFora = _cidade
            .Mover(id, _conexao, Pos(160f, -180f + (float)(alcanceCheio * 0.8)))
            .Resultado;
        Assert.Equal(ResultadoMovimento.Aceito, resultadoFora);
    }

    [Fact]
    public void Frio_e_calor_nao_causam_dano_nem_limitam_velocidade()
    {
        const long idNeve = 31;
        const long idDeserto = 32;

        _cidade.Entrar(idNeve, _conexao, Pos(-920f, 1030f));
        _cidade.Entrar(idDeserto, _conexao, Pos(-1080f, -680f));

        _perigos.Passo(_agora);
        _agora += 500;
        _perigos.Passo(_agora);

        Assert.Empty(_dano.Chamadas);

        // Velocidade cheia é permitida na neve e no deserto
        var alcanceCheio = ValidadorDeMovimento.VelocidadeMaxima * 0.5f;
        var rNeve = _cidade
            .Mover(idNeve, _conexao, Pos(-920f, 1030f + (float)(alcanceCheio * 0.8)))
            .Resultado;
        var rDeserto = _cidade
            .Mover(idDeserto, _conexao, Pos(-1080f, -680f + (float)(alcanceCheio * 0.8)))
            .Resultado;

        Assert.Equal(ResultadoMovimento.Aceito, rNeve);
        Assert.Equal(ResultadoMovimento.Aceito, rDeserto);
    }
}
