
/* ============================================================
    GLOBAL STATE
    ============================================================ */
let productChart = null;
let revenueChart = null;
let topProductsChart = null;
let selectedProductId = null;
let selectedPeriod = "alltime";
let customStartDate = null;
let customEndDate = null;
let selectedRow = null;

/* ============================================================
    CHART THEME
    ============================================================ */
const THEME = {
    primary: "#A36A66",
    primaryLight: "rgba(163,106,102,0.15)",
    primaryGradientStart: "rgba(163,106,102,0.35)",
    primaryGradientEnd: "rgba(163,106,102,0.02)",
    doughnutColors: ["#EC4899", "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6", "#EF4444", "#06B6D4"],
    gridColor: "rgba(0,0,0,0.04)",
    fontFamily: "'Inter', 'Segoe UI', sans-serif",
    tooltipBg: "rgba(30,30,30,0.9)"
};

function createGradient(ctx, startColor, endColor) {
    var gradient = ctx.createLinearGradient(0, 0, 0, ctx.canvas.clientHeight);
    gradient.addColorStop(0, startColor);
    gradient.addColorStop(1, endColor);
    return gradient;
}

/* ============================================================
    CHART.JS GLOBAL DEFAULTS
    ============================================================ */
Chart.defaults.font.family = THEME.fontFamily;
Chart.defaults.font.size = 12;
Chart.defaults.color = "#6B7280";

/* ============================================================
    CUSTOM PLUGIN: "No Data" message
    ============================================================ */
var noDataPlugin = {
    id: 'noDataMessage',
    afterDraw: function (chart) {
        var datasets = chart.data.datasets;
        var hasData = false;
        for (var i = 0; i < datasets.length; i++) {
            var data = datasets[i].data;
            for (var j = 0; j < data.length; j++) {
                if (data[j] && data[j] > 0) { hasData = true; break; }
            }
            if (hasData) break;
        }
        if (!hasData) {
            var ctx = chart.ctx;
            var width = chart.width;
            var height = chart.height;
            ctx.save();
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.font = '14px ' + THEME.fontFamily;
            ctx.fillStyle = '#9CA3AF';
            ctx.fillText('No sales data for this period', width / 2, height / 2);
            ctx.restore();
        }
    }
};

Chart.register(noDataPlugin);

/* ============================================================
    PAGE INITIALIZATION
    ============================================================ */
document.addEventListener("DOMContentLoaded", function () {
    // 1. Create the product sales trend chart
    var ctx = document.getElementById('productSalesChart');
    if (ctx) {
        var chartCtx = ctx.getContext('2d');
        var gradient = createGradient(chartCtx, THEME.primaryGradientStart, THEME.primaryGradientEnd);

        productChart = new Chart(chartCtx, {
            type: "line",
            data: {
                labels: [],
                datasets: [{
                    label: "Units Sold",
                    data: [],
                    borderColor: THEME.primary,
                    backgroundColor: gradient,
                    borderWidth: 2.5,
                    tension: 0.4,
                    fill: true,
                    pointRadius: 4,
                    pointHoverRadius: 7,
                    pointBackgroundColor: "#fff",
                    pointBorderColor: THEME.primary,
                    pointBorderWidth: 2,
                    pointHoverBackgroundColor: THEME.primary,
                    pointHoverBorderColor: "#fff",
                    pointHoverBorderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                animation: { duration: 600, easing: 'easeOutQuart' },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: THEME.gridColor, drawBorder: false },
                        border: { display: false },
                        ticks: {
                            padding: 8,
                            callback: function (val) { return val % 1 === 0 ? val : ''; }
                        }
                    },
                    x: {
                        grid: { display: false },
                        border: { display: false },
                        ticks: { padding: 8 }
                    }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: THEME.tooltipBg,
                        titleFont: { size: 13, weight: '600' },
                        bodyFont: { size: 12 },
                        padding: { top: 10, bottom: 10, left: 14, right: 14 },
                        cornerRadius: 8,
                        displayColors: false,
                        callbacks: {
                            title: function (items) { return items[0].label; },
                            label: function (item) { return item.parsed.y + ' units sold'; }
                        }
                    }
                }
            }
        });
    }

    // 2. Initialize data
    updateDateRangeDisplay(selectedPeriod);
    updateSalesSummary(selectedPeriod);
    loadAllProductSales(1);
    loadSalesReport(selectedPeriod);

    // 3. Auto-select first product row
    var firstRow = document.querySelector(".product-row");
    if (firstRow) selectProduct(firstRow);

    // 4. Setup date range controls
    setupCustomDateRange();

    // 5. Setup search
    var searchInput = document.getElementById("productSearch");
    if (searchInput) {
        searchInput.addEventListener("keyup", function () {
            var value = this.value.toLowerCase();
            document.querySelectorAll(".product-row").forEach(function (row) {
                row.style.display = row.innerText.toLowerCase().includes(value) ? "" : "none";
            });
        });
    }

    // 6. Setup PDF button
    var reportBtn = document.getElementById("generateReportBtn");
    if (reportBtn) reportBtn.addEventListener("click", generatePdfReport);

    // 7. Set the dropdown to match default period
    var dateSelect = document.getElementById("dateRangeSelect");
    if (dateSelect) dateSelect.value = selectedPeriod;
});

