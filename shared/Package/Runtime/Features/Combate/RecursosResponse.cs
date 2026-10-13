namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor responde (200) a <c>GET</c> <see cref="RotasCombate.Recursos"/> com os recursos
    /// atuais e máximos do personagem e os atributos finais. O client só mostra.
    /// </summary>
    [System.Serializable]
    public class RecursosResponse
    {
        public int VidaAtual;
        public int VidaMaxima;
        public int StaminaAtual;
        public int StaminaMaxima;
        public int ManaAtual;
        public int ManaMaxima;

        /// <summary>
        /// Os cinco atributos <b>do personagem</b> (base + pontos + efeitos). O que os equipamentos
        /// dão fica separado em <see cref="DoEquipamento"/> e nos secundários.
        /// </summary>
        public AtributosCombate Atributos = new AtributosCombate();

        /// <summary>
        /// As linhas do que as peças ativas dão ao combate, já convertidas ("Dano da arma: 24 de
        /// dano físico"), para a janela do personagem mostrar separadas das do personagem.
        /// </summary>
        public string[] DoEquipamento = new string[0];

        /// <summary>Os nove secundários, na ordem de <see cref="RegrasSecundarios.Todos"/>, com a origem de cada parcela.</summary>
        public SecundarioCalculado[] Secundarios = new SecundarioCalculado[0];
    }
}
