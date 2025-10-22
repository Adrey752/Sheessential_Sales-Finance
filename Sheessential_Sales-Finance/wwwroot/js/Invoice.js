const { invoices, customers, availableProducts } = window.appData;

// -- ===== New Invoice Modal toglinng and Invoice Searching and selction ===== -->

    /*
    
        FOR OPENING AND CLOSSING THE NEW INVOICE MODAL

    */
    document.querySelectorAll('[data-modal-target]').forEach(btn => {
        btn.addEventListener('click', () => {
            const target = document.getElementById(btn.dataset.modalTarget);
            target.classList.remove('hidden');
            target.classList.add('flex');
        });
    });
    document.querySelectorAll('[data-modal-close]').forEach(btn => {
        btn.addEventListener('click', () => {
            btn.closest('#newInvoiceModal').classList.add('hidden');
            btn.closest('#newInvoiceModal').classList.remove('flex');
        });
    });



    /*
    
        FUNCTIONS IN NEW INVOICE MODAL
    
    */
   
   //      1. VALIDATION FOR ENTERING A CUSTOMER

           document.querySelector("form").addEventListener("submit", function (e) {
      const billedTo = document.getElementById("selectedCustomerId").value.trim();
      if (!billedTo) {
        e.preventDefault();
        document.getElementById("customerError").classList.remove("hidden");
        document.getElementById("customerDetails").classList.add("border-red-400");
      } else {
        document.getElementById("customerError").classList.add("hidden");
        document.getElementById("customerDetails").classList.remove("border-red-400");
      }
    });
    
    //   CUTOMER VALIDATION SCRIPT END


    //    2. SEARCHING PRODUCTS IN NEW INVOICE MODAL      

         document.addEventListener("DOMContentLoaded", () => {
       const searchInput = document.getElementById("productSearchInput");
       const suggestions = document.getElementById("productSuggestions");
       const rows = Array.from(document.querySelectorAll("#productTableBody tr"));

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
           el.textContent = row.children[0].textContent; // product name
           el.addEventListener("click", () => scrollToRow(row));
           suggestions.appendChild(el);
         });

         suggestions.classList.remove("hidden");
       }

     function scrollToRow(row) {
       // container: the element that has overflow-y: auto (the scrolling box for products)
       const container = document.querySelector(".max-h-60.overflow-y-auto") || document.querySelector(".border.border-gray-200.rounded-xl.bg-white.max-h-60");
       if (!container) {
         // fallback: if container not found, use native scrollIntoView
         row.scrollIntoView({ behavior: "smooth", block: "center" });
         return;
       }

       // Get bounding rects
       const containerRect = container.getBoundingClientRect();
       const rowRect = row.getBoundingClientRect();

       // Current scroll position of container
       const currentScroll = container.scrollTop;

       // Distance from top of container to top of row in viewport coordinates
       const offsetTopInViewport = rowRect.top - containerRect.top;

       // Desired scrollTop so the row is vertically centered inside container
       const desiredScrollTop = currentScroll + offsetTopInViewport - (container.clientHeight / 2) + (rowRect.height / 2);

       // Clamp desiredScrollTop between 0 and maxScroll
       const maxScroll = container.scrollHeight - container.clientHeight;
       const finalScroll = Math.max(0, Math.min(desiredScrollTop, maxScroll));

       // Smoothly scroll the container
       container.scrollTo({ top: finalScroll, behavior: "smooth" });

       // optional visual flash (brief)
       row.classList.add("bg-pink-50");
       setTimeout(() => row.classList.remove("bg-pink-50"), 900);

       // hide suggestions and clear search input
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
         if (!e.target.closest("#productSuggestions") && e.target !== searchInput)
           suggestions.classList.add("hidden");
       });
     });


      // SEARCHING PRODUCTS IN NEW INVOICE MODAL SCRIPST END

    // 3. CUSTOMER SEARCHING IN INVOICE MODAL

         document.addEventListener("DOMContentLoaded", () => {
         const searchInput = document.getElementById("customerSearchInput");
         const suggestions = document.getElementById("customerSuggestions");
         const detailsArea = document.getElementById("customerDetails");
         const hiddenCustomerId = document.getElementById("selectedCustomerId");

         searchInput.addEventListener("input", () => {
             const query = searchInput.value.toLowerCase().trim();
             suggestions.innerHTML = "";
             // alert("Script loaded and working. Query: " + query);

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
                 div.className = "px-4 py-2 hover:bg-pink-50 cursor-pointer text-sm";
                 div.textContent = `${c.FirstName} ${c.LastName}${c.Email ? ' (' + c.Email + ')' : ''}`;
                 div.addEventListener("click", () => selectCustomer(c));
                 suggestions.appendChild(div);
             });

             suggestions.classList.remove("hidden");
         });

         function selectCustomer(c) {
             searchInput.value = `${c.FirstName} ${c.LastName}`;
             hiddenCustomerId.value = c.Id;
             suggestions.classList.add("hidden");

             detailsArea.value =
                 `Name: ${c.FirstName} ${c.LastName}\n` +
                 (c.Email ? `Email: ${c.Email}\n` : "") +
                 (c.Phone ? `Phone: ${c.Phone}\n` : "") +
                 (c.Address
                     ? `Address: ${c.Address.Street ?? ""}, ${c.Address.City ?? ""}`
                     : "");
             detailsArea.focus();
         }

         document.addEventListener("click", (e) => {
             if (!suggestions.contains(e.target) && e.target !== searchInput) {
                 suggestions.classList.add("hidden");
             }
         });
     });

     // END OF CUSTOMER SEARCHING


    /* 
    
        SEARCHING FOR INVOICE IN THE TABLE
    
    */
    const searchBox = document.getElementById("searchBox");
    const suggestions = document.getElementById("searchSuggestions");
    const tableBody = document.querySelector("#invoiceTable tbody");
    const rows = Array.from(tableBody.querySelectorAll("tr"));

    // adding listner to search bar
    searchBox.addEventListener("input", () => {
        const term = searchBox.value.trim().toLowerCase();
        suggestions.innerHTML = "";
        suggestions.classList.add("hidden");

        if (term.length < 2) {
            rows.forEach(r => (r.style.display = ""));k
            return;
        }

        //  we search using invoice number, customer and due date 
        // for every row in table if it includes our search we display it else we hide it
        const matches = rows.filter(row => {
            const inv = row.dataset.invoiceNumber;
            const cust = row.dataset.billedTo;
            const due = row.dataset.dueDate;
            const match = inv.includes(term) || cust.includes(term) || due.includes(term);
            row.style.display = match ? "" : "none";
            return match;
        });

        // Move matches to top
        matches.reverse().forEach(r => tableBody.prepend(r));

        // Suggestions dropdown
        if (matches.length > 0) {
            suggestions.innerHTML = matches.slice(0, 5).map(row => `
                <div class="p-3 hover:bg-pink-50 cursor-pointer transition border-b border-gray-100"
                     data-id="${row.dataset.id}">
                    <div class="text-gray-700 font-medium">${row.dataset.invoiceNumber.toUpperCase()}</div>
                    <div class="text-xs text-gray-500">
                        ${row.dataset.billedTo} • ${row.dataset.dueDate || "No Due Date"}
                    </div>
                </div>
            `).join("");
            suggestions.classList.remove("hidden");

            // Clicking suggestion  move row to top + click it
            suggestions.querySelectorAll("[data-id]").forEach(el => {
                el.addEventListener("click", () => {
                    const targetRow = rows.find(r => r.dataset.id === el.dataset.id);
                    if (targetRow) {
                        // Move row to top
                        tableBody.prepend(targetRow);

                        // Make sure it's visible
                        targetRow.scrollIntoView({ behavior: "smooth", block: "center" });

                        // Trigger the row’s click handler
                        setTimeout(() => selectInvoice(el.dataset.id), 300);
                    }

                    // Hide suggestions and clear search box
                    suggestions.classList.add("hidden");
                });
            });
        } else {
            suggestions.innerHTML = "<div class='p-2 text-sm text-gray-500'>No results found</div>";
            suggestions.classList.remove("hidden");
        }
    });

    // Hide suggestions when clicking outside
    document.addEventListener("click", (e) => {
        if (!e.target.closest("#searchSuggestions") && e.target !== searchBox) {
            suggestions.classList.add("hidden");
        }
    });

    // END OF INVOICE SEARCHIGN SCRIPTS





