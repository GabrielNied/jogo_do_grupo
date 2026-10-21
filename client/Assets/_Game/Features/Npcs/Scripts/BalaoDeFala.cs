using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Balão de fala curto sobre a cabeça de um NPC ambiente (#1069). Texto 3D com fundo escuro,
    /// sempre virado para a câmera; some sozinho depois da duração pedida.
    /// </summary>
    public sealed class BalaoDeFala : MonoBehaviour
    {
        private const float AlturaPadrao = 2.7f;
        private const int LarguraDaLinha = 26;

        private TextMesh _texto;
        private GameObject _fundo;
        private float _fimEm;

        public bool Visivel => _texto != null && _texto.gameObject.activeSelf;

        public static BalaoDeFala Criar(Transform pai, float altura = AlturaPadrao)
        {
            var objeto = new GameObject("BalaoDeFala");
            objeto.transform.SetParent(pai, false);
            objeto.transform.localPosition = new Vector3(0f, altura, 0f);
            var balao = objeto.AddComponent<BalaoDeFala>();
            balao.Montar();
            balao.Esconder();
            return balao;
        }

        private void Montar()
        {
            var objetoTexto = new GameObject("Texto");
            objetoTexto.transform.SetParent(transform, false);
            _texto = objetoTexto.AddComponent<TextMesh>();
            _texto.alignment = TextAlignment.Center;
            _texto.anchor = TextAnchor.MiddleCenter;
            _texto.characterSize = 0.07f;
            _texto.fontSize = 36;
            _texto.color = new Color(1f, 0.97f, 0.88f);

            _fundo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _fundo.name = "Fundo";
            var colisor = _fundo.GetComponent<Collider>();
            if (colisor != null)
            {
                Destroy(colisor);
            }

            _fundo.transform.SetParent(transform, false);
            _fundo.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            var renderer = _fundo.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.1f, 0.09f, 0.08f);
            }
        }

        /// <summary>Mostra o texto por <paramref name="duracao"/> segundos.</summary>
        public void Mostrar(string fala, float duracao)
        {
            if (_texto == null || string.IsNullOrEmpty(fala))
            {
                return;
            }

            var quebrado = Quebrar(fala, LarguraDaLinha);
            _texto.text = quebrado;
            var linhas = 1;
            for (var i = 0; i < quebrado.Length; i++)
            {
                if (quebrado[i] == '\n')
                {
                    linhas++;
                }
            }

            var maior = 0;
            foreach (var linha in quebrado.Split('\n'))
            {
                maior = Mathf.Max(maior, linha.Length);
            }

            // Largura e altura aproximadas do texto em metros (fonte padrão do TextMesh).
            _fundo.transform.localScale = new Vector3(
                maior * 0.088f + 0.25f,
                linhas * 0.2f + 0.15f,
                1f
            );
            _texto.gameObject.SetActive(true);
            _fundo.SetActive(true);
            _fimEm = Time.time + duracao;
        }

        public void Esconder()
        {
            if (_texto != null)
            {
                _texto.gameObject.SetActive(false);
            }

            if (_fundo != null)
            {
                _fundo.SetActive(false);
            }

            _fimEm = 0f;
        }

        private void LateUpdate()
        {
            if (!Visivel)
            {
                return;
            }

            if (Time.time >= _fimEm)
            {
                Esconder();
                return;
            }

            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        /// <summary>Quebra o texto em linhas de até <paramref name="largura"/> caracteres, nas palavras.</summary>
        public static string Quebrar(string texto, int largura)
        {
            var palavras = texto.Split(' ');
            var resultado = new System.Text.StringBuilder();
            var linhaAtual = 0;
            for (var i = 0; i < palavras.Length; i++)
            {
                var palavra = palavras[i];
                if (linhaAtual > 0 && linhaAtual + 1 + palavra.Length > largura)
                {
                    resultado.Append('\n');
                    linhaAtual = 0;
                }
                else if (linhaAtual > 0)
                {
                    resultado.Append(' ');
                    linhaAtual++;
                }

                resultado.Append(palavra);
                linhaAtual += palavra.Length;
            }

            return resultado.ToString();
        }
    }
}
