# Conquistas e Títulos

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 18000–18999

## Resumo

Sistema de conquistas da conta baseadas em marcos do jogo (exploração, combate, chefes, economia e progressão). Conquistas concluídas concedem pontos de conquista e podem desbloquear títulos de prestígio selecionáveis para exibição junto ao nome do personagem.

## Definido

- **Conquistas por conta:** o progresso e as conquistas desbloqueadas pertencem à conta do jogador; qualquer personagem da conta contribui para o progresso cumulativo e compartilha os títulos desbloqueados.
- **Autoridade do servidor:** todo avanço de progresso e desbloqueio de conquista é processado estritamente no servidor através de eventos de jogo (derrotar chefes, explorar áreas, atingir marcas de coroas, subir de nível, etc.).
- **Títulos de prestígio:** certas conquistas concedem um título (ex.: "O Pioneiro", "Caçador de Lobos", "Senhor das Coroas"). O jogador pode equipar um título desbloqueado a qualquer momento ou optar por não exibir nenhum.
- **Validação de títulos:** o servidor recusa qualquer tentativa de equipar um título que a conta ainda não tenha conquistado (`OperacaoDeConquistaRecusada.Codigo = CodigosErroConquistas.TituloNaoConquistado`).
- **Interface Lumora (tecla Y):** janela `JanelaConquistas` exibe pontuação total da conta, lista de conquistas categorizadas (Geral, Combate, Exploração, Economia), barras de progresso, status de conclusão e seletor do título ativo.

## Catálogo de Conquistas (v1)

| ID | Nome | Categoria | Meta | Pontos | Título Desbloqueado |
|---|---|---|---|---|---|
| 1 | Primeiros Passos | Geral | Alcançar nível 2 | 10 | "O Iniciante" |
| 2 | Desbravador de Asterra | Exploração | Visitar 3 áreas do mundo | 15 | "O Explorador" |
| 3 | Matador de Mobs | Combate | Derrotar 10 monstros | 15 | "O Caçador" |
| 4 | O Fim do Predador | Combate | Derrotar o chefe Presa-Velha | 25 | "Caçador de Lobos" |
| 5 | Fortuna Acumulada | Economia | Acumular 1.000 coroas | 20 | "O Próspero" |
| 6 | Membro de Guilda | Geral | Fundar ou ingressar em uma guilda | 15 | "O Companheiro" |
