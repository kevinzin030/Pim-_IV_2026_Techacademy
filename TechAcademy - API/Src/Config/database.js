
import sql from "mssql";
import dotenv from "dotenv";

dotenv.config();

const config = {
    server: process.env.DB_SERVER,
    database: process.env.DB_DATABASE,
    user: process.env.DB_USER,
    password: process.env.DB_PASSWORD,
    port: 1433,
    options: {
        encrypt: true,
        trustServerCertificate: false
    },
    connectionTimeout: 30000
};

let pool;

export async function conectarBanco() {
    if (!pool) {
        pool = await sql.connect(config);
    }

    return pool;
}
