import assert from "node:assert/strict";
import { test } from "node:test";
import { NET_MANAGERS, verificarCanais } from "./conferir-contratos-rede.mjs";

const SHARED_OK = "public const byte Canais = 32;";
const CANAIS_OK = "ChannelsCount = TransporteTempoReal.Canais,";

function netManagers(texto = CANAIS_OK) {
  return Object.fromEntries(NET_MANAGERS.map((c) => [c, texto]));
}

test("aceita os três lados usando TransporteTempoReal.Canais", () => {
  assert.deepEqual(verificarCanais(SHARED_OK, netManagers()), []);
});

test("rejeita ChannelsCount literal", () => {
  const n = netManagers();
  n[NET_MANAGERS[1]] = "ChannelsCount = 32,";
  const erros = verificarCanais(SHARED_OK, n);
  assert.equal(erros.length, 1);
  assert.ok(erros[0].includes(NET_MANAGERS[1]));
});

test("rejeita ChannelsCount de outra constante", () => {
  const n = netManagers();
  n[NET_MANAGERS[0]] = "ChannelsCount = MeusCanais,";
  assert.equal(verificarCanais(SHARED_OK, n).length, 1);
});

test("rejeita NetManager sem ChannelsCount (padrão de 1 canal, #826)", () => {
  const n = netManagers();
  n[NET_MANAGERS[1]] = "new NetManager(_ouvinte) { AutoRecycle = true };";
  assert.equal(verificarCanais(SHARED_OK, n).length, 1);
});

test("rejeita shared sem a constante", () => {
  assert.equal(verificarCanais("// vazio", netManagers()).length, 1);
});
