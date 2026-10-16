# 0020. Mapa do mundo calculado no shared, por semente fixa

**Data:** 2026-10-11
**Status:** substituída por [0044](0044-mapa-feito-a-mao-exportado-para-o-shared.md) (a geração por semente vale até o mapa feito à mão ficar pronto)

## Contexto

O mundo passou de 1 km plano para 3 km com relevo e sete biomas ([mundo.md](../features/mundo.md), "Biomas e relevo", issue #135). Com montanha, o servidor precisa saber a altura do chão para conferir o movimento, e mais tarde para pôr inimigos e loot no lugar certo. O client precisa da mesma altura para desenhar o terreno.

Opções avaliadas:

- **Terreno do Unity pintado à mão** (cena ou asset): bom de editar no editor, mas o servidor não lê, o arquivo é binário e grande, e duas pessoas não mexem nele ao mesmo tempo.
- **Mapa de alturas em arquivo** (imagem ou tabela) lido pelos dois lados: funciona, mas é mais um arquivo para gerar, versionar e manter igual nos dois lados.
- **Conta no shared, por semente fixa:** uma função diz o bioma e a altura de qualquer ponto. Client e servidor chamam a mesma função.

## Decisão

- O mapa do mundo é a classe `MapaDoMundo`, em `shared/Package/Runtime/Features/Mundo/`: dado um ponto, devolve os pesos dos biomas e a altura do chão. Sem `UnityEngine`, sem arquivo de dados, com sementes fixas no código.
- A altura oficial é a de uma **grade de 10 m** (301 × 301 pontos), com cada célula dividida em dois triângulos. É a mesma malha que o client desenha, então o chão que o jogador pisa e o chão que o servidor confere são o mesmo.
- O sorteio usa só contas com inteiros, e o ruído usa só soma, multiplicação e divisão de `float`, para dar o mesmo resultado no Unity e no .NET. As duas potências que sobram podem variar na última casa; por isso o servidor confere a altura com folga de metros, não de milímetros.
- O client monta o terreno em tempo de execução (`ConstrutorDoTerreno`), como já fazia com a cidade. Nenhuma cena ou asset de terreno entra no repositório.
- Mudar o relevo é mudar a conta e os testes dela, num PR que mexe em shared, servidor e client juntos.

## Consequências

- Uma fonte só para o relevo. Os testes do shared (xUnit, no CI) garantem os limites de altura, a cidade plana e o lado de cada bioma.
- O mundo não é editável no editor do Unity: um morro fora do lugar se corrige na conta, não com pincel. Lugares feitos à mão (uma vila, a entrada de uma dungeon) precisam de outra solução, por cima deste mapa. Fica em aberto.
- Entrar no mundo calcula a grade inteira (90 mil pontos) e cria 225 malhas com colisor. O custo ainda não foi medido no Unity; se pesar, o caminho é carregar os blocos por distância.
- O servidor calcula a altura a cada movimento recebido (três ou quatro pontos da grade). Se pesar com muitos jogadores, basta guardar a grade em memória (360 KB).
