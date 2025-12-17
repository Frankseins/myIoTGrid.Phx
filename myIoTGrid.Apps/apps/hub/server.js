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
const apiUrl = process.env.API_URL || 'https://phx-api-fze5h4b6gbchg4dv.germanywestcentral-01.azurewebsites.net';

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
const apiProxy = createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    secure: true,
    pathRewrite: (path, req) => path, // Behalte den Pfad unverändert (inkl. /api)
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[API Proxy] ${req.method} ${req.originalUrl} -> ${apiUrl}${req.originalUrl}`);
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

// SignalR Proxy - leitet /hubs/* an das Backend weiter (WebSocket support)
const hubsProxy = createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    secure: true,
    ws: true,
    pathRewrite: (path, req) => path, // Behalte den Pfad unverändert (inkl. /hubs)
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[SignalR Proxy] ${req.method} ${req.originalUrl} -> ${apiUrl}${req.originalUrl}`);
    },
    onProxyRes: (proxyRes, req, res) => {
        console.log(`[SignalR Proxy] Response: ${proxyRes.statusCode} for ${req.originalUrl}`);
    },
    onError: (err, req, res) => {
        console.error(`[SignalR Proxy Error] ${err.message}`);
        if (!res.headersSent) {
            res.status(502).json({ error: 'SignalR unavailable', message: err.message });
        }
    }
});
app.use('/hubs', hubsProxy);

// Health endpoint for Azure
app.get('/health', (req, res) => {
    res.status(200).json({
        status: 'healthy',
        timestamp: new Date().toISOString(),
        apiUrl: apiUrl
    });
});

// Static files bereitstellen
app.use(express.static(distFolder));

// Alle anderen Routen auf index.html umleiten (für Angular Routing)
app.get('*', (req, res) => {
    res.sendFile(path.join(distFolder, 'index.html'));
});

// Port von Azure oder default 8080
const port = process.env.PORT || 8080;
const server = app.listen(port, () => {
    console.log(`myIoTGrid Hub Frontend running on port ${port}`);
    console.log(`Serving from: ${distFolder}`);
    console.log(`API Proxy: /api/* -> ${apiUrl}`);
    console.log(`SignalR Proxy: /hubs/* -> ${apiUrl}`);
});

// WebSocket upgrade handling für SignalR
server.on('upgrade', (req, socket, head) => {
    console.log(`[WebSocket] Upgrade request for: ${req.url}`);
    if (req.url.startsWith('/hubs')) {
        hubsProxy.upgrade(req, socket, head);
    }
});
