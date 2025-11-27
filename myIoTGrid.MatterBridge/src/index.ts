import express from "express";
import dotenv from "dotenv";
import winston from "winston";
import { MatterBridge, BridgeConfig } from "./bridge/MatterBridge";
import { createRoutes } from "./api/routes";

// Load environment variables
dotenv.config();

// Configure Winston logger
const logger = winston.createLogger({
    level: process.env.LOG_LEVEL || "info",
    format: winston.format.combine(
        winston.format.timestamp(),
        winston.format.errors({ stack: true }),
        winston.format.json()
    ),
    defaultMeta: { service: "matter-bridge" },
    transports: [
        new winston.transports.Console({
            format: winston.format.combine(
                winston.format.colorize(),
                winston.format.printf(({ level, message, timestamp, ...meta }) => {
                    const metaStr = Object.keys(meta).length > 1
                        ? ` ${JSON.stringify(meta)}`
                        : "";
                    return `[${timestamp}] ${level}: ${message}${metaStr}`;
                })
            ),
        }),
    ],
});

// Bridge configuration
const bridgeConfig: BridgeConfig = {
    vendorId: parseInt(process.env.VENDOR_ID || "0xFFF1", 16), // Test vendor ID
    vendorName: process.env.VENDOR_NAME || "myIoTGrid",
    productName: process.env.PRODUCT_NAME || "myIoTGrid Hub",
    productId: parseInt(process.env.PRODUCT_ID || "0x8001", 16),
    storagePath: process.env.STORAGE_PATH || "./data",
    port: parseInt(process.env.MATTER_PORT || "5540"),
    discriminator: parseInt(process.env.DISCRIMINATOR || "3840"),
    passcode: parseInt(process.env.PASSCODE || "20202021"),
};

const API_PORT = parseInt(process.env.PORT || "3000");

// Create Express app
const app = express();
app.use(express.json());

// Create Matter Bridge
const bridge = new MatterBridge(bridgeConfig, logger);

// Setup routes
app.use("/", createRoutes(bridge, logger));

// Error handling middleware
app.use((err: Error, req: express.Request, res: express.Response, next: express.NextFunction) => {
    logger.error("Unhandled error", { error: err.message, stack: err.stack });
    res.status(500).json({ error: "Internal server error" });
});

// Graceful shutdown
async function shutdown() {
    logger.info("Shutting down...");
    try {
        await bridge.stop();
        process.exit(0);
    } catch (error) {
        logger.error("Error during shutdown", { error });
        process.exit(1);
    }
}

process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);

// Start the server
async function main() {
    try {
        logger.info("myIoTGrid Matter Bridge starting...");
        logger.info(`Configuration:`, {
            vendorId: bridgeConfig.vendorId,
            productName: bridgeConfig.productName,
            storagePath: bridgeConfig.storagePath,
            matterPort: bridgeConfig.port,
            apiPort: API_PORT,
        });

        // Start Matter Bridge
        await bridge.start();

        // Start Express API server
        app.listen(API_PORT, () => {
            logger.info(`Matter Bridge API listening on port ${API_PORT}`);
            logger.info(`Status endpoint: http://localhost:${API_PORT}/status`);
            logger.info(`Health endpoint: http://localhost:${API_PORT}/health`);
        });

    } catch (error) {
        logger.error("Failed to start Matter Bridge", { error });
        process.exit(1);
    }
}

main();
