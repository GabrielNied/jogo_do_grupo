namespace JogoDoGrupo.Shared.Features.Conquistas
{
    public static class RegrasConquistas
    {
        public static bool PodeEquiparTitulo(string titulo, string[] titulosDesbloqueados)
        {
            if (string.IsNullOrWhiteSpace(titulo))
            {
                // Título vazio significa desequipar/nenhum título, o que é sempre permitido
                return true;
            }

            if (titulosDesbloqueados == null || titulosDesbloqueados.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < titulosDesbloqueados.Length; i++)
            {
                if (
                    string.Equals(
                        titulosDesbloqueados[i],
                        titulo.Trim(),
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        public static int SomarPontos(ProgressoConquista[] progressos)
        {
            if (progressos == null)
            {
                return 0;
            }

            int soma = 0;
            for (int i = 0; i < progressos.Length; i++)
            {
                if (
                    progressos[i].Concluida
                    && CatalogoDeConquistas.TentarObter(progressos[i].ConquistaId, out var def)
                )
                {
                    soma += def.Pontos;
                }
            }

            return soma;
        }
    }
}
