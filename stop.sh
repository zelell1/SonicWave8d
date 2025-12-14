#!/bin/bash

# ═══════════════════════════════════════════════════════════════
#  SonicWave 8D - Stop All Services
#  Останавливает Docker, API и все связанные процессы
# ═══════════════════════════════════════════════════════════════

# Цвета
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}"
echo "╔════════════════════════════════════════════════╗"
echo "║    🛑  Остановка SonicWave 8D Services  🛑    ║"
echo "╚════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# Переход в директорию проекта
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Остановка .NET процессов (API и Frontend)
echo -e "${YELLOW}[1/3] Остановка .NET процессов...${NC}"

# Поиск всех процессов dotnet связанных с проектом
DOTNET_PIDS=$(ps aux | grep -E "dotnet.*(SonicWave8D|run)" | grep -v grep | awk '{print $2}')

if [ ! -z "$DOTNET_PIDS" ]; then
    echo "   Найдены процессы: $DOTNET_PIDS"
    echo "$DOTNET_PIDS" | while read pid; do
        echo "   Остановка PID: $pid"
        kill $pid 2>/dev/null || kill -9 $pid 2>/dev/null
    done
    sleep 2
    echo -e "${GREEN}✅ .NET процессы остановлены${NC}"
else
    echo -e "${CYAN}   Нет запущенных .NET процессов${NC}"
fi
echo ""

# Остановка Docker контейнеров
echo -e "${YELLOW}[2/3] Остановка Docker контейнеров...${NC}"

if docker-compose ps | grep -q "Up"; then
    docker-compose down
    echo -e "${GREEN}✅ Docker контейнеры остановлены${NC}"
else
    echo -e "${CYAN}   Docker контейнеры не запущены${NC}"
fi
echo ""

# Очистка временных файлов
echo -e "${YELLOW}[3/3] Очистка временных файлов...${NC}"

# Удаляем лог файлы
if [ -f api.log ]; then
    rm -f api.log
    echo "   Удалён api.log"
fi

# Опционально: очистка bin/obj (раскомментируйте если нужно)
# find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null
# find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null
# echo "   Очищены bin/obj директории"

echo -e "${GREEN}✅ Очистка завершена${NC}"
echo ""

# Проверка что всё остановлено
echo -e "${CYAN}Проверка запущенных процессов:${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Проверка Docker
if docker ps | grep -q "sonicwave8d"; then
    echo -e "${RED}⚠️  Docker контейнеры всё ещё запущены:${NC}"
    docker ps | grep "sonicwave8d"
else
    echo -e "${GREEN}✓ Docker контейнеры остановлены${NC}"
fi

# Проверка .NET процессов
if ps aux | grep -E "dotnet.*(SonicWave8D|run)" | grep -v grep > /dev/null; then
    echo -e "${RED}⚠️  .NET процессы всё ещё работают:${NC}"
    ps aux | grep -E "dotnet.*(SonicWave8D|run)" | grep -v grep
else
    echo -e "${GREEN}✓ .NET процессы остановлены${NC}"
fi

# Проверка портов
echo ""
echo -e "${CYAN}Проверка портов:${NC}"
if lsof -i :8000 > /dev/null 2>&1; then
    echo -e "${RED}⚠️  Порт 8000 всё ещё занят${NC}"
else
    echo -e "${GREEN}✓ Порт 8000 свободен${NC}"
fi

if lsof -i :5004 > /dev/null 2>&1; then
    echo -e "${RED}⚠️  Порт 5004 всё ещё занят${NC}"
else
    echo -e "${GREEN}✓ Порт 5004 свободен${NC}"
fi

if lsof -i :5432 > /dev/null 2>&1; then
    echo -e "${RED}⚠️  Порт 5432 всё ещё занят${NC}"
else
    echo -e "${GREEN}✓ Порт 5432 свободен${NC}"
fi

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║         ✅  ВСЕ СЕРВИСЫ ОСТАНОВЛЕНЫ  ✅       ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════╝${NC}"
echo ""
echo "Для повторного запуска: ./start-all.sh"
echo ""
