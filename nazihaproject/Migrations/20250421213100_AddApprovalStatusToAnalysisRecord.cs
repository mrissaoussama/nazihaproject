using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nazihaproject.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStatusToAnalysisRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "AnalysisRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "AnalysisRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "AnalysisRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "AnalysisRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRecords_ApprovedById",
                table: "AnalysisRecords",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisRecords_Users_ApprovedById",
                table: "AnalysisRecords",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisRecords_Users_ApprovedById",
                table: "AnalysisRecords");

            migrationBuilder.DropIndex(
                name: "IX_AnalysisRecords_ApprovedById",
                table: "AnalysisRecords");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "AnalysisRecords");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AnalysisRecords");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "AnalysisRecords");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "AnalysisRecords");
        }
    }
}
