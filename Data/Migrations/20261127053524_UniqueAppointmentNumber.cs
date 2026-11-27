using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueAppointmentNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CUSTOMER_APPOINTMENT_AppointmentNo",
                table: "CUSTOMER_APPOINTMENT",
                column: "AppointmentNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CUSTOMER_APPOINTMENT_AppointmentNo",
                table: "CUSTOMER_APPOINTMENT");
        }
    }
}
