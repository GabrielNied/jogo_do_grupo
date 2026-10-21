using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Marcador de um volume de água (lago, poça de dungeon) ou de uma via de escalada, posto na
    /// cena à mão (#349). A caixa do marcador é o próprio Transform: a posição é o centro, a escala
    /// é o tamanho (use um cubo de 1 m) e o giro em Y vale. Na água, o topo da caixa é a superfície
    /// e o fundo é o chão do lago; na via, a frente do marcador (+Z, a seta laranja) aponta para a
    /// parede. O menu <c>Jogo do Grupo/Personagem/Exportar volumes marcados</c> copia os marcadores
    /// com <see cref="Exportar"/> ligado para <c>VolumesMarcados</c> (shared), que é o que o
    /// servidor confere. Sem exportar, o volume vale só neste client (cena de validação).
    /// Rios e lagos do terreno não precisam de marcador: o chão abaixo do nível da água já é nado.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarcadorDeVolume : MonoBehaviour
    {
        [SerializeField]
        private TipoDeVolume _tipo = TipoDeVolume.Agua;

        [SerializeField, Tooltip("Nome único do volume (vai para o catálogo do servidor).")]
        private string _id = "";

        [
            SerializeField,
            Tooltip("Entra no catálogo do servidor ao exportar. Desligado, vale só aqui.")
        ]
        private bool _exportar = true;

        private VolumeMarcado _registrado;

        public TipoDeVolume Tipo => _tipo;

        public string Id => _id;

        public bool Exportar => _exportar;

        /// <summary>O volume que este marcador descreve agora, a partir do Transform.</summary>
        public VolumeMarcado Medir()
        {
            var escala = transform.lossyScale;
            var posicao = transform.position;
            return new VolumeMarcado(
                _id,
                _tipo,
                posicao.x,
                posicao.y,
                posicao.z,
                Mathf.Abs(escala.x),
                Mathf.Abs(escala.y),
                Mathf.Abs(escala.z),
                transform.eulerAngles.y
            );
        }

        /// <summary>Configura o marcador por código (cena gerada pelo script de editor).</summary>
        public void Definir(TipoDeVolume tipo, string id, bool exportar)
        {
            _tipo = tipo;
            _id = id;
            _exportar = exportar;
        }

        private void OnEnable()
        {
            // O que é exportado vem do catálogo do servidor; só o marcador local vale aqui.
            if (Application.isPlaying && !_exportar)
            {
                _registrado = Medir();
                AmbienteDeMovimento.Registrar(_registrado);
            }
        }

        private void OnDisable()
        {
            if (_registrado != null)
            {
                AmbienteDeMovimento.Remover(_registrado);
                _registrado = null;
            }
        }

        private void OnDrawGizmos()
        {
            var cor =
                _tipo == TipoDeVolume.Agua
                    ? new Color(0.2f, 0.5f, 1f, 1f)
                    : new Color(1f, 0.55f, 0.1f, 1f);
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                Quaternion.Euler(0f, transform.eulerAngles.y, 0f),
                transform.lossyScale
            );
            Gizmos.color = new Color(cor.r, cor.g, cor.b, 0.15f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = cor;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            if (_tipo == TipoDeVolume.ViaDeEscalada)
            {
                // Seta para a parede (frente +Z).
                Gizmos.DrawLine(Vector3.zero, new Vector3(0f, 0f, 0.7f));
                Gizmos.DrawLine(new Vector3(0f, 0f, 0.7f), new Vector3(0.1f, 0f, 0.55f));
                Gizmos.DrawLine(new Vector3(0f, 0f, 0.7f), new Vector3(-0.1f, 0f, 0.55f));
            }
        }
    }
}
