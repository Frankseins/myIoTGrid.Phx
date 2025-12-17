const express = require('express');
const path = require('path');
const fs = require('fs');
const compression = require('compression');
const { createProxyMiddleware } = require('http-proxy-middleware');

const app = express();

// Ermittle das richtige Verzeichnis für statische Dateien
// In Azure liegt index.html im selben Verzeichnis wie server.js
// Lokal liegt es in dist/apps/hub/browser
function getDistFolder() {
    // Prüfe ob index.html im aktuellen Verzeichnis liegt (Azure)
    if (fs.existsSync(path.join(__dirname, 'index.html'))) {
        return __dirname;
    }
    // Ansonsten lokaler Entwicklungspfad
    return path.join(__dirname, '..', '..', '..', 'dist', 'apps', 'hub', 'browser');
}

const distFolder = getDistFolder();

// Backend API URL (kann per Environment Variable überschrieben werden)
const apiUrl = process.env.API_URL || 'https://phx-api.myiotgrid.cloud';

console.log(`[Server] Starting myIoTGrid Hub Frontend...`);
console.log(`[Server] API URL: ${apiUrl}`);
console.log(`[Server] Dist folder: ${distFolder}`);

// Kompression aktivieren
app.use(compression());

// Request logging middleware
app.use((req, res, next) => {
    console.log(`[Request] ${req.method} ${req.url}`);
    next();
});

// API Proxy - leitet /api/* an das Backend weiter
// WICHTIG: app.use('/api', ...) entfernt den /api Prefix vom Pfad,
// daher müssen wir ihn in pathRewrite wieder hinzufügen!
const apiProxy = createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    secure: true,
    limit: '200mb', // Für Backup-Uploads
    timeout: 300000, // 5 Minuten Timeout für große Uploads
    proxyTimeout: 300000,
    pathRewrite: (path, req) => '/api' + path, // Füge /api wieder hinzu, da Express es entfernt
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[API Proxy] ${req.method} ${req.originalUrl} -> ${apiUrl}/api${req.url}`);
    },
    onProxyRes: (proxyRes, req, res) => {
        console.log(`[API Proxy] Response: ${proxyRes.statusCode} for ${req.originalUrl}`);
    },
    onError: (err, req, res) => {
        console.error(`[API Proxy Error] ${err.message}`);
        if (!res.headersSent) {
            res.status(502).json({ error: 'Backend unavailable', message: err.message });
        }
    }
});
app.use('/api', apiProxy);

// SignalR HTTP Proxy - für negotiate und andere HTTP-Requests
// NUR für /hubs/sensors (SignalR), NICHT für /hubs (Angular Route!)
const hubsHttpProxy = createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    secure: true,
    ws: false, // Kein WebSocket hier - wird separat gehandhabt
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[SignalR HTTP] ${req.method} ${req.originalUrl} -> ${apiUrl}${req.originalUrl}`);
    },
    onProxyRes: (proxyRes, req, res) => {
        console.log(`[SignalR HTTP] Response: ${proxyRes.statusCode} for ${req.originalUrl}`);
    },
    onError: (err, req, res) => {
        console.error(`[SignalR HTTP Error] ${err.message}`);
        if (!res.headersSent) {
            res.status(502).json({ error: 'SignalR unavailable', message: err.message });
        }
    }
});
// Nur /hubs/sensors proxyen, nicht /hubs (das ist eine Angular Route!)
app.use('/hubs/sensors', hubsHttpProxy);

// Separater WebSocket Proxy für SignalR
// Bei WebSocket-Upgrades läuft der Request NICHT durch Express-Middleware,
// daher ist der Pfad noch vollständig und braucht KEINEN pathRewrite!
const hubsWsProxy = createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    secure: true,
    ws: true,
    // KEIN pathRewrite hier! WebSocket-Requests haben den vollen Pfad
    onProxyReqWs: (proxyReq, req, socket, options, head) => {
        console.log(`[SignalR WS] Upgrade ${req.url} -> ${apiUrl}${req.url}`);
    },
    onError: (err, req, res) => {
        console.error(`[SignalR WS Error] ${err.message}`);
    }
});

// Health endpoint for Azure
app.get('/health', (req, res) => {
    res.status(200).json({
        status: 'healthy',
        timestamp: new Date().toISOString(),
        apiUrl: apiUrl
    });
});

// Static files bereitstellen (nur existierende Dateien)
app.use(express.static(distFolder, {
    index: false // index.html nicht automatisch für Verzeichnisse
}));

// SPA Fallback - ALLE anderen Routen auf index.html umleiten (Angular Routing)
// Funktioniert für /dashboard, /hubs, /settings, etc.
app.use('*', (req, res) => {
    const indexPath = path.join(distFolder, 'index.html');
    console.log(`[SPA Routing] ${req.method} ${req.originalUrl} -> index.html`);
    res.sendFile(indexPath, (err) => {
        if (err) {
            console.error(`[SPA Routing Error] ${err.message}`);
            res.status(500).send('index.html not found');
        }
    });
});

// Port von Azure oder default 8080
const port = process.env.PORT || 8080;
const server = app.listen(port, () => {
    console.log(`myIoTGrid Hub Frontend running on port ${port}`);
    console.log(`Serving from: ${distFolder}`);
    console.log(`API Proxy: /api/* -> ${apiUrl}/api/*`);
    console.log(`SignalR HTTP Proxy: /hubs/sensors/* -> ${apiUrl}/hubs/sensors/*`);
    console.log(`SignalR WebSocket Proxy: /hubs/sensors/* -> ${apiUrl}/hubs/sensors/*`);
});

// WebSocket upgrade handling für SignalR
// WICHTIG: Bei Upgrades kommt der Request direkt zum Server, nicht durch Express!
// Der Pfad ist daher vollständig (z.B. /hubs/sensors?id=...)
server.on('upgrade', (req, socket, head) => {
    console.log(`[WebSocket] Upgrade request for: ${req.url}`);
    if (req.url.startsWith('/hubs')) {
        hubsWsProxy.upgrade(req, socket, head);
    } else {
        console.log(`[WebSocket] Rejected - not a SignalR path: ${req.url}`);
        socket.destroy();
    }
});
