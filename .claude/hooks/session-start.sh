#!/bin/bash
# Prepara las herramientas de RELICBOUND en las sesiones de Claude Code en la nube:
# .NET 8 (tests de Relicbound.Core) y Blender 4.2 LTS headless (modelado y renders).
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

TOOLS="$HOME/tools"
DOTNET_DIR="$HOME/.dotnet"
BLENDER_VERSION="4.2.3"
BLENDER_DIR="$TOOLS/blender-$BLENDER_VERSION-linux-x64"
mkdir -p "$TOOLS"

if [ ! -x "$DOTNET_DIR/dotnet" ]; then
  curl -sSfL https://dot.net/v1/dotnet-install.sh -o "$TOOLS/dotnet-install.sh"
  bash "$TOOLS/dotnet-install.sh" --channel 8.0 --install-dir "$DOTNET_DIR" > /dev/null
fi

if [ ! -x "$BLENDER_DIR/blender" ]; then
  curl -sSfL "https://download.blender.org/release/Blender4.2/blender-$BLENDER_VERSION-linux-x64.tar.xz" -o "$TOOLS/blender.tar.xz"
  tar -xf "$TOOLS/blender.tar.xz" -C "$TOOLS"
  rm "$TOOLS/blender.tar.xz"
fi

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export PATH=\"$DOTNET_DIR:$BLENDER_DIR:\$PATH\""
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export DOTNET_NOLOGO=1"
  } >> "$CLAUDE_ENV_FILE"
fi

# Restaura paquetes NuGet para que `dotnet test` funcione sin red lenta después.
"$DOTNET_DIR/dotnet" restore "$CLAUDE_PROJECT_DIR/Tests/Relicbound.Core.Tests" > /dev/null
