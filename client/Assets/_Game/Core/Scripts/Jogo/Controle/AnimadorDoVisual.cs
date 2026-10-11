using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Alimenta o Animator do visual do jogador com os parâmetros combinados com a funcionalidade
    /// Personagem: <see cref="ParametroVelocidade"/> (float, m/s) e <see cref="ParametroNoChao"/>
    /// (bool). Parâmetro que não existir no Animator é ignorado; sem Animator, não faz nada.
    /// </summary>
    public sealed class AnimadorDoVisual
    {
        /// <summary>Nome do parâmetro float com a velocidade horizontal, em m/s.</summary>
        public const string ParametroVelocidade = "Velocidade";

        /// <summary>Nome do parâmetro bool que diz se o jogador está no chão.</summary>
        public const string ParametroNoChao = "NoChao";

        private static readonly int HashVelocidade = Animator.StringToHash(ParametroVelocidade);
        private static readonly int HashNoChao = Animator.StringToHash(ParametroNoChao);

        private readonly Animator _animator;
        private readonly bool _temVelocidade;
        private readonly bool _temNoChao;

        public AnimadorDoVisual(Animator animator)
        {
            _animator = animator;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (var parametro in animator.parameters)
            {
                if (
                    parametro.nameHash == HashVelocidade
                    && parametro.type == AnimatorControllerParameterType.Float
                )
                {
                    _temVelocidade = true;
                }
                else if (
                    parametro.nameHash == HashNoChao
                    && parametro.type == AnimatorControllerParameterType.Bool
                )
                {
                    _temNoChao = true;
                }
            }
        }

        /// <summary>Atualiza os parâmetros que existirem no Animator.</summary>
        public void Atualizar(float velocidade, bool noChao)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
            {
                return;
            }

            if (_temVelocidade)
            {
                _animator.SetFloat(HashVelocidade, velocidade);
            }

            if (_temNoChao)
            {
                _animator.SetBool(HashNoChao, noChao);
            }
        }
    }
}
