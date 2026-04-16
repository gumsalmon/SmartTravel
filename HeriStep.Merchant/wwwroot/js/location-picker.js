// Hàm dùng chung để khởi tạo bản đồ chọn tọa độ (Leaflet)
function initLocationPicker(mapContainerId, latInputId, lngInputId, gpsButtonId) {
    const defaultLat = 10.7618;
    const defaultLng = 106.7040;

    // Khởi tạo bản đồ
    const map = L.map(mapContainerId).setView([defaultLat, defaultLng], 18);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    const marker = L.marker([defaultLat, defaultLng], { draggable: true }).addTo(map);

    const latInput = document.getElementById(latInputId);
    const lngInput = document.getElementById(lngInputId);

    // Hàm cập nhật input
    function updateInputs(lat, lng) {
        if (latInput) latInput.value = parseFloat(lat).toFixed(6);
        if (lngInput) lngInput.value = parseFloat(lng).toFixed(6);
    }

    // Set giá trị mặc định ban đầu
    updateInputs(defaultLat, defaultLng);

    // Cập nhật khi kéo thả marker
    marker.on('dragend', function () {
        const p = marker.getLatLng();
        updateInputs(p.lat, p.lng);
    });

    // Cập nhật khi click trên bản đồ
    map.on('click', function (e) {
        marker.setLatLng(e.latlng);
        updateInputs(e.latlng.lat, e.latlng.lng);
    });

    // Sự kiện nút lấy GPS tự động
    if (gpsButtonId) {
        const btnGPS = document.getElementById(gpsButtonId);
        if (btnGPS) {
            btnGPS.addEventListener('click', function (e) {
                e.preventDefault();

                const oldText = btnGPS.innerHTML;
                btnGPS.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang tải...';
                btnGPS.disabled = true;

                if (navigator.geolocation) {
                    navigator.geolocation.getCurrentPosition(
                        (position) => {
                            const lat = position.coords.latitude;
                            const lng = position.coords.longitude;

                            // Trượt bản đồ tới điểm GPS
                            map.flyTo([lat, lng], 18);
                            marker.setLatLng([lat, lng]);
                            updateInputs(lat, lng);

                            if (typeof Swal !== 'undefined') {
                                Swal.fire({ icon: "success", title: "Thành công", text: "Đã lấy được tọa độ chuẩn xác!", timer: 2000, showConfirmButton: false });
                            }
                            btnGPS.disabled = false;
                            btnGPS.innerHTML = oldText;
                        },
                        (error) => {
                            if (typeof Swal !== 'undefined') {
                                Swal.fire("Lỗi GPS", "Không thể lấy được vị trí. Vui lòng cấp quyền truy cập vị trí!", "error");
                            }
                            btnGPS.disabled = false;
                            btnGPS.innerHTML = oldText;
                        },
                        { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
                    );
                } else {
                    if (typeof Swal !== 'undefined') Swal.fire("Lỗi", "Trình duyệt không hỗ trợ Geolocation!", "error");
                    btnGPS.disabled = false;
                    btnGPS.innerHTML = oldText;
                }
            });
        }
    }
    return map;
}