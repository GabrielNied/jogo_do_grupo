using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Valores de sensação do client (pausa de impacto, tremor, flash, tempos de animação, câmera)
    /// que a pessoa ajusta com o jogo rodando, no painel F10 de Desenvolvimento. Cada
    /// funcionalidade registra os do seu <c>Config&lt;Assunto&gt;</c>; o Core não conhece nenhuma.
    /// Só vale no editor e no build de desenvolvimento: no build de release o registro é ignorado
    /// e os valores ficam nos padrões. Convenção em <c>docs/convencoes.md</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// private static void Registrar()
    /// {
    ///     Sintonia.Registrar("Combate", nameof(HitStopPesado), () => HitStopPesado,
    ///         v => HitStopPesado = v, 0f, 0.3f, "s de jogo quase parado no golpe pesado");
    /// }
    /// </code>
    /// </example>
    public static class Sintonia
    {
        private static readonly List<AjusteDeSensacao> _ajustes = new List<AjusteDeSensacao>();

        /// <summary>Se o registro vale nesta execução (editor ou build de desenvolvimento).</summary>
        public static bool Ativa => Application.isEditor || Debug.isDebugBuild;

        /// <summary>Valores registrados, na ordem de registro.</summary>
        public static IReadOnlyList<AjusteDeSensacao> Ajustes => _ajustes;

        /// <summary>Algum valor mudou (pelo painel ou por restaurar).</summary>
        public static event Action Mudou;

        /// <summary>
        /// Registra um valor ajustável. O valor atual vira o padrão. Registrar de novo o mesmo
        /// <paramref name="grupo"/> e <paramref name="nome"/> substitui o anterior.
        /// </summary>
        /// <exception cref="ArgumentException">Grupo ou nome vazios, ou mínimo acima do máximo.</exception>
        /// <exception cref="ArgumentNullException">Leitura ou escrita nulas.</exception>
        public static void Registrar(
            string grupo,
            string nome,
            Func<float> ler,
            Action<float> escrever,
            float minimo,
            float maximo,
            string descricao = null
        )
        {
            if (!Ativa)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(grupo) || string.IsNullOrWhiteSpace(nome))
            {
                throw new ArgumentException("Grupo e nome são obrigatórios.");
            }

            if (minimo > maximo)
            {
                throw new ArgumentException($"Mínimo acima do máximo em {grupo}.{nome}.");
            }

            var ajuste = new AjusteDeSensacao(
                grupo,
                nome,
                ler ?? throw new ArgumentNullException(nameof(ler)),
                escrever ?? throw new ArgumentNullException(nameof(escrever)),
                minimo,
                maximo,
                descricao
            );
            var indice = _ajustes.FindIndex(a => a.Chave == ajuste.Chave);
            if (indice >= 0)
            {
                _ajustes[indice] = ajuste;
            }
            else
            {
                _ajustes.Add(ajuste);
            }
        }

        /// <summary>Muda um valor pela chave <c>"grupo.nome"</c>, dentro dos limites.</summary>
        /// <returns><c>false</c> se a chave não está registrada.</returns>
        public static bool Definir(string chave, float valor)
        {
            var ajuste = Procurar(chave);
            if (ajuste == null)
            {
                return false;
            }

            ajuste.Valor = valor;
            return true;
        }

        /// <summary>O ajuste da chave <c>"grupo.nome"</c>, ou <c>null</c>.</summary>
        public static AjusteDeSensacao Procurar(string chave)
        {
            return _ajustes.Find(a => a.Chave == chave);
        }

        /// <summary>Volta todos os valores ao padrão.</summary>
        public static void RestaurarTodos()
        {
            foreach (var ajuste in _ajustes)
            {
                ajuste.Restaurar();
            }
        }

        /// <summary>
        /// Os valores fora do padrão, um por linha (<c>Combate.HitStopPesado = 0.1 (padrão 0.07)</c>),
        /// prontos para colar para quem vai gravar no código. Vazio se nada mudou.
        /// </summary>
        public static string Alterados()
        {
            var texto = new StringBuilder();
            foreach (var ajuste in _ajustes)
            {
                if (!ajuste.Alterado)
                {
                    continue;
                }

                texto
                    .Append(ajuste.Chave)
                    .Append(" = ")
                    .Append(Formatar(ajuste.Valor))
                    .Append(" (padrão ")
                    .Append(Formatar(ajuste.Padrao))
                    .Append(')')
                    .Append('\n');
            }

            return texto.ToString();
        }

        /// <summary>Número com ponto decimal e até 4 casas, como no código.</summary>
        public static string Formatar(float valor)
        {
            return valor.ToString("0.####", CultureInfo.InvariantCulture);
        }

        internal static void AvisarMudanca()
        {
            Mudou?.Invoke();
        }

        /// <summary>Só para testes EditMode.</summary>
        internal static void Limpar()
        {
            _ajustes.Clear();
        }

        // Sem recarregar o domínio entre execuções, os campos estáticos dos Config sobrevivem:
        // volta tudo ao padrão antes de as funcionalidades registrarem de novo.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            RestaurarTodos();
            _ajustes.Clear();
        }
    }

    /// <summary>Um valor registrado na <see cref="Sintonia"/>.</summary>
    public sealed class AjusteDeSensacao
    {
        private readonly Func<float> _ler;
        private readonly Action<float> _escrever;

        internal AjusteDeSensacao(
            string grupo,
            string nome,
            Func<float> ler,
            Action<float> escrever,
            float minimo,
            float maximo,
            string descricao
        )
        {
            Grupo = grupo;
            Nome = nome;
            _ler = ler;
            _escrever = escrever;
            Minimo = minimo;
            Maximo = maximo;
            Descricao = descricao ?? "";
            Padrao = ler();
        }

        public string Grupo { get; }
        public string Nome { get; }

        /// <summary><c>"grupo.nome"</c>, única no registro.</summary>
        public string Chave => Grupo + "." + Nome;

        public float Minimo { get; }
        public float Maximo { get; }

        /// <summary>Unidade e efeito, para o painel.</summary>
        public string Descricao { get; }

        /// <summary>O valor no código, lido no registro.</summary>
        public float Padrao { get; }

        /// <summary>O valor em uso. Escrever limita entre <see cref="Minimo"/> e <see cref="Maximo"/>.</summary>
        public float Valor
        {
            get => _ler();
            set
            {
                var limitado = Mathf.Clamp(value, Minimo, Maximo);
                if (Mathf.Approximately(limitado, _ler()))
                {
                    return;
                }

                _escrever(limitado);
                Sintonia.AvisarMudanca();
            }
        }

        public bool Alterado => !Mathf.Approximately(Valor, Padrao);

        public void Restaurar()
        {
            Valor = Padrao;
        }
    }
}
