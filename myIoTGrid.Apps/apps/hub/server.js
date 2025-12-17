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

// Kompression aktivieren
app.use(compression());

// API Proxy - leitet /api/* an das Backend weiter
app.use('/api', createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    pathRewrite: {
        '^/api': '/api'  // /api/readings -> /api/readings
    },
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[Proxy] ${req.method} ${req.url} -> ${apiUrl}${req.url}`);
    },
    onError: (err, req, res) => {
        console.error(`[Proxy Error] ${err.message}`);
        res.status(502).json({ error: 'Backend unavailable', message: err.message });
    }
}));

// SignalR Proxy - leitet /hubs/* an das Backend weiter (WebSocket support)
app.use('/hubs', createProxyMiddleware({
    target: apiUrl,
    changeOrigin: true,
    ws: true,  // WebSocket support für SignalR
    pathRewrite: {
        '^/hubs': '/hubs'
    },
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[Proxy] ${req.method} ${req.url} -> ${apiUrl}${req.url}`);
    }
}));

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
app.listen(port, () => {
    console.log(`myIoTGrid Hub Frontend running on port ${port}`);
    console.log(`Serving from: ${distFolder}`);
    console.log(`API Proxy: /api/* -> ${apiUrl}`);
    console.log(`SignalR Proxy: /hubs/* -> ${apiUrl}`);
});
