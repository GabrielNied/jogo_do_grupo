import assert from "node:assert/strict";
import { test } from "node:test";
import { travaAbandonada, travaDoComentario, ultimaTrava } from "./trava-abandonada.mjs";

const COMENTARIO = `🚀 **Tarefa em andamento**

- **Responsável:** agente server-core
- **Branch:** \`fix/core/limites-protocolo-udp\`
- **Início:** 2026-10-12T02:13:39.559Z
`;

const INICIO = Date.parse("2026-10-12T02:13:39.559Z");
const DUAS_HORAS = 2 * 3_600_000;

test("lê a branch e o início do comentário do puxar", () => {
  assert.deepEqual(travaDoComentario(COMENTARIO), {
    branch: "fix/core/limites-protocolo-udp",
    inicioMs: Date.parse("2026-10-12T02:13:39.559Z"),
  });
  assert.equal(travaDoComentario("outro comentário"), null);
});

test("vale o puxar mais recente", () => {
  const antiga = COMENTARIO.replace("limites-protocolo-udp", "velha");
  const nova = COMENTARIO.replace("limites-protocolo-udp", "nova");
  assert.equal(ultimaTrava([{ body: antiga }, { body: "nota" }, { body: nova }]).branch, "fix/core/nova");
});

test("sai depois de 2 h sem branch no origin e sem PR", () => {
  const trava = travaDoComentario(COMENTARIO);
  assert.equal(
    travaAbandonada({ trava, agoraMs: INICIO + DUAS_HORAS, branchNoOrigin: false, prAberto: false }),
    true,
  );
  assert.equal(
    travaAbandonada({ trava, agoraMs: INICIO + DUAS_HORAS - 1, branchNoOrigin: false, prAberto: false }),
    false,
  );
});

test("branch no origin ou PR aberto segura a trava", () => {
  const trava = travaDoComentario(COMENTARIO);
  assert.equal(
    travaAbandonada({ trava, agoraMs: INICIO + DUAS_HORAS, branchNoOrigin: true, prAberto: false }),
    false,
  );
  assert.equal(
    travaAbandonada({ trava, agoraMs: INICIO + DUAS_HORAS, branchNoOrigin: false, prAberto: true }),
    false,
  );
  assert.equal(
    travaAbandonada({ trava: null, agoraMs: INICIO + DUAS_HORAS, branchNoOrigin: false, prAberto: false }),
    false,
  );
});
