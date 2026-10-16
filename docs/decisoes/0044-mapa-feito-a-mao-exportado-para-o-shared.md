# 0044. Mapa do mundo feito à mão e exportado para o shared

**Data:** 2026-10-16
**Status:** aceita (substitui a geração por semente do [ADR 0020](0020-mapa-do-mundo-no-shared.md))

## Contexto

O [ADR 0020](0020-mapa-do-mundo-no-shared.md) gera o mundo por uma conta com semente fixa no shared, para client e servidor terem o mesmo chão. A conta não deixa desenhar o mundo: um morro fora do lugar se corrige na fórmula, e lugares feitos à mão ficaram sem solução. No quiz de definições de 2026-10-16, Gabriel decidiu fazer um **mapa novo, do zero, à mão**, sem aumentar o custo do servidor.

Opções avaliadas:

- **Servidor lê o Terrain do Unity:** não dá. O servidor não tem engine, e o asset é binário e grande.
- **Seguir com a semente e colocar lugares à mão por cima:** mantém o mundo atual, que não é o que se quer.
- **Desenhar no Unity e exportar dados compactos para o shared:** o editor serve para desenhar, e o servidor lê só uma grade pequena.

## Decisão

- O mundo é desenhado à mão no editor do Unity (Terrain e objetos), **por uma pessoa**. As IAs só mexem por script de editor ([regra 9](../../AGENTS.md#regras)).
- Um comando de editor (`unity:executar`) exporta para `shared/` os dados que as regras usam:
  - a **altura** do chão numa grade (hoje 10 m, como no 0020);
  - o **bioma** e as **áreas especiais** de cada célula (água, lava, pântano, cidade);
  - os **pontos marcados** (nascimento, entradas de dungeon, pontos de retorno, pontos de coleta e de inimigos).
  O formato é binário pequeno ou JSON compactado, versionado no git; mudar o mapa é rodar o export e commitar o resultado junto.
- O `MapaDoMundo` passa a ler esses dados, com a mesma interface de hoje (altura e bioma de um ponto). O servidor continua só consultando a grade em memória (algumas centenas de KB): o custo por movimento não muda.
- O client desenha o terreno do asset do Unity. Um teste do shared confere que a grade exportada bate com os limites (alturas e cidade plana), e o export falha se o Terrain e a grade divergirem.

## Consequências

- O mundo atual (gerado) é substituído quando o mapa novo ficar pronto. Até lá a semente continua valendo, então nada quebra no meio.
- Desenhar o mapa é tarefa de pessoa (rótulo `pessoa`); o export, a leitura no shared e no servidor e o teste são tarefa de agente.
- O asset de Terrain entra no repositório (binário). Só uma pessoa edita por vez.
- Biomas, lava e pântano com efeito ([mundo](../features/mundo.md#definido)) passam a vir da grade exportada, não da conta.
