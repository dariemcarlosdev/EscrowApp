# -------------------------------------------------
# Test Runner for Notification Hook (Multi-Channel)
# -------------------------------------------------
# Tests the notification hook and verifies config exists.
# Usage: powershell -File .claude/hooks/test-runner

echo "=== Claude Hook Notification Test ===" -ForegroundColor Cyan

# 1. Verify config file exists
$configPath = ".claude/hooks/notification-config.json"
if (Test-Path $configPath) {
    echo "[✓] Config file found" -ForegroundColor Green
    $config = cat $configPath -Raw | ConvertFrom-Json
    echo "    Enabled channels:" -NoNewline
    $enabled = @()
    if ($config.console.enabled) { $enabled += "Console" }
    if ($config.fileLog.enabled) { $enabled += "File" }
    if ($config.slack.enabled -and $config.slack.webhookUrl) { $enabled += "Slack" }
    if ($config.email.enabled -and $config.email.from -and $config.email.to) { $enabled += "Email" }
    if ($config.teams.enabled -and $config.teams.webhookUrl) { $enabled += "Teams" }
    echo ($enabled -join ", ")
} else {
    echo "[✗] Config file missing. Creating default..." -ForegroundColor Red
    echo "    Run the notification script to auto-create." -ForegroundColor Yellow
}

# 2. Run the notification script directly
echo "`n[→] Triggering notification hook directly..." -ForegroundColor Cyan
powershell -ExecutionPolicy Bypass -File ".claude/hooks/notification"

# 3. Verify log file created
$logPath = ".claude/hooks/notifications.log"
if (Test-Path $logPath) {
    $lastEntry = cat $logPath -Tail 1
    echo "`n[✓] Log entry created:" -ForegroundColor Green
    echo "    $lastEntry" -ForegroundColor Gray
} else {
    echo "`n[✗] Log file not found" -ForegroundColor Red
}

# 4. Instructions for enabling external channels
echo "`n=== Next Steps ===" -ForegroundColor Cyan
echo "To enable Slack, Teams, or Email:" -ForegroundColor White
echo "1. Edit: .claude/hooks/notification-config.json" -ForegroundColor Yellow
echo "2. Set 'enabled' to true and fill in credentials" -ForegroundColor Yellow
echo "3. Re-run this test" -ForegroundColor Yellow

echo "`nExample Slack config:" -ForegroundColor Gray
echo '{ "slack": { "webhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL", "enabled": true } }' -ForegroundColor Gray

echo "`nTest complete! 🎉" -ForegroundColor Green
