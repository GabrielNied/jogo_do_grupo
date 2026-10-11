# Desenvolvimento

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 15000–15999

## Resumo

Comandos para testar no editor do Unity e no build de desenvolvimento. O client só envia o texto. Experiência, posição e spawn continuam no servidor. Não é a ferramenta de GM de produção ([#187](https://github.com/GabrielNied/jogo_do_grupo/issues/187)).

## Definido

No editor e no build de desenvolvimento, F12 abre o console. Enter envia, Esc fecha. O personagem fica parado enquanto a caixa está aberta. Build de release não traz o console.

O servidor só executa com `Desenvolvimento:PermitirComandos` em `appsettings.Development.json`. Sem a chave, ignora o pedido e não responde.

| Comando | Efeito |
|---|---|
| `xp <quantidade>` | Soma ou tira experiência de quem pediu. De -1.000.000 a 1.000.000, sem ser zero. |
| `tp <x> <y> <z>` | Teleporta quem pediu, se já entrou na cidade. Números finitos, X e Z dentro do mapa, Y entre -50 e 300. |
| `spawn` | Faz um inimigo comum aparecer 2 m no eixo X de quem pediu, no maior grupo da área. Até 20 a mais por grupo. |
| `ajuda` | Lista os comandos. |

Números usam ponto decimal. O texto tem no máximo 80 caracteres.

## Em aberto

- Mais comandos (item, moeda, matar o inimigo) quando fizer falta para testar.
- A ferramenta de GM de produção, com permissão de conta, continua na #187.

## Regras

`xp` usa `RegrasExperiencia.AplicarAjuste` ([Personagem](personagem.md)). Quantidade negativa desce de nível e tira pontos livres, sem desfazer pontos já gastos. O ganho de inimigo não muda: continua em `AplicarGanho`.

`tp` usa `CidadeDoMundo.ForcarPosicao` ([Mundo](mundo.md)). O dono recebe `PosicaoForcada` e confirma; os outros recebem `PosicaoJogador`.

`spawn` acrescenta um inimigo no maior grupo da área ([Mobs](mobs.md)), a `DistanciaDoSpawnDeTeste` no eixo X, e avisa com `InimigoApareceu`. Não é a volta depois da morte. Passando de `AparicoesExtrasDeTeste`, o comando recusa.

## Depende de

- [Personagem](personagem.md): `IAjusteDeExperienciaDeTeste`
- [Mundo](mundo.md): `ITeleporteDeTeste`
- [Mobs](mobs.md): `ISpawnDeTeste`

## Expõe

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 15000 | `PedirComandoDeTeste` | client → servidor | `Texto` |
| 15001 | `ResultadoDeComandoDeTeste` | servidor → client | `Aceito`, `Texto` |
