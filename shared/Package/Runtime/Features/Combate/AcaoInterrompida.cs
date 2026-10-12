namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área que um acerto interrompeu a ação de um personagem
    /// (<see cref="Jogador"/> verdadeiro, <see cref="AlvoId"/> é o id do personagem) ou o
    /// preparo de um inimigo comum (<see cref="AlvoId"/> é o id do inimigo). Quem foi
    /// interrompido não esquiva nem ataca por <see cref="DuracaoMs"/>
    /// (<see cref="RegrasReacao.AtordoamentoNoAcertoMs"/>).
    /// Id: <see cref="CombateMessageIds.AcaoInterrompida"/>.
    /// </summary>
    [System.Serializable]
    public class AcaoInterrompida
    {
        public long AlvoId = 0;
        public bool Jogador = false;
        public int DuracaoMs = 0;
    }
}
