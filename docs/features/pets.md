# Pets

**Responsável:**
**Status:** em desenvolvimento (cervo jovem implementado; fora da fatia jogável)
**Faixa de IDs de mensagem:** 17000–17999

## Resumo

A pessoa chama um pet, ele segue por perto e some quando é dispensado. O servidor decide onde o pet está.

## Definido

Decidido em 2026-10-12 (#161) e **revisto em 2026-10-16** (quiz de definições 3):

- **Modelo alvo:** pets vêm de **drop raro** de mobs ([mobs](mobs.md#definido)), como **item negociável** (estilo WoW). **Usar o item registra o pet na conta**; não há captura pelo bestiário.
- O **cervo jovem** que já vem com o personagem (#161) é **legado até alinhar** com drops; não é a regra final.
- Um pet chamado por vez. Chamar de novo no mesmo pet não duplica.
- Ele anda até o dono e para a **2 m** (`DistanciaDeSeguidaMetros`). Não fica em cima do dono. Se passar de **20 m** (`DistanciaParaAlcancarMetros`), o servidor o coloca de volta na distância de seguida.
- Não luta, não é alvo e não é montaria. Não tem bolsa própria.
- Ajuda a juntar o loot do dono: se um item que o dono poderia coletar com F está a até **8 m** do pet, o pet vai até o item, o servidor coleta para o inventário do dono e o pet volta a seguir. Um item por vez. Inventário cheio deixa o item no chão. Item de outra pessoa não sai do lugar.
- Dispensar remove. Desconectar remove para os outros. Na dungeon ele continua com o dono.
- Quem está na cidade vê o pet dos outros.
- Fora desta versão: combate do pet, coleta de recurso de profissão, nível do pet e a coleção de conhecimentos.

## Decidido na implementação (2026-10-15, #161)

- **Entrar e sair de caverna ou dungeon:** o pet vai junto. Ao trocar de área ele some para quem via na área antiga e reaparece a 2 m do dono na nova (a mesma publicação por vizinhança); `/pet` e a busca de loot funcionam lá. Sair do jogo ou cair o remove.
- **Como chamar:** comando de chat `/pet` (alterna: chama, ou dispensa se já está com você), `/pet chamar` e `/pet dispensar`, mais a tecla **U** (chamar/dispensar, como alternar; quiz 4, 2026-10-16; [#925](https://github.com/GabrielNied/jogo_do_grupo/issues/925)). Aparece em `/comandos` e em [Teclas e atalhos](interface.md#teclas-e-atalhos). **P** fica reservada a profissões ([#134](https://github.com/GabrielNied/jogo_do_grupo/issues/134)).
- **Posição:** o servidor move o pet 10 vezes por segundo, como a IA dos mobs, a 6,5 m/s (acima da corrida do jogador, 5,5). Publica a quem vê o dono (a mesma área de interesse dos mobs): `PetApareceu` ao entrar no campo de visão, `PetMudou` sequenciado enquanto anda ou para, `PetSumiu` ao sair. O pet nasce atrás do dono, a 2 m.
- **Parar sem tremer:** parado, o pet só volta a andar se o dono passar de 2,5 m (`FolgaDeSeguidaMetros`).
- **Loot:** o pet procura o item a cada 0,5 s, a até 8 m **dele** (não do dono). "Que o dono poderia coletar" quer dizer: o item é do dono, está no prazo e livre; a distância de 12 m da tecla F não vale para o pet. O servidor de Itens faz a coleta pelo mesmo caminho da tecla F (um só vencedor por item, espaço conferido na gravação) e o dono recebe o `LootColetado` normal. Se o pet e o dono pedirem o mesmo item juntos, um deles leva e o outro não recebe nada.
- **Inventário cheio:** o item fica no chão sem aviso ao dono, e o pet para de buscar loot por 30 s (`PausaSemEspacoSegundos`).
- Se o dono se afasta mais de 20 m enquanto o pet busca um item, o pet desiste do item e volta para perto dele.
- O pet não persiste: reiniciar o servidor ou sair do jogo o remove.
- **Visual:** modelo CC0 "Deer Low Poly" ([licencas](../arte/licencas.md)), sem esqueleto: o client suaviza a posição e balança o corpo ao andar. Valores em `ConfigVisualDoPet`.

## Em aberto

- [x] Animação de verdade (esqueleto) do cervo e um jeito de chamar além do comando de chat. Decidido em 2026-10-16 (quiz 4): tecla **U** além do `/pet`; esqueleto continua em aberto.
- [ ] Animação de verdade (esqueleto) do cervo.

## Regras

Quem decide: **servidor** (existência, posição e a coleta). O client pede chamar ou dispensar e mostra. A coleta usa a mesma regra de dono e de inventário do loot no chão ([fatia jogável, seção 5](fatia-jogavel.md#5-loot-no-chão)).

O pet não é um inimigo e não entra no catálogo de criaturas hostis. A posição segue o mesmo jeito de publicar movimento que os mobs já usam, sem biblioteca nova.

Constantes: `ConfigPets` (shared) e `ConfigVisualDoPet` (client).

## Depende de

[Mundo](mundo.md) (posição do dono e quem o vê, por `IPosicoesDoMundo` e `IVizinhanca`) e [Itens](itens.md) (o loot que o dono pode coletar, pelo contrato `ILootDoDono` de `server/.../Contratos/Itens/`). Pets não referencia Personagem, Itens nem Mobs.

## Expõe

A posição do pet, para quem está na mesma área ver (`PetApareceu`, `PetMudou`, `PetSumiu`).
