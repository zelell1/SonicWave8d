# 🎵 SonicWave 8D - Пространственный Аудио Конвертер

Веб-приложение для преобразования аудиофайлов с применением эквалайзера и создания 8D пространственного эффекта.

---

## 📋 Содержание

- [Описание проекта](#описание-проекта)
- [Возможности](#возможности)
- [Технологический стек](#технологический-стек)
- [Структура проекта](#структура-проекта)
- [Быстрый старт](#быстрый-старт)
- [Архитектура системы](#архитектура-системы)
- [Как работает эквалайзер](#как-работает-эквалайзер)
- [Как работает 8D эффект](#как-работает-8d-эффект)
- [База данных](#база-данных)
- [API Endpoints](#api-endpoints)
- [Хранение данных](#хранение-данных)
- [Обработка аудио](#обработка-аудио)

---

## 📖 Описание проекта

**SonicWave 8D** — это полнофункциональное веб-приложение для обработки аудиофайлов с возможностью:

- Загрузки аудиофайлов (MP3, WAV, OGG, FLAC)
- Применения 10-полосного параметрического эквалайзера
- Создания 8D пространственного звука (эффект вращения звука вокруг слушателя)
- Сохранения пользовательских пресетов эквалайзера
- Управления библиотекой треков
- Создания плейлистов

Приложение работает **полностью на стороне клиента** (обработка аудио в браузере) с опциональным серверным API для сохранения данных.

---

## ✨ Возможности

### 🎚️ Эквалайзер
- **10 полос** с частотами: 60, 170, 310, 600, 1000, 3000, 6000, 12000, 14000, 16000 Hz
- Диапазон регулировки: -12 дБ до +12 дБ
- **12 встроенных пресетов**: Flat, Bass Boost, Bass Cut, Treble Boost, Electronic, Vocal Booster, Pop, Rock, Jazz, Classical, Hip-Hop, Acoustic
- Создание и сохранение **кастомных пресетов**

### 🌀 8D Пространственный эффект
- Эффект вращения звука вокруг головы слушателя
- Период вращения: 10 секунд
- Плавное изменение панорамы и громкости
- Можно включать/выключать независимо от эквалайзера

### 💾 Управление треками
- Загрузка файлов до 100 МБ
- Воспроизведение оригинала и обработанной версии
- Сравнение "до/после" одним кликом
- Сохранение настроек для каждого трека
- Избранные треки

### 👤 Система пользователей
- Регистрация и авторизация
- Личная библиотека треков
- Персональные пресеты
- Плейлисты

---

## 🛠️ Технологический стек

### Frontend
- **Blazor WebAssembly** (.NET 9) — клиентское веб-приложение на C#
- **Tailwind CSS** — стилизация интерфейса
- **JavaScript Web Audio API** — обработка аудио
- **IndexedDB** — локальное хранилище (оптимизация с Blob)

### Backend API (опционально)
- **ASP.NET Core 9** Web API
- **Entity Framework Core 9** — ORM
- **PostgreSQL 16** — реляционная база данных
- **Docker** — контейнеризация БД
- **JWT Authentication** — аутентификация

---

## 📁 Структура проекта

```
SonicWave8D copy/
│
├── 📱 Frontend (Blazor WebAssembly)
│   ├── Components/           # Blazor компоненты
│   │   ├── AuthScreen.razor        # Экран входа/регистрации
│   │   ├── FileUpload.razor        # Загрузка файлов
│   │   ├── Header.razor            # Шапка сайта
│   │   └── TrackItem.razor         # Компонент трека с плеером
│   │
│   ├── Models/               # Модели данных (клиент)
│   │   └── AudioModels.cs          # User, AudioTrack, EqualizerPreset
│   │
│   ├── Pages/                # Страницы приложения
│   │   └── Index.razor             # Главная страница
│   │
│   ├── Services/             # Сервисы
│   │   ├── AudioService.cs         # Взаимодействие с Web Audio API
│   │   ├── AuthService.cs          # Аутентификация
│   │   └── StorageService.cs       # Работа с IndexedDB
│   │
│   ├── wwwroot/              # Статические файлы
│   │   ├── css/
│   │   │   └── app.css             # Tailwind стили
│   │   ├── js/
│   │   │   ├── audioPlayer.js      # Плеер
│   │   │   ├── audioProcessor.js   # Обработка аудио (EQ + 8D)
│   │   │   ├── storage.js          # IndexedDB с Blob оптимизацией
│   │   │   └── utils.js            # Утилиты
│   │   └── index.html              # HTML точка входа
│   │
│   ├── Program.cs            # Точка входа приложения
│   ├── App.razor             # Корневой компонент
│   ├── MainLayout.razor      # Основной layout
│   └── SonicWave8D.csproj    # Конфигурация проекта
│
├── 🗄️ Backend API (опционально)
│   ├── SonicWave8D.API/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs       # Аутентификация
│   │   │   ├── TracksController.cs     # CRUD треков
│   │   │   ├── PresetsController.cs    # CRUD пресетов
│   │   │   └── PlaylistsController.cs  # Управление плейлистами
│   │   │
│   │   ├── Data/
│   │   │   └── AppDbContext.cs         # EF Core DbContext
│   │   │
│   │   ├── Database/
│   │   │   └── init.sql                # SQL скрипт инициализации
│   │   │
│   │   ├── Services/
│   │   │   ├── AuthService.cs          # Бизнес-логика auth
│   │   │   ├── TrackService.cs         # Бизнес-логика треков
│   │   │   ├── PresetService.cs        # Бизнес-логика пресетов
│   │   │   └── PlaylistService.cs      # Бизнес-логика плейлистов
│   │   │
│   │   ├── Program.cs                  # Конфигурация API
│   │   ├── appsettings.json            # Настройки
│   │   └── SonicWave8D.API.csproj
│   │
│   └── SonicWave8D.Shared/             # Общие модели
│       ├── Models/
│       │   └── Entities.cs             # Модели БД
│       └── DTOs/
│           └── DTOs.cs                 # Data Transfer Objects
│
├── 🐳 Docker
│   ├── docker-compose.yml    # Конфигурация контейнеров
│   ├── .env.example          # Пример переменных окружения
│   └── .env                  # Переменные окружения (создаётся)
│
├── 🚀 Скрипты запуска
│   ├── start.sh              # Запуск всего стека
│   └── stop.sh               # Остановка всех сервисов
│
└── 📄 Документация
    └── README.md             # Этот файл
```

---

## 🚀 Быстрый старт

### Требования

- **.NET 9 SDK** — https://dotnet.microsoft.com/download
- **Docker Desktop** — https://www.docker.com/products/docker-desktop
- **macOS/Linux** (для скриптов) или **Windows** (нужно запускать вручную)

### Запуск

```bash
# 1. Перейдите в директорию проекта
cd "Physics_Projecttt/SonicWave8D copy"

# 2. Запустите всё одной командой
./start.sh
```

Скрипт автоматически:
1. ✅ Проверит зависимости (Docker, .NET)
2. 🐳 Запустит PostgreSQL через Docker
3. 🔨 Соберёт и запустит API Backend
4. 🌐 Запустит Blazor Frontend

### Доступ к сервисам

После запуска откроются:

| Сервис | URL | Описание |
|--------|-----|----------|
| **Frontend** | http://localhost:8000 | Главное приложение |
| **API** | http://localhost:5004 | REST API |
| **Swagger** | http://localhost:5004/swagger | Документация API |
| **PostgreSQL** | localhost:5432 | База данных |

### Остановка

```bash
./stop.sh
```

Или нажмите **Ctrl+C** в терминале где запущено.

---

## 🏗️ Архитектура системы

### Общая схема

```
┌─────────────────────────────────────────────────────────────┐
│                    Браузер пользователя                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │           Blazor WebAssembly (Frontend)               │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │  │
│  │  │  Pages   │  │Components│  │   Services        │   │  │
│  │  │ (Razor)  │  │ (Razor)  │  │  - AudioService  │   │  │
│  │  └──────────┘  └──────────┘  │  - AuthService   │   │  │
│  │                                │  - StorageService│   │  │
│  │                                └─────────┬────────┘   │  │
│  └──────────────────────────────────────────┼────────────┘  │
│                                              │                │
│  ┌──────────────────────────────────────────▼────────────┐  │
│  │              JavaScript Interop (JSRuntime)           │  │
│  │  ┌──────────────────────────────────────────────────┐ │  │
│  │  │  Web Audio API      │  IndexedDB  │  Fetch API  │ │  │
│  │  │  - audioProcessor.js │  storage.js│             │ │  │
│  │  │  - EQ filters        │  Blob store│             │ │  │
│  │  │  - 8D panning        │            │             │ │  │
│  │  └──────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/REST API
                       │ (опционально)
┌──────────────────────▼──────────────────────────────────────┐
│                   Backend API Server                         │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  ASP.NET Core Web API                               │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │    │
│  │  │ Controllers  │  │  Services    │  │  Models  │ │    │
│  │  │ (REST API)   │──│ (Logic)      │──│ (Entities│ │    │
│  │  └──────────────┘  └──────────────┘  └──────────┘ │    │
│  └──────────────────────────┬──────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────┘
                              │ Entity Framework Core
┌─────────────────────────────▼───────────────────────────────┐
│                    PostgreSQL Database                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Tables: users, tracks, custom_presets, playlists,  │   │
│  │          playlist_tracks, favorite_tracks, files     │   │
│  └──────────────────────────────────────────────────────┘   │
│                      (Docker Container)                      │
└──────────────────────────────────────────────────────────────┘
```

### Компоненты системы

#### 1. **Frontend (Blazor WebAssembly)**
- Работает полностью в браузере
- Написан на C# (компилируется в WebAssembly)
- Управляет UI и взаимодействием с пользователем
- Вызывает JavaScript для работы с Web Audio API

#### 2. **Web Audio API (JavaScript)**
- Обрабатывает аудио в браузере (процессор и память клиента)
- Применяет фильтры эквалайзера
- Создаёт 8D эффект через панорамирование
- Рендерит финальный аудиофайл

#### 3. **IndexedDB (Browser Storage)**
- Локальная база данных в браузере
- Хранит треки (метаданные + аудио как Blob)
- Хранит пресеты пользователя
- **Оптимизация**: вместо Base64 используется Blob (экономия 33% места)

#### 4. **Backend API (опционально)**
- REST API на ASP.NET Core
- Синхронизация данных между устройствами
- Публичные пресеты и плейлисты
- Аутентификация через JWT

#### 5. **PostgreSQL Database**
- Реляционная БД для серверного хранения
- Таблицы: users, tracks, custom_presets, playlists
- Запускается в Docker контейнере

---

## 🎚️ Как работает эквалайзер

### Принцип работы

Эквалайзер — это набор **фильтров**, которые усиливают или ослабляют определённые частоты звука.

### Технические детали

#### Частоты (10 полос)

```
60 Hz    — Саб-бас (sub-bass)
170 Hz   — Бас (bass)
310 Hz   — Низкие частоты (low-mid)
600 Hz   — Средние низкие
1000 Hz  — Средние (mid)
3000 Hz  — Средние высокие
6000 Hz  — Высокие (high)
12000 Hz — Очень высокие
14000 Hz — Ультра высокие
16000 Hz — Экстремальные высокие
```

#### Типы фильтров

В зависимости от полосы используются разные типы фильтров Web Audio API:

```javascript
// Частота 60 Hz (первая полоса)
filter.type = 'lowshelf';  // Полочный фильр для низких частот
filter.Q.value = 0.5;       // Ширина полосы (добротность)

// Средние частоты (170 Hz - 14000 Hz)
filter.type = 'peaking';    // Пиковый фильтр
filter.Q.value = 0.5;       // Широкая музыкальная полоса

// Частота 16000 Hz (последняя полоса)
filter.type = 'highshelf';  // Полочный фильтр для высоких частот
filter.Q.value = 0.5;
```

#### Усиление (Gain)

- Диапазон: **-12 дБ** до **+12 дБ**
- **0 дБ** = без изменений
- **+12 дБ** = усиление в ~4 раза по амплитуде
- **-12 дБ** = ослабление в ~4 раза

### Процесс обработки

```
1. Загрузка аудио файла
   ↓
2. Декодирование в AudioBuffer (PCM данные)
   ↓
3. Создание OfflineAudioContext (рендеринг без воспроизведения)
   ↓
4. Создание цепочки фильтров:
   Source → Filter1 (60Hz) → Filter2 (170Hz) → ... → Filter10 (16kHz)
   ↓
5. Применение значений Gain к каждому фильтру
   ↓
6. Рендеринг финального AudioBuffer
   ↓
7. Конвертация в WAV формат
   ↓
8. Создание Blob и Data URL для воспроизведения
```

### Код обработки (JavaScript)

```javascript
// audioProcessor.js
export async function process8DAudio(fileDataUrl, gains, enable8D) {
    // 1. Декодирование аудио
    const arrayBuffer = await dataUrlToArrayBuffer(fileDataUrl);
    const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);
    
    // 2. Создание offline context для рендеринга
    const offlineCtx = new OfflineAudioContext(
        2, // Stereo
        audioBuffer.length,
        audioBuffer.sampleRate
    );
    
    // 3. Создание источника
    const source = offlineCtx.createBufferSource();
    source.buffer = audioBuffer;
    
    // 4. Создание цепочки EQ фильтров
    let lastNode = source;
    
    for (let i = 0; i < EQ_FREQUENCIES.length; i++) {
        const filter = offlineCtx.createBiquadFilter();
        
        // Настройка типа фильтра
        if (i === 0) {
            filter.type = 'lowshelf';
        } else if (i === EQ_FREQUENCIES.length - 1) {
            filter.type = 'highshelf';
        } else {
            filter.type = 'peaking';
        }
        
        // Установка параметров
        filter.frequency.setValueAtTime(EQ_FREQUENCIES[i], 0);
        filter.gain.setValueAtTime(gains[i] || 0, 0);
        filter.Q.value = 0.5;
        
        // Подключение в цепочку
        lastNode.connect(filter);
        lastNode = filter;
    }
    
    // 5. Подключение к destination
    lastNode.connect(offlineCtx.destination);
    
    // 6. Запуск и рендеринг
    source.start(0);
    const renderedBuffer = await offlineCtx.startRendering();
    
    // 7. Конвертация в WAV
    const wavBlob = bufferToWav(renderedBuffer);
    const processedDataUrl = await blobToDataUrl(wavBlob);
    
    return { success: true, processedDataUrl, duration: audioBuffer.duration };
}
```

### Встроенные пресеты

| Пресет | Описание | Частотный профиль |
|--------|----------|-------------------|
| **Flat** | Без изменений | 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 |
| **Bass Boost** | Усиление басов | +12, +9, +6, +3, 0, 0, 0, 0, 0, 0 |
| **Bass Cut** | Ослабление басов | -12, -9, -6, -3, 0, 0, 0, 0, 0, 0 |
| **Treble Boost** | Усиление высоких | 0, 0, 0, 0, 0, +3, +6, +9, +12, +12 |
| **Electronic** | Для электроники | +8, +7, +3, 0, -3, 0, +2, +6, +8, +8 |
| **Vocal Booster** | Усиление вокала | -3, -3, -3, +2, +6, +6, +4, +2, 0, -2 |
| **Pop** | Поп-музыка | +4, +3, 0, -3, -3, -1, +2, +4, +5, +5 |
| **Rock** | Рок | +7, +5, +2, 0, -2, 0, +3, +6, +8, +8 |
| **Jazz** | Джаз | +4, +3, +1, +3, -3, -3, 0, +2, +4, +5 |
| **Classical** | Классика | +5, +4, +3, +2, -2, -2, 0, +3, +4, +5 |
| **Hip-Hop** | Хип-хоп | +10, +8, +3, 0, -2, -1, +2, +3, +4, +4 |
| **Acoustic** | Акустика | +5, +4, +2, +1, 0, 0, 0, +2, +3, +4 |

---

## 🌀 Как работает 8D эффект

### Что такое 8D аудио?

**8D аудио** — это психоакустический эффект, создающий иллюзию, что звук **вращается вокруг головы** слушателя в 3D пространстве.

На самом деле это **стерео эффект**, использующий:
- **Панорамирование** (перемещение звука между левым и правым каналами)
- **Изменение громкости** (имитация удаления/приближения)

### Принцип работы

```
Звук воспринимается как "вращающийся":

     👤 (слушатель)
      |
  L ←─┴─→ R
      
Время 0s:    звук справа (R=100%, L=0%)
Время 2.5s:  звук сзади  (R=50%, L=50%, тише)
Время 5s:    звук слева  (R=0%, L=100%)
Время 7.5s:  звук спереди (R=50%, L=50%, громче)
Время 10s:   звук справа (цикл повторяется)
```

### Математика эффекта

```javascript
const ROTATION_SPEED_SECONDS = 10.0;  // Полный оборот за 10 секунд
const pointsPerSecond = 50;           // 50 точек анимации в секунду

for (let i = 0; i < totalPoints; i++) {
    const time = i / pointsPerSecond;
    
    // Угол поворота (0 до 2π за 10 секунд)
    const angle = (time % ROTATION_SPEED_SECONDS) / ROTATION_SPEED_SECONDS * 2 * Math.PI;
    
    // Панорама: -1 (левый канал) до 1 (правый канал)
    const pan = Math.sin(angle);
    
    // Глубина/громкость: 0.9 (сзади) до 1.0 (спереди)
    const depth = 0.9 + 0.1 * Math.cos(angle);
    
    panner.pan.setValueAtTime(pan, time);
    gainNode.gain.setValueAtTime(depth, time);
}
```

### Визуализация эффекта

```
График панорамы (Pan) во времени:

 +1 (Right) │     ╱╲       ╱╲
            │    ╱  ╲     ╱  ╲
          0 │───────────────────
            │  ╱      ╲ ╱      ╲
 -1 (Left)  │ ╱        ╲        ╲
            └─────────────────────→ Время
            0s  2.5s  5s  7.5s  10s

График громкости (Gain) во времени:

 1.0 │ ╲       ╱ ╲       ╱
     │  ╲     ╱   ╲     ╱
 0.9 │   ╲   ╱     ╲   ╱
     │    ╲_╱       ╲_╱
     └─────────────────────→ Время
     0s  2.5s  5s  7.5s  10s
```

### Код реализации

```javascript
// Применение 8D эффекта
if (enable8D) {
    const duration = audioBuffer.duration;
    const pointsPerSecond = 50;
    const totalPoints = Math.ceil(duration * pointsPerSecond);
    
    const panCurve = new Float32Array(totalPoints);
    const gainCurve = new Float32Array(totalPoints);
    
    for (let i = 0; i < totalPoints; i++) {
        const time = i / pointsPerSecond;
        const angle = (time % ROTATION_SPEED_SECONDS) / ROTATION_SPEED_SECONDS * 2 * Math.PI;
        
        // Панорама: синусоида (-1 до +1)
        panCurve[i] = Math.sin(angle);
        
        // Глубина: косинусоида с офсетом (0.9 до 1.0)
        const depth = 0.9 + 0.1 * Math.cos(angle);
        gainCurve[i] = depth;
    }
    
    // Применение кривых автоматизации
    panner.pan.setValueCurveAtTime(panCurve, 0, duration);
    gainNode.gain.setValueCurveAtTime(gainCurve, 0, duration);
}
```

### Почему это работает?

Человеческий мозг определяет направление звука по:

1. **ITD (Interaural Time Difference)** — разница времени прихода звука в левое/правое ухо
2. **ILD (Interaural Level Difference)** — разница громкости между ушами
3. **HRTF (Head-Related Transfer Function)** — изменение частот из-за формы головы и ушей

8D эффект **имитирует ILD** через панорамирование и изменение громкости, создавая иллюзию движения источника звука.

---

## 💾 База данных

### Архитектура хранения

Проект использует **двухуровневое хранение**:

1. **IndexedDB (Browser)** — клиентское хранилище
2. **PostgreSQL** — серверное хранилище (опционально)

### IndexedDB (Локальное хранилище)

#### Структура

```javascript
Database: SonicWaveDB (version 2)

ObjectStores:
├── users
│   ├── keyPath: "email"
│   └── indexes: ["id"]
│
├── tracks
│   ├── keyPath: "id"
│   └── indexes: ["userId"]
│
└── files (NEW in v2 - Blob оптимизация)
    └── keyPath: "trackId"
```

#### Оптимизация хранения файлов

**Проблема**: Base64 увеличивает размер файлов на 33%

**Решение**: Хранение аудио как Blob

```javascript
// Старый метод (до v2):
track = {
    id: "abc123",
    fileDataUrl: "data:audio/mp3;base64,AAAABBBBCCCC..." // Огромная строка!
}

// Новый метод (v2):
// Таблица tracks - только метаданные
track = {
    id: "abc123",
    title: "My Song",
    duration: 180,
    fileSize: 5242880
}

// Таблица files - Blob данные отдельно
file = {
    trackId: "abc123",
    originalBlob: Blob,  // Нативный бинарный формат
    processedBlob: Blob
}
```

**Результат**:
- ✅ Экономия места: 33%
- ✅ Скорость загрузки: в 3-5 раз быстрее
- ✅ Меньше зависаний браузера

#### Процесс сохранения трека

```javascript
// storage.js
export async function saveTrack(trackJson) {
    const track = JSON.parse(trackJson);
    
    // 1. Конвертируем Base64 в Blob
    const originalBlob = dataUrlToBlob(track.fileDataUrl);
    const processedBlob = dataUrlToBlob(track.processedDataUrl);
    
    // 2. Сохраняем файлы в store 'files'
    await filesStore.put({
        trackId: track.id,
        originalBlob: originalBlob,
        processedBlob: processedBlob
    });
    
    // 3. Сохраняем метаданные в store 'tracks' (БЕЗ Base64!)
    await tracksStore.put({
        id: track.id,
        title: track.title,
        artist: track.artist,
        duration: track.duration,
        createdAt: new Date(),
        userId: track.userId,
        // Важно: Удаляем тяжелые данные из мета-объекта
        fileDataUrl: null, 
        processedDataUrl: null 
    });
}
PostgreSQL (Серверная БД)
Если запущен Docker-контейнер, используется реляционная база данных для синхронизации метаданных.
Схема базы данных (ERD):
code
SQL
  users
  ├── id (UUID, PK)
  ├── email (VARCHAR)
  ├── password_hash (VARCHAR)
  └── created_at (TIMESTAMP)
  
  tracks
  ├── id (UUID, PK)
  ├── user_id (UUID, FK -> users.id)
  ├── title (VARCHAR)
  ├── file_name (VARCHAR)
  ├── file_size (BIGINT)
  ├── duration (DOUBLE)
  ├── settings_json (TEXT) -- настройки EQ и 8D для этого трека
  └── created_at (TIMESTAMP)

  custom_presets
  ├── id (UUID, PK)
  ├── user_id (UUID, FK -> users.id)
  ├── name (VARCHAR)
  ├── gains (JSONB) -- массив из 10 значений
  └── is_public (BOOLEAN)

  playlists
  ├── id (UUID, PK)
  ├── user_id (UUID, FK -> users.id)
  └── name (VARCHAR)
  
Примечание: Аудиофайлы на сервере обычно хранятся в S3-совместимом хранилище или локальной папке, в БД хранятся только пути к ним. В текущей версии SonicWave 8D основной упор сделан на клиентское хранение аудио для снижения нагрузки на сервер.
  
🔌 API Endpoints
Если запущен Backend API, доступны следующие эндпоинты (Swagger UI доступен по адресу /swagger):
🔐 Аутентификация
Метод	URL	Описание
  POST	/api/auth/register	Регистрация нового пользователя
  POST	/api/auth/login	Вход и получение JWT токена
  POST	/api/auth/refresh	Обновление токена
🎵 Треки
  Метод	URL	Описание
  GET	/api/tracks	Получить список всех треков пользователя
  GET	/api/tracks/{id}	Получить метаданные трека
  POST	/api/tracks	Загрузить новый трек (Metadata + File)
  PUT	/api/tracks/{id}	Обновить настройки трека (EQ, 8D)
  DELETE	/api/tracks/{id}	Удалить трек
🎚️ Пресеты
  Метод	URL	Описание
  GET	/api/presets	Получить системные + пользовательские пресеты
  POST	/api/presets	Создать новый кастомный пресет
  DELETE	/api/presets/{id}	Удалить кастомный пресет
📦 Хранение данных (Итог)
  Приложение реализует гибридный подход к хранению данных (Offline-First):
  При загрузке страницы:
  Приложение проверяет наличие токена.
  Если есть интернет — загружает список треков с API.
  Параллельно читает IndexedDB для доступа к локальным файлам.
  При загрузке файла:
  Файл сохраняется в IndexedDB (Blob) мгновенно.
  Метаданные отправляются на сервер (если пользователь залогинен).
  При отсутствии интернета:
  Приложение полностью функционально.
  Используются данные из IndexedDB.
  При появлении сети происходит синхронизация (в планах v2.0).
