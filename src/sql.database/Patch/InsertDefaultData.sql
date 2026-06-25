-- =============================================
-- Script: InsertDefaultData.sql
-- Description: Inserts default data for Travel Tracker
-- =============================================

DELETE FROM [Travel].[Destinations]
GO
DELETE FROM [Travel].[DestinationTypes]
GO
DELETE FROM [Travel].[Locations]
GO
DELETE FROM [Travel].[LocationTypes]
GO

DBCC CHECKIDENT (N'[Travel].[Destinations]', RESEED, 0)
DBCC CHECKIDENT (N'[Travel].[DestinationTypes]', RESEED, 0)
DBCC CHECKIDENT (N'[Travel].[LocationTypes]', RESEED, 0)
DBCC CHECKIDENT (N'[Travel].[Locations]', RESEED, 0)

Print 'Inserting LocationTypes'
INSERT INTO [Travel].[LocationTypes] (Name, Description) VALUES
('RV Park', 'RV Park or campground'),
('National Park', 'US National Park'),
('National Monument', 'US National Monument'),
('National Memorial', 'US National Memorial'),
('National Military Battlefield', 'National Military Battlefield'),
('Harvest Host', 'Harvest Host location'),
('State Park', 'State Park'),
('Family', 'Family or friends location'),
('Presidential Library', 'Presidential Library'),
('Boondocking', 'Boondocking'),
('Home', 'Home'),
('State High Point', 'State High Point'),
('Other', 'Other location type')

Print 'Inserting DestinationTypes'
INSERT INTO [Travel].[DestinationTypes] (Name, Description) VALUES
('National Park', 'US National Parks'),
('State High Point', 'Highest point in each US state'),
('Presidential Library', 'Presidential Libraries and Museums')

