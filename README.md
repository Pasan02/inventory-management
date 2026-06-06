# Vehicle A/C Inventory Management System

A robust, enterprise-grade desktop application designed for managing vehicle air conditioning parts and inventory. Built with modern .NET technologies, it provides a comprehensive suite of features including barcode generation, label printing, stock tracking, and automated backups.

## 🚀 Features

- **Inventory Tracking**: Manage part types, brands, manufacturers, and vehicle models.
- **Stock Management**: Add and remove stock with full transaction history and checksum verification to prevent tampering.
- **Barcode Integration**: Automatically generate unique barcodes for items and print labels directly to Zebra printers.
- **Media Management**: Upload and store part images and manufacturer logos (securely stored in LocalApplicationData).
- **Automated Backups**: Built-in background service to periodically backup the PostgreSQL database.
- **Data Integrity**: Background checks to verify the integrity of stock transactions.
- **Authentication**: Secure login system with hashed passwords and audit logging.

## 🛠️ Technology Stack

- **Framework**: .NET 8.0 (WPF)
- **Architecture**: MVVM (Model-View-ViewModel) using `CommunityToolkit.Mvvm`
- **Dependency Injection**: `Microsoft.Extensions.Hosting`
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 8 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Logging**: Serilog
- **Barcode Generation**: `ZXing.Net`

## 📋 Prerequisites

Before running the application, ensure you have the following installed:
1. [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. [PostgreSQL](https://www.postgresql.org/download/) (running on `localhost` or a central server)
3. *Optional*: Zebra Printer drivers (if utilizing the barcode printing functionality)

## ⚙️ Installation & Setup

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Pasan02/inventory-management.git
   cd inventory-management/inventory-management
   ```

2. **Database Configuration**
   Open `appsettings.json` and configure your PostgreSQL connection string. The default connection string used during fallback is:
   ```text
   Host=localhost;Database=inventory_ac_db;Username=postgres;Password=pasan
   ```

3. **Database Initialization**
   The application handles database migrations and seeding automatically upon startup. When you run the application for the first time, it will:
   - Create the database if it doesn't exist.
   - Apply all Entity Framework Core migrations.
   - Seed initial placeholder data (types, brands, models).
   - Create the default administrator account.
     - **Default Username**: `admin`
     - **Default Password**: `admin123`

4. **Run the Application**
   ```bash
   dotnet run
   ```

## 📂 Project Structure

```text
inventory-management/
├── Data/                   # Entity Framework DbContext, Migrations, and Entities
├── Services/               # Business logic, Printing, Barcodes, and Background Tasks
├── ViewModels/             # MVVM ViewModels managing UI state and logic
├── Views/                  # WPF XAML files for the user interface
├── Converters/             # XAML Value Converters for UI binding
├── assets/                 # Application default assets
├── App.xaml.cs             # Application entry point & Dependency Injection setup
└── appsettings.json        # Configuration file for DB connection strings
```

## 🔒 Security & Architecture Notes

- **Dependency Injection**: ViewModels are transient but instantiated within individual `IServiceScope` instances during navigation to ensure robust memory management and prevent EF Core `DbContext` memory leaks.
- **Data Protection**: Stock transactions generate a checksum hash. The `IntegrityCheckService` runs on startup to detect if transactions were manually tampered with outside the application.
- **Asset Storage**: User-uploaded media (images, logos) are saved to `%LOCALAPPDATA%\InventoryManagement\assets` to ensure standard Windows users do not run into `Program Files` write permission issues.

## 📝 License

This project is proprietary and confidential.
