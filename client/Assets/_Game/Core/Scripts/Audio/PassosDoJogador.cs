using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>
    /// Passos do próprio personagem (docs/features/interface.md, Áudio): um som a cada
    /// <see cref="ConfigAudio.MetrosPorPasso"/> andados no chão. Pedra dentro dos muros da
    /// cidade, grama fora. Passos de outros jogadores não tocam.
    /// </summary>
    public sealed class PassosDoJogador : MonoBehaviour
    {
        private const int Variacoes = 5;

        private static AudioClip[] _pedra;
        private static AudioClip[] _grama;

        private CharacterController _corpo;
        private AudioSource _fonte;
        private Vector3 _ultimaPosicao;
        private float _andado;
        private int _ultimo = -1;

        private void Awake()
        {
            _corpo = GetComponent<CharacterController>();
            _fonte = gameObject.AddComponent<AudioSource>();
            _fonte.playOnAwake = false;
            _fonte.spatialBlend = 0f;
            _ultimaPosicao = transform.position;
            _pedra ??= Carregar("pedra");
            _grama ??= Carregar("grama");
        }

        private static AudioClip[] Carregar(string superficie)
        {
            var clipes = new AudioClip[Variacoes];
            for (var i = 0; i < Variacoes; i++)
            {
                clipes[i] = Resources.Load<AudioClip>($"Audio/Passos/{superficie}_{i}");
            }

            return clipes;
        }

        private void Update()
        {
            var posicao = transform.position;
            var deslocamento = posicao - _ultimaPosicao;
            _ultimaPosicao = posicao;
            deslocamento.y = 0f;
            var noChao = _corpo == null || _corpo.isGrounded;
            var metros = deslocamento.magnitude;
            // Teleporte ou correção do servidor não vira uma rajada de passos.
            if (!noChao || metros > 3f)
            {
                _andado = 0f;
                return;
            }

            _andado += metros;
            if (_andado < ConfigAudio.MetrosPorPasso)
            {
                return;
            }

            _andado -= ConfigAudio.MetrosPorPasso;
            Tocar(posicao);
        }

        private void Tocar(Vector3 posicao)
        {
            var dentroDosMuros =
                Mathf.Abs(posicao.x) <= MapaDoMundo.MetadeDaCidade
                && Mathf.Abs(posicao.z) <= MapaDoMundo.MetadeDaCidade;
            var clipes = dentroDosMuros ? _pedra : _grama;
            if (clipes == null || clipes.Length == 0)
            {
                return;
            }

            // Nunca o mesmo passo duas vezes seguidas: soa menos mecânico.
            var indice = Random.Range(0, clipes.Length);
            if (indice == _ultimo)
            {
                indice = (indice + 1) % clipes.Length;
            }

            _ultimo = indice;
            var clipe = clipes[indice];
            if (clipe != null)
            {
                _fonte.pitch = Random.Range(0.92f, 1.08f);
                _fonte.PlayOneShot(clipe, ConfigAudio.VolumeDosPassos * VolumesDoJogo.Efeitos);
            }
        }
    }
}