Print 'Inserting National Parks'
INSERT INTO [Travel].[Destinations] (DestinationTypeId, name, state, latitude, longitude, description) VALUES
(1, 'Acadia National Park', 'Maine', 44.338974, -68.273430, 'The only national park in New England, featuring a wild coastal wilderness of mountains, sea cliffs, and beaches.'),
(1, 'American Samoa National Park', 'American Samoa', -14.235000, -170.688000, 'A tropical park protecting coral reefs, volcanic peaks, and Polynesian culture.'),
(1, 'Arches National Park', 'Utah', 38.733082, -109.592514, 'Home to the world''s largest concentration of natural sandstone arches set in dramatic desert scenery.'),
(1, 'Badlands National Park', 'South Dakota', 43.855438, -102.339691, 'A rugged landscape of sharply eroded buttes and colorful eroded hills spanning grassland prairie.'),
(1, 'Big Bend National Park', 'Texas', 29.127500, -103.242500, 'Vast Chihuahuan Desert wilderness alongside the Rio Grande with dramatic canyons and peaks.'),
(1, 'Biscayne National Park', 'Florida', 25.481000, -80.208600, 'A primarily underwater park safeguarding coral reefs, mangrove islands, and marine life.'),
(1, 'Black Canyon of the Gunnison National Park', 'Colorado', 38.575047, -107.724570, 'Features steep, dramatic canyon walls carved by the Gunnison River through ancient rock.'),
(1, 'Bryce Canyon National Park', 'Utah', 37.593048, -112.187332, 'Known for its thousands of vibrant hoodoos-tall, thin spires of rock-in a high desert setting.'),
(1, 'Canyonlands National Park', 'Utah', 38.326875, -109.878286, 'A vast wilderness of canyons, mesas, and buttes carved by the Colorado River.'),
(1, 'Capitol Reef National Park', 'Utah', 38.089600, -111.149910, 'Features colorful sandstone cliffs, domes, and the Waterpocket Fold-a 100 mile geological wrinkle.'),
(1, 'Carlsbad Caverns National Park', 'New Mexico', 32.147938, -104.556584, 'Famed for its vast limestone caves, including the spectacular Big Room with massive stalactites.'),
(1, 'Channel Islands National Park', 'California', 33.998028, -119.772949, 'A biodiversity hotspot and "Galapagos of North America," with unique island ecosystems.'),
(1, 'Congaree National Park', 'South Carolina', 33.791300, -80.524700, 'Old growth bottomland hardwood forest with one of the tallest deciduous tree canopies in the U.S.'),
(1, 'Crater Lake National Park', 'Oregon', 42.944611, -122.109245, 'Protects North America''s deepest, clear blue lake formed in a volcanic caldera.'),
(1, 'Cuyahoga Valley National Park', 'Ohio', 41.280000, -81.567000, 'A lush, glacier-sculpted green oasis between Cleveland and Akron ideal for biking and waterfalls.'),
(1, 'Death Valley National Park', 'California, Nevada', 36.505389, -117.079407, 'Home to the lowest, hottest, driest point in North America with dunes, salt flats, and badlands.'),
(1, 'Denali National Park', 'Alaska', 63.129887, -151.197418, 'Encompasses North America''s tallest peak and vast subarctic wilderness.'),
(1, 'Dry Tortugas National Park', 'Florida', 24.628510, -82.873030, 'Remote reef-fringed islands featuring Fort Jefferson and vibrant coral reefs.'),
(1, 'Everglades National Park', 'Florida', 25.286615, -80.898651, 'A vast "river of grass" wetland sanctuary home to unique wildlife like alligators and manatees.'),
(1, 'Gates of the Arctic National Park', 'Alaska', 67.915199, -153.463730, 'An unfenced, roadless wilderness north of the Arctic Circle with tundra and mountains.'),
(1, 'Gateway Arch National Park', 'Missouri', 38.624700, -90.184800, 'Centered on the iconic Gateway Arch celebrating westward expansion over the Mississippi River.'),
(1, 'Glacier Bay National Park', 'Alaska', 58.665806, -136.900208, 'World class sights of calving glaciers, fjords, and lush temperate rainforests in southeast Alaska.'),
(1, 'Glacier National Park', 'Montana', 48.759613, -113.787023, 'Spectacular alpine landscapes with turquoise lakes, glaciers, and iconic Going to the Sun Road.'),
(1, 'Grand Canyon National Park', 'Arizona', 36.266033, -112.363808, 'A monumental gorge carved by the Colorado River showcasing colorful layered rocks.'),
(1, 'Grand Teton National Park', 'Wyoming', 43.790802, -110.684944, 'Jagged peaks rise above alpine lakes in a stunning valley known as Jackson Hole.'),
(1, 'Great Basin National Park', 'Nevada', 38.980000, -114.301000, 'A high-desert park featuring ancient bristlecone pines, mountains, and the Lehman Caves.'),
(1, 'Great Sand Dunes National Park', 'Colorado', 37.791667, -105.594400, 'Home to North America''s tallest sand dunes backed by mountains, prairie, and creekland.'),
(1, 'Great Smoky Mountains National Park', 'Tennessee, North Carolina', 35.611763, -83.489548, 'America''s most visited park, famed for its misty mountains, biodiversity, and Appalachian heritage.'),
(1, 'Guadalupe Mountains National Park', 'Texas', 31.901900, -104.844100, 'Protects rugged peaks, canyons, and the highest point in Texas in a remote desert setting.'),
(1, 'Haleakalā National Park', 'Hawaii', 20.701283, -156.173325, 'Preserves a massive volcanic crater and native ecosystems atop Maui.'),
(1, 'Hawaiʻi Volcanoes National Park', 'Hawaii', 19.419400, -155.288100, 'Features two of Earth''s most active volcanoes and dramatic volcanic landscapes.'),
(1, 'Hot Springs National Park', 'Arkansas', 34.521530, -93.042267, 'The oldest federally protected park, offering historic bathhouses and thermal springs in a small mountain setting.'),
(1, 'Indiana Dunes National Park', 'Indiana', 41.653600, -87.052000, 'Dynamic dunes, oak woodlands, and wetlands along Lake Michigan''s south shore.'),
(1, 'Isle Royale National Park', 'Michigan', 48.071100, -88.539000, 'A remote island in Lake Superior with wolves, moose, and pristine wilderness trails.'),
(1, 'Joshua Tree National Park', 'California', 33.881866, -115.900650, 'Where the Mojave and Colorado deserts meet, featuring iconic Joshua trees and rugged boulders.'),
(1, 'Katmai National Park', 'Alaska', 58.597813, -154.693756, 'Renowned for bear-viewing and the Valley of Ten Thousand Smokes volcanic landscape.'),
(1, 'Kenai Fjords National Park', 'Alaska', 60.043777, -149.816360, 'Coastal fjords filled with glaciers and marine life, including whales and sea otters.'),
(1, 'Kings Canyon National Park', 'California', 36.887000, -118.555000, 'Deep glacial valleys, massive sequoias, and high mountain peaks characterize this rugged terrain.'),
(1, 'Kobuk Valley National Park', 'Alaska', 67.514500, -159.277000, 'Home to great sand dunes, caribou migrations, and remote Arctic wilderness.'),
(1, 'Lake Clark National Park', 'Alaska', 60.412697, -154.323502, 'Volcanic peaks, turquoise lakes, and salmon streams in a remote Alaskan setting.'),
(1, 'Lassen Volcanic National Park', 'California', 40.497000, -121.420000, 'A volcanic wonderland of boiling mud pots, fumaroles, and four types of volcanoes.'),
(1, 'Mammoth Cave National Park', 'Kentucky', 37.183640, -86.159943, 'The world''s longest known cave system, with more than 400 miles of underground passages.'),
(1, 'Mesa Verde National Park', 'Colorado', 37.230873, -108.461838, 'Preserves ancient Pueblo cliff dwellings like Cliff Palace perched on sandstone cliffs.'),
(1, 'Mount Rainier National Park', 'Washington', 46.879967, -121.726906, 'Centered on majestic Mt. Rainier with glaciers, wildflower meadows, and old-growth forest.'),
(1, 'New River Gorge National Park', 'West Virginia', 38.064600, -81.072500, 'Deep river gorge with one of the longest steel arch bridges and premier whitewater.'),
(1, 'North Cascades National Park', 'Washington', 48.771900, -121.298900, 'A rugged wilderness filled with peaks, glaciers, and remote alpine lakes.'),
(1, 'Olympic National Park', 'Washington', 47.802100, -123.604400, 'Encompasses mountains, temperate rainforest, and wild Pacific coastline.'),
(1, 'Petrified Forest National Park', 'Arizona', 34.909988, -109.806793, 'Famous for colorful petrified wood, Painted Desert vistas, and archaeological ruins.'),
(1, 'Pinnacles National Park', 'California', 36.491508, -121.197243, 'Preserves ancient volcanic spires, talus caves, and is habitat for California condors.'),
(1, 'Redwood National Park', 'California', 41.213181, -124.004631, 'Walk among towering old growth redwoods and explore coastal rivers and prairies.'),
(1, 'Rocky Mountain National Park', 'Colorado', 40.343182, -105.688103, 'High alpine peaks, pristine lakes, and abundant wildlife characterize this mountain park.'),
(1, 'Saguaro National Park', 'Arizona', 32.296900, -111.166900, 'Dedicated to the iconic saguaro cactus forest of the Sonoran Desert around Tucson.'),
(1, 'Sequoia National Park', 'California', 36.486400, -118.565800, 'Home to massive sequoias including General Sherman-the largest tree on earth-and alpine high country.'),
(1, 'Shenandoah National Park', 'Virginia', 38.700516, -78.292694, 'Skyline Drive loops through forested Blue Ridge Mountains with waterfalls and abundant wildlife.'),
(1, 'Theodore Roosevelt National Park', 'North Dakota', 46.979000, -103.538000, 'Badlands terrain named to honor Teddy Roosevelt, with bison herds and prairie dog towns.'),
(1, 'Virgin Islands National Park', 'U.S. Virgin Islands', 18.343500, -64.798500, 'Tropical park preserving beaches, coral reefs, and historic plantation ruins.'),
(1, 'Voyageurs National Park', 'Minnesota', 48.450000, -92.850000, 'Water-based park of interconnected lakes, boreal forests, and historic trade routes.'),
(1, 'White Sands National Park', 'New Mexico', 32.779720, -106.171669, 'World''s largest gypsum dune field with otherworldly white sand dunes.'),
(1, 'Wind Cave National Park', 'South Dakota', 43.587800, -103.450300, 'Home to one of the world''s longest boxwork cave systems and mixed-grass prairie.'),
(1, 'Wrangell-St. Elias National Park', 'Alaska', 61.710445, -142.985687, 'The largest national park in the United States, spanning approximately 13.2 million acres of towering volcanic and glaciated mountain ranges-including Mount St. Elias-and vast wilderness from sea level to 18,008 ft peaks.'),
(1, 'Yellowstone National Park', 'Wyoming', 44.427895, -110.588379, 'The world''s first national park, famed for hydrothermal features like Old Faithful and abundant megafauna.'),
(1, 'Yosemite National Park', 'California', 37.865101, -119.538330, 'Iconic granite cliffs, giant sequoias, waterfalls, and deep valleys define this legendary park'),
(1, 'Zion National Park', 'Utah', 37.297817, -113.028770, 'Known for its towering red sandstone cliffs, narrow canyons, and the Virgin River''s carved landscapes')

