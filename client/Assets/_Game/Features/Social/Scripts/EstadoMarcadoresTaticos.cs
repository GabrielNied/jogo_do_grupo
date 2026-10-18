using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Mantém o estado dos marcadores táticos de alvo do grupo sincronizados no cliente.
    /// </summary>
    public sealed class EstadoMarcadoresTaticos : IDisposable
    {
        private static EstadoMarcadoresTaticos _instancia;

        public static EstadoMarcadoresTaticos Instancia
        {
            get => _instancia ??= new EstadoMarcadoresTaticos();
            internal set => _instancia = value;
        }

        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly Dictionary<long, TipoMarcadorDeAlvo> _marcadores =
            new Dictionary<long, TipoMarcadorDeAlvo>();

        public IReadOnlyDictionary<long, TipoMarcadorDeAlvo> Marcadores => _marcadores;

        public event Action Mudou;
        public event Action<DefinirMarcadorRecusado> Recusado;

        public EstadoMarcadoresTaticos()
            : this(null) { }

        internal EstadoMarcadoresTaticos(IConexaoDoGrupo conexao)
        {
            _conexao = conexao ?? new ConexaoDoGrupoReal();
            _assinaturas.Add(
                _conexao.Assinar<MarcadoresDeAlvoMudaram>(
                    SocialMessageIds.MarcadoresDeAlvoMudaram,
                    AoReceberMarcadoresMudaram
                )
            );
            _assinaturas.Add(
                _conexao.Assinar<DefinirMarcadorRecusado>(
                    SocialMessageIds.DefinirMarcadorRecusado,
                    AoReceberRecusa
                )
            );
        }

        public void Dispose()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura?.Dispose();
            }
            _assinaturas.Clear();
            _marcadores.Clear();
        }

        private void AoReceberMarcadoresMudaram(MarcadoresDeAlvoMudaram mensagem)
        {
            DefinirMarcadores(mensagem?.Marcadores);
        }

        private void AoReceberRecusa(DefinirMarcadorRecusado recusa)
        {
            Recusado?.Invoke(recusa);
        }

        /// <summary>
        /// Atualiza a coleção local a partir da lista recebida do servidor.
        /// </summary>
        public void DefinirMarcadores(MarcadorAlvoDto[] novosMarcadores)
        {
            _marcadores.Clear();
            if (novosMarcadores != null)
            {
                foreach (var dto in novosMarcadores)
                {
                    if (dto.Marcador != TipoMarcadorDeAlvo.Nenhum)
                    {
                        _marcadores[dto.AlvoId] = dto.Marcador;
                    }
                }
            }

            Mudou?.Invoke();
        }

        /// <summary>
        /// Retorna o marcador atribuído ao alvo indicado, ou <see cref="TipoMarcadorDeAlvo.Nenhum"/>.
        /// </summary>
        public TipoMarcadorDeAlvo ObterMarcador(long alvoId)
        {
            return _marcadores.TryGetValue(alvoId, out var marcador)
                ? marcador
                : TipoMarcadorDeAlvo.Nenhum;
        }

        /// <summary>
        /// Envia para o servidor a intenção de marcar ou desmarcar um alvo.
        /// </summary>
        public bool PedirDefinirMarcador(long alvoId, TipoMarcadorDeAlvo marcador)
        {
            if (alvoId <= 0)
            {
                return false;
            }

            return _conexao.Enviar(
                SocialMessageIds.DefinirMarcadorDeAlvo,
                new DefinirMarcadorDeAlvo { AlvoId = alvoId, Marcador = marcador },
                EntregaTempoReal.Confiavel
            );
        }

        /// <summary>
        /// Limpa todos os marcadores locais (ex: ao sair de grupo).
        /// </summary>
        public void Limpar()
        {
            _marcadores.Clear();
            Mudou?.Invoke();
        }
    }
}
