# 🏦 BTG Ledger - Mini Banking System

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-18.x-61DAFB?logo=react&logoColor=black)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Message_Broker-FF6600?logo=rabbitmq&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Container-2496ED?logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white)

Um sistema de Ledger bancário (livro-razão) full-stack, desenhado com foco em segurança, alta disponibilidade e processamento assíncrono. Este projeto simula o núcleo de transações de uma instituição financeira, incluindo autenticação multifator (2FA) e mensageria distribuída.

## 🎯 Visão do Produto
O objetivo deste projeto é demonstrar a implementação de uma arquitetura resiliente para serviços financeiros. O sistema garante que as transações sejam processadas com segurança através de tokens JWT e que a comunicação com serviços externos (como o envio de SMS) não bloqueie o fluxo principal do utilizador.

## 🏗️ Arquitetura e Padrões
O projeto foi estruturado seguindo os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**, garantindo que as regras de negócio bancárias estejam estritamente isoladas de detalhes de infraestrutura.

* **API Principal:** Gere as requisições HTTP, autenticação e validações.
* **Worker Service:** Um microsserviço em segundo plano responsável por consumir filas de eventos (Multi-Consumer) para envio de notificações (SMS) e processamento de recibos.
* **Message Broker:** RabbitMQ implementado para garantir o padrão de *Event-Driven Architecture*, processando tarefas pesadas de forma assíncrona.
* **Segurança:** Autenticação via JWT Token, senhas protegidas com Hash (BCrypt) e cache em memória (IMemoryCache) para gestão do ciclo de vida dos PINs de 2FA.

## ✨ Funcionalidades (Features)
1.  **Criação de Conta Bancária:** Geração automática de Agência e Número de Conta.
2.  **Login com 2FA Seguro:** Envio de PIN temporário (simulado via RabbitMQ) para o telemóvel do cliente antes da emissão do JWT.
3.  **Gestão de Transações:** Operações de Débito, Crédito e Estorno protegidas por rotas autenticadas (`[Authorize]`).
4.  **Dashboard Financeiro:** Interface em React.js para visualização de saldo em tempo real e histórico de transações.

## 🚀 Como Executar Localmente (Cloud Native)

Este projeto está totalmente "Dockerizado", o que significa que pode executá-lo em qualquer máquina sem precisar de instalar bancos de dados ou corretores de mensagens localmente.

### Pré-requisitos
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e a correr.
* Portas `5088` (API), `5173` (React), `1433` (SQL Server) e `5672/15672` (RabbitMQ) livres na sua máquina.

### Passo a Passo

1. **Clone o repositório:**
   ```bash
   git clone [https://github.com/SEU_USUARIO/btg-ledger.git](https://github.com/SEU_USUARIO/btg-ledger.git)
   cd btg-ledger