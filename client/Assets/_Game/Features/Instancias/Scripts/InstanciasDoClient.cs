namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>Estado compartilhado da funcionalidade enquanto o jogo roda.</summary>
    internal static class InstanciasDoClient
    {
        private static EstadoDeInstancias _estado;

        public static EstadoDeInstancias Estado
        {
            get
            {
                if (_estado == null)
                {
                    _estado = new EstadoDeInstancias(new ConexaoDeInstanciasReal());
                }

                return _estado;
            }
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad
        )]
        private static void Iniciar()
        {
            _ = Estado;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration
        )]
        private static void Reiniciar()
        {
            _estado?.Dispose();
            _estado = null;
        }
    }
}
