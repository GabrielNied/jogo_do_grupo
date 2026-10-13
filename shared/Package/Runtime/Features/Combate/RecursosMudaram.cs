namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor envia ao dono os recursos do próprio personagem quando algum muda (gasto de
    /// stamina, regeneração, dano). São os mesmos números de <see cref="RecursosResponse"/>, sem os
    /// atributos. O client só mostra.
    /// Id: <see cref="CombateMessageIds.RecursosMudaram"/>.
    /// </summary>
    [System.Serializable]
    public class RecursosMudaram
    {
        public int VidaAtual = 0;
        public int VidaMaxima = 0;
        public int StaminaAtual = 0;
        public int StaminaMaxima = 0;
        public int ManaAtual = 0;
        public int ManaMaxima = 0;
    }
}
