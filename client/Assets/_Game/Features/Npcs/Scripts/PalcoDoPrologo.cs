using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using Random = System.Random;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Palco do prólogo "O sol que nasceu na estrada" (#327): uma clareira montada em código, longe
    /// do mundo (muito abaixo do terreno), vista por uma câmera própria que desenha numa
    /// <see cref="RenderTexture"/>. Nada disso existe para o servidor nem para os outros jogadores:
    /// os quatro magos são NPCs de cena (visual do papel Mago), a manada são sombras que se desfazem
    /// no clarão, sem loot e sem dano. A linha do tempo vem de <see cref="PrologoDoSol"/>.
    /// </summary>
    public sealed class PalcoDoPrologo : MonoBehaviour
    {
        // Ajustes visuais da cena.
        private const float ProfundidadeDoPalco = -5000f;
        private const int LarguraDaImagem = 1280;
        private const int AlturaDaImagem = 720;
        private const float EspacoEntreMagos = 3.2f;
        private const float AlturaDaEsfera = 4.6f;
        private const float RaioFinalDaEsfera = 1.7f;
        private const float RaioDoClarao = 10f;
        private const float IntensidadeMaximaDaLuz = 900f;

        private readonly List<Transform> _sombras = new List<Transform>();
        private readonly List<Vector3> _inicios = new List<Vector3>();
        private readonly List<Vector3> _fins = new List<Vector3>();
        private readonly List<Animator> _animadoresDosMagos = new List<Animator>();
        private readonly List<GameObject> _criados = new List<GameObject>();
        private RenderTexture _imagem;
        private Camera _camera;
        private Transform _esfera;
        private Renderer _esferaRenderer;
        private Light _luz;
        private float _tempo;
        private bool _terminou;
        private int _falanteAtual = int.MinValue;

        public RenderTexture Imagem => _imagem;

        /// <summary>Disparado quando a fala muda (<c>null</c> sem fala).</summary>
        public event Action<FalaDoPrologo> FalaMudou;

        /// <summary>Intensidade do clarão de 0 a 1, todo quadro.</summary>
        public event Action<float> ClaraoMudou;

        /// <summary>Disparado uma vez, ao fim da linha do tempo.</summary>
        public event Action Terminou;

        public static PalcoDoPrologo Criar()
        {
            var objeto = new GameObject("PalcoDoPrologo");
            var palco = objeto.AddComponent<PalcoDoPrologo>();
            palco.Montar();
            return palco;
        }

        private Vector3 Origem => new Vector3(0f, ProfundidadeDoPalco, 0f);

        private void Montar()
        {
            _imagem = new RenderTexture(LarguraDaImagem, AlturaDaImagem, 24)
            {
                name = "PrologoDoSol",
            };

            var cameraObjeto = Nova("Camera");
            _camera = cameraObjeto.AddComponent<Camera>();
            _camera.targetTexture = _imagem;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.13f, 0.15f, 0.3f);
            _camera.fieldOfView = 50f;
            _camera.nearClipPlane = 0.2f;
            _camera.farClipPlane = 220f;

            // Chão de terra, estrada escura no meio e algumas árvores ao redor.
            var chao = Forma(
                "Chao",
                PrimitiveType.Cylinder,
                new Vector3(0f, -0.25f, 0f),
                new Vector3(120f, 0.5f, 120f),
                new Color(0.32f, 0.28f, 0.18f)
            );
            chao.transform.SetParent(transform, true);
            Forma(
                "Estrada",
                PrimitiveType.Cube,
                new Vector3(0f, 0.02f, -40f),
                new Vector3(7f, 0.06f, 110f),
                new Color(0.2f, 0.17f, 0.12f)
            );
            var sorteio = new Random(327);
            for (var i = 0; i < 16; i++)
            {
                var lado = i % 2 == 0 ? -1f : 1f;
                var x = lado * (12f + (float)sorteio.NextDouble() * 22f);
                var z = -50f + (float)sorteio.NextDouble() * 60f;
                var altura = 4f + (float)sorteio.NextDouble() * 3f;
                Forma(
                    "Tronco",
                    PrimitiveType.Cylinder,
                    new Vector3(x, altura * 0.5f, z),
                    new Vector3(0.6f, altura, 0.6f),
                    new Color(0.3f, 0.2f, 0.12f)
                );
                Forma(
                    "Copa",
                    PrimitiveType.Sphere,
                    new Vector3(x, altura + 1.2f, z),
                    new Vector3(4f, 3.6f, 4f),
                    new Color(0.12f, 0.3f, 0.16f)
                );
            }

            // Os quatro magos, de frente para a estrada por onde a manada vem.
            for (var i = 0; i < PrologoDoSol.QuantidadeDeMagos; i++)
            {
                var x = (i - (PrologoDoSol.QuantidadeDeMagos - 1) * 0.5f) * EspacoEntreMagos;
                var anfitriao = Nova("Mago_" + i);
                anfitriao.transform.position = Origem + new Vector3(x, 0f, 0f);
                anfitriao.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var corpo = FabricaDeCorpoDeNpc.Criar(PapelNpc.Mago, 300 + i, anfitriao.transform);
                var animador = corpo.GetComponentInChildren<Animator>();
                if (animador != null)
                {
                    animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                _animadoresDosMagos.Add(animador);
            }

            // A esfera de luz e a luz que ela espalha.
            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name = "Esfera";
            Eliminar(esfera.GetComponent<Collider>());
            esfera.transform.SetParent(transform, false);
            _esfera = esfera.transform;
            _esferaRenderer = esfera.GetComponent<Renderer>();
            _esferaRenderer.material.color = new Color(1f, 0.92f, 0.6f);
            _esferaRenderer.material.EnableKeyword("_EMISSION");
            _esferaRenderer.material.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 4f);
            var luzObjeto = Nova("LuzDaEsfera");
            luzObjeto.transform.SetParent(_esfera, false);
            _luz = luzObjeto.AddComponent<Light>();
            _luz.type = LightType.Point;
            _luz.color = new Color(1f, 0.85f, 0.55f);
            _luz.range = 60f;

            // A manada: sombras que vêm pela estrada e somem no clarão.
            for (var i = 0; i < PrologoDoSol.QuantidadeDeSombras; i++)
            {
                var sombra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sombra.name = "Sombra_" + i;
                Eliminar(sombra.GetComponent<Collider>());
                sombra.transform.SetParent(transform, false);
                sombra.transform.localScale = new Vector3(2.2f, 2.0f, 3.6f);
                sombra.GetComponent<Renderer>().material.color = new Color(0.04f, 0.03f, 0.05f);
                for (var olho = -1; olho <= 1; olho += 2)
                {
                    var brilho = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Eliminar(brilho.GetComponent<Collider>());
                    brilho.transform.SetParent(sombra.transform, false);
                    brilho.transform.localPosition = new Vector3(olho * 0.22f, 0.15f, -0.5f);
                    brilho.transform.localScale = Vector3.one * 0.12f;
                    var material = brilho.GetComponent<Renderer>().material;
                    material.color = new Color(1f, 0.15f, 0.1f);
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.05f) * 3f);
                }

                var coluna = i % 3 - 1;
                var fila = i / 3;
                var x = coluna * 7f + ((float)sorteio.NextDouble() - 0.5f) * 3f;
                var fim =
                    Origem
                    + new Vector3(x, 0.6f, -9f - fila * 3.5f - (float)sorteio.NextDouble() * 2f);
                _fins.Add(fim);
                _inicios.Add(fim + new Vector3(0f, 0f, -55f - fila * 6f));
                _sombras.Add(sombra.transform);
            }

            Atualizar(0f);
        }

        private GameObject Nova(string nome)
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(transform, false);
            _criados.Add(objeto);
            return objeto;
        }

        private GameObject Forma(
            string nome,
            PrimitiveType tipo,
            Vector3 posicao,
            Vector3 tamanho,
            Color cor
        )
        {
            var objeto = GameObject.CreatePrimitive(tipo);
            objeto.name = nome;
            Eliminar(objeto.GetComponent<Collider>());
            objeto.transform.SetParent(transform, false);
            objeto.transform.position = Origem + posicao;
            var escala = tamanho;
            if (tipo == PrimitiveType.Cylinder)
            {
                escala = new Vector3(tamanho.x, tamanho.y * 0.5f, tamanho.z);
            }

            objeto.transform.localScale = escala;
            objeto.GetComponent<Renderer>().material.color = cor;
            return objeto;
        }

        private void Update()
        {
            if (_terminou)
            {
                return;
            }

            _tempo += Time.unscaledDeltaTime;
            Atualizar(_tempo);
            if (_tempo >= PrologoDoSol.DuracaoTotal)
            {
                _terminou = true;
                Terminou?.Invoke();
            }
        }

        /// <summary>Põe a cena no instante pedido (também usado para conferir quadros no editor).</summary>
        public void Mostrar(float tempo) => Atualizar(tempo);

        private static void Eliminar(UnityEngine.Object objeto)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(objeto);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(objeto);
            }
        }

        private void Atualizar(float tempo)
        {
            var origem = Origem;
            var brilho = PrologoDoSol.BrilhoEm(tempo);
            var clarao = PrologoDoSol.ClaraoEm(tempo);
            var dissolucao = PrologoDoSol.DissolucaoEm(tempo);
            var aproximacao = PrologoDoSol.AproximacaoEm(tempo);

            // Esfera: nasce pequena entre os magos, cresce até o clarão e se desfaz depois dele.
            var raio =
                (0.3f + brilho * RaioFinalDaEsfera + clarao * RaioDoClarao * 0.5f)
                * (1f - dissolucao);
            _esfera.position = origem + new Vector3(0f, AlturaDaEsfera + brilho * 1.2f, -2f);
            _esfera.localScale = Vector3.one * Mathf.Max(raio * 2f, 0.01f);
            _luz.intensity =
                (80f + brilho * 400f + clarao * IntensidadeMaximaDaLuz) * (1f - dissolucao);

            for (var i = 0; i < _sombras.Count; i++)
            {
                var passo = Mathf.Sin((tempo * 2.2f) + i) * 0.12f;
                var posicao = Vector3.Lerp(_inicios[i], _fins[i], aproximacao);
                _sombras[i].position = posicao + new Vector3(0f, Mathf.Abs(passo), 0f);
                _sombras[i].localScale =
                    new Vector3(2.2f, 2.0f, 3.6f) * Mathf.Max(1f - dissolucao, 0.001f);
            }

            // Câmera: aproxima devagar dos magos e olha a esfera.
            var avanco = Mathf.Clamp01(tempo / PrologoDoSol.TempoDoClarao);
            _camera.transform.position =
                origem
                + new Vector3(
                    Mathf.Sin(tempo * 0.15f) * 1.2f,
                    2.0f + avanco * 0.6f,
                    15f - avanco * 5f
                );
            _camera.transform.LookAt(origem + new Vector3(0f, 3.4f + brilho, -4f));

            // Magos gesticulam enquanto sustentam a esfera (da segunda fala dos magos até o clarão).
            var sustentando =
                tempo >= PrologoDoSol.Falas[2].Inicio
                && tempo < PrologoDoSol.TempoDoClarao + PrologoDoSol.DuracaoDoClarao * 0.5f;
            for (var i = 0; i < _animadoresDosMagos.Count; i++)
            {
                if (
                    _animadoresDosMagos[i] != null
                    && _animadoresDosMagos[i].runtimeAnimatorController != null
                )
                {
                    _animadoresDosMagos[i].SetBool(ParametrosDeNpc.Falando, sustentando);
                }
            }

            var fala = PrologoDoSol.FalaEm(tempo);
            var indice = fala == null ? int.MinValue : Array.IndexOf(PrologoDoSol.Falas, fala);
            if (indice != _falanteAtual)
            {
                _falanteAtual = indice;
                FalaMudou?.Invoke(fala);
            }

            ClaraoMudou?.Invoke(clarao);
        }

        private void OnDestroy()
        {
            if (_imagem != null)
            {
                _imagem.Release();
                Eliminar(_imagem);
            }
        }
    }
}
