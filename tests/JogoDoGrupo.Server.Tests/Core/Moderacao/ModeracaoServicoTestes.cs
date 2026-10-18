using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Moderacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Moderacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Core.Moderacao;

public class ModeracaoServicoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private sealed class TeleporteFalso : ITeleporteMundo
    {
        public long UltimoPersonagemId;
        public float UltimoX;
        public float UltimoY;
        public float UltimoZ;
        public bool Aceitar = true;

        public (bool Aceito, string Texto) Teleportar(long personagemId, float x, float y, float z)
        {
            UltimoPersonagemId = personagemId;
            UltimoX = x;
            UltimoY = y;
            UltimoZ = z;
            return Aceitar ? (true, "Teleportado.") : (false, "Posição recusada.");
        }
    }

    private sealed class DiretorioFalso : IDiretorioDePersonagens
    {
        private readonly Dictionary<string, (long PersonagemId, long ContaId)> _porNome = new(
            StringComparer.OrdinalIgnoreCase
        );

        public void Adicionar(string nome, long personagemId, long contaId)
        {
            _porNome[nome] = (personagemId, contaId);
        }

        public bool TentarObterPorNome(string nome, out long personagemId)
        {
            if (_porNome.TryGetValue(nome, out var dados))
            {
                personagemId = dados.PersonagemId;
                return true;
            }
            personagemId = 0;
            return false;
        }

        public bool TentarObterNome(long personagemId, out string nome)
        {
            nome = "";
            return false;
        }

        public bool TentarObterRaca(long personagemId, out int raca)
        {
            raca = 0;
            return false;
        }

        public bool TentarObterContaDoPersonagem(long personagemId, out long contaId)
        {
            foreach (var kvp in _porNome.Values)
            {
                if (kvp.PersonagemId == personagemId)
                {
                    contaId = kvp.ContaId;
                    return true;
                }
            }
            contaId = 0;
            return false;
        }
    }

    private sealed class PosicoesFalsas : IPosicoesDoMundo
    {
        private readonly Dictionary<long, PosicaoNoMundo> _posicoes = [];

        public void Definir(long personagemId, PosicaoNoMundo posicao) =>
            _posicoes[personagemId] = posicao;

        public bool TentarObter(long personagemId, out PosicaoNoMundo posicao) =>
            _posicoes.TryGetValue(personagemId, out posicao);

        public IReadOnlyList<long> PersonagensNaArea(string area) => [];
    }

    private sealed class SilenciadorFalso : ISilenciadorDoChat
    {
        public long UltimoPersonagemId;
        public TimeSpan UltimaDuracao;

        public void Silenciar(long personagemId, TimeSpan duracao)
        {
            UltimoPersonagemId = personagemId;
            UltimaDuracao = duracao;
        }
    }

    private (
        ServicoDeModeracao Servico,
        TeleporteFalso Teleporte,
        DiretorioFalso Diretorio,
        PosicoesFalsas Posicoes,
        SilenciadorFalso Silenciador
    ) CriarCenario()
    {
        var teleporte = new TeleporteFalso();
        var diretorio = new DiretorioFalso();
        var posicoes = new PosicoesFalsas();
        var silenciador = new SilenciadorFalso();

        var servicos = new ServiceCollection();
        servicos.AddSingleton<ITeleporteMundo>(teleporte);
        servicos.AddSingleton<IDiretorioDePersonagens>(diretorio);
        servicos.AddSingleton<IPosicoesDoMundo>(posicoes);
        servicos.AddSingleton<ISilenciadorDoChat>(silenciador);
        servicos.AddScoped(_ =>
            servidor.Services.CreateScope().ServiceProvider.GetRequiredService<JogoDbContext>()
        );

        var sp = servicos.BuildServiceProvider();
        var tempoReal = servidor.Services.GetRequiredService<ServidorTempoReal>();
        var servico = new ServicoDeModeracao(
            sp,
            tempoReal,
            NullLogger<ServicoDeModeracao>.Instance
        );

        return (servico, teleporte, diretorio, posicoes, silenciador);
    }

    private static SessaoTempoReal CriarSessao(long contaId, long personagemId, bool ehGm)
    {
        return new SessaoTempoReal(null!, 100, 100, TimeProvider.System)
        {
            ContaId = contaId,
            PersonagemId = personagemId,
            Autenticada = true,
            EhGm = ehGm,
        };
    }

    [Fact]
    public async Task Nao_gm_e_recusado_ao_tentar_qualquer_comando()
    {
        var (servico, _, _, _, _) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: false);

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "tp", Argumentos = "10 0 10" }
        );

        Assert.False(resultado.Sucesso);
        Assert.Contains("não tem permissão", resultado.Mensagem);
    }

    [Fact]
    public async Task Ajuda_retorna_lista_de_comandos()
    {
        var (servico, _, _, _, _) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: true);

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "ajuda" }
        );

        Assert.True(resultado.Sucesso);
        Assert.Contains("/tp", resultado.Mensagem);
        Assert.Contains("/puxar", resultado.Mensagem);
        Assert.Contains("/silenciar", resultado.Mensagem);
        Assert.Contains("/expulsar", resultado.Mensagem);
        Assert.Contains("/banir", resultado.Mensagem);
    }

    [Fact]
    public async Task Tp_por_coordenadas_teleporta_e_grava_auditoria()
    {
        var (servico, teleporte, _, _, _) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: true);

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "tp", Argumentos = "15.5 2.0 30.5" }
        );

        Assert.True(resultado.Sucesso);
        Assert.Equal(10, teleporte.UltimoPersonagemId);
        Assert.Equal(15.5f, teleporte.UltimoX);
        Assert.Equal(2.0f, teleporte.UltimoY);
        Assert.Equal(30.5f, teleporte.UltimoZ);

        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var auditoria = await db.Set<RegistroAuditoriaGm>()
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync(a => a.GmContaId == 1 && a.Acao == "tp");
        Assert.NotNull(auditoria);
        Assert.Equal("15.5 2 30.5", auditoria.Alvo);
    }

    [Fact]
    public async Task Tp_por_nome_de_jogador_teleporta_para_posicao_do_alvo()
    {
        var (servico, teleporte, diretorio, posicoes, _) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: true);

        diretorio.Adicionar("Guerreiro", 50, 5);
        posicoes.Definir(50, new PosicaoNoMundo("cidade", 100f, 5f, 200f, 0f));

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "tp", Argumentos = "Guerreiro" }
        );

        Assert.True(resultado.Sucesso);
        Assert.Equal(10, teleporte.UltimoPersonagemId);
        Assert.Equal(100f, teleporte.UltimoX);
        Assert.Equal(5f, teleporte.UltimoY);
        Assert.Equal(200f, teleporte.UltimoZ);
    }

    [Fact]
    public async Task Puxar_teleporta_jogador_alvo_para_coordenadas_do_gm()
    {
        var (servico, teleporte, diretorio, posicoes, _) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: true);

        diretorio.Adicionar("Ladrao", 60, 6);
        posicoes.Definir(10, new PosicaoNoMundo("cidade", 45f, 1f, 90f, 0f));

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "puxar", Argumentos = "Ladrao" }
        );

        Assert.True(resultado.Sucesso);
        Assert.Equal(60, teleporte.UltimoPersonagemId);
        Assert.Equal(45f, teleporte.UltimoX);
        Assert.Equal(1f, teleporte.UltimoY);
        Assert.Equal(90f, teleporte.UltimoZ);
    }

    [Fact]
    public async Task Silenciar_aplica_silencio_via_contrato_social()
    {
        var (servico, _, diretorio, _, silenciador) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: true);

        diretorio.Adicionar("Tagarela", 70, 7);

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "silenciar", Argumentos = "Tagarela 15" }
        );

        Assert.True(resultado.Sucesso);
        Assert.Equal(70, silenciador.UltimoPersonagemId);
        Assert.Equal(TimeSpan.FromMinutes(15), silenciador.UltimaDuracao);
    }

    [Fact]
    public async Task Banir_atualiza_conta_no_banco_e_desconecta()
    {
        var (servico, _, diretorio, _, _) = CriarCenario();
        var sessao = CriarSessao(1, 10, ehGm: true);

        long contaAlvoId;
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = new Conta
            {
                Usuario = "Infrator",
                UsuarioNormalizado = "INFRATOR",
                SenhaHash = "hash",
                CriadaEm = DateTime.UtcNow,
            };
            db.Set<Conta>().Add(conta);
            await db.SaveChangesAsync();
            contaAlvoId = conta.Id;
        }

        diretorio.Adicionar("Infrator", 80, contaAlvoId);

        var resultado = await servico.ProcessarComandoGmAsync(
            sessao,
            new PedirComandoGm { Comando = "banir", Argumentos = "Infrator 3 Uso de trapaça" }
        );

        Assert.True(resultado.Sucesso);

        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var contaAtualizada = await db.Set<Conta>().FindAsync(contaAlvoId);
            Assert.NotNull(contaAtualizada);
            Assert.NotNull(contaAtualizada.BanidaAte);
            Assert.True(contaAtualizada.BanidaAte > DateTime.UtcNow.AddDays(2));
            Assert.Equal("Uso de trapaça", contaAtualizada.MotivoBanimento);
        }
    }

    [Fact]
    public async Task Denuncia_valida_registra_no_banco_e_responde_sucesso()
    {
        var (servico, _, diretorio, _, _) = CriarCenario();
        var sessao = CriarSessao(2, 20, ehGm: false);

        diretorio.Adicionar("Suspeito", 90, 9);

        var resultado = await servico.ProcessarDenunciaAsync(
            sessao,
            new DenunciarJogador
            {
                AlvoNome = "Suspeito",
                Motivo = "trapaca",
                Detalhes = "Teleportando pelas paredes na dungeon",
            }
        );

        Assert.True(resultado.Sucesso);

        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var denuncia = await db.Set<Denuncia>()
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync(d => d.AutorContaId == 2 && d.AlvoNome == "Suspeito");

        Assert.NotNull(denuncia);
        Assert.Equal(20, denuncia.AutorPersonagemId);
        Assert.Equal(90, denuncia.AlvoPersonagemId);
        Assert.Equal(9, denuncia.AlvoContaId);
        Assert.Equal("trapaca", denuncia.Motivo);
        Assert.Equal("Teleportando pelas paredes na dungeon", denuncia.Detalhes);
    }

    [Fact]
    public async Task Denuncia_com_motivo_invalido_e_recusada()
    {
        var (servico, _, _, _, _) = CriarCenario();
        var sessao = CriarSessao(2, 20, ehGm: false);

        var resultado = await servico.ProcessarDenunciaAsync(
            sessao,
            new DenunciarJogador
            {
                AlvoNome = "Alguem",
                Motivo = "invalido",
                Detalhes = "Detalhes",
            }
        );

        Assert.False(resultado.Sucesso);
        Assert.Contains("inválido", resultado.Mensagem);
    }
}
