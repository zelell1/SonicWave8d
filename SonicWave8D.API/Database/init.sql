-- ============================================
-- SonicWave 8D Database Initialization Script
-- PostgreSQL 15+
-- ============================================

-- Создание базы данных (выполнить отдельно если нужно)
-- CREATE DATABASE sonicwave8d;

-- Подключение к базе данных
-- \c sonicwave8d;

-- ============================================
-- EXTENSIONS
-- ============================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- Для полнотекстового поиска

-- ============================================
-- TYPES (ENUMS)
-- ============================================
DO $$ BEGIN
    CREATE TYPE track_status AS ENUM ('Pending', 'Processing', 'Completed', 'Error');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- ============================================
-- TABLES
-- ============================================

-- Таблица пользователей
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    avatar_url VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE,

    CONSTRAINT users_email_check CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

-- Индексы для users
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON users(created_at);

-- Таблица кастомных пресетов эквалайзера
CREATE TABLE IF NOT EXISTS custom_presets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    gains JSONB NOT NULL DEFAULT '[0,0,0,0,0,0,0,0,0,0]',
    is_public BOOLEAN DEFAULT FALSE,
    is_system BOOLEAN DEFAULT FALSE,
    is_favorite BOOLEAN DEFAULT FALSE,
    usage_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT custom_presets_gains_check CHECK (jsonb_array_length(gains) = 10)
);

-- Индексы для custom_presets
CREATE INDEX IF NOT EXISTS idx_custom_presets_user_id ON custom_presets(user_id);
CREATE INDEX IF NOT EXISTS idx_custom_presets_is_public ON custom_presets(is_public);
CREATE INDEX IF NOT EXISTS idx_custom_presets_is_system ON custom_presets(is_system);
CREATE INDEX IF NOT EXISTS idx_custom_presets_name ON custom_presets(name);
CREATE INDEX IF NOT EXISTS idx_custom_presets_usage_count ON custom_presets(usage_count DESC);

-- Таблица треков
CREATE TABLE IF NOT EXISTS tracks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    artist VARCHAR(255),
    album VARCHAR(255),
    genre VARCHAR(50),
    duration DOUBLE PRECISION DEFAULT 0,
    original_file_path VARCHAR(500) NOT NULL,
    processed_file_path VARCHAR(500),
    file_type VARCHAR(50) DEFAULT 'audio/mpeg',
    file_size BIGINT DEFAULT 0,
    cover_image_url VARCHAR(500),
    equalizer_settings JSONB,
    preset_id UUID REFERENCES custom_presets(id) ON DELETE SET NULL,
    is_8d_enabled BOOLEAN DEFAULT TRUE,
    status track_status DEFAULT 'Pending',
    play_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_played_at TIMESTAMP WITH TIME ZONE
);

-- Индексы для tracks
CREATE INDEX IF NOT EXISTS idx_tracks_user_id ON tracks(user_id);
CREATE INDEX IF NOT EXISTS idx_tracks_title ON tracks(title);
CREATE INDEX IF NOT EXISTS idx_tracks_artist ON tracks(artist);
CREATE INDEX IF NOT EXISTS idx_tracks_album ON tracks(album);
CREATE INDEX IF NOT EXISTS idx_tracks_genre ON tracks(genre);
CREATE INDEX IF NOT EXISTS idx_tracks_status ON tracks(status);
CREATE INDEX IF NOT EXISTS idx_tracks_created_at ON tracks(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_tracks_play_count ON tracks(play_count DESC);
CREATE INDEX IF NOT EXISTS idx_tracks_preset_id ON tracks(preset_id);

-- Полнотекстовый поиск для треков
CREATE INDEX IF NOT EXISTS idx_tracks_title_trgm ON tracks USING gin(title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_tracks_artist_trgm ON tracks USING gin(artist gin_trgm_ops);

-- Таблица плейлистов
CREATE TABLE IF NOT EXISTS playlists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    cover_image_url VARCHAR(500),
    is_public BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Индексы для playlists
CREATE INDEX IF NOT EXISTS idx_playlists_user_id ON playlists(user_id);
CREATE INDEX IF NOT EXISTS idx_playlists_is_public ON playlists(is_public);
CREATE INDEX IF NOT EXISTS idx_playlists_created_at ON playlists(created_at DESC);

-- Связь плейлистов и треков (many-to-many)
CREATE TABLE IF NOT EXISTS playlist_tracks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    playlist_id UUID NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
    track_id UUID NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
    "order" INTEGER DEFAULT 0,
    added_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT playlist_tracks_unique UNIQUE (playlist_id, track_id)
);

-- Индексы для playlist_tracks
CREATE INDEX IF NOT EXISTS idx_playlist_tracks_playlist_id ON playlist_tracks(playlist_id);
CREATE INDEX IF NOT EXISTS idx_playlist_tracks_track_id ON playlist_tracks(track_id);
CREATE INDEX IF NOT EXISTS idx_playlist_tracks_order ON playlist_tracks("order");

-- Избранные треки пользователя
CREATE TABLE IF NOT EXISTS favorite_tracks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    track_id UUID NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
    added_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT favorite_tracks_unique UNIQUE (user_id, track_id)
);

-- Индексы для favorite_tracks
CREATE INDEX IF NOT EXISTS idx_favorite_tracks_user_id ON favorite_tracks(user_id);
CREATE INDEX IF NOT EXISTS idx_favorite_tracks_track_id ON favorite_tracks(track_id);
CREATE INDEX IF NOT EXISTS idx_favorite_tracks_added_at ON favorite_tracks(added_at DESC);

-- ============================================
-- TRIGGERS
-- ============================================

