// Azure Maps Integration for Travel Tracker
// This script initializes and manages the Azure Maps instance

let maps = {}; // Store multiple map instances
let popups = {}; // Store multiple popup instances
let mapMarkers = {}; // Store markers for each map

// Escape user-provided strings before inserting into HTML to prevent XSS
function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

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

// Get marker color for bucket list based on visited status
function getBucketListMarkerColor(isVisited) {
    return isVisited ? '#28a745' : '#dc3545'; // Green for visited, red for not visited
}

// Check if a destination type is a presidential library/site type
function isPresidentialSiteType(destinationType) {
    if (!destinationType) return false;
    return destinationType.toLowerCase().includes('presidential');
}

// Get the marker text icon for a presidential site based on its name
function getPresidentialSiteMarkerText(name) {
    const nameLower = (name || '').toLowerCase();
    return nameLower.includes('library') ? '🏛' : '📄';
}

// Initialize the map with custom container ID
window.initializeAzureMap = function (containerIdOrKey, subscriptionKeyOrLat, centerLatOrLon, centerLonOrZoom, zoomOrUndef) {
    return new Promise((resolve, reject) => {
        try {
            let containerId, subscriptionKey, centerLat, centerLon, zoom;
            
            // Check if first parameter is a container ID (new signature) or subscription key (old signature)
            if (typeof containerIdOrKey === 'string' && document.getElementById(containerIdOrKey)) {
                // New signature: (containerId, subscriptionKey, centerLat, centerLon, zoom)
                containerId = containerIdOrKey;
                subscriptionKey = subscriptionKeyOrLat;
                centerLat = centerLatOrLon;
                centerLon = centerLonOrZoom;
                zoom = zoomOrUndef || 4;
            } else {
                // Old signature: (subscriptionKey, centerLat, centerLon, zoom) - default to azureMap
                containerId = 'azureMap';
                subscriptionKey = containerIdOrKey;
                centerLat = subscriptionKeyOrLat;
                centerLon = centerLatOrLon;
                zoom = centerLonOrZoom || 4;
            }
            
            // Create a map instance
            const map = new atlas.Map(containerId, {
                center: [centerLon, centerLat],
                zoom: zoom,
                language: 'en-US',
                authOptions: {
                    authType: 'subscriptionKey',
                    subscriptionKey: subscriptionKey
                }
            });

            // Wait for the map resources to be ready
            map.events.add('ready', function () {
                // Create a popup
                const popup = new atlas.Popup({
                    pixelOffset: [0, -18],
                    closeButton: false
                });

                // Store map and popup instances
                maps[containerId] = map;
                popups[containerId] = popup;
                mapMarkers[containerId] = [];

                console.log(`Azure Maps initialized successfully for ${containerId}`);
                resolve(true);
            });
        } catch (error) {
            console.error('Error initializing Azure Maps:', error);
            reject(error);
        }
    });
};

// Update map markers (backward compatible - uses azureMap by default)
window.updateAzureMapMarkers = function (locations) {
    return updateMapMarkersForContainer('azureMap', locations);
};

// Update bucket list map markers
window.updateBucketListMapMarkers = function (containerId, destinations) {
    try {
        const map = maps[containerId];
        const popup = popups[containerId];
        
        if (!map) {
            console.error(`Map ${containerId} not initialized`);
            return false;
        }

        // Clear existing markers
        const markers = mapMarkers[containerId] || [];
        markers.forEach(marker => map.markers.remove(marker));
        mapMarkers[containerId] = [];

        // Add new HTML markers with color based on visited status
        destinations.forEach(dest => {
            const color = getBucketListMarkerColor(dest.isVisited);
            
            // Store destination properties for use in event handlers
            const destProps = {
                name: dest.name,
                state: dest.state,
                destinationType: dest.destinationType || 'Unknown',
                isVisited: dest.isVisited
            };
            
            // Create HTML marker with color and optional icon text for presidential sites
            const markerText = isPresidentialSiteType(destProps.destinationType)
                ? getPresidentialSiteMarkerText(dest.name)
                : '';
            const marker = new atlas.HtmlMarker({
                position: [dest.lon, dest.lat],
                color: color,
                text: markerText
            });

            // Add hover event
            map.events.add('mouseover', marker, function (e) {
                popup.setOptions({
                    content: createBucketListPopupContent(destProps),
                    position: marker.getOptions().position
                });
                popup.open(map);
            });

            // Add mouse leave event
            map.events.add('mouseleave', marker, function () {
                popup.close();
            });

            // Add click event
            map.events.add('click', marker, function (e) {
                const status = destProps.isVisited ? 'Visited' : 'Not Yet Visited';
                alert(`${destProps.name}\n${destProps.state}\nStatus: ${status}`);
            });

            map.markers.add(marker);
            mapMarkers[containerId].push(marker);
        });

        // If we have destinations, zoom to fit them
        if (destinations.length > 0) {
            const positions = destinations.map(dest => [dest.lon, dest.lat]);
            const bounds = atlas.data.BoundingBox.fromPositions(positions);
            map.setCamera({
                bounds: bounds,
                padding: 50
            });
        }

        console.log(`Updated ${containerId} with ${destinations.length} markers`);
        return true;
    } catch (error) {
        console.error(`Error updating ${containerId} markers:`, error);
        return false;
    }
};

