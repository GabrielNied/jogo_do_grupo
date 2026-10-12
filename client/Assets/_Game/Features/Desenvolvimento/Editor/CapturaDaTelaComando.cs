using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Editor
{
    /// <summary>
    /// Entrada em modo batch do print da tela.
    /// Com o editor fechado:
    /// <c>npm run unity:executar -- JogoDoGrupo.Client.Features.Desenvolvimento.Editor.CapturaDaTelaComando.Capturar</c>.
    /// </summary>
    public static class CapturaDaTelaComando
    {
        public static void Capturar()
        {
            var codigo = 1;
            try
            {
                var cena = Environment.GetEnvironmentVariable("PRINT_CENA");
                var painel = Environment.GetEnvironmentVariable("PRINT_PAINEL");
                var pasta = Environment.GetEnvironmentVariable("PRINT_SAIDA");
                if (string.IsNullOrWhiteSpace(cena) && string.IsNullOrWhiteSpace(painel))
                {
                    Debug.LogError(
                        "[CapturaDaTela] Defina PRINT_CENA (Assets/....unity) e/ou PRINT_PAINEL (chave da janela ou da tela)."
                    );
                    codigo = 2;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(cena) && !CenaExiste(cena))
                {
                    Debug.LogError($"[CapturaDaTela] Cena não encontrada: {cena}");
                    codigo = 1;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(cena))
                {
                    EditorSceneManager.OpenScene(cena.Trim(), OpenSceneMode.Single);
                }

                codigo = CapturaDaTela.Salvar(
                    !string.IsNullOrWhiteSpace(cena),
                    cena,
                    string.IsNullOrWhiteSpace(painel) ? null : painel.Trim(),
                    string.IsNullOrWhiteSpace(pasta) ? null : pasta.Trim()
                );
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                codigo = 1;
            }
            finally
            {
                // Fora do batch, Exit fecharia o editor da pessoa.
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(codigo);
                }
            }
        }

        private static bool CenaExiste(string cena)
        {
            var relativo = cena.Trim().Replace('\\', '/');
            var absoluto = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativo));
            return File.Exists(absoluto);
        }
    }
}
