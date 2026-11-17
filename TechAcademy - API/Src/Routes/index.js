
import { Router } from "express";

const router = Router();

router.get("/health", (req, res) => {
    res.status(200).json({
        sistema: "TechAcademy",
        status: "online",
        mensagem: "API funcionando corretamente"
    });
});

export default router;
