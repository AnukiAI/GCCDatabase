<?php
/**
 * Docker Container Diagnostics Script for Plesk
 * Upload this to your Plesk httpdocs folder and access via browser
 */

header('Content-Type: text/html; charset=utf-8');
?>
<!DOCTYPE html>
<html>
<head>
    <title>GCC Docker Diagnostics</title>
    <style>
        body { font-family: monospace; padding: 20px; background: #1e1e1e; color: #d4d4d4; }
        .success { color: #4ec9b0; }
        .error { color: #f48771; }
        .warning { color: #dcdcaa; }
        .info { color: #9cdcfe; }
        pre { background: #2d2d30; padding: 10px; border-radius: 5px; overflow-x: auto; }
        .section { margin: 20px 0; padding: 15px; background: #252526; border-radius: 5px; }
        h2 { color: #569cd6; border-bottom: 2px solid #569cd6; padding-bottom: 5px; }
        .command { background: #1e1e1e; padding: 5px; margin: 5px 0; }
        button { background: #0e639c; color: white; border: none; padding: 10px 20px; cursor: pointer; border-radius: 3px; }
        button:hover { background: #1177bb; }
    </style>
</head>
<body>
<h1>🐳 GCC Docker Container Diagnostics</h1>

<?php
// Functions to run shell commands safely
function runCommand($cmd, $description = '') {
    echo "<div class='section'>";
    if ($description) {
        echo "<h3>$description</h3>";
    }
    echo "<div class='command'>Command: <code>$cmd</code></div>";
    echo "<pre>";
    $output = shell_exec($cmd . ' 2>&1');
    if ($output === null || $output === '') {
        echo "<span class='warning'>No output or command not available</span>";
    } else {
        echo htmlspecialchars($output);
    }
    echo "</pre></div>";
    return $output;
}

// Check if Docker is installed
echo "<div class='section'>";
echo "<h2>1. Docker Installation Check</h2>";
$dockerVersion = shell_exec('docker --version 2>&1');
if (strpos($dockerVersion, 'Docker version') !== false) {
    echo "<span class='success'>✅ Docker is installed</span><br>";
    echo "<pre>$dockerVersion</pre>";
} else {
    echo "<span class='error'>❌ Docker is not installed or not accessible</span><br>";
    echo "<pre>$dockerVersion</pre>";
    echo "<p class='error'>Docker must be installed first. Contact your hosting provider.</p>";
    exit;
}
echo "</div>";

// Check Docker daemon status
echo "<div class='section'>";
echo "<h2>2. Docker Daemon Status</h2>";
$dockerInfo = shell_exec('docker info 2>&1');
if (strpos($dockerInfo, 'Server Version') !== false) {
    echo "<span class='success'>✅ Docker daemon is running</span>";
} else {
    echo "<span class='error'>❌ Docker daemon is not running</span>";
    echo "<pre>$dockerInfo</pre>";
}
echo "</div>";

// Check container status
$containerName = 'gcc-webapp';
echo "<div class='section'>";
echo "<h2>3. Container Status: $containerName</h2>";

// Check if container exists
$containerCheck = shell_exec("docker ps -a --filter name=$containerName --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' 2>&1");
echo "<h3>Container List:</h3>";
echo "<pre>$containerCheck</pre>";

// Get container details if it exists
if (strpos($containerCheck, $containerName) !== false) {
    echo "<span class='info'>Container found. Checking details...</span><br><br>";

    // Check if running
    $isRunning = shell_exec("docker ps --filter name=$containerName --format '{{.Names}}' 2>&1");
    if (trim($isRunning) === $containerName) {
        echo "<span class='success'>✅ Container is RUNNING</span><br>";
    } else {
        echo "<span class='error'>❌ Container is STOPPED</span><br>";

        // Get exit code
        $exitCode = shell_exec("docker inspect $containerName --format='{{.State.ExitCode}}' 2>&1");
        echo "<div class='warning'>Exit Code: " . trim($exitCode) . "</div>";

        // Common exit codes
        $exitCodeInt = intval(trim($exitCode));
        echo "<div class='info'>";
        switch($exitCodeInt) {
            case 0:
                echo "Exit Code 0: Container stopped normally";
                break;
            case 1:
                echo "Exit Code 1: General application error";
                break;
            case 125:
                echo "Exit Code 125: Docker daemon error";
                break;
            case 126:
                echo "Exit Code 126: Container command not executable";
                break;
            case 127:
                echo "Exit Code 127: Container command not found";
                break;
            case 139:
                echo "Exit Code 139: Segmentation fault (SIGSEGV) - Architecture mismatch or missing dependencies";
                break;
            case 143:
                echo "Exit Code 143: Container received SIGTERM (graceful shutdown)";
                break;
            default:
                echo "Exit Code $exitCodeInt: Check logs for details";
        }
        echo "</div>";
    }
} else {
    echo "<span class='error'>❌ Container not found</span>";
}
echo "</div>";

// Get container logs
runCommand("docker logs $containerName --tail 50 2>&1", "4. Container Logs (Last 50 lines)");

// Check port availability
echo "<div class='section'>";
echo "<h2>5. Port Availability Check</h2>";
$ports = [80, 443, 5101, 5102, 8080];
foreach ($ports as $port) {
    $portCheck = shell_exec("netstat -tln | grep :$port 2>&1");
    if ($portCheck) {
        echo "<span class='warning'>Port $port: IN USE</span><br>";
        echo "<pre>$portCheck</pre>";
    } else {
        echo "<span class='success'>Port $port: AVAILABLE</span><br>";
    }
}
echo "</div>";

// Check disk space
runCommand("df -h /var/lib/docker 2>&1", "6. Docker Disk Space");

// Check memory
runCommand("free -h 2>&1", "7. System Memory");

// Get Docker events (last hour)
runCommand("docker events --since '1h ago' --until now --filter container=$containerName 2>&1", "8. Docker Events (Last Hour)");

// Diagnostic commands to fix common issues
echo "<div class='section'>";
echo "<h2>9. Quick Fix Commands</h2>";
echo "<h3>Copy and run these commands to fix common issues:</h3>";
?>

<div class='command'>
<h4>A. Restart Container:</h4>
<pre>docker restart <?php echo $containerName; ?></pre>
</div>

<div class='command'>
<h4>B. Stop and Remove Container:</h4>
<pre>docker stop <?php echo $containerName; ?> && docker rm <?php echo $containerName; ?></pre>
</div>

<div class='command'>
<h4>C. Redeploy with Proper Configuration:</h4>
<pre>docker run -d \
  --name <?php echo $containerName; ?> \
  --restart unless-stopped \
  -p 80:8080 \
  -v /opt/gcc-data:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -e Jwt__Key="your-super-secret-jwt-key-must-be-at-least-64-characters-long-for-security-12345" \
  -e DataProtection__PersistKeysToAzureBlobStorage=false \
  -e AZURE_AD_B2C_CLIENT_SECRET="not-using-azure" \
  -e AZURE_AD_CLIENT_SECRET="not-using-azure" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e SMTP_PASSWORD="not-using-email" \
  ghcr.io/anukiai/gcc-webapp:production-ready</pre>
</div>

<div class='command'>
<h4>D. Check Container Resource Usage:</h4>
<pre>docker stats <?php echo $containerName; ?> --no-stream</pre>
</div>

<div class='command'>
<h4>E. Interactive Debug Mode:</h4>
<pre>docker run -it --rm \
  -p 80:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  ghcr.io/anukiai/gcc-webapp:production-ready</pre>
</div>

<?php
echo "</div>";

// Check for common issues
echo "<div class='section'>";
echo "<h2>10. Common Issues Analysis</h2>";

// Memory check
$memInfo = shell_exec("free -m | grep Mem | awk '{print $2, $3, $4}' 2>&1");
list($total, $used, $free) = explode(' ', trim($memInfo));
if ($free < 500) {
    echo "<div class='error'>⚠️ Low Memory: Only {$free}MB free. Container may be killed by OOM killer.</div>";
}

// Check if port 80 is used by Apache/Nginx
$port80 = shell_exec("netstat -tlnp | grep :80 2>&1");
if (strpos($port80, 'apache') !== false || strpos($port80, 'nginx') !== false) {
    echo "<div class='warning'>⚠️ Port 80 is used by web server. Use a different port or configure reverse proxy.</div>";
}

// Check Docker restart policy
$restartPolicy = shell_exec("docker inspect $containerName --format='{{.HostConfig.RestartPolicy.Name}}' 2>&1");
if (trim($restartPolicy) !== 'unless-stopped' && trim($restartPolicy) !== 'always') {
    echo "<div class='warning'>⚠️ Container restart policy not set. Container won't auto-restart.</div>";
}

echo "</div>";

// Final recommendations
echo "<div class='section'>";
echo "<h2>11. Recommendations</h2>";
echo "<ol>";
echo "<li>If container keeps stopping with exit code 139, rebuild image for correct architecture</li>";
echo "<li>If port 80 is busy, use port 8080 or configure nginx proxy</li>";
echo "<li>Ensure at least 1GB free memory for container operation</li>";
echo "<li>Check container logs for specific error messages</li>";
echo "<li>Use 'unless-stopped' restart policy for automatic recovery</li>";
echo "</ol>";
echo "</div>";

// Refresh button
echo "<div style='text-align: center; margin: 20px;'>";
echo "<button onclick='location.reload()'>🔄 Refresh Diagnostics</button>";
echo "</div>";
?>

<script>
// Auto-refresh every 30 seconds
setTimeout(function() {
    if (confirm('Auto-refresh diagnostics?')) {
        location.reload();
    }
}, 30000);
</script>

</body>
</html>