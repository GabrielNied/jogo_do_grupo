using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens.Loot
{
    /// <summary>
    /// Assina as mensagens de loot desde o início do jogo, não só com o mundo montado: quem volta
    /// ao jogo recebe <see cref="LootApareceu"/> logo ao entrar, antes de o jogador existir. A
    /// parte que se vê (marcadores, tecla F, dica) é o <see cref="ControleLoot"/>.
    /// </summary>
    internal static class ServicoDeLoot
    {
        private static IDisposable[] _assinaturas;

        /// <summary>Os itens do jogador no chão.</summary>
        public static LootNoChaoDoCliente Registro { get; private set; } =
            new LootNoChaoDoCliente();

        /// <summary>Assina as mensagens uma vez só.</summary>
        public static void Iniciar()
        {
            if (_assinaturas != null)
            {
                return;
            }

            _assinaturas = new[]
            {
                ConexaoTempoReal.Assinar<LootApareceu>(
                    ItensMessageIds.LootApareceu,
                    m => Registro.Receber(m)
                ),
                ConexaoTempoReal.Assinar<LootColetado>(ItensMessageIds.LootColetado, AoColetar),
                ConexaoTempoReal.Assinar<LootSumiu>(
                    ItensMessageIds.LootSumiu,
                    m => Registro.Receber(m)
                ),
                ConexaoTempoReal.Assinar<ColetaRecusada>(ItensMessageIds.ColetaRecusada, AoRecusar),
            };
        }

        /// <summary>Solta as assinaturas e esquece os itens (domínio sem recarregar, testes).</summary>
        public static void Reiniciar()
        {
            if (_assinaturas != null)
            {
                foreach (var assinatura in _assinaturas)
                {
                    assinatura?.Dispose();
                }
            }

            _assinaturas = null;
            Registro = new LootNoChaoDoCliente();
        }

        /// <summary>Pede ao servidor tudo o que é do jogador ao alcance (tecla F).</summary>
        public static bool PedirColeta() =>
            ConexaoTempoReal.Enviar(
                ItensMessageIds.ColetarLoot,
                new ColetarLoot { LootId = RegrasLoot.TudoPorPerto }
            );

        private static void AoColetar(LootColetado mensagem)
        {
            var nome = Registro.Receber(mensagem);
            AvisosDoJogo.Mostrar(
                TextosDoLoot.Coletado(nome, mensagem?.Entrada?.Equipamento?.Raridade)
            );
        }

        private static void AoRecusar(ColetaRecusada mensagem)
        {
            AvisosDoJogo.Mostrar(
                TextosDoLoot.Recusa(mensagem?.Codigo),
                mensagem?.Codigo == CodigosErroItens.InventarioCheio
                    ? TipoDeAviso.Aviso
                    : TipoDeAviso.Informacao
            );
        }
    }
}
