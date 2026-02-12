// ===== GLOBAL STATE =====
let currentChartView = 'sales'; // 'sales', 'expense', 'profit'
let cachedChartData = null;

// ===== CARD CLICK HANDLER =====
function switchChartView(view) {
    currentChartView = view;

    // Highlight active card
    document.querySelectorAll('.metric-card').forEach(c => c.classList.remove('active'));
    const card = document.getElementById('card-' + view);
    if (card) card.classList.add('active');

    // Re-render both charts with cached data
    if (cachedChartData) {
        renderLineChartByView(cachedChartData);
        renderDoughnutByView(cachedChartData);
    }
}

// ===== LINE CHART RENDERER =====
function renderLineChartByView(data) {
    if (!data.revenueTrend || !window._revenueTrendChart) return;

    const labels = data.revenueTrend.labels || [];
    const datasets = data.revenueTrend.datasets || [];
    const revenueValues = datasets[0]?.data || [];
    const expenseValues = datasets[1]?.data || [];
    const profitValues = datasets[2]?.data || [];

    let chartDatasets = [];
    let title = 'Total Sales Trend';

    if (currentChartView === 'sales') {
        title = 'Total Sales Trend';
        chartDatasets = [{
            label: 'Total Sales',
            data: revenueValues,
            borderColor: '#A36A66',
            backgroundColor: 'rgba(163, 106, 102, 0.15)',
            fill: true, tension: 0.4, borderWidth: 3, pointRadius: 4,
            pointBackgroundColor: '#A36A66'
        }];
    } else if (currentChartView === 'expense') {
        title = 'Total Expense Trend';
        chartDatasets = [{
            label: 'Total Expense',
            data: expenseValues,
            borderColor: '#e74c3c',
            backgroundColor: 'rgba(231, 76, 60, 0.15)',
            fill: true, tension: 0.4, borderWidth: 3, pointRadius: 4,
            pointBackgroundColor: '#e74c3c'
        }];
    } else if (currentChartView === 'profit') {
        title = 'Net Profit Trend';
        chartDatasets = [{
            label: 'Net Profit',
            data: profitValues,
            borderColor: '#27ae60',
            backgroundColor: 'rgba(39, 174, 96, 0.15)',
            fill: true, tension: 0.4, borderWidth: 3, pointRadius: 4,
            pointBackgroundColor: '#27ae60'
        }];
    }

    const titleEl = document.getElementById('mainChartTitle');
    if (titleEl) titleEl.textContent = title;

    window._revenueTrendChart.data.labels = labels;
    window._revenueTrendChart.data.datasets = chartDatasets;
    window._revenueTrendChart.update();
}

