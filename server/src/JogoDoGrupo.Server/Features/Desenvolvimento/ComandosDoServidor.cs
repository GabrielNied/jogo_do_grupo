using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Shared.Features.Desenvolvimento;

namespace JogoDoGrupo.Server.Features.Desenvolvimento;

/// <summary>
/// Executa o texto do console. Quem chama já conferiu a chave de desenvolvimento: aqui o
/// comando vale, e o resultado (experiência, posição, spawn) continua com a funcionalidade dona.
/// </summary>
public sealed class ComandosDoServidor(
    IAjusteDeExperienciaDeTeste experiencia,
    ITeleporteDeTeste teleporte,
    ISpawnDeTeste spawn
)
{
    public Task<(bool Aceito, string Texto)> ExecutarAsync(
        long personagemId,
        string texto,
        CancellationToken cancelamento
    )
    {
        var comando = ComandosDeTeste.Interpretar(texto);
        return comando.Tipo switch
        {
            TipoDeComandoDeTeste.Ajuda => Task.FromResult((true, ComandosDeTeste.Ajuda)),
            TipoDeComandoDeTeste.Experiencia => experiencia.AjustarAsync(
                personagemId,
                comando.Quantidade,
                cancelamento
            ),
            TipoDeComandoDeTeste.Teleporte => Task.FromResult(
                teleporte.Teleportar(personagemId, comando.X, comando.Y, comando.Z)
            ),
            TipoDeComandoDeTeste.Spawn => Task.FromResult(spawn.SpawnarPerto(personagemId)),
            _ => Task.FromResult((false, comando.Aviso)),
        };
    }
}
