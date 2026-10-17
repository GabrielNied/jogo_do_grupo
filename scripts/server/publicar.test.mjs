import assert from "node:assert/strict";
import test from "node:test";
import { lerArgumentos, montarComandoPublish } from "./publicar.mjs";

test("lerArgumentos usa linux-arm64 e Release por padrão", () => {
  const args = lerArgumentos([]);
  assert.equal(args.rid, "linux-arm64");
  assert.equal(args.config, "Release");
  assert.equal(args.saida, "server/publish/linux-arm64");
});

test("lerArgumentos aceita rid, config e saida customizados", () => {
  const args = lerArgumentos(["--rid", "linux-x64", "--config", "Debug", "--saida", "dist/custom"]);
  assert.equal(args.rid, "linux-x64");
  assert.equal(args.config, "Debug");
  assert.equal(args.saida, "dist/custom");
});

test("lerArgumentos recusa argumento desconhecido", () => {
  assert.throws(() => lerArgumentos(["--invalido"]), /Argumento desconhecido/);
});

test("montarComandoPublish gera argumentos corretos para o dotnet publish", () => {
  const cmd = montarComandoPublish({
    rid: "linux-arm64",
    config: "Release",
    saida: "server/publish/linux-arm64",
  });

  assert.deepEqual(cmd, [
    "publish",
    "server/src/JogoDoGrupo.Server/JogoDoGrupo.Server.csproj",
    "-c",
    "Release",
    "-r",
    "linux-arm64",
    "--self-contained",
    "-o",
    "server/publish/linux-arm64",
  ]);
});
