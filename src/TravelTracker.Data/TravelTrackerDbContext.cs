using Microsoft.EntityFrameworkCore;
using TravelTracker.Data.Models;
using System.Text.Json;

namespace TravelTracker.Data;

public class TravelTrackerDbContext : DbContext
{
    public TravelTrackerDbContext(DbContextOptions<TravelTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<NationalPark> NationalParks { get; set; }
    public DbSet<LocationType> LocationTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntraIdUserId).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        // Configure Location entity
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.StartDate);

            // Handle Tags list as JSON
            entity.Property(e => e.TagsJson)
                .HasDefaultValue("[]");

            entity.Ignore(e => e.Tags);
        });

        // Configure NationalPark entity
        modelBuilder.Entity<NationalPark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.Name);

            // Seed initial location types
            entity.HasData(
                new NationalPark { Id = 1, Type = "National Park", Name = "Acadia National Park", State = "Maine", Latitude = 44.338974, Longitude = -68.273430, Description = "The only national park in New England, featuring a wild coastal wilderness of mountains, sea cliffs, and beaches." },
                new NationalPark { Id = 02, Type = "National Park", Name = "American Samoa National Park", State = "American Samoa", Latitude = -14.235000, Longitude = -170.688000, Description = "A tropical park protecting coral reefs, volcanic peaks, and Polynesian culture." },
                new NationalPark { Id = 03, Type = "National Park", Name = "Arches National Park", State = "Utah", Latitude = 38.733082, Longitude = -109.592514, Description = "Home to the world's largest concentration of natural sandstone arches set in dramatic desert scenery." },
                new NationalPark { Id = 04, Type = "National Park", Name = "Badlands National Park", State = "South Dakota", Latitude = 43.855438, Longitude = -102.339691, Description = "A rugged landscape of sharply eroded buttes and colorful eroded hills spanning grassland prairie." },
                new NationalPark { Id = 05, Type = "National Park", Name = "Big Bend National Park", State = "Texas", Latitude = 29.127500, Longitude = -103.242500, Description = "Vast Chihuahuan Desert wilderness alongside the Rio Grande with dramatic canyons and peaks." },
                new NationalPark { Id = 06, Type = "National Park", Name = "Biscayne National Park", State = "Florida", Latitude = 25.481000, Longitude = -80.208600, Description = "A primarily underwater park safeguarding coral reefs, mangrove islands, and marine life." },
                new NationalPark { Id = 07, Type = "National Park", Name = "Black Canyon of the Gunnison National Park", State = "Colorado", Latitude = 38.575047, Longitude = -107.724570, Description = "Features steep, dramatic canyon walls carved by the Gunnison River through ancient rock." },
                new NationalPark { Id = 08, Type = "National Park", Name = "Bryce Canyon National Park", State = "Utah", Latitude = 37.593048, Longitude = -112.187332, Description = "Known for its thousands of vibrant hoodoos-tall, thin spires of rock-in a high desert setting." },
                new NationalPark { Id = 09, Type = "National Park", Name = "Canyonlands National Park", State = "Utah", Latitude = 38.326875, Longitude = -109.878286, Description = "A vast wilderness of canyons, mesas, and buttes carved by the Colorado River." },
                new NationalPark { Id = 10, Type = "National Park", Name = "Capitol Reef National Park", State = "Utah", Latitude = 38.089600, Longitude = -111.149910, Description = "Features colorful sandstone cliffs, domes, and the Waterpocket Fold-a 100 mile geological wrinkle." },
                new NationalPark { Id = 11, Type = "National Park", Name = "Carlsbad Caverns National Park", State = "New Mexico", Latitude = 32.147938, Longitude = -104.556584, Description = "Famed for its vast limestone caves, including the spectacular Big Room with massive stalactites." },
                new NationalPark { Id = 12, Type = "National Park", Name = "Channel Islands National Park", State = "California", Latitude = 33.998028, Longitude = -119.772949, Description = "A biodiversity hotspot and Galapagos of North America, with unique island ecosystems." },
                new NationalPark { Id = 13, Type = "National Park", Name = "Congaree National Park", State = "South Carolina", Latitude = 33.791300, Longitude = -80.524700, Description = "Old growth bottomland hardwood forest with one of the tallest deciduous tree canopies in the U.S." },
                new NationalPark { Id = 14, Type = "National Park", Name = "Crater Lake National Park", State = "Oregon", Latitude = 42.944611, Longitude = -122.109245, Description = "Protects North America's deepest, clear blue lake formed in a volcanic caldera." },
                new NationalPark { Id = 15, Type = "National Park", Name = "Cuyahoga Valley National Park", State = "Ohio", Latitude = 41.280000, Longitude = -81.567000, Description = "A lush, glacier-sculpted green oasis between Cleveland and Akron ideal for biking and waterfalls." },
                new NationalPark { Id = 16, Type = "National Park", Name = "Death Valley National Park", State = "California",  Latitude = 36.505389, Longitude =-117.079407, Description = "Home to the lowest, hottest, driest point in North America with dunes, salt flats, and badlands." },
                new NationalPark { Id = 17, Type = "National Park", Name = "Denali National Park", State = "Alaska", Latitude = 63.129887, Longitude = -151.197418, Description = "Encompasses North America's tallest peak and vast subarctic wilderness." },
                new NationalPark { Id = 18, Type = "National Park", Name = "Dry Tortugas National Park", State = "Florida", Latitude = 24.628510, Longitude = -82.873030, Description = "Remote reef-fringed islands featuring Fort Jefferson and vibrant coral reefs." },
                new NationalPark { Id = 19, Type = "National Park", Name = "Everglades National Park", State = "Florida", Latitude = 25.286615, Longitude = -80.898651, Description = "A vast river of grass wetland sanctuary home to unique wildlife like alligators and manatees." },
                new NationalPark { Id = 20, Type = "National Park", Name = "Gates of the Arctic National Park", State = "Alaska", Latitude = 67.915199, Longitude = -153.463730, Description = "An unfenced, roadless wilderness north of the Arctic Circle with tundra and mountains." },
                new NationalPark { Id = 21, Type = "National Park", Name = "Gateway Arch National Park", State = "Missouri", Latitude = 38.624700, Longitude = -90.184800, Description = "Centered on the iconic Gateway Arch celebrating westward expansion over the Mississippi River." },
                new NationalPark { Id = 22, Type = "National Park", Name = "Glacier Bay National Park", State = "Alaska", Latitude = 58.665806, Longitude = -136.900208, Description = "World class sights of calving glaciers, fjords, and lush temperate rainforests in southeast Alaska." },
                new NationalPark { Id = 23, Type = "National Park", Name = "Glacier National Park", State = "Montana", Latitude = 48.759613, Longitude = -113.787023, Description = "Spectacular alpine landscapes with turquoise lakes, glaciers, and iconic Going to the Sun Road." },
                new NationalPark { Id = 24, Type = "National Park", Name = "Grand Canyon National Park", State = "Arizona", Latitude = 36.266033, Longitude = -112.363808, Description = "A monumental gorge carved by the Colorado River showcasing colorful layered rocks." },
                new NationalPark { Id = 25, Type = "National Park", Name = "Grand Teton National Park", State = "Wyoming", Latitude = 43.790802, Longitude = -110.684944, Description = "Jagged peaks rise above alpine lakes in a stunning valley known as Jackson Hole." },
                new NationalPark { Id = 26, Type = "National Park", Name = "Great Basin National Park", State = "Nevada", Latitude = 38.980000, Longitude = -114.301000, Description = "A high-desert park featuring ancient bristlecone pines, mountains, and the Lehman Caves." },
                new NationalPark { Id = 27, Type = "National Park", Name = "Great Sand Dunes National Park", State = "Colorado", Latitude = 37.791667, Longitude = -105.594400, Description = "Home to North America's tallest sand dunes backed by mountains, prairie, and creekland." },
                new NationalPark { Id = 28, Type = "National Park", Name = "Great Smoky Mountains National Park", State = "Tennessee", Latitude= 35.611763, Longitude = -83.489548,  Description = "America's most visited park, famed for its misty mountains, biodiversity, and Appalachian heritage." },
                new NationalPark { Id = 29, Type = "National Park", Name = "Guadalupe Mountains National Park", State = "Texas", Latitude = 31.901900, Longitude = -104.844100, Description = "Protects rugged peaks, canyons, and the highest point in Texas in a remote desert setting." },
                new NationalPark { Id = 30, Type = "National Park", Name = "Haleakala National Park", State = "Hawaii", Latitude = 20.701283, Longitude = -156.173325, Description = "Preserves a massive volcanic crater and native ecosystems atop Maui." },
                new NationalPark { Id = 31, Type = "National Park", Name = "Hawaii Volcanoes National Park", State = "Hawaii", Latitude = 19.419400, Longitude = -155.288100, Description = "Features two of Earth's most active volcanoes and dramatic volcanic landscapes." },
                new NationalPark { Id = 32, Type = "National Park", Name = "Hot Springs National Park", State = "Arkansas", Latitude = 34.521530, Longitude = -93.042267, Description = "The oldest federally protected park, offering historic bathhouses and thermal springs in a small mountain setting." },
                new NationalPark { Id = 33, Type = "National Park", Name = "Indiana Dunes National Park", State = "Indiana", Latitude = 41.653600, Longitude = -87.052000, Description = "Dynamic dunes, oak woodlands, and wetlands along Lake Michigan's south shore." },
                new NationalPark { Id = 34, Type = "National Park", Name = "Isle Royale National Park", State = "Michigan", Latitude = 48.071100, Longitude = -88.539000, Description = "A remote island in Lake Superior with wolves, moose, and pristine wilderness trails." },
                new NationalPark { Id = 35, Type = "National Park", Name = "Joshua Tree National Park", State = "California", Latitude = 33.881866, Longitude = -115.900650, Description = "Where the Mojave and Colorado deserts meet, featuring iconic Joshua trees and rugged boulders." },
                new NationalPark { Id = 36, Type = "National Park", Name = "Katmai National Park", State = "Alaska", Latitude = 58.597813, Longitude = -154.693756, Description = "Renowned for bear-viewing and the Valley of Ten Thousand Smokes volcanic landscape." },
                new NationalPark { Id = 37, Type = "National Park", Name = "Kenai Fjords National Park", State = "Alaska", Latitude = 60.043777, Longitude = -149.816360, Description = "Coastal fjords filled with glaciers and marine life, including whales and sea otters." },
                new NationalPark { Id = 38, Type = "National Park", Name = "Kings Canyon National Park", State = "California", Latitude = 36.887000, Longitude = -118.555000, Description = "Deep glacial valleys, massive sequoias, and high mountain peaks characterize this rugged terrain." },
                new NationalPark { Id = 39, Type = "National Park", Name = "Kobuk Valley National Park", State = "Alaska", Latitude = 67.514500, Longitude = -159.277000, Description = "Home to great sand dunes, caribou migrations, and remote Arctic wilderness." },
                new NationalPark { Id = 40, Type = "National Park", Name = "Lake Clark National Park", State = "Alaska", Latitude = 60.412697, Longitude = -154.323502, Description = "Volcanic peaks, turquoise lakes, and salmon streams in a remote Alaskan setting." },
                new NationalPark { Id = 41, Type = "National Park", Name = "Lassen Volcanic National Park", State = "California", Latitude = 40.497000, Longitude = -121.420000, Description = "A volcanic wonderland of boiling mud pots, fumaroles, and four types of volcanoes." },
                new NationalPark { Id = 42, Type = "National Park", Name = "Mammoth Cave National Park", State = "Kentucky", Latitude = 37.183640, Longitude = -86.159943, Description = "The world's longest known cave system, with more than 400 miles of underground passages." },
                new NationalPark { Id = 43, Type = "National Park", Name = "Mesa Verde National Park", State = "Colorado", Latitude = 37.230873, Longitude = -108.461838, Description = "Preserves ancient Pueblo cliff dwellings like Cliff Palace perched on sandstone cliffs." },
                new NationalPark { Id = 44, Type = "National Park", Name = "Mount Rainier National Park", State = "Washington", Latitude = 46.879967, Longitude = -121.726906, Description = "Centered on majestic Mt. Rainier with glaciers, wildflower meadows, and old-growth forest." },
                new NationalPark { Id = 45, Type = "National Park", Name = "New River Gorge National Park", State = "West Virginia", Latitude = 38.064600, Longitude = -81.072500, Description = "Deep river gorge with one of the longest steel arch bridges and premier whitewater." },
                new NationalPark { Id = 46, Type = "National Park", Name = "North Cascades National Park", State = "Washington", Latitude = 48.771900, Longitude = -121.298900, Description = "A rugged wilderness filled with peaks, glaciers, and remote alpine lakes." },
                new NationalPark { Id = 47, Type = "National Park", Name = "Olympic National Park", State = "Washington", Latitude = 47.802100, Longitude = -123.604400, Description = "Encompasses mountains, temperate rainforest, and wild Pacific coastline." },
                new NationalPark { Id = 48, Type = "National Park", Name = "Petrified Forest National Park", State = "Arizona", Latitude = 34.909988, Longitude = -109.806793, Description = "Famous for colorful petrified wood, Painted Desert vistas, and archaeological ruins." },
                new NationalPark { Id = 49, Type = "National Park", Name = "Pinnacles National Park", State = "California", Latitude = 36.491508, Longitude = -121.197243, Description = "Preserves ancient volcanic spires, talus caves, and is habitat for California condors." },
                new NationalPark { Id = 50, Type = "National Park", Name = "Redwood National Park", State = "California", Latitude = 41.213181, Longitude = -124.004631, Description = "Walk among towering old growth redwoods and explore coastal rivers and prairies." },
                new NationalPark { Id = 51, Type = "National Park", Name = "Rocky Mountain National Park", State = "Colorado", Latitude = 40.343182, Longitude = -105.688103, Description = "High alpine peaks, pristine lakes, and abundant wildlife characterize this mountain park." },
                new NationalPark { Id = 52, Type = "National Park", Name = "Saguaro National Park", State = "Arizona", Latitude = 32.296900, Longitude = -111.166900, Description = "Dedicated to the iconic saguaro cactus forest of the Sonoran Desert around Tucson." },
                new NationalPark { Id = 53, Type = "National Park", Name = "Sequoia National Park", State = "California", Latitude = 36.486400, Longitude = -118.565800, Description = "Home to massive sequoias including General Sherman-the largest tree on earth-and alpine high country." },
                new NationalPark { Id = 54, Type = "National Park", Name = "Shenandoah National Park", State = "Virginia", Latitude = 38.700516, Longitude = -78.292694, Description = "Skyline Drive loops through forested Blue Ridge Mountains with waterfalls and abundant wildlife." },
                new NationalPark { Id = 55, Type = "National Park", Name = "Theodore Roosevelt National Park", State = "North Dakota", Latitude = 46.979000, Longitude = -103.538000, Description = "Badlands terrain named to honor Teddy Roosevelt, with bison herds and prairie dog towns." },
                new NationalPark { Id = 56, Type = "National Park", Name = "Virgin Islands National Park", State = "U.S. Virgin Islands", Latitude = 18.343500, Longitude = -64.798500, Description = "Tropical park preserving beaches, coral reefs, and historic plantation ruins." },
                new NationalPark { Id = 57, Type = "National Park", Name = "Voyageurs National Park", State = "Minnesota", Latitude = 48.450000, Longitude = -92.850000, Description = "Water-based park of interconnected lakes, boreal forests, and historic trade routes." },
                new NationalPark { Id = 58, Type = "National Park", Name = "White Sands National Park", State = "New Mexico", Latitude = 32.779720, Longitude = -106.171669, Description = "World's largest gypsum dune field with otherworldly white sand dunes." },
                new NationalPark { Id = 59, Type = "National Park", Name = "Wind Cave National Park", State = "South Dakota", Latitude = 43.587800, Longitude = -103.450300, Description = "Home to one of the world's longest boxwork cave systems and mixed-grass prairie." },
                new NationalPark { Id = 60, Type = "National Park", Name = "Wrangell-St. Elias National Park", State = "Alaska", Latitude = 61.710445, Longitude = 142.985687, Description = "The largest national park in the United States, spanning approximately 13.2 million acres of towering volcanic and glaciated mountain ranges-including Mount St. Elias-and vast wilderness from sea level to 18,008 ft peaks." },
                new NationalPark { Id = 61, Type = "National Park", Name = "Yellowstone National Park", State = "Wyoming", Latitude = 44.427895, Longitude = 110.588379, Description = "The world's first national park, famed for hydrothermal features like Old Faithful and abundant megafauna." },
                new NationalPark { Id = 62, Type = "National Park", Name = "Yosemite National Park", State = "California", Latitude = 37.865101, Longitude = 119.538330, Description = "Iconic granite cliffs, giant sequoias, waterfalls, and deep valleys define this legendary park" },
                new NationalPark { Id = 63, Type = "National Park", Name = "Zion National Park", State = "Utah", Latitude = 37.297817, Longitude = 113.028770, Description = "Known fo6 its towering red sandstone cliffs, narrow canyons, and the Virgin River's carved landscapes" }
            );
        });

        // Configure LocationType entity
        modelBuilder.Entity<LocationType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            // Seed initial location types
            entity.HasData(
                new LocationType { Id = 1, Name = "RV Park", Description = "RV Park or campground" },
                new LocationType { Id = 2, Name = "National Park", Description = "US National Park" },
                new LocationType { Id = 3, Name = "National Monument", Description = "US National Monument" },
                new LocationType { Id = 4, Name = "Harvest Host", Description = "Harvest Host location" },
                new LocationType { Id = 5, Name = "State Park", Description = "State Park" },
                new LocationType { Id = 6, Name = "Family", Description = "Family or friends location" },
                new LocationType { Id = 7, Name = "Other", Description = "Other location type" }
            );
        });
    }

    public override int SaveChanges()
    {
        UpdateLocationTags();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateLocationTags();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateLocationTags()
    {
        var entries = ChangeTracker.Entries<Location>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.Tags != null && entry.Entity.Tags.Any())
            {
                entry.Entity.TagsJson = JsonSerializer.Serialize(entry.Entity.Tags);
            }
            else if (entry.Entity.Tags != null && !entry.Entity.Tags.Any())
            {
                entry.Entity.TagsJson = "[]";
            }
        }
    }
}
