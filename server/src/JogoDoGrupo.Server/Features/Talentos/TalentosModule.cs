using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Server.Features.Talentos;

public sealed class TalentosModule : IFeatureModule
{
    public string Nome => "Talentos";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        // A configuração é conferida aqui, antes de o app existir: árvore inválida não sobe.
        var arvore = ValidarOuFalhar(ArvoreDoJogo.Criar());
        servicos.AddSingleton(arvore);
        servicos.AddScoped<ServicoDaArvore>();
        servicos.AddSingleton<IAtributosDaArvore, AtributosDaArvoreDoPersonagem>();
        servicos.AddSingleton<IFonteDeModificadores, FonteDaArvore>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) => TalentosRotas.Mapear(rotas);

    public void Inicializar(ServerContext contexto) { }

    /// <summary>
    /// Devolve a configuração se ela é válida. Senão lança com o motivo de cada problema, o que
    /// impede o servidor de subir.
    /// </summary>
    public static ConfiguracaoDaArvore ValidarOuFalhar(ConfiguracaoDaArvore configuracao)
    {
        var problemas = ValidadorDaArvore.Validar(configuracao);
        problemas.AddRange(FonteDaArvore.ProblemasDeSecundarios(configuracao));
        if (problemas.Count > 0)
        {
            throw new InvalidOperationException(
                "A configuração da árvore de talentos é inválida: " + string.Join(" ", problemas)
            );
        }

        return configuracao;
    }
}