// ===== DOUGHNUT CHART RENDERER =====
function renderDoughnutByView(data) {
    if (!window._expenseBreakdownChart) return;

    const doughnutTitle = document.getElementById('doughnutChartTitle');

    if (currentChartView === 'sales') {
        // Show: Revenue breakdown — use revenue per period as slices
        const labels = data.revenueTrend?.labels || [];
        const values = data.revenueTrend?.datasets?.[0]?.data || [];
        // Filter out zero-value months for cleaner doughnut
        const filtered = labels.map((l, i) => ({ label: l, value: Number(values[i]) })).filter(x => x.value > 0);

        if (doughnutTitle) doughnutTitle.textContent = 'Sales Distribution';

        if (filtered.length === 0) {
            window._expenseBreakdownChart.data.labels = ['No Sales Data'];
            window._expenseBreakdownChart.data.datasets[0].data = [1];
            window._expenseBreakdownChart.data.datasets[0].backgroundColor = ['#d1d5db'];
        } else {
            window._expenseBreakdownChart.data.labels = filtered.map(x => x.label);
            window._expenseBreakdownChart.data.datasets[0].data = filtered.map(x => x.value);
            window._expenseBreakdownChart.data.datasets[0].backgroundColor = generateColors(filtered.length);
        }

    } else if (currentChartView === 'expense') {
        // Show: Expense breakdown by type (original doughnut data)
        if (doughnutTitle) doughnutTitle.textContent = 'Expense Breakdown';

        const labels = data.expenseBreakdown?.labels || [];
        const values = data.expenseBreakdown?.datasets?.[0]?.data || [];

        if (labels.length === 0) {
            window._expenseBreakdownChart.data.labels = ['No Expense Data'];
            window._expenseBreakdownChart.data.datasets[0].data = [1];
            window._expenseBreakdownChart.data.datasets[0].backgroundColor = ['#d1d5db'];
        } else {
            window._expenseBreakdownChart.data.labels = labels;
            window._expenseBreakdownChart.data.datasets[0].data = values;
            window._expenseBreakdownChart.data.datasets[0].backgroundColor = generateColors(labels.length);
        }

    } else if (currentChartView === 'profit') {
        // Show: Profit vs Expense pie
        if (doughnutTitle) doughnutTitle.textContent = 'Profit vs Expense';

        const totalRev = Number(data.summary?.totalRevenue || 0);
        const totalExp = Number(data.summary?.totalExpense || 0);
        const totalProfit = totalRev - totalExp;

        if (totalRev === 0 && totalExp === 0) {
            window._expenseBreakdownChart.data.labels = ['No Data'];
            window._expenseBreakdownChart.data.datasets[0].data = [1];
            window._expenseBreakdownChart.data.datasets[0].backgroundColor = ['#d1d5db'];
        } else {
            window._expenseBreakdownChart.data.labels = ['Net Profit', 'Total Expense'];
            window._expenseBreakdownChart.data.datasets[0].data = [Math.max(0, totalProfit), totalExp];
            window._expenseBreakdownChart.data.datasets[0].backgroundColor = ['#27ae60', '#e74c3c'];
        }
    }

    window._expenseBreakdownChart.update();
}

// ===== COLOR GENERATOR =====
function generateColors(count) {
    const base = ['#A36A66', '#FFA593', '#d3b5b3', '#bfa8a7', '#8a5a57', '#fcd5ce',
        '#e74c3c', '#27ae60', '#3498db', '#f39c12', '#8e44ad', '#1abc9c'];
    let colors = [];
    for (let i = 0; i < count; i++) {
        colors.push(base[i % base.length]);
    }
    return colors;
}

