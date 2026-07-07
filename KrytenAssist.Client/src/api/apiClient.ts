const apiBaseUrl = 'http://localhost:5005/api';

export async function get<T>(url: string): Promise<T> {
    const response = await fetch(`${apiBaseUrl}${url}`);

    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }

    return response.json() as Promise<T>;
}