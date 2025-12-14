#!/bin/bash

# ═══════════════════════════════════════════════════════════════
#  SonicWave 8D - Complete Stack Launcher
#  Автоматический запуск: Docker + PostgreSQL + API + Frontend
# ═══════════════════════════════════════════════════════════════

set -e  # Выход при ошибке

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${PURPLE}"
echo "╔════════════════════════════════════════════════════════╗"
echo "║                                                        ║"
echo "║          🎵  SonicWave 8D - Full Stack  🎵           ║"
echo "║                                                        ║"
echo "╚════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""

# ═══════════════════════════════════════════════════════════════
# Проверка зависимостей
# ═══════════════════════════════════════════════════════════════

echo -e "${CYAN}[1/6] Проверка зависимостей...${NC}"

# Проверка Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker не установлен!${NC}"
    echo "   Установите: https://www.docker.com/products/docker-desktop"
    exit 1
fi

# Проверка docker-compose
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}❌ docker-compose не установлен!${NC}"
    echo "   Обычно идёт с Docker Desktop"
    exit 1
fi

# Проверка .NET
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK не установлен!${NC}"
    echo "   Установите: https://dotnet.microsoft.com/download"
    exit 1
fi

# Проверка что Docker запущен
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker не запущен!${NC}"
    echo "   Запустите Docker Desktop и попробуйте снова"
    exit 1
fi

echo -e "${GREEN}✅ Все зависимости установлены${NC}"
echo ""

# ═══════════════════════════════════════════════════════════════
# Переход в директорию проекта
# ═══════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# ═══════════════════════════════════════════════════════════════
# Настройка .env файла
# ═══════════════════════════════════════════════════════════════

echo -e "${CYAN}[2/6] Настройка конфигурации...${NC}"

if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠️  Создаю .env из .env.example...${NC}"
    cp .env.example .env
fi

echo -e "${GREEN}✅ Конфигурация готова${NC}"
echo ""

# ═══════════════════════════════════════════════════════════════
# Запуск PostgreSQL через Docker
# ═══════════════════════════════════════════════════════════════

echo -e "${CYAN}[3/6] Запуск PostgreSQL (Docker)...${NC}"

# Останавливаем старые контейнеры если есть
docker-compose down > /dev/null 2>&1 || true

# Запускаем PostgreSQL
echo "   Поднимаю контейнер postgres..."
docker-compose up -d postgres

# Ждём готовности базы данных
echo -e "${YELLOW}   Ожидание готовности PostgreSQL...${NC}"
MAX_RETRIES=30
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if docker exec sonicwave8d-db pg_isready -U postgres > /dev/null 2>&1; then
        break
    fi
    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo -n "."
    sleep 1
done

echo ""

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    echo -e "${RED}❌ PostgreSQL не запустился за 30 секунд${NC}"
    echo "   Проверьте логи: docker-compose logs postgres"
    exit 1
fi

echo -e "${GREEN}✅ PostgreSQL запущен на порту 5432${NC}"
echo "   Database: sonicwave8d"
echo "   User: postgres"
echo "   Password: postgres"
echo ""

# ═══════════════════════════════════════════════════════════════
# Сборка и запуск API Backend
# ═══════════════════════════════════════════════════════════════

echo -e "${CYAN}[4/6] Сборка API Backend...${NC}"

cd SonicWave8D.API

# Очистка предыдущей сборки
dotnet clean > /dev/null 2>&1

# Сборка проекта
echo "   Компиляция API проекта..."
if ! dotnet build --configuration Release > /dev/null 2>&1; then
    echo -e "${RED}❌ Ошибка сборки API${NC}"
    echo "   Попробуйте: cd SonicWave8D.API && dotnet build"
    exit 1
fi

echo -e "${GREEN}✅ API собран успешно${NC}"
echo ""

echo -e "${CYAN}[5/6] Запуск API Backend...${NC}"

# Запускаем API в фоне
dotnet run --urls "http://localhost:5004" > ../api.log 2>&1 &
API_PID=$!

echo "   API запущен с PID: $API_PID"
echo "   Логи в файле: api.log"

# Ждём пока API запустится
echo -e "${YELLOW}   Ожидание запуска API...${NC}"
sleep 5

