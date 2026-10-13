# Combate

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 6000–6999

## Resumo

Cálculo de dano, seleção de alvo e efeitos de status. Usado por PvE e PvP.

## Definido

- Regras iniciais da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- **Combate de ação** em 3ª pessoa, no estilo de New World (D4 da [validação de 2026-10-10](../brainstorm/validacao.md)): mira, esquiva, posicionamento
- Base: [New World](../referencias.md)
- **Ritmo do New World: ação, mas não frenética e sem spam de skills** (preferência do grupo, 2026-10-10). O dano vem principalmente de ataque leve e pesado; poucas skills, com recarga que pesa; stamina para esquiva e bloqueio
- **Papéis no grupo: tank, healer e DPS**, no estilo de World of Warcraft (decidido em 2026-10-10). Sem classes, o papel vem da combinação de itens equipados (D2)
- **Buffs de grupo**: cada papel traz buffs que complementam o time
- **Ataques telegrafados** (decidido em 2026-10-10): bosses e inimigos fortes avisam antes de bater (área marcada no chão, brilho ou animação de preparo) e o jogador tem tempo de esquivar ou sair da área. Base: Final Fantasy XIV e WoW
- **Decisões do Gabriel em 2026-10-11** (estavam só em comentário de issue):
  - **Ataque (#61):** cone de **2,5 m e 90°** à frente do atacante; **sem compensação de lag na fatia**; boneco de treino na cidade. Os números moram em `RegrasAtaque`.
  - **Esquiva (#62):** o dono do código é **Combate** (a stamina é dele); a tecla é o Alt esquerdo ([interface.md](interface.md)).
  - **Caído (#125):** o dono do estado é **Combate**, que já é dono da vida; [Personagem](personagem.md#morte-e-caído-issue-97) só mostra a tela.

## Em aberto

- [x] ~~Combate com alvo fixo (tab target) ou mira livre (action)?~~ Decidido: action (D4)
- [x] ~~Ataque leve e pesado, bloqueio, stamina: quais entram na fatia jogável?~~ Decidido em 2026-10-12, como no New World: entram a cadeia de três leves, o pesado, a esquiva com janela sem dano e a guarda segurada. Os dois gastam stamina.
- [x] ~~**Troca de arma** (duas armas, como New World e Throne and Liberty)?~~ Decidido em 2026-10-12: sim, duas armas e três skills por arma, com recarga separada. A fatia atual ainda joga com uma espada. A troca entra na S10.
- [x] ~~Bosses com ataques telegrafados?~~ Decidido em 2026-10-10: sim (veja "Definido")
- [ ] Como o servidor valida acertos com a latência (lag compensation)? Decidido pelo Gabriel em 2026-10-11 **para a fatia**: sem compensação de lag (o cone é medido no momento do acerto, com a posição que o servidor tem). Depois da fatia a pergunta segue aberta (issue #172).
- [x] ~~Quais efeitos de status existem?~~ Adotado em 2026-10-11 (/decida, #171), fácil de mudar: seis efeitos, veja [Efeitos de status](#efeitos-de-status-171).
- [x] ~~Como funciona o agro (ameaça) para o tank segurar os inimigos?~~ Adotado em 2026-10-12 (/decida, #169), fácil de mudar: tabela de ameaça por inimigo, veja [Ameaça (agro)](#ameaça-agro-169).
- [ ] Quais buffs cada papel traz? Eles acumulam?
- [ ] Um jogador pode trocar de papel trocando de itens no meio da dungeon?
- [ ] **Atributos de 2026-10-12** ([personagem](personagem.md#atributos-do-personagem), prioridade alta): Combate é dono das fórmulas que juntam atributos do personagem, atributos próprios dos equipamentos e propriedades das skills, com uma pipeline única que guarda a origem de cada modificador ([#624](https://github.com/GabrielNied/jogo_do_grupo/issues/624)). **Estrutura do dano com arma** (decidida por Gabriel em 2026-10-12): `dano = dano-base da arma × (1 + % de Força ou Intelecto do personagem + % de Força ou Intelecto do item + árvore) × multiplicador do golpe ou da skill`. O dano-base e os % do item vêm da conversão dos [atributos dos equipamentos](itens.md#atributos-dos-equipamentos); a Força do item é fonte separada da do personagem e nunca soma nela. A pipeline dos secundários e o contrato da skill estão em [Atributos secundários e pipeline](#atributos-secundários-e-pipeline-624) (implementados na #624); a ordem do dano-base com arma fica para a #623. Pendentes ([#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627)): dano por ponto de Força e de Intelecto; crítico (chance, dano e quais skills podem critar); velocidade de ataque física e mágica sobre animação e conjuração; penetração contra defesa física e mágica; ordem e acúmulo dos modificadores. Os recursos são vida, stamina e mana, e Vigor é só atributo ([personagem](personagem.md#atributos-do-personagem); nomes novos no código desde a [#622](https://github.com/GabrielNied/jogo_do_grupo/issues/622)).

### Propostas do livro de Asterra (2026-10-11, não validadas)

Vêm do [Livro de ideias](../brainstorm/figjam.md) (seção 10). São **proposta do board (2026-10-11), não validada pelo grupo**; nada aqui muda o que já está em "Definido" ou implementado.

- [x] **Bloqueio por defesa, adotado em 2026-10-11 (/decida, #344), fácil de mudar:** sem parry nem janela de timing. Desde 2026-10-12 a guarda segurada é o bloqueio ativo ([abaixo](#guarda-segurada)); esta fórmula só reduz o dano que a guarda não segurou. Defesa >= 1,5x a potência de bloqueio do golpe anula o dano direto; abaixo disso reduz proporcionalmente, até 80%. Os dois números ficam em constantes nomeadas. O client avisa defesa insuficiente. Proposta original do livro: (sem parry nem janela de timing): com defesa de pelo menos 1,5x a potência de bloqueio do golpe, anula o dano direto; abaixo disso reduz proporcionalmente, até 80%. Responde em parte à pergunta de bloqueio acima. Depende da defesa (equipamento, [itens.md](itens.md)); o dano recebido hoje não tem defesa (`ServicoDeDanoRecebido.DanoFinal`).
- [x] **Sem defesa de timing, adotado em 2026-10-11 (#344):** a "defesa que abre oportunidade" fica de fora. O livro rejeita parry e bloqueio perfeito. **Conflito leve** com a sugestão "defesa que abre oportunidade" (bloquear no timing certo desequilibra o inimigo), que está só em "Sugestões para discussão" do board.
- [x] ~~**Esquiva sem invulnerabilidade:**~~ Revisto em 2026-10-12: a esquiva segue o New World. Nos primeiros 0,3 s o dano direto some (seção 3 da [fatia](fatia-jogavel.md)). Quem decide é o servidor.
- [x] ~~**Ataques básicos por arma**~~ Decidido em 2026-10-12: a espada está na fatia. Martelo, arco e cajado, quando entrarem, usam a mesma cadeia, o mesmo pesado, a mesma esquiva e a mesma guarda. O volume de cada uma fica na tarefa dessa arma. Sem inventar o formato agora.
- [ ] **Mira e volumes de acerto:** mouse direciona golpes, projéteis e áreas, sem seleção automática obrigatória; a área visível da skill deve ser a área real de acerto; volumes generosos (cone, faixa, círculo). Coerente com o cone de 90° já implementado; vale como critério para as skills.
- [ ] **Movimentos além de correr e esquivar:** pulo, natação e escalada (proposta de escalada em superfícies e bordas do mapa; rios cruzados a nado, com gelo como alternativa). Dono: [personagem.md](personagem.md) e [mundo.md](mundo.md).
- [ ] **Recursos:** pools-base de 100 stamina e 100 mana e regeneração de 10 stamina/s e 5 mana/s após 2 s **já coincidem** com a [fatia](fatia-jogavel.md#1-atributos-e-recursos). Novidade: poções, refeições e descanso em locais seguros como fontes de recuperação, e equipamentos e comida que modificam a regeneração.
- [ ] **Estado caído:** o livro propõe 45 s, levantar por 3 s, 20% de vida e retorno à cidade ou teleporte descoberto; coincide com a [fatia](fatia-jogavel.md#7-dungeon-de-até-5-com-um-boss) e com [personagem.md](personagem.md#morte-e-caído-issue-97). Novidade: o **botão de retorno antes dos 45 s** e o retorno a **teleporte descoberto**; sem perda de XP, moeda, equipamento, materiais nem durabilidade por morte na primeira versão.
- [x] **Cast com compromisso, regra de custo adotada em 2026-10-11 (#345):** skill forte tem preparo visível e sonoro e pode ser cancelada; cancelar custa **50% do recurso** (constante nomeada) e o servidor cobra uma vez só (no cancelamento ou na conclusão, nunca nos dois).
- [ ] **Combos** (sugestão do topo do board): uma skill prepara e outra finaliza; segue como proposta (e skills fortes, veja acima). Ficam em [skills.md](skills.md).

## Atributos secundários e pipeline (#624)

Decidido pelo dono da #624 em 2026-10-13, tudo **valor de teste** em `ConfigAtributosSecundarios` (shared) até a [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627). Código: `Features/Combate` no shared (`PipelineDeModificadores`, `RegrasSecundarios`, `RegrasDeSkill`) e `RecursosEmMemoria` no servidor.

- **Os nove:** regeneração de vida, stamina e mana (pontos por segundo); chance crítica (0 a 1); dano crítico (multiplicador, base 1,5); velocidade de ataque físico e mágico (multiplicador, base 1); penetração física e mágica (fração da defesa ignorada, 0 a 1). Cada um tem limites em constantes (teto de crítico 100%, penetração até 75%, velocidade de 0,25 a 3).
- **Modificador:** atributo, fonte (base, personagem, árvore, equipamento, efeito, alvo), origem (texto para o jogador), operação (soma ou multiplica), ordem (inteiro) e grupo de acúmulo (obrigatório; sem grupo, o modificador é recusado).
- **Conta:** o valor começa em 0; os passos entram por ordem crescente. Em cada passo, primeiro todas as somas; depois cada grupo multiplica por `1 + soma dos bônus do grupo`. +10% e +10% no mesmo grupo dão x1,20; em grupos diferentes, x1,21. A entrada em qualquer ordem dá o mesmo resultado (desempate fixo). Ordens de teste: base 0, personagem 10, árvore 20, equipamento 30, efeito 40, alvo 50.
- **Fontes:** a base vem do Combate e nunca de fora. As outras entram pelo contrato `IFonteDeModificadores` (`Contratos/Combate/`): cada fonte declara a sua e devolve o **total atual**; modificador de outra fonte é recusado e registrado. A árvore ([#625](https://github.com/GabrielNied/jogo_do_grupo/issues/625)) e o equipamento ([#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623)) ainda não registram nada; efeito e alvo também não. O atributo principal não gera secundário sem decisão ([personagem](personagem.md#atributos-do-personagem)).
- **Skill:** `PropriedadesDaSkill` declara categoria de dano (física, mágica ou ambas), tipo de efeito (dano, cura, escudo, controle, mobilidade, utilidade, recuperação de recurso) e as flags `PodeCritar`, `RecebePenetracao` e `PodeSerAcelerada`. Sem a flag, a skill não recebe o secundário (crítico 0%, penetração 0, velocidade 1). A flag "recebe bônus de cura" fica para quando a cura entrar.
- **Skill híbrida (`Ambas`):** declara `FracaoFisica` (padrão de teste 0,5). O dano se divide em parte física e parte mágica; cada parte recebe só a penetração e a defesa do seu tipo; o crítico é **um sorteio só por golpe** e multiplica o total; a velocidade é a da categoria dominante (empate: física). Assim nenhum bônus entra duas vezes.
- **Mitigação provisória:** `dano × K / (K + defesa)` (K = 100, teste), só para provar a penetração. A fórmula final de defesa segue na #627.
- **Regeneração:** a de stamina e a de mana passam a usar o valor calculado (base igual à de antes: 10 e 5 por segundo, após 2 s sem gastar). A de vida existe com taxa de teste **0** (desligada até a #627); contínua, sem atraso, só com vida acima de 0 (quem caiu não volta sozinho). O contador de tempo da vida fica só em memória. A previsão local de stamina do client ainda usa a base.
- **Ainda não usa:** o golpe básico (leve e pesado) não sorteia crítico nem usa penetração, porque não é skill; as regras puras ficam prontas para as skills ([#341](https://github.com/GabrielNied/jogo_do_grupo/issues/341)).
- **Na tela:** a janela do personagem (C) mostra os nove com o valor e, embaixo, de onde veio ("Base 5% · Árvore: Nó de teste +10%"). Vêm de `secundarios` na rota de recursos.

## Regras

### Ritmo do golpe (New World)

Decidido em 2026-10-12 e revisto no mesmo dia: a luta segue o New World. A sensação de contato, o corpo que avisa e a pausa no acerto continuam as do Elden Ring ([referências](../referencias.md#elden-ring-fromsoftware-2022)). Os números da cadeia e da esquiva moram na [fatia](fatia-jogavel.md). O que não entra:

- **Parry.** Segurar a guarda não tem janela de timing. Acertar o instante certo não abre contra-ataque.
- **Combo que prende.** A reação ao acerto não renova ([abaixo](#reação-ao-acerto)).
- **Duas armas nesta fatia.** O desenho está [abaixo](#duas-armas-depois-desta-fatia); a espada sozinha vale até lá.

Para implementar sem refazer a leitura:

- A cadeia, o multiplicador do 3º leve e o teto de giro são contas em `RegrasAtaque`. No acerto, a direção é a `RotacaoY` que o Mundo já guarda em `PosicaoNoMundo`, limitada pelo giro desde a direção do início do preparo. Sem mensagem nova e sem biblioteca.
- Andar a 40% durante o golpe **não** usa `IDeslocamentosDoMundo.AbrirJanela`: essa janela libera um deslocamento mais rápido (é a esquiva). O teto menor é outra operação no contrato do Mundo (`Contratos/Mundo/`), chamada pelo Combate. Sem referência de assembly entre as funcionalidades.
- O pesado não é interrompido por dano tomado; o dano continua valendo. A reação dos outros golpes está em [Reação ao acerto](#reação-ao-acerto).

### Guarda segurada

Decidido em 2026-10-12, como no New World, fácil de mudar. Sem parry. Quem decide é o servidor: o client só diz que a guarda está segurada.

- Segurar a guarda anda a no máximo **40%** da corrida e pausa a regeneração da stamina ([fatia](fatia-jogavel.md#1-atributos-e-recursos)). Não vale durante o preparo nem no momento de acerto de um golpe seu. Na recuperação, segurar a guarda corta o que falta dela.
- O golpe recebido custa **1 de stamina por ponto de dano** (`StaminaPorDanoBloqueado`), antes da defesa do equipamento. Com stamina suficiente, o dano desse golpe é zero e a stamina desce esse tanto.
- Sem stamina para o custo, a guarda quebra: a stamina vai a zero, o dano entra (a defesa do equipamento da [#344](https://github.com/GabrielNied/jogo_do_grupo/issues/344) vale nesse caso) e a [reação ao acerto](#reação-ao-acerto) começa.
- A fórmula da #344 não substitui a guarda. Ela só reduz o que passou.

### Duas armas (depois desta fatia)

Decidido em 2026-10-12, opção B da [S10](../brainstorm/validacao.md#s10-troca-de-arma). Não entra na luta da espada.

- Duas armas equipadas. Cada uma tem até **3** skills ativas: as skills sorteadas naquela arma, não uma árvore à parte. A recarga é da arma. Trocar não zera nem avança a recarga da outra.
- A troca não tem recarga própria. Dura **0,4 s** (`DuracaoDaTrocaSegundos`). Nesse tempo não sai golpe, esquiva nem guarda.
- Martelo, arco e cajado reaproveitam cadeia, pesado, esquiva e guarda. O volume do golpe de cada uma espera a tarefa dela.

### O que o client mostra e o que o servidor decide

Decidido em 2026-10-12. A animação, o passo na tela e a resposta no instante do botão são do client. Quem acertou continua sendo o servidor ([arquitetura](../arquitetura.md)). O client manda intenção (ataque, esquiva, mira) e mostra o resultado. Não informa acerto nem erro.

- O servidor não roda clipe nem esqueleto. O instante do contato é a duração do preparo, a mesma constante dos dois lados. O client só ajusta a velocidade do clipe para a pose cair nesse instante. Vale para o golpe do jogador e para o [contato do inimigo](mobs.md#contato-do-golpe).
- A sensação de desviar e tomar dano mesmo assim, hoje, vem do volume não coincidir com o modelo. Com o contato, quem saiu do cone antes do fim do preparo não leva dano, no servidor e na tela.
- O client começa a esquiva e o golpe na hora do botão, sem esperar a resposta. A vida só muda quando o servidor confirma o acerto.
- **Pausa no acerto** (decidido em 2026-10-12): quando a confirmação chega, o client segura a pose de quem bateu e de quem levou por **70 ms** (`PausaVisualNoAcertoMs`). O relógio do servidor não para. Não é invulnerabilidade nem atordoamento.
- **Clipes da espada** (decidido em 2026-10-12): três poses de leve (uma por passo da cadeia), uma de pesado, uma de esquiva, uma de guarda e uma de dano. A pose de contato do leve e do pesado cai no fim do preparo. A pausa de 70 ms segura essas poses. Clipe que faltar fica com o `artista-personagens`; o dono de Combate liga o tempo.
- Se a mensagem da esquiva chegar depois que o servidor já resolveu o golpe, o dano vale. Rebobinar o mundo até o instante do botão é a compensação de latência, fora desta fatia ([#172](https://github.com/GabrielNied/jogo_do_grupo/issues/172)).
- O teste de cone nesse instante não aumenta a carga: o servidor já dá o passo da IA dez vezes por segundo, e a animação não entra nesse passo.

### Reação ao acerto

Decidido em 2026-10-12, no exemplo do Elden Ring, fácil de mudar. Vale igual para PvE e PvP: um combo não prende o alvo. O dano continua valendo. Quem decide a interrupção é o servidor; o client só toca a pose.

- Um acerto interrompe a ação de um personagem ou de um inimigo comum, exceto o pesado do jogador já em andamento ([fatia](fatia-jogavel.md#2-um-ataque-primeira-arma-leve-e-pesado)) e qualquer ataque do chefe de dungeon.
- Quem foi interrompido não começa esquiva nem ataque por **0,3 s** (`AtordoamentoNoAcertoMs`). A pausa visual de 70 ms cabe dentro dessa janela.
- Outro acerto nesse meio **não renova** nem soma o tempo. Quando os 0,3 s acabam, há **0,6 s** de folga (`FolgaDepoisDoAcertoMs`): o dano entra, mas não interrompe e não começa outra reação. A cadeia de leves não segura o jogador além do golpe que ele já começou.
- No inimigo comum, o preparo cancelado não chega ao contato. A recuperação de um golpe que já encostou continua. O alvo fugir não cancela ([mobs](mobs.md)). Durante a folga, o preparo dele também não é cancelado.
- O chefe não tomba por acerto. A punição dele continua a janela em que fica exposto.

## Depende de

## Expõe

Código em `shared/Package/Runtime/Features/Combate/`. Regras e números: [fatia-jogavel.md](fatia-jogavel.md), seção 1 (não copiados aqui).

- `AtributosCombate`: os cinco atributos finais (Força, Vigor, Agilidade, Intelecto, Espírito). Os valores de teste que viram recursos estão em `RegrasRecursos` (`VidaPorVigor`, `StaminaPorAgilidade`, `ManaPorEspirito`).
- `RegrasRecursos`: fórmulas puras de vida, stamina e mana máximos, `Limitar` (clamp) e regeneração (`RegenerarStamina`, `RegenerarMana`).
- `RecursosResponse`: atuais, máximos e atributos finais.
- `RotasCombate.Recursos`: `GET /api/personagens/{personagemId}/recursos` responde 200 `RecursosResponse`. Erros: `CodigosErroCombate.PersonagemNaoEncontrado` (404) e `NaoAutenticado` (401, Core).

Ataque da primeira arma (tempo real; regras e números em [fatia-jogavel.md](fatia-jogavel.md), seção 2):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 6000 | `IntencaoDeAtaque` | client → servidor | `Tipo` (`TipoDeAtaque`: Leve 0, Pesado 1), `DirecaoY` (graus, mesma convenção de `IntencaoDeMovimento.RotacaoY`), `Sequencia` |
| 6001 | `AtaqueIniciado` | servidor → quem está na área | `AtacanteId`, `Tipo`, `DirecaoY`, `PreparoMs`, `Passo` (0 a 2, da cadeia de leves) |
| 6002 | `AtaqueAcertou` | servidor → quem está na área | `AtacanteId`, `Acertos[]` (`AcertoDeAtaque`: `AlvoId`, `Dano`, `VidaRestante`, `VidaMaxima`), `Tipo`, `Sequencia` (a da `IntencaoDeAtaque`), `Passo` |
| 6003 | `AtaqueRecusado` | servidor → atacante | `Sequencia`, `Codigo` (`CodigosErroCombate.StaminaInsuficiente`, `AcaoEmAndamento` ou `VoceEstaCaido`) |
| 6004 | `RecursosMudaram` | servidor → dono | vida, stamina e mana atuais e máximos |

- `RegrasAtaque`: constantes de balanceamento do ataque e fórmulas puras (`PreparoSegundos`, `RecuperacaoSegundos`, `DuracaoSegundos`, `CustoDeStamina`, `TemStamina`, `Dano`, `EntraNaFila`, `EstaNoVolume`). O servidor decide; o client usa só para tocar a animação no tempo certo.
- ~~Os IDs 6010 a 6012 ficam reservados para a esquiva (#62).~~ Substituído em 2026-10-11: o contrato da esquiva está abaixo.

Esquiva (tempo real; regras e números em [fatia-jogavel.md](fatia-jogavel.md), seção 3):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 6010 | `IntencaoDeEsquiva` | client → servidor | `DirecaoY` (graus, mesma convenção do ataque), `Sequencia` |
| 6011 | `EsquivaRecusada` | servidor → dono | `Sequencia`, `Codigo` (os mesmos códigos do ataque: `StaminaInsuficiente`, `AcaoEmAndamento`, `VoceEstaCaido`) |
| 6012 | `EsquivaIniciada` | servidor → quem está na área | `PersonagemId`, `DirecaoY`, `DuracaoMs` |

- `RegrasEsquiva`: constantes de balanceamento (5 m, 0,6 s, 15 de stamina) e fórmulas puras (`TemStamina`, `EmAndamento`, `DistanciaPermitidaAte`, `DeslocamentoCabe`, `Destino`). A posição continua chegando pelas mensagens do Mundo.

### Ataque no servidor (#215)

- Quem recebe: `AtaqueIniciado` e `AtaqueAcertou` vão a todos que o Mundo diz estarem na área do atacante (`IPosicoesDoMundo`); `AtaqueRecusado` só ao atacante; `RecursosMudaram` só ao dono, e só quando o ataque gasta stamina (o pesado; o leve não gasta nada).
- O servidor mede o cone no **momento do acerto** (preparo depois do pedido) com a posição que o Mundo tem então; não há compensação de lag. A direção é a `RotacaoY` que o Mundo tem do personagem naquele instante, limitada pelo giro desde a direção do pedido (`RegrasAtaque.DirecaoDoGolpe`: leve 240°/s, pesado 60°/s, pelo menor caminho). O client vira o corpo para a mira dentro do mesmo teto (#571).
- **Um relógio só (#477):** a ação nasce no pedido; o acerto sai em `InicioMs + PreparoMs` e o fim da ação vem depois do acerto, mesmo que o banco demore. A cobrança da stamina e a leitura da Força acontecem dentro do preparo (o acerto não volta ao banco; veja [Recursos em memória](#recursos-em-memória-478)), e a fila só avança depois do acerto resolvido. Banco mais lento que o preparo atrasa o acerto, mas não o perde.
- Fila: um pedido feito nos últimos 0,3 s da ação espera e começa no fim dela (stamina e queda são conferidos nessa hora, e a recusa sai com a `Sequencia` dele). Pedido durante a ação fora da janela, ou com a fila ocupada, é recusado com `AcaoEmAndamento`.
- Caído (vida 0) é recusado com `VoceEstaCaido`; o pesado gasta 20 de stamina uma vez só (trava do personagem em memória) e é recusado sem stamina.
- `Tipo` fora do enum, direção inválida, ou ataque de quem ainda não entrou no mundo: o pedido é **descartado sem resposta** (o contrato não tem código para isso; um client correto nunca manda).
- O dano é `RegrasAtaque.Dano(tipo, Força final, passo)`; cada alvo leva o mesmo golpe uma vez, e o `Dano` de `AcertoDeAtaque` é o que o alvo realmente perdeu (nunca mais que a vida que ele tinha).
- **Alvos:** `IAlvosDeCombate` (`server/.../Contratos/Combate/`, ADR 0016) é implementado por quem é dono de alvos; Combate junta todas as implementações. Hoje só há o **boneco de treino** (id de alvo `-1`), em `ConfigBonecoDeTreino`: 180 de vida, posição fixa na cidade (x 0, z -22, 2 m à frente do nascimento), volta à vida cheia 5 s depois do último golpe que levou. Com 0 de vida ele sai de combate (não é mais alvo) até voltar. O servidor **não avisa** a volta: o client deduz pelos 5 s sem golpes.
- **Onde o boneco está:** `RegrasBonecoDeTreino` no shared (posição, raio, vida e recuperação), usado pelo servidor e pelo client; ver [Resposta visual](#resposta-visual-do-combate-no-client-475-476-479-480-482).

### Recursos em memória (#478)

- Vida, stamina e mana de quem está em uso ficam em memória (`RecursosEmMemoria`, o único que escreve em `RecursosPersonagem`). O banco é lido uma vez por personagem (na primeira consulta) e recebe o que mudou a cada 5 s, quando o personagem desconecta e quando o servidor para; um crash perde no máximo ~5 s de gasto. O golpe leve (custo 0) não toca no banco; golpe, esquiva, dano, queda e a rota de recursos passam pela mesma trava por personagem.
- Os atributos finais (Itens + efeitos + pontos) ficam em memória por 2 s e depois são relidos. Itens ainda não publica um evento de troca de equipamento; por isso trocar de peça aparece no golpe seguinte depois de até 2 s (a rota de recursos sempre relê). Quando o evento existir, ele invalida a entrada na hora. Valores em `ConfigRecursos`.

### Golpe de inimigo tira vida (#278)

- Combate, dono da vida, assina `GolpeDeInimigoAcertou` (`Contratos/Mobs/`, ADR 0016). Mobs decide quem estava no círculo; um evento por personagem atingido.
- A vida cai em `Dano` (sem defesa por enquanto; a conta fica em `ServicoDeDanoRecebido.DanoFinal`, onde a defesa entra depois), **nunca abaixo de 0**, e o dono recebe `RecursosMudaram` (só ele). Vida 0 leva ao estado caído ([abaixo](#estado-caído-no-servidor-125)); quem já está caído não recebe dano (o golpe é ignorado).
- O assinante não bloqueia (o evento sai do laço de IA de Mobs): a conta roda numa tarefa, sob a mesma trava dos outros recursos, então golpes simultâneos no mesmo personagem somam. Personagem sem recursos criados começa da vida cheia; um que não existe só registra erro no log.

### Ritmo do golpe no servidor (#569, #571, #570, #578)

Implementado em 2026-10-12. As decisões que a doc não fechava:

- **Cadeia (#569):** o `ControleDeAcoes` guarda, por personagem, o próximo passo e o fim da ação anterior. O passo sobe quando a ação **começa** (errar o alvo não quebra); o pesado vale o passo 0 e reinicia a cadeia; o 4º leve volta ao 1º. Pedido da fila continua a cadeia. Pesado recusado por falta de stamina não quebra a cadeia. `AtaqueIniciado.Passo` (0 a 2) vai ao client; o 3º leve e o pesado mostram o número de dano maior. As animações por passo continuam em [#475](https://github.com/GabrielNied/jogo_do_grupo/issues/475).
- **Janela da cadeia:** conta do fim **real** da ação. Se a esquiva cortou a recuperação, o fim é o instante do corte, então o golpe depois de uma esquiva (0,6 s) ainda continua a cadeia.
- **Giro (#571):** o corpo do client começa virado para a mira no clique e, até o acerto, gira para a mira dentro do teto. O servidor repete a conta com a `RotacaoY` que o Mundo recebeu. A direção do pedido continua sendo o ponto de partida.
- **Compromisso (#570):** a esquiva é recusada antes do `AcertoMs` (preparo e momento de acerto) e vale depois dele; ao valer, a ação ganha `CortadaEmMs`, o pedido da fila some e o fim da ação passa a ser o corte. Se a esquiva for recusada por falta de stamina, a recuperação volta como era. O client anda a 40% da corrida (`VelocidadeDuranteGolpe`) do clique ao acerto, mexendo só no `Movimento` do próprio jogador e devolvendo o valor depois.
- **Teto de velocidade no servidor:** o contrato `ILimiteDeVelocidadeDoMundo` (`Contratos/Mundo/`) existe e o Combate o chama no começo do preparo, mas o **Mundo ainda não o implementa**. Sem implementação, o servidor não corta a velocidade de um client que ignore os 40%. Pendente no responsável pelo Mundo (`ValidadorDeMovimento`/`CidadeDoMundo`).
- **Esquiva sem dano (#578):** no `TentarOcupar` o `ControleDeAcoes` marca a invulnerabilidade até `JanelaDeInvulnerabilidadeSegundos` (0,3 s). O `ServicoDeDanoRecebido` ignora o `GolpeDeInimigoAcertou` que chegar nesse intervalo, pelo relógio do servidor. Recusa por stamina desfaz a janela. Golpe de dano em alvos não-jogador não muda.
- **Direção da esquiva (#578):** andando, a do movimento; parado, para trás da mira (`RegrasEsquiva.DirecaoDaEsquiva`). Antes o client saía para onde o corpo olhava quando parado.

### Reação ao acerto e guarda no servidor (#577, #579)

Implementado em 2026-10-12, a partir do ritmo do golpe. Mensagens (faixa de Combate): `IntencaoDeGuarda` 6030 (client → servidor, `Segurando`), `GuardaMudou` 6031 (`PersonagemId`, `Segurando`, `Quebrou`; vai à área e ao dono quando sobe ou quebra, e só ao dono quando a guarda é recusada; quem solta a guarda não recebe o eco), `GolpeBloqueado` 6032 (`PersonagemId`, `StaminaGasto`) e `AcaoInterrompida` 6033 (`AlvoId`, `Jogador`, `DuracaoMs`). Números em `RegrasReacao` e `RegrasGuarda` (shared). As decisões que a doc não fechava:

- **Quem interrompe:** o golpe de inimigo que entra (depois de esquiva e guarda) chama `ControleDeAcoes.TentarReagir`. O golpe leve em curso do jogador para: no preparo ele não chega ao contato (`FoiInterrompida`), na recuperação a ação é cortada. O pesado em andamento não é interrompido (e não atordoa). Acerto durante a esquiva não interrompe: a esquiva termina.
- **Inimigo comum:** Combate chama `IAlvosDeCombate.Interromper` (método novo no contrato, padrão falso) depois de um acerto confirmado; só `GrupoDeInimigos` o implementa. Acerto no preparo cancela o golpe e o inimigo fica 0,3 s sem atacar; acerto na recuperação não muda nada. O chefe de dungeon e o boneco usam o padrão e nunca são interrompidos. A folga dos inimigos fica em `ServicoDeAtaques`, e só começa se um preparo foi cancelado.
- **Folga:** vale para tudo o que `TentarReagir` e `Interromper` controlam. Um acerto na reação ou nos 0,6 s seguintes entra, mas não interrompe nem começa outra reação. Quebra de guarda na folga também não interrompe: o combo não prende.
- **Guarda bloqueada não interrompe.** Só a quebra (e o acerto sem guarda) começa a reação.
- **Ordem do golpe de inimigo:** esquiva (dano some) → caído → guarda (custo 1 de stamina por ponto de dano, antes da defesa da #344) → vida → reação.
- **Guarda:** `ControleDeAcoes` recusa subir no preparo e no momento de acerto, durante a esquiva e a reação; na recuperação corta o resto do golpe. Com a guarda segurada, ataque e esquiva são recusados (o client solta a guarda antes de pedir). O teto de 40% vai ao Mundo por `ILimiteDeVelocidadeDoMundo` (30 s por vez, `RegrasGuarda.LimiteDeVelocidadeSegundos`; ao soltar, fator 1); o Mundo ainda não o implementa (#598).
- **Regeneração:** com a guarda segurada a espera de 2 s da stamina não corre; ao soltar ou quebrar ela recomeça (`RecursosEmMemoria.DefinirGuardaAsync`).
- **Client:** o clique no botão esquerdo é o leve e segurá-lo por 0,3 s (`SegurarParaPesadoSegundos`) é o pesado, para o botão direito ser a guarda ([interface.md](interface.md#teclas-e-atalhos)). Segurar o direito sobe a guarda (corpo olhando a mira, 40%, indicador "Guarda"); atacar ou esquivar a solta antes do pedido. `AcaoInterrompida` do próprio personagem para o golpe, segura a próxima ação por 0,3 s e inclina o corpo; os parâmetros `Guarda` e `Interrompido` do Animator tocam a pose de guarda e a de dano (ver [Resposta visual](#resposta-visual-do-combate-no-client-475-476-479-480-482)). O visual do inimigo interrompido é de Mobs (hoje ele só deixa de dar o golpe).

### Esquiva no servidor (#233)

- O servidor recebe `IntencaoDeEsquiva`, confere e cobra **15 de stamina uma vez** (mesma transação do ataque) e só então pede ao Mundo a janela de deslocamento (`IDeslocamentosDoMundo`: 5 m em 0,6 s). Quem valida cada posição continua sendo o Mundo; fora da janela vale o limite de corrida.
- Quem recebe: `EsquivaIniciada` vai a todos que estão na área (o dono também); `EsquivaRecusada` e `RecursosMudaram` só ao dono.
- Recusas: `StaminaInsuficiente` (nada é gasto), `VoceEstaCaido` e `AcaoEmAndamento`.
- **Ataque e esquiva:** a esquiva é recusada no preparo e no momento de acerto, e durante outra esquiva. Na recuperação, a esquiva corta o resto da ação. O ataque pedido durante a esquiva é recusado com `AcaoEmAndamento`, sem entrar na fila. De dois pedidos no mesmo instante, um vale. A #62 já está fechada.
- Direção inválida ou esquiva de quem ainda não entrou no mundo: o pedido é descartado sem resposta, como no ataque.
- Parede: o servidor ainda não conhece a geometria; valida distância, velocidade e os limites do mundo. Colisão com parede fica no client até o servidor ter o mapa.

### Estado caído no servidor (#125, subtarefa #423)

Combate é o dono do estado caído (decidido em 2026-10-11). As regras e os casos de borda estão em [personagem.md](personagem.md#morte-e-caído-issue-97) e em [fatia-jogavel.md](fatia-jogavel.md) (seção 7); aqui só o contrato e o que o servidor faz.

| ID | Nome | Direção | Campos |
|---|---|---|---|
| 6020 | `ComecarALevantar` | client → servidor | `AlvoId` |
| 6021 | `CancelarLevantar` | client → servidor | (vazio) |
| 6022 | `VoltarParaACidade` | client → servidor | (vazio) |
| 6023 | `EstadoDeQueda` | servidor → caído, ajudante e quem está na área | `PersonagemId`, `Estado` (`FaseDeQueda`: `Vivo`=0, `Caido`=1, `Levantando`=2, `Retorno`=3), `SegundosRestantes`, `AjudanteId`, `ProgressoMs` |
| 6024 | `AcaoRecusada` | servidor → quem pediu | `Codigo`: `VoceEstaCaido`, `AlvoNaoEstaCaido`, `LongeDemais`, `JaEstaSendoLevantado`, `NaoEstaCaido`, `AindaNaoPodeVoltar` |

- **Números** em `RegrasQueda` (shared): 45 s caído, 3 s para levantar, 20% da vida, 2 m (2,0 vale), 5 s até poder voltar. O servidor lê os tempos de `ConfigQueda` (padrão = `RegrasQueda`; os testes de integração usam tempos curtos).
- **Quando cai:** vida 0 depois de um golpe de inimigo. `EstadoDeQueda` com `Caido` vai ao dono, ao grupo e à área; `PersonagemCaiu` é publicado em `Contratos/Combate/`.
- **Levantar:** `Levantando` com `AjudanteId` e `ProgressoMs`; ao completar, `Vivo` com 20% da vida (stamina e mana como estavam), `RecursosMudaram` ao dono e o evento `PersonagemLevantou`. O servidor confere o alcance a cada 100 ms: sair dos 2 m, soltar, levar dano, cair, atacar, esquivar ou desconectar interrompem e zeram o progresso (volta `Caido`).
- **Voltar à cidade:** fim dos 45 s ou `VoltarParaACidade` (depois de 5 s). Vida, stamina e mana vão ao máximo, `Retorno` é enviado e o evento `PersonagemVoltouParaACidade` é publicado: **quem leva o personagem ao ponto de nascimento é o Mundo** (ainda não assina o evento; veja abaixo). Nada de item, XP, moeda ou nível muda.
- **Uma saída por queda:** levantar, fim do tempo e voltar disputam a mesma trava (`ControleDeQueda`); a primeira que vence ganha e as outras viram `NaoEstaCaido` ou `AlvoNaoEstaCaido`. Se a interação termina no mesmo ms do fim do tempo, levantar vence.
- **Contagem não para** enquanto alguém levanta, e **não há** os 2 s sem dano depois de levantar (os dois seguem em aberto em [personagem.md](personagem.md#em-aberto)).
- **Para outras funcionalidades (ADR 0016):** `IEstadoDeQueda.EstaCaido(id)` (Mobs deve ignorar quem está caído: não atacar nem contar como alvo) e os eventos `PersonagemCaiu`, `PersonagemLevantou` e `PersonagemVoltouParaACidade`, em `server/.../Contratos/Combate/`.
- **Pendências de outras funcionalidades:** Social ainda não expõe o grupo em `Contratos/` (hoje qualquer jogador vivo a até 2 m levanta e o aviso vai a toda a área; TODO em `ServicoDeQueda`); Mundo ainda não assina `PersonagemVoltouParaACidade` para mover ao nascimento; Mobs ainda não consulta `IEstadoDeQueda`.

### Sensação do acerto no client (#453)

O client só reage ao que o servidor confirmou (`AtaqueAcertou`, vida menor em `RecursosMudaram`, `EsquivaIniciada`, `InimigoPreparaAtaque`); nada aparece por previsão de dano. Código em `Features/Combate/Scripts/` (`EscolhaDeEfeito`, `ControleImpacto`).

- **Golpe leve:** flash curto e recuo pequeno no inimigo, som baixo. **Pesado:** flash e recuo maiores, hit-stop de 0,07 s e tremor de câmera leve.
- **Golpe de outro jogador** no mesmo inimigo: só flash curto e som baixo (sem hit-stop, recuo nem tremor), para não poluir a tela.
- **Golpe recebido** (vida que caiu): moldura vermelha, flash no personagem, tremor leve e som; se tirou 20% ou mais da vida máxima, também hit-stop e tremor maior.
- **Limites:** no máximo 8 flashes e 4 sons por janela de 0,12 s; hit-stop e tremor não se somam (vale o maior, com teto).
- **Esquiva:** som ao esquivar. "Esquivou!" aparece quando o jogador estava no círculo de um golpe avisado, esquivou antes do golpe, estava fora quando o preparo acabou e o servidor não tirou vida. Hoje é inferido; uma mensagem do servidor para "esquivou" deixaria exato. Estar dentro de um círculo avisado mostra "Saia da área!" com um som de alerta.
- **Sons:** gerados por código (`SintetizadorDeSons`), sem arquivos de terceiros. Entram no mixer de Core quando o #123 existir. Tremor, hit-stop e som têm chaves em `ConfiguracaoDeImpacto`, a ligar na tela de opções do Core.

### Resposta visual do combate no client (#475, #476, #479, #480, #482)

Decidido em 2026-10-12 pelo dono de Combate, fácil de mudar (constantes nomeadas em `RegrasAtaque`, `RegrasReacao`, `ProntidaoLocal` e `RastroDoGolpe`). Código em `Features/Combate/Scripts/`. O client só mostra; nada aqui muda dano, custo ou acerto.

- **Contrato novo:** `AtaqueAcertou` ganhou `Tipo`, `Sequencia` e `Passo` no fim (o acerto é do golpe certo mesmo com leve e pesado na fila). `RegrasBonecoDeTreino` (shared) guarda posição, raio, vida e tempo de recuperação do boneco; `ConfigBonecoDeTreino` só repete os nomes. `RegrasAtaque` ganhou `JanelaDoBufferSegundos`, `MargemDoEnvioSegundos`, `VelocidadeDoClipe` e `QuandoEnviarOCliqueGuardado`; `RegrasReacao`, `PausaVisualNoAcertoMs` (70).
- **Animações (#475):** golpe, guarda e dano tocam numa **camada de tronco** (`Golpe`) do controller; as pernas seguem o movimento. Três leves (um por `Passo` da cadeia, o 2º espelhado), um pesado, a guarda enquanto segurada, o dano no `Interrompido` e o caído (`Caido`). O contato do clipe `Atacar` (quadro 11 de 26 a 30 por segundo) cai em `RegrasAtaque.PreparoSegundos` pela velocidade do clipe (`VelocidadeDoGolpe`). O passo local segue `RegrasAtaque.PassoDoGolpe` e o `AtaqueIniciado` do servidor corrige. O blend tree corre a 5,5 m/s (o do Core), com o clipe a 1,1x. Tabela dos parâmetros em [personagem.md](personagem.md#parâmetros-do-animator).
- **Clipes que faltam (fica com o `artista-personagens`):** a guarda usa o quadro de preparo do `Atacar` parado, e o 2º leve é o `Atacar` espelhado. Trocar por clipes de verdade é só apontar o estado do controller no gerador; o tempo já está ligado.
- **Pausa no acerto (70 ms):** no acerto confirmado o `Animator` de quem bateu e de quem levou fica parado por `PausaVisualNoAcertoMs`, junto do hit-stop que já existia. O relógio do servidor não para.
- **Boneco (#476):** `BonecoDeTreinoVisual` monta em código um poste com tronco e cabeça de palha na posição do shared, sobre o chão, com barra de vida que olha para a câmera. A vida vem de `AtaqueAcertou` e a volta à vida cheia (`SegundosParaRecuperar`) é a mesma conta do servidor. Flash e recuo são do `ControleImpacto`, que acha o alvo pelo `RegistroDeAlvos` (o boneco se registra; os inimigos de Mobs são achados uma vez por nome e guardados).
- **Número de dano (#482):** nasce sobre a cabeça do alvo, no mundo, e sobe 0,9 m; o pesado e o 3º leve aparecem maiores. Sem o alvo na cena, volta a subir em torno da mira.
- **Golpe no vazio:** o golpe do próprio jogador que o servidor não confirmou até o contato mais 0,25 s (`GolpesEmAberto`) mostra o leque do volume do golpe no chão por 0,22 s e um som de ar cortado. Um acerto sofrido no meio cancela o aviso.
- **Stamina sem polling (#479):** uma consulta ao entrar traz o retrato; depois o servidor manda `RecursosMudaram` a cada gasto, dano e **ao subir ou soltar a guarda**, e a barra e a `ProntidaoLocal` preveem a regeneração de `RegrasRecursos.RegenerarStamina` (espera de 2 s depois do último gasto, parada com a guarda segurada). Um retrato do servidor abaixo da previsão por mais de 2 pontos conta como gasto. Nenhum "Stamina insuficiente" local quando a previsão paga.
- **Buffer de entrada (#480):** um clique de ataque (ou esquiva) durante outra ação: nos últimos 0,3 s vai já ao servidor (entra na fila dele) e a animação começa quando a ação em curso acaba; entre 0,3 s e 0,5 s fica guardado (**1 pedido**) e sai 0,25 s antes do fim, para a latência não fazer o servidor vê-lo cedo; antes disso é descartado ("Aguarde o golpe terminar"). Um pedido guardado vence 0,25 s depois da hora de sair. A esquiva guardada sai quando pode começar (no fim do preparo do golpe ou da esquiva anterior).
- **Golpe recusado desfeito:** `AtaqueRecusado` com a `Sequencia` do golpe em curso tira a pose (trigger `Cancelar`), devolve a stamina prevista e a cadeia de leves; a recusa do pedido que ainda estava na fila só o tira da fila.
- **Fora desta entrega:** compensação de lag ([#172](https://github.com/GabrielNied/jogo_do_grupo/issues/172)), o teto de velocidade no servidor ([#598](https://github.com/GabrielNied/jogo_do_grupo/issues/598)) e o mob reagindo à interrupção (Mobs).

### Golpes e esquivas de outros jogadores (#481)

Decidido em 2026-10-13 pelo dono de Combate. O `AtaqueIniciado` e o `EsquivaIniciada` de outro personagem tocam no avatar dele (`AnimacaoDeOutros`): vira o corpo para a `DirecaoY`, dispara o Trigger do Animator (os mesmos animadores do próprio jogador) e o `ControleImpacto` toca o som a `VolumeDeOutros` (0,2, contra 0,5 a 0,8 dos seus). O próprio jogador não passa por aqui (já animou ao apertar, ADR 0024). Sem número de dano, flash ou rastro para golpe alheio.

- **Achar o avatar:** o Mundo ainda não publica um contrato id do personagem para visual. Até lá o Combate usa a convenção de nome que o Mundo já cria (`Jogador_<id>`, mesmo padrão do `Inimigo <id>` em `RegistroDeAlvos`), atrás de `AnimacaoDeOutros.Buscar`, que troca por um contrato sem mexer no resto.
- **Limite atual:** o avatar remoto do Mundo é uma cápsula sem Animator, então só o giro e o som aparecem até o Mundo trocá-la pelo modelo animado das raças (issue do modelo dos outros jogadores). O Trigger já toca assim que houver Animator com os parâmetros da [tabela](personagem.md#parâmetros-do-animator). O Mundo reaplica a `RotacaoY` do servidor a cada quadro, então o giro do golpe dura só até a próxima posição.

### Sem dano entre aliados (#346)

Decisões de design (2026-10-11, do agente da funcionalidade; ajustáveis):

- **Regra:** um golpe ou skill de área nunca acerta o **próprio atacante** nem **aliados**. Aliado = mesmo grupo ([social.md](social.md)). Boneco, inimigos, chefes e jogadores de fora do grupo continuam alvos válidos (PvP é futuro; hoje nenhum jogador é alvo).
- **Onde:** `SelecaoDeAlvos.PodeAcertar` filtra os alvos do volume antes do dano em `ServicoDeAtaques`; vale para qualquer skill que use `IAlvosDeCombate`. `AlvoDeCombate.PersonagemId` (0 = não é jogador) diz que o alvo é um jogador.
- **Sem referência a Social (ADR 0016):** Combate consulta `IAliadosDeCombate.SaoAliados` (`Contratos/Combate/`); sem implementação vale `NinguemEAliado`. **Pendência de Social:** implementar a interface sobre `GruposDoServidor` (consulta por demanda, sem evento nem cache: reflete o grupo na hora do acerto). Serve também ao TODO de levantar só aliado (queda).
- **Fora do escopo:** cura e buffs em aliados.

### Ameaça (agro) (#169)

Adotado em 2026-10-12 pelo loop autônomo (/decida), a partir da proposta da issue; **fácil de mudar**: todos os números são constantes nomeadas. Base: World of Warcraft (tabela de ameaça, margem de troca de alvo, provocar) e New World (provocar que força o alvo por alguns segundos). RYL (S3) fica para as manadas: puxar muitos inimigos continua possível, porque a tabela não limita quantos perseguem um jogador.

Quem decide: **servidor**. O dono é [Mobs](mobs.md), que já escolhe o alvo; Combate só informa quem causou dano (já informa em `IAlvosDeCombate.TentarAplicarDano`) e, quando existir cura, quem curou. O client continua vendo só o `AlvoId`.

| Regra | Valor (proposta) | Constante sugerida |
|---|---|---|
| Ameaça por ponto de dano real (sem o excedente da morte) | **1** | `AmeacaPorDano` |
| Ameaça por ponto de cura real (sem curar além do máximo), dividida entre os inimigos em combate com o curado | **0,5** | `AmeacaPorCura` |
| Multiplicador de ameaça de quem usa item de tank (escudo) | **3x** | `MultiplicadorDeAmeacaTank` |
| Troca de alvo quando o outro está ao alcance de ataque do inimigo | passa de **110%** da ameaça do alvo atual | `MargemDeTrocaPerto` |
| Troca de alvo quando o outro está fora do alcance | passa de **130%** | `MargemDeTrocaLonge` |
| Provocar: alvo forçado | **4 s** | `DuracaoDaProvocacao` |

- **Tabela por inimigo**, não por grupo de inimigos: cada um guarda a ameaça de cada jogador. O grupo de inimigos continua acordando junto (o "grupo divide o que vê" de [mobs.md](mobs.md)).
- **Primeiro alvo:** como hoje, o jogador mais próximo que ele viu. Enquanto ninguém tem ameaça nele, vale a regra antiga de não trocar.
- **Com ameaça:** o alvo é quem tem mais ameaça, mas o inimigo só troca quando outro passa da margem (110% perto, 130% longe). Empate fica com o alvo atual; entre outros, o de menor id. Isso evita que ele fique pulando de alvo.
- **Provocar** (skill de item de tank, [skills.md](skills.md)): o inimigo vira para quem provocou na hora, e a ameaça dele passa a ser igual à maior da tabela. Durante os 4 s o alvo não troca, nem por margem.
- **Sai da tabela** quem cai, sai da área ou desconecta. A tabela **zera** quando o inimigo desiste e volta para casa (junto com a vida e os participantes) e quando ele reaparece.
- **Sem decaimento** com o tempo nesta versão.
- **Chefe:** usa a mesma tabela no lugar do "mais próximo" (`ChefeDaDungeon`). Ataques de área do chefe (investida) continuam mirando como estão descritos em [mobs.md](mobs.md).
- **Ainda sem efeito:** cura e item de tank não existem; o multiplicador e a ameaça de cura entram quando chegarem ([#170](https://github.com/GabrielNied/jogo_do_grupo/issues/170) e [itens.md](itens.md)). Até lá só o dano conta, o que já faz o jogador que mais bate segurar o inimigo.
- **Implementação sugerida:** contas puras em `RegrasAmeaca` no shared de Mobs (somar ameaça, escolher alvo com margem, provocação), como já é `RegrasInimigo.EscolherAlvo`. Código próprio, sem biblioteca: é uma tabela pequena por inimigo.
- **Testes esperados:** quem bate mais vira o alvo só depois de passar a margem (110% perto, 130% longe); empate não troca; tank com 3x segura o alvo contra quem dá o dobro de dano; provocar vira o alvo na hora e segura por 4 s; caído e quem saiu da área saem da tabela; desistir e reaparecer zeram a tabela; dano além da vida que restava não conta.

### Efeitos de status (#171)

Adotado em 2026-10-11 (/decida), a partir da proposta da issue; é valor inicial, calibrável em constantes nomeadas ([valores de balanceamento](../convencoes.md#valores-de-balanceamento)). O servidor aplica, acumula e expira; o client só mostra o ícone e o tempo restante.

| Efeito | Regra | Acúmulo |
|---|---|---|
| Lentidão | −30% de velocidade | não acumula; reaplicar renova a duração |
| Atordoado | não age; no máximo 2 s; depois **6 s de imunidade** a atordoar | não acumula |
| Sangramento | dano por segundo | até 3 acúmulos |
| Queimadura | dano por segundo | até 3 acúmulos |
| Vulnerável | +15% de dano recebido | não acumula |
| Fortalecido | +15% de dano causado | não acumula |

- Cada efeito tem duração própria, em constante nomeada.
- **Bosses são imunes a atordoado:** em vez disso acumulam uma barra de **quebra**; ao encher, abre a janela de vulnerabilidade da [fatia jogável](fatia-jogavel.md).
- Testes esperados: duração e acúmulo de cada efeito, imunidade após o atordoado, boss não atordoa e enche a barra de quebra.
