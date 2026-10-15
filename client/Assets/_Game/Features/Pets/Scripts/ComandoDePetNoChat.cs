using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pets;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Pets
{
    /// <summary>
    /// O comando <c>/pet</c> do chat: sem argumento alterna (chama o pet, ou dispensa se ele já
    /// está com o jogador); <c>/pet chamar</c> e <c>/pet dispensar</c> são explícitos. O servidor
    /// decide: o client só envia o pedido e mostra o que ele responder.
    /// </summary>
    internal static class ComandoDePetNoChat
    {
        public const string Verbo = "pet";

        internal enum AcaoDoComando
        {
            Chamar,
            Dispensar,
            Invalida,
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            ComandosDeBarraNoChat.Registrar(
                Verbo,
                Executar,
                "/pet [chamar|dispensar] — chama o seu cervo jovem ou o dispensa"
            );
        }

        internal static AcaoDoComando Interpretar(string resto, bool temPet)
        {
            switch ((resto ?? "").Trim().ToLowerInvariant())
            {
                case "":
                    return temPet ? AcaoDoComando.Dispensar : AcaoDoComando.Chamar;
                case "chamar":
                    return AcaoDoComando.Chamar;
                case "dispensar":
                    return AcaoDoComando.Dispensar;
                default:
                    return AcaoDoComando.Invalida;
            }
        }

        private static ResultadoDeComandoDeBarra Executar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            switch (Interpretar(resto, ApresentadorDePets.TemPetProprio))
            {
                case AcaoDoComando.Chamar:
                    ConexaoTempoReal.Enviar(PetsMessageIds.ChamarPet, new ChamarPet());
                    break;
                case AcaoDoComando.Dispensar:
                    ConexaoTempoReal.Enviar(PetsMessageIds.DispensarPet, new DispensarPet());
                    break;
                default:
                    contexto.MostrarSistema("Use /pet, /pet chamar ou /pet dispensar.");
                    break;
            }

            return new ResultadoDeComandoDeBarra();
        }
    }
}
