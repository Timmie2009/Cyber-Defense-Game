from pathlib import Path

from flask import Flask, abort, jsonify, request, send_from_directory, url_for

FOTO_MAP = Path(__file__).resolve().parent / "fotos"

FOTOS = {
       "1": {"A": "keuze_a.png", "B": "keuze_b.png"},
   }

app = Flask(__name__)
laatste_keuze = None


@app.after_request
def cors(response):
    response.headers["Access-Control-Allow-Origin"] = "*"
    response.headers["Access-Control-Allow-Headers"] = "Content-Type"
    response.headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS"
    return response


def als_json(keuze):
    return {
        **keuze,
        "foto_url": url_for("foto", naam=keuze["foto"], _external=True),
    }


@app.route("/api/keuze", methods=["POST", "OPTIONS"])
def kies():
    global laatste_keuze
    if request.method == "OPTIONS":
        return "", 204

    data = request.get_json(silent=True) or {}
    vraag = str(data.get("vraag", ""))
    knop = str(data.get("knop", ""))
    foto_naam = FOTOS.get(vraag, {}).get(knop)
    if foto_naam is None:
        return jsonify(fout="Onbekende vraag of knop"), 400

    laatste_keuze = {"vraag": vraag, "knop": knop, "foto": foto_naam}
    return jsonify(als_json(laatste_keuze))


@app.get("/api/keuze")
def huidige_keuze():
    if laatste_keuze is None:
        return jsonify(fout="Nog geen keuze gemaakt"), 404
    return jsonify(als_json(laatste_keuze))


@app.get("/fotos/<path:naam>")
def foto(naam):
    if not (FOTO_MAP / naam).is_file():
        abort(404)
    return send_from_directory(FOTO_MAP, naam)


@app.get("/toon")
def toon():
    """Pagina die altijd de laatst gekozen foto laat zien."""
    return """<!doctype html><title>Gekozen foto</title>
<p id="tekst">Nog geen keuze</p><img id="foto" style="max-width:100%">
<script>
async function ververs() {
  const r = await fetch('/api/keuze');
  if (!r.ok) return;
  const k = await r.json();
  document.getElementById('tekst').textContent = `Vraag ${k.vraag}: knop ${k.knop}`;
  document.getElementById('foto').src = k.foto_url;
}
ververs(); setInterval(ververs, 1000);
</script>"""


if __name__ == "__main__":
    FOTO_MAP.mkdir(exist_ok=True)
    app.run(host="0.0.0.0", port=5000)