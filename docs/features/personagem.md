# Personagem

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 3000–3999

## Resumo

Raças, atributos e progressão de nível. Sem classes: o estilo de jogo vem dos itens equipados.

## Definido

- Atributos, vida/vigor/energia, XP e nível da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- Raças com características únicas; cada raça dá uma **skill base** própria
- **Sem classes** (D2 da [validação de 2026-10-10](../brainstorm/validacao.md)): a combinação de itens define o estilo de jogo
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
- Comunicação pela API HTTP do [ADR 0009](../decisoes/0009-api-http-para-conta-e-personagens.md) (veja [Expõe](#expõe)).
- O **servidor decide**: valida o nome, a unicidade, a raça e o limite de 4. O client só confere o formato do nome para dar retorno rápido.

## Regras puras de experiência (#136)

`RegrasExperiencia` implementa a curva provisória da [fatia jogável, seção 6](fatia-jogavel.md#6-experiência-e-nível). Os ajustes ficam em [ConfigExperiencia](../../shared/Package/Runtime/Features/Personagem/ConfigExperiencia.cs): nível máximo, pontos por nível, base de XP e expoente da curva. Reduzir a base acelera a progressão; o expoente controla o crescimento do custo entre níveis.

`XpParaOProximoNivel` arredonda para o inteiro mais próximo (empates para longe de zero), inclusive ao consultar o nível máximo; nesse nível, o custo é apenas informativo. `AplicarGanho` recebe nível, XP dentro do nível e ganho; devolve nível, XP restante e pontos ganhos nessa aplicação. Pode subir vários níveis e guarda o excedente no teto. Ganho zero ou negativo preserva o estado; nível fora da faixa e XP acumulado negativo são recusados. XP usa `long`; soma acima da capacidade é recusada, sem truncamento.

`AplicarAjuste` existe só para o comando de teste `xp` ([Desenvolvimento](desenvolvimento.md)). A quantidade pode ser negativa: desce de nível, devolve a barra do nível anterior e tira `PontosPorNivel` dos pontos livres por nível perdido, sem passar de zero e sem desfazer pontos já gastos.

Estas funções não concedem prêmios nem persistem dados. Integração com servidor, mensagens, barra de XP e prevenção de prêmio duplicado continuam na #65. O padrão para outros valores ajustáveis está nas [convenções](../convencoes.md#valores-de-balanceamento).

## Raças propostas no board (2026-10-11)

O "Livro de ideias de Asterra" do FigJam propõe **cinco povos jogáveis**. Cada raça dá um **traço** e uma **skill racial**; armas e equipamentos continuam livres para todos, e cada raça tem 1 ou 2 modelos iniciais. O board avisa que nomes e números novos **continuam propostas**: **implementadas no servidor, na criação e no visual** desde 2026-10-11 (só o traço e a skill racial ainda não: [Skills](skills.md), #341) ([Raça](#raça)). Texto completo de cada raça (identidade, aparência, combinações): [figjam.md](../brainstorm/figjam.md#01--raças-de-asterra).

| Raça | Traço | Skill racial (recarga) |
|---|---|---|
| Valdren (humanos da fronteira) | Espírito de Comunidade: comer perto de outro jogador compartilha 10 de saciedade, uma vez por refeição | Estandarte dos Errantes: 20 s de vigor e resistência a interrupção para aliados em 25 m (90 s) |
| Aurídeos (portadores da alvorada) | Olhar Crepuscular: reconhecem contornos próximos com pouca luz | Aurora Errante: orbe que ilumina 35 m por 30 s e pode ser lançado a até 100 m (70 s) |
| Graníticos (filhos da montanha) | Raiz de Pedra: parados preparando uma ação, sofrem 20% menos deslocamento por empurrões | Ressonância Ancestral: 3 pulsos em 6 s, raio de 30 m, desequilibram inimigos comuns (75 s) |
| Cinéreos (herdeiros da brasa) | Corpo de Fornalha: 15% menos dano de queimadura | Sopro de Cinzas: faixa de 25 m por 8 m por 8 s, inflamável (65 s) |
| Silvanos (guardiões das trilhas) | Leitor de Rastros: pegadas de criaturas estudadas ficam mais visíveis | Chamado da Clareira: figura sonora e luminosa a até 60 m por 12 s atrai criaturas (60 s) |

O Humano provisório virou **Valdren** (decisão do Gabriel, 2026-10-11; mesmo valor `1` no banco). Cada raça tem **um** modelo (o board fala em 1 ou 2; um basta por ora).

## Nível, atributos e rotina (proposta do board, 2026-10-11)

Do "Livro de ideias de Asterra" ([seções 10 e 09](../brainstorm/figjam.md#10--regras-da-primeira-versão)). **Proposta do board (2026-10-11), não validada pelo grupo**; nada disto está implementado e a curva provisória continua a da [fatia](fatia-jogavel.md#6-experiência-e-nível).

- **Nível do personagem** é separado do nível e da raridade dos itens. Proposta: níveis 1 a 30 no MVP (igual à fatia), **5 pontos de atributo por nível**.
- **Cinco atributos:** Força (golpes físicos pesados), Destreza (ataques ágeis e arco), Intelecto (magia), Vitalidade (vida e defesa), Espírito (cura, energia e apoio). Os efeitos numéricos ficam para [combate.md](combate.md) e [itens](itens.md).
- **Fontes de XP:** derrotar inimigos, explorar pontos novos, coletar, produzir receitas e concluir objetivos.
- **Skills não são aprendidas:** subir de nível mexe em atributos e no **orçamento de preparação** (12 pontos iniciais, +1 a cada 5 níveis); as skills continuam dos itens ([skills.md](skills.md#preparação-de-skills-proposta-do-board-2026-10-11)).
- **Rotina:** "três builds" de exemplo, bestiário (registro por observação, cervo jovem como primeiro companheiro) e o ciclo preparar, explorar, voltar: veja a [seção 09](../brainstorm/figjam.md#09--personagem-cooperação-e-rotina). O bestiário e a captura não têm funcionalidade dona ainda.
- **Ataques, defesa e recursos** (golpe leve e pesado por arma, sem parry, bloqueio por defesa contra potência, 100 de vigor e de energia): pertencem a [combate.md](combate.md); o board repete os valores da fatia.
- **Morte:** o board propõe 45 s caído, 3 s para levantar e 20% de vida, **iguais** à seção [Morte e caído](#morte-e-caído-issue-97). Novo: caído também pode ser levantado pela skill Hino do Recomeço ([skills.md](skills.md)).

## Ritmo de progressão (proposta, #454)

**Proposta, não validada pelo grupo.** Parte A da #454; o XP por diferença de nível do mob (depende da #452) e o client ficam para a parte B.

**Meta de ritmo** (referência: jogador solo, 4 mobs comuns por minuto, 20 XP por mob como o Javali do Campo):

- Nível 1 para 2 em até 1,5 minuto (hoje ~0,6).
- Cada nível dos 10 primeiros demora mais que o anterior e no máximo 10 minutos (hoje de 0,6 a ~8,7).
- Chegar ao nível 10 em 30 a 50 minutos (hoje ~40).
- Nível 30 sai de ~37 minutos por nível; o jogo todo pede dezenas de horas.

**Curva calibrada:** XP do nível = arredondar(50 x nível^1,2) (era 100 x nível^1,7, lenta demais: nível 10 em mais de 5 horas de mobs). Os valores e a meta estão em `ConfigExperiencia`; o teste `RitmoDeProgressaoTestes` falha se a curva, o XP do mob de referência ou o ritmo saírem da meta. Mexeu na curva ou no XP do mob? Reconfira o teste.

**O que o nível dá (proposta):** 5 pontos de atributo por nível (já implementado). Vida, vigor e energia crescem só pelos pontos gastos (Vitalidade, Espírito) e pelos itens: o nível em si não dá bônus automático, para o poder vir da escolha e do item. Subir de nível também libera o orçamento de preparação de skills (+1 a cada 5 níveis, [skills.md](skills.md)).

**Destreza e Intelecto:** ainda sem efeito (Combate só usa Força, Vitalidade e Espírito). Seguem na distribuição para não quebrar contrato e client; o jogador pode gastar neles sem ganho até Combate e Itens (#166) dar uso. **Decisão pendente do grupo:** manter assim ou recusar esses dois atributos até terem uso.

## Em aberto

- [ ] Conflito livro x docs: o board pede **sem perda nenhuma na morte** (nem durabilidade) e esta doc já prevê isso só para a fatia; confirmar se vale para o jogo todo.
- [ ] Conflito livro x docs: o board dá o **orçamento de skills de 12 pontos** com o nível (+1 a cada 5), mas ainda não diz se os atributos mudam o orçamento. Validar com o grupo.
- [ ] Quais raças existem e qual a skill base de cada uma? (as raças definitivas substituem a raça provisória Humano) **Cinco raças em uso desde 2026-10-11 ([Raças propostas](#raças-propostas-no-board-2026-10-11)); a skill base de cada uma segue em [Skills](skills.md) (#341).**
- [ ] ~~Quais raças e classes existem?~~ classes não existem mais (D2)
- [ ] O que o nível dá ao personagem (atributos, espaços de skill, acesso a itens)? Proposta em [Ritmo de progressão](#ritmo-de-progressão-proposta-454).
- [ ] Nível máximo e curva de progressão (proposta calibrada em [Ritmo de progressão](#ritmo-de-progressão-proposta-454))
- [ ] Aparência e customização na criação (rosto, corpo, cores...)?
- [ ] Excluir personagem: pode? Tem confirmação (ex.: digitar o nome) ou tempo de espera antes de apagar de vez?
- [ ] Renomear personagem: pode? Tem custo ou limite?
- [ ] Aceitar letras acentuadas no nome (ç, ã, é...) no futuro? Hoje o grupo decidiu só A–Z (veja [Regras](#regras))
- [ ] Morte e caído (#97), decisões do grupo (regras em proposta em [Morte e caído](#morte-e-caído-issue-97)):
  - [ ] A contagem de 45 s **para enquanto um aliado está levantando** (proposta) ou continua correndo? O servidor (#423) implementou **continua correndo** (mais simples e é o que a fatia diz); trocar é mudança em `ControleDeQueda`
  - [x] "Voltar à cidade" antes do fim da contagem: **pode, depois de 5 s caído**; ao fim dos 45 s volta sozinho; voltar = vida cheia no ponto de nascimento da cidade, sem perder nada. **Decidido em 2026-10-11 (coordenador, autonomia dada pelo Gabriel)**
  - [ ] Depois de levantar, **2 s sem receber dano** (proposta), para não cair de novo no mesmo golpe?
  - [x] Inimigos ignoram quem está caído (não atacam, não contam como alvo); o caído não ataca nem esquiva. **Decidido em 2026-10-11 (coordenador, autonomia dada pelo Gabriel)**
  - [x] Levantar: aliado do mesmo grupo a até 2 m segura a interação por 3 s e o caído volta com 20% da vida máxima; dano recebido pelo ajudante interrompe; dois ajudantes não duplicam (só um levanta por vez, o segundo é recusado); fim do tempo e levantar ao mesmo tempo resolvem uma única saída. **Decidido em 2026-10-11 (coordenador, autonomia dada pelo Gabriel)**
  - [ ] Quem voltou à cidade pode **entrar de novo na mesma dungeon** enquanto o grupo ainda luta? (decisão da dungeon, #66)
  - [ ] Cair tem algum custo depois da fatia (durabilidade, XP, tempo de espera)? Na fatia não tem perda nenhuma
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
| Nome em uso | já existe personagem com esse nome, sem diferenciar maiúsculas | "Esse nome já está em uso." |
| Raça inválida | raça fora das cinco do enum | "Raça inválida." |
| Limite atingido | a conta já tem 4 personagens | "Você já tem o máximo de 4 personagens." |
| Não autenticado | sem token ou token inválido/expirado | volta para o login (Core) |

Se mais de um motivo vale ao mesmo tempo, o servidor responde só o primeiro, nesta ordem: nome inválido, raça inválida, limite atingido, nome em uso.

O formato exato dessas respostas (códigos e campos) é definido no contrato do shared (veja [Expõe](#expõe)).

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
| Retorno | fim da contagem, escolha da pessoa ou derrota do grupo | Vivo, na cidade, com vida, vigor e energia cheios |

#### Enquanto está caído

- Não anda, não ataca, não esquiva, não interage e não usa itens: o servidor recusa essas intenções com `VoceEstaCaido`. A câmera continua girando e o chat e o mapa funcionam.
- Não recebe dano nem efeitos, e os inimigos **trocam de alvo** (deixam de perseguir quem caiu).
- Vigor e energia ficam como estavam e não regeneram.
- A contagem é do servidor. O client mostra o tempo que o servidor enviou e não decide o fim.
- **Voltar à cidade:** liberado depois de **5 s** caído (evita clique sem querer), segurando o botão por **1 s**. Antes disso: `AindaNaoPodeVoltar`.
- **Desconectar caído:** a contagem continua no servidor. Se a pessoa voltar depois do fim, aparece na cidade.

#### Levantar um aliado

- Quem pode: um jogador **vivo** da mesma instância, a até **2 m** do caído, segurando **Interagir** (E, veja [teclas](interface.md#teclas-e-atalhos)) por 3 s.
- Interrompe e **zera o progresso**: soltar a tecla, afastar-se mais de 2 m, levar dano, cair, ou atacar e esquivar.
- **Um ajudante por vez:** o primeiro que começou vale; o segundo recebe `JaEstaSendoLevantado` (é a regra da fatia: dois ajudantes não duplicam a cura).
- A contagem de 45 s **para** enquanto alguém está levantando e volta a correr se a interação for interrompida (em aberto).
- Ao levantar: **20% da vida máxima**, vigor e energia como estavam, no mesmo lugar, com **2 s sem receber dano** (em aberto).
- Não há limite de quantas vezes alguém pode cair e ser levantado na mesma dungeon (na fatia).

#### Retorno à cidade

- A pessoa reaparece no **ponto de nascimento da cidade** ([mundo.md](mundo.md)), com vida, vigor e energia cheios.
- Não perde item, XP, moeda nem nível. O loot que já era dela continua no chão da instância pelo tempo normal ([fatia-jogavel.md](fatia-jogavel.md), seção 5).
- Continua **membro do grupo** ([social.md](social.md)); só sai da instância. Voltar a entrar na mesma dungeon é decisão de [instâncias](instancias.md) (#66).
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
| `POST /api/personagens` | Cria um personagem na conta logada | `CriarPersonagemRequest`: Nome e Raca | deu certo: **201** com o `PersonagemResumo` do personagem criado (já com o Id); recusado: o motivo (veja [Motivos de recusa](#motivos-de-recusa-na-criação)) |

- **`PersonagemResumo`:** o que a seleção mostra de cada personagem: Id, Nome, Raca e Nivel.
- **`CriarPersonagemRequest`:** o que a criação envia: Nome e Raca.
- **`Raca`:** enum do shared (`Valdren`=1, `Aurideo`=2, `Granitico`=3, `Cinereo`=4, `Silvano`=5). No JSON trafega como **número**, não como texto.
- **`RegrasPersonagem.ValidarNome`:** validador do formato do nome, usado pelo client e pelo servidor.
- **`ListaPersonagensResponse`:** a lista de personagens da conta (de 0 a 4).
- **`CodigosErroPersonagem`:** os códigos dos motivos de recusa (`NomeInvalido`, `NomeJaExiste`, `LimitePersonagens`, `RacaInvalida`), que chegam no campo `Codigo` do `ErroApi` do Core. Erros genéricos (não autenticado, requisição inválida, erro interno) usam os códigos do Core (`CodigosErro`).
- Por serem HTTP, essas rotas não usam IDs de mensagem da faixa 3000–3999.

### Experiência, nível e pontos de atributo (#65, contrato da #299)

Regras na [fatia jogável, seção 6](fatia-jogavel.md#6-experiência-e-nível). O servidor concede a experiência, sobe o nível e gasta os pontos; o client só mostra e pede. ~~Este é o contrato; o servidor (#356) e o client (#357) vêm depois.~~ O servidor (#356, 11/10/2026) concede a experiência de `InimigoDerrotado` e gasta os pontos. O client (#357) mostra a barra e a tela de atributos (tecla C) e só envia o gasto.

| ID ou rota | Nome | Direção | Campos |
|---|---|---|---|
| 3000 | `ExperienciaGanha` | servidor → dono | `Quantidade`, `XpAtual`, `XpDoNivel`, `Nivel`, `PontosLivres` (o estado inteiro depois do ganho) |
| 3001 | `SubiuDeNivel` | servidor → quem está na área | `PersonagemId`, `Nivel` (uma mensagem só, mesmo subindo vários níveis) |
| `GET /api/personagens/{personagemId}/progresso` | `RotasPersonagem.Progresso` | client → servidor | recebe `ProgressoResponse`: `Nivel`, `XpAtual`, `XpDoNivel`, `PontosLivres`, `Distribuidos` |
| `POST /api/personagens/{personagemId}/atributos` | `RotasPersonagem.Atributos` | client → servidor | envia `DistribuirAtributosRequest` (`Pontos`); recebe `ProgressoResponse` |

- **`PontosDeAtributos`:** os cinco atributos (Forca, Destreza, Intelecto, Vitalidade, Espirito). No pedido é quanto gastar agora; em `Distribuidos` é o total que o jogador já distribuiu, sem base, equipamentos nem efeitos (o valor final é de [Combate](combate.md)).
- **Gasto de pontos:** o pedido vale inteiro ou não vale; recusado, nenhum ponto é gasto. Gastar não tem volta na fatia. `RegrasAtributos.ValidarDistribuicao` é o validador comum a client e servidor: valor negativo ou pedido sem ponto nenhum é `AtributoInvalido` (400); pedir mais do que há livre é `PontosInsuficientes` (409). Um negativo nunca "devolve" ponto para outro atributo.
- **Códigos novos em `CodigosErroPersonagem`:** `AtributoInvalido`, `PontosInsuficientes` e `PersonagemNaoEncontrado` (404: não existe ou não é da conta do token).
- O client pede o `ProgressoResponse` ao entrar no mundo; depois disso a experiência chega por `ExperienciaGanha`.
- **`IDiretorioDePersonagens`** (só no servidor, em `server/src/JogoDoGrupo.Server/Contratos/Personagem/`, [ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)): consulta de nome ↔ id para as outras funcionalidades. `TentarObterPorNome` não diferencia maiúsculas; `TentarObterNome` devolve o nome como foi digitado. Diz se o personagem **existe**; se está online quem sabe é o Mundo (`IPosicoesDoMundo`).

## Visual do jogador (arte)

Cada raça tem **um modelo próprio** low-poly (humanoide de 1,8 m, gerado no Blender por script: [ADR 0025](../decisoes/0025-modelos-proprios-no-blender-por-script.md) e [padrão dos modelos](../arte/padrao-de-modelos.md#personagens-das-raças)), já com esqueleto e animações. O client só **mostra**; quem decide posição e estado é o servidor. O visual vem do identificador da raça (o valor do enum `Raca`).

- **Registro no Core:** `RegistroVisualJogador` (`Features/Personagem/Scripts/`) chama `EntradaNoJogo.DefinirVisualDoJogador` antes da primeira cena (`BeforeSceneLoad`) e devolve o prefab da raça do personagem (`Jogador_<Raca>`); o Core instancia. A seleção guarda a raça de cada personagem da lista (`LembrarRacas`); personagem fora da lista usa o Valdren.
- **Prefabs:** `client/Assets/_Game/Features/Personagem/Resources/Personagem/Jogador_<Raca>.prefab` com `<Raca>` = `Valdren`, `Aurideo`, `Granitico`, `Cinereo` ou `Silvano` (nome do enum, sem acento; carregar com `Resources.Load<GameObject>("Personagem/Jogador_Valdren")`). Altura de 1,8 m, pés em Y=0, frente em +Z. O `Animator` fica na raiz do prefab. Não tem `CharacterController`, câmera nem lógica: isso é do Core.
- **Aparência** (um acabamento por raça; texto das raças no [board](../brainstorm/figjam.md#01--raças-de-asterra)): Valdren, humano de pele morena, tecido grosso, couro e cachecol ocre; Aurídeos, esguios de pele dourada, olhos claros e marcas douradas que brilham, diadema de sol; Graníticos, os mais largos e baixos, pele de granito facetado com fissuras âmbar que brilham e ombros de pedra; Cinéreos, pele de cinza e carvão, olhos de brasa, fissuras vermelhas e placas de obsidiana; Silvanos, os mais esguios, orelhas alongadas, cabelo e coroa de folhas, saiote de folhas.
- **Geração:** os FBX e PNG saem do Blender (`arte/blender/personagens/`, comandos em [padrao-de-modelos.md](../arte/padrao-de-modelos.md#gerar-os-personagens)); material, controller e prefab saem do script de editor `Features/Personagem/Editor/GeradorJogadoresDasRacas.cs`, menu **Jogo do Grupo > Personagem > Gerar jogadores das raças** (`npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.GeradorJogadoresDasRacas.Gerar`). Rodar de novo recria o mesmo resultado. Não edite o controller e o prefab à mão.
- **Parâmetros do Animator** (usados pelo Core e, depois, pelo combate):

| Parâmetro | Tipo | Significado |
|---|---|---|
| `Velocidade` | float | velocidade horizontal em m/s |
| `NoChao` | bool | `true` quando está no chão (padrão `true`); ainda não muda nenhum estado |
| `Esquiva` | trigger | dispara o rolamento |

- **Estados:** `Movimento`, um blend tree 1D por `Velocidade`: parado (0 m/s), andar (2 m/s), correr (5 m/s), e `Esquiva` (estado qualquer, por `Esquiva`; volta a `Movimento` perto do fim). O FBX também traz os clipes `Atacar`, `Morrer` e `Dano` (ainda sem estado no controller: entram com o combate). Nomes dos clipes: `Idle`, `Andar`, `Correr`, `Esquivar`, `Atacar`, `Morrer`, `Dano`.
- **Esqueleto e encaixes:** `Raiz` > `Quadril` > `Tronco` > `Cabeca`; `Tronco` > `Braco_E/D` > `Antebraco_E/D` > `Mao_E/D`; `Quadril` > `Perna_E/D` > `Canela_E/D` > `Pe_E/D` (E = esquerda do personagem). Os encaixes são objetos vazios criados pelo gerador, com esses nomes fixos (uso do artista de itens): **`EncaixeMaoDireita`**, filho do osso `Weapon.R` (que fica na palma da mão direita), e **`EncaixeCostas`**, filho do osso `Costas` (que fica nas costas, no alto do tronco). Os dois começam sem rotação e sem escala; o artista de itens ajusta a pose da arma no próprio encaixe.
