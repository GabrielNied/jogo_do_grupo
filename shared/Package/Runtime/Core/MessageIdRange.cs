namespace JogoDoGrupo.Shared.Core
{
    /// <summary>
    /// Faixa de IDs de mensagem reservada para uma funcionalidade.
    /// Cada funcionalidade usa apenas IDs dentro da própria faixa, assim ninguém
    /// precisa editar um arquivo central de IDs (e não há conflito de merge).
    /// </summary>
    public readonly struct MessageIdRange
    {
        public ushort Inicio { get; }
        public ushort Fim { get; }

        public MessageIdRange(ushort inicio, ushort fim)
        {
            Inicio = inicio;
            Fim = fim;
        }

        public bool Contem(ushort id) => id >= Inicio && id <= Fim;
    }
}
