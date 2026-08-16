<div align="center">
  <h1>PharmaLink Backend Ecosystem</h1>
  <p><strong>A Next-Generation Healthcare, Supply Chain & Logistics Ecosystem Powered by Generative AI</strong></p>
  
  [![Backend](https://img.shields.io/badge/Backend-.NET_10-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/)
  [![AI](https://img.shields.io/badge/AI-Semantic_Kernel-0078D4.svg)](https://github.com/microsoft/semantic-kernel)
</div>

<br/>

> **PharmaLink** is an expansive, enterprise-grade digital healthcare ecosystem powered by Generative AI. It bridges the gap between patients, pharmacies, delivery fleets, review teams, and B2B suppliers through advanced logistics and semantic intelligence.

🔗 **Frontend Repository:** [PharmaLink-Front-End](https://github.com/Omar-Nabil2/PharmaLink-Front-End)

---

## 📖 Introduction

Developed as an ambitious graduation project at the Information Technology Institute (ITI), PharmaLink far exceeds the scope of standard e-commerce applications. It is engineered as a **full-scale digital healthcare ecosystem** that seamlessly interconnects every stakeholder in the medical supply chain.

At its core, the system is designed to solve complex supply chain challenges. When a patient's prescription requires rare or out-of-stock medications that no single pharmacy branch can fulfill, PharmaLink's custom engine dynamically queries stock across a nationwide matrix of branches. It utilizes advanced mathematical clustering and the Traveling Salesperson Problem (TSP) algorithm to intelligently split the order, guaranteeing 100% fulfillment while calculating the absolute shortest delivery route via the Open Source Routing Machine (OSRM).

Beyond logistics, PharmaLink is heavily empowered by a **Generative AI & Machine Learning Suite**. By deeply integrating Microsoft Semantic Kernel and Google Gemini, the platform acts as an autonomous medical assistant. It features native Vision OCR capable of reading handwritten prescriptions, a clinical chatbot for real-time drug contraindication warnings, an autonomous AI auditing agent that reviews prescriptions before human validation, and AI-driven predictive modeling to forecast upcoming inventory shortages and proactively alert pharmacy managers.

---

## 🛠️ Technology Stack

<table>
  <tr>
    <td align="center" width="25%">
      <h3>🌐 Frontend & Backend</h3>
    </td>
    <td align="center" width="25%">
      <h3>🧠 AI</h3>
    </td>
    <td align="center" width="25%">
      <h3>💾 Data & Storage</h3>
    </td>
    <td align="center" width="25%">
      <h3>⚙️ Infrastructure & DevOps</h3>
    </td>
  </tr>
  <tr>
    <td align="center">
      <b>Angular</b><br><br><b>PrimeNG</b><br><br><b>.NET Core Web API</b><br><br><b>Entity Framework Core</b><br><br><b>Clean Architecture</b>
    </td>
    <td align="center">
      <b>Semantic Kernel</b><br><br><b>Gemini Embeddings</b><br><br><b>OCR + LLM</b><br><br><b>RAG</b><br><br><br>
    </td>
    <td align="center">
      <b>SQL Server</b><br><br><b>Redis</b><br><br><b>Qdrant Vector DB</b><br><br><br><br>
    </td>
    <td align="center">
      <b>Hangfire</b><br><br><b>SignalR</b><br><br><b>GitHub</b><br><br><br><br>
    </td>
  </tr>
</table>

*(Domain Specific additions)*
- **Routing & Geography:** Open Source Routing Machine (OSRM), OpenStreetMap Overpass API, NetTopologySuite

---

## 👥 System Actors & Roles (RBAC)

The system relies on a comprehensive Role-Based Access Control (RBAC) architecture, catering to 7 distinct user personas (Actors), each with specialized permissions and workflows:

1. **Patient (`Patient`):** End-users who browse the catalog, manage their carts, place complex multi-branch orders, upload AI-extracted prescriptions, receive medication reminders, and consult the medical team.
2. **Pharmacist (`Pharmacist`):** Pharmacy staff responsible for adjusting branch inventory, preparing orders, and managing fulfillment legs assigned to their specific branch.
3. **Pharmacy Admin (`PharmacyAdmin`):** Branch managers who oversee the operations of one or multiple pharmacies, monitoring stock forecasting, and managing staff workflow.
4. **System Administrator (`Admin`):** Superusers who monitor global system health through overarching dashboards, manage all users and roles, approve pharmacies, and control the global drug catalog.
5. **Prescription Review Team (`PrescriptionReviewTeam`):** A specialized medical unit that audits AI-parsed prescriptions, approves complex medication orders, and responds directly to patients' medical inquiries.
6. **Delivery Driver (`DeliveryDriver`):** Fleet personnel using the logistics module to accept delivery jobs, update live geospatial coordinates, and complete order fulfillment legs following OSRM-optimized routes.
7. **B2B Supplier (`Supplier`):** Wholesale distributors who manage bulk drug catalogs and supply pharmacy branches with restock orders.

---

## 🚀 Key Backend Modules & Architecture

The backend ecosystem is organized into highly modular features and domain controllers:

### 1. 🛡️ System Administration & RBAC
- **Admin Dashboard & Profiles (`AdminDashboardController`, `AdminProfileController`):** Centralized analytics and administrative controls.
- **User & Role Management (`AdminUsersController`):** Role-Based Access Control handling user statuses, permissions, and multi-domain access (Patients, Pharmacists, Drivers, etc.).
- **Pharmacy & Supplier Management (`AdminPharmaciesDashboardController`):** Control over pharmacy branches, their statuses, ownership assignment, and supplier catalogs.
- **Data Integrations:** Uses `ChefaaImporterService` for automated drug database scraping and `EmailService` for SMTP system broadcasts.

### 2. 🧠 Generative AI & Machine Learning Suite
This suite powers the core intelligence of the platform by deeply integrating Microsoft Semantic Kernel and Google Gemini:
- **Vision & OCR (`AIController` | `GeminiExtractionService`):** Integrates Gemini Vision OCR to extract medicine names, dosages, and instructions natively from handwritten prescriptions uploaded by the patient.
- **Clinical Assistant (`AssistantController` | `PromptExecutionService`):** An AI chatbot leveraging Semantic Kernel for real-time clinical conversations, drug-drug interaction engines, and contraindication warnings.
- **RAG & Analytics (`PrescriptionHistoryRagService` | `PrescriptionAnalyticsRagService`):** Utilizes Vector DB embeddings to provide semantic business intelligence for Admins and AI-driven recommendations based on patient prescription histories.
- **Prescription Auditing (`PrescriptionAuditAgent`):** A specialized Semantic Kernel plugin that automatically audits AI-parsed prescriptions before routing them to the human Review Team.
- **Inventory Forecasting (`InventoryController` | `InventoryForecastingService` | `InventoryForecastingCalculator`):** AI and complex mathematical prediction models to anticipate upcoming drug shortages and suggest automated reorder points for Pharmacy Admins.

### 3. 🛍️ E-Commerce, Carts & Orders
- **Cart Management (`CartController`):** Comprehensive cart operations for patients (add, update, remove items).
- **Order Processing (`AdminOrdersController` | `SupplierOrderService`):** Complex logic for ordering, splitting prescriptions across branches, approving/rejecting custom prescriptions, and B2B bulk inventory matching via `SupplierDrugService`.
- **Drug Catalog (`DrugsController`, `CategoriesController`):** Full APIs for browsing, searching, and managing the hierarchical medical catalog.

### 4. 🗺️ Multi-Pharmacy Order Splitting & Logistics
- **Inventory Matrixing (`InventoryController` | `InventoryService`):** Real-time stock querying and adjustments across all branches.
- **Agentic Routing & Delivery (`DeliveryDriversController` | `OrderFulfillmentLegService`):** Drivers can fetch active jobs, update geospatial locations, accept/complete jobs, and track live fulfillment legs. 
- **Geospatial Distance & Routing (`OsrmRoutingService` | `GeoLookupService`):** Integrates with Open Source Routing Machine (OSRM) for ETA/duration calculation, and OSM/BigDataCloud for reverse geocoding.
- **Optimization Algorithm (`GreedyOrderSplittingAlgorithm`):** Employs TSP constraint optimizations to ensure the shortest and most efficient routing for "incomplete cart" scenarios where multiple branches fulfill a single order.

### 5. 🏥 Telehealth & Patient Care
- **Medical Inquiries (`MedicalInquiriesController` | `MedicalInquiryService`):** Direct Q&A system for patients and medical review teams.
- **Medicine Reminders (`MedicineRemindersController` | `SignalRReminderPushService`):** Real-time SignalR automated push notifications to keep patients on track with their treatment schedules.
- **Communications (`WhapiWhatsAppMessageService`):** Dispatches WhatsApp OTPs and order status notifications.
- **Authentication & Security (`AuthController`):** Secure flow for JWT registration, logins, social logins (Google), refresh tokens, and password resets.

---

## 📦 Getting Started & Configuration

To run the PharmaLink Backend locally, you must configure multiple third-party services and connections.

### 1. Prerequisites
- **.NET 10 SDK**
- **SQL Server**
- **Redis Server**
- **Qdrant Vector Database** (Local or Cloud)

### 2. Clone the Repository
```bash
git clone https://github.com/YourUsername/PharmaLink-Backend.git
cd PharmaLink-Backend/src/Pharmacy/API
```

### 3. Configure `appsettings.json`
Before running the application, update `appsettings.json` (or use User Secrets / Environment Variables) with your credentials:

- **Databases:**
  - `ConnectionStrings:DefaultConnection`: Your SQL Server connection string.
  - `ConnectionStrings:Redis`: Your Redis connection string.
- **Vector DB (RAG & Semantic Search):**
  - `Qdrant:Host`, `Port`, and `ApiKey`: Your Qdrant Cloud or local instance details.
- **Generative AI Providers:**
  - `AI:Providers:Gemini:ApiKey` (or env var `GEMINI_API_KEY`).
  - `AI:Providers:OpenRouter:ApiKey` (or env var `OPENROUTER_API_KEY`).
  - `AI:Providers:ITI:ApiKey` (or env var `ITI_API_KEY`).
- **Communications:**
  - `Whapi:ApiToken`: For WhatsApp messages & OTP.
  - `EmailSettings`: Your SMTP Server (Host, Port, Email, Password).
  - `VapidDetails`: Public/Private keys for Web Push Notifications.
- **Security:**
  - `Jwt:SigningKey`: A strong secret key for JWT token generation.
  - `GoogleAuth:ClientId`: For Google Single Sign-On.

### 4. Database Migrations & Seeding
The project uses EF Core. Apply the latest migrations to your database:
```bash
dotnet ef database update
```
*Note: Upon the first run, the Integrated Seeders will automatically connect to OpenStreetMap (OSM) to fetch real Egyptian pharmacies, generate operating schedules, and bulk-insert millions of realistic medication stock entries.*

### 5. Run the Application
```bash
dotnet run
```
The API will be available on `http://localhost:5000` or `https://localhost:5001`.
