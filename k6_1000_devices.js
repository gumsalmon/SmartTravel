// =================================================================
// AUTOMATION TEST - 1000 THIET BI DOC LAP GHI LOG
// =================================================================

import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    // Kich ban: Chinh xac 1000 nguoi (VU), moi nguoi chay dung 1 lan (1 iteration)
    exact_1000_devices: {
      executor: 'per-vu-iterations',
      vus: 1000,
      iterations: 1,
      maxDuration: '30s',
    },
  },
};

const BASE_URL = 'http://127.0.0.1:5297';

export default function () {
  const vuId = __VU; // ID cua thiet bi tu 1 -> 1000
  const now = new Date().toISOString();
  
  // Tao thong tin rieng biet cho tung thiet bi
  const deviceId = `device-auto-test-${vuId}`;
  
  const payload = JSON.stringify({
    stallId:               (vuId % 5) + 1,
    deviceId:              deviceId,
    visitedAt:             now,
    createdAtServer:       now,
    listenDurationSeconds: 45, // Nghe 45 giay
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    timeout: '10s',
  };

  // Gui vao Hang Doi (Queue)
  const res = http.post(`${BASE_URL}/api/analytics/stall-visit`, payload, params);

  const isSuccess = res.status === 200;
  
  check(res, {
    'status 200': (r) => r.status === 200,
  });

  // GHI LOG: Dong nay se duoc xuat ra file .log
  if (isSuccess) {
    console.log(`[SUCCESS] Thiet bi: ${deviceId} | Thoi gian phan hoi: ${res.timings.duration}ms | Payload: OK`);
  } else {
    console.error(`[FAILED] Thiet bi: ${deviceId} | Loi HTTP: ${res.status} | Chi tiet: ${res.body}`);
  }
}
