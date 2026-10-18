using System;
using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    [Serializable]
    public sealed class EspecieDoPincel
    {
        [Tooltip("Prefab para árvore ou planta; deixe vazio quando usar uma textura de grama.")]
        public GameObject Modelo;

        [Tooltip("Textura com alpha para grama nativa. Use modelo OU textura, nunca ambos.")]
        public Texture2D Textura;

        [Min(0f)]
        public float Peso = 1f;

        [Tooltip("Escala de árvores; tamanho nativo de detalhes. Mínimo e máximo.")]
        public Vector2 Tamanho = new Vector2(0.8f, 1.2f);
    }

    /// <summary>Receita de autoria apenas do editor; não gera vegetação durante o jogo.</summary>
    [CreateAssetMenu(menuName = "Jogo do Grupo/Mundo/Conjunto de vegetação")]
    public sealed class ConjuntoDeVegetacao : ScriptableObject
    {
        [Range(0.5f, 50f)]
        public float Raio = 10f;

        [
            Range(0f, 25f),
            Tooltip("Quantidade total de árvores por 100 m², distribuída pelos pesos.")
        ]
        public float ArvoresPorCemMetros = 4f;

        [
            Range(0f, 1f),
            Tooltip("Cobertura nativa de plantas, de 0 a 1; independente das árvores e grama.")
        ]
        public float CoberturaDePlantas = 0.2f;

        [Range(0f, 1f), Tooltip("Cobertura nativa de grama, de 0 a 1; não é uma contagem por m².")]
        public float CoberturaDeGrama = 0.7f;

        [
            Range(1f, 5f),
            Tooltip(
                "Densidade nativa do protótipo; afeta esta espécie em todo o Terrain ao pintar."
            )
        ]
        public float DensidadeDePlantas = 1f;

        [
            Range(1f, 5f),
            Tooltip(
                "Densidade nativa do protótipo; 4 produz grama mais cheia. Afeta esta espécie em todo o Terrain ao pintar."
            )
        ]
        public float DensidadeDeGrama = 4f;

        [Range(0f, 45f)]
        public float InclinacaoMaxima = 35f;

        [Range(0.1f, 20f)]
        public float DistanciaEntreArvores = 3f;

        [
            Range(0f, 20f),
            Tooltip(
                "Espaçamento aproximado entre plantas. Zero usa cobertura nativa livre; não afeta árvores nem grama."
            )
        ]
        public float DistanciaEntrePlantas;

        [
            Range(0f, 20f),
            Tooltip(
                "Espaçamento aproximado entre tufos de grama. Zero permite cobertura nativa livre, inclusive sob árvores."
            )
        ]
        public float DistanciaEntreGramas;
        public int Semente = 12345;
        public List<EspecieDoPincel> Arvores = new List<EspecieDoPincel>();
        public List<EspecieDoPincel> Plantas = new List<EspecieDoPincel>();
        public List<EspecieDoPincel> Gramas = new List<EspecieDoPincel>();
    }
}
