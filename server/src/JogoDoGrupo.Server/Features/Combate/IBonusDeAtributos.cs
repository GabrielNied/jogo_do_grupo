using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Fonte dos bônus de atributos que vêm de fora de Combate (equipamentos e efeitos). Quem
/// calcula os bônus devolve o <b>total atual</b> de cada fonte, nunca um acréscimo: o servidor
/// soma uma vez só, então o mesmo bônus não é contado duas vezes. Itens e efeitos trocam a
/// implementação por esta interface; equipamentos já consultam o contrato publicado por Itens.
/// </summary>
public interface IBonusDeAtributos
{
    /// <summary>Soma dos bônus dos equipamentos que o personagem está usando agora.</summary>
    Task<AtributosCombate> EquipamentosAsync(long personagemId, CancellationToken cancelamento);

    /// <summary>Soma dos bônus dos efeitos ativos no personagem agora.</summary>
    Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken cancelamento);
}

/// <summary>Adapta os totais atuais de Itens; efeitos continuam sem bônus até sua implementação.</summary>
internal sealed class BonusDeEquipamentos(
    JogoDoGrupo.Server.Contratos.Itens.IBonusEquipamentos equipamentos
) : IBonusDeAtributos
{
    public async Task<AtributosCombate> EquipamentosAsync(long personagemId, CancellationToken c)
    {
        var total = new AtributosCombate();
        foreach (var atributo in await equipamentos.ObterAsync(personagemId, c))
        {
            var valor = checked((int)Math.Floor(atributo.Valor));
            switch (atributo.Status)
            {
                case JogoDoGrupo.Shared.Features.Itens.StatusItem.Forca:
                    total.Forca = valor;
                    break;
                case JogoDoGrupo.Shared.Features.Itens.StatusItem.Destreza:
                    total.Agilidade = valor;
                    break;
                case JogoDoGrupo.Shared.Features.Itens.StatusItem.Intelecto:
                    total.Intelecto = valor;
                    break;
                case JogoDoGrupo.Shared.Features.Itens.StatusItem.Vitalidade:
                    total.Vigor = valor;
                    break;
                case JogoDoGrupo.Shared.Features.Itens.StatusItem.Espirito:
                    total.Espirito = valor;
                    break;
            }
        }
        return total;
    }

    public Task<AtributosCombate> EfeitosAsync(long personagemId, CancellationToken c) =>
        Task.FromResult(new AtributosCombate());
}
