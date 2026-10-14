using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>Escolhas locais de visibilidade. Os avisos continuam disponíveis ao chat.</summary>
    public static class PreferenciasDeAvisos
    {
        private const string ChaveSistema = "lumora.avisos.sistema";
        private const string ChaveCombate = "lumora.avisos.combate";

        public static bool MostrarSistema
        {
            get => PlayerPrefs.GetInt(ChaveSistema, 1) != 0;
            set => Gravar(ChaveSistema, value);
        }

        public static bool MostrarCombate
        {
            get => PlayerPrefs.GetInt(ChaveCombate, 1) != 0;
            set => Gravar(ChaveCombate, value);
        }

        public static bool PodeMostrar(CategoriaDeAviso categoria, TipoDeAviso tipo) =>
            tipo == TipoDeAviso.Erro
            || (categoria == CategoriaDeAviso.Combate ? MostrarCombate : MostrarSistema);

        private static void Gravar(string chave, bool valor)
        {
            PlayerPrefs.SetInt(chave, valor ? 1 : 0);
            PlayerPrefs.Save();
            Hud.Avisos?.AtualizarVisibilidade();
        }
    }
}
