namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Regras puras e validações de Barracas de Jogador e Vendedor Offline (#905).
    /// </summary>
    public static class RegrasBarraca
    {
        public static bool PodeMontarBarraca(float x, float z, out string codigoErro)
        {
            if (!ConfigBarraca.DentroDaPraca(x, z))
            {
                codigoErro = CodigosErroEconomia.ForaDaPraca;
                return false;
            }

            codigoErro = null;
            return true;
        }

        public static bool PodeContratarVendedorOffline(
            int dias,
            float x,
            float z,
            long saldoCoroas,
            out string codigoErro,
            out long custoTotal
        )
        {
            custoTotal = 0;
            if (dias <= 0 || dias > ConfigBarraca.DiasMaximosContratacao)
            {
                codigoErro = CodigosErroEconomia.DiariaInvalida;
                return false;
            }

            if (!ConfigBarraca.DentroDaPraca(x, z))
            {
                codigoErro = CodigosErroEconomia.ForaDaPraca;
                return false;
            }

            custoTotal = dias * ConfigBarraca.DiariaVendedorOfflineCoroas;
            if (saldoCoroas < custoTotal)
            {
                codigoErro = CodigosErroEconomia.SaldoInsuficiente;
                return false;
            }

            codigoErro = null;
            return true;
        }

        public static bool PodeInteragirComBarraca(
            float jogadorX,
            float jogadorZ,
            float barracaX,
            float barracaZ
        )
        {
            float dx = jogadorX - barracaX;
            float dz = jogadorZ - barracaZ;
            return (dx * dx + dz * dz)
                <= (
                    ConfigBarraca.RaioInteracaoBarracaMetros
                    * ConfigBarraca.RaioInteracaoBarracaMetros
                );
        }
    }
}
