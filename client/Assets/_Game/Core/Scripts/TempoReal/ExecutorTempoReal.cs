using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Processa a rede de tempo real a cada frame e fecha a conexão ao sair do jogo. Criado pela
    /// <see cref="ConexaoTempoReal"/> na primeira conexão.
    /// </summary>
    internal sealed class ExecutorTempoReal : MonoBehaviour
    {
        private void Update()
        {
            ConexaoTempoReal.Atualizar(Time.unscaledDeltaTime);
        }

        private void OnApplicationQuit()
        {
            ConexaoTempoReal.AoFecharExecutor(this);
        }

        private void OnDestroy()
        {
            ConexaoTempoReal.AoFecharExecutor(this);
        }
    }
}
