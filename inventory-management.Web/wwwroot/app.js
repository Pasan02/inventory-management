// Alpine Inventory Web System Core Logic

const API_BASE = "/api";
let currentUser = null;
let metadataCache = null;
let currentActiveTab = "dashboard";
let activeScannerInputId = null;
let html5QrCode = null;
let searchDebounceTimeout = null;
let createdItemCompatibilityList = [];

// App Startup
document.addEventListener("DOMContentLoaded", () => {
    // Check if user is logged in
    const storedUser = localStorage.getItem("inventory_user");
    if (storedUser) {
        currentUser = JSON.parse(storedUser);
        showMainApp();
    } else {
        showLogin();
    }

    // Bind document level events
    setupSystemInfo();
});

// Setup hostname and IP displays
async function setupSystemInfo() {
    // Simple fetch to get some details
    try {
        document.getElementById("info-hostname").innerText = window.location.hostname;
        document.getElementById("info-ip").innerText = window.location.host;
    } catch (e) {}
}

// Toast Notifications Helper
function showToast(message, type = "info") {
    const toast = document.getElementById("toast");
    const icon = document.getElementById("toast-icon");
    const msg = document.getElementById("toast-message");

    // Clear classes
    toast.className = "toast";
    
    // Add type class
    toast.classList.add(type);
    msg.innerText = message;

    // Icon set
    icon.className = "fa-solid";
    if (type === "success") icon.classList.add("fa-circle-check");
    else if (type === "danger") icon.classList.add("fa-circle-xmark");
    else if (type === "warning") icon.classList.add("fa-triangle-exclamation");
    else icon.classList.add("fa-circle-info");

    toast.classList.remove("hidden");

    setTimeout(() => {
        toast.classList.add("hidden");
    }, 4000);
}

// AUTHENTICATION LOGIC
function showLogin() {
    document.getElementById("login-container").classList.remove("hidden");
    document.getElementById("app-container").classList.add("hidden");
    currentUser = null;
    localStorage.removeItem("inventory_user");
}

function showMainApp() {
    document.getElementById("login-container").classList.add("hidden");
    document.getElementById("app-container").classList.remove("hidden");
    document.getElementById("user-display-name").innerText = currentUser.username;
    
    // Switch to active tab and load metadata
    switchTab(currentActiveTab);
    loadMetadata();
}

async function handleLogin(event) {
    event.preventDefault();
    const usernameInput = document.getElementById("username").value;
    const passwordInput = document.getElementById("password").value;

    try {
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: usernameInput, password: passwordInput })
        });

        const data = await response.json();
        if (response.ok && data.success) {
            currentUser = { username: data.username };
            localStorage.setItem("inventory_user", JSON.stringify(currentUser));
            showToast("Welcome back, " + data.username + "!", "success");
            showMainApp();
        } else {
            showToast(data.message || "Invalid credentials", "danger");
        }
    } catch (err) {
        showToast("Server connection error.", "danger");
    }
}

function handleLogout() {
    if (confirm("Are you sure you want to sign out?")) {
        showLogin();
        showToast("Signed out successfully", "info");
    }
}

// TAB NAVIGATION SYSTEM
function switchTab(tabId) {
    currentActiveTab = tabId;
    
    // Update active tab contents
    document.querySelectorAll(".tab-content").forEach(el => el.classList.add("hidden"));
    document.getElementById(`tab-${tabId}`).classList.remove("hidden");

    // Update nav links
    document.querySelectorAll(".nav-link").forEach(el => el.classList.remove("active"));
    
    // Find active nav-link elements
    const link = Array.from(document.querySelectorAll(".nav-link")).find(el => 
        el.getAttribute("onclick").includes(`'${tabId}'`)
    );
    if (link) link.classList.add("active");

    // Page Title
    const titleMap = {
        "dashboard": "Dashboard Metrics",
        "search": "Inventory Search & Item View",
        "add-stock": "Receive Inventory Stock",
        "remove-stock": "Issue / Remove Stock",
        "create-item": "Register New Part",
        "reports": "System Inventory Reports"
    };
    document.getElementById("page-title").innerText = titleMap[tabId] || "Inventory System";

    // Tab-specific initializations
    if (tabId === "dashboard") loadDashboardStats();
    if (tabId === "search") loadAllItems();
    if (tabId === "reports") loadReportsData();
    if (tabId === "create-item") resetCreateItemForm();
}

