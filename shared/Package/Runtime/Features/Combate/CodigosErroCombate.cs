namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Códigos de erro de Combate que o servidor coloca em <c>ErroApi.Codigo</c>. Os genéricos
    /// (inclusive <c>NaoAutenticado</c>, 401) ficam em <c>CodigosErro</c> (Core).
    /// </summary>
    public static class CodigosErroCombate
    {
        /// <summary>
        /// 404: não existe personagem com esse id, ou ele é de outra conta (os dois casos
        /// respondem igual). Mesmo valor de <c>CodigosErroItens.PersonagemNaoEncontrado</c>.
        /// </summary>
        public const string PersonagemNaoEncontrado = "PersonagemNaoEncontrado";

        /// <summary>
        /// Tempo real, em <see cref="AtaqueRecusado.Codigo"/>: o ataque custa mais vigor do que o
        /// personagem tem agora. Nada é gasto.
        /// </summary>
        public const string VigorInsuficiente = "VigorInsuficiente";

        /// <summary>
        /// Tempo real, em <see cref="AtaqueRecusado.Codigo"/>: o personagem ainda está em outra
        /// ação e o pedido não coube na fila (<see cref="RegrasAtaque.EntraNaFila"/>).
        /// </summary>
        public const string AcaoEmAndamento = "AcaoEmAndamento";

        /// <summary>
        /// Tempo real, em <see cref="AtaqueRecusado.Codigo"/>: o personagem está caído e não age.
        /// </summary>
        public const string VoceEstaCaido = "VoceEstaCaido";

        /// <summary>
        /// Tempo real, em <see cref="AcaoRecusada.Codigo"/>: pediu para levantar quem não está
        /// caído (está vivo, já foi levantado ou já voltou à cidade).
        /// </summary>
        public const string AlvoNaoEstaCaido = "AlvoNaoEstaCaido";

        /// <summary>Em <see cref="AcaoRecusada.Codigo"/>: o ajudante está a mais de 2 m do caído.</summary>
        public const string LongeDemais = "LongeDemais";

        /// <summary>Em <see cref="AcaoRecusada.Codigo"/>: outro aliado já está levantando esse caído.</summary>
        public const string JaEstaSendoLevantado = "JaEstaSendoLevantado";

        /// <summary>Em <see cref="AcaoRecusada.Codigo"/>: pediu para voltar à cidade sem estar caído.</summary>
        public const string NaoEstaCaido = "NaoEstaCaido";

        /// <summary>Em <see cref="AcaoRecusada.Codigo"/>: pediu para voltar antes de 5 s caído.</summary>
        public const string AindaNaoPodeVoltar = "AindaNaoPodeVoltar";
    }
}
