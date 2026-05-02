#!/bin/bash
# ComRouter — installer for Linux ARM64
# Usage: sudo bash comRouter_install.sh

set -e

INSTALL_DIR="/opt/comrouter"
SERVICE_NAME="comrouter"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"
DATA_DIR="/var/lib/comrouter"
LOG_DIR="/var/log/comrouter"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ "$EUID" -ne 0 ]; then
    echo "ERROR: please run as root (sudo bash comRouter_install.sh)"
    exit 1
fi

echo "=== ComRouter installer ==="
echo "Install dir : ${INSTALL_DIR}"
echo "Data dir    : ${DATA_DIR}"

# Create directories
mkdir -p "${INSTALL_DIR}"
mkdir -p "${DATA_DIR}"
mkdir -p "${LOG_DIR}"

# Create service user if not present
id -u comrouter &>/dev/null || useradd -r -s /usr/sbin/nologin -d "${DATA_DIR}" -c "ComRouter service" comrouter

# Stop existing service if running
if systemctl is-active --quiet "${SERVICE_NAME}"; then
    echo "Stopping existing service..."
    systemctl stop "${SERVICE_NAME}"
fi

# Copy application files
echo "Copying application files to ${INSTALL_DIR}..."
cp -r "${SCRIPT_DIR}"/. "${INSTALL_DIR}/"
chmod +x "${INSTALL_DIR}/CommRouter.WebServer"

# Set ownership
chown -R comrouter:comrouter "${INSTALL_DIR}"
chown -R comrouter:comrouter "${DATA_DIR}"
chown -R comrouter:comrouter "${LOG_DIR}"

# Write systemd unit
cat > "${SERVICE_FILE}" << 'UNIT'
[Unit]
Description=ComRouter - Serial/TCP/UDP/Process Communication Router
After=network.target
StartLimitIntervalSec=60
StartLimitBurst=3

[Service]
Type=simple
User=comrouter
Group=comrouter
WorkingDirectory=/opt/comrouter
ExecStart=/opt/comrouter/CommRouter.WebServer
Restart=on-failure
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5025
Environment=HOME=/var/lib/comrouter
StandardOutput=journal
StandardError=journal
SyslogIdentifier=comrouter

[Install]
WantedBy=multi-user.target
UNIT

# Reload systemd, enable and start
systemctl daemon-reload
systemctl enable "${SERVICE_NAME}"
systemctl restart "${SERVICE_NAME}"

echo ""
echo "=== Installation complete ==="
echo "Service status : systemctl status ${SERVICE_NAME}"
echo "Logs           : journalctl -u ${SERVICE_NAME} -f"
echo "Web UI         : http://$(hostname -I | awk '{print $1}'):5025"
