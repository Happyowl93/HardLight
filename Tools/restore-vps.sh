#!/usr/bin/env bash
# Provision (or re-provision) the Novasector14 SS14 server on a fresh
# Ubuntu 24.04 VPS. Idempotent — safe to re-run.
#
# Usage:   sudo bash restore-vps.sh
# Env vars (override the defaults below if desired):
#   PUBLIC_IP, INSTANCE_KEY, HOST_USERNAME, FORK_URL,
#   MANIFEST_URL, API_TOKEN
#
# What this does NOT do:
#   - Open Hetzner Cloud Firewall ports (do that in Hetzner web console)
#   - Restore player data / preferences.db (those live in instances/<key>/data
#     and survive across re-provisioning)
#   - Set up nginx / TLS — separate follow-up
#   - Trigger the GitHub Actions build (push a commit instead)

set -euo pipefail

# ============================================================
# Configuration
# ============================================================
PUBLIC_IP="${PUBLIC_IP:-204.168.177.241}"
INSTANCE_KEY="${INSTANCE_KEY:-Novasector14}"
HOST_USERNAME="${HOST_USERNAME:-Macanudoman}"
FORK_URL="${FORK_URL:-https://github.com/Happyowl93/NovaSector-14}"
MANIFEST_URL="${MANIFEST_URL:-https://happyowl93.github.io/NovaSector-14/manifest.json}"
WATCHDOG_REPO="${WATCHDOG_REPO:-https://github.com/space-wizards/SS14.Watchdog.git}"

# API_TOKEN: paste the existing one to keep curl scripts working,
# or leave empty to generate a fresh random one (printed at end).
API_TOKEN="${API_TOKEN:-}"

# ============================================================
# Pre-flight
# ============================================================
if [[ "$(id -u)" -ne 0 ]]; then
  echo "ERR: run as root (sudo bash $0)" >&2
  exit 1
fi

if ! grep -q "Ubuntu 24" /etc/os-release; then
  echo "WARN: tested only on Ubuntu 24.04. Continuing anyway." >&2
fi

if [[ -z "$API_TOKEN" ]]; then
  API_TOKEN="token_$(openssl rand -hex 32)"
  GENERATED_TOKEN=1
else
  GENERATED_TOKEN=0
fi

echo "==> Provisioning $INSTANCE_KEY on $PUBLIC_IP"

# ============================================================
# 1. System packages + .NET SDKs
# ============================================================
echo "==> [1/8] Installing base packages"
apt-get update -qq
apt-get install -y --no-install-recommends \
  git python3 python3-pip curl wget unzip ufw sqlite3 jq ca-certificates openssl

install_dotnet() {
  local channel="$1"
  if dotnet --list-sdks 2>/dev/null | grep -q "^${channel}\."; then
    echo "    .NET ${channel} already installed"
    return
  fi
  echo "    installing .NET ${channel} SDK"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel "${channel}" --install-dir /usr/lib/dotnet
}

# Ensure dotnet command is on PATH for root + ss14
ln -sf /usr/lib/dotnet/dotnet /usr/local/bin/dotnet

echo "==> [2/8] .NET SDKs"
install_dotnet 9.0    # game server
install_dotnet 10.0   # watchdog

# ============================================================
# 3. ss14 user
# ============================================================
echo "==> [3/8] User ss14"
if ! id ss14 >/dev/null 2>&1; then
  adduser --disabled-password --gecos "" ss14
fi
usermod -aG systemd-journal ss14

# ============================================================
# 4. Firewall (UFW)
# ============================================================
echo "==> [4/8] UFW rules"
ufw allow OpenSSH       >/dev/null
ufw allow 1212/udp      >/dev/null
ufw allow 1212/tcp      >/dev/null
ufw allow 5000/tcp      >/dev/null
ufw --force enable      >/dev/null

# ============================================================
# 5. Sudoers rule (ss14 can manage the watchdog service)
# ============================================================
echo "==> [5/8] Sudo rule for ss14"
cat > /etc/sudoers.d/ss14-watchdog <<'EOF'
ss14 ALL=(root) NOPASSWD: /usr/bin/systemctl restart ss14-watchdog, /usr/bin/systemctl start ss14-watchdog, /usr/bin/systemctl stop ss14-watchdog, /usr/bin/systemctl status ss14-watchdog
EOF
chmod 440 /etc/sudoers.d/ss14-watchdog
visudo -c -f /etc/sudoers.d/ss14-watchdog >/dev/null