/* ============================================================
    ROW SELECTION
    ============================================================ */
function selectProduct(rowElement) {
    if (!rowElement) return;

    var name = rowElement.dataset.name || '';
    var id = rowElement.dataset.id || '';
    var totalOrders = rowElement.dataset.orders || '0';
    var image = rowElement.dataset.img || '';

    if (selectedRow) selectedRow.classList.remove("row-selected");
    selectedRow = rowElement;
    selectedRow.classList.add("row-selected");

    selectedProductId = id;
    var chartLabel = document.getElementById("chartProductName");
    if (chartLabel) chartLabel.textContent = name;

    loadProductSales(selectedPeriod, customStartDate, customEndDate);
    UpdateCardDisplay(name, image, totalOrders);
}

/* ============================================================
    CUSTOM DATE RANGE SETUP
    ============================================================ */
function setupCustomDateRange() {
    var periodSelect = document.getElementById("dateRangeSelect");
    var customDateRange = document.getElementById("customDateRange");
    var applyButton = document.getElementById("applyDateRange");

    if (!periodSelect || !customDateRange || !applyButton) return;

    periodSelect.addEventListener("change", function () {
        if (this.value === "custom") {
            customDateRange.classList.add("active");
        } else {
            customDateRange.classList.remove("active");
            selectedPeriod = this.value;
            customStartDate = null;
            customEndDate = null;
            handlePeriodChange(this.value);
        }
    });

    applyButton.addEventListener("click", function () {
        var sd = document.getElementById("startDate").value;
        var ed = document.getElementById("endDate").value;

        if (!sd || !ed) { showToast("Please select both start and end dates", "warning"); return; }
        if (new Date(sd) > new Date(ed)) { showToast("Start date must be before end date", "error"); return; }

        customStartDate = sd;
        customEndDate = ed;
        selectedPeriod = "custom";
        handlePeriodChange("custom", sd, ed);
    });
}

/* ============================================================
    DATE RANGE CHANGE HANDLER
    ============================================================ */
async function handlePeriodChange(period, startDate, endDate) {
    startDate = startDate || null;
    endDate = endDate || null;
    updateDateRangeDisplay(period, startDate, endDate);
    await updateSalesSummary(period, startDate, endDate);
    if (selectedProductId) loadProductSales(period, startDate, endDate);
    loadAllProductSales(1);
    loadSalesReport(period, startDate, endDate);
}

/* ============================================================
    DATE RANGE DISPLAY
    ============================================================ */
function updateDateRangeDisplay(period, startDate, endDate) {
    var el = document.getElementById("currentDateRange");
    if (!el) return;
    if (period === "custom" && startDate && endDate) {
        el.textContent = formatDate(new Date(startDate)) + " - " + formatDate(new Date(endDate));
    } else if (period === "alltime") {
        el.textContent = "All Time";
    } else {
        var r = getDateRange(period);
        el.textContent = formatDate(r.start) + " - " + formatDate(r.end);
    }
}