// DASHBOARD STATS LOGIC
async function loadDashboardStats() {
    try {
        const response = await fetch(`${API_BASE}/reports`);
        if (!response.ok) return;
        const data = await response.json();

        // Unique items count
        const totalItems = data.stockSnapshot.length;
        document.getElementById("stat-total-items").innerText = totalItems;

        // Total stock quantity
        const totalQty = data.stockSnapshot.reduce((acc, row) => acc + row.quantity, 0);
        document.getElementById("stat-total-stock").innerText = totalQty;

        // Low stock alerts
        const lowStockCount = data.lowStock.length;
        document.getElementById("stat-low-stock").innerText = lowStockCount;

        // Set IP address display
        document.getElementById("info-ip").innerText = window.location.host;
    } catch (e) {
        console.error("Dashboard error", e);
    }
}

// METADATA & DROPDOWNS POPULATION
async function loadMetadata() {
    try {
        const response = await fetch(`${API_BASE}/inventory/metadata`);
        if (!response.ok) return;
        metadataCache = await response.json();

        // Populate dropdowns in Create Item form
        populateDropdown("create-part-type", metadataCache.partTypes, "name", "Part Type");
        populateDropdown("create-brand", metadataCache.brands, "name", "Brand");
        populateDropdown("create-manufacturer", metadataCache.manufacturers, "name", "Manufacturer");
        populateDropdown("create-rack", metadataCache.racks, "locationCode", "Rack");

        // Populate datalists for compatibility builder
        populateDatalist("compat-manufacturers-list", metadataCache.manufacturers.map(x => x.name));
        populateDatalist("compat-brands-list", metadataCache.brands.map(x => x.name));
        
        // Reset models dropdown
        document.getElementById("create-model").innerHTML = '<option value="">-- Select Model (Select Manufacturer first) --</option>';
    } catch (e) {
        console.error("Metadata load error", e);
    }
}

function populateDropdown(selectId, items, displayField, label) {
    const select = document.getElementById(selectId);
    select.innerHTML = `<option value="">-- Select ${label} --</option>`;
    
    items.forEach(item => {
        const option = document.createElement("option");
        option.value = item.id;
        option.innerText = item[displayField];
        select.appendChild(option);
    });

    // Add reference data button options
    const addOption = document.createElement("option");
    addOption.value = "-1";
    addOption.style.fontWeight = "bold";
    addOption.style.color = "var(--primary-hover)";
    addOption.innerText = `+ Add New ${label}...`;
    select.appendChild(addOption);
}

function populateDatalist(listId, items) {
    const datalist = document.getElementById(listId);
    datalist.innerHTML = "";
    items.forEach(item => {
        const option = document.createElement("option");
        option.value = item;
        datalist.appendChild(option);
    });
}

// Handle change events in Select elements for reference data popup
let currentRefType = null;
let currentSelectElement = null;

function checkAddSelection(selectElement, type) {
    if (selectElement.value === "-1") {
        selectElement.value = ""; // reset selection
        openRefModal(type, selectElement);
    }
}

function onManufacturerChanged(manufacturerId) {
    const modelSelect = document.getElementById("create-model");
    modelSelect.innerHTML = '<option value="">-- Select Model (Select Manufacturer first) --</option>';

    if (manufacturerId === "-1") {
        document.getElementById("create-manufacturer").value = "";
        openRefModal("Manufacturer", document.getElementById("create-manufacturer"));
        return;
    }

    if (!manufacturerId || !metadataCache) return;

    const models = metadataCache.models.filter(m => m.vehicleManufacturerId === parseInt(manufacturerId));
    populateDropdown("create-model", models, "name", "Model");
}

// REFERENCE DATA MODAL WINDOWS
function openRefModal(type, selectElement) {
    currentRefType = type;
    currentSelectElement = selectElement;
    
    const modal = document.getElementById("ref-modal");
    const title = document.getElementById("ref-modal-title");
    const label = document.getElementById("ref-input-label");
    const nameInput = document.getElementById("ref-input-name");
    
    // Reset inputs
    nameInput.value = "";
    document.getElementById("ref-model-fields").classList.add("hidden");
    document.getElementById("ref-image-fields").classList.add("hidden");

    title.innerText = `Add New ${type}`;
    label.innerText = `${type} Name / Code`;
    
    if (type === "Model") {
        document.getElementById("ref-model-fields").classList.remove("hidden");
        document.getElementById("ref-input-year").value = "";
    } else if (type === "PartType" || type === "Manufacturer") {
        document.getElementById("ref-image-fields").classList.remove("hidden");
        document.getElementById("ref-input-image").value = "";
    }

    modal.classList.remove("hidden");
}

function closeRefModal() {
    document.getElementById("ref-modal").classList.add("hidden");
}

