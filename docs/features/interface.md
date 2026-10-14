# Interface (Core)

**Responsável:**
**Status:** em definição (números em **proposta**, a calibrar)
**Faixa de IDs de mensagem:** não tem (tudo local no client; o servidor não participa)

## Resumo

Telas e menus do client que não pertencem a uma funcionalidade do jogo. Por enquanto, o menu de opções aberto com Esc (issue #89), as [janelas do jogo](#janelas-do-jogo) abertas por atalho (#134), o [HUD do jogo](#hud-do-jogo) (#93) e a [base de áudio](#áudio) (#96). Código no Core do client; mudanças em `Core/` pedem review do responsável.

## Definido

- O menu abre com **Esc**, tem **continuar**, **opções**, **voltar à seleção**, **sair da conta** e **sair do jogo**. "Sair da conta" já existe (apaga o token gravado; veja [personagem.md](personagem.md)). "Voltar à seleção" volta à escolha de personagem sem sair da conta.
- Opções iniciais (da issue #89, no client em 2026-10-11): volume (geral, música, efeitos), qualidade gráfica, resolução e tela cheia, sensibilidade da câmera, idioma. Gravadas neste computador. O volume geral, a qualidade, a tela e a resolução valem na hora; música e efeitos ficam gravados para o áudio (#123). Resolução 0 significa a do monitor. Idioma: só português. O jogo não pausa.
- **Janelas do jogo por atalho** (#134, decidido por Gabriel em 2026-10-11; provisório, o grupo pode ajustar): **C** personagem, **I** inventário, **L** missões, **M** mapa. **Uma janela por vez**, e com janela aberta o personagem fica **parado**. Regras em [Janelas do jogo](#janelas-do-jogo).
- As teclas atuais podem ser ajustadas em **Opções > Teclas**; os padrões e limites estão em [Teclas e atalhos](#teclas-e-atalhos).
- Na abertura, o client carrega o tema e verifica a disponibilidade e a versão da API em `/saude` antes de mostrar o login; falha mostra "Servidor fora do ar" e **Tentar de novo**. Ao escolher personagem, a tela Lumora mostra **Cenário**, **Conexão** e **Posição** conforme o trabalho real, sem porcentagem simulada. O mundo só aparece depois de aplicar a primeira posição do servidor; até lá, HUD e input ficam bloqueados. A espera por conexão e posição expira em 20 s. A falha oferece **Tentar de novo** e **Voltar à seleção**, desmontando a tentativa parcial antes de outra entrada.
- No mundo, a HUD mostra latência em milissegundos quando a conexão está autenticada. Na queda, uma faixa informa a tentativa de reconexão e o movimento fica bloqueado; após o limite de três tentativas, um diálogo **Conexão perdida** oferece **Tentar de novo** e **Voltar à seleção**. Acesso negado recebe mensagem própria. Na primeira entrada, falha de conexão encerra a espera imediatamente; o erro fica na tela de carregamento. Os avisos são removidos ao sair do mundo.
- O client continua executando quando a janela perde foco, inclusive no Play do Unity. A inicialização do Core habilita `Application.runInBackground` para manter o processamento da rede e evitar que a autenticação UDP expire enquanto outra janela está ativa (#759).

## Em aberto

- [ ] Abrir o menu pausa o jogo? Proposta: **não**. O jogo é online e persistente, o mundo segue rodando; o personagem fica parado e vulnerável (sem tela de pausa).
- [ ] Idiomas disponíveis na primeira versão (proposta: só português; a opção existe mas lista um idioma).
- [ ] Sair do jogo/da conta dentro de dungeon ou combate: sai na hora ou tem espera (proposta: espera de 10 s em combate)? Decisão do grupo.
- [ ] ~~Remapear teclas (#90): só listado, não desenhado aqui.~~ substituído em 2026-10-11 por [Teclas e atalhos](#teclas-e-atalhos) (proposta de atalhos padrão e regras de troca)
- [ ] Janelas do jogo (#134), decisões do grupo:
  - [ ] Confirmar **uma janela por vez e personagem parado** (decisão provisória de 2026-10-11) ou permitir várias janelas abertas e andar com elas (como no WoW)?
  - [ ] Com o chat aberto (#95), as letras não podem abrir janela: a regra entra junto com o chat
  - [ ] Outras janelas que o grupo quer com atalho (sugestões: **K** skills, **N** talentos, **O** social, **P** profissões)
  - [ ] A barra de atalhos fica sempre visível (como está) ou some depois que a pessoa aprende?
- [ ] Teclas e atalhos (#90), decisões do grupo:
  - [x] ~~Aprovar os atalhos padrão da tabela (proposta).~~ Decidido em 2026-10-12, como no New World: leve no clique esquerdo, pesado ao segurar o esquerdo por 0,3 s, guarda no botão direito, esquiva no Alt esquerdo (esta última desde 2026-10-11). A troca de arma (R) e as skills 1, 2 e 3 valem quando a S10 entrar.
  - [ ] Tecla já usada: **trocar as duas ações** (proposta) ou deixar a outra sem tecla?
  - [ ] Tecla de **correr**: segurar (proposta) ou alternar?
  - [x] ~~Combate ainda não decidiu se **bloqueio**, **stamina** e **troca de arma** entram na fatia jogável~~ Decidido em 2026-10-12: guarda e stamina entram nesta fatia. A troca de arma está decidida e fica para a S10; a tecla R vale nessa hora.
  - [ ] ~~[personagem.md](personagem.md) só define andar e correr; **pular** não existe nas docs, então não tem atalho (se entrar, sugestão: Espaço vira pular e a esquiva vai para Alt)~~ substituído em 2026-10-11: o pulo **já existe no client** (Espaço, só no chão, 1,25 m de altura; `MovimentoTerceiraPessoa`), então entrou na tabela e a esquiva foi para o Alt esquerdo. Falta o grupo confirmar: o pulo fica no jogo? Ele serve para alguma coisa no combate (sair de área marcada no chão) ou é só movimento? O servidor precisa validar a altura (hoje a presença aceita de -5 a 50 m, veja [mundo.md](mundo.md))
- [ ] **Textos de interface do livro de Asterra** (proposta do board, 2026-10-11, não validada; [figjam.md](../brainstorm/figjam.md) seção 10, 11 e 09): sugestões de texto para os cartões, a adotar na tela quando o grupo aprovar a regra por trás. Caído: "Você caiu. Aguarde ajuda ou retorne a um ponto seguro." e "Levantado: recupere a vida antes de voltar à linha de frente." Ajuda de mira: "Mire na direção do perigo. Mova-se para fora da área de ataque." Defesa: "Defesa insuficiente para bloquear completamente este impacto." Loot: "Maior dano: você. Recompensa disponível para coleta." (**conflito** com o loot individual da [fatia](fatia-jogavel.md#5-loot-no-chão); o texto precisaria mudar). Tela de atributos: "Distribua seus pontos. Sua raça dá identidade; seus equipamentos dão possibilidades." Mundo: "Entre, encontre seus amigos e continue de onde parou." Diário: "Cada saída pode render uma peça melhor, uma combinação nova ou uma história para contar." Também pede botão de **retorno** na tela de caído e aba **Preparação** de skills (dono: [skills.md](skills.md)).
- [ ] ~~HUD configurável (#93): só listado, não desenhado aqui.~~ substituído em 2026-10-11 por [HUD do jogo](#hud-do-jogo) (proposta de layout e regras)
- [ ] ~~Áudio (#96): só listado; os sons em si não são desenhados aqui.~~ substituído em 2026-10-11 por [Áudio](#áudio) (proposta de áreas com música, sons e fontes)
- [ ] HUD do jogo (#93), decisões do grupo:
  - [ ] Aprovar as posições da tabela de layout (proposta)
  - [ ] **Alvo sem tab target:** vale "o inimigo sob a mira ou o último que você acertou" (proposta), ou o jogador trava o alvo com uma tecla?
  - [ ] Número de dano: mostrar **só o meu dano e o que eu recebo** (proposta) ou também o dos aliados? E a cura que eu faço nos outros?
  - [ ] Aviso de ataque no HUD **só para o boss** (proposta) ou também para o inimigo comum?
  - [ ] Ver a vida exata dos aliados (número) ou só a barra (proposta: barra e porcentagem)?
  - [ ] Opção para esconder o HUD inteiro (captura de tela): entra na primeira versão? (proposta: não)
- [ ] Áudio (#96), decisões do grupo:
  - [ ] Aprovar as áreas com música e a troca entre elas (proposta)
  - [ ] Aprovar as fontes CC0 sugeridas; quem baixa escolhe as faixas e registra em [licenças](../arte/licencas.md)
  - [ ] Som posicional de outros jogadores (passos, golpes): entra na primeira versão? (proposta: sim, só golpes e acertos, até 25 m)
  - [ ] "Som que se ouve de longe" e noite escura ([validação](../brainstorm/validacao.md), S8) ficam fora desta base; entram quando o grupo decidir S8

## Regras

**Base:** menus de pausa de jogos de PC online (WoW, New World). **Igual:** Esc abre o menu; opções ficam num submenu. **Diferente:** não pausa o jogo (proposta).

### Menu (Esc)

| Item | Regra |
|---|---|
| Esc | abre o menu; Esc de novo ou **Continuar** fecha. Se há outro painel aberto (inventário, mapa), Esc fecha o painel primeiro |
| Continuar | fecha o menu |
| Opções | abre a lista abaixo; **Voltar** retorna ao menu |
| Sair da conta | apaga o token e volta ao login (já existe) |
| Sair do jogo | fecha o aplicativo |

Na composição do menu, **Continuar** é a ação principal, **Opções** vem em seguida e as três ações de saída ficam em grupo separado. Um aviso informa que o mundo continua ativo, e a dica no rodapé mostra como fechar com Esc.

Com o menu aberto, o cursor fica livre e a câmera e o movimento não recebem input (**proposta**).

### Janelas do jogo

Decidido por Gabriel em 2026-10-11 (#134); é **provisório** e o grupo pode ajustar. **Base:** MMOs de PC (World of Warcraft, New World), veja [referências](../referencias.md). **Igual:** uma letra abre cada janela e a mesma letra fecha; Esc fecha a janela aberta. **Diferente:** só uma janela aberta por vez e o personagem fica parado, como no New World (no WoW dá para abrir várias e andar com elas).

| Tecla | Janela | Dona | Hoje mostra |
|---|---|---|---|
| **C** | Personagem | [Personagem](personagem.md) | nome, raça e nível, como o servidor informa. Atributos e equipamentos entram quando existirem |
| **I** | Inventário | [Itens](itens.md) | o inventário do personagem; aberta pelo baú (E), mostra também os ganhos |
| **L** | Missões | [Missões](missoes.md) | aviso "Em construção" (as missões ainda não existem) |
| **M** | Mapa | [Mundo](mundo.md#mapa-múndi) | aviso "Em construção" (o mapa é a decisão #92) |
| **N** | Árvore de atributos | [Talentos](talentos.md#implementação-da-625-árvore-compra-e-tela) | a árvore radial, o painel do nó e a compra de ranks |

- **A tecla alterna:** abre a janela; apertar de novo fecha.
- **Uma janela por vez:** abrir outra fecha a que estava aberta.
- **Esc** fecha a janela aberta. Sem janela aberta, Esc abre o [menu](#menu-esc). Um Esc nunca faz as duas coisas.
- Com janela aberta, o personagem fica **parado**, a câmera não gira e o cursor fica livre. O jogo **não pausa**: o mundo segue rodando e o personagem fica vulnerável.
- Com o **menu do Esc aberto**, os atalhos de janela não disparam.
- Toda janela tem a mesma moldura: título, botão de fechar (X) no canto e, no pé, a dica de como fechar (ex.: "I ou Esc: fechar").
- **Barra de atalhos:** no canto inferior direito, mostra a tecla de cada janela e a do menu, com a janela aberta em destaque. Só mostra, não recebe clique. Quando a moldura do HUD existir (#122), a barra passa a ser uma região dela.
- Janela com defeito mostra "Não foi possível abrir esta janela." e continua fechável; o jogo não trava.
  - **Teclas que uma janela não pode usar por padrão:** as de outra ação da tabela de [Teclas e atalhos](#teclas-e-atalhos) (W, A, S, D, Q, E, F, R) e a de outra janela. O client recusa o registro na inicialização.
  - Trocar as teclas das janelas é a tela de **Opções > Teclas** (#124); a tabela mostra os padrões.
- Servidor autoritativo não se aplica: abrir janela não afeta regra de jogo. O que a janela mostra vem do servidor (inventário, personagem).

**Quem monta o quê:** o Core cuida do atalho, da moldura, do Esc, do cursor e da barra. Cada funcionalidade registra a sua janela e monta só o conteúdo, sem referenciar outra funcionalidade (API em [arquitetura.md](../arquitetura.md#entrada-no-mundo)).

**Testes** (client, EditMode): chave ou tecla repetida, reservada ou inválida é recusada; a tecla abre e fecha; abrir outra fecha a atual; Esc fecha a janela antes de abrir o menu; atalho não abre janela com o menu aberto; ordem e destaque da barra de atalhos; janela com defeito mostra o aviso.

### Opções iniciais

| Opção | Valores (proposta) | Padrão |
|---|---|---|
| Volume geral / música / efeitos | 0 a 100, passo 5 | 80 / 70 / 80 |
| Qualidade gráfica | Baixa, Média, Alta (níveis de qualidade do Unity) | Média |
| Resolução | as do monitor | resolução atual |
| Tela cheia | sim/não | sim |
| Sensibilidade da câmera | 0,5x a 2,0x, passo 0,1 | 1,0x |
| Idioma | Português | Português |

- Gravadas neste computador (arquivo local do client, não na conta); valor inválido ou ausente volta ao padrão. Carregam antes da abertura/login. Áudio, qualidade gráfica e sensibilidade mudam sem reconstruir o painel; resolução e tela cheia só mudam após **Aplicar vídeo**. A lista de resoluções mostra cada tamanho uma vez, sem repetir taxas de atualização.
- Opções usa as abas **Áudio**, **Vídeo**, **Jogo** e **Teclas**. Esc em Opções volta ao menu principal, exceto durante a captura de uma tecla, quando cancela a troca. O idioma aparece como Português, único disponível nesta versão.
- Servidor autoritativo não se aplica: nada aqui afeta regra de jogo.
- **Testes esperados** (client, EditMode): salvar e ler as opções; valor fora da faixa é limitado; arquivo corrompido volta aos padrões.

### Teclas e atalhos

**Base:** New World (combate de ação em 3ª pessoa no PC, veja [referências](../referencias.md)) e MMOs de PC em geral. **Igual:** WASD para andar, mouse para câmera e ataque, menu no Esc, tecla fixa por ação com atalhos de painel em letras. **Igual ao New World nas teclas de luta** (decidido em 2026-10-12): leve no clique esquerdo, pesado ao segurar o esquerdo, guarda no direito, esquiva no Alt esquerdo. O Espaço continua o pulo. Só teclado e mouse; gamepad fica fora.

  A tela fica em **Opções > Teclas**, com uma linha por ação (nome, tecla atual, botões **Trocar** e **Padrão**) e um botão **Restaurar padrão** com confirmação. O ataque leve e o pesado compartilham o mesmo botão: soltar faz o leve, segurar por 0,3 s faz o pesado. A coleta de itens no chão também aparece como ação própria.

| Ação | Tecla padrão (proposta) | Troca? |
|---|---|---|
| Andar para frente / trás / esquerda / direita | W / S / A / D | sim, cada direção |
| Correr | Shift (segurar) | sim |
| Câmera | mover o mouse | não (sensibilidade fica em Opções) |
| Pular (já existe no client) | Espaço | sim |
| Esquiva | ~~Espaço~~ Alt esquerdo (substituído e decidido em 2026-10-11) | sim |
  | Ataque leve / pesado | clicar / segurar o botão esquerdo do mouse por 0,3 s | sim, juntos |
| Bloqueio | botão direito do mouse | sim |
| Skills 1, 2 e 3 (quando a S10 entrar) | 1, 2, 3 | sim |
| Trocar de arma (quando a S10 entrar) | R | sim |
| Interagir | E | sim |
  | Pegar item no chão | F | sim |
| Personagem | C | sim |
| Inventário | I | sim |
| Missões | L | sim |
| Mapa | M | sim |
| Chat (abrir e enviar) | Enter | não por enquanto (fixa no código do chat; Esc fecha a caixa sem enviar) |
| Menu | Esc | **não** (reservada) |

  **Regras de troca implementadas:**

- Cada ação tem **uma** tecla ou botão. Clicar em **Trocar** e apertar a nova tecla grava; Esc durante a captura cancela e mantém a tecla antiga.
  - **Tecla já usada por outra ação:** as duas ações **trocam de tecla** (a outra fica com a tecla antiga desta) e a tela avisa "X agora usa Y". Se a troca daria um botão de mouse a uma ação que só aceita teclado, é recusada. Nenhuma ação fica sem tecla.
- **Reservadas, não podem ser usadas nem trocadas:** Esc (menu e cancelar), Alt+F4 e teclas do sistema (Windows, Cmd). Tentar usar uma delas mostra aviso e não muda nada.
- Botões do mouse só valem para ações de combate (ataque leve, ataque pesado, bloqueio, skills); andar, interagir, mapa e inventário aceitam só teclado. A câmera segue o mouse e não é uma ação trocável.
  - **Voltar ao padrão:** por ação (botão ao lado da linha, trocando com outra se necessário) e para todas (botão **Restaurar padrão**, com confirmação).
- Mudanças aplicam na hora, sem botão "Aplicar", e valem também com o menu aberto novamente.
- Com o menu ou a captura de tecla aberta, as ações do jogo não disparam (mesma regra do menu).

**Onde fica gravado:** neste computador, no mesmo arquivo local das opções iniciais (não na conta, e o servidor não participa). Guarda só as teclas diferentes do padrão, por nome de ação. Ação desconhecida, tecla inválida, reservada ou repetida no arquivo volta ao padrão daquela ação; arquivo corrompido volta todas ao padrão.

**Testes esperados** (client, EditMode): os padrões não têm tecla repetida nem reservada; trocar para tecla livre grava e lê de volta; trocar para tecla usada troca as duas ações; tecla reservada é recusada sem mudar nada; botão do mouse recusado em ação só de teclado; restaurar uma ação e restaurar todas voltam ao padrão; arquivo corrompido ou com tecla repetida volta aos padrões.

### HUD do jogo

Tudo aqui é **proposta** (issue #93). **Base:** World of Warcraft (lista do grupo e barra do alvo) e New World (combate de ação com poucos elementos na tela), veja [referências](../referencias.md). **Igual:** lista do grupo à esquerda, barra do boss no topo, números de dano sobre o alvo. **Diferente:** não há alvo travado por Tab (o combate é de mira livre, D4), então o "alvo" do HUD é calculado; e a tela mostra menos coisas (sem barras de skills de classe, sem medidor de dano).

O HUD só **mostra** o que o servidor já enviou. Ele não decide vida, dano, alvo de ataque nem quem está no grupo.

#### Layout

| Região da tela | Elemento | De onde vem o dado |
|---|---|---|
| Canto inferior esquerdo | Barras do personagem: vida, stamina e mana (já existem) | [fatia-jogavel.md](fatia-jogavel.md), seção 1 |
| Abaixo das barras do personagem | Barra de experiência | [personagem.md](personagem.md) |
| Acima das barras e da experiência | Chat; a posição acompanha a altura real do bloco inferior | [social.md](social.md#chat-de-texto-issue-95) |
| Lateral esquerda, do meio para cima | **Lista do grupo**: até 4 aliados (o grupo tem 5 e você não aparece na lista) | lista do grupo de [social.md](social.md) e a vida que o servidor envia |
| Topo, centro | **Barra do alvo** e, acima dela, a **barra do boss** | vida do inimigo ([fatia-jogavel.md](fatia-jogavel.md), seções 4 e 7) |
| Centro, logo abaixo da barra do boss | **Aviso de ataque telegrafado** | início do preparo do ataque, enviado pelo servidor |
| No mundo, sobre quem levou o dano | **Texto flutuante de dano** | resultado do golpe, enviado pelo servidor |
| Parte de baixo, centro | **Mensagens de sistema** | avisos das funcionalidades (convite, loot, erro) |
| Canto superior direito | Minimapa | [mundo.md](mundo.md#minimapa) |

Nada do HUD recebe clique, salvo o que uma funcionalidade pedir (ex.: aceitar convite): o cursor fica preso à câmera durante o jogo. Com o menu do Esc aberto, o HUD continua visível por baixo, sem atualizar animações de aviso atrasadas (um aviso que já venceu não aparece ao fechar o menu). Com o mapa em tela inteira, a HUD do personagem fica oculta; só os controles e informações do próprio mapa aparecem sobre ele (veja [Mapa-múndi](mundo.md#mapa-múndi)).

O bloco de recursos e experiência deve permanecer legível sem competir com o chat. A distância entre eles acompanha a altura resolvida do bloco inferior, inclusive com texto ampliado; em largura estreita, ambos têm limite de largura relativo à área disponível. Conferir o conjunto em 1280 × 720, 1920 × 1080, 2560 × 1440, 960 × 720 e 640 × 720.

#### Lista do grupo

- Uma linha por aliado, na **ordem da lista do servidor** (ordem de entrada), com: nome, barra de vida com porcentagem e uma marca de **líder**.
- Estados da linha: **normal**; **caído** (barra vazia, ícone e a contagem de 45 s descendo, veja [fatia-jogavel.md](fatia-jogavel.md), seção 7); **offline** (linha apagada, sem barra, enquanto a vaga está guardada); **longe** (aliado em outra área: linha apagada com o nome da área, sem vida).
- Vida abaixo de **30%**: a barra pisca, para o healer notar.
- Sem grupo, a lista não aparece.
- A lista mostra só vida. Stamina, mana e efeitos de status dos aliados ficam fora da primeira versão.

#### Barra do alvo e barra do boss

- O combate não tem alvo travado. O **alvo do HUD** é, nesta ordem: (1) o inimigo vivo **sob a mira** (centro da tela) a até **30 m**; (2) se não houver, o **último inimigo que você acertou ou que acertou você** nos últimos **5 s**. Sem nenhum dos dois, a barra some.
- A barra mostra nome, nível, vida em barra e em porcentagem. Não mostra o número exato de vida.
- **Boss:** enquanto a luta do boss está ativa para o seu grupo, a barra do boss fica **fixa no topo**, larga, mesmo sem mirar nele. A barra do alvo comum continua aparecendo abaixo, menor, se você mirar em outro inimigo.
- Inimigo morto: a barra some na hora.
- O alvo do HUD **não muda o golpe**: o acerto continua sendo o volume do ataque, decidido pelo servidor.

#### Aviso de ataque telegrafado

- O aviso principal é **no mundo** (área marcada no chão, brilho ou animação de preparo), e é de [combate](combate.md) e [mobs](mobs.md). O HUD só reforça.
- Para ataques do **boss** (aviso de **3 s**): nome do ataque e uma barra que esvazia até o golpe, abaixo da barra do boss.
- Se o **seu personagem está dentro da área marcada**, a borda da tela fica vermelha até você sair ou o golpe acontecer. O client calcula isso com a área que o servidor enviou; é só aviso, o dano continua sendo decidido pelo servidor.
- Ataque do inimigo comum (aviso de cerca de 1 s): **sem aviso no HUD**, só no mundo, para a tela não piscar o tempo todo com grupos de 3 a 5 inimigos.
- Dois avisos do boss ao mesmo tempo: mostra os dois, o que acontece primeiro em cima. No máximo **3** na tela.

#### Texto flutuante de dano

- O número aparece **quando o servidor confirma** o dano ou a cura, nunca pela animação do client.
- Aparece sobre quem levou o dano, sobe e some em **1 s**.
- Mostra: o dano que **você causou** (branco; ataque pesado em tamanho maior), o dano que **você recebeu** (vermelho) e a cura que **você recebeu** (verde). Dano entre aliados e inimigos que não envolve você não aparece.
- No máximo **20** números na tela; passando disso, os mais antigos somem primeiro.
- Golpe que acertou vários inimigos mostra um número por inimigo.

#### Mensagens de sistema

- Lista de até **5** linhas; cada linha some depois de **6 s**. Linha nova entra embaixo e empurra as outras.
- Três tipos: **informação** (branco), **aviso** (amarelo) e **erro** (vermelho). Exemplos: "Ana entrou no grupo", "Inventário cheio", "Stamina insuficiente".
- A mesma mensagem repetida em menos de **2 s** não cria linha nova: a linha existente ganha um contador ("x2") e o tempo dela recomeça.
- As funcionalidades publicam a mensagem por um ponto do Core (nome sugerido: `AvisosDoJogo.Mostrar(texto, tipo)`), sem referenciar a interface de outra funcionalidade. O texto é o que a pessoa lê; códigos de erro do servidor são traduzidos por quem publica.
- `tipo` indica gravidade; `categoria` indica origem (**Sistema** ou **Combate**). O jogador escolhe a visibilidade de cada categoria nos controles do chat; os avisos de erro continuam visíveis no HUD. O Core ainda publica o evento completo para o histórico mesmo quando a linha do HUD foi ocultada.
- ~~Quando o chat existir (#95), as mensagens de sistema **também** entram no histórico do chat (veja [social.md](social.md)); a lista do HUD continua.~~ Substituído em 2026-10-11: a Social assina `AvisosDoJogo.Mostrado` e copia a linha no canal Sistema; a lista do HUD continua. O Core não referencia a Social.

#### Quem monta o quê

O Core cria a moldura do HUD com as regiões da tabela e entrega um contêiner por região. Cada funcionalidade preenche a sua região, sem referência direta entre funcionalidades: **Social** a lista do grupo, **Combate** as barras do personagem, a barra do alvo, o aviso e o texto de dano, **Mundo** o minimapa. As mensagens de sistema são do Core. A forma do registro (nome das regiões e da API) é do agente `client-core`.

**Testes esperados** (client, EditMode): escolha do alvo (sob a mira ganha do último acertado; mais de 30 m não conta; some depois de 5 s; inimigo morto some); barra do boss fica visível sem mira enquanto a luta está ativa; lista do grupo na ordem do servidor, sem a própria pessoa, com os estados caído, offline e longe; barra pisca abaixo de 30%; aviso do boss respeita o limite de 3 e a ordem por tempo; borda vermelha liga e desliga ao entrar e sair da área; limite de 20 números de dano; mensagens de sistema: limite de 5, expiração em 6 s e contador de repetidas.

### Áudio

Tudo aqui é **proposta** (issue #96). **Base:** World of Warcraft (uma música por área, com troca suave ao mudar de área) e New World (golpes com som pesado, que dão a sensação do acerto). **Igual:** música por área e sons de combate posicionados no mundo. **Diferente:** poucas faixas, todas CC0, e nada de dublagem.

O áudio é só do client: o servidor não envia "toque este som". Os sons tocam em resposta ao que o client já recebe (acerto confirmado, aviso de ataque, mensagem de sistema).

#### Áreas com música

| Área | Música | Quando troca |
|---|---|---|
| Abertura, login e seleção de personagem | tema do jogo (calma) | ao entrar no mundo |
| Cidade (hub) | faixa de cidade medieval, calma, em repetição | ao sair pelo portão |
| Dungeon (entrada e três salões) | faixa de tensão, baixa | ao entrar na dungeon |
| Luta do boss | faixa de combate | quando a luta começa; volta para a faixa da dungeon quando o boss morre ou o grupo cai |
| Vitória | toque curto (até 10 s), uma vez | ao matar o boss; depois, faixa da dungeon |

- Troca de faixa com transição cruzada de **2 s**. Uma faixa por vez.
- Em combate fora do boss a música não muda (o grupo de inimigos comuns é rápido demais para trocar faixa).
- Caído: a música baixa para **50%** até levantar ou voltar ao hub.

#### Efeitos

- **Combate (no mundo, posicionais):** golpe leve e pesado no ar, acerto, esquiva, aviso de ataque do inimigo, morte do inimigo, queda e levantar do personagem. O som de **acerto** toca quando o servidor confirma; o do golpe no ar toca com a animação.
- **Ambiente (no mundo):** fonte da praça, passos do próprio personagem.
- **Interface (sem posição):** clique de botão, abrir e fechar painel, mensagem de erro, convite recebido, item coletado, subir de nível.
- Sons de outros jogadores: só golpes e acertos, até **25 m** de distância; passos de outros jogadores ficam fora.
- No máximo **16** efeitos ao mesmo tempo; passando disso, os mais distantes são cortados primeiro.

#### Volumes e mixer

- Um mixer com três grupos: **Geral**, com **Música** e **Efeitos** dentro dele. São os três volumes do [menu de opções](#opções-iniciais) (0 a 100, padrões 80 / 70 / 80). Sons de interface entram em Efeitos.
- Volume 0 silencia o grupo. A escala do controle é percebida como linear pela pessoa (o client converte para decibéis).
- Com a janela do jogo sem foco, o som continua (o jogo é online e não pausa).
- O mixer e as fontes de áudio são criados pelo Unity (script de editor ou em tempo de execução), nunca escritos à mão ([ADR 0014](../decisoes/0014-conteudo-3d-e-agentes-artistas.md)).

#### Fontes CC0 sugeridas

Só entram arquivos **CC0** (ou CC-BY com crédito), em `client/Assets/ThirdParty/`, com uma linha em [licenças](../arte/licencas.md) **no mesmo PR que traz o arquivo**. Esta lista indica onde procurar; nenhuma faixa foi escolhida nem baixada ainda, e a licença de cada arquivo precisa ser conferida na página dele na hora de baixar.

| Para quê | Onde procurar | Observação |
|---|---|---|
| Efeitos de interface | Kenney, pacotes "Interface Sounds" e "UI Audio" (kenney.nl) | mesma fonte das peças do hub; pacotes CC0 |
| Efeitos de combate e impacto | Kenney, pacotes "Impact Sounds" e "RPG Audio" (kenney.nl) | golpes, passos, itens |
| Música por área | OpenGameArt (opengameart.org), filtrando por licença **CC0** | cada faixa tem licença própria: conferir uma a uma |
| Ambiente (fonte, vento) | Freesound (freesound.org), filtrando por **Creative Commons 0** | exige conta para baixar; conferir a licença do arquivo |

**Testes esperados** (client, EditMode): a área atual escolhe a faixa certa (cidade, dungeon, boss, vitória e volta); começar e terminar a luta do boss troca e destroca a faixa; volume do menu vira o volume do grupo certo (0 silencia, 100 é o máximo, valor fora da faixa é limitado); limite de 16 efeitos corta os mais distantes; som de outro jogador além de 25 m não toca.

### Fora desta primeira versão

~~Remapear teclas~~ (substituído em 2026-10-11: tem regras em [Teclas e atalhos](#teclas-e-atalhos)), ajustes de HUD (mover, redimensionar ou esconder elementos), dublagem e música dinâmica por intensidade do combate, acessibilidade (daltonismo, tamanho de texto), controle de gamepad e opções de rede.

## Depende de

[Personagem](personagem.md) (sair da conta), Core do client (input, UI base). O HUD também depende de [Social](social.md) (lista do grupo), [Combate](combate.md) e [Mobs](mobs.md) (vida, dano e avisos de ataque, pelas regras de [fatia-jogavel.md](fatia-jogavel.md)) e de [Mundo](mundo.md) (minimapa e área atual, que também escolhe a música).

## Expõe

Nada.
