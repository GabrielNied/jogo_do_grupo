# 0058. Recuperação de conta por e-mail com o Brevo

**Data:** 2026-10-23
**Status:** aceita

## Contexto

A conta precisa de um jeito de recuperar o acesso sem o dono do servidor redefinir a senha à mão (#1167). O plano era um código de 6 dígitos por e-mail, válido por 15 minutos, num serviço com plano grátis ([ADR 0021](0021-hospedagem-gratuita.md), custo zero). Os candidatos eram Resend e Brevo. O Resend (3.000 por mês, 100 por dia) só envia para qualquer destinatário com domínio próprio verificado, e o grupo não tem domínio. O Brevo (300 por dia) aceita um remetente verificado só pelo e-mail, sem domínio.

## Decisão

- **Provedor:** Brevo, pela API transacional (`POST https://api.brevo.com/v3/smtp/email`, header `api-key`). A chave fica na variável de ambiente `Email__ChaveApiBrevo` e o remetente verificado em `Email__Remetente`. Nada disso vai para o repositório. Com chave e sem remetente, o servidor não sobe.
- **Interface:** o envio fica atrás de `IProvedorDeEmail` (`server/.../Core/Email/`). Para trocar de provedor, troca-se a implementação registrada. Os testes registram um falso. Sem chave, um provedor que só registra no log: em Development, com o texto (código incluso), para testar no PC; fora de Development, só um aviso.
- **Fila:** as rotas põem o e-mail na `FilaDeEmails`, que um serviço em segundo plano esvazia. A resposta não espera o provedor, então o tempo de resposta não revela se o e-mail tem conta. Falha de envio vai para o log, sem o texto.
- **Código:** 6 dígitos de `RandomNumberGenerator`. O banco guarda só o hash (o mesmo `PasswordHasher` da senha), na própria `Conta`: um código ativo por conta, e cada pedido novo substitui o anterior. Vale 15 minutos, cai depois de 5 tentativas erradas e serve uma vez só. O consumo é um `ExecuteUpdate` condicionado ao hash lido, então duas redefinições ao mesmo tempo não usam o mesmo código.
- **Limites:** por IP, no rate limiter (5 pedidos e 10 redefinições por hora, `Contas:Recuperacao:*`). Por conta, em memória (3 pedidos por hora): acima disso a resposta é a mesma e nada é enviado. O pedido sempre responde 202, exista ou não a conta.
- **E-mail na conta:** opcional, único (índice em `EmailNormalizado`). Informar ou trocar o e-mail pede a senha atual, porque quem controla o e-mail controla a conta.
- **Redefinir:** renova o `CarimboSeguranca` (como a troca de senha, [ADR 0052](0052-rate-limiting-e-tokens-revogaveis.md)), derruba a sessão UDP da conta e libera o bloqueio de login por falhas.

## Consequências

- Recuperar a conta não custa nada até 300 e-mails por dia, bem acima do que um grupo pequeno usa. Para ativar, é preciso criar a conta no Brevo, verificar o remetente e definir as duas variáveis no servidor.
- E-mail de remetente comum (sem domínio próprio, sem SPF e DKIM do grupo) tem mais chance de cair no spam. A tela avisa para conferir a pasta de spam.
- O limite por conta fica em memória e zera quando o servidor reinicia. Com uma instância só, como hoje, basta. O limite por IP continua valendo.
- Não há confirmação de posse do e-mail no registro (fora do escopo da #1167). Um e-mail digitado errado só é percebido na hora de recuperar.