async function handleRefSubmit(event) {
    event.preventDefault();
    const name = document.getElementById("ref-input-name").value;
    const modelYear = document.getElementById("ref-input-year").value;
    const fileInput = document.getElementById("ref-input-image");
    
    let imagePath = null;

    // Handle image upload if available
    if (fileInput.files.length > 0) {
        const formData = new FormData();
        formData.append("file", fileInput.files[0]);
        formData.append("category", currentRefType === "PartType" ? "part-types" : "manufacturers");

        try {
            const upRes = await fetch(`${API_BASE}/upload`, {
                method: "POST",
                body: formData
            });
            if (upRes.ok) {
                const upData = await upRes.json();
                imagePath = upData.relativePath;
            }
        } catch (e) {
            console.error("Reference image upload failed", e);
        }
    }

    let endpoint = "";
    let bodyObj = { name: name };

    if (currentRefType === "PartType") {
        endpoint = "part-type";
        bodyObj.imagePath = imagePath;
    } else if (currentRefType === "Brand") {
        endpoint = "brand";
    } else if (currentRefType === "Manufacturer") {
        endpoint = "manufacturer";
        bodyObj.logoPath = imagePath;
    } else if (currentRefType === "Model") {
        endpoint = "model";
        const mId = parseInt(document.getElementById("create-manufacturer").value);
        bodyObj = {
            name: name,
            manufacturerId: mId,
            yearRange: modelYear
        };
    } else if (currentRefType === "Rack") {
        endpoint = "rack";
        bodyObj = { locationCode: name };
    }

    try {
        const response = await fetch(`${API_BASE}/metadata/${endpoint}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(bodyObj)
        });

        if (response.ok) {
            showToast(`${currentRefType} added successfully!`, "success");
            closeRefModal();
            // Refresh metadata and re-load select lists
            await loadMetadata();
            
            // If manufacturer was modified, trigger re-evaluation of model list
            if (currentRefType === "Manufacturer") {
                document.getElementById("create-manufacturer").value = "";
            }
        } else {
            const err = await response.json();
            showToast(err.message || "Failed to add entry.", "danger");
        }
    } catch (e) {
        showToast("Error adding reference entry.", "danger");
    }
}

// INVENTORY ITEMS SEARCH & TABLES LOGIC
async function loadAllItems() {
    const tbody = document.getElementById("items-table-body");
    tbody.innerHTML = '<tr><td colspan="8" class="loading-cell">Loading items...</td></tr>';

    try {
        const response = await fetch(`${API_BASE}/inventory/items`);
        if (!response.ok) throw new Error();
        const items = await response.json();

        document.getElementById("items-count-badge").innerText = `${items.length} Items`;
        renderItemsTable(items);
    } catch (err) {
        tbody.innerHTML = '<tr><td colspan="8" class="danger-text text-center">Failed to load items list.</td></tr>';
    }
}

function renderItemsTable(items) {
    const tbody = document.getElementById("items-table-body");
    tbody.innerHTML = "";

    if (items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="no-data-cell">No items in inventory.</td></tr>';
        return;
    }

    items.forEach(item => {
        const tr = document.createElement("tr");

        // Image
        const imgCell = document.createElement("td");
        imgCell.className = "item-img-cell";
        const img = document.createElement("img");
        img.className = "table-thumb";
        img.src = item.imagePath ? `/${item.imagePath}` : "/assets/logo/logo.jpeg";
        img.onerror = () => { img.src = "/assets/logo/logo.jpeg"; };
        imgCell.appendChild(img);
        tr.appendChild(imgCell);

        // Barcode
        const barCell = document.createElement("td");
        barCell.innerHTML = `<span class="barcode-badge">${item.barcode}</span>`;
        tr.appendChild(barCell);

        // Description
        const descCell = document.createElement("td");
        descCell.innerText = item.description || "No description";
        tr.appendChild(descCell);

        // Part Type
        const typeCell = document.createElement("td");
        typeCell.innerText = item.partType ? item.partType.name : "N/A";
        tr.appendChild(typeCell);

        // Brand
        const brandCell = document.createElement("td");
        brandCell.innerText = item.partBrand ? item.partBrand.name : "N/A";
        tr.appendChild(brandCell);

        // Manufacturer / Model
        const modelCell = document.createElement("td");
        const manName = item.vehicleModel && item.vehicleModel.manufacturer ? item.vehicleModel.manufacturer.name : "N/A";
        const modName = item.vehicleModel ? item.vehicleModel.name : "N/A";
        modelCell.innerText = `${manName} ${modName}`;
        tr.appendChild(modelCell);

        // Rack / Location
        const rackCell = document.createElement("td");
        rackCell.innerText = item.rack ? item.rack.locationCode : "Unallocated";
        tr.appendChild(rackCell);

        // Stock qty badge
        const stockCell = document.createElement("td");
        const qty = item.stock ? item.stock.quantity : 0;
        const threshold = item.lowStockThreshold || 5;

        let badgeClass = "in-stock";
        if (qty === 0) badgeClass = "out-stock";
        else if (qty <= threshold) badgeClass = "low-stock";

        stockCell.innerHTML = `<span class="stock-badge ${badgeClass}">${qty} units</span>`;
        tr.appendChild(stockCell);

        tr.onclick = () => showSingleItemDetail(item);
        tr.style.cursor = "pointer";

        tbody.appendChild(tr);
    });
}

function showSingleItemDetail(item) {
    const container = document.getElementById("search-single-result");
    container.classList.remove("hidden");
    container.innerHTML = `
        <div class="result-image-panel">
            <img src="${item.imagePath ? '/' + item.imagePath : '/assets/logo/logo.jpeg'}" alt="Item Image" class="result-img" onerror="this.src='/assets/logo/logo.jpeg'">
            <button onclick="printBarcodeFromDetail('${item.barcode}')" class="btn btn-secondary btn-block btn-sm"><i class="fa-solid fa-print"></i> Print Label</button>
        </div>
        <div class="result-info-panel">
            <div class="result-header">
                <h2>${item.partType?.name} - ${item.partBrand?.name}</h2>
                <p>${item.description || 'No description provided.'}</p>
            </div>
            <div class="result-meta-grid">
                <div class="meta-item">
                    <span class="meta-label">Barcode</span>
                    <span class="meta-value barcode-badge" style="width:fit-content">${item.barcode}</span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Current Stock</span>
                    <span class="meta-value">${item.stock?.quantity || 0} units (Threshold: ${item.lowStockThreshold})</span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Rack Location</span>
                    <span class="meta-value">${item.rack?.locationCode || 'Unallocated'}</span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Country of Origin</span>
                    <span class="meta-value">${item.countryOfOrigin || 'N/A'}</span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Vehicle Fitment</span>
                    <span class="meta-value">${item.vehicleModel?.manufacturer?.name} ${item.vehicleModel?.name}</span>
                </div>
                <div class="meta-item">
                    <span class="meta-label">Buying Cipher (Secret Price)</span>
                    <span class="meta-value">${item.secretPriceCode || 'None'}</span>
                </div>
            </div>
        </div>
    `;
    // Scroll container into view
    container.scrollIntoView({ behavior: "smooth" });
}

async function printBarcodeFromDetail(barcode) {
    const copies = prompt("Enter number of copies to print:", "1");
    if (!copies) return;
    const parsed = parseInt(copies);
    if (isNaN(parsed) || parsed <= 0) {
        showToast("Invalid count of copies", "warning");
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/print`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ barcode: barcode, copies: parsed })
        });
        if (response.ok) {
            showToast("Print command successfully sent to host Zebra printer.", "success");
        } else {
            showToast("Failed to communicate print command.", "danger");
        }
    } catch(e) {
        showToast("Printer communication error.", "danger");
    }
}

