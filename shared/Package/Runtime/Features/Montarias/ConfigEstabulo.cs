namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Configuração e balanceamento do estábulo de Asterra (#1067).
    /// </summary>
    public static class ConfigEstabulo
    {
        public const long PrecoMulaDeCarga = 80;
        public const long PrecoCavaloComum = 150;

        public static readonly MontariaEstabuloDto[] Ofertas = new[]
        {
            new MontariaEstabuloDto(
                RegrasMontaria.TipoMulaDeCarga,
                "Mula de Carga",
                PrecoMulaDeCarga,
                "Companheiro rústico e barato para viagens curtas."
            ),
            new MontariaEstabuloDto(
                RegrasMontaria.TipoCavaloComum,
                "Cavalo Comum",
                PrecoCavaloComum,
                "Cavalo manso e fiel, treinado no estábulo da Brasa Mansa."
            ),
        };

        public static bool EstaAVenda(int tipoMontaria)
        {
            for (var i = 0; i < Ofertas.Length; i++)
            {
                if (Ofertas[i].TipoMontaria == tipoMontaria)
                {
                    return true;
                }
            }

            return false;
        }

        public static long ObterPreco(int tipoMontaria)
        {
            for (var i = 0; i < Ofertas.Length; i++)
            {
                if (Ofertas[i].TipoMontaria == tipoMontaria)
                {
                    return Ofertas[i].PrecoCoroas;
                }
            }

            return 0;
        }

        public static MontariaEstabuloDto ObterOferta(int tipoMontaria)
        {
            for (var i = 0; i < Ofertas.Length; i++)
            {
                if (Ofertas[i].TipoMontaria == tipoMontaria)
                {
                    return Ofertas[i];
                }
            }

            return null;
        }
    }
}
