// =================================================================
// k6 LOAD TEST - TEST 1: GHI THANG DATABASE (Direct Insert)
// Endpoint: POST /api/listen/track-visit
// Chay: k6 run k6_direct_db.js
// =================================================================

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// --- Custom metrics ---
const errorRate   = new Rate('error_rate');
const responseTime = new Trend('response_time_ms', true);

export const options = {
  // Ramping up: tang dan tu 0 -> 3000 nguoi trong 30 giay
  // Giu o 3000 nguoi trong 30 giay (day la luc Connection Pool bi nghet)
  // Giam dan ve 0 trong 10 giay
  stages: [
    { duration: '20s', target: 1000 },  // Ramp up den 1000 VU (an toan cho local)
    { duration: '30s', target: 1000 },  // Giu o 1000 VU - Connection Pool (100) se nghet tu nhien
    { duration: '10s', target: 0   },   // Ramp down
  ],
  thresholds: {
    'error_rate':        ['rate<0.50'],  // Cho phep loi cao hon vi Direct DB se bi nghet
    'http_req_duration': ['p(95)<10000'],
  },
};

const BASE_URL = 'http://127.0.0.1:5297';

export default function () {
  const vuId    = __VU;   // Virtual User ID (1..3000)
  const iterId  = __ITER; // Lan lap thu may cua VU nay

  const payload = JSON.stringify({
    stallId:  (vuId % 5) + 1,          // Phan tan vao 5 sap khac nhau
    deviceId: `direct-k6-vu${vuId}-it${iterId}`,
    duration: (vuId % 90) + 10,        // Duration tu 10-99 giay
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    timeout: '30s',
  };

  const res = http.post(`${BASE_URL}/api/listen/track-visit`, payload, params);

  // Do luong
  responseTime.add(res.timings.duration);
  errorRate.add(res.status !== 200);

  // Kiem tra
  check(res, {
    'status 200': (r) => r.status === 200,
    'co message': (r) => r.body != null && r.body.includes('message'),
  });

  // KHONG co sleep() - moi VU ban lien tuc het request nay sang request khac
  // Day la dieu kien sat thuc te nhat (nguoi dung den lien tuc)
}

export function handleSummary(data) {
  const avg  = Math.round(data.metrics.http_req_duration.values.avg);
  const p95  = Math.round(data.metrics.http_req_duration.values['p(95)']);
  const p99  = Math.round(data.metrics.http_req_duration.values['p(99)']);
  const max  = Math.round(data.metrics.http_req_duration.values.max);
  const fail = data.metrics.http_req_failed.values.rate * 100;
  const rps  = Math.round(data.metrics.http_reqs.values.rate);

  console.log('\n');
  console.log('================================================================');
  console.log('  KET QUA: GHI THANG DATABASE (Direct Insert)');
  console.log('================================================================');
  console.log(`  Tong requests          : ${data.metrics.http_reqs.values.count}`);
  console.log(`  Ti le loi              : ${fail.toFixed(2)}%`);
  console.log(`  Throughput             : ${rps} req/s`);
  console.log('----------------------------------------------------------------');
  console.log(`  Avg response time      : ${avg} ms`);
  console.log(`  P95 response time      : ${p95} ms   <-- 95% request duoi muc nay`);
  console.log(`  P99 response time      : ${p99} ms   <-- 99% request duoi muc nay`);
  console.log(`  Max response time      : ${max} ms   <-- Cham nhat (bi nghẹt)`);
  console.log('================================================================');
  console.log('  CHAY TIEP: k6 run k6_queue.js de so sanh');
  console.log('================================================================');

  return {};
}