Print 'Inserting State High Points'
INSERT INTO [Travel].[Destinations] (DestinationTypeId, name, state, latitude, longitude, description) VALUES
(2, 'Denali', 'Alaska', 63.069, -151.0063, 'Highest point in Alaska and North America at 20,320 feet. Located near Talkeetna with a gain of 24,500 feet over 56.0 miles.'),
(2, 'Gannett Peak', 'Wyoming', 43.1843, -109.6544, 'Highest point in Wyoming at 13,804 feet. Located near Pinedale with a gain of 8,650 feet over 40.4 miles.'),
(2, 'Mount Rainier', 'Washington', 46.8529, -121.7604, 'Highest point in Washington at 14,411 feet. Located near Ashford with a gain of 9,100 feet over 16.0 miles.'),
(2, 'Granite Peak', 'Montana', 45.1663, -109.808, 'Highest point in Montana at 12,799 feet. Located near Cooke City with a gain of 7,700 feet over 22.2 miles.'),
(2, 'Mount Whitney', 'California', 36.5785, -118.2924, 'Highest point in California and the contiguous United States at 14,494 feet. Located near Lone Pine with a gain of 6,750 feet over 21.4 miles.'),
(2, 'Kings Peak', 'Utah', 40.7764, -110.3729, 'Highest point in Utah at 13,528 feet. Located near Mountain View with a gain of 5,350 feet over 28.8 miles.'),
(2, 'Mount Elbert', 'Colorado', 39.1178, -106.4451, 'Highest point in Colorado at 14,440 feet. Located near Leadville with a gain of 5,000 feet over 9.0 miles.'),
(2, 'Mount Hood', 'Oregon', 45.3735, -121.6959, 'Highest point in Oregon at 11,239 feet. Located near Government Camp with a gain of 5,300 feet over 8.0 miles.'),
(2, 'Borah Peak', 'Idaho', 44.1373, -113.7811, 'Highest point in Idaho at 12,662 feet. Located near Mackay with a gain of 5,550 feet over 6.8 miles.'),
(2, 'Boundary Peak', 'Nevada', 37.8462, -118.3513, 'Highest point in Nevada at 13,140 feet. Located near Dyer with a gain of 4,400 feet over 7.4 miles.'),
(2, 'Humphreys Peak', 'Arizona', 35.3464, -111.678, 'Highest point in Arizona at 12,633 feet. Located near Flagstaff with a gain of 3,500 feet over 9.0 miles.'),
(2, 'Mount Marcy', 'New York', 44.1126, -73.9235, 'Highest point in New York at 5,344 feet. Located near Keene Valley with a gain of 3,200 feet over 14.8 miles.'),
(2, 'Katahdin', 'Maine', 45.9045, -68.9216, 'Highest point in Maine at 5,268 feet. Located near Millinocket with a gain of 4,200 feet over 10.4 miles.'),
(2, 'Wheeler Peak', 'New Mexico', 36.5568, -105.4169, 'Highest point in New Mexico at 13,161 feet. Located near Taos with a gain of 3,250 feet over 6.2 miles.'),
(2, 'Guadalupe Peak', 'Texas', 31.8914, -104.8608, 'Highest point in Texas at 8,749 feet. Located near Dell City with a gain of 2,950 feet over 8.4 miles.'),
(2, 'Mount Rogers', 'Virginia', 36.659, -81.5415, 'Highest point in Virginia at 5,729 feet. Located near Marion with a gain of 1,500 feet over 8.6 miles.'),
(2, 'Black Mesa', 'Oklahoma', 36.733, -102.997, 'Highest point in Oklahoma at 4,975 feet. Located near Kenton with a gain of 775 feet over 8.6 miles.'),
(2, 'Black Elk Peak', 'South Dakota', 43.8662, -103.5317, 'Highest point in South Dakota at 7,242 feet. Located near Custer with a gain of 1,500 feet over 5.8 miles.'),
(2, 'Eagle Mountain', 'Minnesota', 47.8998, -90.5604, 'Highest point in Minnesota at 2,301 feet. Located near Grand Marais with a gain of 600 feet over 7.0 miles.'),
(2, 'Mount Mansfield', 'Vermont', 44.5438, -72.815, 'Highest point in Vermont at 4,393 feet. Located near Underhill with a gain of 1,053 feet over 2.8 miles.'),
(2, 'Mount Frissell-South Slope', 'Connecticut', 42.0495, -73.483, 'Highest point in Connecticut at 2,380 feet. Located near Salisbury with a gain of 862 feet over 2.3 miles.'),
(2, 'White Butte', 'North Dakota', 46.3861, -103.2986, 'Highest point in North Dakota at 3,506 feet. Located near Amidon with a gain of 400 feet over 3.4 miles.'),
(2, 'Mauna Kea', 'Hawaii', 19.8206, -155.4681, 'Highest point in Hawaii at 13,796 feet. Located near Hilo with a gain of 230 feet over 0.4 miles.'),
(2, 'Hoye Crest', 'Maryland', 39.2069, -79.4853, 'Highest point in Maryland at 3,360 feet. Located near Oakland with a gain of 750 feet over 2.2 miles.'),
(2, 'Clingmans Dome', 'Tennessee', 35.5629, -83.4986, 'Highest point in Tennessee at 6,643 feet. Located near Gatlinburg with a gain of 330 feet over 1.0 miles.'),
(2, 'Brasstown Bald', 'Georgia', 34.8747, -83.8108, 'Highest point in Georgia at 4,784 feet. Located near Hiawassee with a gain of 400 feet over 1.0 miles.'),
(2, 'Charles Mound', 'Illinois', 42.5048, -90.239, 'Highest point in Illinois at 1,235 feet. Located near Galena with a gain of 275 feet over 2.5 miles.'),
(2, 'Mount Mitchell', 'North Carolina', 35.7656, -82.2653, 'Highest point in North Carolina at 6,684 feet. Located near Burnsville with a gain of 100 feet over 0.2 miles.'),
(2, 'Magazine Mountain', 'Arkansas', 35.1671, -93.6447, 'Highest point in Arkansas at 2,753 feet. Located near Paris with a gain of 225 feet over 1.0 miles.'),
(2, 'Driskill Mountain', 'Louisiana', 32.4248, -92.8969, 'Highest point in Louisiana at 535 feet. Located near Bryceland with a gain of 150 feet over 1.8 miles.'),
(2, 'Spruce Knob', 'West Virginia', 38.6996, -79.5329, 'Highest point in West Virginia at 4,863 feet. Located near Riverton with a gain of 20 feet over 0.3 miles.'),
(2, 'Timms Hill', 'Wisconsin', 45.4513, -90.1954, 'Highest point in Wisconsin at 1,951 feet. Located near Ogema with a gain of 120 feet over 0.4 miles.'),
(2, 'Sassafras Mountain', 'South Carolina', 35.0646, -82.7773, 'Highest point in South Carolina at 3,533 feet. Located near Rocky Bottom with a gain of 50 feet over 0.15 miles.'),
(2, 'Taum Sauk Mountain', 'Missouri', 37.5689, -90.7298, 'Highest point in Missouri at 1,772 feet. Located near Ironton with a gain of 30 feet over 0.4 miles.'),
(2, 'Mount Washington', 'New Hampshire', 44.2706, -71.3033, 'Highest point in New Hampshire at 6,288 feet. Located near Gorham with a gain of 20 feet over 0.1 miles.'),
(2, 'Black Mountain', 'Kentucky', 36.9012, -82.8874, 'Highest point in Kentucky at 4,145 feet. Located near Lynch with a gain of 30 feet over 0.1 miles.'),
(2, 'High Point', 'New Jersey', 41.3209, -74.6616, 'Highest point in New Jersey at 1,803 feet. Located near Sussex with a gain of 40 feet over 0.2 miles.'),
(2, 'Panorama Point', 'Nebraska', 41.0036, -104.035, 'Highest point in Nebraska at 5,424 feet. Located near Kimball with a gain of 0 feet over 0.1 miles.'),
(2, 'Mount Greylock', 'Massachusetts', 42.6379, -73.1665, 'Highest point in Massachusetts at 3,491 feet. Located near North Adams with a gain of 20 feet over 0.1 miles.'),
(2, 'Mount Sunflower', 'Kansas', 39.0219, -102.0372, 'Highest point in Kansas at 4,039 feet. Located near Weskan with a gain of 0 feet over 0.1 miles.'),
(2, 'Mount Arvon', 'Michigan', 46.756, -88.1564, 'Highest point in Michigan at 1,979 feet. Located near L''Anse with a gain of 10 feet over 0.1 miles.'),
(2, 'Jerimoth Hill', 'Rhode Island', 41.8384, -71.7789, 'Highest point in Rhode Island at 812 feet. Located near Foster with a gain of 0 feet over 0.2 miles.'),
(2, 'Mount Davis', 'Pennsylvania', 39.7897, -79.1767, 'Highest point in Pennsylvania at 3,213 feet. Located near Meyersdale with a gain of 0 feet over 0.1 miles.'),
(2, 'Cheaha Mountain', 'Alabama', 33.4854, -85.8086, 'Highest point in Alabama at 2,407 feet. Located near Delta with a gain of 0 feet over 0.1 miles.'),
(2, 'Hawkeye Point', 'Iowa', 43.4619, -95.7083, 'Highest point in Iowa at 1,670 feet. Located near Sibley with a gain of 0 feet over 0.1 miles.'),
(2, 'Campbell Hill', 'Ohio', 40.3695, -83.7354, 'Highest point in Ohio at 1,550 feet. Located near Bellefontaine with a gain of 0 feet over 0.1 miles.'),
(2, 'Hoosier Hill', 'Indiana', 40.0001, -84.8486, 'Highest point in Indiana at 1,257 feet. Located near Liberty with a gain of 0 feet over 0.1 miles.'),
(2, 'Woodall Mountain', 'Mississippi', 34.9979, -88.2006, 'Highest point in Mississippi at 806 feet. Located near Iuka with a gain of 0 feet over 0.1 miles.'),
(2, 'Ebright Azimuth', 'Delaware', 39.8385, -75.52, 'Highest point in Delaware at 448 feet. Located near Wilmington with a gain of 0 feet over 0.1 miles.'),
(2, 'Britton Hill', 'Florida', 30.988, -86.2833, 'Highest point in Florida at 345 feet. Located near Lakewood with a gain of 0 feet over 0.1 miles.')


