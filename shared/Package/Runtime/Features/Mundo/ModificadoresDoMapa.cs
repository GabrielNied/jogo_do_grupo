using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Gerenciador de modificadores de terreno (monumentos, áreas aplanadas para construções,
    /// estradas de jogadores e vilas). Acesso seguro e determinístico.
    /// </summary>
    public static class ModificadoresDoMapa
    {
        private static readonly List<ModificadorDeTerreno> ModificadoresFixos = new()
        {
            new ModificadorDeTerreno(
                "cidade.passo_da_neve",
                TipoModificadorTerreno.Planar,
                MapaDoMundo.CidadeDaNeveX,
                MapaDoMundo.CidadeDaNeveZ,
                MapaDoMundo.RaioCidadeSecundaria,
                MapaDoMundo.BordaCidadeSecundaria,
                MapaDoMundo.CidadeDaNeveAltura
            ),
            new ModificadorDeTerreno(
                "cidade.oasis_do_deserto",
                TipoModificadorTerreno.Planar,
                MapaDoMundo.CidadeDoDesertoX,
                MapaDoMundo.CidadeDoDesertoZ,
                MapaDoMundo.RaioCidadeSecundaria,
                MapaDoMundo.BordaCidadeSecundaria,
                MapaDoMundo.CidadeDoDesertoAltura
            ),
            // Ruína do oeste (Colinas) em (-250, -100): área aplanada de 16 m com 8 m de transição suave
            new ModificadorDeTerreno(
                "monumento.ruina_oeste",
                TipoModificadorTerreno.Planar,
                -250f,
                -100f,
                16f,
                8f,
                6.5f
            ),
            // Acampamento da mata em (350, -250): clareira aplanada de 18 m com 10 m de borda suave
            new ModificadorDeTerreno(
                "monumento.acampamento_mata",
                TipoModificadorTerreno.Planar,
                350f,
                -250f,
                18f,
                10f,
                2f
            ),
            // Passagem das colinas em (-300, -200): platô suave de 20 m
            new ModificadorDeTerreno(
                "monumento.passagem_colinas",
                TipoModificadorTerreno.Planar,
                -300f,
                -200f,
                20f,
                12f,
                10f
            ),
            // Marco da estrada norte em (14, 70): acomodação à beira da estrada
            new ModificadorDeTerreno(
                "monumento.marco_estrada_norte",
                TipoModificadorTerreno.Planar,
                14f,
                70f,
                10f,
                6f,
                3.2f
            ),
        };

        private static readonly List<ModificadorDeTerreno> ModificadoresDinamicos = new();
        private static readonly object Trava = new();

        /// <summary>
        /// Aplica todos os modificadores pertinentes sobre a altura original calculada proceduralmente.
        /// </summary>
        public static float Aplicar(float x, float z, float alturaOriginal)
        {
            var altura = alturaOriginal;

            // 1. Modificadores fixos (monumentos do mundo)
            for (var i = 0; i < ModificadoresFixos.Count; i++)
            {
                var mod = ModificadoresFixos[i];
                if (mod.Afeta(x, z))
                {
                    altura = mod.Aplicar(x, z, altura);
                }
            }

            // 2. Modificadores dinâmicos (estradas e terraplanagem do jogador)
            lock (Trava)
            {
                for (var i = 0; i < ModificadoresDinamicos.Count; i++)
                {
                    var mod = ModificadoresDinamicos[i];
                    if (mod.Afeta(x, z))
                    {
                        altura = mod.Aplicar(x, z, altura);
                    }
                }
            }

            return altura;
        }

        /// <summary>Registra um novo modificador dinâmico (ex: estrada construída por jogador).</summary>
        public static void AdicionarDinamico(ModificadorDeTerreno modificador)
        {
            if (modificador == null)
                return;
            lock (Trava)
            {
                ModificadoresDinamicos.RemoveAll(m => m.Id == modificador.Id);
                ModificadoresDinamicos.Add(modificador);
            }
        }

        /// <summary>Remove um modificador dinâmico pelo identificador.</summary>
        public static bool RemoverDinamico(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            lock (Trava)
            {
                return ModificadoresDinamicos.RemoveAll(m => m.Id == id) > 0;
            }
        }

        /// <summary>Limpa todos os modificadores dinâmicos (usado em testes ou recarregamento de sessão).</summary>
        public static void LimparDinamicos()
        {
            lock (Trava)
            {
                ModificadoresDinamicos.Clear();
            }
        }

        /// <summary>Retorna a lista atual de modificadores fixos e dinâmicos para inspeção.</summary>
        public static IReadOnlyList<ModificadorDeTerreno> ObterTodos()
        {
            lock (Trava)
            {
                var todos = new List<ModificadorDeTerreno>(
                    ModificadoresFixos.Count + ModificadoresDinamicos.Count
                );
                todos.AddRange(ModificadoresFixos);
                todos.AddRange(ModificadoresDinamicos);
                return todos;
            }
        }
    }
}
