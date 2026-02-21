# BTG Ledger - Distributed Banking Core

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-18.x-61DAFB?logo=react&logoColor=black)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Message_Broker-FF6600?logo=rabbitmq&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Container-2496ED?logo=docker&logoColor=white)
![Kubernetes](https://img.shields.io/badge/Kubernetes-Orchestration-326CE5?logo=kubernetes&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white)

Implementação full-stack de um núcleo de processamento financeiro (Ledger) baseado em arquitetura orientada a eventos (EDA - Event-Driven Architecture). O sistema é projetado para alta concorrência e resiliência, utilizando mensageria assíncrona para dissociar fluxos críticos de operações secundárias.

## 🏗️ System Architecture

O backend foi desenvolvido seguindo as premissas de **Clean Architecture** e **Domain-Driven Design (DDD)**, garantindo o isolamento das regras de negócio (Core Domain) de frameworks e detalhes de infraestrutura.

### Project Structure
```text
src/
 ├── BtgLedger.Domain/          # Entidades, Agregados, Value Objects e Interfaces (Core)
 ├── BtgLedger.Infrastructure/  # EF Core, Repositórios, RabbitMQ Publisher
 ├── BtgLedger.API/             # ASP.NET Core REST API, Controllers, JWT Auth
 ├── BtgLedger.Worker/          # Background Service (RabbitMQ Consumer)
tests/
 └── BtgLedger.Tests/           # Testes Unitários e de Integração
