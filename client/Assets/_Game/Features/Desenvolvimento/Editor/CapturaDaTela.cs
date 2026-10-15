using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Editor
{
    /// <summary>
    /// Grava a cena e/ou um painel do jogo em 1280×720, 1920×1080 e 2560×1440.
    /// A interface vai para um <see cref="RenderTexture"/> do tamanho da tela: é o mesmo
    /// tamanho que o Game view usaria, e a escala da interface acompanha essa resolução.
    /// Não entra no build.
    /// </summary>
    public static class CapturaDaTela
    {
        private static readonly Vector2Int[] Resolucoes =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
        };

        private static readonly Color FundoSemCena = new Color(0.08f, 0.09f, 0.1f, 1f);

        /// <summary>
        /// Salva um PNG por resolução. <paramref name="temCena"/> usa as câmeras da cena já aberta.
        /// <paramref name="painel"/> é a chave de uma janela (<c>itens.inventario</c>) ou de uma tela
        /// (<c>core.login</c>). A pasta vazia cai em <c>prints/</c> na raiz do repositório
        /// e, nessa pasta, os PNG da execução anterior saem antes de gravar os novos.
        /// </summary>
        /// <returns>0 se gravou; 1 se a cena ou o painel não pôde ser fotografado.</returns>
        public static int Salvar(bool temCena, string cena, string painel, string pasta)
        {
            if (!temCena && string.IsNullOrEmpty(painel))
            {
                return 2;
            }

            pasta = string.IsNullOrEmpty(pasta) ? PastaPadrao() : Path.GetFullPath(pasta);
            Directory.CreateDirectory(pasta);
            if (EhPastaPadrao(pasta))
            {
                LimparPngs(pasta);
            }

            var nome = NomeBase(cena, painel);
            var falhou = false;
            foreach (var resolucao in Resolucoes)
            {
                var imagem = Fotografar(temCena, painel, resolucao.x, resolucao.y);
                if (imagem == null)
                {
                    falhou = true;
                    continue;
                }

                var caminho = Path.Combine(pasta, $"{nome}-{resolucao.x}x{resolucao.y}.png");
                File.WriteAllBytes(caminho, imagem.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(imagem);
                Debug.Log($"[CapturaDaTela] {caminho}");
            }

            return falhou ? 1 : 0;
        }

        private static Texture2D Fotografar(bool temCena, string painel, int largura, int altura)
        {
            var mundo = temCena ? FotografarMundo(largura, altura) : null;
            if (temCena && mundo == null)
            {
                Debug.LogError("[CapturaDaTela] A cena não tem câmera.");
                return null;
            }

            Texture2D interfaceDoJogo = null;
            if (!string.IsNullOrEmpty(painel))
            {
                interfaceDoJogo = FotografarPainel(painel, largura, altura);
                if (interfaceDoJogo == null)
                {
                    UnityEngine.Object.DestroyImmediate(mundo);
                    return null;
                }
            }

            if (interfaceDoJogo == null)
            {
                return mundo;
            }

            var fundo = mundo ?? Fundo(largura, altura);
            var junto = Sobrepor(fundo, interfaceDoJogo);
            UnityEngine.Object.DestroyImmediate(fundo);
            UnityEngine.Object.DestroyImmediate(interfaceDoJogo);
            return junto;
        }

        private static Texture2D FotografarMundo(int largura, int altura)
        {
            Camera escolhida = null;
            foreach (
                var camera in UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Exclude
                )
            )
            {
                if (!camera.enabled || !camera.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (escolhida == null || camera.depth > escolhida.depth)
                {
                    escolhida = camera;
                }
            }

            if (escolhida == null)
            {
                return null;
            }

            var textura = new RenderTexture(largura, altura, 24, RenderTextureFormat.ARGB32);
            var anterior = escolhida.targetTexture;
            escolhida.targetTexture = textura;
            escolhida.Render();
            escolhida.targetTexture = anterior;

            var imagem = Ler(textura);
            textura.Release();
            UnityEngine.Object.DestroyImmediate(textura);
            return imagem;
        }

        private static Texture2D FotografarPainel(string chave, int largura, int altura)
        {
            PrepararRegistros();
            if (!JanelasDoJogo.EstaRegistrada(chave) && !Navegacao.EstaRegistrada(chave))
            {
                Debug.LogError(
                    $"[CapturaDaTela] Nenhum painel \"{chave}\". Janelas: {ChavesDasJanelas()}. Telas: {ChavesDasTelas()}."
                );
                return null;
            }

            var configuracao = EstiloCore.CriarConfiguracaoDoPainel("CapturaDaTela", 0f);
            var destino = new RenderTexture(largura, altura, 0, RenderTextureFormat.ARGB32);
            destino.Create();
            configuracao.targetTexture = destino;

            var objeto = new GameObject("CapturaDaTela");
            objeto.hideFlags = HideFlags.HideAndDontSave;
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            objeto.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            var container = new VisualElement { name = "Telas" };
            container.AddToClassList(EstiloCore.RaizInterface);
            EstiloCore.NaoBloquearCliques(container);
            documento.rootVisualElement.Add(container);

            GerenteDeJanelas gerente = null;
            try
            {
                if (JanelasDoJogo.EstaRegistrada(chave))
                {
                    var apresentador = new ApresentadorDeJanelas(container, () => { });
                    gerente = new GerenteDeJanelas(
                        JanelasDoJogo.Registro,
                        apresentador.Mostrar,
                        apresentador.Esconder
                    );
                    JanelasDoJogo.Ligar(gerente);
                    if (!JanelasDoJogo.Abrir(chave))
                    {
                        Debug.LogError($"[CapturaDaTela] A janela \"{chave}\" não abriu.");
                        return null;
                    }
                }
                else
                {
                    Navegacao.Inicializar(container);
                    if (!Navegacao.Abrir(chave))
                    {
                        Debug.LogError($"[CapturaDaTela] A tela \"{chave}\" não abriu.");
                        return null;
                    }
                }

                RepintarPainelForaDaTela();
                return Ler(destino);
            }
            finally
            {
                if (gerente != null)
                {
                    gerente.Fechar();
                    JanelasDoJogo.Desligar(gerente);
                }

                Navegacao.FecharTelaAtual();
                UnityEngine.Object.DestroyImmediate(objeto);
                destino.Release();
                UnityEngine.Object.DestroyImmediate(destino);
                UnityEngine.Object.DestroyImmediate(configuracao);
            }
        }

        /// <summary>
        /// O painel com <c>targetTexture</c> só é desenhado na repintura interna do UI Toolkit.
        /// No executeMethod não há frame de jogo, então a repintura é chamada direto.
        /// </summary>
        private static void RepintarPainelForaDaTela()
        {
            var tipo = typeof(UIDocument).Assembly.GetType(
                "UnityEngine.UIElements.UIElementsRuntimeUtility"
            );
            if (tipo == null)
            {
                Debug.LogError("[CapturaDaTela] UIElementsRuntimeUtility não encontrado.");
                return;
            }

            Invocar(tipo, "UpdatePanels");
            Invocar(tipo, "RepaintPanels", true);
            Invocar(tipo, "RenderOffscreenPanels");
        }

        private static void Invocar(Type tipo, string nome, params object[] argumentos)
        {
            var tipos = new Type[argumentos.Length];
            for (var i = 0; i < argumentos.Length; i++)
            {
                tipos[i] = argumentos[i].GetType();
            }

            var metodo = tipo.GetMethod(
                nome,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                tipos,
                null
            );
            if (metodo == null)
            {
                Debug.LogError($"[CapturaDaTela] Método interno ausente: {nome}.");
                return;
            }

            metodo.Invoke(null, argumentos);
        }

        private static void PrepararRegistros()
        {
            InvocarSemArgumento(typeof(Navegacao), "Reiniciar");
            InvocarSemArgumento(typeof(JanelasDoJogo), "Reiniciar");

            Navegacao.Registrar(TelasCore.Abertura, () => new TelaAbertura());
            Navegacao.Registrar(TelasCore.Login, () => new TelaLogin());
            Navegacao.Registrar(TelasCore.SemTelaAposLogin, () => new TelaSemTelaAposLogin());

            var registrarJanela = typeof(JanelasDoJogo).GetMethod(nameof(JanelasDoJogo.Registrar));
            var registrarTela = typeof(Navegacao).GetMethod(nameof(Navegacao.Registrar));
            foreach (var metodo in MetodosQueChamam(registrarJanela, registrarTela))
            {
                try
                {
                    metodo.Invoke(null, null);
                }
                catch (TargetInvocationException e)
                {
                    Debug.LogWarning(
                        $"[CapturaDaTela] {metodo.DeclaringType?.Name}.{metodo.Name}: {e.InnerException?.Message}"
                    );
                }
            }
        }

        private static void InvocarSemArgumento(Type tipo, string nome)
        {
            var metodo = tipo.GetMethod(
                nome,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );
            metodo?.Invoke(null, null);
        }

        private static IEnumerable<MethodInfo> MetodosQueChamam(params MethodInfo[] alvos)
        {
            const BindingFlags Todos =
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Static
                | BindingFlags.Instance
                | BindingFlags.DeclaredOnly;

            foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies())
            {
                var nome = assembly.GetName().Name ?? "";
                if (!nome.StartsWith("JogoDoGrupo.Client.", StringComparison.Ordinal))
                {
                    continue;
                }

                if (
                    nome.EndsWith(".Tests", StringComparison.Ordinal)
                    || nome.EndsWith(".Editor", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                foreach (var tipo in TiposDe(assembly))
                {
                    // O inicializador do Core também chama Registrar, e no meio sobe sessão e rede.
                    // As funcionalidades registram a própria tela ou janela e param por aí.
                    var espaco = tipo.Namespace ?? "";
                    if (espaco.IndexOf(".Features.", StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    foreach (var metodo in tipo.GetMethods(Todos))
                    {
                        if (!metodo.IsStatic || metodo.GetParameters().Length > 0)
                        {
                            continue;
                        }

                        if (ChamaAlgum(metodo, alvos))
                        {
                            yield return metodo;
                        }
                    }
                }
            }
        }

        private static IEnumerable<Type> TiposDe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return SemNulos(e.Types);
            }
        }

        private static IEnumerable<Type> SemNulos(Type[] tipos)
        {
            foreach (var tipo in tipos)
            {
                if (tipo != null)
                {
                    yield return tipo;
                }
            }
        }

        private static bool ChamaAlgum(MethodInfo metodo, MethodInfo[] alvos)
        {
            byte[] il;
            try
            {
                il = metodo.GetMethodBody()?.GetILAsByteArray();
            }
            catch (Exception)
            {
                return false;
            }

            if (il == null)
            {
                return false;
            }

            const byte Call = 0x28;
            for (var i = 0; i + 4 < il.Length; i++)
            {
                if (il[i] != Call)
                {
                    continue;
                }

                var token = BitConverter.ToInt32(il, i + 1);
                try
                {
                    var resolvido = metodo.Module.ResolveMethod(token);
                    foreach (var alvo in alvos)
                    {
                        if (resolvido == alvo)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    // Não era um token de método.
                }
            }

            return false;
        }

        private static string ChavesDasJanelas()
        {
            var texto = new StringBuilder();
            foreach (var definicao in JanelasDoJogo.Registro.Definicoes)
            {
                if (texto.Length > 0)
                {
                    texto.Append(", ");
                }

                texto.Append(definicao.Chave);
            }

            return texto.Length == 0 ? "(nenhuma)" : texto.ToString();
        }

        private static string ChavesDasTelas()
        {
            var campo = typeof(Navegacao).GetField(
                "_fabricas",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            if (!(campo?.GetValue(null) is IDictionary fabricas) || fabricas.Count == 0)
            {
                return "(nenhuma)";
            }

            var texto = new StringBuilder();
            foreach (var chave in fabricas.Keys)
            {
                if (texto.Length > 0)
                {
                    texto.Append(", ");
                }

                texto.Append(chave);
            }

            return texto.ToString();
        }

        private static Texture2D Ler(RenderTexture textura)
        {
            var anterior = RenderTexture.active;
            RenderTexture.active = textura;
            var imagem = new Texture2D(textura.width, textura.height, TextureFormat.RGBA32, false);
            imagem.ReadPixels(new Rect(0, 0, textura.width, textura.height), 0, 0);
            imagem.Apply();
            RenderTexture.active = anterior;
            return imagem;
        }

        private static Texture2D Fundo(int largura, int altura)
        {
            var imagem = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
            var pixels = new Color[largura * altura];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = FundoSemCena;
            }

            imagem.SetPixels(pixels);
            imagem.Apply();
            return imagem;
        }

        private static Texture2D Sobrepor(Texture2D fundo, Texture2D frente)
        {
            var largura = fundo.width;
            var altura = fundo.height;
            var basePixels = fundo.GetPixels();
            var frentePixels = frente.GetPixels();
            var saida = new Color[basePixels.Length];
            for (var i = 0; i < saida.Length; i++)
            {
                var cor = frentePixels[i];
                var atras = basePixels[i];
                var alfa = cor.a;
                saida[i] = new Color(
                    cor.r * alfa + atras.r * (1f - alfa),
                    cor.g * alfa + atras.g * (1f - alfa),
                    cor.b * alfa + atras.b * (1f - alfa),
                    1f
                );
            }

            var imagem = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
            imagem.SetPixels(saida);
            imagem.Apply();
            return imagem;
        }

        private static string PastaPadrao()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "prints"));
        }

        private static bool EhPastaPadrao(string pasta)
        {
            return string.Equals(
                Normalizar(pasta),
                Normalizar(PastaPadrao()),
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static string Normalizar(string pasta)
        {
            return Path.GetFullPath(pasta)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void LimparPngs(string pasta)
        {
            if (!Directory.Exists(pasta))
            {
                return;
            }

            foreach (var arquivo in Directory.GetFiles(pasta, "*.png"))
            {
                File.Delete(arquivo);
            }
        }

        private static string NomeBase(string cena, string painel)
        {
            var nome = !string.IsNullOrEmpty(painel)
                ? painel
                : Path.GetFileNameWithoutExtension(cena);
            var limpo = new StringBuilder(nome.Length);
            foreach (var caractere in nome)
            {
                var invalido =
                    caractere == '.'
                    || Array.IndexOf(Path.GetInvalidFileNameChars(), caractere) >= 0;
                limpo.Append(invalido ? '-' : caractere);
            }

            return limpo.ToString();
        }
    }
}