# ============================================================
# 6. Watchdog clone + build (as ss14)
# ============================================================
echo "==> [6/8] Watchdog source + build"
sudo -u ss14 -H bash -s -- "$WATCHDOG_REPO" "$INSTANCE_KEY" <<'SS14_EOF'
set -euo pipefail
WATCHDOG_REPO="$1"
INSTANCE_KEY="$2"

if [[ ! -d "$HOME/SS14.Watchdog/.git" ]]; then
  git clone "$WATCHDOG_REPO" "$HOME/SS14.Watchdog"
else
  git -C "$HOME/SS14.Watchdog" pull --ff-only
fi

if [[ ! -f "$HOME/watchdog/SS14.Watchdog.dll" ]]; then
  cd "$HOME/SS14.Watchdog"
  dotnet publish -c Release --runtime linux-x64 --self-contained false -o "$HOME/watchdog"
fi

mkdir -p "$HOME/watchdog/instances/$INSTANCE_KEY/data"
SS14_EOF

# ============================================================
# 7. Config files (atomically written by root, then chowned)
# ============================================================
echo "==> [7/8] Writing watchdog + instance configs"

# 7a. ~/watchdog/appsettings.yml
cat > "/home/ss14/watchdog/appsettings.yml" <<EOF
Serilog:
  Using: [ "Serilog.Sinks.Console" ]
  MinimumLevel:
    Default: Information
    Override:
      SS14: Debug
      Microsoft: Warning
      Microsoft.Hosting.Lifetime: Information
      Microsoft.AspNetCore: Warning
  WriteTo:
    - Name: Console
      Args:
        OutputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}"

AllowedHosts: "*"

Kestrel:
  Endpoints:
    Http:
      Url: "http://0.0.0.0:5000"

BaseUrl: "http://${PUBLIC_IP}:5000/"

Servers:
  Instances:
    ${INSTANCE_KEY}:
      ApiToken: "${API_TOKEN}"
      ApiPort: 1213
      UpdateType: "Manifest"
      Updates:
        ManifestUrl: "${MANIFEST_URL}"
EOF

# 7b. ~/watchdog/instances/<key>/config.toml
cat > "/home/ss14/watchdog/instances/${INSTANCE_KEY}/config.toml" <<EOF
[game]
hostname = "${INSTANCE_KEY}"
desc = "Welcome to Novasector — a HardLight fork."

[server]
id = "${INSTANCE_KEY}"
lobby_name = "${INSTANCE_KEY}"

[hub]
advertise = false

[console]
login_host_user = "${HOST_USERNAME}"

[adminlogs]
server_name = "${INSTANCE_KEY}"

[infolinks]
github = "${FORK_URL}"
EOF

chown -R ss14:ss14 /home/ss14/watchdog

# ============================================================
# 8. systemd unit + start
# ============================================================
echo "==> [8/8] systemd unit"
cat > /etc/systemd/system/ss14-watchdog.service <<'EOF'
[Unit]
Description=SS14 Watchdog
After=network.target

[Service]
Type=simple
User=ss14
WorkingDirectory=/home/ss14/watchdog
ExecStart=/usr/bin/dotnet /home/ss14/watchdog/SS14.Watchdog.dll
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable ss14-watchdog >/dev/null
systemctl restart ss14-watchdog
sleep 5

# ============================================================
# Summary
# ============================================================
echo
echo "================================================================"
echo "  Done. Watchdog should be fetching the manifest right now."
echo "----------------------------------------------------------------"
echo "  Public IP:     $PUBLIC_IP"
echo "  Instance:      $INSTANCE_KEY"
echo "  Host user:     $HOST_USERNAME (auto-promoted via console.login_host_user)"
echo "  Manifest:      $MANIFEST_URL"
echo "  Game UDP:      $PUBLIC_IP:1212"
echo "  Watchdog API:  http://$PUBLIC_IP:5000/"
echo
if [[ "$GENERATED_TOKEN" -eq 1 ]]; then
  echo "  *** NEW API TOKEN GENERATED — SAVE IT NOW ***"
  echo "  $API_TOKEN"
else
  echo "  API token:     (using value supplied via env)"
fi
echo
echo "  Logs:    journalctl -u ss14-watchdog -f"
echo "  Status:  curl -s http://$PUBLIC_IP:1212/info | jq ."
echo "  Restart: sudo systemctl restart ss14-watchdog"
echo "  Update:  curl -X POST http://127.0.0.1:5000/instances/$INSTANCE_KEY/update -u \"$INSTANCE_KEY:\$API_TOKEN\""
echo "================================================================"
