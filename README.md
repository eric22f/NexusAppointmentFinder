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

## Database Deployment with Flyway

Flyway is used to manage and deploy the database schema changes.

### Setup Flyway

1. **Install Flyway:**
   - Download and install [Flyway](https://flywaydb.org/download/).

2. **Configure Flyway:**
   - Navigate to the `Database` directory in your project:
     ```sh
     cd Database
     ```

   - Create a `flyway.conf` file in the `Database` folder with the following content:

     ```ini
     flyway.url=jdbc:sqlserver://<YourServer>;databaseName=<YourDatabase>
     flyway.user=<YourUsername>
     flyway.password=<YourPassword>
     flyway.locations=filesystem:./migrations
     ```

   Replace `<YourServer>`, `<YourDatabase>`, `<YourUsername>`, and `<YourPassword>` with your actual database connection details.

### Deploy the Database

1. **Create the Baseline:**
   - If you are setting up Flyway for the first time on an existing database, run the following command to baseline the current schema:

     ```sh
     flyway baseline
     ```

2. **Run Migrations:**
   - To apply all available migrations and update your database schema, run:

     ```sh
     flyway migrate
     ```

3. **Check Migration Status:**
   - To see the status of all migrations, use:

     ```sh
     flyway info
     ```

4. **Undo Last Migration (if needed):**
   - If you need to undo the last applied migration, run:

     ```sh
     flyway undo
     ```

### Adding New Migrations

1. **Create Migration Scripts:**
   - Add new SQL migration scripts to the `migrations` directory. Use the naming convention `V<version>__<description>.sql` (e.g., `V2__Add_New_Column.sql`).

2. **Apply New Migrations:**
   - After adding new migration scripts, run the `flyway migrate` command again to apply them to the database.

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
