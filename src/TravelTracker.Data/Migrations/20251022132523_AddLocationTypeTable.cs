using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationTypeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "RV Park or campground", "RV Park" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "US National Park", "National Park" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "US National Monument", "National Monument" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Harvest Host location", "Harvest Host" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "State Park", "State Park" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Family or friends location", "Family" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "US National Park", "National Park" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Hotel or lodging", "Hotel" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Restaurant or dining", "Restaurant" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Museum or cultural site", "Museum" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Beach or coastal area", "Beach" });

            migrationBuilder.UpdateData(
                table: "LocationTypes",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "Name" },
                values: new object[] { "City or town", "City" });
        }
    }
}