// Debounced text search
function debounceSearch() {
    clearTimeout(searchDebounceTimeout);
    searchDebounceTimeout = setTimeout(executeSearch, 300);
}

async function executeSearch() {
    const q = document.getElementById("search-input").value.trim();
    if (!q) {
        document.getElementById("search-single-result").classList.add("hidden");
        loadAllItems();
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/inventory/search?q=${encodeURIComponent(q)}`);
        if (response.ok) {
            const item = await response.json();
            showSingleItemDetail(item);
            // Re-render items table highlighting the single matching record
            renderItemsTable([item]);
        } else {
            // If search has no single exact match, query items list filtering locally
            const itemsRes = await fetch(`${API_BASE}/inventory/items`);
            if (itemsRes.ok) {
                const all = await itemsRes.json();
                const filtered = all.filter(i => 
                    i.barcode.toLowerCase().includes(q.toLowerCase()) ||
                    i.description?.toLowerCase().includes(q.toLowerCase()) ||
                    i.partType?.name.toLowerCase().includes(q.toLowerCase()) ||
                    i.partBrand?.name.toLowerCase().includes(q.toLowerCase()) ||
                    i.vehicleModel?.name.toLowerCase().includes(q.toLowerCase()) ||
                    i.vehicleModel?.manufacturer?.name.toLowerCase().includes(q.toLowerCase())
                );
                renderItemsTable(filtered);
            }
        }
    } catch(e) {}
}

// BARCODE SCANNING INTEGRATION
function startBarcodeScanner(targetInputId) {
    activeScannerInputId = targetInputId;
    document.getElementById("scanner-modal").classList.remove("hidden");

    if (html5QrCode) {
        try { html5QrCode.clear(); } catch(e) {}
    }

    html5QrCode = new Html5Qrcode("qr-reader");
    const config = { fps: 15, qrbox: { width: 300, height: 160 } };

    html5QrCode.start(
        { facingMode: "environment" },
        config,
        (decodedText) => {
            document.getElementById(activeScannerInputId).value = decodedText;
            
            // Trigger change event to load details automatically
            const event = new Event('change');
            document.getElementById(activeScannerInputId).dispatchEvent(event);
            
            closeBarcodeScanner();
            showToast("Scanned: " + decodedText, "success");
        },
        () => {} // silent failure during search frames
    ).catch(err => {
        showToast("Camera permission or initialization error: " + err, "danger");
        closeBarcodeScanner();
    });
}

function closeBarcodeScanner() {
    document.getElementById("scanner-modal").classList.add("hidden");
    if (html5QrCode) {
        html5QrCode.stop().then(() => {
            html5QrCode.clear();
            html5QrCode = null;
        }).catch(() => {
            html5QrCode = null;
        });
    }
}

// RECEIVE / ADD STOCK LOGIC
async function lookupAddStockItem(barcode) {
    if (!barcode) return;
    try {
        const response = await fetch(`${API_BASE}/inventory/search?q=${encodeURIComponent(barcode)}`);
        const previewDiv = document.getElementById("add-item-preview");
        if (response.ok) {
            const item = await response.json();
            previewDiv.innerHTML = `
                <img src="${item.imagePath ? '/' + item.imagePath : '/assets/logo/logo.jpeg'}" class="preview-img" onerror="this.src='/assets/logo/logo.jpeg'">
                <div class="preview-details">
                    <h4>${item.partType?.name} (${item.partBrand?.name})</h4>
                    <p>Model: ${item.vehicleModel?.manufacturer?.name} ${item.vehicleModel?.name} | Stock: ${item.stock?.quantity || 0} units</p>
                </div>
            `;
            previewDiv.classList.remove("hidden");
        } else {
            previewDiv.innerHTML = '<span class="danger-text"><i class="fa-solid fa-triangle-exclamation"></i> Barcode not found. Verify or register item first.</span>';
            previewDiv.classList.remove("hidden");
        }
    } catch(e) {}
}

async function handleAddStock(event) {
    event.preventDefault();
    const barcode = document.getElementById("add-barcode").value;
    const quantity = parseInt(document.getElementById("add-quantity").value);
    const secretCode = document.getElementById("add-price-code").value;

    try {
        const response = await fetch(`${API_BASE}/inventory/stock/add`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ barcode, quantity, secretPriceCode: secretCode })
        });
        const data = await response.json();
        if (response.ok && data.success) {
            showToast(data.message, "success");
            resetAddStockForm();
        } else {
            showToast(data.message || "Failed to add stock.", "danger");
        }
    } catch(e) {
        showToast("Error connecting to server.", "danger");
    }
}

function resetAddStockForm() {
    document.getElementById("add-stock-form").reset();
    document.getElementById("add-item-preview").classList.add("hidden");
}

// ISSUE / REMOVE STOCK LOGIC
async function lookupRemoveStockItem(barcode) {
    if (!barcode) return;
    try {
        const response = await fetch(`${API_BASE}/inventory/search?q=${encodeURIComponent(barcode)}`);
        const previewDiv = document.getElementById("remove-item-preview");
        if (response.ok) {
            const item = await response.json();
            previewDiv.innerHTML = `
                <img src="${item.imagePath ? '/' + item.imagePath : '/assets/logo/logo.jpeg'}" class="preview-img" onerror="this.src='/assets/logo/logo.jpeg'">
                <div class="preview-details">
                    <h4>${item.partType?.name} (${item.partBrand?.name})</h4>
                    <p>Model: ${item.vehicleModel?.manufacturer?.name} ${item.vehicleModel?.name} | Stock: ${item.stock?.quantity || 0} units</p>
                </div>
            `;
            previewDiv.classList.remove("hidden");
        } else {
            previewDiv.innerHTML = '<span class="danger-text"><i class="fa-solid fa-triangle-exclamation"></i> Barcode not found.</span>';
            previewDiv.classList.remove("hidden");
        }
    } catch(e) {}
}

async function handleRemoveStock(event) {
    event.preventDefault();
    const barcode = document.getElementById("remove-barcode").value;
    const quantity = parseInt(document.getElementById("remove-quantity").value);
    const isReplacement = document.getElementById("remove-replacement").checked;

    try {
        const response = await fetch(`${API_BASE}/inventory/stock/remove`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ barcode, quantity, isReplacement })
        });
        const data = await response.json();
        if (response.ok && data.success) {
            showToast(data.message, "success");
            resetRemoveStockForm();
        } else {
            showToast(data.message || "Failed to remove stock.", "danger");
        }
    } catch(e) {
        showToast("Error connecting to server.", "danger");
    }
}

function resetRemoveStockForm() {
    document.getElementById("remove-stock-form").reset();
    document.getElementById("remove-item-preview").classList.add("hidden");
}

// CREATE NEW PART LAYOUT LOGIC
function triggerMobileCameraCapture() {
    document.getElementById("create-camera-file").click();
}

async function handleImageFileChange(event) {
    const file = event.target.files[0];
    if (!file) return;

    // Preview locally
    const imgPreview = document.getElementById("create-image-preview");
    const placeholder = document.getElementById("image-upload-placeholder");
    imgPreview.src = URL.createObjectURL(file);
    imgPreview.classList.remove("hidden");
    placeholder.classList.add("hidden");

    // Upload to server asset directory
    const formData = new FormData();
    formData.append("file", file);
    formData.append("category", "items");

    try {
        showToast("Uploading part image...", "info");
        const response = await fetch(`${API_BASE}/upload`, {
            method: "POST",
            body: formData
        });
        if (response.ok) {
            const data = await response.json();
            document.getElementById("create-image-path").value = data.relativePath;
            showToast("Image uploaded successfully.", "success");
        } else {
            showToast("Failed to upload image file.", "danger");
        }
    } catch (e) {
        showToast("Connection error while uploading.", "danger");
    }
}

// Vehicle Compatibility Builder UI logic
function addCompatibilityRow() {
    const manufacturer = document.getElementById("comp-manufacturer").value.trim();
    const model = document.getElementById("comp-model").value.trim();
    const year = document.getElementById("comp-year").value.trim();
    const brand = document.getElementById("comp-brand").value.trim();
    const origin = document.getElementById("comp-origin").value.trim();

    if (!manufacturer && !model && !year && !brand && !origin) {
        showToast("Enter at least one compatibility parameter", "warning");
        return;
    }

    const row = { manufacturer, model, yearRange: year, brand, countryOfOrigin: origin };
    createdItemCompatibilityList.push(row);
    renderCompatibilityList();

    // Reset inputs
    document.getElementById("comp-manufacturer").value = "";
    document.getElementById("comp-model").value = "";
    document.getElementById("comp-year").value = "";
    document.getElementById("comp-brand").value = "";
    document.getElementById("comp-origin").value = "";
}

function removeCompatibilityRow(index) {
    createdItemCompatibilityList.splice(index, 1);
    renderCompatibilityList();
}

function renderCompatibilityList() {
    const tbody = document.getElementById("compatibility-tbody");
    tbody.innerHTML = "";

    if (createdItemCompatibilityList.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="no-data-cell">No compatibility links added yet.</td></tr>';
        return;
    }

    createdItemCompatibilityList.forEach((row, i) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${row.manufacturer || '-'}</td>
            <td>${row.model || '-'}</td>
            <td>${row.yearRange || '-'}</td>
            <td>${row.brand || '-'}</td>
            <td>${row.countryOfOrigin || '-'}</td>
            <td><button type="button" class="btn btn-secondary btn-sm" onclick="removeCompatibilityRow(${i})"><i class="fa-solid fa-trash"></i></button></td>
        `;
        tbody.appendChild(tr);
    });
}

