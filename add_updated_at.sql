-- Adaugă UpdatedAt la GroupMessages dacă nu există
-- Notă: SQLite va da eroare dacă coloana există deja, dar vom ignora
ALTER TABLE GroupMessages ADD COLUMN UpdatedAt TEXT;

