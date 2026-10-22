using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    /// <summary>Verifica os controles sem salvar cenas, deformar Terrain ou criar assets.</summary>
    public static class ValidacaoVisualDasEstradas
    {
        [MenuItem("Jogo do Grupo/Mundo/Estradas/Validar bordas e cascalhos")]
        public static void Validar()
        {
            var objeto = new GameObject("Validação temporária das bordas")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            var malhas = new List<Mesh>();
            try
            {
                var estrada = objeto.AddComponent<EstradaNativa>();
                var dados = new SerializedObject(estrada);
                Definir(dados, "_variacaoLateral", 0f);
                var pontos = new List<Vector3>();
                for (var i = 0; i <= 4; i++)
                    pontos.Add(new Vector3(0f, 0f, i * 5f));
                var depressao = PerfilDaEstrada.Depressao(estrada, Vector3.zero, 2.5f);
                var raio = PerfilDaEstrada.RaioMaximo(estrada);
                Definir(dados, "_transicaoVisual", 4f);
                Verificar(
                    Mathf.Approximately(
                        depressao,
                        PerfilDaEstrada.Depressao(estrada, Vector3.zero, 2.5f)
                    ),
                    "Transição visual alterou o rebaixo físico."
                );
                Verificar(
                    PerfilDaEstrada.RaioMaximo(estrada) > raio,
                    "O corredor de proteção não acompanhou a borda visual."
                );
                var superficie = VisualDaEstrada.CriarSuperficie(estrada, pontos, _ => 0f);
                malhas.Add(superficie);
                VerificarBorda(superficie, -6f, 0f);
                VerificarBorda(superficie, -4f, 0.5f);
                VerificarBorda(superficie, -2f, 1f);
                VerificarBorda(superficie, 0f, 1f);
                VerificarBorda(superficie, 2f, 1f);
                VerificarBorda(superficie, 4f, 0.5f);
                VerificarBorda(superficie, 6f, 0f);
                Definir(dados, "_transicao", 5f);
                var relevoAlterado = VisualDaEstrada.CriarSuperficie(estrada, pontos, _ => 0f);
                malhas.Add(relevoAlterado);
                Verificar(
                    superficie.uv.SequenceEqual(relevoAlterado.uv)
                        && superficie.colors.SequenceEqual(relevoAlterado.colors),
                    "Suavização do relevo alterou a faixa de transparência."
                );
                Verificar(
                    VisualDaEstrada.QuantidadeDePedregulhos(estrada, pontos) == 160,
                    "A densidade padrão precisa produzir duas pedras por m²."
                );
                var pedras = VisualDaEstrada.CriarPedregulhos(estrada, pontos, _ => 0f);
                var repetidas = VisualDaEstrada.CriarPedregulhos(estrada, pontos, _ => 0f);
                malhas.Add(pedras);
                malhas.Add(repetidas);
                Verificar(
                    pedras.vertexCount == 160 * 6
                        && pedras.vertices.SequenceEqual(repetidas.vertices),
                    "Cascalhos não reproduzem a quantidade ou a semente."
                );
                Verificar(
                    Enumerable
                        .Range(0, 160)
                        .Any(i => Mathf.Abs(pedras.vertices[i * 6 + 4].x) < 0.5f),
                    "Faltam cascalhos no centro do leito."
                );
                Definir(dados, "_densidadeCascalhos", 4f);
                Verificar(
                    VisualDaEstrada.QuantidadeDePedregulhos(estrada, pontos) == 320,
                    "Dobrar a densidade não dobrou a quantidade."
                );
                Definir(dados, "_densidadeCascalhos", 0f);
                var vazia = VisualDaEstrada.CriarPedregulhos(estrada, pontos, _ => 0f);
                malhas.Add(vazia);
                Verificar(
                    vazia.vertexCount == 0,
                    "Densidade zero precisa remover todas as pedras."
                );
                Definir(dados, "_densidadeCascalhos", 8f);
                var longa = new[] { Vector3.zero, Vector3.forward * 20000f };
                var limitada = VisualDaEstrada.CriarPedregulhos(estrada, longa, _ => 0f);
                malhas.Add(limitada);
                Verificar(
                    limitada.vertexCount == VisualDaEstrada.LimiteDePedregulhos * 6,
                    "O limite de geometria dos cascalhos falhou."
                );
                Verificar(
                    estrada.CorCascalhos.r > estrada.CorCascalhos.b,
                    "O tom padrão dos cascalhos precisa ser terroso."
                );
                Debug.Log(
                    "[Estradas] BORDAS E CASCALHOS APROVADOS: transparência métrica, leito opaco, relevo independente, corredor, densidade, centro, semente, zero e limite."
                );
            }
            finally
            {
                foreach (var malha in malhas)
                    Object.DestroyImmediate(malha);
                Object.DestroyImmediate(objeto);
            }
        }

        private static void Definir(SerializedObject dados, string campo, float valor)
        {
            dados.FindProperty(campo).floatValue = valor;
            dados.ApplyModifiedPropertiesWithoutUndo();
            dados.Update();
        }

        private static void VerificarBorda(Mesh malha, float lateral, float alpha)
        {
            var uvs = malha.uv;
            var cores = malha.colors;
            var indice = Array.FindIndex(
                uvs,
                uv => Mathf.Abs(uv.x - lateral) < 0.001f && Mathf.Abs(uv.y - 10f) < 0.001f
            );
            Verificar(
                indice >= 0 && Mathf.Abs(cores[indice].a - alpha) < 0.001f,
                "Transparência incorreta a " + lateral + " m do centro."
            );
        }

        private static void Verificar(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException(mensagem);
        }
    }
}
