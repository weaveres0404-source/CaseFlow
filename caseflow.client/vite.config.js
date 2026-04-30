import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "caseflow.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);
const useHttps = env.DEV_SERVER_HTTPS !== 'false';

if (useHttps) {
    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        if (0 !== child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit', }).status) {
            throw new Error("Could not create certificate.");
        }
    }
}

// Determine backend target: default to HTTPS for normal dev flow, but use HTTP backend during HTTP validation mode.
const target = env.DEV_API_TARGET || (useHttps
    ? (env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
        (env.ASPNETCORE_URLS ? (() => {
            const urls = env.ASPNETCORE_URLS.split(';');
            const httpsUrl = urls.find(u => u.startsWith('https'));
            return httpsUrl || urls[0];
        })() : 'https://localhost:7172'))
    : (env.ASPNETCORE_HTTP_PORT ? `http://localhost:${env.ASPNETCORE_HTTP_PORT}` : 'http://localhost:56002'));

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), tailwindcss()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/weatherforecast': {
                target,
                secure: false
            },
            '^/auth': {
                target,
                secure: false
            }
            ,
            '^/api': {
                target,
                secure: false
            }
        },
        // Use 56003 as the default dev server port to avoid colliding with backend HTTP port 56002
        port: parseInt(env.DEV_SERVER_PORT || '56003'),
        https: useHttps ? {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        } : false
    }
})