function getDateRange(period) {
    var today = new Date();
    var start;
    switch (period) {
        case "week":
            var dow = today.getDay();
            start = new Date(today);
            start.setDate(today.getDate() - (dow === 0 ? 6 : dow - 1));
            break;
        case "month": start = new Date(today.getFullYear(), today.getMonth(), 1); break;
        case "year": start = new Date(today.getFullYear(), 0, 1); break;
        case "alltime": start = new Date(2020, 0, 1); break;
        default: start = new Date(today); start.setDate(today.getDate() - 7);
    }
    return { start: start, end: today };
}

function formatDate(date) {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatCurrency(value) {
    return '₱' + Number(value).toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/* ============================================================
    SALES SUMMARY UPDATE
    ============================================================ */
async function updateSalesSummary(period, startDate, endDate) {
    try {
        var url = '/Sales_Finance/GetSalesSummaryByPeriod?period=' + period;
        if (startDate && endDate) url += '&startDate=' + startDate + '&endDate=' + endDate;

        var response = await fetch(url);
        var result = await response.json();

        if (result.success) {
            var salesEl = document.getElementById('summaryTotalSales');
            var ordersEl = document.getElementById('summaryTotalOrders');
            if (salesEl) salesEl.textContent = result.totalSalesFormatted;
            if (ordersEl) ordersEl.textContent = result.totalOrders;
        }
    } catch (err) {
        console.error("Error updating sales summary:", err);
    }
}

/* ============================================================
    PRODUCT SALES CHART LOADING
    ============================================================ */
async function loadProductSales(period, startDate, endDate) {
    if (!productChart || !selectedProductId) return;

    try {
        var url = '/Sales_Finance/GetProductSales?productId=' + selectedProductId + '&period=' + period;
        if (startDate && endDate) url += '&startDate=' + startDate + '&endDate=' + endDate;

        var response = await fetch(url);
        if (!response.ok) return;

        var data = await response.json();

        if (!Array.isArray(data)) {
            productChart.data.labels = [];
            productChart.data.datasets[0].data = [];
            productChart.update();
            return;
        }

        // Rebuild gradient for current canvas size
        var chartCtx = productChart.ctx;
        var gradient = createGradient(chartCtx, THEME.primaryGradientStart, THEME.primaryGradientEnd);

        productChart.data.labels = data.map(function (x) { return x.label || x.Label; });
        productChart.data.datasets[0].data = data.map(function (x) { return x.total || x.Total; });
        productChart.data.datasets[0].backgroundColor = gradient;
        productChart.update();
    } catch (err) {
        console.error("[loadProductSales] Error:", err);
    }
}

/* ============================================================
    SALES TABLE LOADING WITH PAGINATION
    ============================================================ */
var salesPageSize = 10;

async function loadAllProductSales(page) {
    page = page || 1;
    try {
        var url = '/Sales_Finance/GetAllProductSalesDataPaginated?page=' + page + '&pageSize=' + salesPageSize;
        var response = await fetch(url);
        var result = await response.json();

        var salesTableBody = document.getElementById("salesTableBody");

        if (!result.success || !result.data || result.data.length === 0) {
            if (salesTableBody) salesTableBody.innerHTML = '<tr><td colspan="5" class="px-4 py-8 text-center text-gray-500">No sales records found.</td></tr>';
            var fromEl = document.getElementById("salesShowingFrom");
            var toEl = document.getElementById("salesShowingTo");
            var totalEl = document.getElementById("salesTotalItems");
            var paginEl = document.getElementById("salesPaginationControls");
            if (fromEl) fromEl.textContent = "0";
            if (toEl) toEl.textContent = "0";
            if (totalEl) totalEl.textContent = "0";
            if (paginEl) paginEl.innerHTML = '';
            return;
        }

        salesTableBody.innerHTML = result.data.map(function (sale) {
            return '<tr class="hover:bg-gray-50 transition">'
                + '<td class="px-4 py-3 text-gray-800 font-medium">' + sale.variantName + '</td>'
                + '<td class="px-4 py-3 text-gray-700">' + sale.quantity + '</td>'
                + '<td class="px-4 py-3 text-gray-700">' + formatCurrency(sale.price) + '</td>'
                + '<td class="px-4 py-3 text-gray-900 font-semibold">' + formatCurrency(sale.total) + '</td>'
                + '<td class="px-4 py-3 text-gray-600">' + sale.transactionDate + '</td>'
                + '</tr>';
        }).join('');

        var from = (result.currentPage - 1) * result.pageSize + 1;
        var to = Math.min(result.currentPage * result.pageSize, result.totalItems);
        document.getElementById("salesShowingFrom").textContent = from;
        document.getElementById("salesShowingTo").textContent = to;
        document.getElementById("salesTotalItems").textContent = result.totalItems;

        renderSalesPagination(result.currentPage, result.totalPages);
    } catch (err) {
        console.error("Error loading sales data:", err);
        var tb = document.getElementById("salesTableBody");
        if (tb) tb.innerHTML = '<tr><td colspan="5" class="px-4 py-8 text-center text-red-500">Error loading data.</td></tr>';
    }
}

function renderSalesPagination(currentPage, totalPages) {
    var container = document.getElementById("salesPaginationControls");
    if (!container) return;
    if (totalPages <= 1) { container.innerHTML = ''; return; }

    var btn = 'px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition';
    var active = 'px-4 py-2 bg-[#A36A66] text-white rounded-lg text-sm font-semibold';
    var html = '';

    if (currentPage > 1)
        html += '<button onclick="loadAllProductSales(' + (currentPage - 1) + ')" class="' + btn + '">Previous</button>';

    var max = 5;
    var sp = Math.max(1, currentPage - Math.floor(max / 2));
    var ep = Math.min(totalPages, sp + max - 1);
    if (ep - sp < max - 1) sp = Math.max(1, ep - max + 1);

    if (sp > 1) {
        html += '<button onclick="loadAllProductSales(1)" class="' + btn + '">1</button>';
        if (sp > 2) html += '<span class="px-2 py-2 text-gray-500">...</span>';
    }

    for (var i = sp; i <= ep; i++) {
        html += i === currentPage
            ? '<span class="' + active + '">' + i + '</span>'
            : '<button onclick="loadAllProductSales(' + i + ')" class="' + btn + '">' + i + '</button>';
    }

    if (ep < totalPages) {
        if (ep < totalPages - 1) html += '<span class="px-2 py-2 text-gray-500">...</span>';
        html += '<button onclick="loadAllProductSales(' + totalPages + ')" class="' + btn + '">' + totalPages + '</button>';
    }

    if (currentPage < totalPages)
        html += '<button onclick="loadAllProductSales(' + (currentPage + 1) + ')" class="' + btn + '">Next</button>';

    container.innerHTML = html;
}

/* ============================================================
    TOAST
    ============================================================ */
function showToast(message, type) {
    type = type || "info";
    var toast = document.createElement("div");
    var colors = { success: "bg-green-500", error: "bg-red-500", warning: "bg-yellow-500", info: "bg-blue-500" };
    toast.className = (colors[type] || colors.info) + ' text-white px-4 py-3 rounded-lg shadow-lg';
    toast.textContent = message;
    var container = document.getElementById("toastContainer");
    if (container) container.appendChild(toast);
    setTimeout(function () { toast.remove(); }, 3000);
}

/* ============================================================
    MODAL
    ============================================================ */
function openModal(row) {
    document.getElementById("modalName").textContent = row.dataset.name || 'Unknown';
    document.getElementById("modalCategory").textContent = row.dataset.category || 'N/A';
    document.getElementById("modalSelling").textContent = parseFloat(row.dataset.price || 0).toFixed(2);
    document.getElementById("modalDescription").textContent = row.dataset.description || 'No description available';

    var img = document.getElementById("modalImage");
    if (img) {
        img.src = row.dataset.img || '/images/Hand_Sanitizer.jpg';
        img.alt = (row.dataset.name || 'Product') + ' Image';
    }

    document.getElementById("productModal").classList.remove("hidden");
}

function UpdateCardDisplay(name, image, totalOrders) {
    var nameEl = document.getElementById("productCardName");
    var orderEl = document.getElementById("productCardTotalOrder");
    var imgEl = document.getElementById("productCardImg");

    if (nameEl) nameEl.textContent = name;
    if (orderEl) orderEl.textContent = totalOrders;
    if (imgEl) imgEl.src = image || '/images/Hand_Sanitizer.jpg';
}

function closeModal() {
    var el = document.getElementById("productModal");
    if (el) el.classList.add("hidden");
}

/* ============================================================
    SALES REPORT CHARTS (REVENUE & TOP PRODUCTS)
    ============================================================ */
async function loadSalesReport(period, startDate, endDate) {
    try {
        var url = '/Sales_Finance/SalesReportData?period=' + period;
        if (startDate && endDate) url += '&startDate=' + startDate + '&endDate=' + endDate;

        var resp = await fetch(url);
        if (!resp.ok) return;

        var data = await resp.json();
        if (!data) return;

        window.currentPeriodText = data.periodText;

        if (data.chartLabels && data.chartLabels.length > 0 && data.chartValues) {
            updateRevenueChart(data.chartLabels, data.chartValues);
        } else {
            updateRevenueChart([], []);
        }

        if (data.topProducts && data.topProducts.length > 0) {
            updateTopProductsChart(data.topProducts);
        } else {
            updateTopProductsChart([]);
        }
    } catch (err) {
        console.error("[loadSalesReport] Error:", err);
    }
}

function updateRevenueChart(labels, values) {
    var canvas = document.getElementById("salesRevenueChart");
    if (!canvas) return;

    if (revenueChart !== null) revenueChart.destroy();

    var chartCtx = canvas.getContext('2d');
    var gradient = createGradient(chartCtx, THEME.primaryGradientStart, THEME.primaryGradientEnd);

    revenueChart = new Chart(chartCtx, {
        type: "line",
        data: {
            labels: labels,
            datasets: [{
                label: "Revenue",
                data: values,
                borderColor: THEME.primary,
                backgroundColor: gradient,
                tension: 0.4,
                fill: true,
                borderWidth: 2.5,
                pointRadius: values.length > 20 ? 0 : 4,
                pointHoverRadius: 7,
                pointBackgroundColor: "#fff",
                pointBorderColor: THEME.primary,
                pointBorderWidth: 2,
                pointHoverBackgroundColor: THEME.primary,
                pointHoverBorderColor: "#fff",
                pointHoverBorderWidth: 3
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            animation: { duration: 600, easing: 'easeOutQuart' },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: THEME.gridColor, drawBorder: false },
                    border: { display: false },
                    ticks: {
                        padding: 8,
                        callback: function (val) { return formatCurrency(val); },
                        maxTicksLimit: 6
                    }
                },
                x: {
                    grid: { display: false },
                    border: { display: false },
                    ticks: {
                        padding: 8,
                        maxRotation: 45,
                        autoSkip: true,
                        maxTicksLimit: 12
                    }
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: THEME.tooltipBg,
                    titleFont: { size: 13, weight: '600' },
                    bodyFont: { size: 12 },
                    padding: { top: 10, bottom: 10, left: 14, right: 14 },
                    cornerRadius: 8,
                    displayColors: false,
                    callbacks: {
                        title: function (items) { return items[0].label; },
                        label: function (item) { return 'Revenue: ' + formatCurrency(item.parsed.y); }
                    }
                }
            }
        }
    });
}

function updateTopProductsChart(topProducts) {
    var canvas = document.getElementById("topProductsChart");
    if (!canvas) return;

    if (topProductsChart !== null) topProductsChart.destroy();

    var listContainer = document.getElementById("topProductsList");

    // Handle empty state
    if (!topProducts || topProducts.length === 0) {
        topProductsChart = new Chart(canvas.getContext('2d'), {
            type: "doughnut",
            data: { labels: [], datasets: [{ data: [], backgroundColor: [] }] },
            options: { responsive: true, maintainAspectRatio: false }
        });
        if (listContainer) {
            listContainer.innerHTML = '<p class="text-sm text-gray-400 text-center">No product data available</p>';
        }
        return;
    }

    var labels = topProducts.map(function (p) { return p.productName; });
    var values = topProducts.map(function (p) { return p.totalAmount; });
    var total = values.reduce(function (a, b) { return a + b; }, 0);
    var colors = THEME.doughnutColors.slice(0, topProducts.length);

    topProductsChart = new Chart(canvas.getContext('2d'), {
        type: "doughnut",
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: colors,
                borderWidth: 3,
                borderColor: '#ffffff',
                hoverOffset: 8,
                hoverBorderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: "65%",
            animation: { animateRotate: true, duration: 800, easing: 'easeOutQuart' },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: THEME.tooltipBg,
                    titleFont: { size: 13, weight: '600' },
                    bodyFont: { size: 12 },
                    padding: { top: 10, bottom: 10, left: 14, right: 14 },
                    cornerRadius: 8,
                    callbacks: {
                        label: function (ctx) {
                            var pct = total > 0 ? ((ctx.raw / total) * 100).toFixed(1) : 0;
                            return ctx.label + ': ' + formatCurrency(ctx.raw) + ' (' + pct + '%)';
                        }
                    }
                }
            }
        }
    });

    // Render legend list
    if (listContainer) {
        var legendHtml = '<h3 class="font-semibold mb-4 text-gray-800 text-sm">Top ' + topProducts.length + ' Products</h3>'
            + '<div class="space-y-3">';

        topProducts.forEach(function (p, i) {
            var percent = total > 0 ? ((p.totalAmount / total) * 100).toFixed(1) : 0;
            var barWidth = total > 0 ? Math.max(8, (p.totalAmount / total) * 100) : 8;

            legendHtml += '<div class="group">'
                + '<div class="flex items-center w-full mb-1">'
                + '<div class="w-2.5 h-2.5 rounded-full shrink-0 mr-2.5" style="background:' + colors[i % colors.length] + '"></div>'
                + '<span class="text-xs text-gray-700 truncate min-w-0 flex-1" title="' + p.productName + '">' + p.productName + '</span>'
                + '<span class="ml-2 font-semibold text-gray-800 text-xs whitespace-nowrap">' + percent + '%</span>'
                + '</div>'
                + '<div class="ml-5 h-1.5 rounded-full bg-gray-100 overflow-hidden">'
                + '<div class="h-full rounded-full transition-all duration-500" style="width:' + barWidth + '%; background:' + colors[i % colors.length] + '"></div>'
                + '</div>'
                + '</div>';
        });

        legendHtml += '</div>';
        listContainer.innerHTML = legendHtml;
    }
}

