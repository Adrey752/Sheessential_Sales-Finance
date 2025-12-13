const ctx = document.getElementById('productSalesChart');
let selectedProductId = null;
let selectedPeriod = "week";
let customStartDate = null;
let customEndDate = null;

const productChart = new Chart(ctx, {
    type: "line",
    data: {
        labels: [],
        datasets: [{
            label: "Sales (Units)",
            data: [],
            borderColor: "#A36A66",
            backgroundColor: "rgba(163,106,102,0.25)",
            borderWidth: 2,
            tension: 0.3,
            fill: true
        }]
    },
    options: {
        responsive: true,
        scales: { y: { beginAtZero: true } }
    }
});

let selectedRow = null;

function selectProduct(name, id, totalOrders, image, rowElement) {
    // Remove highlight from previous row
    if (selectedRow) {
        selectedRow.classList.remove("row-selected");
    }
    // Highlight new row
    selectedRow = rowElement;
    selectedRow.classList.add("row-selected");

    // Your original logic
    selectedProductId = id;
    document.getElementById("chartProductName").textContent = name;
    loadProductSales(selectedPeriod, customStartDate, customEndDate);
    UpdateCardDisplay(name, image, totalOrders);
}

/* ============================================================
    CONSOLIDATED PAGE INITIALIZATION - RUNS ONLY ONCE
    ============================================================ */
document.addEventListener("DOMContentLoaded", function () {
    // 1. Initialize with default period
    updateSalesSummary("week");
    updateDateRangeDisplay("week");

    // 2. Load all product sales table
    loadAllProductSales(1);

    // 3. Load sales report charts
    loadSalesReport("week");

    // 4. Auto-select first product row
    const firstRow = document.querySelector(".product-row");
    if (firstRow) {
        firstRow.click(); // triggers selectProduct()
    }

    // 5. Setup custom date range toggle
    setupCustomDateRange();
});

/* ============================================================
    CUSTOM DATE RANGE SETUP
    ============================================================ */
function setupCustomDateRange() {
    const periodSelect = document.getElementById("dateRangeSelect");
    const customDateRange = document.getElementById("customDateRange");
    const applyButton = document.getElementById("applyDateRange");

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
        const startDate = document.getElementById("startDate").value;
        const endDate = document.getElementById("endDate").value;

        if (!startDate || !endDate) {
            showToast("Please select both start and end dates", "warning");
            return;
        }

        if (new Date(startDate) > new Date(endDate)) {
            showToast("Start date must be before end date", "error");
            return;
        }

        customStartDate = startDate;
        customEndDate = endDate;
        selectedPeriod = "custom";
        handlePeriodChange("custom", startDate, endDate);
    });
}

/* ============================================================
    DATE RANGE CHANGE HANDLER - RUNS ONLY ONCE
    ============================================================ */
async function handlePeriodChange(period, startDate = null, endDate = null) {
    // Update date range display
    updateDateRangeDisplay(period, startDate, endDate);

    // Update sales summary cards
    await updateSalesSummary(period, startDate, endDate);

    // Update product chart if a product is selected
    if (selectedProductId) {
        loadProductSales(period, startDate, endDate);
    }

    // Update sales table pagination
    loadAllProductSales(1, period, startDate, endDate);

    // Update revenue chart and top products
    loadSalesReport(period, startDate, endDate);
}

/* ============================================================
    DATE RANGE DISPLAY
    ============================================================ */
function updateDateRangeDisplay(period, startDate = null, endDate = null) {
    const displayElement = document.getElementById("currentDateRange");
    const today = new Date();
    let displayText = "";

    if (period === "custom" && startDate && endDate) {
        const start = new Date(startDate);
        const end = new Date(endDate);
        displayText = `${formatDate(start)} - ${formatDate(end)}`;
    } else {
        const ranges = getDateRange(period);
        displayText = `${formatDate(ranges.start)} - ${formatDate(ranges.end)}`;
    }

    displayElement.textContent = displayText;
}