-- Функция обновления updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Триггеры для автоматического обновления updated_at
DROP TRIGGER IF EXISTS update_tracks_updated_at ON tracks;
CREATE TRIGGER update_tracks_updated_at
    BEFORE UPDATE ON tracks
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_custom_presets_updated_at ON custom_presets;
CREATE TRIGGER update_custom_presets_updated_at
    BEFORE UPDATE ON custom_presets
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_playlists_updated_at ON playlists;
CREATE TRIGGER update_playlists_updated_at
    BEFORE UPDATE ON playlists
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- SEED DATA - System User & Default Presets
-- ============================================

-- Системный пользователь для системных пресетов
INSERT INTO users (id, email, username, password_hash, is_active, created_at)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'system@sonicwave8d.local',
    'System',
    'SYSTEM_USER_NO_LOGIN',
    FALSE,
    '2024-01-01 00:00:00+00'
) ON CONFLICT (id) DO NOTHING;

-- Системные пресеты эквалайзера
INSERT INTO custom_presets (id, user_id, name, description, gains, is_public, is_system, created_at, updated_at)
VALUES
    ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001',
     'Flat (Default)', 'Ровная АЧХ без изменений',
     '[0,0,0,0,0,0,0,0,0,0]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001',
     'Bass Boost', 'Усиление низких частот',
     '[12,9,6,3,0,0,0,0,0,0]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001',
     'Bass Cut', 'Ослабление низких частот',
     '[-12,-9,-6,-3,0,0,0,0,0,0]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001',
     'Treble Boost', 'Усиление высоких частот',
     '[0,0,0,0,0,3,6,9,12,12]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000001',
     'Electronic', 'Для электронной музыки',
     '[8,7,3,0,-3,0,2,6,8,8]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000006', '00000000-0000-0000-0000-000000000001',
     'Vocal Booster', 'Усиление вокала',
     '[-3,-3,-3,2,6,6,4,2,0,-2]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000001',
     'Pop', 'Для поп-музыки',
     '[4,3,0,-3,-3,-1,2,4,5,5]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000008', '00000000-0000-0000-0000-000000000001',
     'Rock', 'Для рок-музыки',
     '[7,5,2,0,-2,0,3,6,8,8]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000009', '00000000-0000-0000-0000-000000000001',
     'Jazz', 'Для джаза',
     '[4,3,1,3,-3,-3,0,2,4,5]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000001',
     'Classical', 'Для классической музыки',
     '[5,4,3,2,-2,-2,0,3,4,5]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000011', '00000000-0000-0000-0000-000000000001',
     'Hip-Hop', 'Для хип-хопа и рэпа',
     '[10,8,3,0,-2,-1,2,3,4,4]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00'),

    ('10000000-0000-0000-0000-000000000012', '00000000-0000-0000-0000-000000000001',
     'Acoustic', 'Для акустической музыки',
     '[5,4,2,1,0,0,0,2,3,4]', TRUE, TRUE, '2024-01-01 00:00:00+00', '2024-01-01 00:00:00+00')
ON CONFLICT (id) DO NOTHING;

-- ============================================
-- VIEWS
-- ============================================

-- Представление для статистики пользователя
CREATE OR REPLACE VIEW user_stats AS
SELECT
    u.id AS user_id,
    u.username,
    COUNT(DISTINCT t.id) AS total_tracks,
    COUNT(DISTINCT p.id) AS total_playlists,
    COUNT(DISTINCT cp.id) AS total_presets,
    COUNT(DISTINCT ft.id) AS total_favorites,
    COALESCE(SUM(t.play_count), 0) AS total_plays,
    COALESCE(SUM(t.duration), 0) AS total_duration_seconds
FROM users u
LEFT JOIN tracks t ON u.id = t.user_id
LEFT JOIN playlists p ON u.id = p.user_id
LEFT JOIN custom_presets cp ON u.id = cp.user_id AND NOT cp.is_system
LEFT JOIN favorite_tracks ft ON u.id = ft.user_id
WHERE u.is_active = TRUE
GROUP BY u.id, u.username;

-- Представление для популярных пресетов
CREATE OR REPLACE VIEW popular_presets AS
SELECT
    cp.id,
    cp.name,
    cp.description,
    cp.gains,
    cp.is_system,
    cp.usage_count,
    u.username AS creator_name
FROM custom_presets cp
JOIN users u ON cp.user_id = u.id
WHERE cp.is_public = TRUE OR cp.is_system = TRUE
ORDER BY cp.usage_count DESC, cp.name;

-- ============================================
-- FUNCTIONS
-- ============================================

-- Функция поиска треков
CREATE OR REPLACE FUNCTION search_tracks(
    p_user_id UUID,
    p_query TEXT,
    p_limit INTEGER DEFAULT 20,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    id UUID,
    title VARCHAR,
    artist VARCHAR,
    album VARCHAR,
    duration DOUBLE PRECISION,
    cover_image_url VARCHAR,
    play_count INTEGER,
    relevance REAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        t.id,
        t.title,
        t.artist,
        t.album,
        t.duration,
        t.cover_image_url,
        t.play_count,
        (
            similarity(t.title, p_query) * 2 +
            COALESCE(similarity(t.artist, p_query), 0) +
            COALESCE(similarity(t.album, p_query), 0)
        )::REAL AS relevance
    FROM tracks t
    WHERE t.user_id = p_user_id
    AND (
        t.title ILIKE '%' || p_query || '%'
        OR t.artist ILIKE '%' || p_query || '%'
        OR t.album ILIKE '%' || p_query || '%'
    )
    ORDER BY relevance DESC, t.play_count DESC
    LIMIT p_limit
    OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- GRANTS (для production)
-- ============================================
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO sonicwave_app;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO sonicwave_app;

-- ============================================
-- Готово!
-- ============================================
SELECT 'SonicWave 8D Database initialized successfully!' AS status;