// Submitting Create Item
let savedBarcode = null;
async function handleCreateItem(event) {
    event.preventDefault();
    
    const partTypeId = parseInt(document.getElementById("create-part-type").value);
    const brandId = parseInt(document.getElementById("create-brand").value);
    const modelId = parseInt(document.getElementById("create-model").value);
    const countryOfOrigin = document.getElementById("create-origin").value.trim();
    const description = document.getElementById("create-description").value.trim();
    const lowStockThreshold = parseInt(document.getElementById("create-threshold").value);
    const rackId = parseInt(document.getElementById("create-rack").value) || 0;
    const customBarcode = document.getElementById("create-custom-barcode").value.trim();
    const imagePath = document.getElementById("create-image-path").value;
    const secretPriceCode = document.getElementById("create-price-code").value.trim();

    const bodyObj = {
        partTypeId,
        brandId,
        modelId,
        countryOfOrigin,
        description,
        lowStockThreshold,
        rackId,
        customBarcode,
        imagePath,
        secretPriceCode,
        compatibleModels: createdItemCompatibilityList
    };

    try {
        const response = await fetch(`${API_BASE}/inventory/items/create`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(bodyObj)
        });

        const data = await response.json();
        if (response.ok && data.success) {
            savedBarcode = data.barcode;
            showToast("New item created successfully!", "success");
            
            // Show print label helper panel
            document.getElementById("saved-barcode-display").innerText = savedBarcode;
            document.getElementById("save-success-panel").classList.remove("hidden");
        } else {
            showToast(data.message || "Failed to create item", "danger");
        }
    } catch (e) {
        showToast("Error sending create request.", "danger");
    }
}

