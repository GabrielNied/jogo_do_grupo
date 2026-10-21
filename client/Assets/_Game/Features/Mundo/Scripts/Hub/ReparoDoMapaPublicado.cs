using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// O mapa feito à mão foi publicado como prefab a partir do hub montado em tempo de execução,
    /// e os materiais tingidos daquela montagem (criados em memória) não foram salvos: no prefab
    /// eles viraram referência vazia e as casas, barracas, lanternas, fonte, túnel e pilares
    /// aparecem rosa (#1179). Ao instanciar o mapa, devolve a cada peça o material do prefab de
    /// origem em <c>Resources/Mundo</c> e, sem origem conhecida, um material do atlas do Kenney ou
    /// de cor sólida pelo tipo da peça. Não muda posição, malha nem colisão.
    /// </summary>
    internal static class ReparoDoMapaPublicado
    {
        private const string PastaPrefabs = "Mundo/";
        private const string MaterialDoAtlas = "Mundo/Materiais/Kenney_Colormap";

        private static readonly Dictionary<string, GameObject> _prefabs =
            new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Material> _solidos =
            new Dictionary<string, Material>();

        /// <summary>Quantos renderizadores foram consertados.</summary>
        public static int Reparar(GameObject mapa)
        {
            if (mapa == null)
                return 0;

            var consertados = 0;
            foreach (var renderizador in mapa.GetComponentsInChildren<Renderer>(true))
            {
                var materiais = renderizador.sharedMaterials;
                var faltando = false;
                for (var i = 0; i < materiais.Length && !faltando; i++)
                    faltando = materiais[i] == null;
                if (!faltando || renderizador is ParticleSystemRenderer)
                    continue;

                var origem = MateriaisDaOrigem(renderizador);
                for (var i = 0; i < materiais.Length; i++)
                {
                    if (materiais[i] != null)
                        continue;
                    materiais[i] =
                        origem != null && i < origem.Length && origem[i] != null
                            ? origem[i]
                            : MaterialDeReserva(renderizador);
                }

                renderizador.sharedMaterials = materiais;
                consertados++;
            }

            if (consertados > 0)
                Debug.Log($"[Mundo] Mapa publicado: {consertados} peças sem material consertadas.");
            return consertados;
        }

        // Sobe na hierarquia até achar um ancestral com o nome de um prefab de Resources/Mundo e
        // pega, no prefab, o renderizador no mesmo caminho relativo.
        private static Material[] MateriaisDaOrigem(Renderer renderizador)
        {
            var caminho = new List<string>();
            for (var atual = renderizador.transform; atual != null; atual = atual.parent)
            {
                var prefab = Prefab(NomeSemSufixo(atual.name));
                if (prefab != null)
                {
                    var alvo = prefab.transform;
                    for (var i = caminho.Count - 1; i >= 0 && alvo != null; i--)
                        alvo = alvo.Find(caminho[i]);
                    var correspondente = alvo != null ? alvo.GetComponent<Renderer>() : null;
                    if (correspondente == null)
                        correspondente = PorMalha(prefab, renderizador);
                    return correspondente != null ? correspondente.sharedMaterials : null;
                }

                caminho.Add(atual.name);
            }

            return null;
        }

        private static Renderer PorMalha(GameObject prefab, Renderer renderizador)
        {
            var filtro = renderizador.GetComponent<MeshFilter>();
            var malha = filtro != null ? filtro.sharedMesh : null;
            foreach (var candidato in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var outro = candidato.GetComponent<MeshFilter>();
                if (malha != null && outro != null && outro.sharedMesh == malha)
                    return candidato;
            }

            return null;
        }

        private static GameObject Prefab(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return null;
            if (!_prefabs.TryGetValue(nome, out var prefab))
            {
                prefab = Resources.Load<GameObject>(PastaPrefabs + nome);
                _prefabs[nome] = prefab;
            }

            return prefab;
        }

        // "Lanterna (3)" e "Casa_Pedra_Larga(Clone)" voltam ao nome do prefab.
        private static string NomeSemSufixo(string nome)
        {
            var fim = nome.IndexOf(" (", System.StringComparison.Ordinal);
            if (fim > 0)
                nome = nome.Substring(0, fim);
            return nome.Replace("(Clone)", string.Empty).Trim();
        }

        private static Material MaterialDeReserva(Renderer renderizador)
        {
            var nome = renderizador.name.ToLowerInvariant();
            var pai =
                renderizador.transform.parent != null
                    ? renderizador.transform.parent.name.ToLowerInvariant()
                    : string.Empty;
            if (
                nome.Contains("tunel")
                || nome.Contains("pedra")
                || pai.Contains("pedra")
                || pai.Contains("caverna")
            )
                return Solido("pedra", new Color(0.42f, 0.39f, 0.36f));
            if (nome.Contains("pilar") || nome.Contains("travessa") || nome.Contains("viga"))
                return Solido("madeira", new Color(0.42f, 0.29f, 0.18f));
            if (nome.Contains("telhado"))
                return Solido("telhado", new Color(0.46f, 0.20f, 0.16f));

            // Peças do kit de cidade do Kenney usam o atlas de cores.
            var atlas = Resources.Load<Material>(MaterialDoAtlas);
            return atlas != null ? atlas : Solido("neutro", new Color(0.6f, 0.58f, 0.54f));
        }

        private static Material Solido(string chave, Color cor)
        {
            if (_solidos.TryGetValue(chave, out var pronto) && pronto != null)
                return pronto;

            // Copia o material do atlas para usar o mesmo shader que já vai no build.
            var baseAtlas = Resources.Load<Material>(MaterialDoAtlas);
            var material =
                baseAtlas != null
                    ? new Material(baseAtlas)
                    : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Reparo_" + chave;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", null);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", cor);
            material.color = cor;
            _solidos[chave] = material;
            return material;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _prefabs.Clear();
            _solidos.Clear();
        }
    }
}
