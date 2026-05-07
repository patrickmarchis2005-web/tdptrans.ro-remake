using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TdpTrans.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trucks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlate = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trucks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TruckId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Missions_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Email", "Name", "Phone" },
                values: new object[,]
                {
                    { 1, "contact@techlog.ro", "Tech Logistics SRL", "0722111222" },
                    { 2, "office@autodepanare.ro", "Auto Depanare SA", "0733444555" },
                    { 3, "ion.popescu@gmail.com", "Ion Popescu", "0744999888" }
                });

            migrationBuilder.InsertData(
                table: "Trucks",
                columns: new[] { "Id", "LicensePlate" },
                values: new object[,]
                {
                    { 112233, "TM 50 RAP" },
                    { 123456, "CJ 99 TEST" },
                    { 654321, "B 101 DEV" }
                });

            migrationBuilder.InsertData(
                table: "Missions",
                columns: new[] { "Id", "Address", "ClientId", "Cost", "Date", "Status", "TruckId", "Type" },
                values: new object[,]
                {
                    { 1, "Strada Lunga 10, Cluj-Napoca", 1, 1500.00m, new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, 123456, 0 },
                    { 2, "Autostrada A3, km 25", 2, 450.50m, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 654321, 1 },
                    { 3, "Bulevardul Unirii, Bucuresti", 3, 3200.00m, new DateTime(2026, 5, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 112233, 0 },
                    { 4, "Soseaua Vestului, Ploiesti", 1, 800.00m, new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, 123456, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ClientId",
                table: "Missions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_TruckId",
                table: "Missions",
                column: "TruckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Trucks");
        }
    }
}
