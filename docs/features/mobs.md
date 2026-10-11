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

## Depende de

- Ainda nada no código. A IA, o dano e a posição no mundo dependem de [Combate](combate.md) (alvo e dano) e de [Mundo](mundo.md) (posição dos jogadores), que as issues #61 e #119 preparam.

## Expõe

Código em `shared/Package/Runtime/Features/Mobs/` (regras e números acima, não copiados aqui). Ainda **não há mensagens de rede**: as criaturas ainda não aparecem no mundo (issue #63).

- `CategoriaDeCriatura`: `Comum`, `Chefe`, `ChefeRaro`.
- `VarianteDeCriatura`: `Normal`, `Fortalecida`.
- `DefinicaoDeCriatura` e `CatalogoCriaturas`: a ficha fixa de cada criatura e `Obter` por id.
- `RegrasCriaturas`: fórmulas puras (variante, aparição, vida, dano, experiência, multiplicador de recompensa e tamanho).

No servidor (`server/.../Features/Mobs/`): `SorteadorDeCriaturas` (sorteia a variante e a aparição) e `AparicaoDeChefeRaro` (o controle de um ponto de chefe raro). Ainda não estão ligados ao mundo.
