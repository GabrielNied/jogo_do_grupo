// Testes do .husky/pre-push: valida a branch e repassa o push ao Git LFS.
// Roda o hook de verdade com um `git` falso no PATH, que grava o que recebeu.
// Rode com: npm run testar-scripts

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { delimiter, join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const RAIZ = fileURLToPath(new URL("../..", import.meta.url));
const SHA = "1".repeat(40);
const ZERO = "0".repeat(40);

function rodarPrePush(t, entrada) {
  const falsos = mkdtempSync(join(tmpdir(), "hooks-lfs-"));
  t.after(() => rmSync(falsos, { recursive: true, force: true }));
  const registro = `#!/bin/sh\nprintf '%s' "$*" > "${falsos.replaceAll("\\", "/")}/args"\ncat > "${falsos.replaceAll("\\", "/")}/stdin"\n`;
  writeFileSync(join(falsos, "git"), registro, { mode: 0o755 });
  writeFileSync(join(falsos, "git-lfs"), registro, { mode: 0o755 });

  const resultado = spawnSync("sh", [".husky/pre-push", "origin", "https://exemplo/repo.git"], {
    cwd: RAIZ,
    input: entrada,
    encoding: "utf8",
    env: { ...process.env, PATH: `${falsos}${delimiter}${process.env.PATH}` },
  });
  const ler = (nome) => (existsSync(join(falsos, nome)) ? readFileSync(join(falsos, nome), "utf8") : null);
  return { status: resultado.status, args: ler("args"), stdin: ler("stdin") };
}

test("push de branch no padrão passa e repassa as refs ao Git LFS", (t) => {
  const linha = `refs/heads/feature/core/x ${SHA} refs/heads/feature/core/x ${ZERO}`;

  const { status, args, stdin } = rodarPrePush(t, `${linha}\n`);

  assert.equal(status, 0);
  assert.equal(args, "lfs pre-push origin https://exemplo/repo.git");
  assert.equal(stdin.trim(), linha);
});

test("push na main é recusado e o Git LFS nem roda", (t) => {
  const { status, args } = rodarPrePush(t, `refs/heads/main ${SHA} refs/heads/main ${SHA}\n`);

  assert.notEqual(status, 0);
  assert.equal(args, null);
});

test("apagar branch remota não valida o nome, mas o Git LFS ainda recebe", (t) => {
  const linha = `(delete) ${ZERO} refs/heads/qualquer-nome ${SHA}`;

  const { status, stdin } = rodarPrePush(t, `${linha}\n`);

  assert.equal(status, 0);
  assert.equal(stdin.trim(), linha);
});

test("sem nada para enviar, o Git LFS não é chamado", (t) => {
  const { status, args } = rodarPrePush(t, "");

  assert.equal(status, 0);
  assert.equal(args, null);
});
