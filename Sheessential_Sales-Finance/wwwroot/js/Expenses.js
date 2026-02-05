// ===========================
// GLOBAL STATE
// ===========================
let availableBalance = 0;

window.setAvailableBalance = function (balance) {
    availableBalance = balance;
    console.log("💰 Available balance set:", balance);
};

// ===========================
// INITIALIZE ALL HANDLERS
// ===========================
document.addEventListener('DOMContentLoaded', function () {
    console.log("🟢 Expenses.js loaded");
    
    initializeReviewButtons();
    initializePendingExpenseHandlers();
    initializeDeclineModal();
    initializeTransferModal();
    initializeIngredientHandlers();
    initializeFilterHandlers();
    initializePayrollFilters(); // Add this line
});

// ===========================
// REVIEW BUTTONS (View Approved/Declined)
// ===========================
function initializeReviewButtons() {
    document.querySelectorAll('.review-btn').forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            handleReviewClick(this);
        });
    });
}

function handleReviewClick(button) {
    console.log("📋 Review button clicked");
    
    const modal = document.getElementById('expenseModal');
    
    if (!modal) {
        console.error("❌ expenseModal not found!");
        return;
    }

    const mode = button.getAttribute('data-mode');
    const expenseId = button.getAttribute('data-expense-id');
    const requester = button.getAttribute('data-requester');
    const department = button.getAttribute('data-department');
    const type = button.getAttribute('data-type');
    const amount = button.getAttribute('data-amount');
    const description = button.getAttribute('data-description');
    const notes = button.getAttribute('data-notes');
    const respondedDate = button.getAttribute('data-responded-date');

    // Populate modal
    const modalExpenseId = document.getElementById('modal-expense-id');
    const modalRequester = document.getElementById('modal-requester');
    const modalDepartment = document.getElementById('modal-department');
    const modalType = document.getElementById('modal-type');
    const modalAmount = document.getElementById('modal-amount');
    const modalDescription = document.getElementById('modal-description');

    if (modalExpenseId) modalExpenseId.textContent = expenseId || 'N/A';
    if (modalRequester) modalRequester.textContent = requester || 'N/A';
    if (modalDepartment) modalDepartment.textContent = department || 'N/A';
    if (modalType) modalType.textContent = type || 'N/A';
    if (modalAmount) modalAmount.textContent = `₱${parseFloat(amount || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;
    if (modalDescription) modalDescription.textContent = description || 'No description';

    const modalTitle = document.getElementById('modal-title');
    const modalStatusText = document.getElementById('modal-status-text');
    const modalDateSection = document.getElementById('modal-date-section');
    const modalDateLabel = document.getElementById('modal-date-label');
    const modalDateValue = document.getElementById('modal-date-value');
    const modalNotesSection = document.getElementById('modal-notes-section');
    const modalNotes = document.getElementById('modal-notes');

    if (mode === 'approved') {
        if (modalTitle) modalTitle.textContent = 'Approved Expense Details';
        if (modalStatusText) modalStatusText.textContent = 'This expense has been approved';
        if (modalDateSection) modalDateSection.classList.remove('hidden');
        if (modalDateLabel) modalDateLabel.textContent = 'Date Approved';
        if (modalDateValue) modalDateValue.textContent = respondedDate || 'N/A';
        if (modalNotesSection) modalNotesSection.classList.add('hidden');
    } else if (mode === 'declined') {
        if (modalTitle) modalTitle.textContent = 'Declined Expense Details';
        if (modalStatusText) modalStatusText.textContent = 'This expense was declined';
        if (modalDateSection) modalDateSection.classList.remove('hidden');
        if (modalDateLabel) modalDateLabel.textContent = 'Date Declined';
        if (modalDateValue) modalDateValue.textContent = respondedDate || 'N/A';
        if (modalNotesSection) modalNotesSection.classList.remove('hidden');
        if (modalNotes) modalNotes.textContent = notes || 'No reason provided';
    }

    modal.classList.remove('hidden');
    modal.classList.add('flex');
}

window.closeExpenseModal = function () {
    const modal = document.getElementById('expenseModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
    }
};

// ===========================
// PENDING EXPENSE HANDLERS (Review button)
// ===========================
function initializePendingExpenseHandlers() {
    document.querySelectorAll('.view-details-btn').forEach(button => {
        button.addEventListener('click', function () {
            openPreview(this);
        });
    });
}

function openPreview(button) {
    const expenseId = button.getAttribute('data-expense-id');
    const requester = button.getAttribute('data-requester');
    const department = button.getAttribute('data-department');
    const type = button.getAttribute('data-type');
    const amount = parseFloat(button.getAttribute('data-amount'));
    const description = button.getAttribute('data-description');
    const notes = button.getAttribute('data-notes');
    const id = button.getAttribute('data-id');

    document.getElementById('preview-expense-id').textContent = expenseId;
    document.getElementById('preview-requester').textContent = requester;
    document.getElementById('preview-requester-initial').textContent = requester.charAt(0).toUpperCase();
    document.getElementById('preview-department').textContent = department;
    document.getElementById('preview-type').textContent = type;
    document.getElementById('preview-amount').textContent = `₱${amount.toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;
    document.getElementById('preview-description').textContent = description;
    document.getElementById('preview-notes').textContent = notes || 'No additional notes';
    document.getElementById('preview-status').textContent = 'Pending Review';

    const balanceAfter = availableBalance - amount;
    document.getElementById('balance-after-approval').textContent = `₱${balanceAfter.toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;

    const previewModal = document.getElementById('previewModal');
    previewModal.classList.remove('hidden');
    previewModal.classList.add('flex');

    const approveBtn = previewModal.querySelector('.approveBtn');
    if (approveBtn) {
        approveBtn.onclick = function () {
            openTransferModal(id, amount);
        };
    }

    const declineBtn = document.getElementById('declineBtn');
    if (declineBtn) {
        declineBtn.onclick = function () {
            openDeclineModal(id, false);
        };
    }
}

function closePreview() {
    const previewModal = document.getElementById('previewModal');
    if (previewModal) {
        previewModal.classList.add('hidden');
        previewModal.classList.remove('flex');
    }
}

// ===========================
// DECLINE MODAL
// ===========================
function initializeDeclineModal() {
    const cancelBtn = document.getElementById('cancelDeclineBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            closeDeclineModal();
        });
    }

    const closePreviewBtn = document.getElementById('closePreviewBtn');
    const cancelPreviewBtn = document.getElementById('cancelPreviewBtn');
    if (closePreviewBtn) closePreviewBtn.addEventListener('click', closePreview);
    if (cancelPreviewBtn) cancelPreviewBtn.addEventListener('click', closePreview);
}

function openDeclineModal(expenseId, isStockRequest) {
    document.getElementById('decline-expense-id').value = expenseId;
    document.getElementById('is-stock-request').value = isStockRequest ? 'true' : 'false';

    const modal = document.getElementById('declineReasonModal');
    modal.classList.remove('hidden');
    modal.classList.add('flex');

    closePreview();
}

function closeDeclineModal() {
    const modal = document.getElementById('declineReasonModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
    }
}

// ===========================
// TRANSFER MODAL
// ===========================
function initializeTransferModal() {
    const cancelBtn = document.getElementById('cancelTransferBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            closeTransferModal();
        });
    }
}

function openTransferModal(expenseId, amount) {
    document.getElementById('approve-expense-id').value = expenseId;
    document.getElementById('transfer-amount-display').value = `₱${amount.toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;
    document.getElementById('transfer-amount-display-main').textContent = `₱${amount.toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;

    const balanceAfter = availableBalance - amount;
    document.getElementById('balance-after-transfer').textContent = `₱${balanceAfter.toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;

    const modal = document.getElementById('transferFundModal');
    modal.classList.remove('hidden');
    modal.classList.add('flex');

    closePreview();
}

function closeTransferModal() {
    const modal = document.getElementById('transferFundModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
    }
}

// ===========================
// INGREDIENT HANDLERS
// ===========================
function initializeIngredientHandlers() {
    const closeIngredientBtn = document.getElementById('closeIngredientRequestBtn');
    const cancelRequestBtn = document.getElementById('cancelRequestBtn');
    
    if (closeIngredientBtn) {
        closeIngredientBtn.addEventListener('click', closeIngredientModal);
    }
    if (cancelRequestBtn) {
        cancelRequestBtn.addEventListener('click', closeIngredientModal);
    }

    const declineRequestBtn = document.getElementById('declineRequestBtn');
    if (declineRequestBtn) {
        declineRequestBtn.addEventListener('click', function() {
            // Add your decline logic here
        });
    }
}

function closeIngredientModal() {
    const modal = document.getElementById('ingredientRequestModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
    }
}

// Make it globally available
window.approveRequest = function(button) {
    const modal = document.getElementById('ingredientRequestModal');
    if (!modal) return;

    const id = button.getAttribute('data-id');
    const expenseId = button.getAttribute('data-expense-id');
    const requester = button.getAttribute('data-requester');
    const supplier = button.getAttribute('data-supplier');
    const ingredient = button.getAttribute('data-ingredient');
    const qty = button.getAttribute('data-qty');
    const unit = button.getAttribute('data-unit');
    const stock = button.getAttribute('data-stock');
    const status = button.getAttribute('data-status');
    const cost = button.getAttribute('data-cost');

    document.getElementById('preview-request-id').textContent = id.substring(id.length - 6);
    document.getElementById('preview-requester-name').textContent = requester;
    document.getElementById('preview-supplier-name').textContent = supplier;
    document.getElementById('preview-ingredient-name').textContent = ingredient;
    document.getElementById('preview-quantity').textContent = `${qty} ${unit}`;
    document.getElementById('preview-request-status').textContent = status;
    document.getElementById('preview-total_cost').textContent = `₱${parseFloat(cost).toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;
    document.getElementById('current-stock').textContent = stock;

    const balanceAfter = availableBalance - parseFloat(cost);
    document.getElementById('stock-after-fulfillment').textContent = `₱${balanceAfter.toLocaleString('en-PH', { minimumFractionDigits: 2 })}`;

    modal.classList.remove('hidden');
    modal.classList.add('flex');
};

// ===========================
// FILTER HANDLERS
// ===========================
function initializeFilterHandlers() {
    const filterButton = document.getElementById('filterButton');
    const filterDropdown = document.getElementById('filterDropdown');

    if (filterButton && filterDropdown) {
        filterButton.addEventListener('click', function(e) {
            e.stopPropagation();
            filterDropdown.classList.toggle('hidden');
        });

        document.addEventListener('click', function(e) {
            if (!filterDropdown.contains(e.target) && e.target !== filterButton) {
                filterDropdown.classList.add('hidden');
            }
        });
    }

    // Ingredient filter
    const filterButtonIngredient = document.getElementById('filterButtonIngredient');
    const filterDropdownIngredient = document.getElementById('filterDropdownIngredient');

    if (filterButtonIngredient && filterDropdownIngredient) {
        filterButtonIngredient.addEventListener('click', function(e) {
            e.stopPropagation();
            filterDropdownIngredient.classList.toggle('hidden');
        });

        document.addEventListener('click', function(e) {
            if (!filterDropdownIngredient.contains(e.target) && e.target !== filterButtonIngredient) {
                filterDropdownIngredient.classList.add('hidden');
            }
        });
    }
}

// ===========================
// PAYROLL HANDLERS
// ===========================
window.selectPayrollRow = function(row) {
    console.log("📋 Payroll row clicked");
    
    // Remove highlight from all rows
    document.querySelectorAll('.payroll-row').forEach(r => {
        r.classList.remove('bg-blue-50', 'border-l-4', 'border-blue-500');
    });
    
    // Highlight selected row
    row.classList.add('bg-blue-50', 'border-l-4', 'border-blue-500');
    
    // Get payslips data
    const payslipsJson = row.getAttribute('data-payslips');
    const payRunId = row.getAttribute('data-id');
    
    if (!payslipsJson) {
        console.error("No payslips data found");
        return;
    }
    
    let payslips;
    try {
        payslips = JSON.parse(payslipsJson);
    } catch (e) {
        console.error("Failed to parse payslips JSON:", e);
        return;
    }
    
    console.log("Payslips:", payslips);
    
    // Update info text
    const infoText = document.getElementById('selectedPayrunInfo');
    if (infoText) {
        infoText.textContent = `${payslips.length} payslip(s) for this pay run`;
    }
    
    // Get container
    const container = document.getElementById('payslipTableContainer');
    if (!container) {
        console.error("Container not found");
        return;
    }
    
    // Clear container
    container.innerHTML = '';
    
    if (!payslips || payslips.length === 0) {
        container.innerHTML = `
            <div class="h-full flex flex-col items-center justify-center text-gray-400">
                <div class="text-center">
                    <svg class="w-16 h-16 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                              d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                    </svg>
                    <h3 class="text-lg font-medium text-gray-500 mb-2">No Payslips Available</h3>
                    <p class="text-sm text-gray-400">This pay run has no payslip records</p>
                </div>
            </div>
        `;
        return;
    }
    
    // Build payslips table
    let html = `
        <div class="space-y-3">
            ${payslips.map((slip, index) => `
                <div class="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start mb-3">
                        <div>
                            <h4 class="font-semibold text-gray-900">${slip.EmployeeName || 'Unknown Employee'}</h4>
                            <p class="text-sm text-gray-500">${slip.EmployeeId || 'N/A'} • ${slip.Department || 'N/A'}</p>
                        </div>
                        <span class="px-3 py-1 bg-blue-100 text-blue-800 text-xs font-semibold rounded-full">
                            ${slip.Position || 'N/A'}
                        </span>
                    </div>
                    
                    <div class="grid grid-cols-2 gap-3 text-sm">
                        <div class="bg-gray-50 p-3 rounded">
                            <p class="text-gray-600 text-xs mb-1">Gross Salary</p>
                            <p class="font-semibold text-gray-900">₱${(slip.GrossSalary || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</p>
                        </div>
                        
                        <div class="bg-gray-50 p-3 rounded">
                            <p class="text-gray-600 text-xs mb-1">Net Salary</p>
                            <p class="font-semibold text-green-700">₱${(slip.NetSalary || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</p>
                        </div>
                        
                        <div class="bg-red-50 p-3 rounded">
                            <p class="text-gray-600 text-xs mb-1">Deductions</p>
                            <p class="font-semibold text-red-700">₱${(slip.TotalDeductions || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</p>
                        </div>
                        
                        <div class="bg-blue-50 p-3 rounded">
                            <p class="text-gray-600 text-xs mb-1">Allowances</p>
                            <p class="font-semibold text-blue-700">₱${(slip.TotalAllowances || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</p>
                        </div>
                    </div>
                    
                    ${slip.Deductions && slip.Deductions.length > 0 ? `
                        <div class="mt-3 pt-3 border-t border-gray-200">
                            <p class="text-xs font-medium text-gray-600 mb-2">Breakdown:</p>
                            <div class="grid grid-cols-2 gap-2 text-xs">
                                ${slip.Deductions.map(d => `
                                    <div class="flex justify-between">
                                        <span class="text-gray-600">${d.Type || 'N/A'}:</span>
                                        <span class="font-medium text-gray-900">₱${(d.Amount || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</span>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                </div>
            `).join('')}
        </div>
    `;
    
    container.innerHTML = html;
};

// Payroll approval/decline functions
window.releasePayroll = function(payrollId, status) {
    console.log(`Releasing payroll: ${payrollId} with status: ${status}`);
    
    if (!confirm(`Are you sure you want to ${status.toLowerCase()} this payroll?`)) {
        return;
    }
    
    fetch('/Sales_Finance/ReleasePayroll', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ id: payrollId, status: status })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            alert(`✅ Payroll ${status.toLowerCase()} successfully!`);
            location.reload();
        } else {
            alert(`❌ Failed to ${status.toLowerCase()} payroll: ${data.message || 'Unknown error'}`);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert(`❌ An error occurred while processing payroll.`);
    });
};

window.resetPayrollFilters = function() {
    const form = document.getElementById('filterFormPayroll');
    if (form) {
        form.reset();
    }
};

window.applyPayrollFilters = function() {
    // Add your filter logic here
    console.log("Applying payroll filters...");
    const form = document.getElementById('filterFormPayroll');
    if (form) {
        // You can submit the form or apply filters via AJAX
        form.submit();
    }
};

// Initialize payroll filter dropdown
function initializePayrollFilters() {
    const filterBtn = document.getElementById('filterButtonPayroll');
    const filterDropdown = document.getElementById('filterDropdownPayroll');
    
    if (filterBtn && filterDropdown) {
        filterBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            filterDropdown.classList.toggle('hidden');
        });
        
        document.addEventListener('click', function(e) {
            if (!filterDropdown.contains(e.target) && e.target !== filterBtn) {
                filterDropdown.classList.add('hidden');
            }
        });
    }
}