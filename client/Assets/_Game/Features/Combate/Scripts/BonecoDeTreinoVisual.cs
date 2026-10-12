using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O boneco de treino da cidade (<see cref="RegrasBonecoDeTreino"/>): um poste com tronco e
    /// cabeça de palha montado em código, com barra de vida sobre a cabeça. Só mostra o que o
    /// servidor confirma: a vida vem de <see cref="AtaqueAcertou"/> e a volta à vida cheia, depois
    /// de <see cref="RegrasBonecoDeTreino.SegundosParaRecuperar"/> sem golpes, é a mesma conta que o
    /// servidor faz. O flash e o recuo são do <see cref="ControleImpacto"/>, que acha o boneco pelo
    /// <see cref="RegistroDeAlvos"/>.
    /// </summary>
    internal sealed class BonecoDeTreinoVisual : MonoBehaviour
    {
        public const string Nome = "Boneco de treino";
        public const float LarguraDaBarra = 1.1f;
        public const float AlturaDaBarra = 0.12f;
        public const float AlturaSobreACabeca = 0.25f;

        private static readonly Color CorDaMadeira = new Color(0.45f, 0.32f, 0.2f);
        private static readonly Color CorDaPalha = new Color(0.78f, 0.66f, 0.38f);
        private static readonly Color CorDoFundoDaBarra = new Color(0.08f, 0.06f, 0.06f, 0.85f);
        private static readonly Color CorDaVida = new Color(0.78f, 0.18f, 0.16f);
        private static readonly Color CorDaVidaZerada = new Color(0.3f, 0.3f, 0.3f);

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly List<Material> _materiais = new List<Material>();
        private Vector3 _posicao;
        private Transform _barra;
        private Transform _preenchimento;
        private Renderer _renderPreenchimento;
        private int _vida = RegrasBonecoDeTreino.Vida;
        private int _vidaMaxima = RegrasBonecoDeTreino.Vida;
        private float _ultimoGolpe = float.NegativeInfinity;

        /// <summary>Monta o boneco na posição do shared, sobre o chão.</summary>
        public static BonecoDeTreinoVisual Criar()
        {
            var raiz = new GameObject(Nome);
            var boneco = raiz.AddComponent<BonecoDeTreinoVisual>();
            boneco.Montar();
            return boneco;
        }

        /// <summary>A fração de vida (0 a 1) que a barra mostra agora.</summary>
        public float Fracao => _vidaMaxima <= 0 ? 0f : Mathf.Clamp01((float)_vida / _vidaMaxima);

        /// <summary>Aplica um acerto confirmado pelo servidor (também usado nos testes).</summary>
        public void AplicarAcerto(AcertoDeAtaque acerto, float agora)
        {
            if (acerto == null || acerto.AlvoId != RegrasBonecoDeTreino.AlvoId)
            {
                return;
            }

            _vida = Mathf.Max(0, acerto.VidaRestante);
            _vidaMaxima = acerto.VidaMaxima > 0 ? acerto.VidaMaxima : _vidaMaxima;
            _ultimoGolpe = agora;
            Atualizar(agora);
        }

        /// <summary>Volta à vida cheia se passou o tempo de recuperação desde o último golpe.</summary>
        public void Atualizar(float agora)
        {
            if (
                _vida < _vidaMaxima
                && agora - _ultimoGolpe >= RegrasBonecoDeTreino.SegundosParaRecuperar
            )
            {
                _vida = _vidaMaxima;
            }

            if (_preenchimento != null)
            {
                var fracao = Fracao;
                _preenchimento.localScale = new Vector3(
                    Mathf.Max(0.0001f, LarguraDaBarra * fracao),
                    AlturaDaBarra,
                    1f
                );
                _preenchimento.localPosition = new Vector3(
                    -LarguraDaBarra * (1f - fracao) * 0.5f,
                    0f,
                    -0.001f
                );
                _renderPreenchimento.sharedMaterial.color =
                    fracao <= 0f ? CorDaVidaZerada : CorDaVida;
            }
        }

        private void Montar()
        {
            _posicao = new Vector3(
                (float)RegrasBonecoDeTreino.X,
                ChaoEm((float)RegrasBonecoDeTreino.X, (float)RegrasBonecoDeTreino.Z),
                (float)RegrasBonecoDeTreino.Z
            );
            transform.position = _posicao;

            RegistroDeAlvos.Registrar(RegrasBonecoDeTreino.AlvoId, gameObject);
            Parte(
                PrimitiveType.Cylinder,
                "Base",
                new Vector3(0f, 0.05f, 0f),
                new Vector3(0.9f, 0.05f, 0.9f),
                CorDaMadeira
            );
            Parte(
                PrimitiveType.Cylinder,
                "Poste",
                new Vector3(0f, 0.7f, 0f),
                new Vector3(0.14f, 0.7f, 0.14f),
                CorDaMadeira
            );
            Parte(
                PrimitiveType.Capsule,
                "Tronco",
                new Vector3(0f, 1.1f, 0f),
                new Vector3(0.62f, 0.4f, 0.62f),
                CorDaPalha
            );
            Parte(
                PrimitiveType.Cylinder,
                "Braco",
                new Vector3(0f, 1.25f, 0f),
                new Vector3(0.1f, 0.5f, 0.1f),
                CorDaMadeira
            ).localRotation = Quaternion.Euler(0f, 0f, 90f);
            Parte(
                PrimitiveType.Sphere,
                "Cabeca",
                new Vector3(0f, 1.62f, 0f),
                new Vector3(0.36f, 0.36f, 0.36f),
                CorDaPalha
            );

            MontarBarra();
            Atualizar(Time.unscaledTime);
        }

        private Transform Parte(
            PrimitiveType forma,
            string nome,
            Vector3 local,
            Vector3 escala,
            Color cor
        )
        {
            var parte = GameObject.CreatePrimitive(forma);
            parte.name = nome;
            RemoverColisor(parte);

            parte.transform.SetParent(transform, false);
            parte.transform.localPosition = local;
            parte.transform.localScale = escala;
            parte.GetComponent<Renderer>().sharedMaterial = NovoMaterial(
                "Universal Render Pipeline/Lit",
                cor
            );
            return parte.transform;
        }

        private void MontarBarra()
        {
            var caixa = new GameObject("Barra de vida");
            _barra = caixa.transform;
            _barra.SetParent(transform, false);
            _barra.localPosition = new Vector3(
                0f,
                (float)RegrasBonecoDeTreino.Altura + AlturaSobreACabeca,
                0f
            );

            var fundo = Quadro("Fundo", CorDoFundoDaBarra);
            fundo.localScale = new Vector3(LarguraDaBarra + 0.04f, AlturaDaBarra + 0.04f, 1f);
            _preenchimento = Quadro("Vida", CorDaVida);
            _renderPreenchimento = _preenchimento.GetComponent<Renderer>();
        }

        private Transform Quadro(string nome, Color cor)
        {
            var quadro = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadro.name = nome;
            RemoverColisor(quadro);

            quadro.transform.SetParent(_barra, false);
            var render = quadro.GetComponent<Renderer>();
            // Sem iluminação e sem sombra: a barra se lê de longe, de qualquer lado.
            render.sharedMaterial = NovoMaterial("Sprites/Default", cor);
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            return quadro.transform;
        }

        // O boneco nao bloqueia ninguem: o servidor nao o conhece como obstaculo. Nos testes de edicao
        // o Destroy comum nao vale.
        private static void RemoverColisor(GameObject objeto)
        {
            var colisor = objeto.GetComponent<Collider>();
            if (colisor == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(colisor);
            }
            else
            {
                DestroyImmediate(colisor);
            }
        }

        private Material NovoMaterial(string shader, Color cor)
        {
            var material = new Material(Shader.Find(shader) ?? Shader.Find("Sprites/Default"))
            {
                color = cor,
            };
            _materiais.Add(material);
            return material;
        }

        private static float ChaoEm(float x, float z)
        {
            if (Physics.Raycast(new Vector3(x, 50f, z), Vector3.down, out var batida, 100f))
            {
                return batida.point.y;
            }

            return 0f;
        }

        private void OnEnable()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueAcertou>(
                    CombateMessageIds.AtaqueAcertou,
                    AoAcertarAtaque
                )
            );
        }

        private void OnDestroy()
        {
            RegistroDeAlvos.Remover(RegrasBonecoDeTreino.AlvoId, gameObject);
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

        private void OnDisable()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
        }

        private void AoAcertarAtaque(AtaqueAcertou ataque)
        {
            if (ataque.Acertos == null)
            {
                return;
            }

            foreach (var acerto in ataque.Acertos)
            {
                AplicarAcerto(acerto, Time.unscaledTime);
            }
        }

        // Antes do ControleImpacto (500): ele soma o recuo à posição de cada quadro, então o
        // boneco volta ao lugar dele antes disso.
        private void Update()
        {
            transform.position = _posicao;
            Atualizar(Time.unscaledTime);
        }

        // A barra de vida olha para a câmera.
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (_barra != null && camera != null)
            {
                _barra.rotation = camera.transform.rotation;
            }
        }
    }
}
