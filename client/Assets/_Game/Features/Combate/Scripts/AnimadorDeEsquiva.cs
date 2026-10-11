using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Toca a animação da esquiva (o modelo já tem o clipe Roll) quando o servidor avisa que ela
    /// começou. O Trigger <see cref="ParametroEsquiva"/> é o combinado com a funcionalidade
    /// Personagem: Animator sem o parâmetro (ou sem Animator) é ignorado, sem erro.
    /// </summary>
    internal static class AnimadorDeEsquiva
    {
        public const string ParametroEsquiva = "Esquiva";

        /// <summary>Dispara o Trigger da esquiva; <c>false</c> se não havia onde tocar.</summary>
        public static bool Tocar(Animator animator)
        {
            if (
                animator == null
                || !animator.isActiveAndEnabled
                || animator.runtimeAnimatorController == null
            )
            {
                return false;
            }

            var alvo = Animator.StringToHash(ParametroEsquiva);
            foreach (var parametro in animator.parameters)
            {
                if (
                    parametro.nameHash == alvo
                    && parametro.type == AnimatorControllerParameterType.Trigger
                )
                {
                    animator.SetTrigger(alvo);
                    return true;
                }
            }

            return false;
        }
    }
}
