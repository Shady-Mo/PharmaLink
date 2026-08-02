# PharmaLink Backend 💊

Welcome to the backend repository of **PharmaLink**, an innovative platform that connects patients with pharmacies, streamlining the ordering process and integrating advanced AI capabilities.

## 🏗 Architecture

The project follows the **Clean Architecture** principles to ensure separation of concerns, scalability, and maintainability.

- **`API/`**: The presentation layer containing the ASP.NET Core Web API controllers, middleware, and configuration files.
- **`Application/`**: Contains the business logic, CQRS handlers (MediatR), DTOs, and interfaces.
- **`Domain/`**: The core of the system containing entities, enums, value objects, and domain exceptions.
- **`Infrastructure/`**: Implementations for data access (Entity Framework Core), AI Execution routing, external services, and background workers.
- **`Tests/`**: Unit tests ensuring system reliability and correctness.

## ✨ Key Features

- **Robust Authentication & Authorization**: Secure JWT-based authentication for multiple roles (Patient, Pharmacist, Admin).
- **AI-Powered Pharmacist Assistant**: Integrated with Semantic Kernel, dynamically routing requests across providers (OpenRouter, Groq, Gemini) based on priorities and rate-limits to provide drug information and interaction checks.
- **Order & Inventory Management**: Full lifecycle management for pharmacy orders, inventory tracking, and notifications.
- **Prescription Audit System**: AI-driven analysis of uploaded prescriptions to detect missing details and automatically assign them for pharmacist review.

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or Docker instance)

### Configuration
1. Clone the repository:
   ```bash
   git clone https://github.com/Shady-Mo/PharmaLink.git
   ```
2. Open `API/appsettings.json` and ensure your database connection string and AI Provider API keys are configured correctly.

### Run the API
```bash
cd API
dotnet run
```
The application will launch and listen on `https://localhost:5001`. You can explore the endpoints using the provided Swagger UI.

## 🛠 Tech Stack

- **Framework**: .NET 8.0
- **Architecture**: Clean Architecture & CQRS
- **Database**: Entity Framework Core with SQL Server
- **AI Integration**: Microsoft.SemanticKernel & OpenRouter API

---
*Built with ❤️ for a better healthcare experience.*
