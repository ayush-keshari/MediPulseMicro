using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS8981

namespace TelemetryService.Migrations
{
    /// <inheritdoc />
    public partial class remigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tables already created by InitialCreate — nothing to do
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SensorDevice')
                BEGIN
                    CREATE TABLE [SensorDevice] (
                        [SensorID] int IDENTITY(1,1) NOT NULL,
                        [DeviceType] nvarchar(50) NOT NULL,
                        [AssignedTo] nvarchar(50) NOT NULL,
                        [AssignedEntityId] int NULL,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'Active',
                        CONSTRAINT [PK_SensorDevice] PRIMARY KEY ([SensorID])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TelemetryRecord')
                BEGIN
                    CREATE TABLE [TelemetryRecord] (
                        [TelemetryID] int IDENTITY(1,1) NOT NULL,
                        [SensorID] int NOT NULL,
                        [Timestamp] datetime2 NOT NULL,
                        [Temperature] decimal(5,2) NULL,
                        [Humidity] decimal(5,2) NULL,
                        [Location] nvarchar(200) NULL,
                        [IsExcursion] bit NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_TelemetryRecord] PRIMARY KEY ([TelemetryID]),
                        CONSTRAINT [FK_TelemetryRecord_SensorDevice] FOREIGN KEY ([SensorID])
                            REFERENCES [SensorDevice] ([SensorID]) ON DELETE CASCADE
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelemetryRecord_SensorID')
                BEGIN
                    CREATE INDEX [IX_TelemetryRecord_SensorID] ON [TelemetryRecord] ([SensorID]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TelemetryRecord') DROP TABLE [TelemetryRecord];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SensorDevice') DROP TABLE [SensorDevice];");
        }
    }
}

#pragma warning restore CS8981