/* ============================================================
    PDF GENERATION
    ============================================================ */
async function generatePdfReport() {
    showLoader();
    try {
        var period = selectedPeriod;
        var sel = document.getElementById("dateRangeSelect");
        var periodText = sel.options[sel.selectedIndex].text;

        if (period === "custom" && customStartDate && customEndDate) {
            periodText = formatDate(new Date(customStartDate)) + ' - ' + formatDate(new Date(customEndDate));
        }

        var url = '/Sales_Finance/GetSalesReportDatatry?period=' + period;
        if (customStartDate && customEndDate) url += '&startDate=' + customStartDate + '&endDate=' + customEndDate;

        var response = await fetch(url);
        if (!response.ok) throw new Error("Server error loading report data.");
        var data = await response.json();

        var revenueImg = await htmlToImage(document.getElementById("revenueCard"));
        var topProductsImg = await htmlToImage(document.getElementById("topProductsCard"));

        var payload = {
            ReportPeriod: periodText,
            Summary: data.summary,
            SalesTrendChartBase64: revenueImg,
            TopProductsChartBase64: topProductsImg,
            TopProductsTable: data.topProductsTable
        };

        var pdfResponse = await fetch("/Sales_Finance/ExportProductSalesPdf", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!pdfResponse.ok) throw new Error("PDF creation failed.");

        var blob = await pdfResponse.blob();
        var link = URL.createObjectURL(blob);
        window.open(link, "_blank");
        URL.revokeObjectURL(link);
        showToast("PDF Report generated! ✅", "success");
    } catch (err) {
        console.error("PDF Generation Error:", err);
        showToast("Failed to generate PDF", "error");
    } finally {
        hideLoader();
    }
}

async function htmlToImage(element) {
    var canvas = await html2canvas(element, { scale: 2, backgroundColor: "#fff" });
    return canvas.toDataURL("image/png");
}

function showLoader() {
    var el = document.getElementById("loadingOverlay");
    if (el) el.classList.remove("hidden");
}

function hideLoader() {
    var el = document.getElementById("loadingOverlay");
    if (el) el.classList.add("hidden");
}