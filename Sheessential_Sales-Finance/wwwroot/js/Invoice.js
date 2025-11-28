const { invoices, customers, availableProducts } = window.appData;

// -- ===== New Invoice Modal Toggling ===== -->

document.querySelectorAll('[data-modal-target]').forEach(btn => {
    btn.addEventListener('click', () => {
        const target = document.getElementById(btn.dataset.modalTarget);
        target.classList.remove('hidden');
        target.classList.add('flex');
    });
});

document.querySelectorAll('[data-modal-close]').forEach(btn => {
    btn.addEventListener('click', () => {
        const modal = btn.closest('#newInvoiceModal') || btn.closest('.fixed');
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
        }
    });
});


// -- ===== FUNCTIONS IN NEW INVOICE MODAL ===== -->

// 1. VALIDATION FOR ENTERING A CUSTOMER
document.querySelector("#createInvoiceForm")?.addEventListener("submit", function (e) {
    const billedTo = document.getElementById("selectedCustomerId").value.trim();
    const customerError = document.getElementById("customerError");
    const customerDetails = document.getElementById("customerDetails");
    const customerSearchInput = document.getElementById("customerSearchInput");

    if (!billedTo) {
        e.preventDefault();

        // Show error styles
        customerError.classList.remove("hidden");
        customerDetails.classList.add("border-red-400");
        customerSearchInput.classList.add("border-red-400", "ring-2", "ring-red-300");

        // scroll to the customer section
        customerSearchInput.scrollIntoView({
            behavior: "smooth",
            block: "center"
        });

        //focus the search input for convenience
        customerSearchInput.focus();

    } else {
        // Remove error styles if valid
        customerError.classList.add("hidden");
        customerDetails.classList.remove("border-red-400");
        customerSearchInput.classList.remove("border-red-400", "ring-2", "ring-red-300");
    }
});


// 2. SEARCHING PRODUCTS IN NEW INVOICE MODAL      
document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById("productSearchInput");
    const suggestions = document.getElementById("productSuggestions");
    // Note: This targets the table inside the modal
    const rows = Array.from(document.querySelectorAll("#productTableBody tr"));

    if (!searchInput) return; // Guard clause if modal not present

    function showSuggestions(term) {
        suggestions.innerHTML = "";
        if (!term || term.length < 1) {
            suggestions.classList.add("hidden");
            return;
        }

        const matches = rows.filter(r =>
            r.textContent.toLowerCase().includes(term.toLowerCase())
        );

        if (matches.length === 0) {
            suggestions.innerHTML = `<div class='p-2 text-gray-500 text-sm'>No results</div>`;
            suggestions.classList.remove("hidden");
            return;
        }

        matches.slice(0, 8).forEach(row => {
            const el = document.createElement("div");
            el.className =
                "p-2 hover:bg-pink-50 cursor-pointer text-sm border-b border-gray-100";
            // Assuming first cell has product name
            el.textContent = row.children[0].textContent.trim();
            el.addEventListener("click", () => scrollToRow(row));
            suggestions.appendChild(el);
        });

        suggestions.classList.remove("hidden");
    }

    function scrollToRow(row) {
        // Find the scrolling container
        const container = document.querySelector(".max-h-60.overflow-y-auto") || document.querySelector(".border.border-gray-200.rounded-xl.bg-white.max-h-60");

        if (!container) {
            row.scrollIntoView({ behavior: "smooth", block: "center" });
            return;
        }

        // Calculate scroll position
        const containerRect = container.getBoundingClientRect();
        const rowRect = row.getBoundingClientRect();
        const currentScroll = container.scrollTop;
        const offsetTopInViewport = rowRect.top - containerRect.top;
        const desiredScrollTop = currentScroll + offsetTopInViewport - (container.clientHeight / 2) + (rowRect.height / 2);
        const maxScroll = container.scrollHeight - container.clientHeight;
        const finalScroll = Math.max(0, Math.min(desiredScrollTop, maxScroll));

        container.scrollTo({ top: finalScroll, behavior: "smooth" });

        // Visual feedback
        row.classList.add("bg-pink-50");
        setTimeout(() => row.classList.remove("bg-pink-50"), 900);

        suggestions.classList.add("hidden");
        searchInput.value = "";
    }

    searchInput.addEventListener("input", e => showSuggestions(e.target.value));

    searchInput.addEventListener("keydown", e => {
        if (e.key === "Enter") {
            e.preventDefault();
            const first = suggestions.querySelector("div");
            if (first) first.click();
        }
    });

    document.addEventListener("click", e => {
        if (suggestions && !e.target.closest("#productSuggestions") && e.target !== searchInput)
            suggestions.classList.add("hidden");
    });
});


