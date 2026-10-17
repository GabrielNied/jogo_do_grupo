using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Responsável por consultar fontes de modificadores, bônus e equipamentos, consolidando
/// o snapshot de atributos finais, valores secundários e linhas de exibição para combate.
/// </summary>
internal sealed class ResolvedorDeAtributosDeCombate
{
    private readonly IBonusDeAtributos _bonus;
    private readonly IAtributosDosEquipamentos _equipamentos;
    private readonly IAtributosDaArvore _pontos;
    private readonly IReadOnlyList<IFonteDeModificadores> _fontes;
    private readonly ILogger _log;

    public ResolvedorDeAtributosDeCombate(
        IBonusDeAtributos bonus,
        IAtributosDosEquipamentos equipamentos,
        IAtributosDaArvore pontos,
        IEnumerable<IFonteDeModificadores> fontes,
        ILogger log
    )
    {
        _bonus = bonus;
        _equipamentos = equipamentos;
        _pontos = pontos;
        _fontes = fontes.OrderBy(f => f.Fonte).ToList();
        _log = log;
    }

    public async Task<InstantaneoDeAtributos> ResolverAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var lidos = CalculadoraRecursos.AtributosFinais(
            await _bonus.EfeitosAsync(personagemId, cancelamento),
            await _pontos.ObterAsync(personagemId, cancelamento)
        );

        // O equipamento é uma fonte à parte: nunca soma nos cinco atributos do personagem.
        var equipamento = await _equipamentos.ObterAsync(personagemId, cancelamento);
        var maximos = CalculadoraRecursos.Maximos(lidos, equipamento);
        var secundarios = RegrasSecundarios.Calcular(
            await ModificadoresAsync(personagemId, cancelamento)
        );

        return new InstantaneoDeAtributos(
            Atributos: lidos,
            Maximos: maximos,
            Dano: CalculadoraRecursos.ParcelasDoDano(lidos, equipamento),
            LinhasDoEquipamento: ExtrairLinhas(equipamento.Linhas),
            Secundarios: secundarios,
            Valores: RegrasSecundarios.Valores(secundarios)
        );
    }

    internal static string[] ExtrairLinhas(LinhaDeEquipamento[]? linhas)
    {
        if (linhas == null || linhas.Length == 0)
        {
            return Array.Empty<string>();
        }

        var resultado = new string[linhas.Length];
        for (var i = 0; i < linhas.Length; i++)
        {
            resultado[i] = linhas[i].Texto;
        }

        return resultado;
    }

    // Junta o que as fontes publicam. Quem declara outra fonte que não a sua, ou a Base, é
    // recusado e registrado; falha de uma fonte não derruba a conta (o jogador fica sem aquela
    // fonte até a próxima leitura).
    private async Task<List<Modificador>> ModificadoresAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        var todos = new List<Modificador>();
        foreach (var fonte in _fontes)
        {
            try
            {
                foreach (var m in await fonte.ObterAsync(personagemId, cancelamento))
                {
                    if (m.Fonte != fonte.Fonte || m.Fonte == FonteDeModificador.Base)
                    {
                        _log.LogWarning(
                            "Modificador {Origem} recusado: a fonte {Fonte} declarou {Declarada}.",
                            m.Origem,
                            fonte.Fonte,
                            m.Fonte
                        );
                        continue;
                    }

                    if (!PipelineDeModificadores.EhValido(m))
                    {
                        _log.LogWarning(
                            "Modificador {Origem} recusado: sem grupo de acúmulo ou valor inválido.",
                            m.Origem
                        );
                        continue;
                    }

                    todos.Add(m);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _log.LogError(
                    e,
                    "Erro ao ler os modificadores da fonte {Fonte} do personagem {Personagem}.",
                    fonte.Fonte,
                    personagemId
                );
            }
        }

        return todos;
    }
}
