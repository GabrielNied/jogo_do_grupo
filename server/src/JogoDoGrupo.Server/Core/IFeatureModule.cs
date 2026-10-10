namespace JogoDoGrupo.Server.Core;

/// <summary>
/// Ponto de entrada de cada funcionalidade no servidor.
/// O Program encontra todos os módulos automaticamente, então adicionar uma
/// funcionalidade nova não exige editar nenhum arquivo central.
/// </summary>
public interface IFeatureModule
{
    string Nome { get; }

    void Inicializar(ServerContext contexto);
}
