# Personagem

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 3000–3999

## Resumo

Raças, atributos e progressão de nível. Sem classes: o estilo de jogo vem da combinação de atributos, árvore, equipamentos e skills.

## Definido

- Atributos principais e secundários do personagem: [Atributos do personagem](#atributos-do-personagem) (decidido em 2026-10-12, prioridade alta)
- Recursos, XP e nível da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- ~~Raças com características únicas; cada raça dá uma **skill base** própria~~ Substituído em 2026-10-16 (Gabriel, quiz de definições): **raça é só visual**. Não dá traço, skill racial nem bônus, e a história é a mesma para todas ([missões](missoes.md#definido)).
- **Sempre correndo** (decidido por Gabriel em 2026-10-16): a velocidade normal do personagem é a de corrida de hoje (5,5 m/s). Não há tecla de correr; uma tecla de **andar devagar** (Ctrl esquerdo, segurar) usa a velocidade de andar. A esquiva vai para o **Shift** e o pulo continua no Espaço ([teclas](interface.md#teclas-e-atalhos)).
- **Natação e dano de queda** (decidido por Gabriel em 2026-10-16): rios e lagos se atravessam a nado (sem atacar, gastando stamina), e cair de muito alto tira vida. O servidor valida profundidade e altura.
- **Escalada limitada** (decidido por Gabriel em 2026-10-16, quiz 3): só **vias marcadas** (bordas, trechos de dungeon ou mundo), estilo New World — sem parkour livre ([#349](https://github.com/GabrielNied/jogo_do_grupo/issues/349)). Números e marcador em [Natação, queda e escalada](#natação-queda-e-escalada-349).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3):
  - **Sem teleporte jogável** por enquanto ([mundo](mundo.md#definido)). Retorno ao cair/morrer: no **mapa aberto**, à **cidade mais próxima** (com várias cidades no futuro; hoje só Brasa Mansa); na **dungeon**, à **entrada/safe zone da instância** (vida cheia, grupo continua), não ao hub da cidade.
- **Renomear não pode** (decidido por Gabriel em 2026-10-16): o nome escolhido na criação é permanente.
- **Sem classes** (D2 da [validação de 2026-10-10](../brainstorm/validacao.md)): ~~a combinação de itens define o estilo de jogo~~ desde 2026-10-12, a especialização vem de atributos, [árvore](talentos.md), equipamentos e skills
- Progressão por nível (leveling) faz parte do loop principal (D5)
- ~~Classes diferenciadas~~ substituído em 2026-10-10 por "sem classes" (D2)

### Criação e seleção de personagem: definido (provisório)

Decidido pelo grupo em 2026-10-10. É **provisório**: serve para implementar a primeira versão e o grupo pode ajustar depois. Os detalhes estão em [Regras](#regras).

- A seleção de personagem exige estar **logado** (nome de usuário + senha). Conta e login são infraestrutura do Core, não desta funcionalidade.
- Até **4 personagens por conta**.
- Nome do personagem: **3 a 16 letras**, só letras de A a Z, sem acento, espaço ou número; **único** no servidor sem diferenciar maiúsculas.
- ~~**Uma raça provisória: Humano**, até o grupo definir as raças definitivas.~~ substituído em 2026-10-11 por: **cinco raças jogáveis** (veja [Raças](#raças-propostas-no-board-2026-10-11) e [Raça](#raça)), a pedido do Gabriel; o antigo Humano (valor 1) virou **Valdren**.
- O personagem começa no **nível 1**.
- Fluxo de entrada: abertura → login (ou registro) → seleção de personagem → criação → volta para a seleção. ~~"Entrar" ainda não leva a um mundo.~~ substituído em 2026-10-11 por: "Entrar" leva ao mundo (veja [Fluxo de entrada no jogo](#fluxo-de-entrada-no-jogo)).
- A seleção mostra quatro vagas na ordem recebida, destaca a escolhida e usa o prefab real da raça como prévia 3D. O nome, a raça e o nível aparecem no resumo ao lado de **Entrar**; vaga livre não imita um personagem. Em largura compacta, lista, prévia e resumo se empilham no painel rolável. A prévia representa a raça, pois ainda não existe personalização de aparência.
- Na criação, nome e cinco raças ficam junto da prévia do prefab escolhido e dos textos definidos para essa raça. A habilidade racial aparece identificada como **em desenvolvimento**; escolher a raça ainda não concede uma habilidade jogável. Em largura compacta, as três áreas se empilham e rolam.
- Comunicação pela API HTTP do [ADR 0009](../decisoes/0009-api-http-para-conta-e-personagens.md) (veja [Expõe](#expõe)).
- O **servidor decide**: valida o nome, a unicidade, a raça e o limite de 4. O client só confere o formato do nome para dar retorno rápido.

## Natação, queda e escalada (#349)

Regras de [Definido](#definido), implementadas em `ConfigMovimentoEspecial` e `RegrasMovimentoEspecial` (shared; client e servidor usam as mesmas), com o servidor decidindo.

- **Natação:** há água para nadar onde a profundidade é de pelo menos **1,3 m** e os pés estão ao menos 0,2 m abaixo da superfície. O personagem **boia** (pés 0,9 m abaixo da superfície), nada a **3 m/s**, sem pulo e **sem atacar** (o servidor recusa o ataque com `VoceEstaNadando`). Gasta **4 de stamina por segundo**; sem stamina continua nadando, a **50%** da velocidade (teto de velocidade do Mundo). Não há afogamento.
- **Onde há água:** rios, lagos e pântano do terreno valem sozinhos (o chão abaixo do nível da água do `MapaDoMundo`, ou da superfície do rio alto). Fora do terreno (poça de dungeon) e em lagos marcados à mão, vale um volume marcado de água. A lava não conta.
- **Queda:** o servidor mede a altura caída (soma da descida em queda livre, comparando as posições que ele aceitou a cada 100 ms; descer rampa de até 45° a pé não conta). Até **4 m** não machuca (o pulo chega a 1,25 m). Acima disso, **40 de vida por metro** além dos 4 m, no máximo **300**. Cair em **água de 2 m ou mais** não machuca; em água mais rasa machuca como no chão. Teleporte, volta à cidade e mudança de área reiniciam a medição, e quem está caído não leva dano de queda. A vida e o evento de dano ambiental são de Combate.
- **Escalada:** só dentro de uma **via marcada**. Espaço dentro da via agarra (o servidor confere o volume, a stamina para o primeiro segundo e que não está nadando nem caído), W/S sobem e descem, A/D andam de lado, Espaço solta com um pulinho para trás e, ao chegar no topo, o personagem passa para cima. Gasta **6 de stamina por segundo**; sem stamina, sai da via e cai (com o dano de queda se for alto). Sai da via sozinho ao deixar o volume. Não ataca na parede (`VoceEstaEscalando`).
- **Só cidade e mundo aberto:** nas instâncias (dungeon) ainda não há natação, queda nem escalada; fica para quando as instâncias tiverem terreno e volumes próprios.

### Marcador de água e de via de escalada

Um `MarcadorDeVolume` (client, Personagem) é um cubo na cena: a **posição é o centro**, a **escala é o tamanho** (use o cubo de 1 m) e o **giro em Y** vale. Na água, o topo da caixa é a superfície e o fundo é o chão do lago. Na via, a **frente (+Z, a seta laranja no Scene)** aponta para a parede, e a caixa deve cobrir a parede de baixo a cima mais o espaço onde o personagem fica. O marcador tem **Tipo**, **Id** único e **Exportar**.

- **Exportar ligado:** o menu `Jogo do Grupo/Personagem/Exportar volumes marcados` copia todos os marcadores exportáveis das cenas abertas para `VolumesMarcados.Todos` (shared). É essa lista que o servidor confere e que o client usa. A exportação reescreve a lista inteira, então abra todas as cenas com marcadores antes de exportar; depois rode `dotnet csharpier format .` e commite.
- **Exportar desligado:** o volume vale só no client, para a cena de validação (`Features/Personagem/Dev/Movimento/ValidacaoMovimentoEspecial.unity`, gerada pelo menu `Criar cena de validação de movimento` ou por `unity:executar`: piscina de 3 m, torre de 12 m com via e plataformas de 3, 7 e 12 m para a queda). Como o servidor não conhece esses volumes, ele recusa a escalada ali; a cena serve para ver o movimento do client.
- **Decisão:** o catálogo vive no shared (e não só na cena) porque o servidor não lê cenas; assim servidor e client conferem o mesmo dado. O mapa do mundo e o Terrain não foram tocados.
- **Pendente:** animações de nadar e escalar (hoje o Animator usa a corrida e a queda), efeito visual da água na tela e vias de escalada de verdade no mundo (dependem do mapa que o Gabriel desenha).

## Regras puras de experiência (#136)

`RegrasExperiencia` implementa a curva decidida da [fatia jogável, seção 6](fatia-jogavel.md#6-experiência-e-nível). Os ajustes ficam em [ConfigExperiencia](../../shared/Package/Runtime/Features/Personagem/ConfigExperiencia.cs): nível máximo, pontos por nível, base de XP e expoente da curva. Reduzir a base acelera a progressão; o expoente controla o crescimento do custo entre níveis.

`XpParaOProximoNivel` arredonda para o inteiro mais próximo (empates para longe de zero), inclusive ao consultar o nível máximo; nesse nível, o custo é apenas informativo. `AplicarGanho` recebe nível, XP dentro do nível e ganho; devolve nível, XP restante e pontos ganhos nessa aplicação. Pode subir vários níveis e guarda o excedente no teto. Ganho zero ou negativo preserva o estado; nível fora da faixa e XP acumulado negativo são recusados. XP usa `long`; soma acima da capacidade é recusada, sem truncamento.

`AplicarAjuste` existe só para o comando de teste `xp` ([Desenvolvimento](desenvolvimento.md)). A quantidade pode ser negativa: desce de nível, devolve a barra do nível anterior e tira `PontosPorNivel` dos pontos livres por nível perdido, sem passar de zero e sem desfazer pontos já gastos.

**Prêmio duplicado (#491):** o servidor trava o mesmo evento de morte (inimigo + sequência) com uma lista em memória que esquece o par depois de 10 minutos (`PrazoDaTravaDeMorte`). Nada é gravado: o par se repete após reiniciar o servidor, e a morte tem de premiar de novo. Com banco ocupado ou falha de serialização a gravação é tentada até 5 vezes (`TentativasDeGravacao`); se todas falharem, a trava da morte é solta.

Estas funções não concedem prêmios nem persistem dados. Integração com servidor, mensagens, barra de XP e prevenção de prêmio duplicado continuam na #65. O padrão para outros valores ajustáveis está nas [convenções](../convencoes.md#valores-de-balanceamento).

## Atributos do personagem

Decidido por Gabriel em 2026-10-12 (especificação de atributos; entrada em [arvore-de-atributos.md](../brainstorm/arvore-de-atributos.md)). **Prioridade alta**, épico [#621](https://github.com/GabrielNied/jogo_do_grupo/issues/621). A estrutura está definida; fórmulas e valores da [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) foram **aprovados como base** em 2026-10-16 (quiz 5; tabela em [talentos](talentos.md#implementação-da-627-números-da-árvore-e-dos-atributos); calibrar na Sintonia). Esta seção é a fonte do que cada atributo faz; a árvore que distribui os pontos está em [talentos.md](talentos.md).

**Princípio:** os atributos do personagem e os atributos dos equipamentos são **sistemas independentes**. A árvore é a progressão do personagem; os equipamentos têm atributos e progressão próprios ([itens](itens.md)). Os dois se encontram no combate, pelas fórmulas, mas não dividem pontos, e um não muda a progressão do outro (DEC-001, [itens](itens.md#invariantes)).

### Atributos principais

| Atributo | Responsabilidade | O que faz | Valor por ponto (#627, base aprovada) |
|---|---|---|---|
| Força | Dano físico | Aumenta o dano físico, conforme a arma e a skill usadas | +1,5% do dano |
| Vigor | Vida | Aumenta a vida máxima | +12 de vida (base 500) |
| Agilidade | Stamina | Aumenta a stamina máxima | +2 de stamina (base 100); não mexe na regeneração |
| Intelecto | Dano mágico | Aumenta o dano mágico das skills e de outras fontes mágicas | +1,5% do dano |
| Espírito | Mana | Aumenta a mana máxima | +2 de mana (base 100); não mexe na regeneração |

- **Na tela:** a janela do personagem lista os cinco atributos na ordem da tabela (Força, Vigor, Agilidade, Intelecto, Espírito), cada um com o efeito ao lado ("Vigor: 3 (aumenta a vida)"); os textos ficam em `TextosPersonagem`. As barras do HUD são vida, stamina e mana.
- **Sem efeito implícito:** um atributo principal não dá outros bônus (esquiva, velocidade, regeneração...) sem decisão registrada. Cada efeito tem fonte e fórmula identificáveis.
- **Recursos:** vida, stamina e mana. Decidido em 2026-10-12 (coordenador), para acabar com a colisão de nomes: o recurso que o código chama de "vigor" passa a ser **stamina**, e "energia" passa a ser **mana**. Vigor fica só como atributo. A troca no código foi a [#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622).
- ~~Destreza~~ e ~~Vitalidade~~ substituídos em 2026-10-12 por **Agilidade** e **Vigor**.
- **Pontos da árvore vêm só de subir de nível** (Gabriel, 2026-10-12). Melhorar equipamento aumenta só os atributos do próprio item e nunca dá ponto da árvore. Cada nível dá 1 ponto (99 no nível 100, decidido em 2026-10-16).

### Atributos secundários

Refinam a especialização e complementam os principais. Cada um é independente dos outros.

| Atributo | Responsabilidade |
|---|---|
| Regeneração de vida | Recuperação de vida ao longo do tempo |
| Regeneração de stamina | Recuperação de stamina ao longo do tempo |
| Regeneração de mana | Recuperação de mana ao longo do tempo |
| Chance crítica | Probabilidade de um ataque ou efeito elegível causar acerto crítico |
| Dano crítico | Modificador aplicado quando o acerto crítico acontece |
| Velocidade de ataque físico | Velocidade de execução dos ataques físicos |
| Velocidade de ataque mágico | Velocidade de execução dos ataques mágicos (conjuração) |
| Penetração física | Interação ofensiva com a defesa física do alvo |
| Penetração mágica | Interação ofensiva com a defesa mágica do alvo |

- As três regenerações são independentes: cada recurso tem fontes e valores próprios.
- Velocidade física e mágica são separadas, para que ataques físicos e mágicos não dividam a mesma progressão.
- As fórmulas de crítico, velocidade e penetração (com defesa, mitigação e resistência) são do [Combate](combate.md). A pipeline que junta as fontes (#624) está em [combate](combate.md#atributos-secundários-e-pipeline-624). **Na tela:** a janela do personagem lista os nove com o valor e a origem de cada parcela; a regeneração de vida tem base 0 (só árvore e equipamento a dão).

### Personagem e equipamento

- **Atributos do personagem:** o que o jogador investiu na [árvore](talentos.md).
- **Atributos do equipamento:** pertencem ao item e evoluem pelas regras de [itens](itens.md) (tier, fusão). Não dão ponto da árvore, não sobem nó, não abrem caminho e não são convertidos em Força, Vigor, Agilidade, Intelecto ou Espírito. O item também tem atributos com esses cinco nomes, mas eles são do item: cada um vira número de combate pela sua própria conversão e nunca soma no atributo do personagem. Lista e conversão em [itens, Atributos dos equipamentos](itens.md#atributos-dos-equipamentos); a implementação é a [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623).
- Trocar de equipamento não muda os pontos investidos nem pede redistribuição.
- Personagem, equipamento e propriedades das skills interagem no combate, pelas fórmulas do [Combate](combate.md).

### Números da #627

Os valores de recursos e dano por ponto, os secundários, a árvore e o custo de redistribuir vêm da simulação da [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) (tabelas em [talentos](talentos.md#implementação-da-627-números-da-árvore-e-dos-atributos)), **aprovados como base** em 2026-10-16 (quiz 5). Eles substituem os "valores de teste" da [fatia, seção 1](fatia-jogavel.md#1-atributos-e-recursos). Nenhum atributo principal dá regeneração, esquiva ou velocidade sem decisão registrada.

### Transição

Os nomes novos valem no código ([#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622)) e o equipamento tem atributos próprios desde a [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623): nada do item soma nos cinco atributos do personagem.

### Pendente

Só o bônus condicional por categoria de skill dos nós grandes ([talentos](talentos.md#números-propostos-627)), que espera as propriedades das skills.

## Raças propostas no board (2026-10-11)

> **Traço e skill racial saíram em 2026-10-16** (Gabriel): raça é só visual ([Definido](#definido)). As colunas abaixo ficam como registro do board; nada delas será implementado (#341 fechada).

O "Livro de ideias de Asterra" do FigJam propõe **cinco povos jogáveis**. Cada raça dá um **traço** e uma **skill racial**; armas e equipamentos continuam livres para todos, e cada raça tem 1 ou 2 modelos iniciais. O board avisa que nomes e números novos **continuam propostas**: **implementadas no servidor, na criação e no visual** desde 2026-10-11 (só o traço e a skill racial ainda não: [Skills](skills.md), #341) ([Raça](#raça)). Texto completo de cada raça (identidade, aparência, combinações): [figjam.md](../brainstorm/figjam.md#01--raças-de-asterra).

| Raça | Traço | Skill racial (recarga) |
|---|---|---|
| Valdren (humanos da fronteira) | Espírito de Comunidade: comer perto de outro jogador compartilha 10 de saciedade, uma vez por refeição | Estandarte dos Errantes: 20 s de stamina e resistência a interrupção para aliados em 25 m (90 s) |
| Aurídeos (portadores da alvorada) | Olhar Crepuscular: reconhecem contornos próximos com pouca luz | Aurora Errante: orbe que ilumina 35 m por 30 s e pode ser lançado a até 100 m (70 s) |
| Graníticos (filhos da montanha) | Raiz de Pedra: parados preparando uma ação, sofrem 20% menos deslocamento por empurrões | Ressonância Ancestral: 3 pulsos em 6 s, raio de 30 m, desequilibram inimigos comuns (75 s) |
| Cinéreos (herdeiros da brasa) | Corpo de Fornalha: 15% menos dano de queimadura | Sopro de Cinzas: faixa de 25 m por 8 m por 8 s, inflamável (65 s) |
| Silvanos (guardiões das trilhas) | Leitor de Rastros: pegadas de criaturas estudadas ficam mais visíveis | Chamado da Clareira: figura sonora e luminosa a até 60 m por 12 s atrai criaturas (60 s) |

O Humano provisório virou **Valdren** (decisão do Gabriel, 2026-10-11; mesmo valor `1` no banco). Cada raça tem **um** modelo (o board fala em 1 ou 2; um basta por ora).

## Nível, atributos e rotina (proposta do board, 2026-10-11)

Do "Livro de ideias de Asterra" ([seções 10 e 09](../brainstorm/figjam.md#10--regras-da-primeira-versão)). **Proposta do board (2026-10-11), não validada pelo grupo**, salvo o nível máximo, decidido na [fatia](fatia-jogavel.md#6-experiência-e-nível). Os atributos seguem [Atributos do personagem](#atributos-do-personagem).

- **Nível do personagem** é separado do nível e da raridade dos itens. O teto é **100**. ~~Subir de nível não dá ponto de atributo.~~ Revisto em 2026-10-12: subir de nível dá pontos da árvore, a única fonte deles; quantos por nível está pendente ([talentos](talentos.md#em-aberto)).
- ~~**Cinco atributos**, só nos itens: Força, Destreza, Intelecto, Vitalidade e Espírito~~ (substituído em 2026-10-12 por [Atributos do personagem](#atributos-do-personagem): os atributos vêm da árvore, e DEC-001 separa os atributos dos itens).
- **Fontes de XP:** derrotar inimigos, explorar pontos novos, coletar, produzir receitas e concluir objetivos.
- **Skills não são aprendidas:** continuar dos itens. O orçamento de preparação segue fora desta fatia ([skills.md](skills.md#preparação-de-skills-proposta-do-board-2026-10-11)). Os atributos não mudam esse orçamento.
- **Rotina:** "três builds" de exemplo, bestiário (registro por observação, cervo jovem como primeiro companheiro) e o ciclo preparar, explorar, voltar: veja a [seção 09](../brainstorm/figjam.md#09--personagem-cooperação-e-rotina). O bestiário e a captura não têm funcionalidade dona ainda.
- **Ataques, defesa e recursos** (golpe leve e pesado por arma, sem parry, bloqueio por defesa contra potência, 100 de stamina e de mana): pertencem a [combate.md](combate.md); o board repete os valores da fatia.
- **Morte:** o board propõe 45 s caído, 3 s para levantar e 20% de vida, **iguais** à seção [Morte e caído](#morte-e-caído-issue-97). Novo: caído também pode ser levantado pela skill Hino do Recomeço ([skills.md](skills.md)).

## Ritmo de progressão

Decidido em 2026-10-12. A curva já está em `ConfigExperiencia`; o teste `RitmoDeProgressaoTestes` falha se a curva, o XP do mob de referência ou o ritmo saírem da meta.

**Meta de ritmo** (referência: jogador solo, 4 mobs por minuto, 20 XP cheios, mob do mesmo nível ou mais forte):

- Nível 1 para 2 em até 1,5 minuto.
- Cada nível dos 10 primeiros demora mais que o anterior e no máximo 10 minutos.
- Chegar ao nível 10 em 30 a 50 minutos.
- O teto 100 usa a mesma curva. Não há meta de minutos para o nível 100.

**Curva:** XP do nível = arredondar(50 × nível^1,2). Nível máximo: **100**. A curva dos primeiros níveis não muda. Era 100 × nível^1,7, e o nível 10 passava de 5 horas só de mobs do mesmo nível.

**O que o nível dá:** ~~nada de atributo. Vida, vigor, energia e dano crescem pelos itens.~~ Revisto em 2026-10-12: cada nível dá pontos da [árvore](talentos.md) via `PontosPorNivel`, e só o nível dá esses pontos (Gabriel). Cada nível dá 1 (99 no nível 100, decidido em 2026-10-16). O nível em si não dá bônus. Os atributos não mudam o orçamento de preparação de skills; esse orçamento continua fora desta fatia ([skills.md](skills.md#preparação-de-skills-proposta-do-board-2026-10-11)).

**XP por diferença de nível** (decidido em 2026-10-12): a diferença é o nível do jogador menos o nível do mob. 0 ou negativa paga 100%. Cada nível acima tira 20 pontos percentuais. 5 ou mais acima paga 0. Cada participante calcula a parcela com seu próprio nível e o nível informado na morte do inimigo; o percentual é aplicado ao XP base com arredondamento para baixo até XP inteiro. Se o contrato informar nível desconhecido (`0`), Personagem concede o XP base sem penalidade, pois não há dado para calcular a diferença. Quem participou recebe essa parcela, não o valor cheio. Ganho zero consome a morte contra duplicatas, mas não grava progresso nem publica notificação de XP. A meta de ritmo acima vale contra mob do mesmo nível ou mais forte. O Javali do Campo é nível 3: a partir do nível 8 do jogador ele não dá XP. Constantes nomeadas, fáceis de calibrar.

O teto 100 e os pontos da árvore por nível (1) estão em `ConfigExperiencia`. A #454 ainda aplica o XP por diferença de nível. Os pontos gastam na [árvore de atributos](talentos.md); a rota antiga de gastar pontos nos atributos saiu na #625.

## Em aberto

- [x] Conflito livro x docs: o board pede **sem perda nenhuma na morte** (nem durabilidade) e esta doc já prevê isso só para a fatia; confirmar se vale para o jogo todo. Decidido por Gabriel em 2026-10-16: vale para o jogo todo, PvP incluído; a única perda é a durabilidade pelo uso ([itens](itens.md#definido)).
- [x] ~~O orçamento de skills muda com os atributos?~~ Decidido em 2026-10-12: não muda. O orçamento em si continua fora desta fatia.
- [x] Quais raças existem e qual a skill base de cada uma? Cinco raças em uso desde 2026-10-11 ([Raças propostas](#raças-propostas-no-board-2026-10-11)). Decidido em 2026-10-16: sem skill base; raça é só visual.
- [x] ~~Quais raças e classes existem?~~ classes não existem mais (D2)
- [x] ~~O que o nível dá ao personagem?~~ ~~Decidido em 2026-10-12: nenhum ponto de atributo; os atributos vêm dos itens.~~ Revisto em 2026-10-12 (Gabriel): pontos da árvore, só por nível. Quantos por nível na [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627).
- [x] ~~Aparência e customização na criação (rosto, corpo, cores...)?~~ Decidido na #182: customização básica com tom de pele (4 opções), cabelo (4 estilos) e cor de cabelo (5 opções). Servidor valida limites e persiste; client aplica prévia e material.
- [x] ~~Excluir personagem: pode? Tem confirmação (ex.: digitar o nome) ou tempo de espera antes de apagar de vez?~~ Decidido na #182: exclusão com confirmação digitando o nome exato. Nome fica reservado para a mesma conta por 7 dias, bloqueando outras contas.
- [x] Renomear personagem: pode? Tem custo ou limite? Decidido em 2026-10-16: não pode.
- [x] Decidido em 2026-10-16 (quiz de definições 2): não; só A–Z. Aceitar letras acentuadas no nome (ç, ã, é...) no futuro? Hoje o grupo decidiu só A–Z (veja [Regras](#regras))
- [x] Morte e caído (#97), decisões do grupo (regras em proposta em [Morte e caído](#morte-e-caído-issue-97)):
  - [x] A contagem de 45 s **para enquanto um aliado está levantando** (proposta) ou continua correndo? O servidor (#423) implementou **continua correndo**; trocar é mudança em `ControleDeQueda`. Decidido em 2026-10-16: **para**.
  - [x] "Voltar à cidade" antes do fim da contagem: **pode, depois de 5 s caído**; ao fim dos 45 s volta sozinho; voltar = vida cheia no ponto de nascimento da cidade, sem perder nada. **Decidido em 2026-10-11 (coordenador, autonomia dada pelo Gabriel)**
  - [x] Depois de levantar, **2 s sem receber dano** (proposta), para não cair de novo no mesmo golpe? Decidido em 2026-10-16: sim.
  - [x] Inimigos ignoram quem está caído (não atacam, não contam como alvo); o caído não ataca nem esquiva. **Decidido em 2026-10-11 (coordenador, autonomia dada pelo Gabriel)**
  - [x] Levantar: aliado do mesmo grupo a até 2 m segura a interação por 3 s e o caído volta com 20% da vida máxima; dano recebido pelo ajudante interrompe; dois ajudantes não duplicam (só um levanta por vez, o segundo é recusado); fim do tempo e levantar ao mesmo tempo resolvem uma única saída. **Decidido em 2026-10-11 (coordenador, autonomia dada pelo Gabriel)**
  - [x] Quem voltou à cidade pode **entrar de novo na mesma dungeon** enquanto o grupo ainda luta? (decisão da dungeon, #66) Decidido em 2026-10-16: pode ([instâncias](instancias.md#definido)).
  - [x] Cair tem algum custo depois da fatia (durabilidade, XP, tempo de espera)? Na fatia não tem perda nenhuma Decidido em 2026-10-16: não; a durabilidade cai só pelo uso.
  - [x] Qual funcionalidade é dona do estado caído no código: Personagem (como a issue #97) ou Combate (que já é dono da vida)? Decisão técnica, vira contrato. **Decidido pelo Gabriel em 2026-10-11: Combate** é o dono do estado caído; Personagem só mostra a tela (issue #125)

## Regras

**Base:** World of Warcraft (tela de seleção de personagem). **Igual:** a conta tem uma lista de personagens; a tela mostra a lista, um botão para criar e um para entrar; a criação pede nome e raça. **Diferente:** não se escolhe classe (D2); são cinco raças, cada uma com um modelo, e não há customização de aparência; o limite é bem menor (4 por conta).

### Nome do personagem

| Regra | Valor | Quem confere |
|---|---|---|
| Tamanho | 3 a 16 caracteres | client (retorno rápido) e servidor (decide) |
| Caracteres | só letras de A a Z, maiúsculas ou minúsculas; nada de espaço, número, acento ou símbolo | client e servidor |
| Unicidade | único em todo o servidor, sem diferenciar maiúsculas ("Ana", "ana" e "ANA" são o mesmo nome) | só o servidor |

- O nome é guardado como a pessoa digitou (mantém as maiúsculas); só a comparação de unicidade ignora maiúsculas.
- O servidor não corrige o nome (não tira espaços, não troca letras): se não estiver no formato, recusa.
- Client e servidor usam **o mesmo validador do shared**, `RegrasPersonagem.ValidarNome`, para tamanho e caracteres. Assim os dois nunca discordam sobre o formato.
- Letras acentuadas não são aceitas (decisão do grupo). Aceitar no futuro é uma possível mudança, em [Em aberto](#em-aberto).
- A unicidade vale **mesmo com pedidos simultâneos** (duas pessoas criando "Ana" ao mesmo tempo): o servidor garante com um índice único no banco sobre o nome normalizado (em minúsculas). Se o índice recusar, a resposta é "nome em uso".

### Raça

- Aceitas: **Valdren, Aurídeos, Graníticos, Cinéreos e Silvanos** (enum `Raca`: 1 a 5). Qualquer outro valor é recusado pelo servidor. ~~Única raça aceita por enquanto: Humano.~~ substituído em 2026-10-11.
- Na tela de criação, a primeira raça (Valdren) já vem escolhida; ao escolher outra, um cartão mostra o povo, a aparência, o traço, a skill racial e a frase de seleção (textos de `DadosDasRacas`, do board). O jogador entra no mundo com o modelo da raça do personagem.
- A skill base da raça ainda não existe (depende de [Skills](skills.md) e das raças definitivas).

### Limite por conta

- No máximo **4 personagens por conta**. O pedido de criar um 5º é recusado pelo servidor.
- O client pode desativar o botão Criar quando a conta já tem 4, mas quem garante o limite é o servidor.
- O limite vale **mesmo com pedidos simultâneos** (dois cliques em Confirmar, duas janelas abertas): o servidor conta os personagens da conta e cria o novo **dentro da mesma transação**, para nunca passar de 4.

### Personagem novo

- Começa no **nível 1**.
- Pertence à conta logada. O servidor descobre a conta pelo token (JWT), **nunca** por um campo enviado pelo client.

### Fluxo de entrada no jogo

~~Ordem das telas: abertura/carregamento → login (ou registro) → seleção de personagem ⇄ criação de personagem.~~ substituído em 2026-10-11 por: abertura/carregamento → login (ou registro), ou direto para a seleção se a sessão gravada ainda valer → seleção de personagem ⇄ criação de personagem.

| Tela | Dona |
|---|---|
| Abertura/carregamento | Core |
| Login ou registro de conta | Core |
| Seleção de personagem | Personagem |
| Criação de personagem | Personagem |

1. **Abertura/carregamento:** tela inicial do jogo (Core).
2. **Login ou registro de conta:** nome de usuário + senha (Core). Ao logar, o client recebe o token (JWT) usado nas chamadas abaixo e o grava neste computador. Nas aberturas seguintes o login é pulado, até **Sair** (nesta tela ou no menu do Esc, "Sair da conta"), que apaga o token gravado.
3. **Seleção de personagem:**
   - ao abrir, pede a lista de personagens da conta ao servidor (`GET`);
   - se o pedido falhar por **não autenticado** (401), volta para o login;
   - se falhar por **qualquer outro motivo** (ex.: servidor fora do ar, sem internet), mostra um erro genérico ("Não foi possível carregar os personagens.") com o botão **Tentar de novo**, que repete o pedido;
   - mostra cada personagem com **nome, raça e nível**, em **ordem de criação** (Id crescente: o mais antigo primeiro);
   - ao abrir a tela, vem selecionado o **primeiro personagem da lista**; se a lista estiver vazia, nenhum fica selecionado e ~~só aparece o botão Criar~~ substituído em 2026-10-11 por: aparecem só os botões **Criar** e **Sair** (Sair para não prender a pessoa na tela);
   - botão **Criar**: abre a tela de criação; com **4 personagens** fica desabilitado e a tela mostra o aviso "Você já tem o máximo de 4 personagens." (quem garante o limite continua sendo o servidor);
   - botão **Sair**: encerra a sessão e volta para o login;
   - botão **Entrar**: só funciona com um personagem selecionado. ~~**Por enquanto não leva a um mundo** (ainda não existe): mostra a mensagem "O jogo ainda não foi implementado." e continua na seleção. Não chama o servidor.~~ substituído em 2026-10-11 por: chama `EntradaNoJogo.Entrar(id)` (Core, veja [Entrada no mundo](../arquitetura.md#entrada-no-mundo)) com o id do personagem selecionado e **deixa a seleção**: o jogador entra no mundo com o visual humano ([Visual do jogador](#visual-do-jogador-arte)). Não chama o servidor. Sem personagem selecionado, não faz nada.
4. **Criação de personagem:**
   - campo de **nome** e escolha de **raça** (lista das cinco raças com o cartão da escolhida);
   - enquanto a pessoa digita, o client confere o formato do nome com `RegrasPersonagem.ValidarNome` e avisa se estiver errado, sem chamar o servidor;
   - botão **Confirmar**: envia nome e raça ao servidor;
     - **deu certo:** volta para a seleção, com a lista atualizada e o personagem novo selecionado;
     - **foi recusado:** continua na criação e mostra o motivo (veja abaixo);
   - botão **Voltar**: volta para a seleção sem criar.

### Motivos de recusa na criação

O servidor recusa e diz o motivo, para o client mostrar uma mensagem clara:

| Motivo | Quando | Mensagem sugerida no client |
|---|---|---|
| Nome inválido | fora do formato (tamanho ou caracteres) | "O nome deve ter de 3 a 16 letras (A–Z, sem acento, espaço ou número)." |
| Raça inválida | raça fora das cinco do enum | "Raça inválida." |
| Aparência inválida | opções de aparência fora das faixas válidas | "Opções de aparência inválidas." |
| Limite atingido | a conta já tem 4 personagens | "Você já tem o máximo de 4 personagens." |
| Nome em uso | já existe personagem com esse nome (ou nome reservado por outra conta), sem diferenciar maiúsculas | "Esse nome já está em uso." |
| Não autenticado | sem token ou token inválido/expirado | volta para o login (Core) |

Se mais de um motivo vale ao mesmo tempo, o servidor responde só o primeiro, nesta ordem: nome inválido, raça inválida, aparência inválida, limite atingido, nome em uso.

O formato exato dessas respostas (códigos e campos) é definido no contrato do shared (veja [Expõe](#expõe)).

### Aparência básica do personagem (#182)

Customização visual inicial definida na [#182](https://github.com/GabrielNied/jogo_do_grupo/issues/182) para dar identidade aos personagens na criação e seleção:

- **Tom de pele:** 4 opções (0 a 3: Padrão, Claro, Bronze, Escuro).
- **Estilo de cabelo:** 4 estilos (0 a 3: Curto, Trançado, Longo, Raspado).
- **Cor de cabelo:** 5 opções (0 a 4: Castanho, Preto, Loiro, Ruivo, Prateado).
- **Validação:** `RegrasPersonagem.ValidarAparencia` no shared e no servidor. Se algum campo for menor que 0 ou igual/maior que o total permitido, a criação é recusada com 400 (`CodigosErroPersonagem.AparenciaInvalida`).
- **Visual:** no client, `AplicadorAparencia` aplica a tonalidade de pele via `MaterialPropertyBlock` (`_BaseColor`) na prévia 3D e no visual no mundo sem modificar materiais compartilhados.

### Exclusão de personagem e reserva de nome (#182)

Regras de exclusão definidas na [#182](https://github.com/GabrielNied/jogo_do_grupo/issues/182):

- **Confirmação por nome:** na tela de seleção, o botão "Excluir personagem" abre diálogo solicitando digitar o nome exato do personagem.
- **Validação de confirmação:** client e servidor usam `RegrasPersonagem.ConfirmacaoDeNomeConfere`. Se não bater com o nome do personagem, a requisição é recusada com 400 (`CodigosErroPersonagem.ConfirmacaoDeNomeIncorreta`).
- **Autorização:** apenas o dono (conta vinculada ao token JWT) pode excluir o personagem. Se não pertencer à conta, retorna 404 (`CodigosErroPersonagem.PersonagemNaoEncontrado`).
- **Reserva de nome por 7 dias:** ao excluir o personagem, o nome entra na tabela de `NomesReservados` com expiração de 7 dias (`RegrasPersonagem.DiasDeReservaDeNome = 7`):
  - Durante esse período, **outras contas são bloqueadas** de criar um personagem com esse nome (retorna 409 `NomeJaExiste`).
  - A **mesma conta** que excluiu pode recriar o personagem com esse nome a qualquer momento, consumindo a reserva.
  - Após 7 dias, a reserva expira e o nome volta a ficar disponível para qualquer conta.

### Morte e caído (issue #97)

As regras base já estão em [fatia-jogavel.md](fatia-jogavel.md), seção 7, e não são repetidas aqui: caído por **45 s**, aliado levanta com **3 s** de interação, volta com **20%** de vida, dano no ajudante interrompe, fim do tempo leva ao hub **sem perdas**. Esta seção detalha a **tela**, os casos de borda e o retorno. Tudo abaixo é **proposta**.

**Base:** New World (o personagem fica caído e um aliado o levanta) e World of Warcraft (voltar para um ponto seguro), veja [referências](../referencias.md). **Igual:** aliado levanta segurando a tecla de interagir; quem não é levantado volta para a cidade. **Diferente:** não há perda de item, de durabilidade nem de XP, e não existe correr como fantasma até o corpo.

Quem decide: **servidor** (quando cai, a contagem, quem está levantando, a vida ao levantar e onde reaparece). O client envia três intenções: **começar a levantar** um aliado, **cancelar** e **voltar à cidade**.

#### Estados

| Estado | Entra quando | Sai para |
|---|---|---|
| Vivo | vida acima de 0 | Caído, quando a vida chega a 0 |
| Caído | vida chegou a 0 | Levantando (um aliado começou a interação); Retorno (a contagem acabou, a pessoa escolheu voltar ou o grupo todo caiu) |
| Levantando | um aliado segura a interação | Vivo com 20% de vida (os 3 s se completaram); Caído (a interação foi interrompida) |
| Retorno | fim da contagem, escolha da pessoa ou derrota do grupo | Vivo, na cidade, com vida, stamina e mana cheios |

#### Enquanto está caído

- Não anda, não ataca, não esquiva, não interage e não usa itens: o servidor recusa essas intenções com `VoceEstaCaido`. A câmera continua girando e o chat e o mapa funcionam.
- Não recebe dano nem efeitos, e os inimigos **trocam de alvo** (deixam de perseguir quem caiu).
- Stamina e mana ficam como estavam e não regeneram.
- A contagem é do servidor. O client mostra o tempo que o servidor enviou e não decide o fim.
- **Voltar à cidade:** liberado depois de **5 s** caído (evita clique sem querer), segurando o botão por **1 s**. Antes disso: `AindaNaoPodeVoltar`.
- **Desconectar caído:** a contagem continua no servidor. Se a pessoa voltar depois do fim, aparece na cidade.

#### Levantar um aliado

- Quem pode: na dungeon, um jogador **vivo do mesmo grupo** (validado por `IConsultaDeGrupo`), da mesma instância, a até **2 m** do caído, segurando **Interagir** (E, veja [teclas](interface.md#teclas-e-atalhos)) por 3 s. Na dungeon, jogador sem grupo ou de outro grupo recebe `NaoSaoDoMesmoGrupo` (#832). Fora da dungeon, desde 2026-10-16 (aplicado no servidor na #1204), qualquer jogador vivo pode levantar (veja [Retorno à cidade](#retorno-à-cidade)).
- Interrompe e **zera o progresso**: soltar a tecla, afastar-se mais de 2 m, levar dano, cair, ou atacar e esquivar.
- **Um ajudante por vez:** o primeiro que começou vale; o segundo recebe `JaEstaSendoLevantado` (é a regra da fatia: dois ajudantes não duplicam a cura).
- A contagem de 45 s **pausa** enquanto alguém está levantando e volta a correr se a interação for interrompida (#895).
- Ao levantar: **20% da vida máxima**, stamina e mana como estavam, no mesmo lugar, com **2 s sem receber dano** (`RegrasQueda.ImunidadeAposLevantarSegundos = 2.0`, #895).
- Não há limite de quantas vezes alguém pode cair e ser levantado na mesma dungeon (na fatia).

#### Retorno à cidade

- **Mundo aberto:** reaparece na **cidade mais próxima** (hoje só Brasa Mansa; ver [mundo](mundo.md#definido)), com vida, stamina e mana cheios.
- **Dungeon:** reaparece na **entrada/safe zone da instância**, com vida, stamina e mana cheios, sem sair do grupo (quiz 3, 2026-10-16).
- **Fora da dungeon** (decidido por Gabriel em 2026-10-16; retorno refinado no quiz 3): quem não é levantado volta à **cidade mais próxima** ([mundo](mundo.md#definido)). No mundo aberto **qualquer jogador vivo** pode levantar, não só o do grupo; na dungeon continua só o grupo.
- **Na dungeon** (quiz 3): retorno manual ou fim dos 45 s leva à **entrada da instância** (safe zone), com vida cheia, sem sair do grupo — ~~hub da cidade~~ substituído em 2026-10-16 (quiz 3) pela entrada da instância.
- Não perde item, XP, moeda nem nível. O loot que já era dela continua no chão da instância pelo tempo normal ([fatia-jogavel.md](fatia-jogavel.md), seção 5).
- Continua **membro do grupo** ([social.md](social.md)); só sai da instância. Pode voltar a entrar na mesma dungeon enquanto ela estiver aberta (decidido em 2026-10-16, [instâncias](instancias.md#definido)).
- **Todos caídos ao mesmo tempo** (inclusive quem joga sozinho): é a derrota da fatia. Não espera os 45 s: a tela de derrota aparece por **5 s** e todos voltam à cidade; a instância fecha.

#### Na tela

| Quem vê | O que aparece |
|---|---|
| Quem caiu | tela escurecida e sem cor; "Você caiu"; a contagem em segundos, grande; "Aguardando um aliado" ou "Ana está levantando você" com a barra de 3 s; o botão **Voltar à cidade** (apagado nos primeiros 5 s) |
| Aliados | marcador sobre o caído com a contagem, visível através de paredes até **40 m**; a linha dele na lista do grupo em estado caído ([HUD](interface.md)); mensagem de sistema "Ana caiu" |
| Aliado a até 2 m | "Segure E para levantar" |
| Quem está levantando | barra de 3 s no centro da tela; se for interrompido, o motivo ("Você levou dano", "Longe demais") |
| Todos, na derrota | "O grupo caiu" por 5 s, depois a cidade |

A música baixa enquanto a pessoa está caída (regra do áudio, em [interface.md](interface.md)).

#### Client da queda (#125)

O client de Personagem ouve `EstadoDeQueda` do contrato de Combate. Para o próprio personagem,
mostra a contagem recebida, o estado de espera ou de levantamento e o progresso informado pelo
servidor. A contagem local só interpola a exibição; o servidor decide quando a queda termina.
Depois de 5 s, a pessoa pode segurar o botão de retorno por 1 s; o client envia
`VoltarParaACidade`, e o servidor valida a ação. A tela desaparece quando chega `Vivo` ou `Retorno`.

O client atual não inicia o levantamento de outro personagem: o contrato aceita o id do alvo, mas
Personagem não recebe a seleção de alvo nem a presença espacial necessária para escolher um aliado.
Essa interação depende da integração de Mundo e deve ser ligada quando essa API existir. O estado
recebido ainda permite mostrar o progresso do aliado que está levantando o personagem local.

#### Erros que o servidor devolve (**proposta** de nomes)

| Código | Quando |
|---|---|
| `VoceEstaCaido` | quem está caído tenta andar, atacar, esquivar, interagir ou levantar alguém |
| `AlvoNaoEstaCaido` | tentar levantar quem está vivo ou já voltou à cidade |
| `LongeDemais` | começar a levantar a mais de 2 m |
| `JaEstaSendoLevantado` | outro aliado já está levantando aquele personagem |
| `NaoEstaCaido` | pedir "voltar à cidade" sem estar caído |
| `AindaNaoPodeVoltar` | pedir "voltar à cidade" antes de 5 s caído |

#### Testes esperados

Os da [fatia](fatia-jogavel.md) (seção 7) continuam valendo. A mais:

- Servidor, caminho feliz: vida 0 leva a caído; aliado completa 3 s e a pessoa volta com 20% da vida; fim dos 45 s leva à cidade com tudo cheio e sem perder item nem XP; quem volta continua no grupo.
- Servidor, erros: um teste por código da tabela, conferindo que nada muda no estado.
- Servidor, limites: levantar a 2,0 m vale e além disso é `LongeDemais`; "voltar à cidade" com 4,9 s é recusado e com 5 s vale; interromper com 2,9 s zera o progresso; a contagem parada durante a interação volta a correr do ponto em que parou; os 2 s sem dano depois de levantar acabam no tempo certo.
- Servidor, concorrência: dois ajudantes começam juntos e só um vale; o tempo acaba no mesmo instante em que a interação termina e sai **um** resultado só; o ajudante cai no meio e a interação é interrompida; o último jogador vivo cai enquanto levanta outro e o resultado é a derrota, uma vez só; "voltar à cidade" pedido no instante em que o aliado termina de levantar dá um resultado só.
- Client (EditMode): a tela mostra o tempo recebido do servidor; o botão de voltar fica apagado nos primeiros 5 s; a tela muda entre "aguardando" e "está levantando você"; a barra do ajudante some ao ser interrompida.

## Depende de

- **Core:** conta, login/registro e emissão do token (JWT). Personagem só lê qual é a conta a partir do token.
- **[Skills](skills.md):** a skill base de cada raça (quando as raças definitivas existirem).
- **Morte e caído:** vida e dano de [combate](combate.md) e [mobs](mobs.md) (#61, #63), grupo de [social](social.md), ponto de nascimento de [mundo](mundo.md) e a instância de [instâncias](instancias.md) (#66), sempre por evento ou contrato do shared.

## Expõe

API HTTP para listar e criar personagens (por que HTTP: [ADR 0009](../decisoes/0009-api-http-para-conta-e-personagens.md)). Todas as chamadas exigem o token (JWT) do login; sem ele, o servidor responde 401. Os tipos ficam em [shared/Package/Runtime/Features/Personagem/](../../shared/Package/Runtime/Features/Personagem/), usados pelo servidor e pelo client; o código do contrato é a fonte dos nomes e tipos exatos.

| Rota | O que faz | Envia | Recebe |
|---|---|---|---|
| `GET /api/personagens` | Lista os personagens da conta logada, em ordem de criação | nada (a conta vem do token) | `ListaPersonagensResponse`: a lista de `PersonagemResumo` |
| `POST /api/personagens` | Cria um personagem na conta logada | `CriarPersonagemRequest`: Nome, Raca e Aparencia | deu certo: **201** com o `PersonagemResumo` do personagem criado (já com o Id); recusado: o motivo (veja [Motivos de recusa](#motivos-de-recusa-na-criação)) |
| `POST /api/personagens/{personagemId}/excluir` | Exclui um personagem com confirmação do nome e reserva de 7 dias | `ExcluirPersonagemRequest`: ConfirmacaoNome | deu certo: **200 OK** com corpo vazio; recusado: 400 (`ConfirmacaoDeNomeIncorreta`) ou 404 (`PersonagemNaoEncontrado`) |

- **`PersonagemResumo`:** o que a seleção mostra de cada personagem: Id, Nome, Raca, Nivel e Aparencia.
- **`CriarPersonagemRequest`:** o que a criação envia: Nome, Raca e Aparencia.
- **`ExcluirPersonagemRequest`:** o que a exclusão envia: ConfirmacaoNome.
- **`AparenciaPersonagem`:** opções cosméticas básicas: TomDePele (0..3), Cabelo (0..3), CorDeCabelo (0..4).
- **`Raca`:** enum do shared (`Valdren`=1, `Aurideo`=2, `Granitico`=3, `Cinereo`=4, `Silvano`=5). No JSON trafega como **número**, não como texto.
- **`RegrasPersonagem`:** validadores de formato do nome (`ValidarNome`), de aparência (`ValidarAparencia`) e de confirmação de exclusão (`ConfirmacaoDeNomeConfere`).
- **`ListaPersonagensResponse`:** a lista de personagens da conta (de 0 a 4).
- **`CodigosErroPersonagem`:** os códigos dos motivos de recusa (`NomeInvalido`, `NomeJaExiste`, `LimitePersonagens`, `RacaInvalida`, `AparenciaInvalida`, `ConfirmacaoDeNomeIncorreta`, `PersonagemNaoEncontrado`), que chegam no campo `Codigo` do `ErroApi` do Core. Erros genéricos (não autenticado, requisição inválida, erro interno) usam os códigos do Core (`CodigosErro`).
- Por serem HTTP, essas rotas não usam IDs de mensagem da faixa 3000–3999.

### Experiência, nível e pontos de atributo (#65, contrato da #299)

Regras na [fatia jogável, seção 6](fatia-jogavel.md#6-experiência-e-nível). O servidor concede a experiência e sobe o nível; o client só mostra. Subir de nível dá pontos da árvore (decidido em 2026-10-12): `PontosPorNivel` em `ConfigExperiencia`, **1 por nível** (99 no nível 100, decidido em 2026-10-16). O saldo (`PontosLivres`) fica no personagem e gasta na compra de nós da [árvore](talentos.md) (#625, 13/10/2026), que substituiu a distribuição livre nos atributos. O servidor concede a experiência de `InimigoDerrotado`; o client mostra a barra e o saldo na janela do personagem (tecla C).

| ID ou rota | Nome | Direção | Campos |
|---|---|---|---|
| 3000 | `ExperienciaGanha` | servidor → dono | `Quantidade`, `XpAtual`, `XpDoNivel`, `Nivel`, `PontosLivres` (o estado inteiro depois do ganho) |
| 3001 | `SubiuDeNivel` | servidor → quem está na área | `PersonagemId`, `Nivel` (uma mensagem só, mesmo subindo vários níveis) |
| `GET /api/personagens/{personagemId}/progresso` | `RotasPersonagem.Progresso` | client → servidor | recebe `ProgressoResponse`: `Nivel`, `XpAtual`, `XpDoNivel`, `PontosLivres` (pontos da árvore ainda não gastos) |

- **Pontos da árvore (`IPontosDeNivel`, só no servidor, em `Contratos/Personagem/`):** Personagem guarda o saldo e oferece a Talentos ler (`ObterAsync`) e gastar (`TentarGastarAsync`, um `UPDATE` condicional que entra na transação do chamador). Quem muda `PontosLivres` para baixo é só Talentos, na compra de um nó. Código novo em `CodigosErroPersonagem`: `PersonagemNaoEncontrado` (404: não existe ou não é da conta do token).
- **Saiu na #625:** a rota `POST .../atributos`, `DistribuirAtributosRequest`, `PontosDeAtributos`, `RegrasAtributos`, os códigos `AtributoInvalido` e `PontosInsuficientes` e o campo `Distribuidos`. Quem já tinha pontos distribuídos nos cinco atributos (colunas `Forca`..`Espirito` do personagem) tem esses pontos **devolvidos como pontos livres** pela migração `TalentosArvoreDeAtributos` e recompra na árvore; nenhum ponto some. Não há pontos retroativos pelos níveis já ganhos.
- O client pede o `ProgressoResponse` ao entrar no mundo; depois disso a experiência chega por `ExperienciaGanha`.
- **`IDiretorioDePersonagens`** (só no servidor, em `server/src/JogoDoGrupo.Server/Contratos/Personagem/`, [ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)): consulta de nome ↔ id para as outras funcionalidades. `TentarObterPorNome` não diferencia maiúsculas; `TentarObterNome` devolve o nome como foi digitado. Diz se o personagem **existe**; se está online quem sabe é o Mundo (`IPosicoesDoMundo`).

## Visual do jogador (arte)

Cada raça tem **um modelo próprio** low-poly (humanoide de 1,8 m, gerado no Blender por script: [ADR 0025](../decisoes/0025-modelos-proprios-no-blender-por-script.md) e [padrão dos modelos](../arte/padrao-de-modelos.md#personagens-das-raças)), já com esqueleto e animações. O client só **mostra**; quem decide posição e estado é o servidor. O visual vem do identificador da raça (o valor do enum `Raca`).

- **Registro no Core:** `RegistroVisualJogador` (`Features/Personagem/Scripts/`) chama `EntradaNoJogo.DefinirVisualDoJogador` antes da primeira cena (`BeforeSceneLoad`) e devolve o prefab da raça do personagem (`Jogador_<Raca>`); o Core instancia. A seleção guarda a raça de cada personagem da lista (`LembrarRacas`); personagem fora da lista usa o Valdren.
- **Visual dos outros jogadores (#498):** o mesmo registro chama `EntradaNoJogo.DefinirVisualDeOutroJogador` com `CarregarVisualDaRaca(int raca)`: o Mundo pede o modelo pela raça que o servidor mandou, sem referenciar Personagem. Raça desconhecida ou sem prefab usa o prefab de reserva (`RacaPadrao`, Valdren); sem nenhum, o Core usa uma cápsula. A aparência dos outros não trafega: vale a do prefab.
- **Prefabs:** `client/Assets/_Game/Features/Personagem/Resources/Personagem/Jogador_<Raca>.prefab` com `<Raca>` = `Valdren`, `Aurideo`, `Granitico`, `Cinereo` ou `Silvano` (nome do enum, sem acento; carregar com `Resources.Load<GameObject>("Personagem/Jogador_Valdren")`). Altura de 1,8 m, pés em Y=0, frente em +Z. O `Animator` fica na raiz do prefab. Não tem `CharacterController`, câmera nem lógica: isso é do Core.
- **Aparência** (um acabamento por raça; texto das raças no [board](../brainstorm/figjam.md#01--raças-de-asterra)): Valdren, humano de pele morena, tecido grosso, couro e cachecol ocre; Aurídeos, esguios de pele dourada, olhos claros e marcas douradas que brilham, diadema de sol; Graníticos, os mais largos e baixos, pele de granito facetado com fissuras âmbar que brilham e ombros de pedra; Cinéreos, pele de cinza e carvão, olhos de brasa, fissuras vermelhas e placas de obsidiana; Silvanos, os mais esguios, orelhas alongadas, cabelo e coroa de folhas, saiote de folhas.
- **Geração:** os FBX e PNG saem do Blender (`arte/blender/personagens/`, comandos em [padrao-de-modelos.md](../arte/padrao-de-modelos.md#gerar-os-personagens)); material, controller e prefab saem do script de editor `Features/Personagem/Editor/GeradorJogadoresDasRacas.cs`, menu **Jogo do Grupo > Personagem > Gerar jogadores das raças** (`npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.GeradorJogadoresDasRacas.Gerar`). Rodar de novo recria o mesmo resultado. Não edite o controller e o prefab à mão.
- **Parâmetros do Animator** (usados pelo Core e, depois, pelo combate):

| Parâmetro | Tipo | Significado |
|---|---|---|
| `Velocidade` | float | velocidade horizontal em m/s |
| `NoChao` | bool | `true` quando está no chão (padrão `true`); ainda não muda nenhum estado |
| `Esquiva` | trigger | dispara o rolamento |
| `AtaqueLeve` | trigger | começa um golpe leve (camada `Golpe`); `Passo` escolhe qual dos três |
| `AtaquePesado` | trigger | começa o golpe pesado |
| `Passo` | int | passo da cadeia de leves (0 a 2); o segundo leve é espelhado |
| `VelocidadeDoGolpe` | float | velocidade do clipe `Atacar` (1 = a do clipe); Combate a calcula para o contato cair no fim do preparo |
| `Guarda` | bool | segura a pose de guarda |
| `Interrompido` | trigger | pose de dano (um acerto interrompeu a ação) |
| `Cancelar` | trigger | sai da pose do golpe sem dano (o servidor recusou o golpe) |
| `Caido` | bool | deita (`Morrer`, que fica no chão) e levanta ao voltar a `false` |

- **Estados:** na camada base, `Movimento` (blend tree 1D por `Velocidade`: parado 0 m/s, andar 2 m/s, correr 5,5 m/s, a velocidade de corrida do Core, com o clipe de correr a 1,1x), `Esquiva` (estado qualquer, por `Esquiva`; volta a `Movimento` perto do fim) e `Caido` (estado qualquer, por `Caido`). A camada `Golpe`, com a máscara `Jogador_<Raca>_Tronco.mask` (o osso `Tronco` e tudo abaixo dele: as pernas seguem o movimento), tem `Vazio` (padrão), `Leve1`, `Leve2` (espelhado), `Leve3` e `Pesado` (o clipe `Atacar`, na velocidade de `VelocidadeDoGolpe`), `Guarda` (pose do quadro de preparo do `Atacar`, parada, até existir o clipe de guarda) e `Dano` (o clipe `Dano`, a 1,56x para caber nos 0,3 s do atordoamento). O contato do `Atacar` é o quadro 11 de 26, a 30 por segundo. Nomes dos clipes: `Idle`, `Andar`, `Correr`, `Esquivar`, `Atacar`, `Morrer`, `Dano`. Os nomes dos parâmetros são o contrato com `Features/Combate/Scripts/AnimadorDeAtaque.cs` e `AnimadorDeDefesa.cs`; um teste EditMode de Combate confere que os cinco controllers os têm.
- **Esqueleto e encaixes:** `Raiz` > `Quadril` > `Tronco` > `Cabeca`; `Tronco` > `Braco_E/D` > `Antebraco_E/D` > `Mao_E/D`; `Quadril` > `Perna_E/D` > `Canela_E/D` > `Pe_E/D` (E = esquerda do personagem). Os encaixes são objetos vazios criados pelo gerador, com esses nomes fixos (uso do artista de itens): **`EncaixeMaoDireita`**, filho do osso `Weapon.R` (que fica na palma da mão direita), e **`EncaixeCostas`**, filho do osso `Costas` (que fica nas costas, no alto do tronco). Os dois começam sem rotação e sem escala; o artista de itens ajusta a pose da arma no próprio encaixe.
