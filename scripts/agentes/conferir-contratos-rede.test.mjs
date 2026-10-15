import assert from "node:assert/strict";
import { test } from "node:test";
import {
  ARQUIVOS,
  verificarCanais,
  verificarPareamentoNaBranch,
} from "./conferir-contratos-rede.mjs";

const CANAIS_OK = "ChannelsCount = TransporteTempoReal.Canais";

function conteudoBase() {
  return {
    sharedTransporte: "public const byte Canais = 32;",
    servidor: CANAIS_OK,
    clientTransporte: CANAIS_OK,
    clientTesteUdp: CANAIS_OK,
    clientTesteUnity: "Sequenciada_NoUltimoCanalDoServidor_Chega",
  };
}

test("aceita os três lados usando TransporteTempoReal.Canais", () => {
  assert.deepEqual(verificarCanais(conteudoBase()), []);
});

test("rejeita ChannelsCount literal no client", () => {
  const c = conteudoBase();
  c.clientTransporte = "ChannelsCount = 32,";
  const erros = verificarCanais(c);
  assert.ok(erros.some((e) => e.includes(ARQUIVOS.clientTransporte)));
});

test("exige teste EditMode do transporte Unity", () => {
  const c = conteudoBase();
  c.clientTesteUnity = "// vazio";
  assert.ok(verificarCanais(c).length > 0);
});

test("pareamento: shared mudou sem TransporteLiteNetLib falha", () => {
  const erros = verificarPareamentoNaBranch([ARQUIVOS.sharedTransporte], "");
  assert.equal(erros.length, 1);
});

test("pareamento: shared + client ok", () => {
  const erros = verificarPareamentoNaBranch(
    [ARQUIVOS.sharedTransporte, ARQUIVOS.clientTransporte],
    "",
  );
  assert.deepEqual(erros, []);
});

test("pareamento: ServidorTempoReal com Canais exige client", () => {
  const erros = verificarPareamentoNaBranch(
    [ARQUIVOS.servidor],
    "+ ChannelsCount = TransporteTempoReal.Canais",
  );
  assert.equal(erros.length, 1);
});

test("pareamento: ServidorTempoReal sem mudança de transporte não exige client", () => {
  const erros = verificarPareamentoNaBranch(
    [ARQUIVOS.servidor],
    "+ comentario qualquer",
  );
  assert.deepEqual(erros, []);
});
