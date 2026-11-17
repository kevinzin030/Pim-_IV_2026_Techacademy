
import express from "express";
import cors from "cors";
import dotenv from "dotenv";
import routes from "./Routes/index.js";

dotenv.config();

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());

app.use("/api", routes);

app.listen(PORT, () => {
    console.log(
        `TechAcademy API rodando em http://localhost:${PORT}`
    );
});
