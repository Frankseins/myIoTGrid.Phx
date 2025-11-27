<p align="center">
  <br><br>
</p>

<h1 align="center">🌐 myIoTGrid</h1>

<h3 align="center">Dein Zuhause. Intelligent.</h3>

<p align="center">
  <em>Die erste IoT-Plattform, die mitdenkt.</em>
</p>

<br>

<p align="center">
  <a href="https://myiotgrid.cloud">Website</a>
  &nbsp;&nbsp;·&nbsp;&nbsp;
  <a href="https://mysocialcare-doku.atlassian.net/wiki/spaces/myIoTGrid">Dokumentation</a>
  &nbsp;&nbsp;·&nbsp;&nbsp;
  <a href="#-schnellstart">Schnellstart</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/License-MIT-brightgreen?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/Status-Active-success?style=flat-square" alt="Active"/>
  <img src="https://img.shields.io/badge/AI-Native-blue?style=flat-square" alt="AI Native"/>
  <img src="https://img.shields.io/badge/Made_with-❤️-red?style=flat-square" alt="Made with Love"/>
</p>

<br><br>

---

<br>

<h2 align="center">Sensoren sammeln Daten.<br><strong>myIoTGrid versteht sie.</strong></h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#e8f5e9', 'primaryTextColor': '#1b5e20', 'primaryBorderColor': '#4caf50', 'lineColor': '#81c784', 'secondaryColor': '#fff3e0', 'tertiaryColor': '#e3f2fd'}}}%%

block-beta
    columns 3
    
    block:vorher:1
        columns 1
        A["😕 VORHER"]
        B["Temperatur: 18.5°C"]
        C["Luftfeuchte: 73%"]
        D["CO₂: 892 ppm"]
        E["\"Okay... und?\""]
    end
    
    space
    
    block:nachher:1
        columns 1
        F["🤖 MIT myIoTGrid"]
        G["🟡 Lüften in 12 Min"]
        H["🟢 Keller optimal"]  
        I["🔵 Energiespartipp"]
        J["\"Verstanden!\""]
    end
```

<br><br>

---

<br>

<h2 align="center">Die Architektur</h2>

<p align="center"><em>Drei Komponenten. Ein System.</em></p>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#f3e5f5', 'primaryTextColor': '#4a148c', 'primaryBorderColor': '#9c27b0', 'lineColor': '#ba68c8', 'secondaryColor': '#e8f5e9', 'tertiaryColor': '#e3f2fd'}}}%%

flowchart LR
    subgraph SENSOR ["📡 GRID.SENSOR"]
        S1["🌡️ Temperatur"]
        S2["💧 Feuchte"]
        S3["💨 CO₂"]
        S4["🌱 Boden"]
    end
    
    subgraph HUB ["🧠 GRID.HUB"]
        H1["📊 Dashboard"]
        H2["🤖 Edge-KI"]
        H3["💾 SQLite"]
        H4["🏠 Matter"]
    end
    
    subgraph CLOUD ["☁️ GRID.CLOUD"]
        C1["🗺️ Community Map"]
        C2["🤖 Cloud-KI"]
        C3["🤝 Sharing"]
        C4["📡 Open API"]
    end
    
    SENSOR -->|MQTT| HUB
    HUB -->|HTTPS| CLOUD
    
    style SENSOR fill:#e8f5e9,stroke:#4caf50,stroke-width:2px
    style HUB fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
    style CLOUD fill:#fff3e0,stroke:#ff9800,stroke-width:2px
```

<br>

<table align="center">
<tr>
<td align="center" width="33%">

### 📡 Grid.Sensor

**ESP32 · Ab 10€**

Misst alles. Überall.

</td>
<td align="center" width="33%">

### 🧠 Grid.Hub

**Raspberry Pi · ~50€**

Denkt mit. Auch offline.

</td>
<td align="center" width="33%">

### ☁️ Grid.Cloud

**Optional · Kostenlos**

Verbindet. Wenn du willst.

</td>
</tr>
</table>

<br><br>

