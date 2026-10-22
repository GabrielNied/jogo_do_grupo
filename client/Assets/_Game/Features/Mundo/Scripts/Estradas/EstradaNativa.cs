using UnityEngine;
using UnityEngine.Splines;

namespace JogoDoGrupo.Client.Features.Mundo.Estradas
{
    public enum TipoDeEstrada
    {
        Terra,
        Lajotas,
    }

    /// <summary>Autoria de estrada. A geometria pronta e o Terrain são os dados usados no jogo.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer))]
    [AddComponentMenu("Jogo do Grupo/Mundo/Estrada nativa")]
    public sealed class EstradaNativa : MonoBehaviour
    {
        [SerializeField, Min(0.5f)]
        private float _largura = 4f;

        [
            SerializeField,
            Min(0.2f),
            Tooltip("Suavização física do rebaixo, em metros. Não controla a transparência.")
        ]
        private float _transicao = 1f;

        [
            SerializeField,
            Range(0.2f, 50f),
            Tooltip(
                "Faixa de transparência em cada borda, em metros, além da largura do leito. Não aumenta o rebaixo."
            )
        ]
        private float _transicaoVisual = 1.5f;

        [SerializeField, Range(0f, 0.35f)]
        private float _rebaixo = 0.12f;

        [SerializeField, Range(0f, 0.4f)]
        private float _variacaoLateral = 0.15f;

        [SerializeField, Min(0f)]
        private float _margemVegetacao = 0.5f;

        [SerializeField]
        private TipoDeEstrada _tipo;

        [SerializeField]
        private bool _pedregulhos = true;

        [
            SerializeField,
            Range(0f, 8f),
            Tooltip(
                "Quantidade de pedras por metro quadrado de leito. A malha tem limite de segurança de 8.192 pedras por estrada."
            )
        ]
        private float _densidadeCascalhos = 2f;

        [
            SerializeField,
            ColorUsage(false),
            Tooltip("Tom dos cascalhos. O padrão é terroso; não altera a cor das lajotas.")
        ]
        private Color _corCascalhos = new Color(0.42f, 0.32f, 0.21f, 1f);

        [SerializeField, Range(0f, 1f)]
        private float _intensidadeCascos = 0.15f;

        [SerializeField]
        private int _semente = 17;

        [SerializeField]
        private Material _material;

        [SerializeField, Tooltip("Vazio: todos os Terrain ativos desta cena.")]
        private Terrain[] _terrenos;

        [SerializeField, HideInInspector]
        private string _id;

        [SerializeField, HideInInspector]
        private string _registroGuid;
        public string Id
        {
            get => _id;
            set => _id = value;
        }
        public bool PossuiRegistro => !string.IsNullOrEmpty(_registroGuid);

        // Uma string não empacota o backup de autoria (classe Editor) no mapa do jogador.
        public Object Registro
        {
            get
            {
#if UNITY_EDITOR
                return string.IsNullOrEmpty(_registroGuid)
                    ? null
                    : UnityEditor.AssetDatabase.LoadMainAssetAtPath(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(_registroGuid)
                    );
#else
                return null;
#endif
            }
            set
            {
#if UNITY_EDITOR
                _registroGuid =
                    value == null
                        ? null
                        : UnityEditor.AssetDatabase.AssetPathToGUID(
                            UnityEditor.AssetDatabase.GetAssetPath(value)
                        );
#endif
            }
        }
        public SplineContainer Spline => GetComponent<SplineContainer>();
        public float Largura => _largura;
        public float Transicao => _transicao;
        public float TransicaoVisual => _transicaoVisual;
        public float TransicaoDoCorredor => Mathf.Max(Transicao, TransicaoVisual);
        public float Rebaixo => _rebaixo;
        public float VariacaoLateral => _variacaoLateral;
        public float MargemVegetacao => _margemVegetacao;
        public TipoDeEstrada Tipo => _tipo;
        public bool Pedregulhos => _pedregulhos;
        public float DensidadeCascalhos => _densidadeCascalhos;
        public Color CorCascalhos => _corCascalhos;
        public float IntensidadeCascos => _intensidadeCascos;
        public int Semente => _semente;
        public Material Material => _material;
        public Terrain[] Terrenos => _terrenos;
    }
}
