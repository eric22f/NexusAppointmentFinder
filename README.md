# NexusAppointmentFinder

Get notifications when a Nexus appointment becomes available.

## Overview

NexusAppointmentFinder is an Azure Functions project designed to notify users when Nexus appointments become available. This project aims to automate the process of checking for available appointments and sending notifications to users.

## Features

- **Automated Appointment Checking**: Periodically checks for available Nexus appointments.
- **Notifications**: Sends notifications when new appointments are found.
- **Azure Functions**: Utilizes serverless functions for scalability and efficiency.
- **Integration Tests**: Ensures the reliability and correctness of the functionality.

## Missing Features

 - **Hardcoded to a single Nexus Location**: Currently only looking for appointments in Blaine, WA.
 - **No UI for user management and prefrences**: User alerts and settings must be entered in the database manually.
 - **Redis Cache incomplete for caching**: Code is in place for Redis but it has not been fully tested.

## Prerequisites

- .NET Core SDK
- Azure Functions Core Tools
- An Azure account

## Setup

1. **Clone the repository:**
   ```sh
   git clone https://github.com/eric22f/NexusAppointmentFinder.git
   cd NexusAppointmentFinder
   ```

2. **Install dependencies:**
   ```sh
   dotnet restore
   ```

3. **Configure settings:**
   Update the `local.settings.json` file with your Azure and notification service credentials.

4. **Run the Azure Functions locally:**
   ```sh
   func start
   ```

## Deployment

1. **Login to Azure:**
   ```sh
   az login
   ```

2. **Create a resource group and function app:**
   ```sh
   az group create --name <ResourceGroupName> --location <Location>
   az functionapp create --resource-group <ResourceGroupName> --consumption-plan-location <Location> --runtime dotnet --functions-version 3 --name <AppName> --storage-account <StorageAccountName>
   ```

3. **Create SQL Server database:**
 - *In Process*: Updating project to include database deployment.

4. **Deploy the functions:**
   ```sh
   func azure functionapp publish <AppName>
   ```

## Usage

- The function will run periodically based on the specified timer trigger settings.
- Notifications will be sent through text or email.

## Contributing

Contributions are welcome! Please fork the repository and submit pull requests.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Appointment Scanner](https://appointmentscanner.com)
- [TTP Appointments](https://ttpappointments.com)

---

For detailed code and further updates, visit the [GitHub repository](https://github.com/eric22f/NexusAppointmentFinder).