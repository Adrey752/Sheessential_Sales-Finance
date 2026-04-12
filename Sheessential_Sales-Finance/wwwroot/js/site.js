// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Smooth navigation DropDown
function toggleDropdown(id, button) {
    const dropdown = document.getElementById(id);
    const chevron = button.querySelector("i.fa-chevron-down");

    if (dropdown.classList.contains("max-h-0")) {
        dropdown.classList.remove("max-h-0");
        dropdown.classList.add("max-h-40");
        chevron.classList.add("rotate-180");
    } else {
        dropdown.classList.add("max-h-0");
        dropdown.classList.remove("max-h-40");
        chevron.classList.remove("rotate-180");
    }
}

// debounce helper
function debounce(fn, wait) {
    let t;
    return (...args) => {
        clearTimeout(t);
        t = setTimeout(() => fn(...args), wait);
    };
}

function escapeHtml(s) {
    return String(s || '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

// Close productModal on backdrop click (only if modal exists)
document.addEventListener("DOMContentLoaded", function () {
    const modal = document.getElementById("productModal");
    if (modal) {
        modal.addEventListener("click", function (e) {
            if (e.target === modal && typeof closeModal === 'function') closeModal();
        });
    }
});

// Invoice Modal controls — guarded
function openInvoiceModal() {
    const el = document.getElementById('newInvoiceModal');
    if (el) el.classList.remove('hidden');
}

function closeInvoiceModal() {
    const el = document.getElementById('newInvoiceModal');
    if (el) el.classList.add('hidden');
}

function showProductPicker() {
    const picker = document.getElementById('productPicker');
    const search = document.getElementById('invoiceProductSearch');
    if (picker) picker.classList.remove('hidden');
    if (search) search.focus();
}

function hideProductPicker() {
    const el = document.getElementById('productPicker');
    if (el) el.classList.add('hidden');
}

// Invoice product search (debounced) — uses invoiceProductSearch id, NOT productSearch
var searchProducts = debounce(async function (q) {
    if (!q || q.length < 1) { renderProductResults([]); return; }
    try {
        var res = await fetch('/api/products?query=' + encodeURIComponent(q));
        var items = await res.json();
        renderProductResults(items);
    } catch (err) {
        console.error(err);
        renderProductResults([]);
    }
}, 250);

document.addEventListener('input', function (e) {
    if (e.target && e.target.id === 'invoiceProductSearch') {
        searchProducts(e.target.value);
    }
});

function renderProductResults(products) {
    var container = document.getElementById('productResults');
    if (!container) return;
    container.innerHTML = '';
    if (!products || products.length === 0) {
        container.innerHTML = '<div class="text-gray-500 text-sm">No results</div>';
        return;
    }
    products.forEach(function (p) {
        var el = document.createElement('div');
        el.className = 'p-3 rounded-md hover:bg-gray-50 cursor-pointer flex justify-between items-center';
        el.innerHTML =
            '<div>' +
            '<div class="font-medium text-gray-800">' + escapeHtml(p.item) + '</div>' +
            '<div class="text-xs text-gray-500">' + escapeHtml(p.sku) + ' • ' + escapeHtml(p.category) + '</div>' +
            '</div>' +
            '<div class="text-sm text-gray-700">₱' + p.unitPrice.toFixed(2) + '</div>';
        el.onclick = function () { addProductRow(p); hideProductPicker(); };
        container.appendChild(el);
    });
}

function addProductRow(product) {
    var container = document.getElementById('invoiceItems');
    if (!container) return;

    var pid = product._id || product.id || product.Id;
    if (container.querySelector('[data-product-id="' + pid + '"]')) return;

    var row = document.createElement('div');
    row.className = 'grid grid-cols-12 gap-3 items-center';
    row.dataset.productId = pid;

    row.innerHTML =
        '<div class="col-span-5">' +
        '<div class="text-sm font-medium text-gray-800">' + escapeHtml(product.item) + '</div>' +
        '<input type="hidden" class="productId" value="' + pid + '" />' +
        '</div>' +
        '<div class="col-span-4">' +
        '<input class="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm desc" placeholder="Description (optional)" />' +
        '</div>' +
        '<div class="col-span-1 text-center">' +
        '<input type="number" class="w-14 px-2 py-2 border border-gray-200 rounded-lg qty" value="1" min="1" />' +
        '</div>' +
        '<div class="col-span-1 text-right">' +
        '<div class="px-3 py-2 inline-block border border-gray-200 rounded-lg text-sm price">₱' + Number(product.unitPrice).toFixed(2) + '</div>' +
        '</div>' +
        '<div class="col-span-1 text-right">' +
        '<div class="px-3 py-2 inline-block font-semibold amount">₱' + Number(product.unitPrice).toFixed(2) + '</div>' +
        '</div>';
    container.appendChild(row);

    var qtyInput = row.querySelector('.qty');
    if (qtyInput) qtyInput.addEventListener('input', function () { updateRowAmount(row); });
    updateTotals();
}

function updateRowAmount(row) {
    var qty = Number(row.querySelector('.qty') ? row.querySelector('.qty').value : 0);
    var priceEl = row.querySelector('.price');
    var priceText = priceEl ? priceEl.textContent.replace(/[₱,]/g, '').trim() : '0';
    var price = Number(priceText || 0);
    var amountEl = row.querySelector('.amount');
    if (amountEl) amountEl.textContent = '₱' + (qty * price).toFixed(2);
    updateTotals();
}

function updateTotals() {
    var rows = document.querySelectorAll('#invoiceItems > .grid');
    var sub = 0;
    rows.forEach(function (r) {
        var amountEl = r.querySelector('.amount');
        var amountText = amountEl ? amountEl.textContent.replace(/[₱,]/g, '').trim() : '0';
        sub += Number(amountText || 0);
    });
    var subtotalEl = document.getElementById('subtotal');
    var grandEl = document.getElementById('grandTotal');
    if (subtotalEl) subtotalEl.textContent = '₱' + sub.toFixed(2);
    if (grandEl) grandEl.textContent = '₱' + sub.toFixed(2);
}

async function submitInvoiceForm() {
    var rows = Array.from(document.querySelectorAll('#invoiceItems > .grid'));
    if (rows.length === 0) { alert('Please add at least one product.'); return false; }

    var invoice = {
        invoiceNumber: document.getElementById('invoiceNumber') ? document.getElementById('invoiceNumber').value : '',
        orderNumber: document.getElementById('orderNumber') ? document.getElementById('orderNumber').value : '',
        invoiceDate: document.getElementById('invoiceDate') ? document.getElementById('invoiceDate').value : '',
        dueDate: document.getElementById('dueDate') ? document.getElementById('dueDate').value : '',
        notes: document.getElementById('notes') ? document.getElementById('notes').value : '',
        items: rows.map(function (r) {
            var priceEl = r.querySelector('.price');
            return {
                productId: r.querySelector('.productId') ? r.querySelector('.productId').value : '',
                quantity: Number(r.querySelector('.qty') ? r.querySelector('.qty').value : 0),
                salePrice: Number(priceEl ? priceEl.textContent.replace(/[₱,]/g, '').trim() : 0)
            };
        })
    };

    for (var idx = 0; idx < invoice.items.length; idx++) {
        if (!invoice.items[idx].productId) {
            alert('One or more items missing product reference.');
            return false;
        }
    }

    try {
        var res = await fetch('/Sales_Finance/CreateInvoice', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(invoice)
        });
        if (res.ok) { closeInvoiceModal(); location.reload(); }
        else { alert('Save failed: ' + await res.text()); }
    } catch (err) { console.error(err); alert('Save failed, check console.'); }
    return false;
}