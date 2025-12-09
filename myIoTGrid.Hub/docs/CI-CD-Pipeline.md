# CI/CD Pipeline - myIoTGrid Hub Backend

## Übersicht

Die CI/CD Pipeline automatisiert Build, Test und Docker-Image-Erstellung für das myIoTGrid Hub Backend.

| Eigenschaft | Wert |
|-------------|------|
| **Workflow-Datei** | `.github/workflows/ci-cd.yml` |
| **Registry** | GitHub Container Registry (ghcr.io) |
| **Image Name** | `ghcr.io/<owner>/myiotgrid-hub-api` |
| **Plattformen** | `linux/amd64`, `linux/arm64` |

---

## Trigger

Die Pipeline wird ausgelöst bei:

| Event | Branch-Pattern | Beschreibung |
|-------|----------------|--------------|
| Push | `main` | Produktion |
| Push | `test*` | Test-Branches (z.B. `test-feature`, `testing`) |
| Push | `beta*` | Beta-Releases (z.B. `beta-1.0`, `beta-release`) |
| Pull Request | `main` | Code Review (nur Build & Test, kein Docker Push) |

---

## Jobs

### 1. Build & Test

| Schritt | Beschreibung |
|---------|--------------|
| Checkout | Code auschecken |
| Setup .NET | .NET 10 SDK installieren |
| Restore | NuGet-Pakete wiederherstellen |
| Build | Release-Build erstellen |
| Test | Alle Tests ausführen mit Coverage |
| Upload | Test-Results und Coverage-Reports als Artifacts |

**Artifacts:**
- `test-results` - Test-Ergebnisse (`.trx` Dateien)
- `coverage-reports` - Code Coverage (Cobertura XML)

### 2. Docker Build & Push

> Läuft nur bei Push (nicht bei Pull Requests)

| Schritt | Beschreibung |
|---------|--------------|
| Setup QEMU | Multi-Arch Emulation |
| Setup Buildx | Docker Buildx für Multi-Platform |
| Login | Anmeldung an GitHub Container Registry |
| Build & Push | Image bauen und pushen |

---

## Docker Image Tags

| Tag-Format | Beispiel | Beschreibung |
|------------|----------|--------------|
| `<branch>` | `main`, `test-feature`, `beta-1.0` | Branch-Name |
| `latest` | `latest` | Nur für `main` Branch |
| `<sha>` | `abc1234` | Kurzer Commit-SHA |
| `YYYYMMDD-HHmmss` | `20251130-143022` | Timestamp |

---

## Image verwenden

### Pull Command

```bash
# Latest (main branch)
docker pull ghcr.io/<owner>/myiotgrid-hub-api:latest

# Spezifischer Branch
docker pull ghcr.io/<owner>/myiotgrid-hub-api:beta-1.0

# Spezifischer Commit
docker pull ghcr.io/<owner>/myiotgrid-hub-api:abc1234
```

### Docker Compose

```yaml
services:
  hub-api:
    image: ghcr.io/<owner>/myiotgrid-hub-api:latest
    ports:
      - "8080:8080"
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__HubDb=Data Source=/app/data/hub.db
```

### Raspberry Pi (ARM64)

```bash
# Das Image unterstützt ARM64 nativ
docker pull ghcr.io/<owner>/myiotgrid-hub-api:latest

# Oder mit docker-compose
docker-compose up -d
```

---

## Secrets & Permissions

Die Pipeline benötigt keine manuellen Secrets. Sie verwendet:

| Secret | Quelle | Beschreibung |
|--------|--------|--------------|
| `GITHUB_TOKEN` | Automatisch | Für GitHub Container Registry Login |

**Repository Settings:**
- Packages müssen aktiviert sein
- Workflow-Permissions: "Read and write permissions"

---

## Workflow-Status prüfen

1. **GitHub Actions Tab** → Repository → Actions
2. **Badge im README** (optional):

```markdown
![CI/CD](https://github.com/<owner>/<repo>/actions/workflows/ci-cd.yml/badge.svg)
```

---

## Fehlerbehebung

### Build schlägt fehl

```bash
# Lokal testen
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

### Docker Build schlägt fehl

```bash
# Lokal testen
docker build -f src/myIoTGrid.Hub.Api/Dockerfile -t hub-api:local .
```

### Permission Denied bei Registry

1. Repository Settings → Actions → General
2. "Workflow permissions" → "Read and write permissions"
3. "Allow GitHub Actions to create and approve pull requests" ✓

---

## Architektur-Diagramm

```
┌─────────────────────────────────────────────────────────────────┐
│                        GitHub Repository                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   Push to main/test*/beta*                                       │
│            │                                                     │
│            ▼                                                     │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              GitHub Actions Workflow                     │   │
│   │                                                          │   │
│   │   ┌─────────────────┐    ┌─────────────────────────┐    │   │
│   │   │  Build & Test   │───▶│  Docker Build & Push    │    │   │
│   │   │                 │    │                         │    │   │
│   │   │ • dotnet build  │    │ • Multi-arch build     │    │   │
│   │   │ • dotnet test   │    │ • Push to ghcr.io      │    │   │
│   │   └─────────────────┘    └───────────┬─────────────┘    │   │
│   │                                      │                   │   │
│   └──────────────────────────────────────┼───────────────────┘   │
│                                          │                       │
│                                          ▼                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │            GitHub Container Registry (ghcr.io)           │   │
│   │                                                          │   │
│   │   ghcr.io/<owner>/myiotgrid-hub-api:latest              │   │
│   │   ghcr.io/<owner>/myiotgrid-hub-api:main                │   │
│   │   ghcr.io/<owner>/myiotgrid-hub-api:beta-1.0            │   │
│   │   ghcr.io/<owner>/myiotgrid-hub-api:<sha>               │   │
│   │                                                          │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │      Deployment Target        │
              │                               │
              │   • Raspberry Pi (ARM64)      │
              │   • Server (AMD64)            │
              │   • Docker Compose            │
              │                               │
              └───────────────────────────────┘
```

---

**Erstellt:** 30. November 2025
**Version:** 1.0
