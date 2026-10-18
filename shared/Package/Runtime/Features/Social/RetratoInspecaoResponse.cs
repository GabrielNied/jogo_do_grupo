using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → client: retrato completo e somente leitura do personagem inspecionado.
    /// Id: <see cref="SocialMessageIds.RetratoInspecaoResponse"/>.
    /// </summary>
    [Serializable]
    public class RetratoInspecaoResponse
    {
        public long PersonagemId;
        public string Nome = "";
        public int Raca;
        public int Nivel = 1;
        public int ConjuntoAtivo = 1;
        public EntradaInventario[] Equipados = Array.Empty<EntradaInventario>();
        public AtributosDosEquipamentos Atributos = new AtributosDosEquipamentos();
        public int Forca;
        public int Vigor;
        public int Agilidade;
        public int Intelecto;
        public int Espirito;
    }
}
