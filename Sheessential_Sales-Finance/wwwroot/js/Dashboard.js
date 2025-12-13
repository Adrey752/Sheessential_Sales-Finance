document.addEventListener('DOMContentLoaded', function () {
    console.log('🚀 Dashboard.js loaded');

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

    // --- Date Range Display Helper ---
    function updateDateRangeDisplay(period, startDate = null, endDate = null) {
        const displayElement = document.getElementById('currentDateRange');
        if (!displayElement) return;

        const now = new Date();
        let displayText = '';

        if (period === 'custom' && startDate && endDate) {
            const start = new Date(startDate);
            const end = new Date(endDate);
            displayText = `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} - ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
        } else {
            switch (period) {
                case 'week':
                    const weekStart = new Date(now);
                    const dayOfWeek = now.getDay();
                    const diffToMonday = (dayOfWeek === 0 ? -6 : 1) - dayOfWeek;
                    weekStart.setDate(now.getDate() + diffToMonday);
                    displayText = `${weekStart.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${now.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
                    break;
                case 'month':
                    displayText = now.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
                    break;
                case 'year':
                    displayText = now.getFullYear().toString();
                    break;
                case 'alltime':
                    displayText = 'All Time';
                    break;
                default:
                    displayText = 'Unknown Period';
            }
        }

        displayElement.textContent = displayText;
    }

    // ------------------------------------------------------------
    // 1. Init Revenue Trend Chart
    // ------------------------------------------------------------
    const revenueTrendCtx = document.getElementById('profitTrendChart')?.getContext('2d');

    if (revenueTrendCtx) {
        console.log('✅ Revenue chart canvas found');
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
    } else {
        console.error('❌ profitTrendChart canvas not found');
    }

    // ------------------------------------------------------------
    // 2. Init Expense Breakdown Chart
    // ------------------------------------------------------------
    const expenseBreakdownCtx = document.getElementById('expenseBreakdownChart')?.getContext('2d');

    if (expenseBreakdownCtx) {
        console.log('✅ Expense chart canvas found');
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
    } else {
        console.error('❌ expenseBreakdownChart canvas not found');
    }

    // ------------------------------------------------------------
    // 3. Fetch + Update charts
    // ------------------------------------------------------------
    async function updateCharts(period, startDate = null, endDate = null) {
        try {
            console.log('📊 Fetching chart data for period:', period);

            let url = `/Sales_Finance/GetChartData?period=${period}`;
            if (period === 'custom' && startDate && endDate) {
                url += `&startDate=${startDate}&endDate=${endDate}`;
            }

            const response = await fetch(url);

            console.log('📡 Response status:', response.status);

            if (!response.ok) {
                console.error('❌ HTTP Error:', response.status);
                throw new Error(`Chart data fetch failed with status: ${response.status}`);
            }

            const data = await response.json();
            console.log('📦 Received full data:', data);

            if (!data) {
                console.error('❌ No data received from API');
                return;
            }

            // ✅ UPDATE DATE RANGE DISPLAY
            updateDateRangeDisplay(period, startDate, endDate);

            // ✅ UPDATE SUMMARY CARDS
            if (data.summary) {
                console.log('💰 Updating summary cards...');

                const revenueEl = document.getElementById('totalRevenue');
                const expenseEl = document.getElementById('totalExpense');
                const profitEl = document.getElementById('netProfit');

                if (revenueEl) {
                    revenueEl.textContent = `₱${data.summary.totalRevenue.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ",")}`;
                }

                if (expenseEl) {
                    expenseEl.textContent = `₱${data.summary.totalExpense.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ",")}`;
                }

                if (profitEl) {
                    profitEl.textContent = `₱${data.summary.netProfit.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ",")}`;
                }

                console.log('✅ Summary cards updated');
            }

            // ✅ UPDATE REVENUE CHART
            if (revenueTrendChart && data.revenueTrend) {
                console.log('📈 Updating revenue chart...');

                const labels = data.revenueTrend.labels || [];
                const datasets = data.revenueTrend.datasets || [];

                if (!Array.isArray(datasets)) {
                    console.error('❌ Datasets is not an array:', datasets);
                    return;
                }

                revenueTrendChart.data.labels = labels;
                revenueTrendChart.data.datasets = datasets.map((ds, i) => {
                    const color = baseColors[i % baseColors.length];
                    const label = ds.label || 'Revenue';
                    const dataValues = ds.data || [];

                    return {
                        label: label,
                        data: dataValues,
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
                console.log('✅ Revenue chart updated successfully');
            }

            // ✅ UPDATE EXPENSE CHART
            if (expenseBreakdownChart && data.expenseBreakdown) {
                console.log('📊 Updating expense chart...');

                const labels = data.expenseBreakdown.labels || [];
                const datasets = data.expenseBreakdown.datasets || [];

                if (datasets.length > 0) {
                    const values = datasets[0].data || [];

                    expenseBreakdownChart.data.labels = labels;
                    expenseBreakdownChart.data.datasets[0].data = values;
                    expenseBreakdownChart.data.datasets[0].backgroundColor =
                        generateDoughnutColors(labels.length);

                    expenseBreakdownChart.update();
                    console.log('✅ Expense chart updated successfully');
                }
            }

        } catch (err) {
            console.error("❌ Error updating charts:", err);
            console.error("Stack trace:", err.stack);
        }
    }

    // ------------------------------------------------------------
    // 4. Time period dropdown + Custom Date Range
    // ------------------------------------------------------------
    const periodSelect = document.getElementById('timePeriodSelect');
    const customDateRangeDiv = document.getElementById('customDateRange');
    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');
    const applyDateBtn = document.getElementById('applyDateRange');

    if (periodSelect) {
        console.log('✅ Period select found, value:', periodSelect.value);

        // Handle dropdown change
        periodSelect.addEventListener('change', e => {
            const selectedPeriod = e.target.value;
            console.log('🔄 Period changed to:', selectedPeriod);

            if (selectedPeriod === 'custom') {
                // Show custom date inputs
                customDateRangeDiv.classList.add('active');

                // Set default dates (last 30 days)
                const today = new Date();
                const thirtyDaysAgo = new Date();
                thirtyDaysAgo.setDate(today.getDate() - 30);

                startDateInput.value = thirtyDaysAgo.toISOString().split('T')[0];
                endDateInput.value = today.toISOString().split('T')[0];
            } else {
                // Hide custom date inputs
                customDateRangeDiv.classList.remove('active');
                updateCharts(selectedPeriod);
            }
        });

        // Handle apply custom date range
        if (applyDateBtn) {
            applyDateBtn.addEventListener('click', () => {
                const startDate = startDateInput.value;
                const endDate = endDateInput.value;

                if (!startDate || !endDate) {
                    alert('Please select both start and end dates');
                    return;
                }

                if (new Date(startDate) > new Date(endDate)) {
                    alert('Start date must be before end date');
                    return;
                }

                console.log('📅 Applying custom date range:', startDate, 'to', endDate);
                updateCharts('custom', startDate, endDate);
            });
        }

        // ✅ Initial load
        console.log('🚀 Loading initial charts with period:', periodSelect.value);
        updateCharts(periodSelect.value);
    } else {
        console.error('❌ timePeriodSelect not found');
    }
});