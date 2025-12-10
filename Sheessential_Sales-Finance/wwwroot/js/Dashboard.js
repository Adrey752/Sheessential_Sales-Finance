document.addEventListener('DOMContentLoaded', function () {

    // --- Shared Color Variables ---
    const primaryColor = '#A36A66';
    const secondaryColor = '#FFA593';
    const mutedColor = '#f0f0f0';

    const baseColors = [
        primaryColor,
        secondaryColor,
        '#d3b5b3',
        '#bfa8a7',
        '#8a5a57',
        '#fcd5ce'
    ];

    const generateDoughnutColors = (count) => {
        let colors = [];
        for (let i = 0; i < count; i++) {
            colors.push(baseColors[i % baseColors.length]);
        }
        return colors;
    };

    let revenueTrendChart;
    let expenseBreakdownChart;

    // ------------------------------------------------------------
    // 1. Init Revenue Trend Chart
    // ------------------------------------------------------------
    const revenueTrendCtx = document
        .getElementById('profitTrendChart')
        .getContext('2d');

    revenueTrendChart = new Chart(revenueTrendCtx, {
        type: 'line',
        data: { labels: [], datasets: [] },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom' },
                tooltip: { mode: 'index', intersect: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: { display: true, text: 'Amount (₱)', color: '#6c757d' },
                    grid: { color: mutedColor }
                },
                x: { grid: { display: false } }
            }
        }
    });

    // ------------------------------------------------------------
    // 2. Init Expense Breakdown Chart
    // ------------------------------------------------------------
    const expenseBreakdownCtx = document
        .getElementById('expenseBreakdownChart')
        .getContext('2d');

    expenseBreakdownChart = new Chart(expenseBreakdownCtx, {
        type: 'doughnut',
        data: {
            labels: [],
            datasets: [{
                label: 'Expense Share',
                data: [],
                backgroundColor: [],
                borderWidth: 1,
                borderColor: '#ffffff',
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '70%',
            plugins: {
                legend: { position: 'right' },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.label || '';
                            if (label) label += ': ';
                            const total = context.chart.data.datasets[0].data.reduce((a, b) => a + b, 0);
                            const value = context.parsed;
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                            return ` ${label}₱${value.toFixed(2)} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });

    // ------------------------------------------------------------
    // 3. Fetch + Update charts
    // ------------------------------------------------------------
    async function updateCharts(period) {
        try {
            const response = await fetch(`/Sales_Finance/GetChartData?period=${period}`);
            if (!response.ok) throw new Error("Chart data fetch failed.");

            const data = await response.json();

            // --- Update Revenue Trend ---
            revenueTrendChart.data.labels = data.revenueTrend.labels;
            revenueTrendChart.data.datasets = data.revenueTrend.datasets.map((ds, i) => {
                const color = baseColors[i % baseColors.length];
                return {
                    ...ds,
                    borderColor: color,
                    backgroundColor: "rgba(163, 106, 102, 0.1)",
                    borderWidth: 2,
                    tension: 0.4,
                    fill: false,
                    pointRadius: 4,
                    pointBackgroundColor: color
                };
            });
            revenueTrendChart.update();

            // --- Update Expense Breakdown ---
            const labels = data.expenseBreakdown.labels;
            const values = data.expenseBreakdown.datasets[0].data;

            expenseBreakdownChart.data.labels = labels;
            expenseBreakdownChart.data.datasets[0].data = values;
            expenseBreakdownChart.data.datasets[0].backgroundColor =
                generateDoughnutColors(labels.length);

            expenseBreakdownChart.update();

        } catch (err) {
            console.error("Error updating charts:", err);
        }
    }

    // ------------------------------------------------------------
    // 4. Time period dropdown
    // ------------------------------------------------------------
    const periodSelect = document.getElementById('timePeriodSelect');
    periodSelect.addEventListener('change', e => {
        updateCharts(e.target.value);
    });

    updateCharts(periodSelect.value);
});
