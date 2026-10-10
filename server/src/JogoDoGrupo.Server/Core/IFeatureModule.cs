namespace JogoDoGrupo.Server.Core;

/// <summary>
/// Ponto de entrada de cada funcionalidade no servidor.
/// O Program encontra todos os módulos automaticamente, então adicionar uma
/// funcionalidade nova não exige editar nenhum arquivo central.
/// Os métodos com corpo vazio são opcionais: o módulo só sobrescreve os que usa.
/// Ordem de chamada: <see cref="ConfigurarServicos"/>, <see cref="MapearRotas"/>,
/// <see cref="Inicializar"/>. Veja "Servidor: como uma funcionalidade se pluga" em
/// docs/arquitetura.md.
/// </summary>
public interface IFeatureModule
{
    string Nome { get; }

    /// <summary>
    /// Registra os serviços da funcionalidade na injeção de dependência
    /// (ex.: <c>servicos.AddScoped&lt;ServicoPersonagens&gt;()</c>). Roda antes do app ser montado.
    /// </summary>
    void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) { }

    /// <summary>
    /// Mapeia as rotas HTTP da funcionalidade
    /// (ex.: <c>rotas.MapGet("/api/x", ...).RequireAuthorization()</c>).
    /// </summary>
    void MapearRotas(IEndpointRouteBuilder rotas) { }

    void Inicializar(ServerContext contexto);
}
