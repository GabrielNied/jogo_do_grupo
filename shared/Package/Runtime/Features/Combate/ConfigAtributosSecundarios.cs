namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Valores dos atributos secundários. <b>TODOS são valores de TESTE</b> até a decisão da
    /// #627 (docs/features/personagem.md, "Valores de teste até a decisão"); não são balanceamento.
    /// Ficam aqui, separados das contas, para trocar sem mexer nas regras.
    /// </summary>
    public static class ConfigAtributosSecundarios
    {
        // Base (fonte Base). Teste.

        /// <summary>Teste: 0 = a vida não regenera sozinha até a decisão (#627).</summary>
        public const double BaseRegeneracaoVida = 0.0;
        public const double BaseRegeneracaoStamina = RegrasRecursos.RegeneracaoStaminaPorSegundo;
        public const double BaseRegeneracaoMana = RegrasRecursos.RegeneracaoManaPorSegundo;
        public const double BaseChanceCritica = 0.05;
        public const double BaseDanoCritico = 1.5;
        public const double BaseVelocidadeAtaque = 1.0;
        public const double BasePenetracao = 0.0;

        // Limites finais. Teste.
        public const double TetoChanceCritica = 1.0;
        public const double DanoCriticoMinimo = 1.0;
        public const double VelocidadeMinima = 0.25;
        public const double VelocidadeMaxima = 3.0;
        public const double TetoPenetracao = 0.75;

        // Ordem em que cada fonte entra (menor primeiro). Teste; cada fonte declara a sua.
        public const int OrdemBase = 0;
        public const int OrdemPersonagem = 10;
        public const int OrdemArvore = 20;
        public const int OrdemEquipamento = 30;
        public const int OrdemEfeito = 40;
        public const int OrdemAlvo = 50;

        public const string GrupoBase = "base";

        /// <summary>
        /// Teste: mitigação provisória <c>dano * K / (K + defesa)</c>, só para provar a
        /// penetração. A fórmula de mitigação é do Combate e segue em aberto (#627).
        /// </summary>
        public const double ConstanteDeMitigacao = 100.0;

        /// <summary>Teste: parte física de uma skill híbrida que não declarou a divisão.</summary>
        public const double FracaoFisicaPadraoDaHibrida = 0.5;
    }
}
