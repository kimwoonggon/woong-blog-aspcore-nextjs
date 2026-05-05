import http from 'k6/http';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const baseUrl = (__ENV.BASE_URL || 'http://nginx').replace(/\/+$/, '');
const scenario = __ENV.SCENARIO || 'constant';
const rate = Number(__ENV.RATE || '300');
const peakRate = Number(__ENV.PEAK_RATE || '1000');
const duration = __ENV.DURATION || '30s';
const preAllocatedVUs = Number(__ENV.PRE_ALLOCATED_VUS || '300');
const maxVUs = Number(__ENV.MAX_VUS || '1000');
const listPageSize = Number(__ENV.LIST_PAGE_SIZE || '12');
const workReadPath = __ENV.WORK_READ_PATH || '';
const studyReadPath = __ENV.STUDY_READ_PATH || '';

const listTargets = [
  { key: 'work_list', label: 'Work list', path: `/api/public/works?page=1&pageSize=${listPageSize}` },
  { key: 'study_list', label: 'Study list', path: `/api/public/blogs?page=1&pageSize=${listPageSize}` },
];

const readTargets = [
  workReadPath ? { key: 'work_read', label: 'Work read', path: workReadPath } : null,
  studyReadPath ? { key: 'study_read', label: 'Study read', path: studyReadPath } : null,
].filter(Boolean);

const targets = [...listTargets, ...readTargets];

const failures = new Counter('http_failures_total');
const successRate = new Rate('http_success_rate');
const workListDuration = new Trend('target_work_list_duration', true);
const workReadDuration = new Trend('target_work_read_duration', true);
const studyListDuration = new Trend('target_study_list_duration', true);
const studyReadDuration = new Trend('target_study_read_duration', true);
const trends = {
  work_list: workListDuration,
  work_read: workReadDuration,
  study_list: studyListDuration,
  study_read: studyReadDuration,
};

const scenarioOptions = scenario === 'spike'
  ? {
      executor: 'ramping-arrival-rate',
      timeUnit: '1s',
      preAllocatedVUs,
      maxVUs,
      stages: [
        { target: rate, duration: '10s' },
        { target: peakRate, duration: duration },
        { target: rate, duration: '10s' },
      ],
    }
  : {
      executor: 'constant-arrival-rate',
      rate,
      timeUnit: '1s',
      duration,
      preAllocatedVUs,
      maxVUs,
    };

export const options = {
  discardResponseBodies: true,
  scenarios: { public_api: scenarioOptions },
  thresholds: {
    http_req_failed: ['rate<0.001'],
  },
};

export default function runPublicApiScenario() {
  const target = targets[__ITER % targets.length];
  const res = http.get(`${baseUrl}${target.path}`, { tags: { target: target.key } });
  const ok = res.status >= 200 && res.status < 300;

  successRate.add(ok, { target: target.key });
  if (!ok) {
    failures.add(1, { target: target.key, status: String(res.status) });
  }

  trends[target.key].add(res.timings.duration);
  check(res, { 'status is 2xx': () => ok }, { target: target.key });
}

function metricValue(data, name, stat) {
  const metric = data.metrics[name];
  return metric && metric.values ? metric.values[stat] : null;
}

export function handleSummary(data) {
  const summary = {
    scenario,
    rate,
    peakRate,
    duration,
    preAllocatedVUs,
    maxVUs,
    listPageSize,
    http_reqs: metricValue(data, 'http_reqs', 'count'),
    rps: metricValue(data, 'http_reqs', 'rate'),
    failed_rate: metricValue(data, 'http_req_failed', 'rate'),
    duration_p50_ms: metricValue(data, 'http_req_duration', 'med'),
    duration_p95_ms: metricValue(data, 'http_req_duration', 'p(95)'),
    duration_p99_ms: metricValue(data, 'http_req_duration', 'p(99)'),
    duration_max_ms: metricValue(data, 'http_req_duration', 'max'),
    dropped_iterations: metricValue(data, 'dropped_iterations', 'count') || 0,
    vus_max: metricValue(data, 'vus_max', 'max'),
    targets: Object.fromEntries(targets.map((target) => [
      target.key,
      {
        label: target.label,
        path: target.path,
        p95_ms: metricValue(data, `target_${target.key}_duration`, 'p(95)'),
        p99_ms: metricValue(data, `target_${target.key}_duration`, 'p(99)'),
      },
    ])),
    note: 'Read targets are intentionally not seeded by default. Provide WORK_READ_PATH and STUDY_READ_PATH from the current public content selected by the Real Backend Test UI when using this standalone script.',
  };

  return {
    stdout: JSON.stringify(summary, null, 2) + '\n',
    '/tmp/public-api-k6-summary.json': JSON.stringify(summary, null, 2),
  };
}
