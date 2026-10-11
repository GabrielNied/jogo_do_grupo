using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Estado de um outro jogador na cidade: a posição mais recente do servidor (alvo) e a
    /// posição mostrada, que a segue de forma suave.
    /// </summary>
    public sealed class AvatarRemoto
    {
        /// <summary>Quanto maior, mais rápido a posição mostrada alcança o alvo (por segundo).</summary>
        public const float Suavizacao = 15f;

        /// <summary>Acima desta distância (m) o avatar salta para o alvo em vez de deslizar.</summary>
        public const float DistanciaDeSalto = 8f;

        public AvatarRemoto(long personagemId, Vector3 posicao, float rotacaoY)
        {
            PersonagemId = personagemId;
            Posicao = posicao;
            Alvo = posicao;
            RotacaoY = rotacaoY;
            AlvoRotacaoY = rotacaoY;
        }

        public long PersonagemId { get; }
        public Vector3 Posicao { get; private set; }
        public float RotacaoY { get; private set; }
        public Vector3 Alvo { get; private set; }
        public float AlvoRotacaoY { get; private set; }

        public void DefinirAlvo(Vector3 posicao, float rotacaoY)
        {
            Alvo = posicao;
            AlvoRotacaoY = rotacaoY;
        }

        /// <summary>Posiciona direto, sem interpolar (entrada na cidade).</summary>
        public void Teleportar(Vector3 posicao, float rotacaoY)
        {
            Posicao = posicao;
            Alvo = posicao;
            RotacaoY = rotacaoY;
            AlvoRotacaoY = rotacaoY;
        }

        /// <summary>Avança a posição mostrada em direção ao alvo.</summary>
        public void Atualizar(float delta)
        {
            if ((Alvo - Posicao).sqrMagnitude > DistanciaDeSalto * DistanciaDeSalto)
            {
                Posicao = Alvo;
                RotacaoY = AlvoRotacaoY;
                return;
            }

            var t = 1f - Mathf.Exp(-Suavizacao * Mathf.Max(0f, delta));
            Posicao = Vector3.Lerp(Posicao, Alvo, t);
            RotacaoY = Mathf.LerpAngle(RotacaoY, AlvoRotacaoY, t);
        }
    }
}
