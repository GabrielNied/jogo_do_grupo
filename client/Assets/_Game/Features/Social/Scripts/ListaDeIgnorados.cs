using System;
using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>Onde a lista de ignorados fica guardada (neste computador).</summary>
    internal interface IArmazenamentoDeTexto
    {
        string Ler(string chave);
        void Gravar(string chave, string valor);
    }

    /// <summary>Guarda no <c>PlayerPrefs</c>, ou seja, neste computador e para este usuário do sistema.</summary>
    internal sealed class ArmazenamentoPlayerPrefs : IArmazenamentoDeTexto
    {
        public string Ler(string chave) => PlayerPrefs.GetString(chave, "");

        public void Gravar(string chave, string valor)
        {
            PlayerPrefs.SetString(chave, valor);
            PlayerPrefs.Save();
        }
    }

    internal enum ResultadoDeIgnorar
    {
        Adicionado,
        JaIgnorado,
        ListaCheia,
        NomeInvalido,
    }

    /// <summary>
    /// Nomes ignorados neste computador (até <see cref="Maximo"/>), sem diferenciar maiúsculas.
    /// O servidor não participa e quem foi ignorado não fica sabendo.
    /// </summary>
    internal sealed class ListaDeIgnorados
    {
        public const int Maximo = 50;
        public const string ChaveDeArmazenamento = "social.chat.ignorados";

        private readonly IArmazenamentoDeTexto _armazenamento;
        private readonly List<string> _nomes = new List<string>();

        public ListaDeIgnorados(IArmazenamentoDeTexto armazenamento)
        {
            _armazenamento = armazenamento;
            Carregar();
        }

        /// <summary>Os nomes na ordem em que foram ignorados.</summary>
        public IReadOnlyList<string> Nomes => _nomes;

        public bool Contem(string nome) => Indice(nome) >= 0;

        public ResultadoDeIgnorar Adicionar(string nome)
        {
            var limpo = Normalizar(nome);
            if (limpo == null)
            {
                return ResultadoDeIgnorar.NomeInvalido;
            }

            if (Indice(limpo) >= 0)
            {
                return ResultadoDeIgnorar.JaIgnorado;
            }

            if (_nomes.Count >= Maximo)
            {
                return ResultadoDeIgnorar.ListaCheia;
            }

            _nomes.Add(limpo);
            Gravar();
            return ResultadoDeIgnorar.Adicionado;
        }

        /// <summary>Tira o nome da lista; <c>false</c> se ele não estava nela.</summary>
        public bool Remover(string nome)
        {
            var indice = Indice(nome);
            if (indice < 0)
            {
                return false;
            }

            _nomes.RemoveAt(indice);
            Gravar();
            return true;
        }

        // Nome de personagem não tem espaço: um nome com espaço ou vazio não serve.
        private static string Normalizar(string nome)
        {
            var limpo = (nome ?? "").Trim();
            if (limpo.Length == 0)
            {
                return null;
            }

            foreach (var c in limpo)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c))
                {
                    return null;
                }
            }

            return limpo;
        }

        private int Indice(string nome)
        {
            var limpo = Normalizar(nome);
            if (limpo == null)
            {
                return -1;
            }

            return _nomes.FindIndex(n =>
                string.Equals(n, limpo, StringComparison.OrdinalIgnoreCase)
            );
        }

        private void Carregar()
        {
            string texto;
            try
            {
                texto = _armazenamento?.Ler(ChaveDeArmazenamento) ?? "";
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[Social] Não foi possível ler a lista de ignorados: {e.Message}"
                );
                texto = "";
            }

            foreach (var parte in texto.Split('\n'))
            {
                var limpo = Normalizar(parte);
                if (limpo != null && _nomes.Count < Maximo && Indice(limpo) < 0)
                {
                    _nomes.Add(limpo);
                }
            }
        }

        private void Gravar()
        {
            try
            {
                _armazenamento?.Gravar(ChaveDeArmazenamento, string.Join("\n", _nomes));
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[Social] Não foi possível gravar a lista de ignorados: {e.Message}"
                );
            }
        }
    }
}