// ===== MAIN INIT =====
document.addEventListener('DOMContentLoaded', function () {
    console.log('🚀 Dashboard.js loaded');

    // Wire up card clicks
    document.getElementById('card-sales')?.addEventListener('click', () => switchChartView('sales'));
    document.getElementById('card-expense')?.addEventListener('click', () => switchChartView('expense'));
    document.getElementById('card-profit')?.addEventListener('click', () => switchChartView('profit'));

    const primaryColor = '#A36A66';
    const mutedColor = '#f0f0f0';

    let revenueTrendChart;
    let expenseBreakdownChart;

    // --- Date Range Display ---
    function updateDateRangeDisplay(period, startDate = null, endDate = null) {
        const el = document.getElementById('currentDateRange');
        if (!el) return;
        const now = new Date();
        let text = '';
        if (period === 'custom' && startDate && endDate) {
            const s = new Date(startDate); const e = new Date(endDate);
            text = `${s.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} - ${e.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
        } else {
            switch (period) {
                case 'week':
                    const ws = new Date(now); const dw = now.getDay();
                    ws.setDate(now.getDate() + ((dw === 0 ? -6 : 1) - dw));
                    text = `${ws.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${now.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
                    break;
                case 'month': text = now.toLocaleDateString('en-US', { month: 'long', year: 'numeric' }); break;
                case 'year': text = now.getFullYear().toString(); break;
                case 'alltime': text = 'All Time'; break;
                default: text = '';
            }
        }
        el.textContent = text;
    }

    // --- Init Line Chart ---
    const lineCtx = document.getElementById('profitTrendChart')?.getContext('2d');
    if (lineCtx) {
        revenueTrendChart = new Chart(lineCtx, {
            type: 'line',
            data: { labels: [], datasets: [] },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        mode: 'index', intersect: false,
                        callbacks: { label: (ctx) => `${ctx.dataset.label}: ₱${Number(ctx.raw).toLocaleString('en-PH', { minimumFractionDigits: 2 })}` }
                    }
                },
                scales: {
                    y: { beginAtZero: true, title: { display: true, text: 'Amount (₱)', color: '#6c757d' }, grid: { color: mutedColor }, ticks: { callback: v => '₱' + Number(v).toLocaleString('en-PH') } },
                    x: { grid: { display: false } }
                }
            }
        });
        window._revenueTrendChart = revenueTrendChart;
    }

    // --- Init Doughnut Chart ---
    const doughCtx = document.getElementById('expenseBreakdownChart')?.getContext('2d');
    if (doughCtx) {
        expenseBreakdownChart = new Chart(doughCtx, {
            type: 'doughnut',
            data: { labels: [], datasets: [{ label: 'Share', data: [], backgroundColor: [], borderWidth: 1, borderColor: '#fff', hoverOffset: 8 }] },
            options: {
                responsive: true, maintainAspectRatio: false, cutout: '70%',
                plugins: {
                    legend: { position: 'right' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                let l = ctx.label || ''; if (l) l += ': ';
                                const total = ctx.chart.data.datasets[0].data.reduce((a, b) => a + b, 0);
                                const pct = total > 0 ? ((ctx.parsed / total) * 100).toFixed(1) : 0;
                                return ` ${l}₱${ctx.parsed.toFixed(2)} (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
        window._expenseBreakdownChart = expenseBreakdownChart;
    }

    // --- Fetch & Update ---
    async function updateCharts(period, startDate = null, endDate = null) {
        try {
            let url = `/Sales_Finance/GetChartData?period=${period}`;
            if (period === 'custom' && startDate && endDate) url += `&startDate=${startDate}&endDate=${endDate}`;

            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            if (!data) return;

            cachedChartData = data;
            updateDateRangeDisplay(period, startDate, endDate);

            // Update summary card values
            if (data.summary) {
                const fmt = v => `₱${v.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ",")}`;
                const re = document.getElementById('totalRevenue');
                const ex = document.getElementById('totalExpense');
                const pr = document.getElementById('netProfit');
                if (re) re.textContent = fmt(data.summary.totalRevenue);
                if (ex) ex.textContent = fmt(data.summary.totalExpense);
                if (pr) pr.textContent = fmt(data.summary.netProfit);
            }

            // Render both charts based on current view
            renderLineChartByView(data);
            renderDoughnutByView(data);

        } catch (err) { console.error("❌ Error:", err); }
    }

    // --- Period Dropdown ---
    const ps = document.getElementById('timePeriodSelect');
    const cdr = document.getElementById('customDateRange');
    const sd = document.getElementById('startDate');
    const ed = document.getElementById('endDate');
    const ab = document.getElementById('applyDateRange');

    if (ps) {
        ps.addEventListener('change', e => {
            const v = e.target.value;
            cdr.classList.toggle('active', v === 'custom');
            if (v === 'custom') {
                const t = new Date(); const ago = new Date(); ago.setDate(t.getDate() - 30);
                sd.value = ago.toISOString().split('T')[0]; ed.value = t.toISOString().split('T')[0];
            } else { updateCharts(v); }
        });

        if (ab) ab.addEventListener('click', () => {
            if (sd.value && ed.value && new Date(sd.value) <= new Date(ed.value)) updateCharts('custom', sd.value, ed.value);
        });

        updateCharts(ps.value);
    }
});