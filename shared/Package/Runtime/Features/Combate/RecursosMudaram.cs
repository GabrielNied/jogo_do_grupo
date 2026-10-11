namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor envia ao dono os recursos do próprio personagem quando algum muda (gasto de
    /// vigor, regeneração, dano). São os mesmos números de <see cref="RecursosResponse"/>, sem os
    /// atributos. O client só mostra.
    /// Id: <see cref="CombateMessageIds.RecursosMudaram"/>.
    /// </summary>
    [System.Serializable]
    public class RecursosMudaram
    {
        public int VidaAtual = 0;
        public int VidaMaxima = 0;
        public int VigorAtual = 0;
        public int VigorMaximo = 0;
        public int EnergiaAtual = 0;
        public int EnergiaMaxima = 0;
    }
}
