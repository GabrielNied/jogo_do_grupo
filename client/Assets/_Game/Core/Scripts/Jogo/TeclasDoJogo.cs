using System;
using System.Collections.Generic;
using System.Linq;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>Teclas locais compartilhadas pelas ações do mundo e pelas janelas registradas.</summary>
    public static class TeclasDoJogo
    {
        public const string Frente = "frente";
        public const string Tras = "tras";
        public const string Esquerda = "esquerda";
        public const string Direita = "direita";

        /// <summary>Segurar anda devagar; sem ela o personagem está sempre correndo (#894).</summary>
        public const string AndarDevagar = "andar-devagar";
        public const string Pular = "pular";
        public const string Esquiva = "esquiva";
        public const string Ataque = "ataque";
        public const string Defesa = "defesa";
        public const string Interagir = "interagir";
        public const string Coletar = "coletar";
        public const string EsconderHud = "esconder-hud";

        private static readonly Dictionary<string, string> _padroes = new Dictionary<string, string>
        {
            [Frente] = "<Keyboard>/w",
            [Tras] = "<Keyboard>/s",
            [Esquerda] = "<Keyboard>/a",
            [Direita] = "<Keyboard>/d",
            [AndarDevagar] = "<Keyboard>/leftCtrl",
            [Pular] = "<Keyboard>/space",
            [Esquiva] = "<Keyboard>/leftShift",
            [Ataque] = "<Mouse>/leftButton",
            [Defesa] = "<Mouse>/rightButton",
            [Interagir] = "<Keyboard>/e",
            [Coletar] = "<Keyboard>/f",
            [EsconderHud] = "<Keyboard>/f11",
        };

        // Ações que saíram do jogo: a preferência salva delas é descartada e a tecla volta ao
        // padrão novo (#894: "correr" virou "andar-devagar" no Ctrl, e a esquiva foi para o Shift).
        private static readonly HashSet<string> _obsoletas = new HashSet<string> { "correr" };

        private static readonly Dictionary<string, string> _alteradas =
            new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _pendentes =
            new Dictionary<string, string>();

        public static event Action Mudou;

        public static string Janela(string chave) => "janela." + chave;

        public static void RegistrarJanela(string chave, string tecla)
        {
            if (string.IsNullOrEmpty(tecla))
            {
                return;
            }

            var id = Janela(chave);
            if (!_padroes.ContainsKey(id))
            {
                _padroes[id] = "<Keyboard>/" + tecla.ToLowerInvariant();
            }

            if (_pendentes.Count > 0)
                RecalcularPendentes();
        }

        public static string Caminho(string id) =>
            _alteradas.TryGetValue(id, out var caminho) ? caminho : _padroes[id];

        public static string Rotulo(string id) => RotuloDoCaminho(Caminho(id));

        public static string RotuloDoCaminho(string caminho)
        {
            var tecla = caminho.Substring(caminho.IndexOf('/') + 1);
            switch (tecla)
            {
                case "leftButton":
                    return "Mouse 1";
                case "rightButton":
                    return "Mouse 2";
                case "middleButton":
                    return "Mouse 3";
                case "space":
                    return "Espaço";
                case "leftShift":
                    return "Shift esq.";
                case "rightShift":
                    return "Shift dir.";
                case "leftAlt":
                    return "Alt esq.";
                case "rightAlt":
                    return "Alt dir.";
                case "leftCtrl":
                    return "Ctrl esq.";
                case "rightCtrl":
                    return "Ctrl dir.";
                case "upArrow":
                    return "↑";
                case "downArrow":
                    return "↓";
                case "leftArrow":
                    return "←";
                case "rightArrow":
                    return "→";
                default:
                    return tecla.ToUpperInvariant();
            }
        }

        public static bool TentarTrocar(string id, string caminho, out string aviso)
        {
            aviso = null;
            if (!_padroes.ContainsKey(id) || !Valido(id, caminho))
            {
                aviso = "Tecla reservada ou indisponível para esta ação.";
                return false;
            }

            var anterior = Caminho(id);
            if (anterior == caminho)
            {
                return true;
            }

            var outra = _padroes.Keys.FirstOrDefault(chave =>
                chave != id && Caminho(chave) == caminho
            );
            if (outra != null && !Valido(outra, anterior))
            {
                aviso = "A outra ação não pode receber a tecla anterior.";
                return false;
            }

            Definir(id, caminho);
            _pendentes.Clear();
            if (outra != null)
            {
                Definir(outra, anterior);
                aviso = outra + " agora usa " + Rotulo(outra) + ".";
            }

            Mudou?.Invoke();
            return true;
        }

        public static void Restaurar(string id)
        {
            if (_padroes.TryGetValue(id, out var caminho))
            {
                TentarTrocar(id, caminho, out _);
            }
        }

        public static void RestaurarTudo()
        {
            _pendentes.Clear();
            _alteradas.Clear();
            Mudou?.Invoke();
        }

        public static string Gravar() =>
            string.Join(
                "",
                _alteradas
                    .OrderBy(par => par.Key)
                    .Select(par => "\ntecla." + par.Key + "=" + par.Value)
            );

        public static void Carregar(string texto)
        {
            _alteradas.Clear();
            _pendentes.Clear();
            if (texto != null)
            {
                foreach (var linha in texto.Split('\n'))
                {
                    if (!linha.StartsWith("tecla.", StringComparison.Ordinal))
                        continue;
                    var partes = linha.Substring(6).Split('=');
                    if (partes.Length == 2 && !_obsoletas.Contains(partes[0]))
                        _pendentes[partes[0]] = partes[1];
                }
            }

            RecalcularPendentes();
            Mudou?.Invoke();
        }

        private static void RecalcularPendentes()
        {
            _alteradas.Clear();
            foreach (var par in _pendentes)
                if (_padroes.ContainsKey(par.Key) && Valido(par.Key, par.Value))
                    Definir(par.Key, par.Value);
            var caminhos = new HashSet<string>();
            foreach (var id in _padroes.Keys)
                if (!caminhos.Add(Caminho(id)))
                {
                    _alteradas.Clear();
                    break;
                }
            Mudou?.Invoke();
        }

        private static void Definir(string id, string caminho)
        {
            if (_padroes[id] == caminho)
                _alteradas.Remove(id);
            else
                _alteradas[id] = caminho;
        }

        private static bool Valido(string id, string caminho)
        {
            if (string.IsNullOrEmpty(caminho))
                return false;
            if (caminho.StartsWith("<Mouse>/", StringComparison.Ordinal))
                return (id == Ataque || id == Defesa)
                    && (
                        caminho == "<Mouse>/leftButton"
                        || caminho == "<Mouse>/rightButton"
                        || caminho == "<Mouse>/middleButton"
                    );
            if (!caminho.StartsWith("<Keyboard>/", StringComparison.Ordinal))
                return false;
            var tecla = caminho.Substring(11);
            if (tecla.Length == 1)
                return char.IsLetterOrDigit(tecla[0]);
            return tecla == "space"
                || tecla == "leftShift"
                || tecla == "rightShift"
                || tecla == "leftAlt"
                || tecla == "rightAlt"
                || tecla == "leftCtrl"
                || tecla == "rightCtrl"
                || tecla == "upArrow"
                || tecla == "downArrow"
                || tecla == "leftArrow"
                || tecla == "rightArrow"
                || tecla == "tab"
                || (
                    tecla.Length >= 2
                    && tecla[0] == 'f'
                    && int.TryParse(tecla.Substring(1), out var fn)
                    && fn >= 1
                    && fn <= 12
                );
        }
    }
}
