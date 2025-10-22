// Azure Maps Integration for Travel Tracker
// This script initializes and manages the Azure Maps instance

let map = null;
let popup = null;

// Marker color selection based on location type
function getMarkerColor(locationType) {
    if (!locationType) return '#dc3545';
    const t = locationType.toLowerCase();

    // National Parks (ensure both words present)
    if (t.includes('national') && t.includes('park')) return '#006400';
    // State Parks
    if (t.includes('state') && t.includes('park')) return '#28a745';
    // RV Parks / RV Resort / RV Campground
    if (t.includes('rv')) return '#6f42c1';
    // Harvest Host locations
    if (t.includes('harvest')) return '#ffc107';
    // Family / Relatives visits
    if (t.includes('family') || t.includes('relative')) return '#0d6efd';

    // Fallback for anything else
    return '#dc3545';
}

// Initialize the map
window.initializeAzureMap = function (subscriptionKey, centerLat, centerLon, zoom) {
    return new Promise((resolve, reject) => {
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
                // Create a popup
                popup = new atlas.Popup({
                    pixelOffset: [0, -18],
                    closeButton: false
                });

                console.log('Azure Maps initialized successfully');
                resolve(true);
            });
        } catch (error) {
            console.error('Error initializing Azure Maps:', error);
            reject(error);
        }
    });
};

// Store markers for later cleanup
let markers = [];

// Update map markers
window.updateAzureMapMarkers = function (locations) {
    try {
        if (!map) {
            console.error('Map not initialized');
            return false;
        }

        // Clear existing markers
        markers.forEach(marker => map.markers.remove(marker));
        markers = [];

        // Add new HTML markers with individualized colors
        locations.forEach(loc => {
            const color = getMarkerColor(loc.locationType);
            
            // Store location properties for use in event handlers
            const locationProps = {
                name: loc.name,
                city: loc.city,
                state: loc.state,
                date: loc.date,
                locationType: loc.locationType || 'Unknown',
                rating: loc.rating || 0
            };
            
            // Create HTML marker with custom color
            const marker = new atlas.HtmlMarker({
                position: [loc.lon, loc.lat],
                color: color,
                text: ''
            });

            // Add hover event (using closure to capture locationProps)
            map.events.add('mouseover', marker, function (e) {
                popup.setOptions({
                    content: createPopupContent(locationProps),
                    position: marker.getOptions().position
                });
                popup.open(map);
            });

            // Add mouse leave event
            map.events.add('mouseleave', marker, function () {
                popup.close();
            });

            // Add click event (using closure to capture locationProps)
            map.events.add('click', marker, function (e) {
                alert(`Location: ${locationProps.name}\nCity: ${locationProps.city}, ${locationProps.state}\nDate: ${locationProps.date}`);
            });

            map.markers.add(marker);
            markers.push(marker);
        });

        // If we have locations, zoom to fit them
        if (locations.length > 0) {
            const positions = locations.map(loc => [loc.lon, loc.lat]);
            const bounds = atlas.data.BoundingBox.fromPositions(positions);
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

        console.log('States to highlight:', states);
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

        // Clear markers
        if (map && markers.length > 0) {
            markers.forEach(marker => map.markers.remove(marker));
            markers = [];
        }

        if (map) {
            map.dispose();
            map = null;
        }

        console.log('Azure Maps disposed');
        return true;
    } catch (error) {
        console.error('Error disposing Azure Maps:', error);
        return false;
    }
};
