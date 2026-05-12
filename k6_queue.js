// =================================================================
// k6 LOAD TEST - TEST 2: HANG DOI RAM (Queue)
// Endpoint: POST /api/analytics/stall-visit
// Chay: k6 run k6_queue.js
// =================================================================

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// --- Custom metrics ---
const errorRate    = new Rate('error_rate');
const responseTime = new Trend('response_time_ms', true);

export const options = {
  // Cung dieu kien chinh xac nhu k6_direct_db.js de so sanh cong bang
  stages: [
    { duration: '20s', target: 1000 },
    { duration: '30s', target: 1000 },
    { duration: '10s', target: 0   },
  ],
  thresholds: {
    'error_rate':        ['rate<0.01'],
    'http_req_duration': ['p(95)<500'],
  },
};

const BASE_URL = 'http://127.0.0.1:5297';

export default function () {
  const vuId   = __VU;
  const iterId = __ITER;

  const now = new Date().toISOString();
  const dur = (vuId % 90) + 10;

  // Khong gui truong 'id' - de server tu sinh Guid.NewGuid() theo default value
  // Neu gui sai dinh dang GUID, ASP.NET Core se tra 400 Bad Request ngay lap tuc
  const payload = JSON.stringify({
    stallId:               (vuId % 5) + 1,
    deviceId:              `queue-k6-vu${vuId}-it${iterId}`,
    visitedAt:             now,
    createdAtServer:       now,
    listenDurationSeconds: dur,
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    timeout: '10s',  // Queue chi can 10s timeout, khong can 30s nhu Direct
  };

  const res = http.post(`${BASE_URL}/api/analytics/stall-visit`, payload, params);

  responseTime.add(res.timings.duration);
  errorRate.add(res.status !== 200);

  check(res, {
    'status 200':  (r) => r.status === 200,
    'co message':  (r) => r.body != null && r.body.includes('message'),
    'duoi 500ms':  (r) => r.timings.duration < 500,
  });

  // Giai lap thuc te: moi nguoi nghe xong moi gui 1 request tracking
  // Khong co nguoi that nao ban 1000 request/giay lien tuc
  sleep(0.5); // Moi VU nghi 0.5 giay giua cac request
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
  console.log('  KET QUA: HANG DOI RAM (Queue)');
  console.log('================================================================');
  console.log(`  Tong requests          : ${data.metrics.http_reqs.values.count}`);
  console.log(`  Ti le loi              : ${fail.toFixed(2)}%`);
  console.log(`  Throughput             : ${rps} req/s`);
  console.log('----------------------------------------------------------------');
  console.log(`  Avg response time      : ${avg} ms`);
  console.log(`  P95 response time      : ${p95} ms`);
  console.log(`  P99 response time      : ${p99} ms`);
  console.log(`  Max response time      : ${max} ms`);
  console.log('================================================================');
  console.log('  SAO CHEP KET QUA 2 TEST LAI VA SO SANH');
  console.log('================================================================');

  return {};
}
