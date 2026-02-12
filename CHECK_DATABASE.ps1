# Database Connection Check Script for Inventory Management System

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Connection Diagnostic Tool" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if PostgreSQL is running
Write-Host "1. Checking if PostgreSQL is running..." -ForegroundColor Yellow
$postgresProcess = Get-Process -Name postgres -ErrorAction SilentlyContinue
if ($postgresProcess) {
    Write-Host "   ? PostgreSQL is running" -ForegroundColor Green
} else {
    Write-Host "   ? PostgreSQL is NOT running!" -ForegroundColor Red
    Write-Host "   Please start PostgreSQL service and try again." -ForegroundColor Red
    Write-Host "`nPress any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit
}

# Check if port 5432 is accessible
Write-Host "`n2. Checking if PostgreSQL port (5432) is accessible..." -ForegroundColor Yellow
try {
    $connection = Test-NetConnection -ComputerName localhost -Port 5432 -WarningAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Host "   ? Port 5432 is accessible" -ForegroundColor Green
    } else {
        Write-Host "   ? Port 5432 is NOT accessible!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ? Cannot test port connectivity" -ForegroundColor Red
}

# Check database connection
Write-Host "`n3. Checking database connection..." -ForegroundColor Yellow
Write-Host "   Using connection: Host=localhost;Database=inventory_ac_db;Username=postgres;Password=2003" -ForegroundColor Cyan

$env:PGPASSWORD = "2003"
$testQuery = "SELECT version();"
$result = psql -U postgres -h localhost -d postgres -c $testQuery 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ? Successfully connected to PostgreSQL" -ForegroundColor Green
    
    # Check if database exists
    Write-Host "`n4. Checking if database 'inventory_ac_db' exists..." -ForegroundColor Yellow
    $dbCheck = psql -U postgres -h localhost -d postgres -t -c "SELECT 1 FROM pg_database WHERE datname='inventory_ac_db';" 2>&1
    
    if ($dbCheck -match "1") {
        Write-Host "   ? Database 'inventory_ac_db' exists" -ForegroundColor Green
        
        # Check tables
        Write-Host "`n5. Checking database tables..." -ForegroundColor Yellow
        $tables = psql -U postgres -h localhost -d inventory_ac_db -t -c "\dt" 2>&1
        if ($tables) {
            Write-Host "   ? Database has tables" -ForegroundColor Green
            Write-Host "`n   Tables found:" -ForegroundColor Cyan
            Write-Host $tables
        } else {
            Write-Host "   ! Database exists but has no tables (migrations will create them)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ! Database 'inventory_ac_db' does NOT exist" -ForegroundColor Yellow
        Write-Host "   The application will create it automatically on first run." -ForegroundColor Cyan
    }
} else {
    Write-Host "   ? Cannot connect to PostgreSQL!" -ForegroundColor Red
    Write-Host "`n   Error details:" -ForegroundColor Red
    Write-Host "   $result" -ForegroundColor Red
    Write-Host "`n   Possible issues:" -ForegroundColor Yellow
    Write-Host "   - Wrong password (current: 2003)" -ForegroundColor Yellow
    Write-Host "   - PostgreSQL not configured to allow local connections" -ForegroundColor Yellow
    Write-Host "   - pg_hba.conf needs to allow password authentication" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "If PostgreSQL is running and port is accessible," -ForegroundColor White
Write-Host "but you cannot connect, please check:" -ForegroundColor White
Write-Host "  1. Your PostgreSQL password (should be '2003')" -ForegroundColor White
Write-Host "  2. Update appsettings.json if password is different" -ForegroundColor White
Write-Host "  3. Check pg_hba.conf for connection permissions" -ForegroundColor White

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