function getDateRange(period) {
    const today = new Date();
    let start, end = today;

    switch (period) {
        case "week":
            const dayOfWeek = today.getDay();
            const daysToMonday = (dayOfWeek === 0 ? 6 : dayOfWeek - 1);
            start = new Date(today);
            start.setDate(today.getDate() - daysToMonday);
            break;
        case "month":
            start = new Date(today.getFullYear(), today.getMonth(), 1);
            break;
        case "year":
            start = new Date(today.getFullYear(), 0, 1);
            break;
        case "alltime":
            start = new Date(2020, 0, 1); // Or your earliest data date
            break;
        default:
            start = new Date(today);
            start.setDate(today.getDate() - 7);
    }

    return { start, end };
}

function formatDate(date) {
    return date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    });
}

/* ============================================================
    SALES TABLE LOADING WITH PAGINATION
    ============================================================ */
let currentSalesPage = 1;
let totalSalesPages = 1;
const salesPageSize = 10;

async function loadAllProductSales(page = 1, period = "week", startDate = null, endDate = null) {
    try {
        let url = `/Sales_Finance/GetAllProductSalesDataPaginated?page=${page}&pageSize=${salesPageSize}`;

        // Add period and custom date params if applicable
        if (period) {
            url += `&period=${period}`;
        }
        if (startDate && endDate) {
            url += `&startDate=${startDate}&endDate=${endDate}`;
        }

        const response = await fetch(url);
        const result = await response.json();

        if (!result.success) {
            showToast(result.message || "No sales data found.", "warning");
            return;
        }

        const salesTableBody = document.getElementById("salesTableBody");

        if (!result.data || result.data.length === 0) {
            salesTableBody.innerHTML = `
                <tr>
                    <td colspan="5" class="px-4 py-8 text-center text-gray-500">
                        No sales records found.
                    </td>
                </tr>
            `;
            return;
        }

        // Update pagination state
        currentSalesPage = result.currentPage;
        totalSalesPages = result.totalPages;

        // Populate table with sales data
        salesTableBody.innerHTML = result.data.map(sale => `
            <tr class="hover:bg-gray-50 transition">
                <td class="px-4 py-3 text-gray-800 font-medium">${sale.variantName}</td>
                <td class="px-4 py-3 text-gray-700">${sale.quantity}</td>
                <td class="px-4 py-3 text-gray-700">₱${sale.price.toFixed(2)}</td>
                <td class="px-4 py-3 text-gray-900 font-semibold">₱${sale.total.toFixed(2)}</td>
                <td class="px-4 py-3 text-gray-600">${sale.transactionDate}</td>
            </tr>
        `).join('');

        // Update pagination info
        const showingFrom = (result.currentPage - 1) * result.pageSize + 1;
        const showingTo = Math.min(result.currentPage * result.pageSize, result.totalItems);

        document.getElementById("salesShowingFrom").textContent = showingFrom;
        document.getElementById("salesShowingTo").textContent = showingTo;
        document.getElementById("salesTotalItems").textContent = result.totalItems;

        // Render pagination controls
        renderSalesPagination(result.currentPage, result.totalPages);

        // ✅ ONLY show toast on initial load (page 1)
        if (page === 1) {
            showToast("Sales summary loaded! ✅", "success");
        }

    } catch (err) {
        console.error("Error loading sales data:", err);
        showToast("Failed to load sales summary 😭", "error");

        document.getElementById("salesTableBody").innerHTML = `
            <tr>
                <td colspan="5" class="px-4 py-8 text-center text-red-500">
                    Error loading data. Please try again.
                </td>
            </tr>
        `;
    }
}

