using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// O baú de teste no mundo: o baú do Survival Kit do Kenney (CC0) e as contas de onde ele
    /// fica e de quando está perto o bastante para abrir. Sem o modelo, dois blocos de reserva.
    /// </summary>
    public static class BauNoMundo
    {
        /// <summary>Distância horizontal máxima do centro do baú para poder abri-lo, em metros.</summary>
        public const float DistanciaParaAbrir = 3f;

        /// <summary>Tamanho do baú (largura, altura, profundidade), em metros.</summary>
        public static readonly Vector3 Tamanho = new Vector3(1.2f, 0.9f, 0.8f);

        private const string NomeObjeto = "BauDeTeste";

        /// <summary>
        /// Onde pôr o baú: o ponto fixo que o servidor confere (<see cref="ConfigBau"/>), perto do
        /// nascimento da cidade. A altura é a de <paramref name="altura"/> (o raio ao chão ajusta).
        /// </summary>
        public static Vector3 Posicao(float altura) =>
            new Vector3(ConfigBau.X, altura, ConfigBau.Z);

        /// <summary>Se o jogador está perto o bastante (no plano horizontal) para abrir o baú.</summary>
        public static bool EstaPerto(Vector3 jogador, Vector3 bau)
        {
            var dx = jogador.x - bau.x;
            var dz = jogador.z - bau.z;
            return dx * dx + dz * dz <= DistanciaParaAbrir * DistanciaParaAbrir;
        }

        /// <summary>
        /// Cria o baú com a base em <paramref name="posicao"/>, com um colisor para o jogador não
        /// atravessar. Fica na cena ativa; quem chama move para a cena do mundo.
        /// </summary>
        public static GameObject Criar(Vector3 posicao)
        {
            var raiz = new GameObject(NomeObjeto);
            raiz.transform.position = posicao;

            var colisor = raiz.AddComponent<BoxCollider>();
            colisor.size = Tamanho;
            colisor.center = new Vector3(0f, Tamanho.y / 2f, 0f);

            if (PropsDoMundo.Instanciar("Props/chest", raiz.transform, Tamanho.y, 180f) != null)
            {
                return raiz;
            }

            var materiais = raiz.AddComponent<MateriaisDoBau>();
            var corpo = Tamanho.y * 0.6f;
            var tampa = Tamanho.y - corpo;

            CriarBloco(
                raiz.transform,
                materiais,
                "Corpo",
                new Vector3(Tamanho.x, corpo, Tamanho.z),
                corpo / 2f,
                new Color(0.45f, 0.27f, 0.12f)
            );
            CriarBloco(
                raiz.transform,
                materiais,
                "Tampa",
                new Vector3(Tamanho.x, tampa, Tamanho.z),
                corpo + tampa / 2f,
                new Color(0.9f, 0.7f, 0.2f)
            );

            return raiz;
        }

        private static void CriarBloco(
            Transform pai,
            MateriaisDoBau materiais,
            string nome,
            Vector3 escala,
            float alturaDoCentro,
            Color cor
        )
        {
            var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloco.name = nome;

            // O colisor é um só, na raiz: o do primitivo só atrapalharia.
            var colisorDoBloco = bloco.GetComponent<Collider>();
            if (Application.isPlaying)
            {
                Object.Destroy(colisorDoBloco);
            }
            else
            {
                Object.DestroyImmediate(colisorDoBloco);
            }

            bloco.transform.SetParent(pai, false);
            bloco.transform.localPosition = new Vector3(0f, alturaDoCentro, 0f);
            bloco.transform.localScale = escala;

            var renderizador = bloco.GetComponent<Renderer>();
            var material = new Material(renderizador.sharedMaterial) { color = cor };
            renderizador.sharedMaterial = material;
            materiais.Registrar(material);
        }
    }

    /// <summary>Guarda os materiais criados em código para destruí-los junto com o baú.</summary>
    internal sealed class MateriaisDoBau : MonoBehaviour
    {
        private readonly List<Material> _materiais = new List<Material>();

        public void Registrar(Material material)
        {
            _materiais.Add(material);
        }

        private void OnDestroy()
        {
            foreach (var material in _materiais)
            {
                if (material == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
            }

            _materiais.Clear();
        }
    }
}
