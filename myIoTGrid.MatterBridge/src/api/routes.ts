import { Router, Request, Response } from "express";
import { MatterBridge } from "../bridge/MatterBridge";
import { DeviceFactory, DeviceType } from "../bridge/DeviceFactory";
import QRCode from "qrcode";
import { Logger } from "winston";

export function createRoutes(bridge: MatterBridge, logger: Logger): Router {
    const router = Router();

    // GET /status - Bridge status
    router.get("/status", (req: Request, res: Response) => {
        try {
            const status = bridge.getStatus();
            res.json(status);
        } catch (error) {
            logger.error("Error getting status", { error });
            res.status(500).json({ error: "Failed to get bridge status" });
        }
    });

    // POST /devices - Register a new device
    router.post("/devices", async (req: Request, res: Response) => {
        try {
            const { sensorId, name, type, location } = req.body;

            if (!sensorId || !name || !type) {
                res.status(400).json({
                    error: "Missing required fields: sensorId, name, type"
                });
                return;
            }

            const deviceType = DeviceFactory.mapSensorTypeToDeviceType(type);
            if (!deviceType) {
                res.status(400).json({
                    error: `Invalid device type: ${type}. Supported: ${DeviceFactory.getSupportedSensorTypes().join(", ")}, contact`
                });
                return;
            }

            await bridge.registerDevice(sensorId, name, deviceType, location);

            res.status(201).json({
                success: true,
                sensorId,
                name,
                type: deviceType,
                location
            });
        } catch (error) {
            logger.error("Error registering device", { error });
            res.status(500).json({ error: "Failed to register device" });
        }
    });

    // PUT /devices/:sensorId - Update device value
    router.put("/devices/:sensorId", async (req: Request, res: Response) => {
        try {
            const { sensorId } = req.params;
            const { sensorType, value } = req.body;

            if (!sensorType || value === undefined) {
                res.status(400).json({
                    error: "Missing required fields: sensorType, value"
                });
                return;
            }

            await bridge.updateDeviceValue(sensorId, sensorType, value);

            res.json({
                success: true,
                sensorId,
                sensorType,
                value
            });
        } catch (error) {
            logger.error("Error updating device", { error });
            res.status(500).json({ error: "Failed to update device" });
        }
    });

    // DELETE /devices/:sensorId - Remove device
    router.delete("/devices/:sensorId", async (req: Request, res: Response) => {
        try {
            const { sensorId } = req.params;

            await bridge.removeDevice(sensorId);

            res.json({
                success: true,
                sensorId
            });
        } catch (error) {
            logger.error("Error removing device", { error });
            res.status(500).json({ error: "Failed to remove device" });
        }
    });

    // PUT /devices/:sensorId/contact - Set contact sensor state (for alerts)
    router.put("/devices/:sensorId/contact", async (req: Request, res: Response) => {
        try {
            const { sensorId } = req.params;
            const { isOpen } = req.body;

            if (isOpen === undefined) {
                res.status(400).json({
                    error: "Missing required field: isOpen"
                });
                return;
            }

            await bridge.setContactSensorState(sensorId, isOpen);

            res.json({
                success: true,
                sensorId,
                isOpen
            });
        } catch (error) {
            logger.error("Error setting contact state", { error });
            res.status(500).json({ error: "Failed to set contact state" });
        }
    });

    // GET /commission - Get commissioning info
    router.get("/commission", (req: Request, res: Response) => {
        try {
            const status = bridge.getStatus();
            const manualCode = bridge.getManualPairingCode();

            res.json({
                pairingCode: status.pairingCode,
                discriminator: status.discriminator,
                manualPairingCode: manualCode,
                qrCodeData: bridge.getQrCodeData()
            });
        } catch (error) {
            logger.error("Error getting commission info", { error });
            res.status(500).json({ error: "Failed to get commission info" });
        }
    });

    // POST /commission/qr - Generate QR code image
    router.post("/commission/qr", async (req: Request, res: Response) => {
        try {
            const qrData = bridge.getQrCodeData();
            const qrImage = await QRCode.toDataURL(qrData, {
                width: 300,
                margin: 2,
                color: {
                    dark: "#000000",
                    light: "#FFFFFF"
                }
            });

            res.json({
                qrCodeData: qrData,
                qrCodeImage: qrImage,
                manualPairingCode: bridge.getManualPairingCode()
            });
        } catch (error) {
            logger.error("Error generating QR code", { error });
            res.status(500).json({ error: "Failed to generate QR code" });
        }
    });

    // GET /health - Health check
    router.get("/health", (req: Request, res: Response) => {
        res.json({
            status: "healthy",
            isRunning: bridge.isRunning(),
            timestamp: new Date().toISOString()
        });
    });

    return router;
}
