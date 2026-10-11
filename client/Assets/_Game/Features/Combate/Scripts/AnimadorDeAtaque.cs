using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Toca a animação do golpe no Animator do visual quando o servidor avisa que o ataque
    /// começou. Os Triggers <see cref="ParametroAtaqueLeve"/> e <see cref="ParametroAtaquePesado"/>
    /// são o combinado com a funcionalidade Personagem: Animator sem o parâmetro (ou sem
    /// Animator) é ignorado, sem erro.
    /// </summary>
    internal static class AnimadorDeAtaque
    {
        public const string ParametroAtaqueLeve = "AtaqueLeve";
        public const string ParametroAtaquePesado = "AtaquePesado";

        public static string ParametroDe(TipoDeAtaque tipo) =>
            tipo == TipoDeAtaque.Pesado ? ParametroAtaquePesado : ParametroAtaqueLeve;

        /// <summary>Dispara o Trigger do ataque; <c>false</c> se não havia onde tocar.</summary>
        public static bool Tocar(Animator animator, TipoDeAtaque tipo)
        {
            if (
                animator == null
                || !animator.isActiveAndEnabled
                || animator.runtimeAnimatorController == null
            )
            {
                return false;
            }

            var alvo = Animator.StringToHash(ParametroDe(tipo));
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
