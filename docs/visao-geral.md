# Visão geral

## O jogo

RPG de ação online para PC, medieval, em terceira pessoa, de **mundo aberto**. ~~feito para **jogar com poucas pessoas**. O grupo entra em sessões pequenas para subir de nível, juntar loot, enfrentar eventos e, principalmente, derrotar bosses cada vez mais fortes em dungeons e raids.~~ Substituído em 2026-10-11 (D1 opção A): os jogadores compartilham um mundo contínuo, sobem de nível, juntam loot, enfrentam eventos e derrotam bosses cada vez mais fortes em dungeons e raids.

Decisões de direção tomadas em 2026-10-10 e revisadas em 2026-10-11, a partir do brainstorm: veja [brainstorm/validacao.md](brainstorm/validacao.md). Jogos usados como base: [referencias.md](referencias.md).

## Pilares

1. ~~**Sessões de grupo pequeno:** o foco é jogar em poucas pessoas, em instâncias, e não num mundo aberto com centenas de jogadores.~~ Substituído em 2026-10-11 por: **Mundo persistente:** um mundo único e contínuo, compartilhado por quem está na área. Dungeons e raids continuam instanciadas.
2. **Sem classes, você é o que equipa:** as skills estão nos itens, geradas aleatoriamente, e a combinação de itens define o estilo de jogo.
3. **Combate de ação:** mira, esquiva e posicionamento, no estilo de New World.
4. **Progressão por loot e bosses, sem fim:** subir de nível farmando mapas em grupo, juntar muito loot, participar de eventos e derrotar bosses mais fortes em dungeons (5 jogadores) e raids (10), com dificuldade que escala sem fim, no estilo de Diablo.
5. **Papéis que se completam:** tank, healer e DPS, cada um com buffs que ajudam o time, no estilo de World of Warcraft. Sem classes: o papel vem dos itens.
6. **Fusão de itens:** nenhum drop é lixo. Itens são fundidos para evoluir, com limites claros para manter o equilíbrio.

## Foco atual

**Sistema de itens** (2026-10-10): itens com skills sorteadas (ativas, passivas e buffs), papéis vindos dos itens e fusão. Talentos ficam para depois.

## Diferenciais

<!-- O que este jogo tem que outros não têm? -->

Candidatos que aparecem no brainstorm, ainda a validar:

- Skills aleatórias nos itens combinadas com a fusão: builds quase infinitas, sem classe.
- Luz e som como mecânica: noite escura de verdade, skills que iluminam, sons ouvidos de longe.

## Ideias ainda não confirmadas

Sugestões que ainda não entraram no board: progressão (nível máximo, endgame), lore, eventos (world bosses, sazonais), monetização, customização de aparência, montarias e pets, moradia (housing), facções.

## Plataforma

PC.

## Primeiro marco: fatia jogável

Antes de expandir, o objetivo é uma versão pequena e completa. Revisada em 2026-10-10 a partir das decisões D1–D5:

- [ ] Uma área segura (a cidade) do mundo persistente, onde os jogadores se encontram. ~~Uma área segura (hub) onde o grupo se encontra~~ substituído em 2026-10-11 pela cidade compartilhada (D1 opção A).
- [ ] Uma dungeon instanciada para até 5 jogadores, com um boss no final
- [ ] Itens suficientes para montar os três papéis: tank, healer e DPS
- [ ] Uma raça, com a sua skill base
- [ ] Alguns itens com skills aleatórias, para testar a "build pelos itens"
- [ ] Um tipo de inimigo comum, aparecendo em grupo
- [ ] Combate de ação: ataque, esquiva e as skills dos itens
- [ ] Loot no chão, coletado com uma tecla
- [ ] Ganho de experiência e subida de nível

Regras detalhadas de cada item (números em proposta): [features/fatia-jogavel.md](features/fatia-jogavel.md).

## Histórico de direção

O que foi substituído fica registrado aqui, para entender de onde o projeto veio.

**Até 2026-10-10** (antes da validação do brainstorm):

- ~~MMORPG para PC, criado em grupo, com mundo aberto e foco em trazer mecânicas novas para o gênero.~~ Substituído em 2026-10-10 pelas sessões de grupo pequeno (D1 opção B). ~~Sessões de grupo pequeno~~ substituídas em 2026-10-11 pelo mundo persistente (D1 opção A); vale a descrição atual de "O jogo".
- Pilares antigos:
  1. ~~**Mundo aberto:** exploração livre, regiões conectadas, PvP no mundo aberto com áreas seguras.~~ Substituído em 2026-10-10 pelas sessões de grupo pequeno. ~~Sessões de grupo pequeno~~ substituídas em 2026-10-11: o mundo aberto volta (D1 opção A).
  2. **Economia movida pelos jogadores:** profissões interdependentes, itens negociáveis (inclusive os fundidos), casa de leilão e saídas de recursos para evitar inflação. Não foi descartado: fica para ser decidido durante a construção (veja [economia.md](features/economia.md)).
  3. **Fusão de itens:** continua como pilar 6.
  4. ~~**Personalização de personagem:** raças, classes, magias (skills) e árvore de talentos.~~ Substituído por "sem classes, você é o que equipa" (D2 e D3).
- Fatia jogável antiga:
  - ~~Uma região do mapa~~
  - ~~Uma raça e uma classe~~
  - ~~Algumas magias e um inimigo~~
  - ~~Alguns jogadores conectados ao mesmo tempo, se vendo e lutando juntos~~ (continua valendo, dentro da sessão do grupo)
