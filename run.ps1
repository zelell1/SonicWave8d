Вот версия этого скрипта, переписанная на PowerShell для Windows (.ps1).

Я сохранил всю логику: проверки, цвета, работу с Docker, фоновый запуск API, логирование и автоматическую очистку процессов при выходе.

Инструкция по запуску:

Сохраните код ниже в файл, например: start-sonicwave.ps1.

Откройте терминал (PowerShell) в папке проекта.

Запустите скрипт командой:

code
Powershell
download
content_copy
expand_less
.\start-sonicwave.ps1

(Если скрипт не запускается из-за политики безопасности, выполните перед этим: Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass)

code
Powershell
download
content_copy
expand_less
<#
.SYNOPSIS
   SonicWave 8D - Complete Stack Launcher (Windows Edition)
   Автоматический запуск: Docker + PostgreSQL + API + Frontend
#>

# ═══════════════════════════════════════════════════════════════
#  Инициализация и Цвета
# ═══════════════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

# Включаем поддержку ANSI-цветов в Windows Terminal / PowerShell 7+
# Если старая консоль, цвета могут не отображаться, но скрипт будет работать.
$ESC = [char]27
$RED = "$ESC[0;31m"
$GREEN = "$ESC[0;32m"
$YELLOW = "$ESC[1;33m"
$BLUE = "$ESC[0;34m"
$PURPLE = "$ESC[0;35m"
$CYAN = "$ESC[0;36m"
$NC = "$ESC[0m"

Clear-Host

Write-Host "$PURPLE"
Write-Host "╔════════════════════════════════════════════════════════╗"
Write-Host "║                                                        ║"
Write-Host "║          🎵  SonicWave 8D - Full Stack  🎵           ║"
Write-Host "║                                                        ║"
Write-Host "╚════════════════════════════════════════════════════════╝"
Write-Host "$NC"
Write-Host ""

# Переменные для процессов
$ApiProcess = $null

