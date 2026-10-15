# Desenvolvimento

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 15000–15999

## Resumo

Comandos para testar no editor do Unity e no build de desenvolvimento. O client só envia o texto. Experiência, posição e spawn continuam no servidor. Não é a ferramenta de GM de produção ([#187](https://github.com/GabrielNied/jogo_do_grupo/issues/187)).

## Definido

No editor e no build de desenvolvimento, os comandos de teste entram **pelo chat** (Enter ou F12, que abre ou fecha a caixa com `/` pronto). Esc fecha a caixa. O personagem fica parado enquanto a caixa está aberta. Build de release não traz esses comandos nem o atalho F12.

`/comandos` no chat lista o que vale na sessão (chat social + comandos de teste, quando existirem). Tab completa o verbo depois da barra.

O servidor só executa com `Desenvolvimento:PermitirComandos` ligado **e** ambiente Development (`PoliticaDeComandos`, mesmo padrão do `ConfiguracaoJwt`). Sem a chave, ignora o pedido e não responde. Com a chave ligada fora de Development, os comandos ficam desligados e o servidor avisa no log (não deixa de subir).

`Desenvolvimento:Contas` é uma lista opcional de ids de conta autorizados. Vazia ou ausente, vale para qualquer conta (só em Development); com ids, a conta fora da lista é ignorada, com aviso no log. Cada comando executado é registrado no log com a conta, o personagem e o texto.

`Mundo:PermitirVelocidadeDeTeste` mora em [Mundo](mundo.md) e segue a mesma proposta na [#552](https://github.com/GabrielNied/jogo_do_grupo/issues/552).

| Comando no chat | Efeito |
|---|---|
| `/xp <quantidade>` | Soma ou tira experiência de quem pediu. De -1.000.000 a 1.000.000, sem ser zero. |
| `/tp <x> <y> <z>` | Teleporta quem pediu, se já entrou na cidade. Números finitos, X e Z dentro do mapa, Y entre -50 e 300. |
| `/spawn` | Faz um inimigo comum aparecer 2 m no eixo X de quem pediu, no maior grupo da área. Até 20 a mais por grupo. |
| `/ajuda` ou `/comandos` | Lista os comandos ( `/comandos` inclui também os do chat). |

O servidor continua lendo o texto **sem barra** (`xp 10`, `tp …`); o client tira a barra antes de enviar.

Números usam ponto decimal. O texto tem no máximo 80 caracteres.

## Em aberto

- Mais comandos (item, moeda, matar o inimigo) quando fizer falta para testar.
- A ferramenta de GM de produção, com permissão de conta, continua na #187.

## Regras

`xp` usa `RegrasExperiencia.AplicarAjuste` ([Personagem](personagem.md)). Quantidade negativa desce de nível e tira pontos livres, sem desfazer pontos já gastos. O ganho de inimigo não muda: continua em `AplicarGanho`.

`tp` usa `CidadeDoMundo.ForcarPosicao` ([Mundo](mundo.md)). O dono recebe `PosicaoForcada` e confirma; os outros recebem `PosicaoJogador`.

`spawn` acrescenta um inimigo no maior grupo da área ([Mobs](mobs.md)), a `DistanciaDoSpawnDeTeste` no eixo X, e avisa com `InimigoApareceu`. Não é a volta depois da morte. Passando de `AparicoesExtrasDeTeste`, o comando recusa.

## Relato e ajuste ao vivo

Para melhorar a sensação do jogo (peso, resposta, animação, câmera) com dados que a IA lê. Só no editor e no build de desenvolvimento. Vocabulário e exemplos em [sensacao-do-jogo.md](../sensacao-do-jogo.md).

- **F9, relato:** o jogo guarda sempre os últimos 3 s. F9 congela, abre uma caixa ("o que pareceu errado?") e o personagem para. Enter salva em `relatos/AAAA-MM-DD_HHMMSS/` na raiz do repositório (no build, em `persistentDataPath/relatos`), fora do git; Esc descarta. `relatos/ultimo.txt` aponta o mais recente.
- **O que o relato tem:** `nota.txt` (frase, tela, latência, posição, valores do F10 fora do padrão), `linha-do-tempo.txt` (só o que mudou, em ms e com o quadro: tecla, mensagem do servidor com o JSON, `timeScale`, clipe e `speed` do Animator do jogador e do mais próximo, tremor da câmera), `folha-impacto.png` (12 quadros seguidos em volta do último acerto), `folha-geral.png` (12 quadros espalhados), `quadros.csv` e `quadros/NNN.png` (todos). Mensagens muito frequentes, como posição, viram só um total.
- **F10, ajustes:** lista os valores registrados na `Sintonia` (Core), por funcionalidade, com slider, campo e "↺". Mudam o jogo na hora e valem até sair do Play Mode ou do jogo. "Copiar valores" põe os valores fora do padrão na área de transferência e grava `relatos/ajustes-<data>.txt`. Hoje: os de Combate ([ConfigSensacaoCombate](../../client/Assets/_Game/Features/Combate/Scripts/ConfigSensacaoCombate.cs)).
- **Com a IA:** `/relato` lê o relato mais recente, diagnostica com os números e propõe o valor para testar no F10 ou a correção.

## Prints da tela

Script de editor que grava a cena ou um painel em três resoluções para o agente ler antes do PR. O comando está em [Prints da tela](../agentes.md#prints-da-tela).

## Depende de

- [Personagem](personagem.md): `IAjusteDeExperienciaDeTeste`
- [Mundo](mundo.md): `ITeleporteDeTeste`
- [Mobs](mobs.md): `ISpawnDeTeste`

## Expõe

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 15000 | `PedirComandoDeTeste` | client → servidor | `Texto` |
| 15001 | `ResultadoDeComandoDeTeste` | servidor → client | `Aceito`, `Texto` |
