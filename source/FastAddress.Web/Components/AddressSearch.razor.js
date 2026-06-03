// Browser-side search client. Owns a single AbortController so each new query cancels the
// previous in-flight request. Talks to the same-origin proxy at /api/addresses/search.

const SEARCH_URL = "/api/addresses/search";
const RESULT_LIMIT = 5;
const PROBLEM_JSON = "application/problem+json";
const FALLBACK_TITLE = "Address search failed";

let controller = null;

// Returns a SearchOutcome: { results } on success, { error } on an HTTP or network failure,
// or null when the request was superseded and aborted. HTTP failures are returned, not thrown,
// so the caller surfaces the problem-details title and detail instead of a JS stack trace.
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

    let response;
    try {
        response = await fetch(SEARCH_URL, {
            method: "POST",
            headers: { "Content-Type": "application/json", Accept: "application/json" },
            body,
            signal: controller.signal,
        });
    } catch (error) {
        // An aborted request was deliberately superseded. This isn't a failure to report.
        if (error.name === "AbortError") {
            return null;
        }
        // The request never reached the server (offline, DNS, blocked). Report a clean message.
        return {
            error: {
                title: FALLBACK_TITLE,
                detail: "Could not reach the address service. Check your connection and try again.",
            },
        };
    }

    if (!response.ok) {
        return { error: await readProblemDetails(response) };
    }

    const data = await response.json();
    return {
        results: data.map((item) => ({
            streetAddress: item.streetAddress,
            postalCode: item.postalCode ?? null,
            postalTown: item.postalTown ?? null,
            longitude: item.location?.coordinates?.[0] ?? null,
            latitude: item.location?.coordinates?.[1] ?? null,
            score: item.score,
        })),
    };
}

// Reads an RFC 7807 problem-details body, preserving every member it carries (status, type,
// instance, traceId, and any extensions) and only filling in a title or detail when absent.
// Falls back to the HTTP status when the response is not problem+json or the body cannot be parsed.
async function readProblemDetails(response) {
    const contentType = response.headers.get("Content-Type") ?? "";
    if (contentType.includes(PROBLEM_JSON)) {
        try {
            const problem = await response.json();
            return {
                ...problem,
                title: problem.title ?? FALLBACK_TITLE,
                detail: problem.detail ?? statusDetail(response.status),
            };
        } catch {
            // Body was missing or not valid JSON. Fall through to the status-based message.
        }
    }
    return { status: response.status, title: FALLBACK_TITLE, detail: statusDetail(response.status) };
}

function statusDetail(status) {
    return `The address service responded with status ${status}.`;
}

export function dispose() {
    controller?.abort();
    controller = null;
}
