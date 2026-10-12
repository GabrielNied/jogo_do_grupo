using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Posturas da defesa no Animator do visual: o Bool <see cref="ParametroGuarda"/> enquanto a
    /// guarda está segurada e o Trigger <see cref="ParametroInterrompido"/> quando um acerto
    /// interrompe o golpe. São o combinado com a funcionalidade Personagem (clipes em #475):
    /// Animator sem o parâmetro (ou sem Animator) é ignorado, sem erro.
    /// </summary>
    internal static class AnimadorDeDefesa
    {
        public const string ParametroGuarda = "Guarda";
        public const string ParametroInterrompido = "Interrompido";

        public const string ParametroCaido = "Caido";

        /// <summary>Deita o personagem (ou o levanta); <c>false</c> se não havia onde tocar.</summary>
        public static bool DefinirCaido(Animator animator, bool caido) =>
            Definir(animator, ParametroCaido, AnimatorControllerParameterType.Bool, caido);

        /// <summary>Liga ou desliga a postura de guarda; <c>false</c> se não havia onde tocar.</summary>
        public static bool DefinirGuarda(Animator animator, bool segurando) =>
            Definir(animator, ParametroGuarda, AnimatorControllerParameterType.Bool, segurando);

        /// <summary>Para o golpe em curso; <c>false</c> se não havia onde tocar.</summary>
        public static bool Interromper(Animator animator)
        {
            if (!Pronto(animator))
            {
                return false;
            }

            // O golpe que ainda não saiu do estado de espera não deve começar depois do acerto.
            foreach (var nome in new[] { "AtaqueLeve", "AtaquePesado" })
            {
                if (Tem(animator, nome, AnimatorControllerParameterType.Trigger))
                {
                    animator.ResetTrigger(Animator.StringToHash(nome));
                }
            }

            return Definir(
                animator,
                ParametroInterrompido,
                AnimatorControllerParameterType.Trigger,
                true
            );
        }

        private static bool Definir(
            Animator animator,
            string nome,
            AnimatorControllerParameterType tipo,
            bool valor
        )
        {
            if (!Pronto(animator) || !Tem(animator, nome, tipo))
            {
                return false;
            }

            var hash = Animator.StringToHash(nome);
            if (tipo == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(hash);
            }
            else
            {
                animator.SetBool(hash, valor);
            }

            return true;
        }

        private static bool Pronto(Animator animator) =>
            animator != null
            && animator.isActiveAndEnabled
            && animator.runtimeAnimatorController != null;

        private static bool Tem(
            Animator animator,
            string nome,
            AnimatorControllerParameterType tipo
        )
        {
            var hash = Animator.StringToHash(nome);
            foreach (var parametro in animator.parameters)
            {
                if (parametro.nameHash == hash && parametro.type == tipo)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
