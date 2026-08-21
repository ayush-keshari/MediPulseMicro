# MediPulseMicro - Healthcare Supply Chain Microservices

[![CI/CD Pipeline](https://github.com/ayush-keshari/MediPulseMicro/actions/workflows/ci.yml/badge.svg?branch=dev2)](https://github.com/ayush-keshari/MediPulseMicro/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-blue)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-18+-red)](https://angular.io/)

A modern **microservices-based healthcare supply chain management system** built with .NET 10 and Angular. Designed to manage medical inventory, procurement, logistics, and cold chain monitoring across hospital networks.

---

## 🏗️ Architecture Overview

MediPulseMicro decomposes hospital supply chain operations into **11 independent microservices**, each with its own database and clear responsibilities:

### Core Services

| Service | Purpose | Tech Stack |
|---------|---------|-----------|
| **AuthService** | JWT authentication & user management | .NET 10, BCrypt, EF Core |
| **FacilityService** | Hospital/clinic & storage zone management | .NET 10, EF Core |
| **InventoryService** | Item inventory, lot tracking, exceptions, forecasting | .NET 10, EF Core |
| **ProcurementService** | Supplier management, purchase orders, GRN receipts | .NET 10, EF Core |
| **LogisticsService** | Inter-facility transfers, consumption tracking | .NET 10, EF Core |
| **TelemetryService** | IoT sensor data, cold chain monitoring | .NET 10, EF Core |
| **NotificationService** | User notifications (exceptions, expiry, receipts) | .NET 10, EF Core |
| **AuditService** | Complete audit trail (separate database) | .NET 10, EF Core |
| **Gateway** | API Gateway (routing, rate limiting) | .NET 10 |
| **Shared** | Common DTOs, models, utilities | .NET 10 |
| **Frontend** | SPA for dashboards & operations | Angular 18+, TypeScript |

### Database Architecture

- **MedipulseMain**: Primary database for Facility, Inventory, Procurement, Logistics, Telemetry, Notifications
- **MedipulseAudit**: Isolated audit database for compliance records

---

## 🔐 Authentication Flow

1. User logs in via `POST /api/auth/login`
2. AuthService validates credentials (BCrypt hashing)
3. JWT token issued with 24-hour expiry
4. Token sent in request: `Authorization: Bearer {token}`
5. Gateway validates JWT and routes to appropriate service

---

## 📋 Prerequisites

Before you begin, ensure you have:

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **SQL Server 2019+** or **SQL Server 2022** - [Download](https://www.microsoft.com/en-us/sql-server/sql-server-2022)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **npm 9+** - (Included with Node.js)
- **Git** - [Download](https://git-scm.com/)
- **Docker & Docker Compose** - [Download](https://www.docker.com/get-started)
- **Visual Studio 2022+** or **VS Code**

### Verify Installation
dotnet --version         # Should be 10.0.x
node --version           # Should be v18+
npm --version            # Should be 9+
git --version            # Should be 2.40+
docker --version         # Docker version
docker-compose --version # Docker Compose version

---

## 🚀 Quick Start

### Option 1: Docker Compose (Recommended - Self-contained)
1. Clone Repository
   ```bash
   git clone https://github.com/ayush-keshari/MediPulseMicro.git
   cd MediPulseMicro
   git checkout dev2
   ```

2. Setup Environment (Optional but recommended)
   ```bash
   cp .env.example .env
   # Edit .env if you want to customize values
   ```

3. Start All Services
   ```bash
   docker-compose up -d
   ```

4. Wait for Initialization
   - The migrator service will run EF Core migrations automatically
   - This may take 1-2 minutes on first run
   - Check logs with: `docker-compose logs -f migrator`

5. Access the Application
   - Gateway: http://localhost:5000
   - Frontend: http://localhost:80
   - API Documentation: Available through individual service endpoints

### Option 2: Manual Setup (For Development)
1. Follow steps 1-2 above
2. Setup Databases using SSMS or .NET Migrations
3. Update appsettings.json in each service with your connection strings
4. Run each service individually as shown in the Running Services section below

---

## 🏃 Running Services

### Backend Services (Docker Compose - Recommended)
All backend services are orchestrated via Docker Compose:
```bash
docker-compose up -d
```

To view logs:
```bash
docker-compose logs -f
```

To stop services:
```bash
docker-compose down
```

### Backend Services (Manual - For Development)
Start each service in a separate terminal:
Terminal 1 - AuthService (Port 5001)
cd AuthService
dotnet run

Terminal 2 - FacilityService (Port 5002)
cd FacilityService
dotnet run

Terminal 3 - InventoryService (Port 5003)
cd InventoryService
dotnet run

Terminal 4 - ProcurementService (Port 5004)
cd ProcurementService
dotnet run

Terminal 5 - LogisticsService (Port 5005)
cd LogisticsService
dotnet run

Terminal 6 - TelemetryService (Port 5006)
cd TelemetryService
dotnet run

Terminal 7 - NotificationService (Port 5007)
cd NotificationService
dotnet run

Terminal 8 - AuditService (Port 5008)
cd AuditService
dotnet run

Terminal 9 - Gateway (Port 5000)
cd Gateway
dotnet run

### Frontend
cd Frontend
npm install
npm start
Access the application at: **http://localhost:4200**

---

## 🧪 Testing

### Run All Tests
dotnet test

### Run Tests by Service
dotnet test AuthService.Tests
dotnet test FacilityService.Tests
dotnet test InventoryService.Tests
dotnet test ProcurementService.Tests
dotnet test LogisticsService.Tests
dotnet test TelemetryService.Tests
dotnet test NotificationService.Tests

### Test Coverage

- **31 tests total** across all services
- Model validation tests
- Database context tests
- Service integration tests

---

## 📊 API Endpoints

### Authentication
POST   /api/auth/register      - Register new user
POST   /api/auth/login         - Login (returns JWT)
GET    /api/auth/profile       - Get current user profile

### Facility Management
GET    /api/facilities         - List all facilities
POST   /api/facilities         - Create facility
GET    /api/facilities/{id}    - Get facility details
PUT    /api/facilities/{id}    - Update facility
DELETE /api/facilities/{id}    - Delete facility
GET    /api/storage-zones      - List storage zones
POST   /api/storage-zones      - Create storage zone

### Inventory Management
GET    /api/items              - List all items
POST   /api/items              - Create item
GET    /api/items/{id}         - Get item details
GET    /api/inventory-positions - View inventory by lot
GET    /api/exceptions         - List open exceptions
POST   /api/recall-actions     - Create recall action
GET    /api/forecasts          - Demand forecasts
GET    /api/replenishment-plans - Replenishment suggestions

### Procurement
GET    /api/suppliers          - List suppliers
POST   /api/suppliers          - Create supplier
GET    /api/purchase-orders    - List POs
POST   /api/purchase-orders    - Create PO
POST   /api/receipts           - Create receipt (GRN)

### Logistics
GET    /api/transfer-orders    - List transfers
POST   /api/transfer-orders    - Create transfer
POST   /api/consumption        - Log consumption

### Telemetry
GET    /api/sensors            - List IoT sensors
POST   /api/telemetry          - Ingest sensor readings
GET    /api/excursions         - View temperature breaches

### Notifications
GET    /api/notifications      - List user notifications
PATCH  /api/notifications/{id}/read - Mark as read

---

## 🔒 Security Best Practices

✅ **Implemented:**
- Passwords hashed with **BCrypt** (minimum 11 rounds)
- JWT tokens with **24-hour expiry**
- CORS configured per service
- SQL injection prevention via EF Core parameterized queries
- Audit logging on all data modifications
- Sensitive config stored in appsettings.json (not in git)
- Authority validation on protected endpoints

⚠️ **Security Checklist:**
- [ ] Change JWT secret key in all services
- [ ] Use strong database passwords
- [ ] Enable HTTPS in production
- [ ] Configure CORS for your domain
- [ ] Regular dependency updates via Dependabot
- [ ] Monitor audit logs regularly

---

## 🐳 Docker (Optional)

### Build and Run with Docker Compose
docker-compose up -d
Services will be available at:
- Gateway: http://localhost:5000
- Frontend: http://localhost:80
- SQL Server: localhost:1433

---

## 📁 Project Structure

MediPulseMicro/
├── .github/
│   └── workflows/
│       ├── ci.yml              # GitHub Actions CI/CD
│       └── dependabot.yml      # Automated dependency updates
├── AuthService/
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Data/
│   └── Program.cs
├── FacilityService/
├── InventoryService/
├── ProcurementService/
├── LogisticsService/
├── TelemetryService/
├── NotificationService/
├── AuditService/
├── Gateway/
├── Shared/                      # Common models & DTOs
├── Frontend/                    # Angular SPA
├── AuthService.Tests/
├── FacilityService.Tests/
└── [other test projects]

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

### 1. Create Feature Branch
git checkout dev2
git pull origin dev2
git checkout -b feature/your-feature-name

### 2. Make Changes & Write Tests
Make your changes
Write tests for new functionality
dotnet test

### 3. Commit with Clear Messages
git commit -m "feat: add new feature
•	Detailed description of what changed
•	Why this change was needed
•	Any related issues or PRs"

### 4. Push & Create Pull Request
git push origin feature/your-feature-name
Then create a Pull Request on GitHub targeting the `dev2` branch.

### Commit Message Convention
feat:        Add new feature
fix:         Bug fix
docs:        Documentation changes
test:        Test additions/updates
chore:       Build/config/tooling changes
ci:          CI/CD pipeline changes
refactor:    Code refactoring without behavior change
perf:        Performance improvements

---

## 🚨 Troubleshooting

### Database Connection Issues
Test SQL Server connection
sqlcmd -S localhost -U sa -P "YourPassword"
If connection fails, check:
1. SQL Server is running
2. Connection string is correct
3. Database exists

### Port Already in Use
Find and kill process using port (Windows)
netstat -ano | findstr :5001
taskkill /PID <PID> /F

### Tests Failing
Clean and rebuild
dotnet clean
dotnet restore
dotnet test --verbosity normal

### Frontend Not Loading
cd Frontend
npm cache clean --force
rm -r node_modules
package-lock.json
npm install
npm start

---

## 📞 Support & Contact

- **Issues**: [GitHub Issues](https://github.com/ayush-keshari/MediPulseMicro/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ayush-keshari/MediPulseMicro/discussions)
- **Email**: ayush-keshari@github.com

---

## 📄 License

This project is licensed under the **MIT License** - see the LICENSE file for details.

---

## 👥 Authors

- **Ayush Keshari** ([@ayush-keshari](https://github.com/ayush-keshari)) - Lead Developer

---

## 🙏 Acknowledgments

- Built with [.NET 10](https://dotnet.microsoft.com/)
- Frontend with [Angular 18+](https://angular.io/)
- Database with [SQL Server](https://www.microsoft.com/en-us/sql-server/)
- CI/CD with [GitHub Actions](https://github.com/features/actions)

---

## 📈 Project Stats

- **Services**: 11 microservices
- **Databases**: 2 (MedipulseMain + MedipulseAudit)
- **Tests**: 31+ unit tests
- **CI/CD**: Automated GitHub Actions pipeline
- **Code Quality**: TypeScript + C# with strict null checking

---

## 🎯 Roadmap

- [ ] Add Swagger/OpenAPI documentation
- [ ] Implement GraphQL endpoint
- [ ] Add WebSocket real-time notifications
- [ ] Mobile app (React Native/Flutter)
- [ ] Advanced analytics dashboard
- [ ] Machine learning inventory predictions
- [ ] Integration with hospital ERP systems
- [ ] Kubernetes deployment configs
- [ ] Performance monitoring dashboard