// 3. CUSTOMER SEARCHING (UPDATED FOR TbUser Model)
document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById("customerSearchInput");
    const suggestions = document.getElementById("customerSuggestions");
    const detailsArea = document.getElementById("customerDetails");
    const hiddenCustomerId = document.getElementById("selectedCustomerId");

    if (!searchInput) return;

    searchInput.addEventListener("input", () => {
        const query = searchInput.value.toLowerCase().trim();
        suggestions.innerHTML = "";

        if (!query) {
            suggestions.classList.add("hidden");
            return;
        }

        // Filter customers (TbUser)
        const matches = customers.filter(c =>
            c.FirstName.toLowerCase().includes(query) ||
            c.LastName.toLowerCase().includes(query) ||
            (c.Email && c.Email.toLowerCase().includes(query))
        );

        if (!matches.length) {
            suggestions.classList.add("hidden");
            return;
        }

        matches.forEach(c => {
            const div = document.createElement("div");
            div.className = "px-4 py-2 hover:bg-pink-50 cursor-pointer text-sm border-b border-gray-50";
            div.textContent = `${c.FirstName} ${c.LastName} ${c.Email ? '(' + c.Email + ')' : ''}`;
            div.addEventListener("click", () => selectCustomer(c));
            suggestions.appendChild(div);
        });

        suggestions.classList.remove("hidden");
    });

    function selectCustomer(c) {
        searchInput.value = `${c.FirstName} ${c.LastName}`;
        hiddenCustomerId.value = c.Id;
        suggestions.classList.add("hidden");

        // Safe access to nested Address object
        const addressStr = c.Address
            ? `${c.Address.Street || ""}, ${c.Address.City || ""}`
            : "No Address";

        detailsArea.value =
            `Name: ${c.FirstName} ${c.LastName}\n` +
            (c.Email ? `Email: ${c.Email}\n` : "") +
            (c.Phone ? `Phone: ${c.Phone}\n` : "") +
            `Address: ${addressStr}`;

        detailsArea.focus();
    }

    document.addEventListener("click", (e) => {
        if (suggestions && !suggestions.contains(e.target) && e.target !== searchInput) {
            suggestions.classList.add("hidden");
        }
    });
});


// -- ===== INVOICE/ORDER TABLE SEARCH ===== -->

const searchBox = document.getElementById("searchBox");
const searchSuggestions = document.getElementById("searchSuggestions");
const tableBody = document.querySelector("#invoiceTable tbody");

if (searchBox && tableBody) {
    const rows = Array.from(tableBody.querySelectorAll("tr"));

    searchBox.addEventListener("input", () => {
        const term = searchBox.value.trim().toLowerCase();
        searchSuggestions.innerHTML = "";
        searchSuggestions.classList.add("hidden");

        if (term.length < 2) {
            rows.forEach(r => (r.style.display = ""));
            return;
        }

        // Search in data attributes
        const matches = rows.filter(row => {
            const inv = row.dataset.invoiceNumber || "";
            const cust = row.dataset.billedTo || "";
            const date = row.dataset.dueDate || ""; // Using 'dueDate' attr even though it stores CreatedAt in View
            const match = inv.includes(term) || cust.includes(term) || date.includes(term);
            row.style.display = match ? "" : "none";
            return match;
        });

        // Dropdown Suggestions
        if (matches.length > 0) {
            searchSuggestions.innerHTML = matches.slice(0, 5).map(row => `
                <div class="p-3 hover:bg-pink-50 cursor-pointer transition border-b border-gray-100"
                     data-id="${row.dataset.id}">
                    <div class="text-gray-700 font-medium">${row.dataset.invoiceNumber.toUpperCase()}</div>
                    <div class="text-xs text-gray-500">
                        ${row.dataset.billedTo}
                    </div>
                </div>
            `).join("");
            searchSuggestions.classList.remove("hidden");

            // Click handler for suggestions
            searchSuggestions.querySelectorAll("[data-id]").forEach(el => {
                el.addEventListener("click", () => {
                    const targetRow = rows.find(r => r.dataset.id === el.dataset.id);
                    if (targetRow) {
                        targetRow.style.display = ""; // Ensure it's visible
                        tableBody.prepend(targetRow); // Move to top
                        targetRow.scrollIntoView({ behavior: "smooth", block: "center" });
                        setTimeout(() => selectInvoice(el.dataset.id), 300);
                    }
                    searchSuggestions.classList.add("hidden");
                });
            });
        } else {
            searchSuggestions.innerHTML = "<div class='p-2 text-sm text-gray-500'>No results found</div>";
            searchSuggestions.classList.remove("hidden");
        }
    });

    document.addEventListener("click", (e) => {
        if (!e.target.closest("#searchSuggestions") && e.target !== searchBox) {
            searchSuggestions.classList.add("hidden");
        }
    });
}