function renderSalesPagination(currentPage, totalPages) {
    const paginationControls = document.getElementById("salesPaginationControls");

    if (totalPages <= 1) {
        paginationControls.innerHTML = '';
        return;
    }

    let paginationHTML = '';

    // Previous button
    if (currentPage > 1) {
        paginationHTML += `
            <button onclick="loadAllProductSales(${currentPage - 1}, '${selectedPeriod}', ${customStartDate ? `'${customStartDate}'` : null}, ${customEndDate ? `'${customEndDate}'` : null})" 
                    class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                Previous
            </button>
        `;
    }

    // Page numbers (show max 5 pages)
    const maxVisiblePages = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

    // Adjust start if we're near the end
    if (endPage - startPage < maxVisiblePages - 1) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    // First page + ellipsis
    if (startPage > 1) {
        paginationHTML += `
            <button onclick="loadAllProductSales(1, '${selectedPeriod}', ${customStartDate ? `'${customStartDate}'` : null}, ${customEndDate ? `'${customEndDate}'` : null})" 
                    class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                1
            </button>
        `;
        if (startPage > 2) {
            paginationHTML += `<span class="px-2 py-2 text-gray-500">...</span>`;
        }
    }

    // Page numbers
    for (let i = startPage; i <= endPage; i++) {
        if (i === currentPage) {
            paginationHTML += `
                <span class="px-4 py-2 bg-[#A36A66] text-white rounded-lg text-sm font-semibold">
                    ${i}
                </span>
            `;
        } else {
            paginationHTML += `
                <button onclick="loadAllProductSales(${i}, '${selectedPeriod}', ${customStartDate ? `'${customStartDate}'` : null}, ${customEndDate ? `'${customEndDate}'` : null})" 
                        class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                    ${i}
                </button>
            `;
        }
    }

    // Last page + ellipsis
    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            paginationHTML += `<span class="px-2 py-2 text-gray-500">...</span>`;
        }
        paginationHTML += `
            <button onclick="loadAllProductSales(${totalPages}, '${selectedPeriod}', ${customStartDate ? `'${customStartDate}'` : null}, ${customEndDate ? `'${customEndDate}'` : null})" 
                    class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                ${totalPages}
            </button>
        `;
    }

    // Next button
    if (currentPage < totalPages) {
        paginationHTML += `
            <button onclick="loadAllProductSales(${currentPage + 1}, '${selectedPeriod}', ${customStartDate ? `'${customStartDate}'` : null}, ${customEndDate ? `'${customEndDate}'` : null})" 
                    class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                Next
            </button>
        `;
    }

    paginationControls.innerHTML = paginationHTML;
}

/* ============================================================
    SALES SUMMARY UPDATE
    ============================================================ */
async function updateSalesSummary(period, startDate = null, endDate = null) {
    try {
        let url = `/Sales_Finance/GetSalesSummaryByPeriod?period=${period}`;

        if (startDate && endDate) {
            url += `&startDate=${startDate}&endDate=${endDate}`;
        }

        const response = await fetch(url);
        const result = await response.json();

        if (result.success) {
            // Update Total Sales card
            const totalSalesElement = document.querySelector('[style="color: #A36A66;"] + div p.text-2xl');
            if (totalSalesElement) {
                totalSalesElement.textContent = result.totalSalesFormatted;
            }

            // Update Total Orders card
            const totalOrdersElement = document.querySelectorAll('[style="color: #A36A66;"] + div p.text-2xl')[1];
            if (totalOrdersElement) {
                totalOrdersElement.textContent = result.totalOrders;
            }
        }
    } catch (err) {
        console.error("Error updating sales summary:", err);
    }
}

/* ============================================================
    PRODUCT SALES CHART LOADING
    ============================================================ */
async function loadProductSales(period, startDate = null, endDate = null) {
    try {
        let url = `/Sales_Finance/GetProductSales?productId=${selectedProductId}&period=${period}`;

        if (startDate && endDate) {
            url += `&startDate=${startDate}&endDate=${endDate}`;
        }

        const response = await fetch(url);
        const data = await response.json();

        if (!Array.isArray(data)) {
            showToast(data.message || "No sales data found.", "warning");
            productChart.data.labels = [];
            productChart.data.datasets[0].data = [];
            productChart.update();
            return;
        }

        productChart.data.labels = data.map(x => x.label ?? x.Label);
        productChart.data.datasets[0].data = data.map(x => x.total ?? x.Total);
        productChart.update();

    } catch (err) {
        console.error("Error loading product sales:", err);
        showToast("Error loading sales 😭", "error");
    }
}

/* ============================================================
    SEARCH + TOAST UTILITIES
    ============================================================ */
