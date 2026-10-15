namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Client pede para trocar de arma (tecla R). Só a intenção: o servidor recusa no preparo e
    /// no momento de acerto de um golpe, com a guarda segurada, na esquiva, na reação ao acerto,
    /// atordoado ou caído, e responde com <see cref="TrocaDeArmaIniciada"/> ou
    /// <see cref="TrocaDeArmaRecusada"/>. Id: <see cref="CombateMessageIds.IntencaoDeTrocaDeArma"/>.
    /// </summary>
    [System.Serializable]
    public class IntencaoDeTrocaDeArma
    {
        /// <summary>Número do pedido, devolvido na recusa.</summary>
        public int Sequencia = 0;
    }
}
