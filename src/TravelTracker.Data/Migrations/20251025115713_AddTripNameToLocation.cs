using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TravelTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripNameToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TripName",
                table: "Locations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.InsertData(
                table: "NationalParks",
                columns: new[] { "Id", "Description", "Latitude", "Longitude", "Name", "State", "Type" },
                values: new object[,]
                {
                    { 1, "The only national park in New England, featuring a wild coastal wilderness of mountains, sea cliffs, and beaches.", 44.338974, -68.273430000000005, "Acadia National Park", "Maine", "National Park" },
                    { 2, "A tropical park protecting coral reefs, volcanic peaks, and Polynesian culture.", -14.234999999999999, -170.68799999999999, "American Samoa National Park", "American Samoa", "National Park" },
                    { 3, "Home to the world's largest concentration of natural sandstone arches set in dramatic desert scenery.", 38.733082000000003, -109.59251399999999, "Arches National Park", "Utah", "National Park" },
                    { 4, "A rugged landscape of sharply eroded buttes and colorful eroded hills spanning grassland prairie.", 43.855437999999999, -102.339691, "Badlands National Park", "South Dakota", "National Park" },
                    { 5, "Vast Chihuahuan Desert wilderness alongside the Rio Grande with dramatic canyons and peaks.", 29.127500000000001, -103.24250000000001, "Big Bend National Park", "Texas", "National Park" },
                    { 6, "A primarily underwater park safeguarding coral reefs, mangrove islands, and marine life.", 25.481000000000002, -80.208600000000004, "Biscayne National Park", "Florida", "National Park" },
                    { 7, "Features steep, dramatic canyon walls carved by the Gunnison River through ancient rock.", 38.575046999999998, -107.72457, "Black Canyon of the Gunnison National Park", "Colorado", "National Park" },
                    { 8, "Known for its thousands of vibrant hoodoos-tall, thin spires of rock-in a high desert setting.", 37.593048000000003, -112.187332, "Bryce Canyon National Park", "Utah", "National Park" },
                    { 9, "A vast wilderness of canyons, mesas, and buttes carved by the Colorado River.", 38.326875000000001, -109.878286, "Canyonlands National Park", "Utah", "National Park" },
                    { 10, "Features colorful sandstone cliffs, domes, and the Waterpocket Fold-a 100 mile geological wrinkle.", 38.089599999999997, -111.14991000000001, "Capitol Reef National Park", "Utah", "National Park" },
                    { 11, "Famed for its vast limestone caves, including the spectacular Big Room with massive stalactites.", 32.147938000000003, -104.556584, "Carlsbad Caverns National Park", "New Mexico", "National Park" },
                    { 12, "A biodiversity hotspot and Galapagos of North America, with unique island ecosystems.", 33.998027999999998, -119.772949, "Channel Islands National Park", "California", "National Park" },
                    { 13, "Old growth bottomland hardwood forest with one of the tallest deciduous tree canopies in the U.S.", 33.7913, -80.524699999999996, "Congaree National Park", "South Carolina", "National Park" },
                    { 14, "Protects North America's deepest, clear blue lake formed in a volcanic caldera.", 42.944611000000002, -122.109245, "Crater Lake National Park", "Oregon", "National Park" },
                    { 15, "A lush, glacier-sculpted green oasis between Cleveland and Akron ideal for biking and waterfalls.", 41.280000000000001, -81.566999999999993, "Cuyahoga Valley National Park", "Ohio", "National Park" },
                    { 16, "Home to the lowest, hottest, driest point in North America with dunes, salt flats, and badlands.", 36.505389000000001, -117.079407, "Death Valley National Park", "California", "National Park" },
                    { 17, "Encompasses North America's tallest peak and vast subarctic wilderness.", 63.129886999999997, -151.197418, "Denali National Park", "Alaska", "National Park" },
                    { 18, "Remote reef-fringed islands featuring Fort Jefferson and vibrant coral reefs.", 24.628509999999999, -82.87303, "Dry Tortugas National Park", "Florida", "National Park" },
                    { 19, "A vast river of grass wetland sanctuary home to unique wildlife like alligators and manatees.", 25.286615000000001, -80.898651000000001, "Everglades National Park", "Florida", "National Park" },
                    { 20, "An unfenced, roadless wilderness north of the Arctic Circle with tundra and mountains.", 67.915199000000001, -153.46373, "Gates of the Arctic National Park", "Alaska", "National Park" },
                    { 21, "Centered on the iconic Gateway Arch celebrating westward expansion over the Mississippi River.", 38.624699999999997, -90.184799999999996, "Gateway Arch National Park", "Missouri", "National Park" },
                    { 22, "World class sights of calving glaciers, fjords, and lush temperate rainforests in southeast Alaska.", 58.665806000000003, -136.90020799999999, "Glacier Bay National Park", "Alaska", "National Park" },
                    { 23, "Spectacular alpine landscapes with turquoise lakes, glaciers, and iconic Going to the Sun Road.", 48.759613000000002, -113.787023, "Glacier National Park", "Montana", "National Park" },
                    { 24, "A monumental gorge carved by the Colorado River showcasing colorful layered rocks.", 36.266033, -112.36380800000001, "Grand Canyon National Park", "Arizona", "National Park" },
                    { 25, "Jagged peaks rise above alpine lakes in a stunning valley known as Jackson Hole.", 43.790801999999999, -110.684944, "Grand Teton National Park", "Wyoming", "National Park" },
                    { 26, "A high-desert park featuring ancient bristlecone pines, mountains, and the Lehman Caves.", 38.979999999999997, -114.301, "Great Basin National Park", "Nevada", "National Park" },
                    { 27, "Home to North America's tallest sand dunes backed by mountains, prairie, and creekland.", 37.791666999999997, -105.59439999999999, "Great Sand Dunes National Park", "Colorado", "National Park" },
                    { 28, "America's most visited park, famed for its misty mountains, biodiversity, and Appalachian heritage.", 35.611763000000003, -83.489547999999999, "Great Smoky Mountains National Park", "Tennessee", "National Park" },
                    { 29, "Protects rugged peaks, canyons, and the highest point in Texas in a remote desert setting.", 31.901900000000001, -104.8441, "Guadalupe Mountains National Park", "Texas", "National Park" },
                    { 30, "Preserves a massive volcanic crater and native ecosystems atop Maui.", 20.701283, -156.17332500000001, "Haleakala National Park", "Hawaii", "National Park" },
                    { 31, "Features two of Earth's most active volcanoes and dramatic volcanic landscapes.", 19.4194, -155.28809999999999, "Hawaii Volcanoes National Park", "Hawaii", "National Park" },
                    { 32, "The oldest federally protected park, offering historic bathhouses and thermal springs in a small mountain setting.", 34.521529999999998, -93.042266999999995, "Hot Springs National Park", "Arkansas", "National Park" },
                    { 33, "Dynamic dunes, oak woodlands, and wetlands along Lake Michigan's south shore.", 41.653599999999997, -87.052000000000007, "Indiana Dunes National Park", "Indiana", "National Park" },
                    { 34, "A remote island in Lake Superior with wolves, moose, and pristine wilderness trails.", 48.071100000000001, -88.539000000000001, "Isle Royale National Park", "Michigan", "National Park" },
                    { 35, "Where the Mojave and Colorado deserts meet, featuring iconic Joshua trees and rugged boulders.", 33.881866000000002, -115.90065, "Joshua Tree National Park", "California", "National Park" },
                    { 36, "Renowned for bear-viewing and the Valley of Ten Thousand Smokes volcanic landscape.", 58.597813000000002, -154.69375600000001, "Katmai National Park", "Alaska", "National Park" },
                    { 37, "Coastal fjords filled with glaciers and marine life, including whales and sea otters.", 60.043776999999999, -149.81636, "Kenai Fjords National Park", "Alaska", "National Park" },
                    { 38, "Deep glacial valleys, massive sequoias, and high mountain peaks characterize this rugged terrain.", 36.887, -118.55500000000001, "Kings Canyon National Park", "California", "National Park" },
                    { 39, "Home to great sand dunes, caribou migrations, and remote Arctic wilderness.", 67.514499999999998, -159.27699999999999, "Kobuk Valley National Park", "Alaska", "National Park" },
                    { 40, "Volcanic peaks, turquoise lakes, and salmon streams in a remote Alaskan setting.", 60.412697000000001, -154.32350199999999, "Lake Clark National Park", "Alaska", "National Park" },
                    { 41, "A volcanic wonderland of boiling mud pots, fumaroles, and four types of volcanoes.", 40.497, -121.42, "Lassen Volcanic National Park", "California", "National Park" },
                    { 42, "The world's longest known cave system, with more than 400 miles of underground passages.", 37.183639999999997, -86.159942999999998, "Mammoth Cave National Park", "Kentucky", "National Park" },
                    { 43, "Preserves ancient Pueblo cliff dwellings like Cliff Palace perched on sandstone cliffs.", 37.230873000000003, -108.461838, "Mesa Verde National Park", "Colorado", "National Park" },
                    { 44, "Centered on majestic Mt. Rainier with glaciers, wildflower meadows, and old-growth forest.", 46.879967000000001, -121.726906, "Mount Rainier National Park", "Washington", "National Park" },
                    { 45, "Deep river gorge with one of the longest steel arch bridges and premier whitewater.", 38.064599999999999, -81.072500000000005, "New River Gorge National Park", "West Virginia", "National Park" },
                    { 46, "A rugged wilderness filled with peaks, glaciers, and remote alpine lakes.", 48.771900000000002, -121.2989, "North Cascades National Park", "Washington", "National Park" },
                    { 47, "Encompasses mountains, temperate rainforest, and wild Pacific coastline.", 47.802100000000003, -123.6044, "Olympic National Park", "Washington", "National Park" },
                    { 48, "Famous for colorful petrified wood, Painted Desert vistas, and archaeological ruins.", 34.909987999999998, -109.806793, "Petrified Forest National Park", "Arizona", "National Park" },
                    { 49, "Preserves ancient volcanic spires, talus caves, and is habitat for California condors.", 36.491508000000003, -121.197243, "Pinnacles National Park", "California", "National Park" },
                    { 50, "Walk among towering old growth redwoods and explore coastal rivers and prairies.", 41.213180999999999, -124.004631, "Redwood National Park", "California", "National Park" },
                    { 51, "High alpine peaks, pristine lakes, and abundant wildlife characterize this mountain park.", 40.343181999999999, -105.688103, "Rocky Mountain National Park", "Colorado", "National Park" },
                    { 52, "Dedicated to the iconic saguaro cactus forest of the Sonoran Desert around Tucson.", 32.296900000000001, -111.1669, "Saguaro National Park", "Arizona", "National Park" },
                    { 53, "Home to massive sequoias including General Sherman-the largest tree on earth-and alpine high country.", 36.486400000000003, -118.5658, "Sequoia National Park", "California", "National Park" },
                    { 54, "Skyline Drive loops through forested Blue Ridge Mountains with waterfalls and abundant wildlife.", 38.700516, -78.292693999999997, "Shenandoah National Park", "Virginia", "National Park" },
                    { 55, "Badlands terrain named to honor Teddy Roosevelt, with bison herds and prairie dog towns.", 46.978999999999999, -103.538, "Theodore Roosevelt National Park", "North Dakota", "National Park" },
                    { 56, "Tropical park preserving beaches, coral reefs, and historic plantation ruins.", 18.343499999999999, -64.798500000000004, "Virgin Islands National Park", "U.S. Virgin Islands", "National Park" },
                    { 57, "Water-based park of interconnected lakes, boreal forests, and historic trade routes.", 48.450000000000003, -92.849999999999994, "Voyageurs National Park", "Minnesota", "National Park" },
                    { 58, "World's largest gypsum dune field with otherworldly white sand dunes.", 32.779719999999998, -106.17166899999999, "White Sands National Park", "New Mexico", "National Park" },
                    { 59, "Home to one of the world's longest boxwork cave systems and mixed-grass prairie.", 43.587800000000001, -103.4503, "Wind Cave National Park", "South Dakota", "National Park" },
                    { 60, "The largest national park in the United States, spanning approximately 13.2 million acres of towering volcanic and glaciated mountain ranges-including Mount St. Elias-and vast wilderness from sea level to 18,008 ft peaks.", 61.710445, 142.98568700000001, "Wrangell-St. Elias National Park", "Alaska", "National Park" },
                    { 61, "The world's first national park, famed for hydrothermal features like Old Faithful and abundant megafauna.", 44.427894999999999, 110.588379, "Yellowstone National Park", "Wyoming", "National Park" },
                    { 62, "Iconic granite cliffs, giant sequoias, waterfalls, and deep valleys define this legendary park", 37.865101000000003, 119.53833, "Yosemite National Park", "California", "National Park" },
                    { 63, "Known fo6 its towering red sandstone cliffs, narrow canyons, and the Virgin River's carved landscapes", 37.297817000000002, 113.02876999999999, "Zion National Park", "Utah", "National Park" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "NationalParks",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DropColumn(
                name: "TripName",
                table: "Locations");
        }
    }
}
