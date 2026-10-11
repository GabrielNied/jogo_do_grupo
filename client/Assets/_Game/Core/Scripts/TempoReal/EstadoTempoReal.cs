namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>Estado da conexão de tempo real (<see cref="ConexaoTempoReal"/>).</summary>
    public enum EstadoTempoReal
    {
        /// <summary>Sem conexão. Veja <see cref="ConexaoTempoReal.UltimoErro"/> para saber se caiu.</summary>
        Desconectado,

        /// <summary>Abrindo a conexão UDP com o servidor.</summary>
        Conectando,

        /// <summary>Conexão aberta, esperando o servidor aceitar o token da sessão.</summary>
        Autenticando,

        /// <summary>Autenticado: já dá para enviar e receber mensagens.</summary>
        Conectado,

        /// <summary>A conexão caiu depois de autenticada; esperando para tentar de novo.</summary>
        Reconectando,
    }
}
