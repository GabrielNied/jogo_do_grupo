using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Monta as árvores e pedras do mundo (<see cref="PecasDosBiomas"/>) em etapas: cada etapa
    /// trabalha até esgotar o orçamento de tempo do quadro (<see cref="ConfigVegetacao"/>) e
    /// devolve a vez. Cada peça vira uma instância no <see cref="BlocosDeVegetacao"/> (o
    /// desenho) e, se tem colisor, um objeto só com o colisor.
    /// </summary>
    public static class MontadorDaVegetacao
    {
        public const string NomeDoCaminho = "Mundo/";

        private static readonly Dictionary<string, ModeloDePeca> _modelosDosPrefabs =
            new Dictionary<string, ModeloDePeca>();

        private static readonly HashSet<string> _avisados = new HashSet<string>();

        /// <summary>Esquece os modelos guardados (testes).</summary>
        public static void Limpar()
        {
            _modelosDosPrefabs.Clear();
            _avisados.Clear();
            VegetacaoProcedural.Limpar();
            MateriaisDosBiomas.Limpar();
        }

        /// <summary>
        /// Cria sob <paramref name="pai"/> o desenho e os colisores de todas as peças. Cada
        /// <c>yield</c> é um ponto onde dá para ceder o quadro; quem quiser tudo de uma vez só
        /// percorre o enumerador.
        /// </summary>
        /// <param name="escalaDasPecas">Escala dos prefabs do kit (a do layout do hub).</param>
        public static IEnumerable<int> Montar(
            Transform pai,
            float escalaDasPecas,
            float orcamentoMs = ConfigVegetacao.OrcamentoDeMontagemPorQuadroMs
        )
        {
            foreach (var passo in Montar(pai, escalaDasPecas, new OrcamentoDeQuadro(orcamentoMs)))
            {
                yield return passo;
            }
        }

        /// <summary>Mesma montagem, compartilhando o relógio de <paramref name="orcamento"/>.</summary>
        public static IEnumerable<int> Montar(
            Transform pai,
            float escalaDasPecas,
            OrcamentoDeQuadro orcamento
        )
        {
            var blocos = pai.gameObject.AddComponent<BlocosDeVegetacao>();
            var indice = 0;
            foreach (var peca in PecasDosBiomas.Todas)
            {
                MontarPeca(peca, pai, blocos, escalaDasPecas);
                indice++;
                if (orcamento.Esgotou())
                {
                    yield return indice;
                }
            }

            yield return indice;
        }

        private static void MontarPeca(
            PecaDoBioma peca,
            Transform pai,
            BlocosDeVegetacao blocos,
            float escalaDasPecas
        )
        {
            var procedural = VegetacaoProcedural.ConsegueCriar(peca.Prefab);
            var modelo = procedural
                ? VegetacaoProcedural.Modelo(peca.Prefab)
                : ModeloDoPrefab(peca.Prefab);
            if (modelo == null)
            {
                return;
            }

            // Os prefabs do kit nascem na escala do layout; as malhas do jogo já estão no tamanho.
            var escala = peca.Escala * (procedural ? 1f : escalaDasPecas);
            var posicao = new Vector3(peca.X, peca.Y, peca.Z);
            var rotacao = Quaternion.Euler(0f, peca.Guinada, 0f);
            blocos.Adicionar(
                modelo,
                Matrix4x4.TRS(posicao, rotacao, Vector3.one * escala),
                peca.Bioma
            );

            if (modelo.Colisor.HasValue)
            {
                var objeto = new GameObject(peca.Prefab);
                objeto.transform.SetParent(pai, false);
                objeto.transform.SetLocalPositionAndRotation(posicao, rotacao);
                objeto.transform.localScale = Vector3.one * escala;
                modelo.Colisor.Value.Aplicar(objeto);
            }
        }

        /// <summary>
        /// Modelo de um prefab do kit em <c>Resources/Mundo/</c>: as partes vêm dos renderizadores
        /// dele e o colisor, do colisor da raiz. Guardado depois da primeira vez.
        /// </summary>
        public static ModeloDePeca ModeloDoPrefab(string nome)
        {
            if (_modelosDosPrefabs.TryGetValue(nome, out var pronto))
            {
                return pronto;
            }

            var prefab = Resources.Load<GameObject>(NomeDoCaminho + nome);
            if (prefab == null)
            {
                if (_avisados.Add(nome))
                {
                    Debug.LogWarning($"[Mundo] Prefab '{NomeDoCaminho}{nome}' não encontrado.");
                }

                _modelosDosPrefabs[nome] = null;
                return null;
            }

            var partes = new List<ParteDeModelo>();
            var raiz = prefab.transform.worldToLocalMatrix;
            foreach (var filtro in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderizador = filtro.GetComponent<MeshRenderer>();
                if (renderizador == null || filtro.sharedMesh == null)
                {
                    continue;
                }

                var local = raiz * filtro.transform.localToWorldMatrix;
                var materiais = renderizador.sharedMaterials;
                for (
                    var sub = 0;
                    sub < materiais.Length && sub < filtro.sharedMesh.subMeshCount;
                    sub++
                )
                {
                    partes.Add(new ParteDeModelo(filtro.sharedMesh, sub, materiais[sub], local));
                }
            }

            var modelo = new ModeloDePeca(
                nome,
                partes.ToArray(),
                ColisorDoPrefab(prefab),
                ConfigVegetacao.EhPequena(nome)
            );
            _modelosDosPrefabs[nome] = modelo;
            return modelo;
        }

        private static ColisorDaPeca? ColisorDoPrefab(GameObject prefab)
        {
            if (prefab.TryGetComponent<CapsuleCollider>(out var capsula))
            {
                return new ColisorDaPeca(capsula.center, capsula.radius, capsula.height);
            }

            if (prefab.TryGetComponent<BoxCollider>(out var caixa))
            {
                return new ColisorDaPeca(caixa.center, caixa.size);
            }

            return null;
        }
    }
}