function showToast(message, type = "info") {
    const toast = document.createElement("div");
    const colors = {
        success: "bg-green-500",
        error: "bg-red-500",
        warning: "bg-yellow-500",
        info: "bg-blue-500"
    };

    toast.className = `${colors[type]} text-white px-4 py-3 rounded-lg shadow-lg animate-slideIn`;
    toast.textContent = message;

    document.getElementById("toastContainer").appendChild(toast);

    setTimeout(() => {
        toast.classList.add("animate-fadeOut");
        setTimeout(() => toast.remove(), 500);
    }, 3000);
}

document.getElementById("productSearch").addEventListener("keyup", function () {
    const value = this.value.toLowerCase();
    document.querySelectorAll("tbody tr").forEach(row => {
        row.style.display = row.innerText.toLowerCase().includes(value) ? "" : "none";
    });
});

/* ============================================================
    PDF GENERATION PIPELINE
    ============================================================ */
const reportBtn = document.getElementById("generateReportBtn");
const loadingOverlay = document.getElementById("loadingOverlay");

reportBtn.addEventListener("click", generatePdfReport);
async function generatePdfReport() {
    showLoader();

    try {
        const period = selectedPeriod;
        const dateRangeSelect = document.getElementById("dateRangeSelect");
        let periodText = dateRangeSelect.options[dateRangeSelect.selectedIndex].text;

        // ✅ If custom range is selected, use the actual date range instead of "Custom Range"
        if (period === "custom" && customStartDate && customEndDate) {
            const startDate = new Date(customStartDate);
            const endDate = new Date(customEndDate);
            periodText = `${formatDate(startDate)} - ${formatDate(endDate)}`;
        }

        // 1. Fetch full dataset
        let url = `/Sales_Finance/GetSalesReportDatatry?period=${period}`;
        if (customStartDate && customEndDate) {
            url += `&startDate=${customStartDate}&endDate=${customEndDate}`;
        }

        const response = await fetch(url);
        if (!response.ok) throw new Error("Server error loading report data.");

        const data = await response.json();

        // 2. Generate Base64 charts for PDF
        const revenueCard = document.getElementById("revenueCard");
        const topProductsCard = document.getElementById("topProductsCard");

        const revenueImg = await htmlToImage(revenueCard);
        const topProductsImg = await htmlToImage(topProductsCard);

        const payload = {
            ReportPeriod: periodText, // ✅ Now contains actual date range for custom periods
            Summary: data.summary,
            SalesTrendChartBase64: revenueImg,
            TopProductsChartBase64: topProductsImg,
            TopProductsTable: data.topProductsTable
        };

        // 3. Send to PDF generator
        const pdfResponse = await fetch("/Sales_Finance/ExportProductSalesPdf", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!pdfResponse.ok) throw new Error("PDF creation failed.");

        // 4. Download PDF
        const blob = await pdfResponse.blob();
        const link = URL.createObjectURL(blob);
        window.open(link, "_blank");
        URL.revokeObjectURL(link);

        showToast("PDF Report generated! ✅", "success");

    } catch (err) {
        console.error("PDF Generation Error:", err);
        showToast("Failed to generate PDF 😭", "error");
    } finally {
        hideLoader();
    }
}

async function htmlToImage(element) {
    const canvas = await html2canvas(element, { scale: 2, backgroundColor: "#fff" });
    return canvas.toDataURL("image/png");
}

/* ============================================================
    PDF CHART CONFIGS
    ============================================================ */
function pdfTrendChartConfig(data) {
    return {
        type: "line",
        data: {
            labels: data.map(x => x.label),
            datasets: [{
                label: "Total Revenue",
                data: data.map(x => x.total),
                borderColor: "#A36A66",
                backgroundColor: "rgba(163,106,102,0.15)",
                fill: true,
                tension: 0.3
            }]
        },
        options: {
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } }
        }
    };
}

function pdfTopProductsConfig(data) {
    return {
        type: "doughnut",
        data: {
            labels: data.map(x => x.name),
            datasets: [{
                data: data.map(x => x.percentage),
                backgroundColor: [
                    "#A36A66",
                    "#D9AFA8",
                    "#E8C4C4",
                    "#B77E79",
                    "#C9988F"
                ]
            }]
        },
        options: {
            plugins: { legend: { display: true } }
        }
    };
}

