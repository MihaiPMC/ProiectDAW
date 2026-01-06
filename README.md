# ProiectDAW - Platformă de Știri și Socializare

ProiectDAW este o aplicație web complexă dezvoltată în **ASP.NET Core MVC (.NET 9.0)**, care combină funcționalități de publicare știri cu elemente de rețea socială și verificare a conținutului folosind Inteligența Artificială.

Aplicația permite utilizatorilor să citească și să publice articole, să interacționeze prin voturi și comentarii, să se organizeze în grupuri și să urmărească activitatea altor utilizatori, totul într-un mediu securizat.

---

## 🚀 Funcționalități Principale

### 1. Sistem de Știri și Articole
*   **Publicare**: Utilizatorii pot crea și publica articole de știri.
*   **AI Fact-Checking**: Integrare cu **OpenAI** pentru verificarea veridicității articolelor. Fiecare articol primește un scor de încredere (0-100) generat automat de AI.
*   **Sistem de Votare**: Utilizatorii pot vota articolele (Upvote/Downvote), similar cu Reddit.
*   **Comentarii**: Posibilitatea de a discuta pe baza articolelor.

### 2. Socializare & Profiluri
*   **Profiluri Utilizator**: Fiecare utilizator are un profil personalizabil.
*   **Sistem de Follow**: Utilizatorii pot urmări alți utilizatori pentru a vedea activitatea lor.
*   **Grupuri**: Crearea și gestionarea grupurilor de discuții sau interese comune. Membrii se pot alătura grupurilor și pot interacționa în cadrul acestora.

### 3. Securitate și Administrare
*   **Autentificare**: Sistem complet de înregistrare și autentificare bazat pe **ASP.NET Core Identity**.
*   **Roluri**: Gestiune bazată pe roluri (ex. Administratori, Utilizatori, Editori) pentru a controla accesul la funcționalități sensibile.
*   **Protecție Date**: Stocarea securizată a parolelor și datelor utilizatorilor.

---

## 🛠 Tehnologii Utilizate

*   **Backend**: C# / .NET 9.0, ASP.NET Core MVC
*   **Database**: SQLite (via Entity Framework Core)
*   **ORM**: Entity Framework Core
*   **Auth**: ASP.NET Core Identity
*   **AI**: Integrare OpenAI API (pentru fact-checking)
*   **Configurare**: DotNetEnv (pentru variabile de mediu)
*   **Frontend**: Razor Views, HTML5, CSS3, Bootstrap (implicit din template-uri)

---

## ⚙️ Instalare și Configurare

Urmează acești pași pentru a rula proiectul pe mașina locală.

### 1. Cerințe Preliminare
Asigură-te că ai instalat:
*   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
*   Un editor de cod (ex. Visual Studio 2022, VS Code, JetBrains Rider)

### 2. Clonare Proiect
Clonează repository-ul în folderul dorit:
```bash
git clone https://github.com/MihaiPMC/ProiectDAW.git
cd ProiectDAW/ProiectDAW
```

### 3. Configurare Variabile de Mediu (.env)
Proiectul folosește un fișier `.env` pentru a stoca chei secrete (precum cheia OpenAI).
Creează un fișier numit `.env` în rădăcina proiectului (`ProiectDAW/ProiectDAW/`) și adaugă:

```env
OPENAI_API_KEY=cheia_ta_aici
```
*Notă: Ai nevoie de o cheie validă de la OpenAI pentru ca funcția de Fact-Checking să funcționeze.*

### 4. Baza de Date
Proiectul este configurat să folosească **SQLite**. Fișierul bazei de date `app.db` ar putea fi deja creat sau va fi creat la aplicarea migrațiilor.

Dacă este prima dată când rulezi proiectul, aplică migrațiile pentru a crea baza de date:
```bash
dotnet ef database update
```

### 5. Populare Date (Seeding)
Aplicația include un mecanism de **Seeding** (`SeedData.InitializeAsync` în `Program.cs`) care va popula baza de date cu date inițiale (roluri, useri admin impliciți) la prima rulare.

### 6. Rulare Aplicație
Poți rula aplicația folosind comanda:
```bash
dotnet run
```
Sau, pentru profilul HTTPS:
```bash
dotnet run --launch-profile https
```

Accesează aplicația în browser la adresa indicată în consolă (de obicei `https://localhost:7082` sau `http://localhost:5163`).

---

## 📂 Structură Proiect

*   **Controllers/**: Logica aplicației (Actions pentru Știri, Grupuri, Profil, etc.)
*   **Models/**: Definirea entităților din baza de date (NewsArticle, ApplicationUser, Group, Vote, etc.)
*   **Views/**: Interfața utilizator (fișiere .cshtml)
*   **Data/**: Contextul bazei de date (`ApplicationDbContext`) și Migrațiile.
*   **Services/**: Servicii auxiliare (ex. `AiFactCheckService`).
*   **wwwroot/**: Fișiere statice (CSS, JS, imagini).

---

## ✨ Cum se folosește?

1.  **Înregistrează-te**: Creează un cont nou folosind butonul din meniu.
2.  **Explorează**: Vezi articolele publicate pe prima pagină.
3.  **Publică**: Dacă ai drepturi, folosește opțiunea de a adăuga un articol nou. AI-ul va analiza automat textul.
4.  **Socializează**: Intră pe profilul altor utilizatori, dă Follow sau alătură-te Grupurilor disponibile.

---
Dezvoltat în cadrul cursului DAW.
