namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Regras puras de evolução e validação de conhecimento do bestiário (#922).
    /// </summary>
    public static class RegrasBestiario
    {
        public const int DerrotasAvistadoComum = 1;
        public const int DerrotasEstudadoComum = 5;
        public const int DerrotasMestreComum = 20;

        public const int DerrotasAvistadoChefe = 1;
        public const int DerrotasEstudadoChefe = 3;
        public const int DerrotasMestreChefe = 5;

        /// <summary>
        /// Calcula o nível de conhecimento baseado na categoria da criatura e no número de derrotas acumuladas.
        /// </summary>
        public static NivelDeConhecimentoBestiario CalcularNivel(
            CategoriaDeCriatura categoria,
            int derrotas
        )
        {
            if (derrotas <= 0)
            {
                return NivelDeConhecimentoBestiario.Desconhecido;
            }

            if (EhCategoriaEspecial(categoria))
            {
                if (derrotas >= DerrotasMestreChefe)
                {
                    return NivelDeConhecimentoBestiario.Mestre;
                }
                if (derrotas >= DerrotasEstudadoChefe)
                {
                    return NivelDeConhecimentoBestiario.Estudado;
                }
                return NivelDeConhecimentoBestiario.Avistado;
            }

            if (derrotas >= DerrotasMestreComum)
            {
                return NivelDeConhecimentoBestiario.Mestre;
            }
            if (derrotas >= DerrotasEstudadoComum)
            {
                return NivelDeConhecimentoBestiario.Estudado;
            }
            return NivelDeConhecimentoBestiario.Avistado;
        }

        /// <summary>
        /// Quantidade de derrotas necessárias para atingir determinado nível.
        /// </summary>
        public static int DerrotasParaNivel(
            CategoriaDeCriatura categoria,
            NivelDeConhecimentoBestiario nivel
        )
        {
            bool especial = EhCategoriaEspecial(categoria);
            switch (nivel)
            {
                case NivelDeConhecimentoBestiario.Mestre:
                    return especial ? DerrotasMestreChefe : DerrotasMestreComum;
                case NivelDeConhecimentoBestiario.Estudado:
                    return especial ? DerrotasEstudadoChefe : DerrotasEstudadoComum;
                case NivelDeConhecimentoBestiario.Avistado:
                    return especial ? DerrotasAvistadoChefe : DerrotasAvistadoComum;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Quantidade de derrotas necessárias para o próximo patamar de conhecimento.
        /// Se já for Mestre, retorna o teto do nível Mestre.
        /// </summary>
        public static int DerrotasParaProximoNivel(
            CategoriaDeCriatura categoria,
            int derrotasAtuais
        )
        {
            NivelDeConhecimentoBestiario nivelAtual = CalcularNivel(categoria, derrotasAtuais);
            switch (nivelAtual)
            {
                case NivelDeConhecimentoBestiario.Desconhecido:
                    return DerrotasParaNivel(categoria, NivelDeConhecimentoBestiario.Avistado);
                case NivelDeConhecimentoBestiario.Avistado:
                    return DerrotasParaNivel(categoria, NivelDeConhecimentoBestiario.Estudado);
                case NivelDeConhecimentoBestiario.Estudado:
                    return DerrotasParaNivel(categoria, NivelDeConhecimentoBestiario.Mestre);
                default:
                    return DerrotasParaNivel(categoria, NivelDeConhecimentoBestiario.Mestre);
            }
        }

        /// <summary>
        /// Calcula o progresso fracionário dentro da faixa atual para exibição em barras de progresso.
        /// </summary>
        public static void ObterProgressoNoNivel(
            CategoriaDeCriatura categoria,
            int derrotasAtuais,
            out int progressoNoNivel,
            out int metaDoNivel
        )
        {
            if (derrotasAtuais < 0)
            {
                derrotasAtuais = 0;
            }

            NivelDeConhecimentoBestiario nivelAtual = CalcularNivel(categoria, derrotasAtuais);
            if (nivelAtual == NivelDeConhecimentoBestiario.Mestre)
            {
                int max = DerrotasParaNivel(categoria, NivelDeConhecimentoBestiario.Mestre);
                progressoNoNivel = max;
                metaDoNivel = max;
                return;
            }

            int piso = DerrotasParaNivel(categoria, nivelAtual);
            int teto = DerrotasParaProximoNivel(categoria, derrotasAtuais);

            metaDoNivel = teto - piso;
            progressoNoNivel = derrotasAtuais - piso;
        }

        private static bool EhCategoriaEspecial(CategoriaDeCriatura categoria)
        {
            return categoria == CategoriaDeCriatura.Chefe
                || categoria == CategoriaDeCriatura.ChefeRaro
                || categoria == CategoriaDeCriatura.ChefeDeMundo;
        }
    }
}
