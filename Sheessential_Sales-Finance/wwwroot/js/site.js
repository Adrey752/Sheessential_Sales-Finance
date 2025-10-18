// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


// Product Modal
const modal = document.getElementById("productModal");
const modalName = document.getElementById("modalName");
const modalCategory = document.getElementById("modalCategory");
const modalSelling = document.getElementById("modalSelling");
const modalPurchase = document.getElementById("modalPurchase");

function openModal(name, category, selling, purchase) {
    modal.classList.remove("hidden");
    modalName.textContent = name;
    modalCategory.textContent = category;
    modalSelling.textContent = selling;
    modalPurchase.textContent = purchase;
}

function closeModal() {
    modal.classList.add("hidden");
}

// Close on outside click
modal.addEventListener("click", e => {
    if (e.target === modal) closeModal();
});

// Product Model script end


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


// Invoice Modal
// debounce helper
function debounce(fn, wait) {
    let t;
    return (...args) => {
        clearTimeout(t);
        t = setTimeout(() => fn(...args), wait);
    };
}

// Modal controls
function openInvoiceModal() { document.getElementById('newInvoiceModal').classList.remove('hidden'); }
function closeInvoiceModal() { document.getElementById('newInvoiceModal').classList.add('hidden'); }

// Product picker
function showProductPicker() {
    document.getElementById('productPicker').classList.remove('hidden');
    document.getElementById('productSearch').focus();
}
function hideProductPicker() { document.getElementById('productPicker').classList.add('hidden'); }

// fetch products (debounced)
const productSearchInput = document.getElementById?.('productSearch');
const searchProducts = debounce(async (q) => {
    if (!q || q.length < 1) { renderProductResults([]); return; }
    try {
        const res = await fetch(`/api/products?query=${encodeURIComponent(q)}`);
        const items = await res.json();
        renderProductResults(items);
    } catch (err) {
        console.error(err);
        renderProductResults([]);
    }
}, 250);

document.addEventListener('input', (e) => {
    if (e.target && e.target.id === 'productSearch') {
        searchProducts(e.target.value);
    }
});

function renderProductResults(products) {
    const container = document.getElementById('productResults');
    container.innerHTML = '';
    if (!products || products.length === 0) {
        container.innerHTML = '<div class="text-gray-500 text-sm">No results</div>';
        return;
    }
    products.forEach(p => {
        const el = document.createElement('div');
        el.className = 'p-3 rounded-md hover:bg-gray-50 cursor-pointer flex justify-between items-center';
        el.innerHTML = `
      <div>
        <div class="font-medium text-gray-800">${escapeHtml(p.item)}</div>
        <div class="text-xs text-gray-500">${escapeHtml(p.sku)} • ${escapeHtml(p.category)}</div>
      </div>
      <div class="text-sm text-gray-700">₱${p.unitPrice.toFixed(2)}</div>
    `;
        el.onclick = () => { addProductRow(p); hideProductPicker(); };
        container.appendChild(el);
    });
}

function escapeHtml(s) { return String(s || '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c])); }

// Invoice rows handling
function addProductRow(product) {
    // product: { id, item, sku, unitPrice, srp, ... }
    const container = document.getElementById('invoiceItems');

    // Prevent duplicate product rows (optional)
    if (container.querySelector(`[data-product-id="${product._id || product.id || product.Id || product.Id}"]`)) {
        // optionally increment quantity instead
        return;
    }

    const row = document.createElement('div');
    row.className = 'grid grid-cols-12 gap-3 items-center';
    row.dataset.productId = product._id || product.id || product.Id;

    row.innerHTML = `
    <div class="col-span-5">
      <div class="text-sm font-medium text-gray-800">${escapeHtml(product.item)}</div>
      <input type="hidden" class="productId" value="${product._id || product.id || product.Id}" />
    </div>
    <div class="col-span-4">
      <input class="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm desc" placeholder="Description (optional)" />
    </div>
    <div class="col-span-1 text-center">
      <input type="number" class="w-14 px-2 py-2 border border-gray-200 rounded-lg qty" value="1" min="1" />
    </div>
    <div class="col-span-1 text-right">
      <div class="px-3 py-2 inline-block border border-gray-200 rounded-lg text-sm price">₱${Number(product.unitPrice).toFixed(2)}</div>
    </div>
    <div class="col-span-1 text-right">
      <div class="px-3 py-2 inline-block font-semibold amount">₱${Number(product.unitPrice).toFixed(2)}</div>
    </div>
  `;
    container.appendChild(row);

    // listen to qty changes to update totals
    const qtyInput = row.querySelector('.qty');
    qtyInput.addEventListener('input', () => updateRowAmount(row));
    updateTotals();
}

function updateRowAmount(row) {
    const qty = Number(row.querySelector('.qty').value || 0);
    const priceText = row.querySelector('.price').textContent.replace(/[₱,]/g, '').trim();
    const price = Number(priceText || 0);
    const amount = qty * price;
    row.querySelector('.amount').textContent = `₱${amount.toFixed(2)}`;
    updateTotals();
}

function updateTotals() {
    const rows = document.querySelectorAll('#invoiceItems > .grid');
    let sub = 0;
    rows.forEach(r => {
        const amountText = r.querySelector('.amount').textContent.replace(/[₱,]/g, '').trim();
        sub += Number(amountText || 0);
    });
    document.getElementById('subtotal').textContent = `₱${sub.toFixed(2)}`;
    // currently discount = 0
    document.getElementById('grandTotal').textContent = `₱${sub.toFixed(2)}`;
}

// sanitize form submission: ensure all rows have productId (server-side will also validate)
async function submitInvoiceForm() {
    const rows = Array.from(document.querySelectorAll('#invoiceItems > .grid'));
    if (rows.length === 0) {
        alert('Please add at least one product.');
        return false;
    }

    const invoice = {
        invoiceNumber: document.getElementById('invoiceNumber').value,
        orderNumber: document.getElementById('orderNumber').value,
        invoiceDate: document.getElementById('invoiceDate').value,
        dueDate: document.getElementById('dueDate').value,
        notes: document.getElementById('notes').value,
        items: rows.map(r => ({
            productId: r.querySelector('.productId').value,
            quantity: Number(r.querySelector('.qty').value),
            salePrice: Number(r.querySelector('.price').textContent.replace(/[₱,]/g, '').trim())
        }))
    };

    // final client-side check: productId must be present for each
    for (const it of invoice.items) {
        if (!it.productId) { alert('One or more items missing product reference. Please add via product picker.'); return false; }
    }

    // send to server
    try {
        const res = await fetch('/Sales_Finance/CreateInvoice', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(invoice)
        });
        if (res.ok) {
            // close modal + reload page or show success
            closeInvoiceModal();
            location.reload();
        } else {
            const txt = await res.text();
            alert('Save failed: ' + txt);
        }
    } catch (err) {
        console.error(err);
        alert('Save failed, check console.');
    }
    return false; // prevent normal form submit
}



