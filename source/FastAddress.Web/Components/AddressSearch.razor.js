// Browser-side search client. Owns a single AbortController so each new query cancels the
// previous in-flight request. Talks to the same-origin proxy at /api/addresses/search.

const SEARCH_URL = "/api/addresses/search";
const RESULT_LIMIT = 5;

let controller = null;

export async function search(text, latitude, longitude) {
    // Cancel any request still in flight before starting the next one.
    controller?.abort();
    controller = new AbortController();

    const hasBias = latitude != null && longitude != null;
    const body = JSON.stringify({
        address: text,
        limit: RESULT_LIMIT,
        // GeoJSON order is [longitude, latitude].
        locationBias: hasBias ? { type: "Point", coordinates: [longitude, latitude] } : null,
    });

    try {
        const response = await fetch(SEARCH_URL, {
            method: "POST",
            headers: { "Content-Type": "application/json", Accept: "application/json" },
            body,
            signal: controller.signal,
        });

        if (!response.ok) {
            throw new Error(`Search request failed with status ${response.status}.`);
        }

        const data = await response.json();
        return data.map((item) => ({
            streetAddress: item.streetAddress,
            postalCode: item.postalCode ?? null,
            postalTown: item.postalTown ?? null,
            longitude: item.location?.coordinates?.[0] ?? null,
            latitude: item.location?.coordinates?.[1] ?? null,
            score: item.score,
        }));
    } catch (error) {
        // An aborted request was deliberately superseded — not a failure to report.
        if (error.name === "AbortError") {
            return null;
        }
        throw error;
    }
}

export function dispose() {
    controller?.abort();
    controller = null;
}
