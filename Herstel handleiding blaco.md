# Herstelhandleiding back-up Cyber Defense Quiz

![Overzicht](screenshots-commando's/voorblad-foto.png)

Met deze handleiding test je of de back-up van de Pi werkt en of je de game kunt herstellen.

- **Testpersoon:**
- **Datum:**

---

## Zo vul je deze handleiding in

**Programma:** open dit bestand in VS Code. Druk op `Ctrl + Shift + V` om het voorbeeld (preview) te zien.

**Uitslag invullen**
- Lees per stap wat de bedoeling is en wat je moet doen.
- Voer het commando van een stap uit op de Pi.
- Vul de uitkomst van de terminal in bij `Resultaat`.
- Bij Status vul je **Gelukt** in als het resultaat hetzelfde is als bij Verwacht. Anders vul je **Niet gelukt** in.
- Voer elk commando apart in en wacht tot `Jason@Bjorn` terugkomt.

## Voorbereiding

1. Open **PowerShell** en log in op de Pi (vul daarna het wachtwoord in):
   ```bash
   ssh Jason@100.85.165.111
   ```
2. Zoek de nieuwste back-up:
   ```bash
   NIEUWSTE=$(rclone lsf gdrive-crypt: | sort | tail -n 1); echo $NIEUWSTE
   ```

---

## Stap 1 – Cron ingesteld (T1)

**Doel:** controleren dat de back-up automatisch start.

```bash
crontab -l
```

- **Verwacht:** een regel met `0 2 * * *` en `backup.sh`
- **Resultaat:** ...
- **Status:** 

---

## Stap 2 – Dagelijkse back-up (T2)

**Doel:** controleren dat er op het ingestelde tijdstip een back-up wordt gemaakt.

```bash
rclone lsl gdrive-crypt:
```

- **Verwacht:** één bestand per dag, met de naam `backup_jjjj-mm-dd.tar.gz`
- **Resultaat:** ...
- **Status:** 

---

## Stap 3 – Log zonder fouten (T3)

**Doel:** controleren dat het script zonder fouten draait.

```bash
tail -n 30 ~/backup.log
grep -iE "error|fout|failed" ~/backup.log
```

- **Verwacht:**
  1. Elke nacht een "gestart"- en een "klaar"-regel.
  2. Bij `grep` komt de prompt meteen terug, dus er zijn geen fouten.
- **Resultaat:** ...
- **Status:** 

---

## Stap 4 – Versleuteld in Drive (T4)

**Doel:** het back-upbestand mag niet leesbaar zijn.

```bash
rclone ls gdrive:pi-backup
```
Kijk daarna ook in Google Drive bij de back-up.

- **Verwacht:** onleesbare bestandsnamen
- **Resultaat:** …
- **Status:** 

---

## Stap 5 – Onbruikbaar zonder wachtwoord (T5)

**Doel:** zonder wachtwoord kun je het bestand niet openen.

```bash
rclone cat gdrive:pi-backup/$(rclone lsf gdrive:pi-backup | tail -n 1) | tar -tzf -
```

- **Verwacht:** de fout `not in gzip format`
- **Resultaat:** …
- **Status:** 

---

## Stap 6 – Niet beschadigd (T6)

**Doel:** het back-upbestand moet heel zijn.

```bash
rclone cat gdrive-crypt:$NIEUWSTE | gzip -t && echo OK
```

- **Verwacht:** `OK`
- **Resultaat:** …
- **Status:** 

---

## Stap 7 – Compleet (T7)

**Doel:** alle bestanden moeten er nog zijn.

```bash
rclone cat gdrive-crypt:$NIEUWSTE | tar -tzf - | grep -cE "(server|quiz-data|wwwroot)/"
rclone cat gdrive-crypt:$NIEUWSTE | tar -tzf - | grep docker-compose.yml
```

- **Verwacht:**
  1. Een getal groter dan 1
  2. `./docker-compose.yml`
- **Resultaat:** …
- **Status:** 

---

## Stap 8 – Hersteltest (T8)

**Doel:** de game herstellen vanuit de back-up.

De back-up wordt teruggezet in een aparte testmap en draait op andere poorten (8081 en 8082), naast de echte game. De echte game wordt niet aangeraakt.

**8.1 Starttijd noteren**
```bash
date
```

**8.2 Testmap maken en de back-up uitpakken**
```bash
mkdir ~/hersteltest
rclone cat gdrive-crypt:$NIEUWSTE | tar -xzf - -C ~/hersteltest
```

**8.3 Testserver starten (poort 8081)**
```bash
docker run -d --name test-server -p 8081:8080 -w /app \
  -v ~/hersteltest/server:/app \
  -v ~/hersteltest/quiz-data:/app/data \
  mcr.microsoft.com/dotnet/aspnet:10.0 dotnet Cyber_Defense_Game.Server.dll
```

**8.4 Testwebsite starten (poort 8082)**
```bash
docker run -d --name test-web -p 8082:80 \
  -v ~/hersteltest/wwwroot:/usr/share/nginx/html:ro nginx:alpine
```

**8.5 Wachten en controleren of de containers draaien**
```bash
sleep 10
docker ps --filter name=test-
```

**8.6 Server testen met curl**
```bash
curl -i -X POST "http://localhost:8081/gamehub/negotiate?negotiateVersion=1"
```

**8.7 In de browser testen**
Open `http://145.93.116.9:8082`. De game zou moeten laden.

De website maakt verbinding met de echte server (cyberquiz…ts.net), dus een CORS-fout in de browser is normaal. Daarom test je de herstelde server apart met curl.

**8.8 Eindtijd noteren**
```bash
date
```

- **Verwacht:** curl geeft `HTTP/1.1 200 OK`, de game laadt via poort 8082 en de RTO (hersteltijd) is maximaal 20 minuten.
- **Resultaat:** …
- **Starttijd / eindtijd:** … / …
- **Status:** 

---

## Stap 9 – Opruimen (verplicht!)

In `~/hersteltest/.env` staan geheimen die niet versleuteld zijn. Als je niet opruimt, blijven poort 8081 en 8082 ook open.

1. Verwijder de testcontainers en de testmap:
   ```bash
   docker rm -f test-server test-web
   rm -rf ~/hersteltest
   ```
2. Controleer het resultaat:
   ```bash
   docker ps
   ```
   - Er staat geen testcontainer meer.
   - De echte game staat op **Up**.

---

## Stap 10 – Oude back-ups verwijderd (T9)

**Doel:** back-ups ouder dan 7 dagen moeten verwijderd zijn.
**Let op:** doe deze stap pas als de back-up minstens 8 dagen draait.

```bash
rclone lsl gdrive-crypt:
```

- **Verwacht:** maximaal 7 back-ups, geen enkele ouder dan 7 dagen
- **Resultaat:** …
- **Status:** 