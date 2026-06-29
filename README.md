# CraftConnect - Artisan Craft Business Management System

CraftConnect is a desktop application developed to help Sri Lankan artisans and craft business owners manage their daily business operations digitally. By providing a centralized platform for managing products, materials, orders, and business data, the application aims to improve efficiency, reduce manual record-keeping, and support the growth and sustainability of traditional craft businesses.

## Current Phase: Backend & CLI Prototype
Currently, the application is in its initial prototype phase. It operates via a Command Line Interface (CLI) and uses a lightweight text-file-based database (`.txt` files) to perform Create, Read, Update, and Delete (CRUD) operations without needing complex SQL setups.

### Features
* **Product Management**: View existing products and add new inventory.
* **Supplier Management**: Keep track of raw material suppliers and their contact details.
* **Order Management (Full CRUD)**: Create new customer orders, view order histories, update order statuses (Pending, Shipped, Delivered), and delete cancelled/invalid orders.

## Setup Guide

### Prerequisites
1. **.NET 10.0 SDK**: Make sure you have the .NET 10 SDK installed on your machine.
2. **Code Editor / IDE**: Visual Studio 2022, JetBrains Rider, or Visual Studio Code with the C# Dev Kit extension.

### How to Run the Application

1. **Clone or Download the Repository**: Open the project folder in your terminal or IDE.
2. **Restore Dependencies**: Open your terminal in the project directory (where `CraftConnect.csproj` is located) and run:
   ```bash
   dotnet restore