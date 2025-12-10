
const ctx = document.getElementById('productSalesChart');
let selectedProductId = null;
let selectedPeriod = "week";

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
    loadProductSales(selectedPeriod);
    UpdateCardDisplay(name, image, totalOrders);
}


document.addEventListener("DOMContentLoaded", function () {
    const firstRow = document.querySelector(".product-row");
    if (firstRow) {
        firstRow.click(); // triggers selectProduct()
    }
}); 
document.getElementById("dateRangeSelect").addEventListener("change", (e) => {
    selectedPeriod = e.target.value;
    if (selectedProductId) loadProductSales(selectedPeriod);
    loadAllProductSales(selectedPeriod);

});

// Add this function to your existing Products.js file

let currentSalesPage = 1;
let totalSalesPages = 1;
const salesPageSize = 10;

async function loadAllProductSales(page = 1) {
    try {
        const response = await fetch(`/Sales_Finance/GetAllProductSalesDataPaginated?page=${page}&pageSize=${salesPageSize}`);
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

        showToast("Sales summary loaded! ✅", "success");

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
            <button onclick="loadAllProductSales(${currentPage - 1})" 
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
            <button onclick="loadAllProductSales(1)" 
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
                <button onclick="loadAllProductSales(${i})" 
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
            <button onclick="loadAllProductSales(${totalPages})" 
                    class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                ${totalPages}
            </button>
        `;
    }

    // Next button
    if (currentPage < totalPages) {
        paginationHTML += `
            <button onclick="loadAllProductSales(${currentPage + 1})" 
                    class="px-4 py-2 bg-gray-200 rounded-lg text-gray-700 hover:bg-gray-300 text-sm transition">
                Next
            </button>
        `;
    }

    paginationControls.innerHTML = paginationHTML;
}

// Add this new function
async function updateSalesSummary(period) {
    try {
        const response = await fetch(`/Sales_Finance/GetSalesSummaryByPeriod?period=${period}`);
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

// Update the dateRangeSelect change handler


// Initialize with default period on page load
document.addEventListener("DOMContentLoaded", function () {
    // Load initial summary
    updateSalesSummary("week");

    loadAllProductSales(1);

    const firstRow = document.querySelector(".product-row");
    if (firstRow) {
        firstRow.click();
    }
});


// Keep the existing dateRangeSelect change handler but remove the loadAllProductSales call
// since it doesn't filter by period

// Call this function when the page loads
document.addEventListener("DOMContentLoaded", function () {
    loadAllProductSales();

    const firstRow = document.querySelector(".product-row");
    if (firstRow) {
        firstRow.click();
    }
});

// Also update when period changes
document.getElementById("dateRangeSelect").addEventListener("change", async (e) => {
    selectedPeriod = e.target.value;

    // Update sales summary cards
    await updateSalesSummary(selectedPeriod);

    // Update product chart if a product is selected
    if (selectedProductId) loadProductSales(selectedPeriod);

    // Update sales table pagination
    loadAllProductSales(1);

    // Update revenue chart and top products
    loadSalesReport(selectedPeriod);
});

async function loadProductSales(period) {
    try {
        const response = await fetch(`/Sales_Finance/GetProductSales?productId=${selectedProductId}&period=${period}`);
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

        showToast("Sales data loaded successfully! ✅", "success");

    } catch (err) {
        alert(err.message)
        showToast("Error loading sales 😭", "error");
    }
}
 

/* ============================================================
    2. SEARCH + TOAST UTILITIES
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
    3. PDF GENERATION PIPELINE
    ============================================================ */

const reportBtn = document.getElementById("generateReportBtn");
const dateRangeSelect = document.getElementById("dateRangeSelect");
const loadingOverlay = document.getElementById("loadingOverlay");

reportBtn.addEventListener("click", generatePdfReport);

async function generatePdfReport() {
    showLoader();

    try {
        const period = dateRangeSelect.value;
        const periodText = dateRangeSelect.options[dateRangeSelect.selectedIndex].text;

        // 1. Fetch full dataset
        const response = await fetch(`/Sales_Finance/GetSalesReportDatatry?period=${period}`);
        if (!response.ok) throw new Error("Server error loading report data.");

        const data = await response.json();

        // 2. Generate Base64 charts for PDF
        // Use Promise.all to generate both charts concurrently
        // const [salesTrendBase64, topProductsBase64] = await Promise.all([
        //     createHiddenChart(pdfTrendChartConfig(data.salesTrend), 600, 300),
        //     createHiddenChart(pdfTopProductsConfig(data.topProductsChart), 400, 400)
        // ]);


        // 3. Match your EXACT C# payload structure
        const revenueCard = document.getElementById("revenueCard");

        const revenueImg = await htmlToImage(revenueCard);
        const topProductsImg = await htmlToImage(topProductsCard);
        const payload = {
            ReportPeriod: periodText,
            Summary: data.summary,
            SalesTrendChartBase64: revenueImg,
            TopProductsChartBase64: topProductsImg,
            TopProductsTable: data.topProductsTable
        };


        // 4. Send to PDF generator
        const pdfResponse = await fetch("/Sales_Finance/ExportProductSalesPdf", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!pdfResponse.ok) throw new Error("PDF creation failed.");

        // 5. Download PDF
        const blob = await pdfResponse.blob();
        const link = URL.createObjectURL(blob);
        window.open(link, "_blank");
        URL.revokeObjectURL(link);

        showToast("PDF Report generated! ✅", "success");

    } catch (err) {
        console.error("PDF Generation Error:", err);
        showToast("Failed to generate PDF 😭", "error");
    } finally {
        // Hides loader even if an error occurs!
        hideLoader();
    }
}
async function htmlToImage(element) {
    const canvas = await html2canvas(element, { scale: 2, backgroundColor: "#fff" });
    return canvas.toDataURL("image/png");
}

/* ============================================================
    4. OFFSCREEN CHART CREATION FOR PDF (CORRECTED)
    ============================================================ */


/* ... (rest of your script) ... */
/* ============================================================
    5. PDF CHART CONFIGS (MATCHES YOUR PINK THEME)
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
    6. LOADER
    ============================================================ */

function showLoader() {
    loadingOverlay.classList.remove("hidden");
}

function hideLoader() {
    loadingOverlay.classList.add("hidden");
}

/* ============================================================
    7. MODAL UTILITIES (Missing in your code, added for completeness)
    ============================================================ */
function openModal(name, category, sellingPrice, imageUrl, description, totalOrders) {
    // Set the text content for existing elements
    document.getElementById("modalName").textContent = name;
    document.getElementById("modalCategory").textContent = category;
    document.getElementById("modalSelling").textContent = sellingPrice;
    document.getElementById("modalDescription").textContent = description;

    // Set the image source (assuming you add id="modalImage" to your <img> tag)
    const imageElement = document.getElementById("modalImage");
    if (imageElement) {
        // Since the image URL from the database is a string, set it as the src.
        imageElement.src = imageUrl;

        // Optional: Set alt text for accessibility, using the product name
        imageElement.alt = name + " Image";
    }

    // Show the modal
    document.getElementById("productModal").classList.remove("hidden");
}

function UpdateCardDisplay(name, image, totalOrders) {
    // 1. Update Product Name
    document.getElementById("productCardName").textContent = name;

    // 2. FIX: Use .src to change the image source on the <img> element
    document.getElementById("productCardImg").src = image;

    // 3. Update Total Orders
    document.getElementById("productCardTotalOrder").textContent = totalOrders;
}

function closeModal() {
    document.getElementById("productModal").classList.add("hidden");
}

let revenueChart = null;
let topProductsChart = null;

document.getElementById("dateRangeSelect").addEventListener("change", function () {
    loadSalesReport(this.value);
});

loadSalesReport("week");

async function loadSalesReport(period) {
    const resp = await fetch(`/Sales_Finance/SalesReportData?period=${period}`);
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

    // 🎨 Draw donut chart
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

    // 🧾 Generate product list
    // 🧾 Generate product list
    const listContainer = document.getElementById("topProductsList");
    listContainer.innerHTML = `
        <h3 class="font-semibold mb-4 text-gray-800">Top ${topProducts.length}</h3>
        <div class="space-y-3"> ${topProducts.map((p, i) => {
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
