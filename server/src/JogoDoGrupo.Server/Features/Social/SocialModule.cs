using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Social no servidor. Por enquanto, o grupo de jogadores (convite, lista, líder, sair, expulsar,
/// passar liderança e desconexão). As regras vivem em <see cref="GruposDoServidor"/>; aqui só se
/// liga o tempo real a elas. A identidade de quem pede vem sempre da sessão autenticada.
/// </summary>
public sealed class SocialModule : IFeatureModule
{
    private readonly DiretorioTardio _diretorio = new();
    private GruposDoServidor? _grupos;

    public string Nome => "Social";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        // O módulo não recebe o provedor de serviços; o host cria este serviço antes de abrir a
        // porta UDP, então o diretório já está ligado quando a primeira mensagem chegar.
        servicos.AddHostedService(sp =>
        {
            _diretorio.Ligar(sp.GetRequiredService<IDiretorioDePersonagens>());
            return new VarreduraDosGrupos(
                _grupos
                    ?? throw new InvalidOperationException(
                        "Social: Inicializar precisa rodar antes de o host subir."
                    ),
                sp.GetRequiredService<ILogger<VarreduraDosGrupos>>()
            );
        });

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(SocialMessageIds.Faixa)
            .Ao<ConvidarParaGrupo>(
                SocialMessageIds.ConvidarParaGrupo,
                (sessao, pedido) => _grupos!.Convidar(sessao.PersonagemId, pedido.Nome)
            )
            .Ao<ResponderConvite>(
                SocialMessageIds.ResponderConvite,
                (sessao, pedido) =>
                    _grupos!.Responder(sessao.PersonagemId, pedido.ConviteId, pedido.Aceitar)
            )
            .Ao<SairDoGrupo>(
                SocialMessageIds.SairDoGrupo,
                (sessao, _) => _grupos!.Sair(sessao.PersonagemId)
            )
            .Ao<ExpulsarDoGrupo>(
                SocialMessageIds.ExpulsarDoGrupo,
                (sessao, pedido) => _grupos!.Expulsar(sessao.PersonagemId, pedido.PersonagemId)
            )
            .Ao<PassarLideranca>(
                SocialMessageIds.PassarLideranca,
                (sessao, pedido) =>
                    _grupos!.PassarLideranca(sessao.PersonagemId, pedido.PersonagemId)
            );

    public void Inicializar(ServerContext contexto)
    {
        var saida = new SaidaTempoReal(contexto.TempoReal);
        var grupos = new GruposDoServidor(_diretorio, saida);
        _grupos = grupos;

        contexto.TempoReal.SessaoAutenticada += sessao =>
        {
            saida.Registrar(sessao);
            grupos.AoConectar(sessao.PersonagemId);
        };
        contexto.TempoReal.SessaoEncerrada += sessao =>
        {
            // Sessão já substituída por outra do mesmo personagem: ele continua online.
            if (saida.Remover(sessao))
            {
                grupos.AoDesconectar(sessao.PersonagemId);
            }
        };
        foreach (var sessao in contexto.TempoReal.Sessoes)
        {
            saida.Registrar(sessao);
            grupos.AoConectar(sessao.PersonagemId);
        }
    }

    /// <summary>
    /// O diretório de personagens só existe depois que o app é montado; este repassa as consultas
    /// a ele quando for ligado (antes disso, nenhum personagem é encontrado).
    /// </summary>
    private sealed class DiretorioTardio : IDiretorioDePersonagens
    {
        private volatile IDiretorioDePersonagens? _real;

        public void Ligar(IDiretorioDePersonagens real) => _real = real;

        public bool TentarObterPorNome(string nome, out long personagemId)
        {
            personagemId = 0;
            return _real?.TentarObterPorNome(nome, out personagemId) ?? false;
        }

        public bool TentarObterNome(long personagemId, out string nome)
        {
            nome = "";
            return _real?.TentarObterNome(personagemId, out nome) ?? false;
        }
    }
}