---

<br>

<h2 align="center">KI, die für dich arbeitet</h2>

<p align="center"><em>Nicht irgendwann. Von Anfang an.</em></p>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#ffebee', 'primaryTextColor': '#b71c1c'}}}%%

flowchart TB
    subgraph KI ["🤖 KI-WARNSTUFEN"]
        direction LR
        K1["🔴<br>KRITISCH<br><small>Sofort handeln</small>"]
        K2["🟡<br>WARNUNG<br><small>Bald handeln</small>"]
        K3["🔵<br>HINWEIS<br><small>Optimieren</small>"]
        K4["🟢<br>ALLES OK<br><small>Entspannen</small>"]
    end
    
    style K1 fill:#ffcdd2,stroke:#e53935,stroke-width:2px
    style K2 fill:#fff9c4,stroke:#fdd835,stroke-width:2px
    style K3 fill:#bbdefb,stroke:#1e88e5,stroke-width:2px
    style K4 fill:#c8e6c9,stroke:#43a047,stroke-width:2px
```

<br>

<p align="center">
  <strong>Schimmelwarnung.</strong> 3 Tage bevor du ihn siehst.<br><br>
  <strong>Frostgefahr.</strong> 12 Stunden bevor es kalt wird.<br><br>
  <strong>Luftqualität.</strong> Bevor du Kopfschmerzen bekommst.
</p>

<br><br>

---

<br>

<h2 align="center">Was die KI kann</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#e8eaf6', 'primaryTextColor': '#283593', 'primaryBorderColor': '#3f51b5'}}}%%

mindmap
  root((🤖 KI))
    🔍 Anomalie-Erkennung
      Lernt normales Verhalten
      Warnt bei Abweichungen
      Funktioniert offline
    ⚠️ Prädiktive Warnungen
      Schimmel in 3 Tagen
      Frost in 12 Stunden
      Hochwasser in 48h
    🧠 Community Intelligence
      Vergleich mit anderen
      Regionale Muster
      Anonymisiert
    💡 Empfehlungen
      Lüftungstipps
      Energiesparen
      Bewässerung
```

<br><br>

---

<br>

<h2 align="center">Privatsphäre ist kein Feature.<br>Es ist das Fundament.</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#fafafa'}}}%%

flowchart TB
    A["🔒 PRIVAT<br><small>Nur du siehst deine Daten</small>"]
    B["👥 GETEILT<br><small>Familie · Freunde · Handwerker</small>"]
    C["🏘️ COMMUNITY<br><small>Anonymisiert · Alle profitieren</small>"]
    D["🌍 ÖFFENTLICH<br><small>Open Data · Wissenschaft</small>"]
    
    A --> B
    B --> C
    C --> D
    
    style A fill:#e8f5e9,stroke:#4caf50,stroke-width:2px
    style B fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
    style C fill:#fff3e0,stroke:#ff9800,stroke-width:2px
    style D fill:#fce4ec,stroke:#e91e63,stroke-width:2px
```

<br>

<p align="center">
  <em>Jeder Sensor startet privat.<br>Teilen ist immer deine Entscheidung.</em>
</p>

<br><br>

---

<br>

<h2 align="center">Je mehr mitmachen,<br>desto schlauer für alle.</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#e0f7fa', 'primaryTextColor': '#006064'}}}%%

flowchart LR
    A["1<br>Sensor"]
    B["100<br>Sensoren"]
    C["10.000<br>Sensoren"]
    D["1.000.000<br>Sensoren"]
    
    A -->|"Deine Daten"| B
    B -->|"Lokale Muster"| C
    C -->|"Stadtweite Prognosen"| D
    D -->|"Klimaforschung"| E["🌍"]
    
    style A fill:#b2ebf2,stroke:#00acc1,stroke-width:2px
    style B fill:#80deea,stroke:#00acc1,stroke-width:2px
    style C fill:#4dd0e1,stroke:#00acc1,stroke-width:2px
    style D fill:#26c6da,stroke:#00acc1,stroke-width:2px
    style E fill:#00bcd4,stroke:#00acc1,stroke-width:3px
```

