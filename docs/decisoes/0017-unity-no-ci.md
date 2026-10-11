# 0017. Unity no CI e agentes sem Unity

**Data:** 2026-10-11
**Status:** proposta

## Contexto

Toda verificação do client dependia do Unity instalado na máquina de quem fez a mudança: `npm run unity:compilar` usa as DLLs do editor e a `Library/` do checkout principal, `npm run unity:testes` usa o Unity CLI, e o CI só compilava server e shared. Os agentes na nuvem não têm Unity. Eles paravam o PR em rascunho e pediam à pessoa para rodar comandos na máquina dela, e os agentes locais disputavam o único editor. Os `.meta` de script e pasta já saíam sem Unity (`npm run unity:meta`, #141), mas `.asmdef` e estilos de UI ainda dependiam do editor.

## Decisão

1. **O CI compila e testa o client.** O workflow [client.yml](../../.github/workflows/client.yml) roda o GameCI (`game-ci/unity-test-runner`) com a versão do projeto. Ele compila todos os assemblies e roda os testes EditMode, com a `Library/` em cache. Como leva uns 10 minutos, **não roda em cada PR**: roda depois do merge na `main` quando o merge mexe em `client/` ou `shared/Package/` (merges seguidos cancelam a execução anterior, então um lote gera uma execução só), num PR com a label `testar-unity` e à mão pelo Actions. Se falhar na `main`, o workflow abre uma pendência `pendente-unity` (item 4). A licença é Personal, nos secrets `UNITY_LICENSE`, `UNITY_EMAIL` e `UNITY_PASSWORD`. O check não é obrigatório e não segura nenhum PR.
2. **Sem Unity, a verificação local é pulada, não repetida.** `unity:compilar`, `unity:testes`, `unity:recompilar` e `unity:importar` saem 0 com um aviso quando o ambiente não tem nenhum Unity. O agente não tenta de novo nem pede à pessoa para rodar: abre o PR e registra a pendência (item 4). Unity de outra versão continua sendo erro, e `UNITY_OBRIGATORIO=1` transforma o pulo em erro. `unity:executar` (gerar conteúdo) não tem substituto e continua falhando.
3. **`npm run unity:meta` cobre também `.asmdef`, `.uss`, `.tss` e textos.** Esses `.meta` também são texto fixo mais um guid, conferidos por teste contra os que o Unity já gravou no repo. Modelos, texturas, prefabs, cenas e `.uxml` continuam saindo só do Unity.

4. **O que só o Unity faz vira pendência, e o PR não espera.** Sem Unity, o agente abre o PR normal (pode ir para merge sem essa validação) e roda `npm run tarefa -- pendencia-unity <PR> --falta "..."`. Isso cria uma issue `pendente-unity` que o PR só menciona, sem `Closes`, para continuar aberta depois do merge. Numa máquina com Unity, um hook de início de sessão avisa o agente das pendências abertas. Quem puxa trabalha na branch do PR, se ele ainda estiver aberto, ou numa branch nova a partir da `main`. Assim o trabalho anda na nuvem, e nada que faltou validar se perde.

## Consequências

- Agentes de código (`client-*`, `shared-contratos`, `regras-do-jogo`) podem trabalhar inteiros na nuvem. O Unity local fica para os artistas e para o teste visual de uma pessoa.
- Cada execução gasta de 10 a 15 minutos de Actions (o plano gratuito de repo privado tem 2.000 por mês). Rodando por lote de merges, e não por PR, isso fica longe do limite.
- Um erro de compilação do client pode chegar à `main` e só aparecer na execução depois do merge. Ele vira pendência e é corrigido em seguida. Quem quiser a garantia antes do merge usa a label `testar-unity`.
- A licença Personal fica num secret do repositório. Se a senha da conta mudar ou a licença expirar, o job falha até alguém atualizar o secret. Só uma pessoa faz isso, numa máquina com Unity e acesso de admin ao repo. Os outros devs não precisam fazer nada:
  1. Unity Hub > Preferences > Licenses > **Add** > **Get a free personal license** (gera `C:\ProgramData\Unity\Unity_lic.ulf`; o `UnityEntitlementLicense.xml` não serve).
  2. No PowerShell: `cmd /c 'gh secret set UNITY_LICENSE < "C:\ProgramData\Unity\Unity_lic.ulf"'` (o `<` não funciona direto no PowerShell).
  3. `gh secret set UNITY_EMAIL` e `gh secret set UNITY_PASSWORD`, da mesma conta.
- O workflow fixa a imagem do GameCI na versão do Unity do projeto. Quem atualizar o Unity atualiza a imagem no `client.yml` também.
- Até alguém cadastrar os secrets, o job só avisa e passa. Nesse intervalo, o client de um agente sem Unity só é compilado quando alguém puxa a pendência.
- Como o PR pode ir para merge antes da validação, a `main` pode ficar com o client quebrado até a pendência ser resolvida. O CI com licença reduz esse risco; a pendência garante que alguém vai resolver.
- O teste visual no editor continua sendo de uma pessoa.
