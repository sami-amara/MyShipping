
# MyShipping

**A full-stack shipping management platform with secure authentication, multi-gateway payment processing, and real-time notifications.**

## Overview

MyShipping is an enterprise-grade shipping management system designed to streamline shipment tracking, payment processing, and user management. Built with **ASP.NET Core** and **Razor Views**, the platform provides a robust backend API and responsive web interface for shipping logistics management.

### Key Capabilities

✅ **User Management** - Secure authentication with ASP.NET Identity  
✅ **Payment Integration** - Multi-gateway support (PayPal, Stripe)  
✅ **Shipment Tracking** - Real-time shipment status monitoring  
✅ **Admin Dashboard** - Comprehensive management interface  
✅ **Email Notifications** - SendGrid integration for communications  
✅ **RESTful API** - WebApi backend for third-party integrations  
✅ **Localization** - Multi-language support (Arabic, English)  
✅ **Data Persistence** - Entity Framework Core with SQL Server  

## 🏗️ Architecture
The project follows a **layered architecture** pattern for clean separation of concerns, ensuring scalability, maintainability, and testability.

## 🚀 Tech Stack

### Backend
- **Framework:** ASP.NET Core 9.0
- **Language:** C# 13
- **Database:** SQL Server / Entity Framework Core
- **Authentication:** ASP.NET Identity with JWT
- **UI Framework:** Razor Views

### Payment Processing
- **PayPal Integration** - OAuth 2.0, webhook verification
- **Stripe Integration** - Payment processing & webhooks
- **Custom Gateway Factory** - Extensible payment architecture

### Third-Party Services
- **SendGrid** - Email notifications
- **Entity Framework Core** - ORM & migrations
- **xUnit** - Unit testing framework

### Front-End
- **Razor Views** - Server-side rendering
- **Bootstrap** - Responsive UI
- **JavaScript** - Client-side functionality
- **Localization** - Resource-based translations (Arabic, English)

---

## ⚡ Getting Started

### Prerequisites
- .NET 9.0 SDK or higher
- Visual Studio 2022 
- SQL Server (LocalDB or remote instance)
- API keys for payment gateways (optional for development)

### Installation

1. **Clone the repository**

