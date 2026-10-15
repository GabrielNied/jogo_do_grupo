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
- Report de jogador comum, fora do grupo: [proposta](#report-de-jogador-comum-futuro), sem issue até haver testador de fora.

## Regras

`xp` usa `RegrasExperiencia.AplicarAjuste` ([Personagem](personagem.md)). Quantidade negativa desce de nível e tira pontos livres, sem desfazer pontos já gastos. O ganho de inimigo não muda: continua em `AplicarGanho`.

`tp` usa `CidadeDoMundo.ForcarPosicao` ([Mundo](mundo.md)). O dono recebe `PosicaoForcada` e confirma; os outros recebem `PosicaoJogador`.

`spawn` acrescenta um inimigo no maior grupo da área ([Mobs](mobs.md)), a `DistanciaDoSpawnDeTeste` no eixo X, e avisa com `InimigoApareceu`. Não é a volta depois da morte. Passando de `AparicoesExtrasDeTeste`, o comando recusa.

## Report beta e ajuste ao vivo

Para quem testa no editor transformar o que sentiu ou viu (bug, sensação, melhoria, UX) numa issue que a IA puxa sem reler o jogo. Só no editor e no build de desenvolvimento. Vocabulário de sensação em [sensacao-do-jogo.md](../sensacao-do-jogo.md). Decisão no [ADR 0041](../decisoes/0041-report-beta-vira-issue.md).

- **Abrir:** F9 ou **J** (aparece como "Report" na barra de atalhos). F9 abre por cima de outra janela aberta, então dá para reportar o inventário aberto. O jogo guarda sempre os últimos 15 s de linha do tempo e os últimos 3 s de imagem; abrir congela isso e o personagem para.
- **Janela:** frase (obrigatória), tipo (bug, sensação, melhoria, UX), severidade (crítica, alta, média, baixa) e área (a funcionalidade, ou "não sei"). Enter ou **Enviar** grava e fecha; Esc ou **Cancelar** descarta. O Enter não abre o chat. A janela lista os reports da sessão e a issue de cada um, e a gravação recomeça ao fechar: dá para reportar várias vezes seguidas sem sair do Play Mode.
- **O que o report grava**, em `relatos/pendentes/AAAA-MM-DD_HHMMSS/` na raiz do repositório (no build, em `persistentDataPath/relatos`), fora do git:
  - `reporte.json`: o que a pessoa escolheu, hora, versão, tela, latência, posição, cena, janela aberta, branch e commit (lidos da `.git`) e o estado do envio. É o contrato com o [script de envio](../../scripts/relatos/reporte.mjs).
  - `linha-do-tempo.txt`: só o que mudou nos 15 s, em ms e com o quadro (`qNNN`): tecla, mensagem do servidor com o JSON, `timeScale`, clipe e `speed` do Animator do jogador e do mais próximo, tremor da câmera. Mensagens muito frequentes, como posição, viram um total.
  - `estado.txt`: cena, janela aberta, latência, posição e a última mensagem de cada tipo recebida na sessão (há quanto tempo e o JSON). `console.txt`: os últimos avisos e erros do Unity, com o começo da pilha.
  - `tela.png` (a tela inteira), `folha-impacto.png` (12 quadros seguidos em volta do último acerto, com o centro ampliado), `folha-geral.png` (12 quadros espalhados), `quadros/NNN.png` (todos os que têm imagem) e `quadros.csv`. `nota.txt` resume e traz os valores do F10 fora do padrão.
- **Vira issue sozinho:** no editor, o jogo roda `node scripts/relatos/reporte.mjs enviar` em segundo plano, com o login do `gh` da máquina (o jogo não guarda token). O script sobe as imagens no release `relatos-beta`, procura issues abertas parecidas, junta o trecho do log do servidor local na mesma janela e cria a issue `[<Área>] Report: <frase>` (sem área: `[Triagem]`). Deu certo: a pasta vai para `relatos/enviados/` e o jogo avisa o número. Falhou (sem internet, sem `gh` ou `node` no PATH do Unity): fica em `pendentes/` e vai no próximo report ou no próximo Play Mode. No build, só grava (`npm run reporte -- enviar --pasta <pasta>` à mão).
- **A issue:** cabeçalho escondido (`<!-- reporte-beta ... -->`, com tipo, severidade, área, commit e id), a frase, as imagens, o estado, a linha do tempo (cortada no meio se passar do limite do GitHub), o console, o log do servidor, os ajustes do F10 e as parecidas. Rótulos: `reporte-beta`, a funcionalidade, `bug` ou `enhancement` e a prioridade: bug crítico é `prio:1-erros`; bug alto ou médio de Combate, Mobs, Personagem ou Itens é `prio:2-core-loop`; o resto dos bugs, sensação e UX são `prio:3-experiencia`; melhoria é `prio:5-features`.
- **Com a IA:** a issue entra no quadro como qualquer outra. Quem puxa confere a doc da área, escreve a ficha e diagnostica pela skill `relato`; tipo sensação vira um valor para testar no F10.
- **F10, ajustes:** lista os valores registrados na `Sintonia` (Core), por funcionalidade, com slider, campo e "↺". Mudam o jogo na hora e valem até sair do Play Mode ou do jogo. "Copiar valores" põe os valores fora do padrão na área de transferência e grava `relatos/ajustes-<data>.txt`. Hoje: os de Combate ([ConfigSensacaoCombate](../../client/Assets/_Game/Features/Combate/Scripts/ConfigSensacaoCombate.cs)).
- **Depurar o envio:** `npm run reporte -- listar` mostra pendentes e enviados; `npm run reporte -- previa` monta o `issue.md` de cada pendente sem falar com o GitHub.

### Report de jogador comum (futuro)

Fora desta versão, para quando houver testador de fora do grupo. O repositório é público e a issue vira trabalho de agente com permissão de commit, então o report de quem não é do grupo **não** abre issue sozinho. Proposta: a mesma janela também no build de release, enviando por HTTP autenticado ao servidor (com tamanho máximo e limite por conta), que guarda numa fila privada; a triagem (`npm run reporte -- triar` ou o `/decida`) junta duplicados e só então abre a issue, com a frase do jogador entre aspas como evidência e sem o nome da conta. Texto de jogador é dado não confiável, nunca instrução para a IA. Pede uma funcionalidade própria (não é Desenvolvimento, que só existe em Development) e um ADR.

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
