using System;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    // Sem CreateAssetMenu: a receita e criada na janela existente, sem outro comando de Mundo.
    public sealed class ReceitaDeEncostas : ScriptableObject
    {
        public ConfigDeEncostas Pintura = new ConfigDeEncostas();
        public ConfigDeRochas Rochas = new ConfigDeRochas();
    }

    [Serializable]
    public sealed class ConfigDeEncostas
    {
        public TerrainLayer Camada;
        public bool UsarInclinacao = true;
        public float Inicio = 35f;
        public float Plena = 55f;

        [Range(0f, 1f)]
        public float Cobertura = 1f;
        public bool UsarAltitude;
        public float AltitudeInicial = 50f;
        public float AltitudePlena = 100f;

        [Range(0f, 1f)]
        public float CoberturaAltitude = 1f;
        public float EscalaIrregularidade = 35f;
        public float VariacaoAltitude;
        public float VariacaoAngulo;
        public int Semente = 12345;
        public bool LimitarRegiao;
        public Vector2 Centro;
        public float Raio = 50f;
        public float Borda = 5f;

        public string Validar()
        {
            if (Camada == null)
                return "Escolha uma TerrainLayer.";
            if (!UsarInclinacao && !UsarAltitude)
                return "Ative inclinacao, altitude ou ambas.";
            if (
                !Finito(
                    Inicio,
                    Plena,
                    Cobertura,
                    AltitudeInicial,
                    AltitudePlena,
                    CoberturaAltitude,
                    EscalaIrregularidade,
                    VariacaoAltitude,
                    VariacaoAngulo,
                    Centro.x,
                    Centro.y,
                    Raio,
                    Borda
                )
            )
                return "Os parametros precisam ser numeros finitos.";
            if (UsarInclinacao && (Inicio < 0 || Plena > 90 || Plena <= Inicio))
                return "Use angulos crescentes entre 0 e 90 graus.";
            if (UsarAltitude && AltitudePlena <= AltitudeInicial)
                return "A altitude plena precisa ser maior que a inicial (metros mundiais).";
            if (
                Cobertura < 0
                || Cobertura > 1
                || CoberturaAltitude < 0
                || CoberturaAltitude > 1
                || EscalaIrregularidade <= 0
                || VariacaoAltitude < 0
                || VariacaoAngulo < 0
            )
                return "Use coberturas entre 0 e 1, escala positiva e variacoes nao negativas.";
            if (LimitarRegiao && (Raio <= 0 || Borda < 0 || Borda > Raio))
                return "Use raio positivo e borda entre zero e o raio.";
            return null;
        }

        public float Mascara(Vector3 mundo, float inclinacao)
        {
            var ruido = Irregularidade(mundo.x, mundo.z);
            var encosta = UsarInclinacao
                ? Suave(Inicio, Plena, inclinacao + ruido * VariacaoAngulo) * Cobertura
                : 0f;
            var altitude = UsarAltitude
                ? Suave(AltitudeInicial, AltitudePlena, mundo.y + ruido * VariacaoAltitude)
                    * CoberturaAltitude
                : 0f;
            return Mathf.Max(encosta, altitude) * PesoRegiao(mundo.x, mundo.z);
        }

        public float PesoRegiao(float x, float z)
        {
            if (!LimitarRegiao)
                return 1f;
            var distancia = Vector2.Distance(Centro, new Vector2(x, z));
            if (Borda == 0f)
                return distancia <= Raio ? 1f : 0f;
            return 1f - Suave(Raio - Borda, Raio, distancia);
        }

        public float Irregularidade(float x, float z)
        {
            // Coordenadas de mundo: blocos vizinhos consultam o mesmo campo, sem reiniciar a semente.
            var hash = unchecked((uint)Semente * 747796405u + 2891336453u);
            var ox = (hash & 65535u) / 64f;
            var oz = (hash >> 16) / 64f;
            return Mathf.PerlinNoise(x / EscalaIrregularidade + ox, z / EscalaIrregularidade + oz)
                    * 2f
                - 1f;
        }

        private static float Suave(float inicio, float fim, float valor) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inicio, fim, valor));

        private static bool Finito(params float[] valores)
        {
            foreach (var valor in valores)
                if (!float.IsFinite(valor))
                    return false;
            return true;
        }
    }

    [Serializable]
    public sealed class ConfigDeRochas
    {
        public GameObject[] Modelos = Array.Empty<GameObject>();

        [Tooltip("Maior dimensao mundial de cada peca: minimo e maximo, em metros.")]
        public Vector2 Tamanho = new Vector2(4f, 7f);

        [Tooltip(
            "Espacamento aproximado sobre a superficie, em metros; paredes fortes recebem subdivisoes."
        )]
        public float Espacamento = 5f;

        [Range(0f, 1f)]
        public float Densidade = 0.65f;

        [Range(0f, 1f)]
        public float Enterramento = 0.35f;

        [Range(0f, 1f)]
        public float AcompanharNormal = 0.6f;
        public float InclinacaoMinima = 35f;

        [Tooltip(
            "Maximo de pecas geradas nesta aplicacao. Pecas de outras regioes sao preservadas."
        )]
        public int Limite = 1000;
        public int Semente = 12345;
    }
}