// -- ===== RECEIPT VIEW (RIGHT PANEL) - UPDATED FOR TbOrder ===== -->

function formatDate(dateStr) {
    if (!dateStr) return "—";
    const d = new Date(dateStr);
    return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

function getProductName(productId) {
    const product = availableProducts.find(p => p.Id == productId);
    return product ? product.Item : "(Unknown Product)";
}

function selectInvoice(id) {
    // Note: 'invoices' here refers to the list of TbOrder objects passed from C#
    const order = invoices.find(i => i.Id === id);

    if (!order) {
        console.warn("Order not found for ID:", id);
        return;
    }

    // Highlight row
    document.querySelectorAll("#invoiceTable tbody tr").forEach(r => r.classList.remove("bg-pink-50"));
    const selectedRow = document.querySelector(`#invoiceTable tbody tr[data-id="${id}"]`);
    if (selectedRow) selectedRow.classList.add("bg-pink-50");

    const container = document.getElementById("invoiceDetailContainer");
    if (!container) return;

    // Construct Name safely
    const customerName = order.ShippingAddress
        ? `${order.ShippingAddress.FirstName} ${order.ShippingAddress.LastName}`
        : "Unknown";

    // TbOrder uses CreatedAt, and usually no DueDate. 
    // We will hide DueDate if it doesn't exist.
    const dateIssued = formatDate(order.CreatedAt);

    container.innerHTML = `
        <div class="bg-white rounded-2xl shadow-sm border border-gray-100 p-5 invoice-detail">
            <h2 class="text-lg font-semibold text-gray-700 mb-3">Order of the courts Details</h2>

            <div class="border border-gray-200 rounded-xl p-4 text-sm">
                <div class="flex justify-between mb-3">
                    <div>
                        <h3 class="text-xl font-semibold text-gray-800">Order</h3>
                        <p class="text-gray-500 text-sm"># <span class="font-medium text-gray-700">${order.OrderNumber}</span></p>
                    </div>
                    <img src="${window.logoPath || '/images/Logo.jpg'}" alt="Logo" class="w-20 opacity-80 object-contain">
                </div>

                <div class="flex justify-between text-xs text-gray-600 mb-3">
                    <div>
                        <p class="font-medium text-gray-700 mb-1">From:</p>
                        <p>Sheessentials</p>
                    </div>
                    <div>
                        <p class="font-medium text-gray-700 mb-1">To:</p>
                        <p>${customerName}</p>
                    </div>
                </div>

                <div class="flex justify-between text-xs mb-3">
                    <p><span class="font-medium text-gray-700">Date:</span> ${dateIssued}</p>
                    <p><span class="font-medium text-gray-700">Status:</span> ${order.PaymentStatus || order.OrderStatus}</p>
                </div>

                <table class="w-full text-xs border-t border-b border-gray-200 mb-3">
                    <thead>
                        <tr class="text-gray-600 text-left">
                            <th class="py-2">Product</th>
                            <th class="py-2">Qty</th>
                            <th class="py-2">Price</th>
                            <th class="py-2 text-right">Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${(order.Items || []).map(item => `
                            <tr>
                                <td class="py-1">${item.ProductName}</td>
                                <td>${item.Quantity}</td>
                                <td>₱${Number(item.Price).toFixed(2)}</td> <td class="text-right">₱${(item.Quantity * item.Price).toFixed(2)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>

                <div class="text-right space-y-1 text-gray-700">
                    <p>Subtotal: <span class="font-semibold">₱${Number(order.Subtotal).toFixed(2)}</span></p>
                    <p>Tax: <span class="font-semibold">₱${Number(order.Tax).toFixed(2)}</span></p>
                    <p>Shipping: <span class="font-semibold">₱${Number(order.ShippingFee || 0).toFixed(2)}</span></p>
                    <hr class="my-1 border-gray-100"/>
                    <p class="text-lg font-semibold">Total: ₱${Number(order.TotalAmount).toFixed(2)}</p>
                </div>
            </div>
        </div>
    `;

    // Smooth scroll mobile/tablet
    if (window.innerWidth < 1024) {
        container.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
}


// -- ===== UPDATE AND DELETE MODALS ===== -->

document.addEventListener("DOMContentLoaded", () => {

    // === UPDATE MODAL ===
    function openUpdateModal(id, number, status) {
        const modal = document.getElementById('updateModal');
        if (!modal) return;
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        document.getElementById('updateInvoiceId').value = id;
        document.getElementById('updateInvoiceNumber').textContent = number;

        // Ensure the select matches available options in HTML
        const statusSelect = document.getElementById('updateStatus');
        if (statusSelect) statusSelect.value = status;
    }

    // === DELETE MODAL ===
    function openDeleteModal(id, number) {
        const modal = document.getElementById('deleteModal');
        if (!modal) return;
        modal.classList.remove('hidden');
        modal.classList.add('flex');

        document.getElementById('deleteInvoiceId').value = id;
        document.getElementById('deleteInvoiceNumber').textContent = number;

        const input = document.getElementById('deleteConfirmInput');
        const button = document.getElementById('confirmDeleteBtn');
        input.value = '';
        button.disabled = true;
        button.classList.add('opacity-70', 'cursor-not-allowed');

        input.oninput = () => {
            const expected = `delete_${number}`;
            if (input.value.trim() === expected) {
                button.disabled = false;
                button.classList.remove('opacity-70', 'cursor-not-allowed');
                button.classList.replace('bg-red-400', 'bg-red-500');
            } else {
                button.disabled = true;
                button.classList.add('opacity-70', 'cursor-not-allowed');
                button.classList.replace('bg-red-500', 'bg-red-400');
            }
        };
    }

    function closeModal(id) {
        const modal = document.getElementById(id);
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
        }
    }

    // === HANDLE UPDATE SUBMIT ===
    const updateForm = document.getElementById("updateForm");
    if (updateForm) {
        updateForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const id = document.getElementById("updateInvoiceId").value;
            const newStatus = document.getElementById("updateStatus").value;

            try {
                const response = await fetch("/Sales_Finance/UpdateStatus", {
                    method: "POST",
                    headers: { "Content-Type": "application/x-www-form-urlencoded" },
                    body: new URLSearchParams({ id, newStatus })
                });

                if (!response.ok) throw new Error("Failed to update status");

                const result = await response.json();
                if (result.success) {
                    updateInvoiceRowStatus(id, newStatus);
                    closeModal("updateModal");
                    // Update data source as well to reflect in receipt view
                    const order = invoices.find(i => i.Id === id);
                    if (order) order.PaymentStatus = newStatus;
                }
            } catch (err) {
                console.error(err);
                alert("Error updating status.");
            }
        });
    }

    function getStatusColorClass(status) {
        switch (status) {
            // C# Equivalent: "Paid" => "bg-[#763F62] text-white"
            case "Paid":
                return "bg-[#763F62] text-white";

            // Note: C# logic does not include "Completed" in this list, 
            // but if you want it to match "Paid", you can keep it separate or merge. 
            // For now, I'll map "Completed" to the Paid color for consistency.
            case "Completed":
                return "bg-[#763F62] text-white";

            // C# Equivalent: "Unpaid" => "bg-[#A36A66] text-white"
            case "Unpaid":
                return "bg-[#A36A66] text-white";

            // C# Equivalent: "Overdue" => "bg-yellow-100 text-yellow-600"
            case "Overdue":
                return "bg-yellow-100 text-yellow-600";

            // C# Equivalent: "Pending" => "bg-blue-100 text-blue-600"
            case "Pending":
            // C# logic does not include "Processing" but it's typically Pending's sibling
            case "Processing":
                return "bg-blue-100 text-blue-600";

            // C# Equivalent: "Failed" => "bg-red-100 text-red-600"
            case "Failed":
            // Note: C# logic does not include "Cancelled" in this list, 
            // but it's typically grouped with "Failed"
            case "Cancelled":
                return "bg-red-100 text-red-600";

            // C# Equivalent: _ => "bg-gray-100 text-gray-600"
            default:
                return "bg-gray-100 text-gray-600";
        }
    }
    // === HANDLE DELETE SUBMIT ===
    const deleteBtn = document.getElementById('confirmDeleteBtn');
    if (deleteBtn) {
        deleteBtn.addEventListener('click', async () => {
            const id = document.getElementById('deleteInvoiceId').value;
            try {
                const response = await fetch(`/Sales_Finance/DeleteInvoice?id=${encodeURIComponent(id)}`, {
                    method: 'POST'
                });

                const result = await response.json();
                if (result.success) {
                    closeModal('deleteModal');
                    const row = document.querySelector(`[data-id="${id}"]`);
                    if (row) row.remove();
                    // Clear detail view if deleted item was selected
                    const detailContainer = document.getElementById("invoiceDetailContainer");
                    if (detailContainer) detailContainer.innerHTML = '<p class="text-gray-500 text-center py-10">Select an order to view details</p>';
                } else {
                    alert(result.message || 'Failed to delete');
                }
            } catch (e) {
                console.error(e);
                alert("Error deleting record.");
            }
        });
    }

    // Expose functions to global scope for onclick attributes in HTML
    window.openUpdateModal = openUpdateModal;
    window.openDeleteModal = openDeleteModal;
    window.closeModal = closeModal;
    window.selectInvoice = selectInvoice; // Make sure selectInvoice is global too
});


// -- ===== NEW INVOICE FORM CALCULATIONS (Modal) ===== -->

document.addEventListener("DOMContentLoaded", function () {
    const invoiceDateInput = document.getElementById("invoiceDate");
    const dueDateInput = document.getElementById("dueDate");

    if (invoiceDateInput && dueDateInput) {
        const today = new Date();
        const todayStr = today.toISOString().split("T")[0];
        invoiceDateInput.value = todayStr;

        // Even though TbOrder doesn't store DueDate, we keep UI logic intact for now
        const due = new Date(today);
        due.setDate(due.getDate() + 7);
        const dueStr = due.toISOString().split("T")[0];
        dueDateInput.value = dueStr;
        dueDateInput.min = dueStr;

        dueDateInput.addEventListener("change", () => {
            const selected = new Date(dueDateInput.value);
            const minDue = new Date(today);
            minDue.setDate(minDue.getDate() + 7);
            if (selected < minDue) {
                alert("Due date cannot be less than 7 days from today.");
                dueDateInput.value = dueStr;
            }
        });
    }

    // QTY & SUBTOTAL CALC
    const qtyInputs = document.querySelectorAll(".qtyInput");
    const subtotalEl = document.getElementById("invoiceSubtotal");
    const totalEl = document.getElementById("invoiceTotal");

    if (qtyInputs.length > 0) {
        qtyInputs.forEach(input => {
            const price = parseFloat(input.dataset.price);
            const row = input.closest("tr");
            const amountCell = row.querySelector(".amountCell");
            const availableStock = parseInt(input.dataset.stock || "9999", 10);

            input.addEventListener("input", () => {
                let qty = parseInt(input.value) || 0;
                if (qty < 0) qty = 0;
                if (qty > availableStock) {
                    alert(`Only ${availableStock} in stock.`);
                    qty = availableStock;
                    input.value = availableStock;
                }

                const amount = qty * price;
                amountCell.textContent = "₱" + amount.toFixed(2);
                updateSubtotal();
            });
        });

        function updateSubtotal() {
            let subtotal = 0;
            document.querySelectorAll(".amountCell").forEach(cell => {
                const val = parseFloat(cell.textContent.replace(/[₱,]/g, "")) || 0;
                subtotal += val;
            });

            if (subtotalEl) subtotalEl.textContent = "₱" + subtotal.toFixed(2);
            if (totalEl) totalEl.textContent = "₱" + subtotal.toFixed(2);
        }
    }
});