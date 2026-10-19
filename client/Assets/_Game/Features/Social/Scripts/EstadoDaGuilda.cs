using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Estado da guilda no client: dados da guilda, membros, convites recebidos e erros.
    /// Comunica com o servidor autoritativo via mensagens de tempo real.
    /// </summary>
    internal sealed class EstadoDaGuilda : IDisposable
    {
        private static EstadoDaGuilda _instancia;

        public static EstadoDaGuilda Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = new EstadoDaGuilda(new ConexaoDoGrupoReal());
                }
                return _instancia;
            }
        }

        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public DadosDaGuilda Dados { get; private set; }
        public ConviteDeGuildaRecebido ConviteRecebido { get; private set; }
        public string UltimoErro { get; private set; } = "";

        public bool TemGuilda => Dados != null && Dados.GuildaId > 0;

        public event Action Mudou;

        public EstadoDaGuilda(IConexaoDoGrupo conexao)
        {
            _conexao = conexao;
            if (_conexao == null)
            {
                return;
            }

            _assinaturas.Add(
                _conexao.Assinar<DadosDaGuilda>(SocialMessageIds.DadosDaGuilda, AoReceberDados)
            );
            _assinaturas.Add(
                _conexao.Assinar<ConviteDeGuildaRecebido>(
                    SocialMessageIds.ConviteDeGuildaRecebido,
                    AoReceberConvite
                )
            );
            _assinaturas.Add(
                _conexao.Assinar<OperacaoDeGuildaRecusada>(
                    SocialMessageIds.OperacaoDeGuildaRecusada,
                    AoReceberRecusa
                )
            );
        }

        public void Consultar()
        {
            _conexao?.Enviar(
                SocialMessageIds.ConsultarGuilda,
                new ConsultarGuilda(),
                EntregaTempoReal.Conviavel
            );
        }

        public void Criar(string nome)
        {
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.CriarGuilda,
                new CriarGuilda { Nome = nome ?? "" },
                EntregaTempoReal.Conviavel
            );
        }

        public void Convidar(string nomeDoAlvo)
        {
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.ConvidarParaGuilda,
                new ConvidarParaGuilda { NomeDoPersonagem = nomeDoAlvo ?? "" },
                EntregaTempoReal.Conviavel
            );
        }

        public void ResponderConvite(long guildaId, bool aceitar)
        {
            ConviteRecebido = null;
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.ResponderConviteDeGuilda,
                new ResponderConviteDeGuilda { GuildaId = guildaId, Aceitar = aceitar },
                EntregaTempoReal.Conviavel
            );
            Mudou?.Invoke();
        }

        public void Expulsar(long personagemId)
        {
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.ExpulsarDaGuilda,
                new ExpulsarDaGuilda { PersonagemId = personagemId },
                EntregaTempoReal.Conviavel
            );
        }

        public void AlterarCargo(long personagemId, CargoDaGuilda novoCargo)
        {
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.AlterarCargoDaGuilda,
                new AlterarCargoDaGuilda { PersonagemId = personagemId, NovoCargo = novoCargo },
                EntregaTempoReal.Conviavel
            );
        }

        public void PassarLideranca(long novoLiderId)
        {
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.PassarLiderancaDaGuilda,
                new PassarLiderancaDaGuilda { NovoLiderId = novoLiderId },
                EntregaTempoReal.Conviavel
            );
        }

        public void Sair()
        {
            UltimoErro = "";
            _conexao?.Enviar(
                SocialMessageIds.SairDaGuilda,
                new SairDaGuilda(),
                EntregaTempoReal.Conviavel
            );
        }

        private void AoReceberDados(DadosDaGuilda dados)
        {
            Dados = dados;
            UltimoErro = "";
            Mudou?.Invoke();
        }

        private void AoReceberConvite(ConviteDeGuildaRecebido convite)
        {
            ConviteRecebido = convite;
            Mudou?.Invoke();
        }

        private void AoReceberRecusa(OperacaoDeGuildaRecusada recusa)
        {
            UltimoErro = recusa != null ? recusa.Codigo : "";
            Mudou?.Invoke();
        }

        public void Dispose()
        {
            foreach (var sub in _assinaturas)
            {
                sub.Dispose();
            }
            _assinaturas.Clear();
        }
    }
}