<br>

<p align="center">
  <strong>Community Intelligence.</strong><br>
  <em>Die KI lernt von allen. Ohne individuelle Daten preiszugeben.</em>
</p>

<br><br>

---

<br>

<h2 align="center">Smart Home Integration</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#fafafa'}}}%%

flowchart TB
    HUB["🧠 Grid.Hub<br><small>Matter Bridge</small>"]
    
    HUB --> APPLE["🍎 Apple Home"]
    HUB --> GOOGLE["🏠 Google Home"]
    HUB --> ALEXA["🔵 Amazon Alexa"]
    
    style HUB fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    style APPLE fill:#f5f5f5,stroke:#000000,stroke-width:2px
    style GOOGLE fill:#e8f5e9,stroke:#4caf50,stroke-width:2px
    style ALEXA fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
```

<br>

<p align="center">
  <em>Via <strong>Matter</strong> – dem neuen Smart-Home-Standard.</em>
</p>

<br><br>

---

<br>

<h2 align="center">Sensoren für alles</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#fff8e1'}}}%%

mindmap
  root((📡 Sensoren))
    🌡️ Klima
      Temperatur
      Luftfeuchte
      Luftdruck
    💨 Luft
      CO₂
      Feinstaub
      VOC
    🌱 Garten
      Bodenfeuchte
      Licht
      UV-Index
    🌧️ Wetter
      Regen
      Wind
      Sonne
    🏠 Haus
      Bewegung
      Türen
      Schall
```

<br>

<p align="center">
  <strong>+ 34.000 externe Sensoren</strong><br>
  <em>Sensor.Community · OpenWeather · DWD</em>
</p>

<br><br>

---

<br>

<h2 align="center">Für alle, die mehr wollen</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#f3e5f5'}}}%%

flowchart LR
    subgraph USERS [" "]
        direction TB
        U1["🔧 Maker<br><small>Volle Kontrolle</small>"]
        U2["🏠 Familien<br><small>Ein Dashboard</small>"]
        U3["🏫 Schulen<br><small>MINT lernen</small>"]
        U4["🌾 Landwirte<br><small>Smarte Felder</small>"]
        U5["🏙️ Städte<br><small>Smart City</small>"]
        U6["🔬 Forscher<br><small>Open Data</small>"]
    end
    
    style U1 fill:#e8f5e9,stroke:#4caf50,stroke-width:2px
    style U2 fill:#e3f2fd,stroke:#2196f3,stroke-width:2px
    style U3 fill:#fff3e0,stroke:#ff9800,stroke-width:2px
    style U4 fill:#f1f8e9,stroke:#8bc34a,stroke-width:2px
    style U5 fill:#e0f2f1,stroke:#009688,stroke-width:2px
    style U6 fill:#fce4ec,stroke:#e91e63,stroke-width:2px
```

<br><br>

---

<br>

<h2 align="center">Roadmap</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#e8f5e9'}}}%%

timeline
    title myIoTGrid Roadmap 2025-2026
    
    section 2025
        Q1 : 🎯 Phase 1
           : Hub MVP
           : Matter Bridge
           : Edge-KI Basis
        Q2 : Phase 2
           : Sensor Plugins
           : Mehr Sensortypen
        Q3 : Phase 3
           : Cloud MVP
           : Cloud-KI
           : Multi-Tenant
        Q4 : Phase 4
           : Community Features
           : Sharing
           : Map
    
    section 2026
        Q1 : Phase 5
           : Externe Quellen
           : Sensor.Community
           : OpenWeather
        Q2 : Phase 6
           : Prädiktive KI
           : Warnungen
           : Prognosen
        Q3 : Phase 7
           : Mobile App
           : iOS & Android
```

<br><br>

---

<br>

<h2 align="center">🚀 Schnellstart</h2>

<p align="center"><em>In 5 Minuten live.</em></p>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#e8f5e9'}}}%%

