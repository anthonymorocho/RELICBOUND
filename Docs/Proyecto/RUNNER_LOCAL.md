# Compilar en tu PC (runner local de GitHub Actions)

Con tu PC como runner, la CI usa tu Unity 6000.3.27f1 y tu tarjeta gráfica: la importación, los shaders y los tests tardan minutos en lugar de una hora. El workflow ya está preparado (`.github/workflows/unity.yml`, trabajo `unity-local`).

## Pasos (unos 10 minutos)

1. En GitHub: repositorio **RELICBOUND → Settings → Actions → Runners → New self-hosted runner**.
2. Elige **Windows** y **x64**. GitHub muestra unos comandos de PowerShell con un token que caduca en una hora.
3. En tu PC, abre **PowerShell** y ejecuta esos comandos tal cual en una carpeta nueva, por ejemplo `C:\actions-runner`.
4. Cuando `config.cmd` pregunte por etiquetas adicionales (*additional labels*), escribe: `unity`
5. Al terminar, ejecuta `.\run.cmd` y deja esa ventana abierta mientras quieras que compile. Si prefieres que arranque solo, responde *Y* a «run as service» durante la configuración.
6. En GitHub: **Settings → Secrets and variables → Actions → Variables → New repository variable**:
   - Nombre `UNITY_RUNNER`, valor `local`.
   - Solo si Unity no está en `C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe`: variable `UNITY_EDITOR_PATH` con la ruta a tu `Unity.exe`.

Desde ese momento, cada push compila en tu PC. Para volver a la nube, borra la variable `UNITY_RUNNER`.

## Notas

- El runner usa la licencia de Unity que ya tienes activada en tu PC.
- Mientras compila, Unity se abre en modo batch (sin ventana). No cierres ni reinicies tu editor de Unity: el runner usa su propia copia del proyecto en `C:\actions-runner\_work`.
- No compartas nunca el token del paso 2 en el chat: se pega solo en tu PowerShell.
