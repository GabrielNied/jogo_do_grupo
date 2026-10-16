import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { test } from "node:test";
import { ajustarShimsHusky } from "./preparar-husky.mjs";

test("ajustarShimsHusky substitui shebang por #!/bin/sh e injeta caminho do Git em h", (t) => {
  const tempDir = mkdtempSync(join(tmpdir(), "husky-test-"));
  t.after(() => rmSync(tempDir, { recursive: true, force: true }));

  const pastaInterna = join(tempDir, "_");
  mkdirSync(pastaInterna, { recursive: true });

  const commitMsgConteudo = '#!/usr/bin/env sh\n. "$(dirname "$0")/h"\n';
  const hConteudo = '#!/usr/bin/env sh\nexport PATH="node_modules/.bin:$PATH"\nsh -e "$s" "$@"\n';

  writeFileSync(join(pastaInterna, "commit-msg"), commitMsgConteudo);
  writeFileSync(join(pastaInterna, "h"), hConteudo);

  ajustarShimsHusky(tempDir);

  const commitMsgAjustado = readFileSync(join(pastaInterna, "commit-msg"), "utf8");
  const hAjustado = readFileSync(join(pastaInterna, "h"), "utf8");

  assert.ok(commitMsgAjustado.startsWith("#!/bin/sh"), "commit-msg deve iniciar com #!/bin/sh");
  assert.ok(hAjustado.startsWith("#!/bin/sh"), "h deve iniciar com #!/bin/sh");
  assert.ok(hAjustado.includes("Garante utilitários POSIX"), "h deve conter bloco de garantia POSIX");
  assert.ok(hAjustado.includes("C:/Program Files/Git"), "h deve conter fallback para pasta do Git");

  // Idempotência: rodar novamente não duplica o bloco
  ajustarShimsHusky(tempDir);
  const hRepetido = readFileSync(join(pastaInterna, "h"), "utf8");
  const ocorrencias = (hRepetido.match(/Garante utilitários POSIX/g) || []).length;
  assert.equal(ocorrencias, 1, "bloco não deve ser duplicado");
});
