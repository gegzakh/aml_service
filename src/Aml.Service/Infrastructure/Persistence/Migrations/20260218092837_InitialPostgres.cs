using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmlOps.Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "aml");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    IdentifiersJson = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Dob = table.Column<DateOnly>(type: "date", nullable: true),
                    RiskFlagsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportedAlerts",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAlertId = table.Column<string>(type: "text", nullable: false),
                    CustomerExternalId = table.Column<string>(type: "text", nullable: false),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    AlertDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RiskHint = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaSettings",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LowRiskHours = table.Column<int>(type: "integer", nullable: false),
                    MediumRiskHours = table.Column<int>(type: "integer", nullable: false),
                    HighRiskHours = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlaDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Decision = table.Column<string>(type: "text", nullable: true),
                    DecisionReason = table.Column<string>(type: "text", nullable: true),
                    DecisionBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "aml",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileKey = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "aml",
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseComments",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseComments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "aml",
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseEvents",
                schema: "aml",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseEvents_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "aml",
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CaseId",
                schema: "aml",
                table: "Attachments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseComments_CaseId",
                schema: "aml",
                table: "CaseComments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseEvents_CaseId",
                schema: "aml",
                table: "CaseEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseEvents_TenantId_CaseId_At",
                schema: "aml",
                table: "CaseEvents",
                columns: new[] { "TenantId", "CaseId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CustomerId",
                schema: "aml",
                table: "Cases",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_TenantId_CaseNumber",
                schema: "aml",
                table: "Cases",
                columns: new[] { "TenantId", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_ExternalId",
                schema: "aml",
                table: "Customers",
                columns: new[] { "TenantId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedAlerts_TenantId_ExternalAlertId",
                schema: "aml",
                table: "ImportedAlerts",
                columns: new[] { "TenantId", "ExternalAlertId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaSettings_TenantId",
                schema: "aml",
                table: "SlaSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "CaseComments",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "CaseEvents",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "ImportedAlerts",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "SlaSettings",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "Cases",
                schema: "aml");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "aml");
        }
    }
}
