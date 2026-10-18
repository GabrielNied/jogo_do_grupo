using System;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// O que uma funcionalidade informa para ter uma janela no jogo: a chave, o título, a tecla
    /// que abre e fecha, a posição na barra de atalhos e quem cria o conteúdo.
    /// Registrada em <see cref="JanelasDoJogo.Registrar"/>.
    /// </summary>
    public sealed class DefinicaoDeJanela
    {
        /// <param name="chave">
        /// Chave única, no formato <c>"&lt;funcionalidade&gt;.&lt;janela&gt;"</c>
        /// (ex.: <c>"itens.inventario"</c>).
        /// </param>
        /// <param name="titulo">Título mostrado na moldura e na barra de atalhos.</param>
        /// <param name="tecla">
        /// Uma letra de A a Z (ex.: <c>"I"</c>), ou <c>null</c>/<c>""</c> quando a janela só
        /// abre por interação no mundo. Veja <see cref="Tecla"/>.
        /// </param>
        /// <param name="ordem">Posição na barra de atalhos: menor fica à esquerda.</param>
        /// <param name="criar">
        /// Chamado a cada abertura; deve devolver uma instância nova de <see cref="JanelaDoJogo"/>.
        /// </param>
        public DefinicaoDeJanela(
            string chave,
            string titulo,
            string tecla,
            int ordem,
            Func<JanelaDoJogo> criar
        )
        {
            Chave = chave;
            Titulo = titulo;
            Tecla = tecla == null ? null : tecla.Trim().ToUpperInvariant();
            Ordem = ordem;
            Criar = criar;
        }

        /// <summary>Chave única da janela.</summary>
        public string Chave { get; }

        /// <summary>Título mostrado na moldura e na barra de atalhos.</summary>
        public string Titulo { get; }

        /// <summary>
        /// Tecla que abre e fecha a janela, em maiúscula (ex.: <c>"I"</c>), ou <c>null</c> se
        /// não há atalho. As teclas padrão ficam em <c>docs/features/interface.md</c>.
        /// </summary>
        public string Tecla { get; }

        /// <summary>Posição na barra de atalhos: menor fica à esquerda.</summary>
        public int Ordem { get; }

        /// <summary>Cria o conteúdo da janela a cada abertura.</summary>
        public Func<JanelaDoJogo> Criar { get; }
    }
}
