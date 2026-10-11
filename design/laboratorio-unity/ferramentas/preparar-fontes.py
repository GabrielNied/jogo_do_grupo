"""Gera instâncias estáticas OFL; execute a partir de qualquer diretório com fonttools 4.60.1."""
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "laboratorio-unity" / "fontes"
SOURCES = ROOT / "tipografia" / "fontes"


def preparar():
    DEST.mkdir(parents=True, exist_ok=True)
    for source, family, weight in [
        ("NunitoSans[YTLC,opsz,wdth,wght].ttf", "LumoraNunito", 800),
        ("SourceSans3[wght].ttf", "LumoraSource", 500),
        ("SourceSans3[wght].ttf", "LumoraSource", 600),
        ("SourceSans3[wght].ttf", "LumoraSource", 700),
    ]:
        font = TTFont(SOURCES / source, recalcTimestamp=False)
        axes = {axis.axisTag: axis.defaultValue for axis in font["fvar"].axes}
        axes["wght"] = weight
        if "opsz" in axes:
            axes["opsz"] = 28
        font = instantiateVariableFont(font, axes, inplace=True)
        # Famílias derivadas recebem nome próprio; avisos de copyright/OFL permanecem.
        for record in font["name"].names:
            values = {1: family, 2: f"Weight {weight}", 3: f"{family}-{weight}",
                      4: f"{family} {weight}", 6: f"{family}-{weight}",
                      16: family, 17: f"Weight {weight}"}
            if record.nameID in values:
                record.string = values[record.nameID].encode(record.getEncoding())
        output = DEST / f"{family}-{weight}.ttf"
        font.save(output)
        check = TTFont(output)
        assert "fvar" not in check, "Fonte ainda variável"
        for char in "áéíóúãõçÁÉÍÓÚÃÕÇ0123456789":
            assert ord(char) in check.getBestCmap(), f"Glifo ausente: {char}"
        print(output.name)


if __name__ == "__main__":
    preparar()
