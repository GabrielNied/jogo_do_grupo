# Fatia jogável: regras

**Responsável:**
**Status:** em definição (números em **proposta**, a calibrar)
**Faixa de IDs de mensagem:** não tem; cada regra usa a faixa da sua funcionalidade ([combate](combate.md), [itens](itens.md), [personagem](personagem.md), [mobs](mobs.md), [instâncias](instancias.md))

## Resumo

Regras mínimas para jogar o [primeiro marco](../visao-geral.md#primeiro-marco-fatia-jogável): atributos e recursos, um ataque (primeira arma), esquiva, um inimigo comum em grupo, loot no chão, experiência e nível, e uma dungeon de até 5 com um boss. Esta página é a **fonte** dessas regras; as páginas de cada funcionalidade apontam para cá.

## Histórico do marco

Não há foco de marco desde 2026-10-19 (Gabriel): qualquer tarefa livre pode ser puxada, na ordem de prioridade (só ficam de fora as que esperam uma pessoa ou issue aberta). A lista abaixo é o que o marco entregou ou priorizou ([ADR 0030](../decisoes/0030-processo-v2-dono-da-funcionalidade.md), 2026-10-11):

0. **Prioridade alta** (decidido por Gabriel em 2026-10-12), antes dos outros itens:
   - **Modelo de itens v2:** tier, fusão e atributos próprios dos equipamentos entregues ([#615](https://github.com/GabrielNied/jogo_do_grupo/issues/615), [#616](https://github.com/GabrielNied/jogo_do_grupo/issues/616), [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623)). **Próximo de Itens:** [#617](https://github.com/GabrielNied/jogo_do_grupo/issues/617) materiais de fusão (regras em [itens — Materiais de fusão](itens.md#materiais-de-fusão-617); épico [#619](https://github.com/GabrielNied/jogo_do_grupo/issues/619)). Poderes e sets ([#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629), [#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630)) ficam depois das skills na arma ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)).
   - **Atributos do personagem e árvore:** nomes, secundários, pipeline, árvore com tela e compra no servidor entregues ([#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622)–[#625](https://github.com/GabrielNied/jogo_do_grupo/issues/625)). **Próximo de Talentos:** [#626](https://github.com/GabrielNied/jogo_do_grupo/issues/626) redistribuição e perfis; números finais na [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) (épico [#621](https://github.com/GabrielNied/jogo_do_grupo/issues/621); regras em [personagem](personagem.md#atributos-do-personagem) e [talentos](talentos.md)).
1. **Mais mobs no mapa:** grupos de inimigos espalhados pela área, com respawn, para o jogador sempre ter o que enfrentar (seção 4 abaixo e [mobs](mobs.md)).
2. **Combate mais refinado:** cadeia de leves e pesado no ritmo do New World, esquiva, telegrafia e resposta visual de acerto (seções 2 e 3 abaixo e [combate](combate.md)).
3. **Progressão balanceada:** XP por mob e por nível que faça subir de nível num ritmo bom, e itens que melhoram de fato (seções 5 e 6 abaixo, [personagem](personagem.md) e [itens](itens.md)).

O ciclo que o marco fechava: sair do hub, enfrentar mobs, ganhar XP e loot, subir de nível e sentir o personagem mais forte.

## Definido

Já decidido pelo grupo e que esta fatia respeita (não é reaberto aqui):

- ~~Sessão pequena em instâncias, sem mundo aberto (D1)~~ Substituído em 2026-10-11 por mundo persistente (D1 opção A). Sem classes (D2); skills nos itens, sorteadas (D3); combate de ação no ritmo do New World, sem spam de skills (D4); papéis tank, healer e DPS, dungeon de 5 (D6); ataques telegrafados ([combate.md](combate.md)). Detalhes na [validação](../brainstorm/validacao.md). A fatia ainda cobre uma área e uma dungeon.
- Servidor autoritativo: o client envia intenções; dano, custo, drop, XP e morte são decididos pelo servidor.

## Proposta

Tudo marcado **proposta** abaixo vem do plano de desenvolvimento do grupo, ou é um valor inicial. Quem implementa pode calibrar e decidir ([Decisões de design](../agentes.md#decisões-de-design)), registrando aqui o valor novo.

## Fora desta fatia

Várias regiões, dezenas de jogadores na mesma área, cinco raças, 25 skills, catálogos de peças, fome, mercado e profissões (seguem em aberto em S1 a S10, ou são escala para depois desta fatia). ~~20 jogadores na mesma instância e mundo contínuo ficam de fora porque contrariam D1~~ substituído em 2026-10-11: o mundo contínuo é o formato do jogo; esta fatia ainda cobre uma área e uma dungeon.

## Em aberto

- [x] ~~Os cinco atributos (Força, Destreza, Intelecto, Vitalidade, Espírito) ficam todos na fatia ou só os que a primeira arma usa?~~ Decidido em 2026-10-11: os cinco entram na fatia (seção 1), mesmo que Intelecto e Espírito ainda não tenham uso. Desde 2026-10-12 os cinco são Força, Vigor, Agilidade, Intelecto e Espírito ([personagem](personagem.md#atributos-do-personagem)).
- [x] ~~Vida do boss e do inimigo comum para 1 a 5 jogadores (os números do plano eram para grupos maiores).~~ Decidido em 2026-10-11: valor fixo, igual para 1 a 5 jogadores, sem escalar por jogador (seções 4 e 7); os números seguem a calibrar.
- [x] ~~Quem recebe o loot do boss: maior dano (plano) ou outra regra mais justa com tank e healer?~~ Decidido em 2026-10-11: loot individual, cada jogador que participou da luta sorteia o próprio (seção 5). ~~Maior dano~~ substituído em 2026-10-11 por loot individual no boss.
- [x] ~~Troca de arma (S10) fica fora; a fatia tem uma arma equipada.~~ Decidido em 2026-10-12: duas armas, como o New World ([combate](combate.md#duas-armas-580)). Esta fatia ainda joga com uma espada.
- [x] **Livro de Asterra (proposta do board, 2026-10-11; [figjam.md](../brainstorm/figjam.md), seções 10, 11 e 09).** **Arquivado em 2026-10-16 (quiz 3):** conflitos com **Definido** valem a doc atual (loot por participação, party 5, etc.); o resto é histórico ou tarefa própria ([missões](missoes.md#definido)). Nada abaixo muda os números da fatia:
  - **Coincide** com as seções 1, 2, 3 e 7: 100 stamina e 100 mana, regeneração 10 e 5 por s após 2 s, caído 45 s, levantar em 3 s com 20% de vida. **Conflito (nível):** o livro pedia nível 1 a 30 com 5 pontos por nível; a seção 6, em 2026-10-12, adotou teto 100 e nenhum ponto de atributo por nível. Vale a seção 6. **Conflito (esquiva):** o livro pedia esquiva sem invulnerabilidade; a seção 3, em 2026-10-12, adotou a janela curta do New World. Vale a seção 3.
  - **Conflito (loot):** o livro dá os itens a quem causou mais dano, com empate para o primeiro atacante; a seção 5 decidiu loot individual por participação. Vale a seção 5.
  - **Conflito (escala):** o livro fala em instâncias de 20 jogadores; a seção 7 é para 1 a 5. Fica em "Fora desta fatia".
  - **Novidade do livro (histórico):** ~~teleporte descoberto~~ (sem teleporte, quiz 3); escalada limitada ([#349](https://github.com/GabrielNied/jogo_do_grupo/issues/349)); ~~orçamento de preparação~~ (#339 not planned); cinco raças, catálogo de skills (#167), cinco espécies de inimigos; cidade com forja, banco e mercado.
  - **Chegada ao mundo (seção 11):** prólogo e primeira hora na fila por prioridade ([#327](https://github.com/GabrielNied/jogo_do_grupo/issues/327), [#328](https://github.com/GabrielNied/jogo_do_grupo/issues/328); [missões](missoes.md#definido)).

## Regras

**Base:** New World (ritmo, ataque e esquiva), World of Warcraft (dungeon de 5), Diablo IV (loot no chão). **Igual:** ação em 3ª pessoa, esquiva com custo de stamina, dungeon com boss, itens caindo no chão. **Diferente:** sem classes, skills nos itens. ~~Sessão pequena~~ substituído em 2026-10-11 por mundo persistente; a fatia ainda tem uma área e uma dungeon.

### 1. Atributos e recursos

Quem decide: **servidor** (calcula, valida e envia o resultado; o client só mostra).

| Item | Regra | Origem |
|---|---|---|
| Atributos | Força, Vigor, Agilidade, Intelecto, Espírito, do personagem ([personagem](personagem.md#atributos-do-personagem)). Valor final = pontos da [árvore](talentos.md) + efeitos. Atributo de equipamento não soma aqui (DEC-001). Pontos da árvore só por nível via `PontosPorNivel` (1 por nível, 99 no nível 100) | decidido em 2026-10-12 (estrutura) e 2026-10-16 (pontos); demais números propostos na [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) |
| Vida | base 500, +12 por ponto de Vigor. Chegar a 0 leva ao estado caído (seção 7) | proposto na #627 |
| Stamina | base 100, +2 por ponto de Agilidade. Paga esquiva, ataque pesado e guarda. | proposto na #627 |
| Mana | base 100, +2 por ponto de Espírito. Paga as skills dos itens. | proposto na #627 |
| Regeneração | após 2 s sem atacar, esquivar ou segurar a guarda: +10 stamina/s e +5 mana/s. Segurar a guarda pausa a espera mesmo antes de um golpe gastar stamina. Vida não regenera sozinha na fatia. Regeneração é atributo secundário próprio de cada recurso ([personagem](personagem.md#atributos-secundários)); Agilidade e Espírito não a aumentam (decidido) | base #627 (quiz 5) |
| Cálculo | recalcular ao equipar ou desequipar; nunca somar o mesmo bônus duas vezes | proposta |
| Limites | valor atual nunca passa do máximo nem fica negativo; reduzir o máximo não cura | proposta |

- ~~Decidido em 2026-10-12: os cinco atributos só vêm dos itens.~~ Substituído em 2026-10-12 pela [especificação de atributos](personagem.md#atributos-do-personagem): os atributos vêm da árvore, e os itens têm atributos próprios. Força aumenta o dano físico (seção 2). Vigor aumenta a vida. Agilidade aumenta a stamina máxima. Intelecto aumenta o dano mágico (**1,5%** por ponto, como a Força, #627). Espírito aumenta a mana máxima. No código, os nomes novos valem desde a [#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622) (constantes `VidaPorVigor`, `StaminaPorAgilidade` e `ManaPorEspirito` em `RegrasRecursos`); até a [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623), o código ainda soma os atributos das peças nos do personagem.
- Dano físico pesado escala com Força; a primeira arma usa Força (seção 2).
- O client mostra barras de vida, stamina e mana; o servidor envia valores atuais e máximos quando mudam.
- **Testes esperados:** equipar/desequipar recalcula sem duplicar bônus; máximo reduzido limita o atual; regeneração só após 2 s sem atacar, esquivar ou segurar a guarda; recurso nunca fica negativo.

### 2. Um ataque: primeira arma, leve e pesado

Quem decide: **servidor** (acerto, dano, custo). O client envia a intenção (leve ou pesado, com a direção da mira) e toca a animação.

- Primeira arma: **espada**, com ataque **leve** (corte rápido, sem custo) e **pesado** (corte lento, gasta stamina). O clique no botão do leve é o leve; segurar o mesmo botão por **0,3 s** (`SegurarParaPesadoSegundos`) começa o pesado. As teclas estão em [interface](interface.md#teclas-e-atalhos). Valores iniciais, **proposta a calibrar**: leve 0,5 s de preparo, dano 1,0x; pesado 1,0 s de preparo, dano 2,2x, custo 20 de stamina, recuperação maior.
- **Cadeia de leves** (decidido em 2026-10-12, ritmo da espada do New World, fácil de mudar): três leves seguidos. O 1º e o 2º ficam em 1,0x; o 3º em **1,2x**. O pesado no lugar de qualquer passo usa o 2,2x e a cadeia volta ao 1º. A cadeia também volta ao 1º se o próximo golpe não começar em até **0,8 s** depois que a ação anterior acaba. Errar o alvo não quebra a cadeia: o passo sobe quando a ação começa. Cada leve é uma ação: no fim dela dá para esquivar, e o próximo leve só sai se a pessoa pedir. A cadeia não prende ninguém num combo. Constantes em `RegrasAtaque`: `PassosDaCadeiaLeve`, `MultiplicadorDoTerceiroLeve`, `JanelaDaCadeiaSegundos`.
- Cada ataque tem preparo, momento de acerto e recuperação; o dano só vale no momento de acerto, dentro do arco da lâmina do golpe (combate.md, "Arco da lâmina").
- **Compromisso** (decidido em 2026-10-12, como no New World): durante preparo e momento de acerto o personagem anda a no máximo **40%** da corrida (`VelocidadeDuranteGolpe`) e a esquiva é recusada. Na recuperação, a esquiva cancela o que falta dela e gasta a stamina da esquiva.
- **Pesado firme** (decidido em 2026-10-12): dano tomado durante o pesado não interrompe a ação. O dano continua valendo. A reação dos outros golpes está em [combate](combate.md#reação-ao-acerto).
- **Mira** (decidido em 2026-10-12): o arco da lâmina usa a direção que o personagem olha no momento do acerto, limitada pelo giro desde o início do preparo. Leve até **240°/s**, pesado até **60°/s** (`GiroMaximoLeveGrausPorSegundo`, `GiroMaximoPesadoGrausPorSegundo`). Sem alvo fixo.
- O mesmo golpe **não acerta o mesmo alvo duas vezes**; acerta vários alvos no volume.
- Pedir novo ataque durante a ação espera o fim dela (fila de 1 pedido, descartado se passar de 0,3 s, **proposta**).
- Sem stamina suficiente, o pesado é recusado (o leve sempre pode).
- Valores que faltavam para implementar, **proposta a calibrar** (constantes em `RegrasAtaque`, no shared): recuperação de 0,3 s no leve e 0,6 s no pesado; dano do golpe = (dano da espada 20 + 2 por ponto de Força) x o multiplicador do ataque, arredondado (valor de teste: na [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623) e na [#624](https://github.com/GabrielNied/jogo_do_grupo/issues/624), o 20 vira o dano da arma e a Força passa a aumentar em %, veja [combate](combate.md#em-aberto)); o volume é medido no plano do chão (a altura não conta, então por enquanto o pulo não tira ninguém do golpe).
- As skills dos itens ficam fora desta seção (veja [skills.md](skills.md) e [itens.md](itens.md)).
- Base: New World. Igual: cadeia de leves, pesado firme, stamina na esquiva e no pesado, esquiva com janela curta sem dano (seção 3) e guarda segurada ([combate](combate.md#guarda-segurada)). Sem parry. Nesta fatia ainda é uma espada só; duas armas e três skills por arma são o desenho, e entram depois (S10 e [skills](skills.md)).
- **Testes esperados:** acerto único por golpe e alvo; vários alvos no volume; pesado recusado sem stamina; fila de ataque; dano calculado só no servidor; o 3º leve usa 1,2x e o 4º volta a 1,0x; pesado no meio da cadeia volta a cadeia ao 1º; cadeia expira em 0,8 s; durante o preparo o deslocamento não passa de 40% da corrida e a esquiva é recusada; na recuperação a esquiva corta o resto da ação; o giro do pesado não passa de 60° no 1 s de preparo.

### 3. Esquiva

Quem decide: **servidor** (custo, deslocamento e posição final); o client antecipa o movimento.

- Deslocamento de **5 m em 0,6 s**, custo **15 de stamina**. A direção é a do movimento nesse instante; parado, é para trás da mira (`DirecaoDaEsquiva`). Nos primeiros **0,3 s** (`JanelaDeInvulnerabilidadeSegundos`) o dano direto some; o servidor confere o instante, o client não avisa que desviou. Depois dessa janela, o golpe acerta se a pessoa ainda está no volume.
- Sem stamina suficiente, a esquiva é recusada.
- Não atravessa paredes nem sai do chão válido; o servidor valida a posição final.
- Serve contra ataques telegrafados: o aviso dá tempo de esquivar ou sair da área.
- **Testes esperados:** esquiva gasta 15 de stamina; recusada sem stamina; não atravessa parede; andando, sai na direção do movimento; parado, sai para trás da mira; golpe no meio dos primeiros 0,3 s não tira vida; golpe depois desses 0,3 s, ainda no volume, tira vida.

### 4. Um inimigo comum, em grupo

Quem decide: **servidor** (IA, alvo, dano, morte). Força **fixa**: o inimigo não escala com o número nem o nível dos jogadores.

- Um único tipo de inimigo comum, em **grupos de 3 a 5** nas salas da dungeon (**proposta**).
- Estados: repouso, alerta, perseguição, ataque, retorno, morto (**proposta**). Persegue o alvo mais próximo que viu; se o alvo fugir longe, volta ao ponto de origem.
- Ataque **telegrafado** (decidido em 2026-10-12): o aviso do inimigo comum é o preparo da própria animação, cerca de 1 s (valor a calibrar). O dano sai quando o modelo encosta, no volume à frente do corpo naquele instante, e não num círculo desenhado no chão. Quem saiu a tempo não leva. Os números e o passo do golpe estão em [mobs](mobs.md#contato-do-golpe). Investida, pisada e raios do chefe continuam com a marca no chão.
- Decidido em 2026-10-11: a vida é **valor fixo**, igual para grupos de 1 a 5, sem escalar por jogador; o número segue a calibrar.
- Exemplo de força fixa: inimigo de nível 3 com **180 de vida** diante de jogador de nível 1 ou 30 (**proposta**).
- Sem respawn durante uma corrida de dungeon; ~~o respawn de **120 s** do plano fica para o mundo (fora da fatia).~~ Decidido pelo Gabriel em 2026-10-11: o primeiro grupo de inimigos fica no **campo aberto**, a uns 150 m do portão norte, e volta **120 s** depois de morto; a dungeon reaproveita o mesmo inimigo, sem volta. Detalhes em [mobs.md](mobs.md), "Inimigo comum da fatia".
- O client mostra barra de vida, o preparo do golpe no modelo e a morte; não decide dano. A marca no chão fica só nos ataques de área do chefe.
- Base: RYL e Diablo IV (grupos de inimigos). Diferente: grupos pequenos.
- **Testes esperados:** vida igual com 1 ou 5 jogadores; transições de estado; dano só no servidor; inimigo morto não age; quem está à frente do corpo no fim do preparo leva dano; quem saiu do volume não leva; o golpe comum não usa círculo no chão.

### 5. Loot no chão

Quem decide: **servidor** (sorteio, dono, validação da coleta).

- Ao morrer, o inimigo larga itens no chão da instância. São peças reais do catálogo de equipamentos: a raridade pesa pela origem do inimigo (comum, elite, chefe raro, chefe) e o grau e o requisito vêm do nível dele. Tabela e meta de ritmo em [itens.md](itens.md#loot-dos-inimigos-493). O aviso de coleta mostra nome e raridade. A raridade vira tier calculado pelos atributos na [#615](https://github.com/GabrielNied/jogo_do_grupo/issues/615) ([itens](itens.md#tier)).
- ~~**Dono do loot** (**proposta**): quem causou **mais dano real** àquele inimigo; empate vai ao primeiro atacante; dano contínuo conta para quem o causou. Resolve-se uma vez, na morte.~~ Substituído em 2026-10-11: **loot individual também no inimigo comum**; cada jogador que participou (o servidor define "participou") sorteia o próprio loot, no chão só para ele.
- **Loot do boss** (decidido em 2026-10-11): **individual**; cada jogador que participou da luta sorteia o próprio loot, no chão para ele (o servidor define "participou"). ~~Dono por maior dano~~ substituído em 2026-10-11 por loot individual, só para o boss; o inimigo comum segue a proposta acima.
- **Quem participou** (decidido por Gabriel em 2026-10-16): quem causou dano ao inimigo, curou alguém que lutava com ele ou foi alvo dele, esteja ou não no grupo de quem matou, como o crédito compartilhado do New World. Não existe dono do mob nem roubo de mob. Vale para loot, XP e objetivo de [missão](missoes.md). Hoje o código conta só quem causou dano ([mobs](mobs.md#inimigo-comum-da-fatia-issue-63)).
- Coleta pela tecla **F** (**proposta**: raio de 12 m). O servidor confere dono, disponibilidade e espaço no inventário.
- Inventário cheio: o item continua no chão, sem perda, até expirar.
- Reservado ao dono por **180 s**; ao expirar desaparece (**proposta**).
- O client mostra o item no chão, o dono e o tempo restante.
- **Testes esperados:** ~~maior dano recebe o loot; empate vai ao primeiro atacante;~~ cada participante recebe o próprio sorteio e só ele coleta; quem não participou não recebe; coleta simultânea dá o item a um só; inventário cheio mantém o item; expiração remove; variante aumenta a raridade; nível do inimigo define grau e requisito; jogador solo recebe uma peça melhor no ritmo da meta nos níveis 1-10.

### 6. Experiência e nível

Quem decide: **servidor** (concede e confere; cada prêmio vale uma vez).

- Nível inicial 1; nível máximo **100** (decidido em 2026-10-12).
- XP para o próximo nível = arredondar(50 × nível^1,2) (decidido em 2026-10-12; meta em [personagem](personagem.md#ritmo-de-progressão)).
- XP vem de derrotar inimigos; quem **participou** (dano, cura, defesa) recebe, mesmo sem ser o dono do loot. O valor cai pela diferença de nível ([personagem](personagem.md#ritmo-de-progressão)).
- Subir de nível dá pontos da árvore via `PontosPorNivel`, a única fonte deles ([personagem](personagem.md#atributos-do-personagem)); 1 por nível (99 no nível 100, decidido em 2026-10-16).
- No nível máximo o XP excedente é guardado e não sobe de nível.
- O client mostra barra de XP e nível.
- **Testes esperados:** XP certo por nível; mob 5 níveis abaixo não dá XP; mob do mesmo nível dá o valor inteiro; subir de nível não cria ponto de atributo; mesmo prêmio não concede XP duas vezes; teto de nível.

### 7. Dungeon de até 5 com um boss

Quem decide: **servidor** (instância, grupo, estado do boss, resultado).

- Instância para **1 a 5** jogadores, criada ao entrar pelo hub e fechada ao fim (**proposta**; base: WoW).
- Percurso (**proposta**, a partir da Mina do Reflexo do plano): entrada, três salões com grupos do inimigo comum e um salão do boss. O desenho físico é do [mundo](mundo.md).
- Boss: **um**, de força fixa (não cresce com mais jogadores), com ataques telegrafados (investida, pisada e raios com **3 s de aviso**, **proposta**) e uma janela em que fica exposto a mais dano. A vida é **valor fixo**, igual para 1 a 5 jogadores (decidido em 2026-10-11); o número final segue a calibrar.
- Morrer não perde equipamento (**proposta**): o personagem fica **caído por 45 s**; um aliado o levanta interagindo por **3 s** (volta com **20%** de vida); dano no ajudante interrompe. Se o tempo acabar, retorna ao hub sem perdas. Dois ajudantes não duplicam a cura.
- Vitória: o boss morto gera loot (seção 5) e XP; a instância libera a saída.
- Derrota: todos caídos ou fora leva ao hub e a instância fecha.
- O client mostra o grupo, os estados caído/levantando, os avisos do boss e o resultado.
- **Testes esperados:** boss com a mesma vida para 1 ou 5; levantar uma vez só; fim do tempo e ajuda simultâneos resolvem uma saída; morrer não perde itens; instância fecha ao fim.

## Depende de

[Personagem](personagem.md) (atributos, nível), [Itens](itens.md) (arma, loot), [Mobs](mobs.md) (inimigo e boss), [Instâncias](instancias.md) (dungeon), [Mundo](mundo.md) (hub e salões).

## Expõe

Nada: é documento de regras. Os contratos de rede saem dos agentes de implementação, dentro da faixa de cada funcionalidade.
