// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.initAnalyticsDashboard = async function initAnalyticsDashboard() {
    const mapContainer = document.getElementById("analyticsMap");
    if (!mapContainer || typeof L === "undefined") {
        return;
    }

    const API = {
        stalls: "/api/stalls/nearby",
        heatmap: "/api/analytics/heatmap",
        route: (deviceId) => `/api/analytics/route/${encodeURIComponent(deviceId)}`,
        avgListenTime: "/api/analytics/avg-listen-time"
    };

    let map = null;
    let stallLayerGroup = null;
    let heatLayer = null;
    let routePolyline = null;
    let routeStartMarker = null;
    let routeEndMarker = null;
    let avgListenTimeChart = null;

    const selectors = {
        modeInputs: document.querySelectorAll('input[name="mapMode"]'),
        deviceInput: document.getElementById("deviceIdInput"),
        drawRouteBtn: document.getElementById("drawRouteBtn"),
        clearRouteBtn: document.getElementById("clearRouteBtn"),
        chartCanvas: document.getElementById("avgListenTimeChart")
    };

    function normalizePoint(item) {
        const lat = Number(item.latitude ?? item.lat);
        const lng = Number(item.longitude ?? item.lng);
        return { lat, lng, name: item.name ?? item.stallName ?? "Không rõ tên" };
    }

    async function fetchJson(url) {
        try {
            const response = await fetch(url, { method: "GET" });
            if (!response.ok) throw new Error(`HTTP ${response.status} at ${url}`);
            return await response.json();
        } catch (error) {
            console.error(`[AnalyticsDashboard] Fetch failed: ${url}`, error);
            return [];
        }
    }

    async function loadStallMarkers() {
        const raw = await fetchJson(API.stalls);
        const points = Array.isArray(raw) ? raw.map(normalizePoint).filter(p => !Number.isNaN(p.lat) && !Number.isNaN(p.lng)) : [];
        if (!stallLayerGroup) stallLayerGroup = L.layerGroup().addTo(map);
        stallLayerGroup.clearLayers();
        points.forEach(point => L.marker([point.lat, point.lng]).bindPopup(`<strong>${point.name}</strong>`).addTo(stallLayerGroup));
    }

    async function loadHeatmapLayer() {
        const raw = await fetchJson(API.heatmap);
        const heatPoints = Array.isArray(raw)
            ? raw.map(normalizePoint).filter(p => !Number.isNaN(p.lat) && !Number.isNaN(p.lng)).map(p => [p.lat, p.lng, 0.9])
            : [];

        if (heatLayer) map.removeLayer(heatLayer);
        heatLayer = L.heatLayer(heatPoints, { blur: 15, maxZoom: 17, radius: 25 }).addTo(map);
    }

    async function switchMapMode(mode) {
        if (mode === "heatmap") {
            if (stallLayerGroup) map.removeLayer(stallLayerGroup);
            await loadHeatmapLayer();
        } else {
            if (heatLayer) {
                map.removeLayer(heatLayer);
                heatLayer = null;
            }
            if (stallLayerGroup && !map.hasLayer(stallLayerGroup)) stallLayerGroup.addTo(map);
            await loadStallMarkers();
        }
    }

    function clearRouteOverlay() {
        if (routePolyline) { map.removeLayer(routePolyline); routePolyline = null; }
        if (routeStartMarker) { map.removeLayer(routeStartMarker); routeStartMarker = null; }
        if (routeEndMarker) { map.removeLayer(routeEndMarker); routeEndMarker = null; }
    }

    async function drawRouteByDeviceId() {
        const deviceId = selectors.deviceInput?.value?.trim();
        if (!deviceId) {
            console.error("[AnalyticsDashboard] Device ID is empty.");
            return;
        }

        const raw = await fetchJson(API.route(deviceId));
        const points = Array.isArray(raw) ? raw.map(normalizePoint).filter(p => !Number.isNaN(p.lat) && !Number.isNaN(p.lng)) : [];
        clearRouteOverlay();
        if (points.length < 2) {
            console.error(`[AnalyticsDashboard] Route data is insufficient for device: ${deviceId}`);
            return;
        }

        const polylineData = points.map(p => [p.lat, p.lng]);
        routePolyline = L.polyline(polylineData, { color: "#FF5722", weight: 5, opacity: 0.8 }).addTo(map);
        routeStartMarker = L.marker(polylineData[0]).addTo(map).bindPopup("Điểm Bắt Đầu");
        routeEndMarker = L.marker(polylineData[polylineData.length - 1]).addTo(map).bindPopup("Điểm Kết Thúc");
        map.fitBounds(routePolyline.getBounds(), { padding: [40, 40] });
    }

    async function renderAvgListenTimeChart() {
        const raw = await fetchJson(API.avgListenTime);
        const rows = Array.isArray(raw) ? raw : [];
        const labels = rows.map(x => x.stallName ?? x.name ?? "Không rõ");
        const values = rows.map(x => Number(x.avgListenDurationSeconds ?? x.avgListenTimeSeconds ?? 0));

        if (avgListenTimeChart) avgListenTimeChart.destroy();
        avgListenTimeChart = new Chart(selectors.chartCanvas, {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: "Thời gian nghe trung bình (giây)",
                    data: values,
                    backgroundColor: ["#4F46E5", "#0EA5E9", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899", "#14B8A6", "#F97316", "#64748B"],
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    tooltip: { callbacks: { label: (ctx) => `${ctx.parsed.y} giây` } },
                    legend: { display: false }
                },
                scales: {
                    y: { beginAtZero: true, title: { display: true, text: "Giây" } },
                    x: { title: { display: true, text: "Tên Sạp" } }
                }
            }
        });
    }

    map = L.map(mapContainer).setView([10.762, 106.703], 16);
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap contributors"
    }).addTo(map);

    selectors.modeInputs.forEach(input => {
        input.addEventListener("change", async (event) => {
            await switchMapMode(event.target.value);
        });
    });
    selectors.drawRouteBtn?.addEventListener("click", async () => {
        await drawRouteByDeviceId();
    });
    selectors.clearRouteBtn?.addEventListener("click", clearRouteOverlay);

    await switchMapMode("stall");
    await renderAvgListenTimeChart();
};

window.initDashboardCharts = async function initDashboardCharts() {
    const canvas = document.getElementById("listenTimeChart");
    if (!canvas || typeof Chart === "undefined") {
        return;
    }

    try {
        const response = await fetch("/api/analytics/avg-listen-time", { method: "GET" });
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const rows = await response.json();
        const labels = (rows || []).map(x => x.stallName ?? "Không rõ");
        const values = (rows || []).map(x => Number(x.avgListenDurationSeconds ?? 0));

        if (window._listenTimeChartInstance) {
            window._listenTimeChartInstance.destroy();
        }

        window._listenTimeChartInstance = new Chart(canvas, {
            type: "bar",
            data: {
                labels,
                datasets: [{
                    label: "Giây trung bình",
                    data: values,
                    backgroundColor: "#0d6efd",
                    borderRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true }
                },
                scales: {
                    x: {
                        title: { display: true, text: "Tên Sạp" }
                    },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Giây trung bình" }
                    }
                }
            }
        });
    } catch (error) {
        console.error("[Dashboard] initDashboardCharts failed:", error);
    }
};
