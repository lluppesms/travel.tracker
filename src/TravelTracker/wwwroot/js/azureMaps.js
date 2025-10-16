// Azure Maps Integration for Travel Tracker
// This script initializes and manages the Azure Maps instance

let map = null;
let datasource = null;
let popup = null;

// Initialize the map
window.initializeAzureMap = function (subscriptionKey, centerLat, centerLon, zoom) {
    try {
        // Create a map instance
        map = new atlas.Map('azureMap', {
            center: [centerLon, centerLat],
            zoom: zoom || 4,
            language: 'en-US',
            authOptions: {
                authType: 'subscriptionKey',
                subscriptionKey: subscriptionKey
            }
        });

        // Wait for the map resources to be ready
        map.events.add('ready', function () {
            // Create a data source for markers
            datasource = new atlas.source.DataSource();
            map.sources.add(datasource);

            // Create a symbol layer to render the markers
            var symbolLayer = new atlas.layer.SymbolLayer(datasource, null, {
                iconOptions: {
                    image: 'pin-red',
                    allowOverlap: true,
                    ignorePlacement: true
                }
            });
            map.layers.add(symbolLayer);

            // Create a popup
            popup = new atlas.Popup({
                pixelOffset: [0, -18],
                closeButton: false
            });

            // Add a hover event to the symbol layer
            map.events.add('mouseover', symbolLayer, function (e) {
                if (e.shapes && e.shapes.length > 0) {
                    var properties = e.shapes[0].getProperties();
                    popup.setOptions({
                        content: createPopupContent(properties),
                        position: e.shapes[0].getCoordinates()
                    });
                    popup.open(map);
                }
            });

            // Close the popup when the mouse leaves the symbol
            map.events.add('mouseleave', symbolLayer, function () {
                popup.close();
            });

            // Add click event for more details
            map.events.add('click', symbolLayer, function (e) {
                if (e.shapes && e.shapes.length > 0) {
                    var properties = e.shapes[0].getProperties();
                    alert(`Location: ${properties.name}\nCity: ${properties.city}, ${properties.state}\nDate: ${properties.date}`);
                }
            });

            console.log('Azure Maps initialized successfully');
        });

        return true;
    } catch (error) {
        console.error('Error initializing Azure Maps:', error);
        return false;
    }
};

// Update map markers
window.updateAzureMapMarkers = function (locations) {
    try {
        if (!datasource) {
            console.error('Data source not initialized');
            return false;
        }

        // Clear existing markers
        datasource.clear();

        // Add new markers
        const features = locations.map(loc => {
            return new atlas.data.Feature(new atlas.data.Point([loc.lon, loc.lat]), {
                name: loc.name,
                city: loc.city,
                state: loc.state,
                date: loc.date,
                locationType: loc.locationType || 'Unknown',
                rating: loc.rating || 0
            });
        });

        datasource.add(features);

        // If we have locations, zoom to fit them
        if (locations.length > 0) {
            const bounds = atlas.data.BoundingBox.fromData(features);
            map.setCamera({
                bounds: bounds,
                padding: 50
            });
        }

        console.log(`Updated map with ${locations.length} markers`);
        return true;
    } catch (error) {
        console.error('Error updating map markers:', error);
        return false;
    }
};

// Center map on specific location
window.centerMapOnLocation = function (lat, lon, zoom) {
    try {
        if (!map) {
            console.error('Map not initialized');
            return false;
        }

        map.setCamera({
            center: [lon, lat],
            zoom: zoom || 12
        });

        return true;
    } catch (error) {
        console.error('Error centering map:', error);
        return false;
    }
};

// Create popup content HTML
function createPopupContent(properties) {
    const stars = '★'.repeat(properties.rating) + '☆'.repeat(5 - properties.rating);
    
    return `
        <div style="padding: 10px;">
            <strong>${properties.name}</strong><br/>
            <span style="color: #666;">📍 ${properties.city}, ${properties.state}</span><br/>
            <span style="color: #666;">📅 ${properties.date}</span><br/>
            <span style="color: #666;">🏷️ ${properties.locationType}</span><br/>
            <span style="color: #ffc107;">${stars}</span>
        </div>
    `;
}

// Add state overlay for state overview mode
window.highlightStates = function (states) {
    try {
        if (!map) {
            console.error('Map not initialized');
            return false;
        }

        // This would require GeoJSON data for US states
        // For now, just log the states to be highlighted
        console.log('States to highlight:', states);
        
        // In a full implementation, you would:
        // 1. Load US states GeoJSON
        // 2. Add a polygon layer
        // 3. Style visited states differently
        
        return true;
    } catch (error) {
        console.error('Error highlighting states:', error);
        return false;
    }
};

// Clean up when navigating away
window.disposeAzureMap = function () {
    try {
        if (popup) {
            popup.close();
            popup = null;
        }
        
        if (map) {
            map.dispose();
            map = null;
        }
        
        datasource = null;
        
        console.log('Azure Maps disposed');
        return true;
    } catch (error) {
        console.error('Error disposing Azure Maps:', error);
        return false;
    }
};
