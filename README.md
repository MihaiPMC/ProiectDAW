# ProiectDAW

ProiectDAW is a social networking application built with ASP.NET Core MVC. It allows users to connect, join groups, share news, and manage their profiles.

## Features

- **User Accounts**: Secure registration and login system powered by ASP.NET Core Identity.
- **Roles**: Role-based access control including Administrator privileges.
- **Social Interactions**: Follow other users to see their updates.
- **Groups**: Create and join groups to connect with people sharing similar interests.
- **News Feed**: Post, view, and manage news articles.
- **User Profiles**: Personalized user profiles with customizable details.

## Technologies Used

- **Framework**: ASP.NET Core 9.0 (MVC)
- **Database**: SQLite
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Razor Views, CSS

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ProiectDAW
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   cd ProiectDAW
   dotnet run
   ```
   The application uses SQLite, and the database will be automatically created and seeded with initial data upon the first run.

## Usage

Once the application is running, navigate to `https://localhost:7198` (or the port indicated in your console) to start exploring the features.
