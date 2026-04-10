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

        customerError.classList.remove("hidden");
        customerDetails.classList.add("border-red-400");
        customerSearchInput.classList.add("border-red-400", "ring-2", "ring-red-300");

        customerSearchInput.scrollIntoView({ behavior: "smooth", block: "center" });
        customerSearchInput.focus();

    } else {
        customerError.classList.add("hidden");
        customerDetails.classList.remove("border-red-400");
        customerSearchInput.classList.remove("border-red-400", "ring-2", "ring-red-300");
    }
});


// 2. SEARCHING PRODUCTS IN NEW INVOICE MODAL
document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById("productSearchInput");
    const suggestions = document.getElementById("productSuggestions");
    const rows = Array.from(document.querySelectorAll("#productTableBody tr"));

    if (!searchInput) return;

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
            el.className = "p-2 hover:bg-pink-50 cursor-pointer text-sm border-b border-gray-100";
            el.textContent = row.children[0].textContent.trim();
            el.addEventListener("click", () => scrollToRow(row));
            suggestions.appendChild(el);
        });

        suggestions.classList.remove("hidden");
    }

    function scrollToRow(row) {
        const container = document.querySelector(".max-h-60.overflow-y-auto") || document.querySelector(".border.border-gray-200.rounded-xl.bg-white.max-h-60");

        if (!container) {
            row.scrollIntoView({ behavior: "smooth", block: "center" });
            return;
        }

        const containerRect = container.getBoundingClientRect();
        const rowRect = row.getBoundingClientRect();
        const currentScroll = container.scrollTop;
        const offsetTopInViewport = rowRect.top - containerRect.top;
        const desiredScrollTop = currentScroll + offsetTopInViewport - (container.clientHeight / 2) + (rowRect.height / 2);
        const maxScroll = container.scrollHeight - container.clientHeight;
        const finalScroll = Math.max(0, Math.min(desiredScrollTop, maxScroll));

        container.scrollTo({ top: finalScroll, behavior: "smooth" });

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


// 3. CUSTOMER SEARCHING
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

        const matches = rows.filter(row => {
            const inv = row.dataset.invoiceNumber || "";
            const cust = row.dataset.billedTo || "";
            const date = row.dataset.dueDate || "";
            const match = inv.includes(term) || cust.includes(term) || date.includes(term);
            row.style.display = match ? "" : "none";
            return match;
        });

        if (matches.length > 0) {
            searchSuggestions.innerHTML = matches.slice(0, 5).map(row => `
                <div class="p-3 hover:bg-pink-50 cursor-pointer transition border-b border-gray-100"
                     data-id="${row.dataset.id}">
                    <div class="text-gray-700 font-medium">${row.dataset.invoiceNumber.toUpperCase()}</div>
                    <div class="text-xs text-gray-500">${row.dataset.billedTo}</div>
                </div>
            `).join("");
            searchSuggestions.classList.remove("hidden");

            searchSuggestions.querySelectorAll("[data-id]").forEach(el => {
                el.addEventListener("click", () => {
                    const targetRow = rows.find(r => r.dataset.id === el.dataset.id);
                    if (targetRow) {
                        targetRow.style.display = "";
                        tableBody.prepend(targetRow);
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


// -- ===== RECEIPT VIEW (RIGHT PANEL) ===== -->

function formatDate(dateStr) {
    if (!dateStr) return "—";
    const d = new Date(dateStr);
    return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
}

function getProductName(productId) {
    const product = availableProducts.find(p => p.Id == productId);
    return product ? product.Item : "(Unknown Product)";
}

function getStatusBadgeClass(status) {
    switch (status) {
        case "Paid":
        case "Completed":   return "background:#763F62; color:#fff;";
        case "Unpaid":      return "background:#A36A66; color:#fff;";
        case "Overdue":     return "background:#fef9c3; color:#ca8a04;";
        case "Pending":
        case "Processing":  return "background:#dbeafe; color:#2563eb;";
        case "Failed":
        case "Cancelled":   return "background:#fee2e2; color:#dc2626;";
        default:            return "background:#f3f4f6; color:#4b5563;";
    }
}

function selectInvoice(id) {
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

    // Align detail panel content to top-left once an order is selected
    container.classList.remove("items-center", "justify-center");
    container.classList.add("items-start", "justify-start");

    const customerName = order.ShippingAddress
        ? `${order.ShippingAddress.FirstName} ${order.ShippingAddress.LastName}`
        : "Unknown";

    const dateIssued = formatDate(order.CreatedAt);
    const statusStyle = getStatusBadgeClass(order.PaymentStatus || order.OrderStatus);

    // Build items rows
    const itemRows = (order.Items || []).map(item => `
        <tr class="border-b border-gray-100">
            <td class="py-1.5 pr-2 text-gray-700">${item.ProductName || '—'}</td>
            <td class="py-1.5 text-center text-gray-600">${item.Quantity}</td>
            <td class="py-1.5 text-right text-gray-600">&#8369;${Number(item.Price).toFixed(2)}</td>
            <td class="py-1.5 text-right font-medium text-gray-800">&#8369;${(item.Quantity * item.Price).toFixed(2)}</td>
        </tr>
    `).join('');

    container.innerHTML = `
        <div class="w-full text-left text-[13px] text-gray-700" style="font-family:sans-serif;">

            <!-- Header: Order title + logo -->
            <div style="display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:12px;">
                <div>
                    <p style="font-size:18px; font-weight:700; color:#1f2937; margin:0;">Order</p>
                    <p style="font-size:12px; color:#6b7280; margin:4px 0 0;"># <span style="font-weight:600; color:#374151;">${order.OrderNumber}</span></p>
                </div>
                <img src="${window.logoPath || '/images/Logo.jpg'}" alt="Logo"
                     style="width:70px; opacity:0.85; object-fit:contain;" />
            </div>

            <!-- From / To -->
            <div style="display:grid; grid-template-columns:1fr 1fr; gap:12px; padding:10px 0; border-top:1px solid #f3f4f6; border-bottom:1px solid #f3f4f6; margin-bottom:10px;">
                <div>
                    <p style="font-size:10px; font-weight:600; color:#9ca3af; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 4px;">From</p>
                    <p style="font-size:13px; font-weight:500; color:#1f2937; margin:0;">Sheessentials</p>
                </div>
                <div>
                    <p style="font-size:10px; font-weight:600; color:#9ca3af; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 4px;">To</p>
                    <p style="font-size:13px; font-weight:500; color:#1f2937; margin:0;">${customerName}</p>
                </div>
            </div>

            <!-- Date / Status -->
            <div style="display:grid; grid-template-columns:1fr 1fr; gap:12px; padding-bottom:10px; border-bottom:1px solid #f3f4f6; margin-bottom:12px;">
                <div>
                    <p style="font-size:10px; font-weight:600; color:#9ca3af; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 4px;">Date</p>
                    <p style="font-size:13px; color:#374151; margin:0;">${dateIssued}</p>
                </div>
                <div>
                    <p style="font-size:10px; font-weight:600; color:#9ca3af; text-transform:uppercase; letter-spacing:0.05em; margin:0 0 4px;">Status</p>
                    <span style="display:inline-block; font-size:11px; font-weight:600; padding:2px 10px; border-radius:999px; ${statusStyle}">
                        ${order.PaymentStatus || order.OrderStatus}
                    </span>
                </div>
            </div>

            <!-- Items Table -->
            <table style="width:100%; border-collapse:collapse; font-size:12px; margin-bottom:12px;">
                <thead>
                    <tr style="border-bottom:1px solid #e5e7eb;">
                        <th style="text-align:left; padding:6px 8px 6px 0; color:#9ca3af; font-weight:600; text-transform:uppercase; font-size:10px; letter-spacing:0.05em;">Product</th>
                        <th style="text-align:center; padding:6px 4px; color:#9ca3af; font-weight:600; text-transform:uppercase; font-size:10px; letter-spacing:0.05em;">Qty</th>
                        <th style="text-align:right; padding:6px 4px; color:#9ca3af; font-weight:600; text-transform:uppercase; font-size:10px; letter-spacing:0.05em;">Price</th>
                        <th style="text-align:right; padding:6px 0 6px 4px; color:#9ca3af; font-weight:600; text-transform:uppercase; font-size:10px; letter-spacing:0.05em;">Total</th>
                    </tr>
                </thead>
                <tbody>
                    ${(order.Items || []).map(item => `
                        <tr style="border-bottom:1px solid #f9fafb;">
                            <td style="padding:6px 8px 6px 0; color:#374151;">${item.ProductName || '—'}</td>
                            <td style="padding:6px 4px; text-align:center; color:#6b7280;">${item.Quantity}</td>
                            <td style="padding:6px 4px; text-align:right; color:#6b7280;">&#8369;${Number(item.Price).toFixed(2)}</td>
                            <td style="padding:6px 0 6px 4px; text-align:right; font-weight:600; color:#1f2937;">&#8369;${(item.Quantity * item.Price).toFixed(2)}</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>

            <!-- Totals -->
            <div style="font-size:13px; color:#374151; border-top:1px solid #e5e7eb; padding-top:10px;">
                <div style="display:flex; justify-content:space-between; margin-bottom:4px;">
                    <span style="color:#6b7280;">Subtotal</span>
                    <span style="font-weight:500;">&#8369;${Number(order.Subtotal || 0).toFixed(2)}</span>
                </div>
                <div style="display:flex; justify-content:space-between; margin-bottom:4px;">
                    <span style="color:#6b7280;">Tax</span>
                    <span style="font-weight:500;">&#8369;${Number(order.Tax || 0).toFixed(2)}</span>
                </div>
                <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                    <span style="color:#6b7280;">Shipping</span>
                    <span style="font-weight:500;">&#8369;${Number(order.ShippingFee || 0).toFixed(2)}</span>
                </div>
                <div style="display:flex; justify-content:space-between; border-top:2px solid #e5e7eb; padding-top:8px;">
                    <span style="font-size:15px; font-weight:700; color:#1f2937;">Total</span>
                    <span style="font-size:15px; font-weight:700; color:#A36A66;">&#8369;${Number(order.TotalAmount || 0).toFixed(2)}</span>
                </div>
            </div>

        </div>
    `;

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
                await fetch("/Sales_Finance/UpdateStatus", {
                    method: "POST",
                    headers: { "Content-Type": "application/x-www-form-urlencoded" },
                    body: new URLSearchParams({ id, newStatus })
                });

                closeModal("updateModal");
                window.location.reload();
            } catch (err) {
                console.error(err);
                alert(err.message);
            }
        });
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

    // Expose to global scope
    window.openUpdateModal = openUpdateModal;
    window.openDeleteModal = openDeleteModal;
    window.closeModal = closeModal;
    window.selectInvoice = selectInvoice;
});


// -- ===== NEW INVOICE FORM CALCULATIONS (Modal) ===== -->

document.addEventListener("DOMContentLoaded", function () {
    const invoiceDateInput = document.getElementById("invoiceDate");
    const dueDateInput = document.getElementById("dueDate");

    if (invoiceDateInput && dueDateInput) {
        const today = new Date();
        const todayStr = today.toISOString().split("T")[0];
        invoiceDateInput.value = todayStr;

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