async function printSavedBarcode() {
    if (!savedBarcode) return;
    const copies = parseInt(document.getElementById("saved-print-copies").value) || 1;
    
    try {
        const response = await fetch(`${API_BASE}/print`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ barcode: savedBarcode, copies })
        });
        if (response.ok) {
            showToast("Printed successfully", "success");
        } else {
            showToast("Print command failed", "danger");
        }
    } catch(e) {
        showToast("Printer error.", "danger");
    }
}

function resetCreateItemForm() {
    document.getElementById("create-item-form").reset();
    createdItemCompatibilityList = [];
    renderCompatibilityList();
    
    // Hide image elements
    document.getElementById("create-image-preview").classList.add("hidden");
    document.getElementById("image-upload-placeholder").classList.remove("hidden");
    document.getElementById("create-image-path").value = "";

    // Hide success message alert box
    document.getElementById("save-success-panel").classList.add("hidden");
    savedBarcode = null;
}

// SYSTEM REPORTS LOGIC
let activeReportSubTab = "snapshot";
let reportsCache = null;

function switchReportSubTab(subTabId) {
    activeReportSubTab = subTabId;
    document.querySelectorAll(".report-section-body").forEach(el => el.classList.add("hidden"));
    document.getElementById(`report-subtab-${subTabId}`).classList.remove("hidden");

    document.querySelectorAll(".report-nav-btn").forEach(el => el.classList.remove("active"));
    const btn = Array.from(document.querySelectorAll(".report-nav-btn")).find(el => 
        el.getAttribute("onclick").includes(`'${subTabId}'`)
    );
    if (btn) btn.classList.add("active");
}

