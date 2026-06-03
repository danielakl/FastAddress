// Leaflet (OpenStreetMap) map. Default mode is zoom-only - panning is disabled. The .NET component
// drives everything through `sync(state)`; the only thing JS reports back is a map click (used while
// the user is picking a search-location bias).

const TILE_URL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
const TILE_ATTRIBUTION =
    '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
const MAX_TILE_ZOOM = 19;
const SELECT_ZOOM = 15;

// Selected-address marker (blue) vs. location-bias marker (amber).
const ADDRESS_STYLE = { radius: 8, color: "#1f6feb", fillColor: "#1f6feb", fillOpacity: 0.9, weight: 2 };
const BIAS_STYLE = { radius: 9, color: "#d97706", fillColor: "#f59e0b", fillOpacity: 0.85, weight: 2 };

const keyOf = (point) => (point ? `${point.latitude},${point.longitude}` : null);

export function createMap(element, dotnetRef, latitude, longitude, zoom) {
    const map = L.map(element, {
        center: [latitude, longitude],
        zoom,
        dragging: false, // zoom-only until "set location" mode is enabled
        keyboard: false,
        boxZoom: false,
        doubleClickZoom: false,
        scrollWheelZoom: true,
        touchZoom: true,
        zoomControl: true,
    });

    L.tileLayer(TILE_URL, { maxZoom: MAX_TILE_ZOOM, attribution: TILE_ATTRIBUTION }).addTo(map);

    // Stop map text (attribution, "+"/"-" zoom glyphs) from being text-selectable.
    map.getContainer().classList.add("select-none");

    let addressMarker = null;
    let biasMarker = null;

    // Last applied values so sync() only touches what changed.
    const applied = { searchedKey: null, biasKey: null, setting: false };

    // Leaflet fires "click" only for genuine clicks, never at the end of a drag. The .NET side
    // decides whether the click means anything (i.e. only while in set-location mode).
    map.on("click", (event) => {
        dotnetRef.invokeMethodAsync("OnMapClickedAsync", event.latlng.lat, event.latlng.lng);
    });

    return {
        sync(state) {
            if (state.setting !== applied.setting) {
                if (state.setting) {
                    map.dragging.enable();
                } else {
                    map.dragging.disable();
                }
                element.style.cursor = state.setting ? "crosshair" : "";
                applied.setting = state.setting;
            }

            const searchedKey = keyOf(state.searched);
            if (searchedKey !== applied.searchedKey) {
                if (state.searched) {
                    const latLng = [state.searched.latitude, state.searched.longitude];
                    map.setView(latLng, Math.max(map.getZoom(), SELECT_ZOOM));
                    if (addressMarker) {
                        addressMarker.setLatLng(latLng);
                    } else {
                        addressMarker = L.circleMarker(latLng, ADDRESS_STYLE).addTo(map);
                    }
                }
                applied.searchedKey = searchedKey;
            }

            const biasKey = keyOf(state.bias);
            if (biasKey !== applied.biasKey) {
                if (state.bias) {
                    const latLng = [state.bias.latitude, state.bias.longitude];
                    if (biasMarker) {
                        biasMarker.setLatLng(latLng);
                    } else {
                        biasMarker = L.circleMarker(latLng, BIAS_STYLE).addTo(map);
                    }
                } else if (biasMarker) {
                    map.removeLayer(biasMarker);
                    biasMarker = null;
                }
                applied.biasKey = biasKey;
            }
        },
        dispose() {
            map.remove();
        },
    };
}
