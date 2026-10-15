namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Pergunta de Combate a quem sabe quem é aliado de quem (hoje o grupo, de Social): dois
/// personagens são aliados quando estão no mesmo grupo. Golpes e skills de área não tiram vida de
/// aliado (docs/features/combate.md). Combate só consulta; Social implementa a consulta sobre o
/// grupo em <c>AliadosDeCombateDoSocial</c>, e quando não houver implementação registrada (ou em
/// testes isolados) vale <see cref="NinguemEAliado"/>. Quem implementar registra com
/// <c>AddSingleton&lt;IAliadosDeCombate&gt;</c> (a última registrada vence). Pode ser chamada de
/// qualquer thread.
/// </summary>
public interface IAliadosDeCombate
{
    /// <summary>Verdadeiro se os dois personagens estão no mesmo grupo.</summary>
    bool SaoAliados(long personagemA, long personagemB);
}

/// <summary>Padrão enquanto o grupo não publica a consulta: ninguém é aliado de ninguém.</summary>
public sealed class NinguemEAliado : IAliadosDeCombate
{
    public bool SaoAliados(long personagemA, long personagemB) => false;
}