async function loadReportsData() {
    try {
        const response = await fetch(`${API_BASE}/reports`);
        if (!response.ok) throw new Error();
        reportsCache = await response.json();

        renderSnapshotReport(reportsCache.stockSnapshot);
        renderLowStockReport(reportsCache.lowStock);
        renderTransactionsReport(reportsCache.transactions);
        renderOrderingReport(reportsCache.pendingOrders, reportsCache.orderedItems);
    } catch(e) {
        showToast("Error loading reporting statistics.", "danger");
    }
}

function renderSnapshotReport(snapshot) {
    const tbody = document.getElementById("report-snapshot-tbody");
    tbody.innerHTML = "";
    if (snapshot.length === 0) {
        tbody.innerHTML = '<tr><td colspan="9" class="no-data-cell">No items to report.</td></tr>';
        return;
    }

    snapshot.forEach(row => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><strong>${row.description || 'N/A'}</strong></td>
            <td>${row.partType}</td>
            <td>${row.brand}</td>
            <td>${row.manufacturer} ${row.model}</td>
            <td>${row.countryOfOrigin}</td>
            <td>${row.rack || '-'}</td>
            <td><strong>${row.quantity}</strong></td>
            <td><small class="barcode-badge">${row.barcode}</small></td>
            <td><small>${row.compatibleModelsText}</small></td>
        `;
        tbody.appendChild(tr);
    });
}

function renderLowStockReport(lowStock) {
    const tbody = document.getElementById("report-low-stock-tbody");
    tbody.innerHTML = "";
    if (lowStock.length === 0) {
        tbody.innerHTML = '<tr><td colspan="9" class="success-text text-center padding-20">No low stock items! Keep it up.</td></tr>';
        return;
    }

    lowStock.forEach(row => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><strong>${row.description || 'N/A'}</strong></td>
            <td>${row.partType}</td>
            <td>${row.brand}</td>
            <td>${row.manufacturer} ${row.model}</td>
            <td>${row.countryOfOrigin}</td>
            <td>${row.rack || '-'}</td>
            <td class="danger-text"><strong>${row.quantity}</strong></td>
            <td>${row.lowStockThreshold}</td>
            <td><small class="barcode-badge">${row.barcode}</small></td>
        `;
        tbody.appendChild(tr);
    });
}