Print 'Inserting Presidential Libraries and Museums'
INSERT INTO [Travel].[Destinations] (DestinationTypeId, name, state, latitude, longitude, description) VALUES
(3, 'George Washington Papers', 'Virginia', 38.7074, -77.0868, '1 (Not-NARA) Mount Vernon research library preserving George Washington''s papers and personal archives.'),
(3, 'John Adams Papers', 'Massachusetts', 42.2551, -71.0113, '2 (Not-NARA) Historic Quincy library containing John Adams'' personal collection at Adams National Historical Park.'),
(3, 'Thomas Jefferson Papers', 'Virginia', 38.0097, -78.4544, '3 (Not-NARA) Charlottesville research campus at Monticello supporting scholarship on Thomas Jefferson.'),
(3, 'James Madison Papers', 'Virginia', 38.0336, -78.508, '4 (Collection) University of Virginia collection housing James Madison''s papers and correspondence.'),
(3, 'James Monroe Memorial Library and Museum', 'Virginia', 38.3019, -77.4606, '5 (Not-NARA) Fredericksburg museum and archives interpreting President James Monroe''s life.'),
(3, 'John Quincy Adams Papers', 'Massachusetts', 42.2551, -71.0113, '6 (Not-NARA) Adams family stone library preserving John Quincy Adams'' extensive book collection.'),
(3, 'Andrew Jackson Papers', 'Tennessee', 35.9553, -83.9309, '7 (Collection) University of Tennessee collection and reading room for Andrew Jackson''s papers.'),
(3, 'James Buchanan Papers', 'Pennsylvania', 39.9489, -75.1635, '15 (Collection) Philadelphia archives providing access to James Buchanan''s manuscripts and letters.'),
(3, 'Abraham Lincoln Presidential Library and Museum', 'Illinois', 39.7983, -89.6485, '16 (Not-NARA) Springfield complex blending museum exhibits with the Lincoln Presidential Library.'),
(3, 'Andrew Johnson Museum and Library and Museum', 'Tennessee', 36.1687, -82.7419, '17 (Collection) Tusculum University museum preserving artifacts and records for Andrew Johnson.'),
(3, 'Ulysses S. Grant Presidential Library and Museum', 'Mississippi', 33.4543, -88.794, '18 (Collection) Mississippi State University library dedicated to Ulysses S. Grant scholarship.'),
(3, 'Rutherford B. Hayes Presidential Library and Museum', 'Ohio', 41.3503, -83.121, '19 (Not-NARA) Fremont estate housing the Hayes home, museum, and research library.'),
(3, 'Grover Cleveland Papers', 'New Jersey', 40.347, -74.656, '22 and 24 (Collection) Princeton repository for Grover Cleveland''s papers and memorabilia.'),
(3, 'William McKinley Presidential Library and Museum', 'Ohio', 40.8207, -81.3816, '25 (Not-NARA) Canton museum featuring science exhibits and tributes to William McKinley.'),
(3, 'Theodore Roosevelt Presidential Library and Museum', 'North Dakota', 46.9133, -103.5243, '26 (NARA) Opening 2026 - planned Medora campus honoring Theodore Roosevelt''s conservation legacy.'),
(3, 'Woodrow Wilson Presidential Library and Museum', 'Virginia', 38.1496, -79.073, '28 (Not-NARA) Staunton birthplace site with museum exhibits on Woodrow Wilson''s presidency.'),
(3, 'Warren G. Harding Presidential Center', 'Ohio', 40.5895, -83.1289, '29 (Not-NARA) Marion site combining the Harding Home, memorial, and newly built library museum.'),
(3, 'Calvin Coolidge Presidential Library and Museum', 'Massachusetts', 42.3188, -72.6311, '30 (Not-NARA) Northampton collection inside Forbes Library chronicling Calvin Coolidge''s career.'),
(3, 'Herbert Hoover Presidential Library and Museum', 'Iowa', 41.6712, -91.346, '31 (NARA) Located in West Branch, Iowa. Dedicated to America''s 31st President.'),
(3, 'Franklin D. Roosevelt Presidential Library and Museum', 'New York', 41.7677, -73.9327, '32 (NARA) Located in Hyde Park, NY. The first presidential library, dedicated to FDR.'),
(3, 'Harry S. Truman Presidential Library and Museum', 'Missouri', 39.0911, -94.4178, '33 (NARA) Located in Independence, Missouri. Dedicated to America''s 33rd President.'),
(3, 'Dwight D. Eisenhower Presidential Library and Museum', 'Kansas', 38.9172, -97.2133, '34 (NARA) Located in Abilene, Kansas. Dedicated to the 34th President and WWII Supreme Commander.'),
(3, 'John F. Kennedy Presidential Library and Museum', 'Massachusetts', 42.3161, -71.0337, '35 (NARA) Located in Boston, Massachusetts. Dedicated to America''s 35th President.'),
(3, 'Lyndon Baines Johnson Presidential Library and Museum', 'Texas', 30.285, -97.7324, '36 (NARA) Located in Austin, Texas. Dedicated to America''s 36th President.'),
(3, 'Richard Nixon Presidential Library and Museum', 'California', 33.8895, -117.8822, '37 (NARA) Located in Yorba Linda, California. Dedicated to America''s 37th President.'),
(3, 'Gerald R. Ford Presidential Library and Museum', 'Michigan', 42.9664, -85.6741, '38 (NARA) Located in Grand Rapids, Michigan. Dedicated to America''s 38th President.'),
(3, 'Jimmy Carter Presidential Library and Museum', 'Georgia', 33.7677, -84.3486, '39 (NARA) Located in Atlanta, Georgia. Dedicated to America''s 39th President.'),
(3, 'Ronald Reagan Presidential Library and Museum', 'California', 34.2596, -118.8196, '40 (NARA) Located in Simi Valley, California. Dedicated to America''s 40th President.'),
(3, 'George H. W. Bush Presidential Library and Museum', 'Texas', 30.6154, -96.3141, '41 (NARA) Located in College Station, Texas. Dedicated to America''s 41st President.'),
(3, 'William J. Clinton Presidential Library and Museum', 'Arkansas', 34.7466, -92.2635, '42 (NARA) Located in Little Rock, Arkansas. Dedicated to America''s 42nd President.'),
(3, 'George W. Bush Presidential Library and Museum', 'Texas', 32.8407, -96.7784, '43 (NARA) Located in Dallas, Texas. Dedicated to America''s 43rd President.'),
(3, 'Barack Obama Presidential Library and Museum', 'Illinois', 41.783, -87.59, '44 (NARA) Jackson Park campus opening in 2026 to showcase President Barack Obama''s story.'),
(3, 'Donald J. Trump Presidential Center', 'Florida', 25.7663, -80.2374, '45 and 47 (Fictional) Conceptual propaganda site in Miami; not an official NARA facility.'),
(3, 'Joseph R. Biden Jr. Presidential Library and Museum', 'Delaware', 39.7391, -75.5398, '46 (NARA) Planned Delaware presidential center announced for President Joe Biden.')

Print 'Showing Results'
Select * From [Travel].[LocationTypes]
Select dt.Name as DestType, d.* From [Travel].[Destinations] d INNER JOIN [Travel].[DestinationTypes] dt ON d.DestinationTypeId = dt.Id
