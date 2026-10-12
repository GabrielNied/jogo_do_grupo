using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Toca a animação do golpe no Animator do visual. Os parâmetros são o combinado com a
    /// funcionalidade Personagem (tabela em <c>docs/features/personagem.md</c>): os Triggers
    /// <see cref="ParametroAtaqueLeve"/> e <see cref="ParametroAtaquePesado"/>, o inteiro
    /// <see cref="ParametroPasso"/> (qual dos três leves da cadeia) e o decimal
    /// <see cref="ParametroVelocidadeDoGolpe"/> (a velocidade do clipe, para o contato cair no fim do
    /// preparo). Animator sem o parâmetro (ou sem Animator) é ignorado, sem erro.
    /// </summary>
    internal static class AnimadorDeAtaque
    {
        public const string ParametroAtaqueLeve = "AtaqueLeve";
        public const string ParametroAtaquePesado = "AtaquePesado";
        public const string ParametroPasso = "Passo";
        public const string ParametroVelocidadeDoGolpe = "VelocidadeDoGolpe";
        public const string ParametroCancelar = "Cancelar";

        public static string ParametroDe(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? ParametroAtaquePesado : ParametroAtaqueLeve;

        /// <summary>
        /// Dispara o golpe do passo da cadeia (o pesado é sempre o passo 0); <c>false</c> se não
        /// havia onde tocar.
        /// </summary>
        public static bool Tocar(Animator animator, TipoDeAtaque tipo, int passo = 0)
        {
            if (!ParametrosDoAnimator.Pronto(animator))
            {
                return false;
            }

            ParametrosDoAnimator.DefinirDecimal(
                animator,
                ParametroVelocidadeDoGolpe,
                (float)RegrasAtaque.VelocidadeDoClipe(tipo)
            );
            ParametrosDoAnimator.DefinirInteiro(animator, ParametroPasso, passo);

            var alvo = Animator.StringToHash(ParametroDe(tipo));
            if (!ParametrosDoAnimator.Tem(animator, alvo, AnimatorControllerParameterType.Trigger))
            {
                return false;
            }

            animator.SetTrigger(alvo);
            return true;
        }

        /// <summary>
        /// O servidor recusou o golpe que já tocou (ADR 0024): tira a pose do golpe, sem a reação
        /// de dano. <c>false</c> se não havia onde tocar.
        /// </summary>
        public static bool Cancelar(Animator animator)
        {
            if (!ParametrosDoAnimator.Pronto(animator))
            {
                return false;
            }

            foreach (var nome in new[] { ParametroAtaqueLeve, ParametroAtaquePesado })
            {
                var hash = Animator.StringToHash(nome);
                if (
                    ParametrosDoAnimator.Tem(
                        animator,
                        hash,
                        AnimatorControllerParameterType.Trigger
                    )
                )
                {
                    animator.ResetTrigger(hash);
                }
            }

            var cancelar = Animator.StringToHash(ParametroCancelar);
            if (
                !ParametrosDoAnimator.Tem(
                    animator,
                    cancelar,
                    AnimatorControllerParameterType.Trigger
                )
            )
            {
                return false;
            }

            animator.SetTrigger(cancelar);
            return true;
        }
    }

    /// <summary>Leitura e escrita segura dos parâmetros do Animator (o que não existe é ignorado).</summary>
    internal static class ParametrosDoAnimator
    {
        public static bool Pronto(Animator animator) =>
            animator != null
            && animator.isActiveAndEnabled
            && animator.runtimeAnimatorController != null;

        public static bool Tem(Animator animator, int hash, AnimatorControllerParameterType tipo)
        {
            foreach (var parametro in animator.parameters)
            {
                if (parametro.nameHash == hash && parametro.type == tipo)
                {
                    return true;
                }
            }

            return false;
        }

        public static void DefinirDecimal(Animator animator, string nome, float valor)
        {
            var hash = Animator.StringToHash(nome);
            if (Tem(animator, hash, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(hash, valor);
            }
        }

        public static void DefinirInteiro(Animator animator, string nome, int valor)
        {
            var hash = Animator.StringToHash(nome);
            if (Tem(animator, hash, AnimatorControllerParameterType.Int))
            {
                animator.SetInteger(hash, valor);
            }
        }
    }
}