//-- =============== THE PRODUCT RECIEPT ON THE Right ============== -->


    

    function formatDate(dateStr) {
        if (!dateStr) return "—";
        const d = new Date(dateStr);
        return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
    }

    function selectInvoice(id) {

        // check if it exist generate the modal 
        const invoice = invoices.find(i => i.Id === id);
        if (!invoice) {
            console.warn("Invoice not found for ID:", id);
            return;
        }
    // remove highlight to other rows
    document.querySelectorAll("#invoiceTable tbody tr").forEach(r => r.classList.remove("bg-pink-50"));

     // Select and highlight the selected row
    const selectedRow = document.querySelector(`#invoiceTable tbody tr[data-id="${id}"]`);
    if (selectedRow) selectedRow.classList.add("bg-pink-50");

        const container = document.getElementById("invoiceDetailContainer");
        if (!container) return;
        // insert the modal in the container
        container.innerHTML = `
            <div class="bg-white rounded-2xl shadow-sm border border-gray-100 p-5 invoice-detail">
                <h2 class="text-lg font-semibold text-gray-700 mb-3">Invoice Detail</h2>

                <div class="border border-gray-200 rounded-xl p-4 text-sm">
                    <div class="flex justify-between mb-3">
                        <div>
                            <h3 class="text-xl font-semibold text-gray-800">Invoice</h3>
                            <p class="text-gray-500 text-sm">Invoice Number <span class="font-medium text-gray-700">${invoice.InvoiceNumber}</span></p>
                        </div>
                        <img src="/images/logo.png" alt="Logo" class="w-20 opacity-80">
                    </div>

                    <div class="flex justify-between text-xs text-gray-600 mb-3">
                        <div>
                            <p class="font-medium text-gray-700 mb-1">Billed by:</p>
                            <p>Sheessentials</p>
                        </div>
                        <div>
                            <p class="font-medium text-gray-700 mb-1">Billed to:</p>
                            <p>${invoice.BilledTo || "Unknown"}</p>
                        </div>
                    </div>

                    <div class="flex justify-between text-xs mb-3">
                        <p><span class="font-medium text-gray-700">Date Issued:</span> ${formatDate(invoice.IssuedAt)}</p>
                        <p><span class="font-medium text-gray-700">Due Date:</span> ${formatDate(invoice.DueDate)}</p>
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
                            ${(invoice.Items || []).map(item => `
                                <tr>
                                    <td class="py-1">${getProductName(item.ProductId)}</td>
                                    <td>${item.Quantity}</td>
                                    <td>₱${Number(item.SalePrice).toFixed(2)}</td>
                                    <td class="text-right">₱${(item.Quantity * item.SalePrice).toFixed(2)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>

                    <div class="text-right space-y-1 text-gray-700">
                        <p>Subtotal: <span class="font-semibold">₱${Number(invoice.Subtotal).toFixed(2)}</span></p>
                        <p>Tax: <span class="font-semibold">₱${Number(invoice.Tax).toFixed(2)}</span></p>
                        <p>Discount: <span class="font-semibold">₱${Number(invoice.Discount).toFixed(2)}</span></p>
                        <hr />
                        <p class="text-lg font-semibold">Total: ₱${Number(invoice.Total).toFixed(2)}</p>
                    </div>
                </div>
            </div>
        `;

        // Smooth scroll into view of the right panel
        container.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }

        function getProductName(productId) {
        const product = availableProducts.find(p => p.Id == productId);
            console.log(availableProducts);

            return product ? product.Item : "(Unknown Product)";
    }


// --END OF RECIEPT VIEW GENERATION SCRIPT-- >

//--Update and Delete Modal Scripts-- >
    
    document.addEventListener("DOMContentLoaded", () => {

            function openUpdateModal(id, number, status) {
                const modal = document.getElementById('updateModal');
                modal.classList.remove('hidden');
                modal.classList.add('flex');
                document.getElementById('updateInvoiceId').value = id;
                document.getElementById('updateInvoiceNumber').textContent = number;
                document.getElementById('updateStatus').value = status;
            }

      function openDeleteModal(id, number) {
        const modal = document.getElementById('deleteModal');
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        // Invoice Id to display

        document.getElementById('deleteInvoiceId').value = id;

        // delete string confirmation
        document.getElementById('deleteInvoiceNumber').textContent = number;

        // set up
        const input = document.getElementById('deleteConfirmInput');
        const button = document.getElementById('confirmDeleteBtn');
        input.value = '';
        button.disabled = true;
        button.classList.add('opacity-70', 'cursor-not-allowed');


        // enables if the input matches the delete confirmation string it eneables the button
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
        modal.classList.add('hidden');
        modal.classList.remove('flex');
      }
    document.getElementById("updateForm").addEventListener("submit", async (e) => {
            e.preventDefault();

        const id = document.getElementById("updateInvoiceId").value;
        const newStatus = document.getElementById("updateStatus").value;

        try {
            const response = await fetch("/Sales_Finance/UpdateStatus", {
            method: "POST",
        headers: {"Content-Type": "application/x-www-form-urlencoded" },
        body: new URLSearchParams({id, newStatus})
            });

        if (!response.ok) throw new Error("Failed to update invoice status");

        const result = await response.json();
        if (result.success) {
            // ✅ Update UI
            updateInvoiceRowStatus(id, newStatus);

        closeModal("updateModal");

            }
        } catch (err) {
            console.error(err);
        alert("Error updating invoice status.");
        }
    });

        function updateInvoiceRowStatus(id, newStatus) {
        const row = document.querySelector(`#invoiceTable tbody tr[data-id='${id}']`);
        if (!row) return;

        // Find the <span> inside the Status cell
            const statusSpan = row.querySelector("td:first-child span");
            if (!statusSpan) return;

            statusSpan.textContent = newStatus;

            // Update color based on new status
            statusSpan.className = getStatusColorClass(newStatus) + " text-xs px-3 py-1 rounded-full font-medium";
    }

            function getStatusColorClass(status) {
        switch (status) {
            case "Paid": return "bg-green-100 text-green-600";
            case "Unpaid": return "bg-pink-100 text-pink-600";
            case "Overdue": return "bg-yellow-100 text-yellow-600";
            case "Partial": return "bg-blue-100 text-blue-600";
            case "Draft": return "bg-gray-100 text-gray-600";
            case "Cancelled": return "bg-red-100 text-red-600";
            default: return "bg-gray-100 text-gray-600";
        }
    }





        document.getElementById('confirmDeleteBtn').addEventListener('click', async () => {
        const id = document.getElementById('deleteInvoiceId').value;

            const response = await fetch(`/Sales_Finance/DeleteInvoice?id=${encodeURIComponent(id)}`, {
                method: 'POST'
    });


            const result = await response.json();
            if (result.success) {
                closeModal('deleteModal');
            // Optionally remove the row without reloading:
            document.querySelector(`[data-id="${id}"]`)?.remove();
        } else {
                alert(result.message || 'Failed to delete invoice');
        }
    });



            // make them global (so buttons in HTML can call them)
            window.openUpdateModal = openUpdateModal;
            window.openDeleteModal = openDeleteModal;
            window.closeModal = closeModal;

    });
    