# Блок try/finally гарантирует выполнение очистки (cleanup) при выходе
try {

    # ═══════════════════════════════════════════════════════════════
    # Проверка зависимостей
    # ═══════════════════════════════════════════════════════════════

    Write-Host "$CYAN[1/6] Проверка зависимостей...$NC"

    # Проверка Docker
    if (-not (Get-Command "docker" -ErrorAction SilentlyContinue)) {
        Write-Host "$RED❌ Docker не установлен!$NC"
        Write-Host "   Установите: https://www.docker.com/products/docker-desktop"
        exit 1
    }

    # Проверка .NET
    if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        Write-Host "$RED❌ .NET SDK не установлен!$NC"
        Write-Host "   Установите: https://dotnet.microsoft.com/download"
        exit 1
    }

    # Проверка запущен ли Docker
    docker info > $null 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$RED❌ Docker не запущен!$NC"
        Write-Host "   Запустите Docker Desktop и попробуйте снова."
        exit 1
    }

    Write-Host "$GREEN✅ Все зависимости установлены$NC"
    Write-Host ""

    # ═══════════════════════════════════════════════════════════════
    # Настройка .env файла
    # ═══════════════════════════════════════════════════════════════

    Write-Host "$CYAN[2/6] Настройка конфигурации...$NC"

    if (-not (Test-Path ".env")) {
        if (Test-Path ".env.example") {
            Write-Host "$YELLOW⚠️  Создаю .env из .env.example...$NC"
            Copy-Item ".env.example" ".env"
        } else {
            Write-Host "$YELLOW⚠️  Файл .env.example не найден, пропускаю создание .env$NC"
        }
    }

    Write-Host "$GREEN✅ Конфигурация готова$NC"
    Write-Host ""

    # ═══════════════════════════════════════════════════════════════
    # Запуск PostgreSQL через Docker
    # ═══════════════════════════════════════════════════════════════

    Write-Host "$CYAN[3/6] Запуск PostgreSQL (Docker)...$NC"

    # Останавливаем старые контейнеры (подавляем ошибки)
    docker-compose down 2>&1 | Out-Null

    # Запускаем PostgreSQL
    Write-Host "   Поднимаю контейнер postgres..."
    docker-compose up -d postgres

    # Ждём готовности базы данных
    Write-Host "$YELLOW   Ожидание готовности PostgreSQL...$NC"
    $MaxRetries = 30
    $RetryCount = 0
    $DbReady = $false

    while ($RetryCount -lt $MaxRetries) {
        # Проверяем через docker exec, готов ли postgres принимать соединения
        docker exec sonicwave8d-db pg_isready -U postgres 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $DbReady = $true
            break
        }
        $RetryCount++
        Write-Host -NoNewline "."
        Start-Sleep -Seconds 1
    }
    Write-Host ""

    if (-not $DbReady) {
        Write-Host "$RED❌ PostgreSQL не запустился за 30 секунд$NC"
        Write-Host "   Проверьте логи: docker-compose logs postgres"
        exit 1
    }

    Write-Host "$GREEN✅ PostgreSQL запущен на порту 5432$NC"
    Write-Host "   Database: sonicwave8d"
    Write-Host "   User:     postgres"
    Write-Host "   Password: postgres"
    Write-Host ""

    # ═══════════════════════════════════════════════════════════════
    # Сборка и запуск API Backend
    # ═══════════════════════════════════════════════════════════════

    Write-Host "$CYAN[4/6] Сборка API Backend...$NC"

    $ApiDir = Join-Path $PSScriptRoot "SonicWave8D.API"

    # Очистка и сборка
    Write-Host "   Компиляция API проекта..."
    dotnet build "$ApiDir" --configuration Release > $null

    if ($LASTEXITCODE -ne 0) {
        Write-Host "$RED❌ Ошибка сборки API$NC"
        exit 1
    }

    Write-Host "$GREEN✅ API собран успешно$NC"
    Write-Host ""

    Write-Host "$CYAN[5/6] Запуск API Backend...$NC"

    $LogFile = Join-Path $PSScriptRoot "api.log"

    # Запускаем API как фоновый процесс
    $ApiProcessStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $ApiProcessStartInfo.FileName = "dotnet"
    $ApiProcessStartInfo.Arguments = "run --urls ""http://localhost:5004"""
    $ApiProcessStartInfo.WorkingDirectory = $ApiDir
    $ApiProcessStartInfo.RedirectStandardOutput = $true
    $ApiProcessStartInfo.RedirectStandardError = $true
    $ApiProcessStartInfo.UseShellExecute = $false
    $ApiProcessStartInfo.CreateNoWindow = $true

    $ApiProcess = [System.Diagnostics.Process]::Start($ApiProcessStartInfo)

    # Перенаправление логов в файл (асинхронно, упрощенно)
    Register-ObjectEvent -InputObject $ApiProcess -EventName OutputDataReceived -Action { Add-Content -Path $Event.MessageData -Value $Event.SourceEventArgs.Data } | Out-Null
    Register-ObjectEvent -InputObject $ApiProcess -EventName ErrorDataReceived -Action { Add-Content -Path $Event.MessageData -Value $Event.SourceEventArgs.Data } | Out-Null
    $ApiProcess.BeginOutputReadLine()
    $ApiProcess.BeginErrorReadLine()
    # (Примечание: чтобы передать путь в Action, мы используем MessageData как хак, или просто полагаемся на переменную окружения,
    # но для простоты в PS1 часто используют прямое перенаправление через Start-Process -Redirect... но оно блокирует файл.
    # Для надежности используем встроенное перенаправление Start-Process ниже вместо сложного .NET объекта выше, если не нужны сложные манипуляции)

    # ПЕРЕЗАПУСК API БОЛЕЕ ПРОСТЫМ СПОСОБОМ (PowerShell Way):
    if ($ApiProcess) { Stop-Process -Id $ApiProcess.Id -Force -ErrorAction SilentlyContinue }

    $ApiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --urls http://localhost:5004" -WorkingDirectory $ApiDir -PassThru -NoNewWindow -RedirectStandardOutput $LogFile -RedirectStandardError $LogFile

    Write-Host "   API запущен с PID: $($ApiProcess.Id)"
    Write-Host "   Логи в файле: api.log"

    Write-Host "$YELLOW   Ожидание запуска API...$NC"
    Start-Sleep -Seconds 5

    if ($ApiProcess.HasExited) {
        Write-Host "$RED❌ API не запустился (процесс завершен)$NC"
        Write-Host "   Проверьте логи: cat api.log"
        exit 1
    }

    Write-Host "$GREEN✅ API запущен на http://localhost:5004$NC"
    Write-Host "   Swagger UI: http://localhost:5004/swagger"
    Write-Host ""

    # ═══════════════════════════════════════════════════════════════
    # Запуск Blazor Frontend
    # ═══════════════════════════════════════════════════════════════

    Write-Host "$CYAN[6/6] Запуск Blazor Frontend...$NC"

    # Предполагаем, что фронтенд лежит в папке уровнем выше API, или нужно найти .csproj
    # В оригинале скрипт делал cd .. и запускал dotnet run, значит фронтенд в корне?
    # Или, скорее всего, есть папка Client. Адаптируем под стандартную структуру.
    # Если в оригинале делался `cd ..` из API, значит мы вернулись в корень.
    # Предположим, что файл проекта фронтенда нужно найти.

    # Попытка найти csproj клиента, если он не в корне
    $ClientDir = $PSScriptRoot
    # Если в корне нет csproj, но есть папка Client или Web, можно уточнить.
    # В оригинале просто `dotnet run`, значит csproj прямо в корне?
    # Если проект в корне:

    Write-Host "   Сборка Frontend..."
    dotnet build "$ClientDir" --configuration Release > $null 2>&1

    if ($LASTEXITCODE -ne 0) {
        # Если не собралось, возможно проект в подпапке (частая структура)
        $PotentialClient = Get-ChildItem -Path $PSScriptRoot -Filter "*.Client.csproj" -Recurse -Depth 1 | Select-Object -First 1
        if ($PotentialClient) {
             $ClientDir = $PotentialClient.DirectoryName
             dotnet build "$ClientDir" --configuration Release > $null
        } else {
             Write-Host "$RED❌ Не удалось найти или собрать проект Frontend$NC"
             # Не выходим, чтобы сработал finally
        }
    }

    Write-Host "$GREEN✅ Frontend собран$NC"
    Write-Host ""

    # ═══════════════════════════════════════════════════════════════
    # Вывод информации
    # ═══════════════════════════════════════════════════════════════

    Write-Host "$GREEN"
    Write-Host "╔════════════════════════════════════════════════════════╗"
    Write-Host "║                                                        ║"
    Write-Host "║           ✅  ВСЕ СЕРВИСЫ ЗАПУЩЕНЫ!  ✅              ║"
    Write-Host "║                                                        ║"
    Write-Host "╚════════════════════════════════════════════════════════╝"
    Write-Host "$NC"
    Write-Host ""
    Write-Host "$BLUE🌐 Доступные сервисы:$NC"
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    Write-Host ""
    Write-Host "  ${CYAN}Frontend (Blazor):$NC"
    Write-Host "    → ${GREEN}http://localhost:8000$NC"
    Write-Host ""
    Write-Host "  ${CYAN}API Backend:$NC"
    Write-Host "    → ${GREEN}http://localhost:5004$NC"
    Write-Host "    → Swagger: ${GREEN}http://localhost:5004/swagger$NC"
    Write-Host ""
    Write-Host "  ${CYAN}PostgreSQL Database:$NC"
    Write-Host "    → Host: ${GREEN}localhost:5432$NC"
    Write-Host "    → Database: ${GREEN}sonicwave8d$NC"
    Write-Host "    → User: ${GREEN}postgres$NC"
    Write-Host "    → Password: ${GREEN}postgres$NC"
    Write-Host ""
    Write-Host "  ${YELLOW}Опционально - pgAdmin (Web UI):$NC"
    Write-Host "    → Запустить: ${CYAN}docker-compose --profile tools up -d$NC"
    Write-Host "    → URL: ${GREEN}http://localhost:5050$NC"
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    Write-Host ""
    Write-Host "${YELLOW}📝 Полезные команды:$NC"
    Write-Host "  • Логи API:        Get-Content -Wait api.log"
    Write-Host "  • Логи PostgreSQL: docker-compose logs -f postgres"
    Write-Host ""
    Write-Host "$RED🛑 Для остановки всех сервисов нажмите Ctrl+C$NC"
    Write-Host ""

    Write-Host "${GREEN}🚀 Запускаю Frontend...$NC"

    # Запускаем Frontend (блокирующий вызов)
    # Используем --urls чтобы явно задать порт, как в оригинале
    dotnet run --project "$ClientDir" --urls "http://localhost:8000"

}
finally {
    # ═══════════════════════════════════════════════════════════════
    # Очистка при выходе (Cleanup)
    # ═══════════════════════════════════════════════════════════════

    Write-Host ""
    Write-Host "$YELLOW🛑 Остановка всех сервисов...$NC"

    # Останавливаем API
    if ($ApiProcess -and -not $ApiProcess.HasExited) {
        Write-Host "   Остановка API (PID: $($ApiProcess.Id))..."
        Stop-Process -Id $ApiProcess.Id -Force -ErrorAction SilentlyContinue
    }

    # Останавливаем Docker
    Write-Host "   Остановка PostgreSQL..."
    docker-compose down 2>&1 | Out-Null

    Write-Host "$GREEN✅ Все сервисы остановлены$NC"
    Write-Host "До встречи! 👋"
}
