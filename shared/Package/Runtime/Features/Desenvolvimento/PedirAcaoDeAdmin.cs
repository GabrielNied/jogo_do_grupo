using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>
    /// Client pede uma ação da tela de admin (F8). O servidor valida cada campo
    /// (<see cref="RegrasAdmin.Validar"/>) e só executa com <c>Desenvolvimento:PermitirComandos</c>;
    /// sem isso, ignora e não responde. Id: <see cref="DesenvolvimentoMessageIds.PedirAcaoDeAdmin"/>.
    /// </summary>
    [System.Serializable]
    public class PedirAcaoDeAdmin
    {
        public AcaoDeAdmin Acao = AcaoDeAdmin.GerarItem;

        /// <summary>Gerar item: modelo do <see cref="CatalogoEquipamentos"/>.</summary>
        public int ModeloId = 0;

        /// <summary>Gerar item: grau do exemplar.</summary>
        public GrauEquipamento Grau = GrauEquipamento.Inicial;

        /// <summary>Gerar item: origem do inimigo, que dá mais rolagens por atributo.</summary>
        public OrigemDoLoot Origem = OrigemDoLoot.Comum;

        /// <summary>Gerar item e spawnar inimigo: quantas vezes.</summary>
        public int Quantidade = 1;

        /// <summary>Experiência, coroas e essências: quanto.</summary>
        public long Valor = 0;

        /// <summary>Spawnar inimigo: id do <c>CatalogoCriaturas</c>.</summary>
        public int EspecieId = 0;

        /// <summary>Teleportar: destino.</summary>
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
    }
}