/* ============================================================
    LOADER
    ============================================================ */
function showLoader() {
    loadingOverlay.classList.remove("hidden");
}

function hideLoader() {
    loadingOverlay.classList.add("hidden");
}

/* ============================================================
    MODAL UTILITIES
    ============================================================ */
function openModal(name, category, sellingPrice, imageUrl, description, totalOrders) {
    document.getElementById("modalName").textContent = name;
    document.getElementById("modalCategory").textContent = category;
    document.getElementById("modalSelling").textContent = sellingPrice;
    document.getElementById("modalDescription").textContent = description;

    const imageElement = document.getElementById("modalImage");
    if (imageElement) {
        imageElement.src = imageUrl;
        imageElement.alt = name + " Image";
    }

    document.getElementById("productModal").classList.remove("hidden");
}

function UpdateCardDisplay(name, image, totalOrders) {
    document.getElementById("productCardName").textContent = name;
    document.getElementById("productCardImg").src = image;
    document.getElementById("productCardTotalOrder").textContent = totalOrders;
}

function closeModal() {
    document.getElementById("productModal").classList.add("hidden");
}

/* ============================================================
    SALES REPORT CHARTS (REVENUE & TOP PRODUCTS)
    ============================================================ */
let revenueChart = null;
let topProductsChart = null;

async function loadSalesReport(period, startDate = null, endDate = null) {
    let url = `/Sales_Finance/SalesReportData?period=${period}`;

    if (startDate && endDate) {
        url += `&startDate=${startDate}&endDate=${endDate}`;
    }

    const resp = await fetch(url);
    const data = await resp.json();
    window.currentPeriodText = data.periodText;

    updateRevenueChart(data.chartLabels, data.chartValues);
    updateTopProductsChart(data.topProducts);
}

function updateTopProductsChart(topProducts) {
    const labels = topProducts.map(p => p.productName);
    const values = topProducts.map(p => p.totalAmount);
    const total = values.reduce((a, b) => a + b, 0);

    const colors = ["#FF4FC3", "#3FA9F5", "#00C49A", "#FFC75F", "#FF6F91"];

    if (topProductsChart !== null) topProductsChart.destroy();

    const ctx = document.getElementById("topProductsChart");
    topProductsChart = new Chart(ctx, {
        type: "doughnut",
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: colors,
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: "60%",
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => {
                            const percentage = ((ctx.raw / total) * 100).toFixed(1);
                            return `${ctx.label}: ${percentage}%`;
                        }
                    }
                }
            }
        }
    });

    const listContainer = document.getElementById("topProductsList");
    listContainer.innerHTML = `
        <h3 class="font-semibold mb-4 text-gray-800">Top ${topProducts.length}</h3>
        <div class="space-y-3">
            ${topProducts.map((p, i) => {
        const percent = ((p.totalAmount / total) * 100).toFixed(1);
        return `
                    <div class="flex items-center w-full">
                        <div class="w-3 h-3 rounded-full shrink-0 mr-3" style="background:${colors[i % colors.length]}"></div>
                        <span class="text-sm text-gray-700 truncate min-w-0 flex-1" title="${p.productName}">
                            ${p.productName}
                        </span>
                        <span class="ml-3 font-medium text-gray-500 text-sm whitespace-nowrap">
                            ${percent}%
                        </span>
                    </div>
                `;
    }).join("")}
        </div>
    `;
}

function updateRevenueChart(labels, values) {
    if (revenueChart !== null) revenueChart.destroy();

    revenueChart = new Chart(document.getElementById("salesRevenueChart"), {
        type: "line",
        data: {
            labels: labels,
            datasets: [{
                label: "Sales Revenue",
                data: values,
                borderColor: "#A36A66",
                backgroundColor: "rgba(163,106,102,0.25)",
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            scales: { y: { beginAtZero: true } },
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            cutout: "70%"
        }
    });
}