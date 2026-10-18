using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Shared.Features.Desenvolvimento;

namespace JogoDoGrupo.Server.Features.Desenvolvimento;

/// <summary>
/// Executa as ações da tela de admin (F8). Quem chama já conferiu a chave de desenvolvimento:
/// aqui o pedido é validado campo a campo (<see cref="RegrasAdmin.Validar"/>) e cada ação vai
/// para o contrato da funcionalidade dona, que continua decidindo o resultado.
/// </summary>
public sealed class ExecutorDeAcoesDeAdmin(
    IServiceScopeFactory escopos,
    IAjusteDeExperienciaDeTeste experiencia,
    ITeleporteDeTeste teleporte,
    IInimigosDeTeste inimigos
)
{
    public async Task<(bool Aceito, string Texto)> ExecutarAsync(
        long personagemId,
        PedirAcaoDeAdmin pedido,
        CancellationToken cancelamento
    )
    {
        var recusa = RegrasAdmin.Validar(pedido);
        if (recusa != null)
        {
            return (false, recusa);
        }

        switch (pedido.Acao)
        {
            case AcaoDeAdmin.AjustarExperiencia:
                return await experiencia.AjustarAsync(personagemId, pedido.Valor, cancelamento);
            case AcaoDeAdmin.Teleportar:
                return teleporte.Teleportar(personagemId, pedido.X, pedido.Y, pedido.Z);
            case AcaoDeAdmin.SpawnarInimigo:
                return inimigos.SpawnarEspecie(personagemId, pedido.EspecieId, pedido.Quantidade);
            case AcaoDeAdmin.MatarInimigosDaArea:
                return inimigos.MatarDaArea(personagemId);
        }

        // As demais mexem no banco: um escopo por pedido, com o contexto e as transações dele.
        await using var escopo = escopos.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;
        switch (pedido.Acao)
        {
            case AcaoDeAdmin.AjustarCoroas:
                return await servicos
                    .GetRequiredService<IAjusteDeCoroasDeTeste>()
                    .AjustarAsync(personagemId, pedido.Valor, cancelamento);
            case AcaoDeAdmin.GerarItem:
                return await servicos
                    .GetRequiredService<IFerramentasDeItensDeTeste>()
                    .GerarAsync(
                        personagemId,
                        pedido.ModeloId,
                        pedido.Grau,
                        pedido.Origem,
                        pedido.Quantidade,
                        cancelamento
                    );
            case AcaoDeAdmin.CreditarEssencias:
                return await servicos
                    .GetRequiredService<IFerramentasDeItensDeTeste>()
                    .CreditarEssenciasAsync(personagemId, (int)pedido.Valor, cancelamento);
            case AcaoDeAdmin.EquiparUltimoItem:
                return await servicos
                    .GetRequiredService<IFerramentasDeItensDeTeste>()
                    .EquiparUltimoGeradoAsync(personagemId, cancelamento);
            case AcaoDeAdmin.LimparBolsa:
                return await servicos
                    .GetRequiredService<IFerramentasDeItensDeTeste>()
                    .LimparBolsaAsync(personagemId, cancelamento);
            case AcaoDeAdmin.QuebrarEquipados:
            case AcaoDeAdmin.ConsertarEquipados:
                return await servicos
                    .GetRequiredService<IFerramentasDeItensDeTeste>()
                    .AjustarDurabilidadeDosEquipadosAsync(
                        personagemId,
                        pedido.Acao == AcaoDeAdmin.QuebrarEquipados,
                        cancelamento
                    );
            default:
                return (false, "Ação desconhecida.");
        }
    }
}
