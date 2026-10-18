using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>IDs de mensagem de Pets. Use somente valores entre 17000 e 17999.</summary>
    public static class PetsMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(17000, 17999);

        /// <summary>Client → servidor: chama o pet (<see cref="Pets.ChamarPet"/>).</summary>
        public const ushort ChamarPet = 17000;

        /// <summary>Client → servidor: dispensa o pet (<see cref="Pets.DispensarPet"/>).</summary>
        public const ushort DispensarPet = 17001;

        /// <summary>Servidor → quem vê o dono: um pet entrou no campo de visão (<see cref="Pets.PetApareceu"/>).</summary>
        public const ushort PetApareceu = 17002;

        /// <summary>Servidor → quem vê o dono: o pet andou ou parou (<see cref="Pets.PetMudou"/>).</summary>
        public const ushort PetMudou = 17003;

        /// <summary>Servidor → quem via o pet: ele sumiu (<see cref="Pets.PetSumiu"/>).</summary>
        public const ushort PetSumiu = 17004;

        /// <summary>Servidor → o dono: o pedido foi recusado (<see cref="Pets.PetRecusado"/>).</summary>
        public const ushort PetRecusado = 17005;

        /// <summary>Client → servidor: usa item consumível de pet (<see cref="Pets.UsarItemPet"/>).</summary>
        public const ushort UsarItemPet = 17006;

        /// <summary>Servidor → client: lista de pets desbloqueados na conta (<see cref="Pets.ColecaoPets"/>).</summary>
        public const ushort ColecaoPets = 17007;

        /// <summary>Servidor → client: notifica novo pet desbloqueado na conta (<see cref="Pets.PetDesbloqueado"/>).</summary>
        public const ushort PetDesbloqueado = 17008;

        /// <summary>Client → servidor: consulta as ofertas do cuidador de pets (<see cref="Pets.ConsultarCuidador"/>).</summary>
        public const ushort ConsultarCuidador = 17009;

        /// <summary>Servidor → client: catálogo de pets à venda no cuidador (<see cref="Pets.RespostaCuidador"/>).</summary>
        public const ushort RespostaCuidador = 17010;

        /// <summary>Client → servidor: compra de pet no cuidador com coroas (<see cref="Pets.ComprarPetCuidador"/>).</summary>
        public const ushort ComprarPetCuidador = 17011;

        /// <summary>Servidor → client: resultado da compra no cuidador (<see cref="Pets.RespostaCompraPet"/>).</summary>
        public const ushort RespostaCompraPet = 17012;
    }
}
