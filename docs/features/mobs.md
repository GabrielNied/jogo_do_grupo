# Mobs e bosses

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 11000–11999

## Resumo

Tipos de mobs, comportamento por região e bosses com mecânicas marcantes.

## Definido

- Regras iniciais da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- Mobs por região
- Bosses com mecânicas marcantes e **ataques telegrafados** (veja [combate.md](combate.md)); **bosses cada vez mais fortes são o objetivo do loop principal** (D5 da [validação de 2026-10-10](../brainstorm/validacao.md))
- Referência de agro: RYL: Path of the Emperor (agro longo, juntar muitos inimigos e derrotar de uma vez); veja S3 na validação
- **Três categorias de criatura** (pedido do Gabriel em 2026-10-11; os números são **proposta**, veja "Regras"):
  - **Comum:** pode aparecer **fortalecida**: mais forte, maior e com **aura vermelha**, e dá mais recompensa.
  - **Chefe:** sempre aparece e sempre com a mesma força. Nunca vem fortalecido.
  - **Chefe raro:** força fixa como a do chefe, mas **pode aparecer ou não**, no estilo dos raros de World of Warcraft.

## Em aberto

- [ ] Bosses de mundo aberto (world boss)?
- [ ] Respawn: tempo fixo ou dinâmico?
- [ ] O nome "fortalecida" fica, ou o grupo prefere outro (elite, enfurecida)?
- [ ] Os números da criatura fortalecida e do chefe raro (tabelas em "Regras").
- [ ] As três primeiras criaturas (Javali do Campo, Guardião do Reflexo, Presa-Velha) são só um ponto de partida: nomes, espécies e onde ficam.
- [ ] O chefe raro avisa quando aparece (mensagem na área, marca no mapa) ou o jogador tem que achar?
- [ ] O chefe raro some sozinho se ninguém o matar? Proposta atual: fica até morrer.
- [ ] Dungeon tem chefe raro (um sorteio ao criar a instância)? As regras já servem; falta decidir.
- [ ] Loot próprio do chefe raro (item que só ele larga): depende de [itens.md](itens.md).

## Regras

Tudo abaixo é **proposta** de 2026-10-11, a calibrar. Quem decide é o **servidor**: ele sorteia a variante e a aparição, e o client só mostra. Força sempre **fixa**: nenhuma criatura escala com o número ou o nível dos jogadores ([fatia-jogavel.md](fatia-jogavel.md), seções 4 e 7).

### Criatura fortalecida

Só a criatura **comum**. O sorteio acontece uma vez, quando ela aparece (inclusive no respawn), e vale até ela morrer.

| Item | Valor |
|---|---|
| Chance de vir fortalecida | **5%** (1 em 20) |
| Vida | **2x** |
| Dano | **1,5x** (meio arredonda para cima) |
| Tamanho do modelo | **1,3x** |
| Aura | vermelha |
| Recompensa | **2x**: experiência em dobro e o dobro de sorteios de loot |

- O loot é da funcionalidade [Itens](itens.md): Mobs só informa o multiplicador de recompensa na morte.
- Desistir da perseguição e voltar ao ponto de origem **não** sorteia de novo: a fortalecida continua fortalecida.

### Chefe e chefe raro

- **Chefe:** sempre aparece (chance 100%), com os valores de base. O de dungeon não volta sozinho: só numa instância nova.
- **Chefe raro:** tem um ponto de aparição no mundo. A cada **intervalo** o servidor sorteia a **chance** de ele aparecer.
  - Não apareceu: só tenta de novo no intervalo seguinte.
  - Apareceu: fica no mundo até morrer; enquanto isso não há sorteio.
  - Morreu: a espera recomeça a partir da morte.
  - O primeiro sorteio é quando o servidor sobe. Se o servidor ficar um tempo sem sortear, os intervalos perdidos **não** acumulam: é um sorteio só.
- Com chance de 20% e intervalo de 30 minutos, o chefe raro aparece em média a cada 2 h 30 min, mas pode vir em sequência ou demorar bem mais.

### Primeiras criaturas

| Id | Nome | Categoria | Nível | Vida | Dano | XP | Aparece | Intervalo |
|---|---|---|---|---|---|---|---|---|
| 1 | Javali do Campo | comum | 3 | 180 | 40 | 20 | sempre | volta em 120 s |
| 100 | Guardião do Reflexo | chefe | 5 | 6000 | 120 | 500 | sempre | não volta (dungeon) |
| 200 | Presa-Velha | chefe raro | 5 | 1500 | 80 | 200 | 20% por sorteio | sorteio a cada 30 min |