// Helper function to update markers for a specific container
function updateMapMarkersForContainer(containerId, locations) {
    try {
        const map = maps[containerId];
        const popup = popups[containerId];
        
        if (!map) {
            console.error(`Map ${containerId} not initialized`);
            return false;
        }

        // Clear existing markers
        const markers = mapMarkers[containerId] || [];
        markers.forEach(marker => map.markers.remove(marker));
        mapMarkers[containerId] = [];

        // Add new HTML markers with individualized colors
        locations.forEach(loc => {
            const color = getMarkerColor(loc.locationType);
            
            // Store location properties for use in event handlers
            const locationProps = {
                name: loc.name,
                tripName: loc.tripName || '',
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
            mapMarkers[containerId].push(marker);
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

        console.log(`Updated ${containerId} with ${locations.length} markers`);
        return true;
    } catch (error) {
        console.error(`Error updating ${containerId} markers:`, error);
        return false;
    }
}

// Center map on specific location
window.centerMapOnLocation = function (lat, lon, zoom) {
    return centerMapOnLocationForContainer('azureMap', lat, lon, zoom);
};

function centerMapOnLocationForContainer(containerId, lat, lon, zoom) {
    try {
        const map = maps[containerId];
        if (!map) {
            console.error(`Map ${containerId} not initialized`);
            return false;
        }

        map.setCamera({
            center: [lon, lat],
            zoom: zoom || 12
        });

        return true;
    } catch (error) {
        console.error(`Error centering ${containerId}:`, error);
        return false;
    }
}

// Create popup content HTML
function createPopupContent(properties) {
    const stars = '★'.repeat(properties.rating) + '☆'.repeat(5 - properties.rating);
    const tripNameLine = properties.tripName ? `<span style="color: #666;">🧳 ${properties.tripName}</span><br/>` : '';
    return `
        <div style="padding: 10px;">
            <strong>${properties.name}</strong><br/>
            ${tripNameLine}<span style="color: #666;">📍 ${properties.city}, ${properties.state}</span><br/>
            <span style="color: #666;">📅 ${properties.date}</span><br/>
            <span style="color: #666;">🏷️ ${properties.locationType}</span><br/>
            <span style="color: #ffc107;">${stars}</span>
        </div>
    `;
}

// Create bucket list popup content HTML
function createBucketListPopupContent(properties) {
    const status = properties.isVisited ? '✓ Visited' : '○ Not Yet Visited';
    const statusColor = properties.isVisited ? '#28a745' : '#dc3545';
    const descriptionLine = properties.description ? `<span style="color: #444; font-style: italic;">${escapeHtml(properties.description)}</span><br/>` : '';
    return `
        <div style="padding: 10px; max-width: 260px;">
            <strong>${properties.name}</strong><br/>
            <span style="color: #666;">📍 ${properties.state}</span><br/>
            <span style="color: #666;">🏷️ ${properties.destinationType}</span><br/>
            ${descriptionLine}<span style="color: ${statusColor}; font-weight: bold;">${status}</span>
        </div>
    `;
}

// Add state overlay for state overview mode
window.highlightStates = function (states) {
    try {
        const map = maps['azureMap'];
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

// Clean up when navigating away - with optional containerId
window.disposeAzureMap = function (containerIdOrUndef) {
    try {
        const containerId = containerIdOrUndef || 'azureMap';
        
        const popup = popups[containerId];
        if (popup) {
            popup.close();
            delete popups[containerId];
        }

        // Clear markers
        const map = maps[containerId];
        const markers = mapMarkers[containerId];
        if (map && markers && markers.length > 0) {
            markers.forEach(marker => map.markers.remove(marker));
            delete mapMarkers[containerId];
        }

        if (map) {
            map.dispose();
            delete maps[containerId];
        }

        console.log(`Azure Maps ${containerId} disposed`);
        return true;
    } catch (error) {
        console.error('Error disposing Azure Maps:', error);
        return false;
    }
};
