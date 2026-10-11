namespace JogoDoGrupo.Server.Contratos.Personagem;

/// <summary>
/// Soma ou tira experiência de um personagem. Só o comando de teste de Desenvolvimento chama;
/// o ganho normal continua em <c>InimigoDerrotado</c>.
/// </summary>
public interface IAjusteDeExperienciaDeTeste
{
    Task<(bool Aceito, string Texto)> AjustarAsync(
        long personagemId,
        long quantidade,
        CancellationToken cancelamento
    );
}
