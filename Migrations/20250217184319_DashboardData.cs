using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Learning.Migrations
{
    /// <inheritdoc />
    public partial class DashboardData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EducatorDashboardViewModelTotalEnrollments",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DashBoardData",
                columns: table => new
                {
                    TotalEnrollments = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalCourses = table.Column<int>(type: "int", nullable: false),
                    TotalEarnings = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashBoardData", x => x.TotalEnrollments);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EducatorDashboardViewModelTotalEnrollments",
                table: "Enrollments",
                column: "EducatorDashboardViewModelTotalEnrollments");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_DashBoardData_EducatorDashboardViewModelTotalEnrollments",
                table: "Enrollments",
                column: "EducatorDashboardViewModelTotalEnrollments",
                principalTable: "DashBoardData",
                principalColumn: "TotalEnrollments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_DashBoardData_EducatorDashboardViewModelTotalEnrollments",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "DashBoardData");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_EducatorDashboardViewModelTotalEnrollments",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "EducatorDashboardViewModelTotalEnrollments",
                table: "Enrollments");
        }
    }
}
