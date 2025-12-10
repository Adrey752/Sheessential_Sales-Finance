// ===========================
// CONSTANTS & GLOBALS
// ===========================
let currentExpenseId = null;
let currentExpenseAmount = null;
let selectedRowId = null;
let AVAILABLE_BALANCE = 0; // Will be set from view

// ===========================
// INITIALIZATION
// ===========================
document.addEventListener("DOMContentLoaded", function () {
    // Initialize balance check
    checkLowBalance();

    // Initialize filter buttons
    initializeFilterButtons();

    // Initialize modal handlers
    initializeModalHandlers();

    // Initialize expense action buttons
    initializeExpenseButtons();

    // Initialize ingredient request handlers
    initializeIngredientHandlers();

    // Initialize payroll handlers
    initializePayrollHandlers();

    // Select first payroll row if exists
    const firstRow = document.querySelector('.payroll-row');
    if (firstRow) {
        setTimeout(() => selectPayrollRow(firstRow), 100);
    }

    // Add keyboard navigation
    initializeKeyboardNavigation();
});

// ===========================
// UTILITY FUNCTIONS
// ===========================
function formatCurrency(amount) {
    return '₱' + parseFloat(amount).toLocaleString('en-PH', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function calculateBalanceAfter(expenseAmount) {
    const amount = parseFloat(expenseAmount.toString().replace(/,/g, ''));
    return AVAILABLE_BALANCE - amount;
}

function checkBalance(expenseAmount) {
    const amount = parseFloat(expenseAmount.toString().replace(/,/g, ''));
    return AVAILABLE_BALANCE >= amount;
}

function checkLowBalance() {
    const alertDiv = document.getElementById('balanceAlert');
    if (alertDiv && AVAILABLE_BALANCE < 20000) {
        alertDiv.classList.remove('hidden');
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ===========================
// FILTER FUNCTIONALITY
// ===========================
function initializeFilterButtons() {
    // Expense filter
    const filterButton = document.getElementById("filterButton");
    const filterDropdown = document.getElementById("filterDropdown");

    if (filterButton && filterDropdown) {
        filterButton.addEventListener("click", function (e) {
            e.stopPropagation();
            filterDropdown.classList.toggle("hidden");
        });

        document.addEventListener("click", function (e) {
            if (!filterDropdown.contains(e.target) && !filterButton.contains(e.target)) {
                filterDropdown.classList.add("hidden");
            }
        });
    }

    // Ingredient filter
    const filterButtonIngredient = document.getElementById("filterButtonIngredient");
    const filterDropdownIngredient = document.getElementById("filterDropdownIngredient");

    if (filterButtonIngredient && filterDropdownIngredient) {
        filterButtonIngredient.addEventListener("click", function (e) {
            e.stopPropagation();
            filterDropdownIngredient.classList.toggle("hidden");
        });

        document.addEventListener("click", function (e) {
            if (!filterDropdownIngredient.contains(e.target) && !filterButtonIngredient.contains(e.target)) {
                filterDropdownIngredient.classList.add("hidden");
            }
        });
    }

    // Payroll filter
    const filterButtonPayroll = document.getElementById("filterButtonPayroll");
    const filterDropdownPayroll = document.getElementById("filterDropdownPayroll");

    if (filterButtonPayroll && filterDropdownPayroll) {
        filterButtonPayroll.addEventListener("click", function (e) {
            e.stopPropagation();
            filterDropdownPayroll.classList.toggle("hidden");
        });

        document.addEventListener("click", function (e) {
            if (!filterDropdownPayroll.contains(e.target) && !filterButtonPayroll.contains(e.target)) {
                filterDropdownPayroll.classList.add("hidden");
            }
        });

        filterDropdownPayroll.addEventListener("click", function (e) {
            e.stopPropagation();
        });
    }
}

// ===========================
// MODAL HANDLERS
// ===========================
function initializeModalHandlers() {
    // Add expense modal
    const openModalBtn = document.getElementById("openModalBtn");
    const closeModalBtn = document.getElementById("closeModalBtn");
    const expenseModal = document.getElementById("expenseModal");

    if (openModalBtn && expenseModal) {
        openModalBtn.addEventListener("click", function () {
            expenseModal.classList.remove("hidden");
            expenseModal.classList.add("flex");
        });
    }

    if (closeModalBtn && expenseModal) {
        closeModalBtn.addEventListener("click", function () {
            expenseModal.classList.add("hidden");
            expenseModal.classList.remove("flex");
        });
    }

    // Preview modal
    const closePreviewBtn = document.getElementById("closePreviewBtn");
    const cancelPreviewBtn = document.getElementById("cancelPreviewBtn");

    if (closePreviewBtn) closePreviewBtn.addEventListener("click", closePreview);
    if (cancelPreviewBtn) cancelPreviewBtn.addEventListener("click", closePreview);

    // Decline modal
    const cancelDeclineBtn = document.getElementById("cancelDeclineBtn");
    if (cancelDeclineBtn) {
        cancelDeclineBtn.addEventListener("click", function () {
            const declineModal = document.getElementById("declineReasonModal");
            declineModal.classList.add("hidden");
            declineModal.classList.remove("flex");
            document.getElementById("declineForm").reset();
        });
    }

    // Transfer fund modal
    const cancelTransferBtn = document.getElementById("cancelTransferBtn");
    if (cancelTransferBtn) {
        cancelTransferBtn.addEventListener("click", function () {
            const transferModal = document.getElementById("transferFundModal");
            transferModal.classList.add("hidden");
            transferModal.classList.remove("flex");
            document.getElementById("approveForm").reset();
        });
    }

    // Expense form submission
    const expenseForm = document.getElementById("expenseForm");
    if (expenseForm) {
        expenseForm.addEventListener("submit", handleExpenseFormSubmit);
    }
}

function closePreview() {
    const previewModal = document.getElementById("previewModal");
    previewModal.classList.add("hidden");
    previewModal.classList.remove("flex");
}

// ===========================
// EXPENSE HANDLERS
// ===========================
function initializeExpenseButtons() {
    // View details buttons
    document.querySelectorAll(".view-details-btn").forEach(button => {
        button.addEventListener("click", function () {
            const expenseId = this.getAttribute("data-expense-id");
            const requester = this.getAttribute("data-requester");
            const department = this.getAttribute("data-department");
            const type = this.getAttribute("data-type");
            const amount = this.getAttribute("data-amount");
            const description = this.getAttribute("data-description");
            const notes = this.getAttribute("data-notes") || "No additional notes provided";
            const status = this.getAttribute("data-status");
            const id = this.getAttribute("data-id");

            currentExpenseId = id;
            currentExpenseAmount = amount;

            const balanceAfter = calculateBalanceAfter(amount);

            document.getElementById("preview-expense-id").textContent = expenseId;
            document.getElementById("preview-requester").textContent = requester;
            document.getElementById("preview-requester-initial").textContent = requester.substring(0, 1).toUpperCase();
            document.getElementById("preview-department").textContent = department;
            document.getElementById("preview-type").textContent = type;
            document.getElementById("preview-amount").textContent = "₱" + amount;
            document.getElementById("preview-description").textContent = description;
            document.getElementById("preview-notes").textContent = notes;
            document.getElementById("preview-status").textContent = status;
            document.getElementById("balance-after-approval").textContent = formatCurrency(balanceAfter);

            document.getElementById("previewModal").classList.remove("hidden");
            document.getElementById("previewModal").classList.add("flex");
        });
    });

    // Approve buttons
    document.querySelectorAll(".approveBtn").forEach(approveBtn => {
        approveBtn.addEventListener("click", handleApproveClick);
    });

    // Decline button
    const declineBtn = document.getElementById("declineBtn");
    if (declineBtn) {
        declineBtn.addEventListener("click", function () {
            closePreview();
            document.getElementById("decline-expense-id").value = currentExpenseId;
            document.getElementById("is-stock-request").value = false;

            const declineModal = document.getElementById("declineReasonModal");
            declineModal.classList.remove("hidden");
            declineModal.classList.add("flex");
        });
    }

    // Review buttons for approved/declined expenses
    document.querySelectorAll(".review-btn").forEach(btn => {
        btn.addEventListener("click", handleReviewClick);
    });
}

async function handleApproveClick(event) {
    ["previewModal", "ingredientRequestModal"].forEach(id => {
        const modal = document.getElementById(id);
        modal.classList.add("hidden");
        modal.classList.remove("flex");
    });

    const r = window.selectedStockRequest;
    const requestType = event.currentTarget.dataset.requestType;

    let amountToUse = 0;

    if (requestType === "stock_expense") {
        document.getElementById("approve-expense-id").value = "No Id yet";
        document.getElementById("request-id").value = r.Id;
        document.getElementById("transfer-amount-display").value = "₱" + r.TotalCost;
        document.getElementById("transfer-amount-display-main").textContent = "₱" + r.TotalCost;
        amountToUse = r.TotalCost;
    } else {
        document.getElementById("approve-expense-id").value = currentExpenseId;
        document.getElementById("transfer-amount-display").value = "₱" + currentExpenseAmount;
        document.getElementById("transfer-amount-display-main").textContent = "₱" + currentExpenseAmount;
        amountToUse = currentExpenseAmount;
    }

    const balanceAfter = calculateBalanceAfter(amountToUse);
    document.getElementById("balance-after-transfer").textContent = formatCurrency(balanceAfter);

    const isSufficient = checkBalance(amountToUse);
    const warningDiv = document.getElementById("insufficientBalanceWarning");
    const submitBtn = document.getElementById("confirmApproveBtn");

    if (!isSufficient) {
        warningDiv.classList.remove("hidden");
        submitBtn.disabled = true;
        submitBtn.classList.add("opacity-50", "cursor-not-allowed");
        document.getElementById("balance-after-transfer").classList.add("text-red-600");
    } else {
        // Auto-create expense if stock replenishment
        if (requestType === "stock_expense") {
            await createStockExpense(r);
        }
        warningDiv.classList.add("hidden");
        submitBtn.disabled = false;
    }

    document.getElementById("transferFundModal").classList.remove("hidden");
    document.getElementById("transferFundModal").classList.add("flex");
}

async function createStockExpense(r) {
    try {
        const stockExpenseData = {
            Department: "Inventory",
            ExpenseType: "Ingredient Stock Request",
            RequestedBy: r.RequestedBy,
            Amount: r.TotalCost,
            Description: `Replenishment: ${r.IngredientName} (${r.Quantity} ${r.Unit})`,
            Notes: "Generated via One-Click Approval.",
            isIngredientsRequest: true
        };

        const formData = new FormData();
        for (const key in stockExpenseData) {
            formData.append(key, stockExpenseData[key]);
        }

        const response1 = await fetch('/Sales_Finance/AddExpense', {
            method: 'POST',
            body: formData
        });

        const result1 = await response1.json();

        if (result1.success) {
            document.getElementById("approve-expense-id").value = result1.expenseId;

            const response2 = await fetch(`/Sales_Finance/UpdatedStockRequestStatus?id=${r.Id}&expenseId=${result1.expenseId}`, {
                method: "POST"
            });

            const result2 = await response2.json();
            if (!result2.success) {
                alert("Failed to update stock request status.");
            }
        } else {
            alert("Failed to submit stock request.");
        }
    } catch (error) {
        console.error("Network Error:", error);
    }
}

async function handleExpenseFormSubmit(e) {
    e.preventDefault();

    const formData = new FormData(this);
    const response = await fetch('/Sales_Finance/AddExpense', {
        method: 'POST',
        body: formData
    });

    const result = await response.json();

    if (result.success) {
        window.location.href = "/Sales_Finance/Expenses";
    } else {
        alert("Something went wrong.");
    }
}

function handleReviewClick() {
    const status = this.dataset.status;
    const respondedDate = this.dataset.respondedDate;

    document.getElementById("modal-title").textContent =
        status === "Approved" ? "Approved Expense Details" :
            status === "Declined" ? "Declined Expense Details" :
                "Expense Details";

    document.getElementById("modal-status-text").textContent = status;
    document.getElementById("modal-expense-id").textContent = this.dataset.expenseId;
    document.getElementById("modal-requester").textContent = this.dataset.requester;
    document.getElementById("modal-department").textContent = this.dataset.department;
    document.getElementById("modal-type").textContent = this.dataset.type;
    document.getElementById("modal-description").textContent = this.dataset.description;
    document.getElementById("modal-amount").textContent = "₱" + this.dataset.amount;

    // Show notes only for declined
    if (status === "Declined") {
        document.getElementById("modal-notes-section").classList.remove("hidden");
        document.getElementById("modal-notes").textContent = this.dataset.notes || "No reason provided.";
    } else {
        document.getElementById("modal-notes-section").classList.add("hidden");
    }

    // Show responded date
    if (respondedDate) {
        document.getElementById("modal-date-section").classList.remove("hidden");
        document.getElementById("modal-date-label").textContent =
            status === "Approved" ? "Date Approved" :
                status === "Declined" ? "Date Declined" :
                    "Date Responded";
        document.getElementById("modal-date-value").textContent = respondedDate;
    } else {
        document.getElementById("modal-date-section").classList.add("hidden");
    }

    document.getElementById("expenseModal").classList.remove("hidden");
}

function closeExpenseModal() {
    document.getElementById("expenseModal").classList.add("hidden");
}

// ===========================
// INGREDIENT REQUEST HANDLERS
// ===========================
function initializeIngredientHandlers() {
    const modal = document.getElementById('ingredientRequestModal');
    const closeBtn = document.getElementById('closeIngredientRequestBtn');
    const cancelBtn = document.getElementById('cancelRequestBtn');
    const declineBtn = document.getElementById('declineRequestBtn');

    if (closeBtn) closeBtn.addEventListener('click', closeIngredientModal);
    if (cancelBtn) cancelBtn.addEventListener('click', closeIngredientModal);

    if (declineBtn) {
        declineBtn.addEventListener("click", function () {
            document.getElementById("ingredientRequestModal").classList.add("hidden");
            document.getElementById("decline-expense-id").value = window.selectedStockRequest.Id;
            document.getElementById("is-stock-request").value = true;

            const declineModal = document.getElementById("declineReasonModal");
            declineModal.classList.remove("hidden");
            declineModal.classList.add("flex");
        });
    }

    // Close on backdrop click
    if (modal) {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) closeIngredientModal();
        });
    }
}

function closeIngredientModal() {
    const modal = document.getElementById('ingredientRequestModal');
    modal.classList.add('hidden');
    modal.classList.remove('flex');
}

// Global function for ingredient approval (called from HTML onclick)
window.approveRequest = async function (buttonElement) {
    const data = buttonElement.dataset;

    // Show view-only modal if already approved by finance
    if (data.status === "Approved by Finance") {
        await showTransactionDetails(data);
        return;
    }

    // Normal flow - show approval modal
    window.selectedStockRequest = {
        Id: data.id,
        expenseId: data.expenseId,
        RequestedBy: data.requester,
        SupplierName: data.supplier,
        IngredientName: data.ingredient,
        QuantityRequested: data.qty,
        Unit: data.unit,
        CurrentStockAtRequest: data.stock,
        RequestStatus: data.status,
        TotalCost: data.cost
    };

    const els = {
        id: document.getElementById('preview-request-id'),
        requesterName: document.getElementById('preview-requester-name'),
        requesterInitial: document.getElementById('preview-requester-initial'),
        supplier: document.getElementById('preview-supplier-name'),
        ingredient: document.getElementById('preview-ingredient-name'),
        quantity: document.getElementById('preview-quantity'),
        status: document.getElementById('preview-request-status'),
        cost: document.getElementById('preview-total_cost'),
        stockAfter: document.getElementById('stock-after-fulfillment')
    };

    els.id.textContent = `SRQ-${data.id.substr(data.id.length - 6)}`;
    els.requesterName.textContent = data.requester || 'Unknown';
    els.requesterInitial.textContent = (data.requester || 'U').charAt(0).toUpperCase();
    els.supplier.textContent = data.supplier;
    els.ingredient.textContent = data.ingredient;
    els.quantity.textContent = `${data.qty} ${data.unit}`;
    els.status.textContent = data.status;
    els.cost.textContent = formatCurrency(data.cost);

    const balanceAfter = calculateBalanceAfter(data.cost);
    els.stockAfter.textContent = formatCurrency(balanceAfter);

    const modal = document.getElementById('ingredientRequestModal');
    modal.classList.remove('hidden');
    modal.classList.add('flex');
};

async function showTransactionDetails(data) {
    const response = await fetch(`/Sales_Finance/GetExpenseAndPaymentDetails?expenseId=${data.expenseId}`);
    const result = await response.json();

    if (!result.success) {
        alert("Failed to load payment info.");
        return;
    }

    const expense = result.expense;
    const payment = result.payment;

    document.getElementById("transaction-request-id").textContent = `SRQ-${data.id.substr(data.id.length - 6)}`;
    document.getElementById("transaction-requester").textContent = expense.requestedBy || data.requester;
    document.getElementById("transaction-supplier").textContent = expense.supplierName || data.supplier;
    document.getElementById("transaction-ingredient").textContent = expense.ingredientName || data.ingredient;
    document.getElementById("transaction-qty").textContent = data.qty;
    document.getElementById("transaction-total-cost").textContent = formatCurrency(payment.amount);

    if (payment) {
        document.getElementById("payment-section").classList.remove("hidden");
        document.getElementById("payment-amount").textContent = formatCurrency(payment.amount);
        document.getElementById("payment-date").textContent = new Date(payment.paymentDate).toLocaleString("en-PH");
        document.getElementById("payment-method").textContent = payment.paymentMethod;
        document.getElementById("payment-transfer-to").textContent = payment.transferTo;
        document.getElementById("payment-ref").textContent = payment.referenceNumber || "—";
        document.getElementById("payment-notes").textContent = payment.notes || "—";
    } else {
        document.getElementById("payment-section").classList.add("hidden");
    }

    const transModal = document.getElementById("transactionDetailsModal");
    transModal.classList.remove("hidden");
    transModal.classList.add("flex");
}

function closeTransactionModal() {
    const modal = document.getElementById("transactionDetailsModal");
    modal.classList.add("hidden");
    modal.classList.remove("flex");
}

// Make closeTransactionModal global
window.closeTransactionModal = closeTransactionModal;

// ===========================
// PAYROLL HANDLERS
// ===========================
function initializePayrollHandlers() {
    // Payroll row clicks are handled by inline onclick="selectPayrollRow(this)"
    // but we can add additional handlers here if needed
}

function selectPayrollRow(row) {
    // Remove highlight from all rows
    document.querySelectorAll('.payroll-row').forEach(r => {
        r.classList.remove('bg-blue-50', 'border-l-4', 'border-l-blue-500', 'shadow-sm');
        r.classList.add('hover:bg-gray-50');
    });

    // Add highlight to clicked row
    row.classList.remove('hover:bg-gray-50');
    row.classList.add('bg-blue-50', 'border-l-4', 'border-l-blue-500', 'shadow-sm');

    selectedRowId = row.dataset.id;

    try {
        const payslipsData = JSON.parse(row.dataset.payslips);
        const payrunNumber = row.cells[0].textContent;
        const payPeriod = row.cells[1].textContent;
        const payDate = row.cells[2].textContent;
        const payType = row.cells[3].textContent;
        const employeeCount = row.cells[4].textContent;
        const grossTotal = row.cells[5].textContent;
        const netTotal = row.cells[6].textContent;

        document.getElementById('selectedPayrunInfo').innerHTML =
            `<span class="font-semibold">${payrunNumber}</span> • ${payDate} • ${payType}<br>
             <span class="text-xs">${employeeCount} employees • Gross: ${grossTotal} • Net: ${netTotal}</span>`;

        displayPayslipsTable(payslipsData, payrunNumber);
    } catch (error) {
        console.error('Error parsing payslips data:', error);
        document.getElementById('payslipTableContainer').innerHTML =
            '<div class="text-center py-10 text-red-500">Error loading payslip data</div>';
    }
}

function displayPayslipsTable(payslips, payrunNumber) {
    const container = document.getElementById('payslipTableContainer');

    if (!payslips || payslips.length === 0) {
        container.innerHTML = `
            <div class="h-full flex flex-col items-center justify-center text-gray-400">
                <div class="text-center">
                    <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-yellow-50 flex items-center justify-center">
                        <svg class="w-8 h-8 text-yellow-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.464 0L4.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                        </svg>
                    </div>
                    <h3 class="text-lg font-medium text-gray-600 mb-2">No Payslips Found</h3>
                    <p class="text-sm text-gray-500">No payslip data available for this pay run</p>
                </div>
            </div>
        `;
        return;
    }

    const tableHtml = `
        <div class="mb-4">
            <div class="flex justify-between items-center mb-3">
                <h3 class="font-semibold text-gray-700">Employee Payslips</h3>
                <span class="text-sm text-gray-500">${payslips.length} records</span>
            </div>
            <div class="overflow-x-auto rounded-lg border border-gray-200">
                <table class="min-w-full divide-y divide-gray-200">
                    <thead class="bg-gray-50">
                        <tr>
                            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Employee</th>
                            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Position</th>
                            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Days</th>
                            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Gross</th>
                            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Net</th>
                        </tr>
                    </thead>
                    <tbody class="bg-white divide-y divide-gray-200">
                        ${payslips.map((payslip, index) => `
                            <tr class="${index % 2 === 0 ? 'bg-white' : 'bg-gray-50'} hover:bg-gray-100 transition-colors">
                                <td class="px-4 py-3 whitespace-nowrap">
                                    <div class="flex items-center">
                                        <div class="flex-shrink-0 h-8 w-8 bg-blue-100 rounded-full flex items-center justify-center mr-3">
                                            <span class="text-xs font-medium text-blue-600">
                                                ${(payslip.EmployeeName || 'N/A').charAt(0)}
                                            </span>
                                        </div>
                                        <div>
                                            <div class="text-sm font-medium text-gray-900">${escapeHtml(payslip.EmployeeName || 'N/A')}</div>
                                            <div class="text-xs text-gray-500">${escapeHtml(payslip.Department || '')}</div>
                                        </div>
                                    </div>
                                </td>
                                <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-700">${escapeHtml(payslip.Position || 'N/A')}</td>
                                <td class="px-4 py-3 whitespace-nowrap text-center">
                                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                                        ${payslip.TotalWorkingDays || 0}
                                    </span>
                                </td>
                                <td class="px-4 py-3 whitespace-nowrap text-sm font-semibold text-gray-900">${formatCurrency(payslip.GrossSalary || 0)}</td>
                                <td class="px-4 py-3 whitespace-nowrap text-sm font-semibold text-green-600">${formatCurrency(payslip.NetSalary || 0)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                    <tfoot class="bg-gray-50 border-t border-gray-200">
                        <tr>
                            <td colspan="3" class="px-4 py-3 text-sm font-semibold text-gray-700 text-right">Totals:</td>
                            <td class="px-4 py-3 text-sm font-bold text-gray-900 border-l border-gray-200">
                                ${formatCurrency(payslips.reduce((sum, p) => sum + (p.GrossSalary || 0), 0))}
                            </td>
                            <td class="px-4 py-3 text-sm font-bold text-green-600 border-l border-gray-200">
                                ${formatCurrency(payslips.reduce((sum, p) => sum + (p.NetSalary || 0), 0))}
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>
            <div class="mt-4 grid grid-cols-2 gap-3">
                <div class="bg-blue-50 p-3 rounded-lg">
                    <p class="text-xs text-blue-600 font-medium">Average Gross</p>
                    <p class="text-lg font-bold text-blue-700">
                        ${formatCurrency(payslips.reduce((sum, p) => sum + (p.GrossSalary || 0), 0) / payslips.length)}
                    </p>
                </div>
                <div class="bg-green-50 p-3 rounded-lg">
                    <p class="text-xs text-green-600 font-medium">Average Net</p>
                    <p class="text-lg font-bold text-green-700">
                        ${formatCurrency(payslips.reduce((sum, p) => sum + (p.NetSalary || 0), 0) / payslips.length)}
                    </p>
                </div>
            </div>
        </div>
    `;

    container.innerHTML = tableHtml;
}

async function releasePayroll(id, status) {
    if (status === "Approved") {
        document.getElementById("approveModal").classList.remove("hidden");
        document.getElementById("approveConfirmBtn").onclick = async () => {
            await sendPayrollRequest(id, status);
            closeApproveModal();
        };
    } else {
        document.getElementById("declineModal").classList.remove("hidden");
        document.getElementById("declineConfirmBtn").onclick = async () => {
            await sendPayrollRequest(id, status);
            closeDeclineModal();
        };
    }
}

async function sendPayrollRequest(id, status) {
    const response = await fetch(`/Sales_Finance/ReleasePayroll?id=${id}&status=${status}`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        }
    });

    const result = await response.json();

    if (result.success) {
        location.reload();
    } else {
        alert("Failed to release payroll.");
    }
}

function closeApproveModal() {
    document.getElementById("approveModal").classList.add("hidden");
}

function closeDeclineModal() {
    document.getElementById("declineModal").classList.add("hidden");
}

function resetPayrollFilters() {
    document.getElementById("filterFormPayroll").reset();
    applyPayrollFilters();
}

function applyPayrollFilters() {
    const tbody = document.querySelector("#payrollTableBody");

    if (!tbody) {
        console.error("Error: The table body with ID '#payrollTableBody' was not found.");
        return;
    }

    const rows = Array.from(tbody.querySelectorAll("tr"));
    const selectedCategories = Array.from(document.querySelectorAll("input[name='category']:checked"))
        .map(cb => cb.value);

    let filteredRows = rows;

    if (selectedCategories.length > 0) {
        filteredRows = filteredRows.filter(row => {
            const status = row.dataset.status;
            return selectedCategories.includes(status);
        });
    }

    const sortBy = document.getElementById("sortBy").value;

    filteredRows.sort((a, b) => {
        const getValue = (row, type) => {
            switch (type) {
                case "date":
                    const dateValue = new Date(row.dataset.date);
                    return isNaN(dateValue.getTime()) ? 0 : dateValue.getTime();
                case "gross":
                    const grossValue = row.dataset.gross ? row.dataset.gross.replace(/,/g, '') : '0';
                    return parseFloat(grossValue) || 0;
                case "count":
                    return parseInt(row.dataset.count) || 0;
                default: return 0;
            }
        };

        switch (sortBy) {
            case "date-desc": return getValue(b, "date") - getValue(a, "date");
            case "date-asc": return getValue(a, "date") - getValue(b, "date");
            case "gross-desc": return getValue(b, "gross") - getValue(a, "gross");
            case "gross-asc": return getValue(a, "gross") - getValue(b, "gross");
            case "count-desc": return getValue(b, "count") - getValue(a, "count");
            case "count-asc": return getValue(a, "count") - getValue(b, "count");
            default: return 0;
        }
    });

    tbody.innerHTML = "";
    filteredRows.forEach(r => tbody.appendChild(r));
}

// ===========================
// KEYBOARD NAVIGATION
// ===========================
function initializeKeyboardNavigation() {
    document.addEventListener('keydown', function (e) {
        if (!selectedRowId) return;

        const rows = Array.from(document.querySelectorAll('.payroll-row'));
        const currentIndex = rows.findIndex(row => row.dataset.id === selectedRowId);

        if (e.key === 'ArrowDown' && currentIndex < rows.length - 1) {
            selectPayrollRow(rows[currentIndex + 1]);
            e.preventDefault();
        } else if (e.key === 'ArrowUp' && currentIndex > 0) {
            selectPayrollRow(rows[currentIndex - 1]);
            e.preventDefault();
        }
    });
}

// ===========================
// GLOBAL EXPORTS
// ===========================
window.selectPayrollRow = selectPayrollRow;
window.releasePayroll = releasePayroll;
window.closeApproveModal = closeApproveModal;
window.closeDeclineModal = closeDeclineModal;
window.resetPayrollFilters = resetPayrollFilters;
window.applyPayrollFilters = applyPayrollFilters;
window.closeExpenseModal = closeExpenseModal;

// Function to set available balance from view
window.setAvailableBalance = function (balance) {
    AVAILABLE_BALANCE = balance;
};


// Add at the end of the file

// ===========================
// PAGINATION HELPERS
// ===========================

/**
 * Preserves current filter and pagination state when navigating
 */
function preservePaginationState(targetPage, section) {
    const url = new URL(window.location.href);
    const params = new URLSearchParams(url.search);

    // Set the target page for the specific section
    if (section === 'expense') {
        params.set('expensePage', targetPage);
    } else if (section === 'ingredient') {
        params.set('ingredientPage', targetPage);
    } else if (section === 'payroll') {
        params.set('payrollPage', targetPage);
    }

    window.location.href = `${url.pathname}?${params.toString()}`;
}

/**
 * Updates pagination links to preserve filter state
 */
function initializePaginationLinks() {
    document.querySelectorAll('a[href*="Page="]').forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const href = this.getAttribute('href');
            const url = new URL(window.location.href);
            const newParams = new URLSearchParams(href.split('?')[1]);

            // Merge existing params with new page param
            const params = new URLSearchParams(url.search);
            newParams.forEach((value, key) => {
                params.set(key, value);
            });

            window.location.href = `${url.pathname}?${params.toString()}`;
        });
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializePaginationLinks();

    // Scroll to the active table section if pagination was used
    const params = new URLSearchParams(window.location.search);
    if (params.has('ingredientPage')) {
        document.querySelector('#ingredientRequestsSection')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } else if (params.has('payrollPage')) {
        document.querySelector('#payrollSection')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
});

// Export functions for use in other parts of the application
window.preservePaginationState = preservePaginationState;