flowchart LR
    A["1️⃣<br>Hub starten"] --> B["2️⃣<br>Sensor flashen"] --> C["3️⃣<br>Dashboard öffnen"]
    
    style A fill:#c8e6c9,stroke:#43a047,stroke-width:2px
    style B fill:#a5d6a7,stroke:#43a047,stroke-width:2px
    style C fill:#81c784,stroke:#43a047,stroke-width:2px
```

<br>

### 1️⃣ Hub starten

```bash
docker run -d --name myiotgrid \
  -p 5000:5000 -p 1883:1883 \
  ghcr.io/myiotgrid/hub:latest
```

### 2️⃣ Sensor verbinden

```bash
cd grid-sensor && pio run --target upload
```

### 3️⃣ Dashboard öffnen

```
http://localhost:5000
```

<br>

<p align="center"><strong>Das war's.</strong></p>

<br><br>

---

<br>

<h2 align="center">Technologie</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#f5f5f5'}}}%%

flowchart TB
    subgraph SENSOR ["📡 Grid.Sensor"]
        S["C++ · PlatformIO · ESP32"]
    end
    
    subgraph HUB ["🧠 Grid.Hub"]
        H1[".NET 8 · ASP.NET Core"]
        H2["Angular 18 · TypeScript"]
        H3["SQLite · SignalR"]
        H4["ML.NET · ONNX"]
    end
    
    subgraph CLOUD ["☁️ Grid.Cloud"]
        C1[".NET 10 · PostgreSQL"]
        C2["Redis · OAuth 2.0"]
        C3["ML.NET · Python ML"]
    end
    
    SENSOR -->|MQTT| HUB
    HUB -->|HTTPS| CLOUD
    
    style SENSOR fill:#e8f5e9,stroke:#4caf50
    style HUB fill:#e3f2fd,stroke:#2196f3
    style CLOUD fill:#fff3e0,stroke:#ff9800
```

<br><br>

---

<br>

<h2 align="center">Open Source. Für immer.</h2>

<br>

<p align="center">
  <strong>MIT License</strong>
</p>

<p align="center">
  <em>Keine Einschränkungen. Keine versteckten Kosten. Keine Abhängigkeit.</em>
</p>

<br>

> [!NOTE]
> **Warum MIT?**
> 
> 🌍 Weil **Umweltdaten** allen gehören sollten.
> 
> 🤖 Weil **KI** für alle da sein sollte – nicht nur für Big Tech.
> 
> 🤝 Weil eine **Community** mehr erreicht als ein Unternehmen.

<br><br>

---

<br>

<h2 align="center">Mitmachen</h2>

<br>

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#fafafa'}}}%%

flowchart LR
    A["🐛<br>Bugs<br>melden"] 
    B["💡<br>Features<br>vorschlagen"]
    C["🔧<br>Code<br>beitragen"]
    D["📚<br>Docs<br>verbessern"]
    E["🤖<br>KI-Modelle<br>entwickeln"]
    
    style A fill:#ffcdd2,stroke:#e53935
    style B fill:#fff9c4,stroke:#fdd835
    style C fill:#c8e6c9,stroke:#43a047
    style D fill:#bbdefb,stroke:#1e88e5
    style E fill:#e1bee7,stroke:#8e24aa
```

<br>

```bash
git clone https://github.com/myiotgrid/myiotgrid.git
cd myiotgrid
# Los geht's! 🚀
```

<br><br>

---

<br><br>

<p align="center">
  <strong>myIoTGrid</strong>
</p>

<p align="center">
  Open Source · Privacy First · AI Native
</p>

<p align="center">
  <a href="https://github.com/myiotgrid/myiotgrid">GitHub</a>
  &nbsp;·&nbsp;
  <a href="https://myiotgrid.cloud">Website</a>
  &nbsp;·&nbsp;
  <a href="https://mysocialcare-doku.atlassian.net/wiki/spaces/myIoTGrid">Docs</a>
</p>

<br>

<p align="center">
  <sub>Made with ❤️ in Germany</sub>
</p>

<br><br>
