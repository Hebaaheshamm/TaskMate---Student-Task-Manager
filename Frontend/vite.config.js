import { defineConfig } from "vite";
export default defineConfig({ build:{outDir:"../backend/TaskMate.Api/wwwroot",emptyOutDir:false}, server:{port:5173,proxy:{"/api":"http://localhost:5217"}} });
