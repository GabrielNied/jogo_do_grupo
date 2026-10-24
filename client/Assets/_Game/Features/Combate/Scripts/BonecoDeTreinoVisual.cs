using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O boneco de treino da cidade (<see cref="RegrasBonecoDeTreino"/>): o modelo de palha CC0
    /// (prefab <c>Combate/BonecoDeTreino</c>, gerado por <c>GeradorDoBonecoDeTreino</c>), ou um
    /// poste de primitivas se o prefab faltar. A vida aparece na etiqueta de UI no estilo dos
    /// inimigos (<see cref="EtiquetaDoBoneco"/>), sem barra 3D. Só mostra o que o servidor
    /// confirma: a vida vem de <see cref="AtaqueAcertou"/> e a volta à vida cheia, depois de
    /// <see cref="RegrasBonecoDeTreino.SegundosParaRecuperar"/> sem golpes, é a mesma conta que o
    /// servidor faz. O flash e o recuo são do <see cref="ControleImpacto"/>, que acha o boneco pelo
    /// <see cref="RegistroDeAlvos"/>.
    /// </summary>
    internal sealed class BonecoDeTreinoVisual : MonoBehaviour
    {
        public const string Nome = "Boneco de treino";
        public const string CaminhoDoPrefab = "Combate/BonecoDeTreino";

        /// <summary>Folga, em metros, entre o alto do boneco e a etiqueta.</summary>
        public const float AlturaDaEtiqueta = 0.35f;

        private static readonly Color CorDaMadeira = new Color(0.45f, 0.32f, 0.2f);
        private static readonly Color CorDaPalha = new Color(0.78f, 0.66f, 0.38f);

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly List<Material> _materiais = new List<Material>();
        private Vector3 _posicao;
        private EtiquetaDoBoneco _etiqueta;
        private int _vida = RegrasBonecoDeTreino.Vida;
        private int _vidaMaxima = RegrasBonecoDeTreino.Vida;
        private float _ultimoGolpe = float.NegativeInfinity;
        private bool _mostrarEtiqueta;

        /// <summary>Monta o boneco na posição do shared, sobre o chão.</summary>
        public static BonecoDeTreinoVisual Criar(VisualElement camadaDaEtiqueta = null)
        {
            var raiz = new GameObject(Nome);
            var boneco = raiz.AddComponent<BonecoDeTreinoVisual>();
            boneco.Montar(camadaDaEtiqueta);
            return boneco;
        }

        /// <summary>A fração de vida (0 a 1) que a barra mostra agora.</summary>
        public float Fracao => _vidaMaxima <= 0 ? 0f : Mathf.Clamp01((float)_vida / _vidaMaxima);

        /// <summary>
        /// Se a etiqueta (nome e barra) aparece agora: só nos segundos depois de um golpe, até a
        /// vida voltar a encher; parado e inteiro, o boneco fica sem etiqueta.
        /// </summary>
        public bool EtiquetaVisivel(float agora) =>
            agora - _ultimoGolpe < RegrasBonecoDeTreino.SegundosParaRecuperar;

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

            _etiqueta?.MostrarVida(Fracao);
            _mostrarEtiqueta = EtiquetaVisivel(agora);
        }

        private void Montar(VisualElement camadaDaEtiqueta)
        {
            _posicao = new Vector3(
                (float)RegrasBonecoDeTreino.X,
                ChaoEm((float)RegrasBonecoDeTreino.X, (float)RegrasBonecoDeTreino.Z),
                (float)RegrasBonecoDeTreino.Z
            );
            transform.position = _posicao;

            RegistroDeAlvos.Registrar(RegrasBonecoDeTreino.AlvoId, gameObject);
            if (!MontarModelo())
            {
                MontarDePrimitivas();
            }

            if (camadaDaEtiqueta != null)
            {
                _etiqueta = new EtiquetaDoBoneco(camadaDaEtiqueta, Nome);
            }

            Atualizar(Time.unscaledTime);
        }

        private bool MontarModelo()
        {
            var prefab = Resources.Load<GameObject>(CaminhoDoPrefab);
            if (prefab == null)
            {
                return false;
            }

            var modelo = Instantiate(prefab, transform);
            modelo.name = "Modelo";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            foreach (var colisor in modelo.GetComponentsInChildren<Collider>())
            {
                RemoverColisor(colisor.gameObject);
            }

            return true;
        }

        private void MontarDePrimitivas()
        {
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
            _etiqueta?.Remover();
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

        // A etiqueta de vida segue a cabeça do boneco na tela.
        private void LateUpdate()
        {
            _etiqueta?.Posicionar(
                _mostrarEtiqueta ? Camera.main : null,
                _posicao + Vector3.up * ((float)RegrasBonecoDeTreino.Altura + AlturaDaEtiqueta)
            );
        }
    }
}
