using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Transforma os reports pendentes em issues sem a pessoa rodar nada: no editor, roda
    /// <c>node scripts/relatos/reporte.mjs enviar</c> em segundo plano depois de cada report e
    /// ao entrar no Play Mode (para os que ficaram pendentes). O token é o do <c>gh</c> da
    /// máquina; o jogo não guarda nenhum. Falhou (sem internet, <c>gh</c> sem login): o report
    /// fica em <c>relatos/pendentes/</c> e vai na próxima vez. No build, só grava.
    /// </summary>
    internal static class EnvioDeReporte
    {
        private const string Script = "scripts/relatos/reporte.mjs";
        private const string Fim = "\u0000fim";

        private static readonly ConcurrentQueue<string> _saida = new ConcurrentQueue<string>();
        private static readonly List<ItemDoHistorico> _historico = new List<ItemDoHistorico>();

        private static Process _processo;
        private static int _geracao;
        private static float _limite;
        private static bool _deNovo;

        /// <summary>Algo do histórico mudou (envio terminou ou falhou).</summary>
        public static event Action Mudou;

        /// <summary>Os reports desta sessão, do mais antigo ao mais novo.</summary>
        public static IReadOnlyList<ItemDoHistorico> Historico => _historico;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _historico.Clear();
            while (_saida.TryDequeue(out _)) { }
            _processo = null;
            _deNovo = false;
            Mudou = null;
        }

        /// <summary>Um report acabou de ser gravado: entra no histórico e o envio começa.</summary>
        public static void Registrar(string id, string frase)
        {
            var item = new ItemDoHistorico(id, frase);
            _historico.Add(item);
            if (!Application.isEditor)
            {
                item.Situacao =
                    "salvo (no build o envio é manual: npm run reporte -- enviar --pasta <persistentDataPath>/relatos)";
                AvisosDoJogo.Mostrar("Report salvo. No build, envie pelo npm run reporte.");
                Mudou?.Invoke();
                return;
            }

            AvisosDoJogo.Mostrar("Report salvo. Abrindo a issue...");
            Iniciar();
        }

        /// <summary>Ao entrar no Play Mode: envia o que ficou pendente de outra vez.</summary>
        public static void EnviarPendentes()
        {
            if (!Application.isEditor || !Directory.Exists(EscritorDeRelato.PastaDosPendentes))
            {
                return;
            }

            if (Directory.GetDirectories(EscritorDeRelato.PastaDosPendentes).Length > 0)
            {
                Iniciar();
            }
        }

        /// <summary>Chamado todo quadro pelo gravador: lê a saída do script na thread principal.</summary>
        public static void Atualizar()
        {
            while (_saida.TryDequeue(out var linha))
            {
                if (linha.StartsWith(Fim, StringComparison.Ordinal))
                {
                    // O fim de um processo já abandonado (passou do tempo) não encerra o atual.
                    if (linha == Fim + _geracao)
                    {
                        Terminar();
                    }
                }
                else
                {
                    Ler(linha);
                }
            }

            if (_processo != null && Time.realtimeSinceStartup > _limite)
            {
                Debug.LogWarning(
                    "[Relato] O envio do report passou do tempo; tenta de novo no próximo."
                );
                try
                {
                    _processo.Kill();
                }
                catch (InvalidOperationException) { }

                Falhar("passou do tempo");
                Terminar();
            }
        }

        private static void Iniciar()
        {
            if (_processo != null)
            {
                _deNovo = true;
                return;
            }

            var raiz = EscritorDeRelato.RaizDoRepositorio;
            var inicio = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{Script}\" enviar --pasta \"{EscritorDeRelato.PastaDosRelatos}\"",
                WorkingDirectory = raiz,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            try
            {
                var processo = new Process { StartInfo = inicio, EnableRaisingEvents = true };
                processo.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        _saida.Enqueue(e.Data);
                    }
                };
                processo.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        _saida.Enqueue("erro " + e.Data);
                    }
                };
                var geracao = ++_geracao;
                processo.Exited += (_, __) => _saida.Enqueue(Fim + geracao);
                processo.Start();
                processo.BeginOutputReadLine();
                processo.BeginErrorReadLine();
                _processo = processo;
                _limite = Time.realtimeSinceStartup + ConfigRelato.SegundosDoEnvio;
            }
            catch (Win32Exception)
            {
                Falhar("node não encontrado no PATH (fica pendente)");
                AvisosDoJogo.Mostrar(
                    "Report salvo, mas sem node no PATH para abrir a issue. Fica em relatos/pendentes.",
                    TipoDeAviso.Erro
                );
            }
        }

        // Protocolo do reporte.mjs: "enviado <id> <número> <url>" ou "falhou <id> <motivo>".
        private static void Ler(string linha)
        {
            var partes = linha.Split(new[] { ' ' }, 4);
            switch (partes[0])
            {
                case "enviado" when partes.Length >= 3:
                    Atualizar(partes[1], "#" + partes[2]);
                    AvisosDoJogo.Mostrar($"Issue #{partes[2]} criada a partir do report.");
                    Debug.Log("[Relato] " + linha);
                    break;
                case "falhou" when partes.Length >= 2:
                    var motivo = linha.Substring(Math.Min(linha.Length, 8 + partes[1].Length));
                    Atualizar(partes[1], "falhou: " + motivo + " (tenta de novo no próximo)");
                    AvisosDoJogo.Mostrar(
                        "O report não virou issue: " + motivo + ". Fica pendente.",
                        TipoDeAviso.Erro
                    );
                    Debug.LogWarning("[Relato] " + linha);
                    break;
                case "ocupado":
                    Falhar("outro envio estava rodando; vai no próximo report");
                    break;
                case "erro":
                    Debug.LogWarning("[Relato] " + linha);
                    break;
            }
        }

        private static void Terminar()
        {
            if (_processo == null)
            {
                return;
            }

            _processo.Dispose();
            _processo = null;
            Falhar("o envio parou sem resposta (fica pendente)");
            if (_deNovo)
            {
                _deNovo = false;
                Iniciar();
            }
        }

        // Quem ainda estava "enviando" quando o processo acabou sem dizer nada.
        private static void Falhar(string motivo)
        {
            var mudou = false;
            foreach (var item in _historico)
            {
                if (item.Situacao == ItemDoHistorico.Enviando && !_deNovo)
                {
                    item.Situacao = "falhou: " + motivo;
                    mudou = true;
                }
            }

            if (mudou)
            {
                Mudou?.Invoke();
            }
        }

        private static void Atualizar(string id, string situacao)
        {
            foreach (var item in _historico)
            {
                if (item.Id == id)
                {
                    item.Situacao = situacao;
                }
            }

            Mudou?.Invoke();
        }
    }

    /// <summary>Um report da sessão e em que pé está o envio.</summary>
    internal sealed class ItemDoHistorico
    {
        public const string Enviando = "enviando...";

        public ItemDoHistorico(string id, string frase)
        {
            Id = id;
            Frase = frase;
        }

        public string Id { get; }
        public string Frase { get; }
        public string Situacao { get; set; } = Enviando;
    }
}
