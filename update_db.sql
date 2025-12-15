-- Script de actualizare bază de date pentru funcționalitatea Grupuri

-- Verifică dacă tabelul GroupMessages există
SELECT name FROM sqlite_master WHERE type='table' AND name='GroupMessages';

-- Adaugă coloana UpdatedAt la GroupMessages (va da eroare dacă există deja, ignore)
-- În SQLite, nu putem face IF NOT EXISTS pentru ADD COLUMN, deci trebuie rulat manual
-- ALTER TABLE GroupMessages ADD COLUMN UpdatedAt TEXT NULL;

-- Verifică structura finală
PRAGMA table_info(GroupMessages);
PRAGMA table_info(Groups);
PRAGMA table_info(GroupMembers);

