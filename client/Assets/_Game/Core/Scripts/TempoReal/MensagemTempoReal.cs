using System;
using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Mensagem recebida do servidor: o Id (da faixa de alguma funcionalidade) e o corpo em JSON.
    /// Leia o corpo com <see cref="Ler{T}"/> usando o DTO do shared que corresponde ao Id.
    /// </summary>
    public readonly struct MensagemTempoReal
    {
        public ushort Id { get; }

        /// <summary>Corpo em JSON (vazio se a mensagem não tem corpo).</summary>
        public string Json { get; }

        private readonly CacheDto _cache;

        public MensagemTempoReal(ushort id, string json)
            : this(id, json, new CacheDto()) { }

        internal MensagemTempoReal(ushort id, string json, CacheDto cache)
        {
            Id = id;
            Json = json ?? "";
            _cache = cache ?? new CacheDto();
        }

        /// <summary>
        /// Converte o corpo no DTO <typeparamref name="T"/>. Sem corpo, devolve um
        /// <typeparamref name="T"/> com os valores padrão. JSON inválido lança <c>ArgumentException</c>.
        /// Chamadas repetidas devolvem a instância já deserializada (cache por mensagem).
        /// </summary>
        public T Ler<T>()
        {
            if (_cache != null && _cache.TentarObter<T>(out var emCache))
            {
                return emCache;
            }

            var dto = JsonUtility.FromJson<T>(string.IsNullOrEmpty(Json) ? "{}" : Json);
            _cache?.Armazenar(dto);
            return dto;
        }
    }

    internal sealed class CacheDto
    {
        private Type _tipo;
        private object _instancia;
        private Dictionary<Type, object> _outros;

        public bool TentarObter<T>(out T valor)
        {
            if (_tipo == typeof(T))
            {
                valor = (T)_instancia;
                return true;
            }

            if (_outros != null && _outros.TryGetValue(typeof(T), out var obj))
            {
                valor = (T)obj;
                return true;
            }

            valor = default;
            return false;
        }

        public void Armazenar<T>(T valor)
        {
            if (_tipo == null)
            {
                _tipo = typeof(T);
                _instancia = valor;
            }
            else if (_tipo == typeof(T))
            {
                _instancia = valor;
            }
            else
            {
                _outros ??= new Dictionary<Type, object>();
                _outros[typeof(T)] = valor;
            }
        }
    }
}