function renderTransactionsReport(txs) {
    const tbody = document.getElementById("report-transactions-tbody");
    tbody.innerHTML = "";
    if (txs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="no-data-cell">No transactions recorded.</td></tr>';
        return;
    }

    txs.forEach(tx => {
        const date = new Date(tx.timestamp).toLocaleString();
        const tr = document.createElement("tr");
        
        let actionClass = "success-text";
        if (tx.actionType === "OUT") actionClass = "danger-text";
        else if (tx.actionType === "REPLACEMENT") actionClass = "warning-text";

        tr.innerHTML = `
            <td><small>${date}</small></td>
            <td><span class="barcode-badge">${tx.barcode}</span></td>
            <td>${tx.description || 'N/A'}</td>
            <td>${tx.partType}</td>
            <td>${tx.manufacturer} ${tx.model}</td>
            <td><strong class="${actionClass}">${tx.actionType}</strong></td>
            <td><strong>${tx.quantityChange}</strong></td>
            <td><small class="text-muted">${tx.machineName}</small></td>
        `;
        tbody.appendChild(tr);
    });
}

function renderOrderingReport(pending, ordered) {
    // Render Pending
    const pendBody = document.getElementById("report-pending-tbody");
    pendBody.innerHTML = "";
    if (pending.length === 0) {
        pendBody.innerHTML = '<tr><td colspan="6" class="no-data-cell">No pending orders.</td></tr>';
    } else {
        pending.forEach((row, i) => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td><input type="checkbox" class="pending-chk" data-ids="${row.orderIds.join(',')}"></td>
                <td><strong>${row.description}</strong> (${row.partType})</td>
                <td>${row.manufacturer} ${row.model}</td>
                <td>${row.rack || '-'}</td>
                <td><strong>${row.quantity} units</strong></td>
                <td><button onclick="placeSingleOrder([${row.orderIds.join(',')}])" class="btn btn-secondary btn-sm"><i class="fa-solid fa-cart-shopping"></i> Order</button></td>
            `;
            pendBody.appendChild(tr);
        });
    }

    // Render Ordered
    const ordBody = document.getElementById("report-ordered-tbody");
    ordBody.innerHTML = "";
    if (ordered.length === 0) {
        ordBody.innerHTML = '<tr><td colspan="6" class="no-data-cell">No active orders.</td></tr>';
    } else {
        ordered.forEach((row, i) => {
            const date = new Date(row.orderedAt).toLocaleDateString();
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td><input type="checkbox" class="ordered-chk" data-ids="${row.orderIds.join(',')}"></td>
                <td><strong>${row.description}</strong> (${row.partType})</td>
                <td>${row.manufacturer} ${row.model}</td>
                <td><strong>${row.quantity} units</strong></td>
                <td>${date}</td>
                <td><button onclick="arriveSingleOrder([${row.orderIds.join(',')}])" class="btn btn-secondary btn-sm"><i class="fa-solid fa-box-open"></i> Arrive</button></td>
            `;
            ordBody.appendChild(tr);
        });
    }
}

function toggleSelectAll(type, checked) {
    const list = document.querySelectorAll(`.${type}-chk`);
    list.forEach(chk => chk.checked = checked);
}

// Place / Arrive order API updates
async function placeSingleOrder(orderIds) {
    try {
        const response = await fetch(`${API_BASE}/reports/orders/place`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ orderIds })
        });
        if (response.ok) {
            showToast("Order status successfully updated to 'Ordered'", "success");
            loadReportsData();
        }
    } catch(e) {}
}

async function placeSelectedOrders() {
    const checked = Array.from(document.querySelectorAll(".pending-chk:checked"));
    if (checked.length === 0) {
        showToast("No items selected to order.", "warning");
        return;
    }

    const orderIds = checked.flatMap(chk => chk.getAttribute("data-ids").split(',').map(Number));
    await placeSingleOrder(orderIds);
}

async function arriveSingleOrder(orderIds) {
    try {
        const response = await fetch(`${API_BASE}/reports/orders/arrive`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ orderIds })
        });
        if (response.ok) {
            showToast("Stock items received. Order state updated to 'Arrived'.", "success");
            loadReportsData();
        }
    } catch(e) {}
}

async function arriveSelectedOrders() {
    const checked = Array.from(document.querySelectorAll(".ordered-chk:checked"));
    if (checked.length === 0) {
        showToast("No items selected to mark as arrived.", "warning");
        return;
    }

    const orderIds = checked.flatMap(chk => chk.getAttribute("data-ids").split(',').map(Number));
    await arriveSingleOrder(orderIds);
}
