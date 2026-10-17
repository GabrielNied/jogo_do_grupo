using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Estado em memória dos recursos e atributos de um personagem em uso.
/// </summary>
internal sealed class EntradaRecursosPersonagem(long personagemId)
{
    public long PersonagemId { get; } = personagemId;
    public SemaphoreSlim Trava { get; } = new(1, 1);
    public RecursosPersonagem? Recursos;
    public AtributosCombate? Atributos;
    public MaximosDeRecursos Maximos;
    public ParcelasDeDano? Dano;
    public string[] LinhasDoEquipamento = [];
    public SecundarioCalculado[]? Secundarios;
    public AtributosSecundarios? Valores;

    // Só em memória (sem migração): até onde a regeneração de vida já foi paga.
    public DateTime VidaContadaAte;
    public DateTimeOffset? AtributosEm;
    public DateTimeOffset UltimoUso = DateTimeOffset.UnixEpoch;
    public bool Sujo;
    public bool Guardando;
    public bool Descartada;

    public void Aplicar(InstantaneoDeAtributos atributos, DateTimeOffset agora)
    {
        Atributos = atributos.Atributos;
        Maximos = atributos.Maximos;
        Dano = atributos.Dano;
        LinhasDoEquipamento = atributos.LinhasDoEquipamento;
        Secundarios = atributos.Secundarios;
        Valores = atributos.Valores;
        AtributosEm = agora;
    }
}
