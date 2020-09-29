using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Mqtt.Context.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Audit",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(nullable: true),
                    ClientId = table.Column<string>(nullable: true),
                    Topic = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    Payload = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Connection",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(nullable: true),
                    Endpoint = table.Column<string>(nullable: true),
                    Username = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    CleanSession = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Exception = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MqttMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Topic = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MqttMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MqttUsers",
                columns: table => new
                {
                    Username = table.Column<string>(nullable: false),
                    Password = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MqttUsers", x => x.Username);
                });

            migrationBuilder.CreateTable(
                name: "Payload",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Maneuver = table.Column<int>(nullable: false),
                    PlatoonGap = table.Column<int>(nullable: false),
                    PlatoonOverrideStatus = table.Column<bool>(nullable: false),
                    VehicleRank = table.Column<int>(nullable: false),
                    BreakPedal = table.Column<int>(nullable: false),
                    PlatoonDissolveStatus = table.Column<bool>(nullable: false),
                    StationId = table.Column<int>(nullable: false),
                    StreamingRequests = table.Column<int>(nullable: false),
                    V2HealthStatus = table.Column<bool>(nullable: false),
                    TruckRoutingStaus = table.Column<int>(nullable: false),
                    RealPayload = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payload", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Platoon",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlatoonRealId = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    ClientId = table.Column<string>(nullable: true),
                    VechicleId = table.Column<string>(nullable: true),
                    IsLead = table.Column<bool>(nullable: false),
                    IsFollower = table.Column<bool>(nullable: false),
                    Enable = table.Column<bool>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platoon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscribe",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(nullable: true),
                    Topic = table.Column<string>(nullable: true),
                    QoS = table.Column<string>(nullable: true),
                    Enable = table.Column<bool>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribe", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MqttMessages_Created",
                table: "MqttMessages",
                column: "Created");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audit");

            migrationBuilder.DropTable(
                name: "Connection");

            migrationBuilder.DropTable(
                name: "Log");

            migrationBuilder.DropTable(
                name: "MqttMessages");

            migrationBuilder.DropTable(
                name: "MqttUsers");

            migrationBuilder.DropTable(
                name: "Payload");

            migrationBuilder.DropTable(
                name: "Platoon");

            migrationBuilder.DropTable(
                name: "Subscribe");
        }
    }
}