- O Javali do Campo usa os números do inimigo comum da fatia (seção 4 e ficha da issue #63).
- Faixas de id: comuns de 1 a 99, chefes de 100 a 199, chefes raros de 200 a 299. Id publicado não muda nem é reaproveitado.

### Inimigo comum da fatia (issue #63)

As regras de base estão em [fatia-jogavel.md](fatia-jogavel.md), seção 4. O que a ficha da issue #63 acrescentou, e o que faltava para o contrato, fica aqui. Tudo **proposta a calibrar**, em constantes de `RegrasInimigo` (vida, dano e tempo de volta vêm da tabela acima).

| Item | Valor |
|---|---|
| Criatura | Javali do Campo (id 1), em grupos de 3 a 5 |
| Onde | campo aberto, a uns 150 m do portão norte da cidade (decidido em 2026-10-11); a dungeon reaproveita o mesmo inimigo, sem volta |
| Visão | vê o jogador a **12 m** |
| Alerta | **0,5 s** entre ver e começar a perseguir; o grupo todo entra em alerta junto |
| Perseguição | **4 m/s**, em linha reta (mais lento que o jogador correndo) |
| Ataque | começa a até **2 m** do alvo; **1 s** de aviso, depois o golpe e **1,5 s** de recuperação |
| Área do golpe | círculo de **1,5 m** de raio no chão, com o centro **1,5 m** à frente do inimigo; fica fixa desde o aviso |
| Desistência | a mais de **30 m** do ponto de origem ele volta (a **6 m/s**), com a vida cheia, sem escolher alvo até chegar |
| Corpo | raio de **0,6 m** como alvo dos golpes dos jogadores |
| Atualização da IA | passo fixo, **10 vezes por segundo** |

- **Estados:** repouso → alerta → perseguição ⇄ ataque; alerta, perseguição e ataque → retorno → repouso; qualquer estado vivo → morto. Morto não muda mais: a volta é um inimigo novo (`InimigoApareceu`), com a variante sorteada de novo.
- **Alvo:** o jogador mais próximo que ele viu. Enquanto esse alvo continuar valendo, ele **não troca**, mesmo com outro mais perto. No empate de distância fica o de menor id.
- **Golpe telegrafado:** quem estiver na área quando o aviso acaba leva o dano; quem saiu a tempo não leva (a esquiva de 5 m tira o jogador da área).
- Sem desvio de obstáculos na fatia: o campo é aberto.

## Depende de

- ~~Ainda nada no código. A IA, o dano e a posição no mundo dependem de [Combate](combate.md) (alvo e dano) e de [Mundo](mundo.md) (posição dos jogadores), que as issues #61 e #119 preparam.~~ Substituído em 2026-10-11: os dois contratos já existem no servidor (`Contratos/Combate/IAlvosDeCombate` e `Contratos/Mundo/IPosicoesDoMundo`). O inimigo da issue #63 vai usá-los; o contrato do shared (abaixo) ainda não depende de nenhuma outra funcionalidade.

## Expõe

Código em `shared/Package/Runtime/Features/Mobs/` (regras e números acima, não copiados aqui). ~~Ainda **não há mensagens de rede**: as criaturas ainda não aparecem no mundo (issue #63).~~ Substituído em 2026-10-11: o contrato do inimigo comum está abaixo; o servidor e o client que o usam são as issues #268 e #269.

- `CategoriaDeCriatura`: `Comum`, `Chefe`, `ChefeRaro`.
- `VarianteDeCriatura`: `Normal`, `Fortalecida`.
- `DefinicaoDeCriatura` e `CatalogoCriaturas`: a ficha fixa de cada criatura e `Obter` por id.
- `RegrasCriaturas`: fórmulas puras (variante, aparição, vida, dano, experiência, multiplicador de recompensa e tamanho).

Inimigo comum (tempo real; regras em "Inimigo comum da fatia"):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 11000 | `InimigosDaArea` | servidor → quem entra na área | `Inimigos[]` (`InimigoNoMundo`: `InimigoId`, `CriaturaId`, `Variante`, `X`, `Y`, `Z`, `RotacaoY`, `Vida`, `VidaMaxima`, `Estado`, `AlvoId`); só os vivos |
| 11001 | `InimigoMudou` | servidor → quem está na área | `InimigoId`, `X`, `Y`, `Z`, `RotacaoY`, `Estado`, `AlvoId`, `Vida`, `Sequencia` (o client descarta a que não for maior que a última) |
| 11002 | `InimigoPreparaAtaque` | servidor → quem está na área | `InimigoId`, `PreparoMs`, `CentroX`, `CentroZ`, `Raio`, `DirecaoY` |
| 11003 | `InimigoMorreu` | servidor → quem está na área | `InimigoId` |
| 11004 | `InimigoApareceu` | servidor → quem está na área | `Inimigo` (`InimigoNoMundo`) |

- `EstadoDeInimigo`: `Repouso` 0, `Alerta` 1, `Perseguicao` 2, `Ataque` 3, `Retorno` 4, `Morto` 5.
- `InimigoId` é o mesmo `AlvoId` que o Combate manda em `AcertoDeAtaque`: o dano de um golpe do jogador chega por `AtaqueAcertou`, não por `InimigoMudou`. `AlvoId` 0 (`RegrasInimigo.SemAlvo`) é sem alvo.
- `CriaturaId` aponta para o `CatalogoCriaturas` (nome, nível, categoria); a ficha da issue chamava esse campo de `Tipo`.
- `RegrasInimigo`: constantes de balanceamento e fórmulas puras (`TransicaoValida`, `EstaVivo`, `ProcuraAlvo`, `TamanhoDoGrupo`, `Ve`, `NoAlcanceDeAtaque`, `DeveDesistir`, `EscolherAlvo`, `DirecaoPara`, `Avancar`, `CentroDoGolpe`, `EstaNaAreaDoGolpe`). O servidor decide; o client usa só para mostrar.

No servidor (`server/.../Features/Mobs/`): `SorteadorDeCriaturas` (sorteia a variante e a aparição) e `AparicaoDeChefeRaro` (o controle de um ponto de chefe raro). Ainda não estão ligados ao mundo.
