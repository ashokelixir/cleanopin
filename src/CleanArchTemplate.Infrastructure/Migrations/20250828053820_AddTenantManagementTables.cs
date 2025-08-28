using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Identifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectionString = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Configuration = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystemConfiguration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantUsageMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<double>(type: "numeric(18,6)", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsageMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUsageMetrics_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigurations_IsSystemConfiguration",
                table: "TenantConfigurations",
                column: "IsSystemConfiguration");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigurations_TenantId",
                table: "TenantConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigurations_TenantId_Key",
                table: "TenantConfigurations",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                table: "Tenants",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive_SubscriptionExpiresAt",
                table: "Tenants",
                columns: new[] { "IsActive", "SubscriptionExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_MetricName",
                table: "TenantUsageMetrics",
                column: "MetricName");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_RecordedAt",
                table: "TenantUsageMetrics",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_RecordedBy",
                table: "TenantUsageMetrics",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_TenantId",
                table: "TenantUsageMetrics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_TenantId_MetricName",
                table: "TenantUsageMetrics",
                columns: new[] { "TenantId", "MetricName" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_TenantId_MetricName_RecordedAt",
                table: "TenantUsageMetrics",
                columns: new[] { "TenantId", "MetricName", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsageMetrics_TenantId_RecordedAt",
                table: "TenantUsageMetrics",
                columns: new[] { "TenantId", "RecordedAt" });

            // Create a default tenant for existing data before adding foreign key constraints
            var defaultTenantId = "00000000-0000-0000-0000-000000000001";
            var now = DateTime.UtcNow;
            
            migrationBuilder.Sql($@"
                INSERT INTO ""Tenants"" (""Id"", ""Name"", ""Identifier"", ""Configuration"", ""IsActive"", ""CreatedAt"", ""CreatedBy"")
                VALUES ('{defaultTenantId}', 'Default Tenant', 'default', '{{}}', true, '{now:yyyy-MM-dd HH:mm:ss}', 'System')
                ON CONFLICT (""Id"") DO NOTHING;
            ");
            
            // Update existing users and roles to use the default tenant
            migrationBuilder.Sql($@"
                UPDATE ""Users"" SET ""TenantId"" = '{defaultTenantId}' WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
            ");
            
            migrationBuilder.Sql($@"
                UPDATE ""Roles"" SET ""TenantId"" = '{defaultTenantId}' WHERE ""TenantId"" = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                table: "Roles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "TenantConfigurations");

            migrationBuilder.DropTable(
                name: "TenantUsageMetrics");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Roles");
        }
    }
}
