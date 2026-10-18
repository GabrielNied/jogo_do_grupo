# Pets

**Responsável:**
**Status:** em desenvolvimento (drop raro, item consumível negociável, registro na conta e janela U implementados; fora da fatia jogável)
**Faixa de IDs de mensagem:** 17000–17999

## Resumo

A pessoa chama um pet, ele segue por perto e some quando é dispensado. O servidor decide onde o pet está.

## Definido

- **Cuidador de pets na Brasa Mansa** (decidido por Gabriel em 2026-10-18): vende 3 **pets comuns** por coroas (`GalinhaComum` por 50 coroas, `GatoComum` por 75 coroas e `CachorroComum` por 100 coroas) no NPC Cuidadora Mirna (id 9, posição `(-14f, 0f, 14f)` em Brasa Mansa). A compra registra o pet diretamente na conta (idempotente e segura contra concorrência por `(ContaId, Especie)`). Os pets bonitos/exóticos continuam no drop raro ([npcs](npcs.md#elenco-da-brasa-mansa), [#1068](https://github.com/GabrielNied/jogo_do_grupo/issues/1068)).
  - **Mensagens e rotas:** `ConsultarCuidador` (17009), `RespostaCuidador` (17010), `ComprarPetCuidador` (17011), `RespostaCompraPet` (17012). REST: `GET /api/pets/cuidador`, `POST /api/personagens/{personagemId}/pets/cuidador/comprar`.
  - **Interface:** tecla [E] no Cuidador abre `pets.cuidador` (`JanelaCuidadorDePets`), desacoplada via `JanelasDoJogo`. Pet comprado aparece imediatamente na janela [U] (aba Pets).

Decidido em 2026-10-12 (#161) e **revisto em 2026-10-16** (quiz de definições 3):

- **Modelo alvo:** pets vêm de **drop raro** de mobs ([mobs](mobs.md#definido)), como **item negociável** (estilo WoW). **Usar o item registra o pet na conta**; não há captura pelo bestiário. A conta pode **colecionar vários**; só **um pet ativo** (chamado) por vez.
- O **cervo jovem** que já vem com o personagem (#161) é **legado até alinhar** com drops; não é a regra final.
- **Menu (tecla U):** janela [Companheiros](interface.md#companheiros-u), aba **Pets** — **catálogo completo** (não obtidos em cinza, sem ativar), origem ao selecionar bloqueado; obtidos: **ativa ou desativa** (chamar/dispensar). Mesma janela que [montarias](montarias.md#definido), outra aba.
- Um pet chamado por vez. Chamar de novo no mesmo pet não duplica.
- Ele anda até o dono e para a **2 m** (`DistanciaDeSeguidaMetros`). Não fica em cima do dono. Se passar de **20 m** (`DistanciaParaAlcancarMetros`), o servidor o coloca de volta na distância de seguida.
- Não luta, não é alvo e não é montaria. Não tem bolsa própria.
- Ajuda a juntar o loot do dono: se um item que o dono poderia coletar com F está a até **8 m** do pet, o pet vai até o item, o servidor coleta para o inventário do dono e o pet volta a seguir. Um item por vez. Inventário cheio deixa o item no chão. Item de outra pessoa não sai do lugar.
- Dispensar remove. Desconectar remove para os outros. Na dungeon ele continua com o dono.
- Quem está na cidade vê o pet dos outros.
- Fora desta versão: combate do pet, coleta de recurso de profissão, nível do pet e a coleção de conhecimentos.

## Decidido na implementação (2026-10-15, #161)

- **Entrar e sair de caverna ou dungeon:** o pet vai junto. Ao trocar de área ele some para quem via na área antiga e reaparece a 2 m do dono na nova (a mesma publicação por vizinhança); `/pet` e a busca de loot funcionam lá. Sair do jogo ou cair o remove.
- **Como chamar:** preferencialmente pela janela **U** (aba Pets); comando de chat `/pet` (alterna), `/pet chamar` e `/pet dispensar` permanecem. Aparece em `/comandos`. **P** fica reservada a profissões ([#134](https://github.com/GabrielNied/jogo_do_grupo/issues/134)). UI: [#925](https://github.com/GabrielNied/jogo_do_grupo/issues/925).
- **Posição:** o servidor move o pet 10 vezes por segundo, como a IA dos mobs, a 6,5 m/s (acima da corrida do jogador, 5,5). Publica a quem vê o dono (a mesma área de interesse dos mobs): `PetApareceu` ao entrar no campo de visão, `PetMudou` sequenciado enquanto anda ou para, `PetSumiu` ao sair. O pet nasce atrás do dono, a 2 m.
- **Parar sem tremer:** parado, o pet só volta a andar se o dono passar de 2,5 m (`FolgaDeSeguidaMetros`).
- **Loot:** o pet procura o item a cada 0,5 s, a até 8 m **dele** (não do dono). "Que o dono poderia coletar" quer dizer: o item é do dono, está no prazo e livre; a distância de 12 m da tecla F não vale para o pet. O servidor de Itens faz a coleta pelo mesmo caminho da tecla F (um só vencedor por item, espaço conferido na gravação) e o dono recebe o `LootColetado` normal. Se o pet e o dono pedirem o mesmo item juntos, um deles leva e o outro não recebe nada.
- **Inventário cheio:** o item fica no chão sem aviso ao dono, e o pet para de buscar loot por 30 s (`PausaSemEspacoSegundos`).
- Se o dono se afasta mais de 20 m enquanto o pet busca um item, o pet desiste do item e volta para perto dele.
- O pet não persiste: reiniciar o servidor ou sair do jogo o remove.
- **Visual:** modelo CC0 "Deer Low Poly" ([licencas](../arte/licencas.md)), sem esqueleto: o client suaviza a posição e balança o corpo ao andar. Valores em `ConfigVisualDoPet`.

## Em aberto

- [x] Janela **U** com coleção, catálogo, bloqueados com origem e chamar/dispensar (#925). Drop raro negociável e registro na conta (#923).
- [ ] Animação de verdade (esqueleto) do cervo.
- [x] Demais espécies e itens do [catálogo proposto](#catálogo-proposta) no `shared` e no loot por criatura.

## Catálogo (proposta)

**Proposta** alinhada ao loot em [itens](itens.md) e [mobs](mobs.md#definido). Implementado em `RegrasPets`, `CatalogoItens` e `RegrasLootCompanheiros` (drop por `CriaturaId`).

### Drop e item

| Regra | Valor |
|---|---|
| Quem dropa | Criatura **comum** ou **elite** ([mobs](mobs.md#criatura-elite)) |
| Quem **não** dropa | **Chefe**, chefe raro, dungeon isolada sem mob comum elegível |
| Chance | **2%** por participante por morte (`ConfigLoot.ChanceDePetRaro`) |
| Item | Consumível **negociável**; **usar** fora de combate registra na conta (não consome se já possui) |
| Sorteio futuro | Depois de passar nos 2%, sortear **só** entre pets cuja **espécie de origem** coincide com a criatura morta (estilo WoW) |

Convenção de nome de item: **Sino do/da …**, **Colar …** ou **Frasco …** quando fizer sentido. Cada linha reserva um `EspecieDePet` (1–99) e um id estável em `CatalogoItens` (a partir de 12; ids publicados não mudam).

### Lista por progressão

| `EspecieDePet` | Nome | Item (proposto) | Origem (texto na UI) | Espécies de mob |
|---|---|---|---|---|
| 1 | Cervo Jovem | Sino do Cervo Jovem *(id 11, implementado)* | Drop raro de criaturas no campo (2%) | Campo genérico hoje; alvo: **Cervo de Galhada Oca** |
| 2 | Filhote de Javali | Sino do Filhote de Javali | Drop raro do Javali (2%) | **Javali do Campo**, **Javali Ferrugem** |
| 3 | Lobo Filhote | Sino do Lobo Filhote | Drop raro do Lobo (2%) | **Lobo do Campo** |
| 4 | Tatu Pequeno | Sino do Tatu Pequeno | Drop raro do Tatu (2%) | **Tatu Rochoso** |
| 5 | Coruja da Clareira | Sino da Coruja da Clareira | Drop raro na mata (2%) | **Cervo de Galhada Oca** ou pontos de mata |
| 6 | Mariposa-luz | Frasco da Mariposa-luz | Drop raro da Mariposa (2%) | **Mariposa de Breu** |
| 7 | Coelho do Pasto | Sino do Coelho do Pasto | Drop raro no pasto (2%) | Manada do pasto / javalis nível 1–2 |
| 8 | Lobo de Geada Filhote | Sino do Lobo de Geada | Drop raro do Lobo de Geada (2%) | **Lobo de Geada** |
| 9 | Carapaça Jovem | Colar da Carapaça Jovem | Drop raro da Carapaça (2%) | **Carapaça de Brasa** |
| 10 | Mosquito do Pântano | Frasco do Mosquito do Pântano | Drop raro no pântano (2%) | **Mariposa de Breu** e mobs do pântano ([tabela bioma × espécie](mobs.md#tabela-de-distribuição-bioma--espécie--nível)) |
| 11 | Cachorro Comum | — (compra direta) | Cuidadora Mirna na Brasa Mansa (100 coroas) | Venda NPC |
| 12 | Gato Comum | — (compra direta) | Cuidadora Mirna na Brasa Mansa (75 coroas) | Venda NPC |
| 13 | Galinha Comum | — (compra direta) | Cuidadora Mirna na Brasa Mansa (50 coroas) | Venda NPC |

Todas as entradas: **só visual e loot helper**; mesma mecânica do Cervo Jovem. Sem pet de chefe, sem captura pelo bestiário, sem combate.

### Prioridade sugerida

1. Campo: Filhote de Javali, Lobo Filhote, Tatu Pequeno (com [mais mobs no campo](mobs.md#mais-mobs-no-campo-452)).
2. Amarrar origem por espécie no loot (substituir um único `ItemPetRaro` global).
3. Mata/colinas e biomas altos quando as fichas existirem no catálogo de criaturas.

Montarias correspondentes: [montarias.md — Catálogo (proposta)](montarias.md#catálogo-proposta).

## Regras

Quem decide: **servidor** (existência, posição e a coleta). O client pede chamar ou dispensar e mostra. A coleta usa a mesma regra de dono e de inventário do loot no chão ([fatia jogável, seção 5](fatia-jogavel.md#5-loot-no-chão)).

O pet não é um inimigo e não entra no catálogo de criaturas hostis. A posição segue o mesmo jeito de publicar movimento que os mobs já usam, sem biblioteca nova.

Constantes: `ConfigPets` (shared) e `ConfigVisualDoPet` (client).

## Depende de

[Mundo](mundo.md) (posição do dono e quem o vê, por `IPosicoesDoMundo` e `IVizinhanca`) e [Itens](itens.md) (o loot que o dono pode coletar, pelo contrato `ILootDoDono` de `server/.../Contratos/Itens/`). Pets não referencia Personagem, Itens nem Mobs.

## Expõe

A posição do pet, para quem está na mesma área ver (`PetApareceu`, `PetMudou`, `PetSumiu`).
