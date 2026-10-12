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
  - **Esquiva (#62):** o dono do código é **Combate** (o vigor é dele); a tecla é o Alt esquerdo ([interface.md](interface.md)).
  - **Caído (#125):** o dono do estado é **Combate**, que já é dono da vida; [Personagem](personagem.md#morte-e-caído-issue-97) só mostra a tela.

## Em aberto

- [x] ~~Combate com alvo fixo (tab target) ou mira livre (action)?~~ Decidido: action (D4)
- [x] ~~Ataque leve e pesado, bloqueio, stamina: quais entram na fatia jogável?~~ Decidido em 2026-10-12: entram a cadeia de três leves, o pesado e o vigor (esquiva e pesado). O bloqueio da fatia continua o da defesa do equipamento ([abaixo](#ritmo-do-golpe-new-world)), não a guarda segurada.
- [ ] **Troca de arma** (duas armas, como New World e Throne and Liberty)? Veja S10 na [validação](../brainstorm/validacao.md)
- [x] ~~Bosses com ataques telegrafados?~~ Decidido em 2026-10-10: sim (veja "Definido")
- [ ] Como o servidor valida acertos com a latência (lag compensation)? Decidido pelo Gabriel em 2026-10-11 **para a fatia**: sem compensação de lag (o cone é medido no momento do acerto, com a posição que o servidor tem). Depois da fatia a pergunta segue aberta (issue #172).
- [x] ~~Quais efeitos de status existem?~~ Adotado em 2026-10-11 (/decida, #171), fácil de mudar: seis efeitos, veja [Efeitos de status](#efeitos-de-status-171).
- [x] ~~Como funciona o agro (ameaça) para o tank segurar os inimigos?~~ Adotado em 2026-10-12 (/decida, #169), fácil de mudar: tabela de ameaça por inimigo, veja [Ameaça (agro)](#ameaça-agro-169).
- [ ] Quais buffs cada papel traz? Eles acumulam?
- [ ] Um jogador pode trocar de papel trocando de itens no meio da dungeon?

### Propostas do livro de Asterra (2026-10-11, não validadas)

Vêm do [Livro de ideias](../brainstorm/figjam.md) (seção 10). São **proposta do board (2026-10-11), não validada pelo grupo**; nada aqui muda o que já está em "Definido" ou implementado.

- [x] **Bloqueio por defesa, adotado em 2026-10-11 (/decida, #344), fácil de mudar:** sem parry nem janela de timing. Defesa >= 1,5x a potência de bloqueio do golpe anula o dano direto; abaixo disso reduz proporcionalmente, até 80%. Os dois números ficam em constantes nomeadas. O client avisa defesa insuficiente. Proposta original do livro: (sem parry nem janela de timing): com defesa de pelo menos 1,5x a potência de bloqueio do golpe, anula o dano direto; abaixo disso reduz proporcionalmente, até 80%. Responde em parte à pergunta de bloqueio acima. Depende da defesa (equipamento, [itens.md](itens.md)); o dano recebido hoje não tem defesa (`ServicoDeDanoRecebido.DanoFinal`).
- [x] **Sem defesa de timing, adotado em 2026-10-11 (#344):** a "defesa que abre oportunidade" fica de fora. O livro rejeita parry e bloqueio perfeito. **Conflito leve** com a sugestão "defesa que abre oportunidade" (bloquear no timing certo desequilibra o inimigo), que está só em "Sugestões para discussão" do board.
- [x] ~~**Esquiva sem invulnerabilidade:**~~ Confirmado em 2026-10-12, junto com o ritmo do New World: continua sem invulnerabilidade (seção 3 da [fatia](fatia-jogavel.md)). A esquiva do New World, com janela em que o dano some, não entra.
- [ ] **Ataques básicos por arma** (espada corta, martelo varre, arco com disparo ou tiro carregado, cajado com pulso ou projétil ampliado): hoje só a espada existe. Outras armas entram com [itens.md](itens.md).
- [ ] **Mira e volumes de acerto:** mouse direciona golpes, projéteis e áreas, sem seleção automática obrigatória; a área visível da skill deve ser a área real de acerto; volumes generosos (cone, faixa, círculo). Coerente com o cone de 90° já implementado; vale como critério para as skills.
- [ ] **Movimentos além de correr e esquivar:** pulo, natação e escalada (proposta de escalada em superfícies e bordas do mapa; rios cruzados a nado, com gelo como alternativa). Dono: [personagem.md](personagem.md) e [mundo.md](mundo.md).
- [ ] **Recursos:** pools-base de 100 vigor e 100 energia e regeneração de 10 vigor/s e 5 energia/s após 2 s **já coincidem** com a [fatia](fatia-jogavel.md#1-atributos-e-recursos). Novidade: poções, refeições e descanso em locais seguros como fontes de recuperação, e equipamentos e comida que modificam a regeneração.
- [ ] **Estado caído:** o livro propõe 45 s, levantar por 3 s, 20% de vida e retorno à cidade ou teleporte descoberto; coincide com a [fatia](fatia-jogavel.md#7-dungeon-de-até-5-com-um-boss) e com [personagem.md](personagem.md#morte-e-caído-issue-97). Novidade: o **botão de retorno antes dos 45 s** e o retorno a **teleporte descoberto**; sem perda de XP, moeda, equipamento, materiais nem durabilidade por morte na primeira versão.
- [x] **Cast com compromisso, regra de custo adotada em 2026-10-11 (#345):** skill forte tem preparo visível e sonoro e pode ser cancelada; cancelar custa **50% do recurso** (constante nomeada) e o servidor cobra uma vez só (no cancelamento ou na conclusão, nunca nos dois).
- [ ] **Combos** (sugestão do topo do board): uma skill prepara e outra finaliza; segue como proposta (e skills fortes, veja acima). Ficam em [skills.md](skills.md).

## Regras

### Ritmo do golpe (New World)

Decidido em 2026-10-12. A cadência (leve, pesado, vigor) é a do New World. A sensação de contato, o corpo que avisa e a pausa no acerto são a do Elden Ring ([referências](../referencias.md#elden-ring-fromsoftware-2022)). Os números e a cadeia moram na [seção 2 da fatia](fatia-jogavel.md#2-um-ataque-primeira-arma-leve-e-pesado). Aqui fica o que essas referências sugerem e **não** entra:

- **Esquiva com invulnerabilidade.** No New World o dano some numa janela curta. Aqui a esquiva só evita se a pessoa sair da área a tempo.
- **Guarda segurada e guarda quebrada.** No New World segurar o bloqueio gasta vigor conforme o golpe. Aqui o bloqueio é a defesa do equipamento ([#344](https://github.com/GabrielNied/jogo_do_grupo/issues/344)): sem parry e sem janela de timing.
- **Duas armas e três skills por arma.** Ficam fora desta fatia (troca de arma ainda em aberto; skills nos itens).

Para implementar sem refazer a leitura:

- A cadeia, o multiplicador do 3º leve e o teto de giro são contas em `RegrasAtaque`. No acerto, a direção é a `RotacaoY` que o Mundo já guarda em `PosicaoNoMundo`, limitada pelo giro desde a direção do início do preparo. Sem mensagem nova e sem biblioteca.
- Andar a 40% durante o golpe **não** usa `IDeslocamentosDoMundo.AbrirJanela`: essa janela libera um deslocamento mais rápido (é a esquiva). O teto menor é outra operação no contrato do Mundo (`Contratos/Mundo/`), chamada pelo Combate. Sem referência de assembly entre as funcionalidades.
- O pesado não é interrompido por dano tomado; o dano continua valendo. Só importa quando existir atordoamento por dano (seção 2 da fatia).

### O que o client mostra e o que o servidor decide

Decidido em 2026-10-12. A animação, o passo na tela e a resposta no instante do botão são do client. Quem acertou continua sendo o servidor ([arquitetura](../arquitetura.md)). O client manda intenção (ataque, esquiva, mira) e mostra o resultado. Não informa acerto nem erro.

- O servidor não roda clipe nem esqueleto. O instante do contato é a duração do preparo, a mesma constante dos dois lados. O client só ajusta a velocidade do clipe para a pose cair nesse instante. Vale para o golpe do jogador e para o [contato do inimigo](mobs.md#contato-do-golpe).
- A sensação de desviar e tomar dano mesmo assim, hoje, vem do volume não coincidir com o modelo. Com o contato, quem saiu do cone antes do fim do preparo não leva dano, no servidor e na tela.
- O client começa a esquiva e o golpe na hora do botão, sem esperar a resposta. A vida só muda quando o servidor confirma o acerto.
- **Pausa no acerto** (decidido em 2026-10-12): quando a confirmação chega, o client segura a pose de quem bateu e de quem levou por **70 ms** (`PausaVisualNoAcertoMs`). O relógio do servidor não para. Não é invulnerabilidade nem atordoamento.
- Se a mensagem da esquiva chegar depois que o servidor já resolveu o golpe, o dano vale. Rebobinar o mundo até o instante do botão é a compensação de latência, fora desta fatia ([#172](https://github.com/GabrielNied/jogo_do_grupo/issues/172)).
- O teste de cone nesse instante não aumenta a carga: o servidor já dá o passo da IA dez vezes por segundo, e a animação não entra nesse passo.

### Reação ao acerto

Decidido em 2026-10-12, no exemplo do Elden Ring, fácil de mudar. O dano continua valendo. Quem decide a interrupção é o servidor; o client só toca a pose.

- Um acerto interrompe a ação de um personagem ou de um inimigo comum, exceto o pesado do jogador já em andamento ([fatia](fatia-jogavel.md#2-um-ataque-primeira-arma-leve-e-pesado)) e qualquer ataque do chefe de dungeon.
- Quem foi interrompido não começa esquiva nem ataque por **0,3 s** (`AtordoamentoNoAcertoMs`). A pausa visual de 70 ms cabe dentro dessa janela.
- No inimigo comum, o preparo cancelado não chega ao contato. A recuperação de um golpe que já encostou continua. O alvo fugir não cancela ([mobs](mobs.md)).
- O chefe não tomba por acerto. A punição dele continua a janela em que fica exposto.

## Depende de

## Expõe

Código em `shared/Package/Runtime/Features/Combate/`. Regras e números: [fatia-jogavel.md](fatia-jogavel.md), seção 1 (não copiados aqui).

- `AtributosCombate`: os cinco atributos finais (Força, Destreza, Intelecto, Vitalidade, Espírito).
- `RegrasRecursos`: fórmulas puras de vida, vigor e energia máximos, `Limitar` (clamp) e regeneração (`RegenerarVigor`, `RegenerarEnergia`).
- `RecursosResponse`: atuais, máximos e atributos finais.
- `RotasCombate.Recursos`: `GET /api/personagens/{personagemId}/recursos` responde 200 `RecursosResponse`. Erros: `CodigosErroCombate.PersonagemNaoEncontrado` (404) e `NaoAutenticado` (401, Core).

Ataque da primeira arma (tempo real; regras e números em [fatia-jogavel.md](fatia-jogavel.md), seção 2):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 6000 | `IntencaoDeAtaque` | client → servidor | `Tipo` (`TipoDeAtaque`: Leve 0, Pesado 1), `DirecaoY` (graus, mesma convenção de `IntencaoDeMovimento.RotacaoY`), `Sequencia` |
| 6001 | `AtaqueIniciado` | servidor → quem está na área | `AtacanteId`, `Tipo`, `DirecaoY`, `PreparoMs` |
| 6002 | `AtaqueAcertou` | servidor → quem está na área | `AtacanteId`, `Acertos[]` (`AcertoDeAtaque`: `AlvoId`, `Dano`, `VidaRestante`, `VidaMaxima`) |
| 6003 | `AtaqueRecusado` | servidor → atacante | `Sequencia`, `Codigo` (`CodigosErroCombate.VigorInsuficiente`, `AcaoEmAndamento` ou `VoceEstaCaido`) |
| 6004 | `RecursosMudaram` | servidor → dono | vida, vigor e energia atuais e máximos |

- `RegrasAtaque`: constantes de balanceamento do ataque e fórmulas puras (`PreparoSegundos`, `RecuperacaoSegundos`, `DuracaoSegundos`, `CustoDeVigor`, `TemVigor`, `Dano`, `EntraNaFila`, `EstaNoVolume`). O servidor decide; o client usa só para tocar a animação no tempo certo.
- ~~Os IDs 6010 a 6012 ficam reservados para a esquiva (#62).~~ Substituído em 2026-10-11: o contrato da esquiva está abaixo.

Esquiva (tempo real; regras e números em [fatia-jogavel.md](fatia-jogavel.md), seção 3):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 6010 | `IntencaoDeEsquiva` | client → servidor | `DirecaoY` (graus, mesma convenção do ataque), `Sequencia` |
| 6011 | `EsquivaRecusada` | servidor → dono | `Sequencia`, `Codigo` (os mesmos códigos do ataque: `VigorInsuficiente`, `AcaoEmAndamento`, `VoceEstaCaido`) |
| 6012 | `EsquivaIniciada` | servidor → quem está na área | `PersonagemId`, `DirecaoY`, `DuracaoMs` |

- `RegrasEsquiva`: constantes de balanceamento (5 m, 0,6 s, 15 de vigor) e fórmulas puras (`TemVigor`, `EmAndamento`, `DistanciaPermitidaAte`, `DeslocamentoCabe`, `Destino`). A posição continua chegando pelas mensagens do Mundo.

### Ataque no servidor (#215)

- Quem recebe: `AtaqueIniciado` e `AtaqueAcertou` vão a todos que o Mundo diz estarem na área do atacante (`IPosicoesDoMundo`); `AtaqueRecusado` só ao atacante; `RecursosMudaram` só ao dono, e só quando o ataque gasta vigor (o pesado; o leve não gasta nada).
- O servidor mede o cone no **momento do acerto** (preparo depois do pedido) com a posição que o Mundo tem então; não há compensação de lag. A direção vem do pedido.
- **Um relógio só (#477):** a ação nasce no pedido; o acerto sai em `InicioMs + PreparoMs` e o fim da ação vem depois do acerto, mesmo que o banco demore. A cobrança do vigor e a leitura da Força acontecem dentro do preparo (o acerto não volta ao banco; veja [Recursos em memória](#recursos-em-memória-478)), e a fila só avança depois do acerto resolvido. Banco mais lento que o preparo atrasa o acerto, mas não o perde.
- Fila: um pedido feito nos últimos 0,3 s da ação espera e começa no fim dela (vigor e queda são conferidos nessa hora, e a recusa sai com a `Sequencia` dele). Pedido durante a ação fora da janela, ou com a fila ocupada, é recusado com `AcaoEmAndamento`.
- Caído (vida 0) é recusado com `VoceEstaCaido`; o pesado gasta 20 de vigor uma vez só (trava do personagem em memória) e é recusado sem vigor.
- `Tipo` fora do enum, direção inválida, ou ataque de quem ainda não entrou no mundo: o pedido é **descartado sem resposta** (o contrato não tem código para isso; um client correto nunca manda).
- O dano é `RegrasAtaque.Dano(tipo, Força final)`; cada alvo leva o mesmo golpe uma vez, e o `Dano` de `AcertoDeAtaque` é o que o alvo realmente perdeu (nunca mais que a vida que ele tinha).
- **Alvos:** `IAlvosDeCombate` (`server/.../Contratos/Combate/`, ADR 0016) é implementado por quem é dono de alvos; Combate junta todas as implementações. Hoje só há o **boneco de treino** (id de alvo `-1`), em `ConfigBonecoDeTreino`: 180 de vida, posição fixa na cidade (x 0, z -22, 2 m à frente do nascimento), volta à vida cheia 5 s depois do último golpe que levou. Com 0 de vida ele sai de combate (não é mais alvo) até voltar. O servidor **não avisa** a volta: o client deduz pelos 5 s sem golpes.
- **Onde o boneco está (provisório):** o contrato não tem mensagem para isso, então o client coloca o modelo na mesma posição fixa (x 0, z -22) até o shared ganhar uma mensagem ou constante para os alvos fixos (pendente com o responsável pelo contrato).

### Recursos em memória (#478)

- Vida, vigor e energia de quem está em uso ficam em memória (`RecursosEmMemoria`, o único que escreve em `RecursosPersonagem`). O banco é lido uma vez por personagem (na primeira consulta) e recebe o que mudou a cada 5 s, quando o personagem desconecta e quando o servidor para; um crash perde no máximo ~5 s de gasto. O golpe leve (custo 0) não toca no banco; golpe, esquiva, dano, queda e a rota de recursos passam pela mesma trava por personagem.
- Os atributos finais (Itens + efeitos + pontos) ficam em memória por 2 s e depois são relidos. Itens ainda não publica um evento de troca de equipamento; por isso trocar de peça aparece no golpe seguinte depois de até 2 s (a rota de recursos sempre relê). Quando o evento existir, ele invalida a entrada na hora. Valores em `ConfigRecursos`.

### Golpe de inimigo tira vida (#278)

- Combate, dono da vida, assina `GolpeDeInimigoAcertou` (`Contratos/Mobs/`, ADR 0016). Mobs decide quem estava no círculo; um evento por personagem atingido.
- A vida cai em `Dano` (sem defesa por enquanto; a conta fica em `ServicoDeDanoRecebido.DanoFinal`, onde a defesa entra depois), **nunca abaixo de 0**, e o dono recebe `RecursosMudaram` (só ele). Vida 0 leva ao estado caído ([abaixo](#estado-caído-no-servidor-125)); quem já está caído não recebe dano (o golpe é ignorado).
- O assinante não bloqueia (o evento sai do laço de IA de Mobs): a conta roda numa tarefa, sob a mesma trava dos outros recursos, então golpes simultâneos no mesmo personagem somam. Personagem sem recursos criados começa da vida cheia; um que não existe só registra erro no log.

### Esquiva no servidor (#233)

- O servidor recebe `IntencaoDeEsquiva`, confere e cobra **15 de vigor uma vez** (mesma transação do ataque) e só então pede ao Mundo a janela de deslocamento (`IDeslocamentosDoMundo`: 5 m em 0,6 s). Quem valida cada posição continua sendo o Mundo; fora da janela vale o limite de corrida.
- Quem recebe: `EsquivaIniciada` vai a todos que estão na área (o dono também); `EsquivaRecusada` e `RecursosMudaram` só ao dono.
- Recusas: `VigorInsuficiente` (nada é gasto), `VoceEstaCaido` e `AcaoEmAndamento`.
- **Ataque e esquiva não valem ao mesmo tempo:** a esquiva é recusada durante um ataque (do começo ao fim da recuperação) e durante outra esquiva; o ataque pedido durante a esquiva é recusado com `AcaoEmAndamento`, sem entrar na fila. De dois pedidos no mesmo instante, um vale. A esquiva **não cancela** o preparo de um ataque (decidido em 2026-10-12; a #62 já está fechada).
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
- **Levantar:** `Levantando` com `AjudanteId` e `ProgressoMs`; ao completar, `Vivo` com 20% da vida (vigor e energia como estavam), `RecursosMudaram` ao dono e o evento `PersonagemLevantou`. O servidor confere o alcance a cada 100 ms: sair dos 2 m, soltar, levar dano, cair, atacar, esquivar ou desconectar interrompem e zeram o progresso (volta `Caido`).
- **Voltar à cidade:** fim dos 45 s ou `VoltarParaACidade` (depois de 5 s). Vida, vigor e energia vão ao máximo, `Retorno` é enviado e o evento `PersonagemVoltouParaACidade` é publicado: **quem leva o personagem ao ponto de nascimento é o Mundo** (ainda não assina o evento; veja abaixo). Nada de item, XP, moeda ou nível muda.
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