# Проверяем что процесс запустился
if ! ps -p $API_PID > /dev/null; then
    echo -e "${RED}❌ API не запустился${NC}"
    echo "   Проверьте логи: tail -f api.log"
    exit 1
fi

echo -e "${GREEN}✅ API запущен на http://localhost:5004${NC}"
echo "   Swagger UI: http://localhost:5004/swagger"
echo ""

cd ..

# ═══════════════════════════════════════════════════════════════
# Запуск Blazor Frontend
# ═══════════════════════════════════════════════════════════════

echo -e "${CYAN}[6/6] Запуск Blazor Frontend...${NC}"

# Очистка предыдущей сборки
dotnet clean > /dev/null 2>&1

echo "   Сборка Frontend..."
if ! dotnet build --configuration Release > /dev/null 2>&1; then
    echo -e "${RED}❌ Ошибка сборки Frontend${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Frontend собран${NC}"
echo ""

# ═══════════════════════════════════════════════════════════════
# Вывод информации о запущенных сервисах
# ═══════════════════════════════════════════════════════════════

echo -e "${GREEN}"
echo "╔════════════════════════════════════════════════════════╗"
echo "║                                                        ║"
echo "║           ✅  ВСЕ СЕРВИСЫ ЗАПУЩЕНЫ!  ✅              ║"
echo "║                                                        ║"
echo "╚════════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""
echo -e "${BLUE}🌐 Доступные сервисы:${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo -e "  ${CYAN}Frontend (Blazor):${NC}"
echo -e "    → ${GREEN}http://localhost:8000${NC}"
echo ""
echo -e "  ${CYAN}API Backend:${NC}"
echo -e "    → ${GREEN}http://localhost:5004${NC}"
echo -e "    → Swagger: ${GREEN}http://localhost:5004/swagger${NC}"
echo ""
echo -e "  ${CYAN}PostgreSQL Database:${NC}"
echo -e "    → Host: ${GREEN}localhost:5432${NC}"
echo -e "    → Database: ${GREEN}sonicwave8d${NC}"
echo -e "    → User: ${GREEN}postgres${NC}"
echo -e "    → Password: ${GREEN}postgres${NC}"
echo ""
echo -e "  ${YELLOW}Опционально - pgAdmin (Web UI):${NC}"
echo -e "    → Запустить: ${CYAN}docker-compose --profile tools up -d${NC}"
echo -e "    → URL: ${GREEN}http://localhost:5050${NC}"
echo -e "    → Email: ${GREEN}admin@sonicwave8d.local${NC}"
echo -e "    → Password: ${GREEN}admin${NC}"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo -e "${YELLOW}📝 Полезные команды:${NC}"
echo "  • Логи API:        tail -f api.log"
echo "  • Логи PostgreSQL: docker-compose logs -f postgres"
echo "  • Остановить всё:  ./stop-all.sh  (или Ctrl+C)"
echo ""
echo -e "${RED}🛑 Для остановки всех сервисов нажмите Ctrl+C${NC}"
echo ""

# ═══════════════════════════════════════════════════════════════
# Функция очистки при выходе
# ═══════════════════════════════════════════════════════════════

cleanup() {
    echo ""
    echo -e "${YELLOW}🛑 Остановка всех сервисов...${NC}"

    # Останавливаем API
    if [ ! -z "$API_PID" ]; then
        echo "   Остановка API (PID: $API_PID)..."
        kill $API_PID 2>/dev/null || true
        sleep 1
        # Принудительно если не остановился
        kill -9 $API_PID 2>/dev/null || true
    fi

    # Останавливаем Docker контейнеры
    echo "   Остановка PostgreSQL..."
    docker-compose down > /dev/null 2>&1

    echo -e "${GREEN}✅ Все сервисы остановлены${NC}"
    echo ""
    echo "До встречи! 👋"
    exit 0
}

# Регистрируем обработчик сигналов
trap cleanup SIGINT SIGTERM EXIT

# ═══════════════════════════════════════════════════════════════
# Запуск Frontend (блокирующий вызов)
# ═══════════════════════════════════════════════════════════════

echo -e "${GREEN}🚀 Запускаю Frontend...${NC}"
echo ""

# Запускаем и ждём
dotnet run --urls "http://localhost:8000"

# Если dotnet run завершился, вызываем cleanup
cleanup
