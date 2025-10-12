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


