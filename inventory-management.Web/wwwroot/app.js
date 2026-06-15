// Alpine Inventory Web System Core Logic

const API_BASE = "/api";
let currentUser = null;
let metadataCache = null;
let currentActiveTab = "home";
let activeScannerInputId = null;
let html5QrCode = null;
let searchDebounceTimeout = null;
let createdItemCompatibilityList = [];

// Hierarchical Search Step Navigation State
let currentSearchStep = "parts"; // parts, manufacturers, models, items
let selectedSearchPart = null;
let selectedSearchManufacturer = null;
let selectedSearchModel = null;
let searchFilterTextModels = "";
let searchFilterTextItems = "";
let searchIncludeOutOfStock = false;
let searchItemsPage = 1;
let searchItemsTotalPages = 1;
const SEARCH_PAGE_SIZE = 20;
let allInventoryItems = [];

// Stock views state
let addStockCurrentItem = null;
let removeStockCurrentItem = null;
let addStockDebounceTimeout = null;
let removeStockDebounceTimeout = null;
let addStockPendingOrderIds = [];

function formatLocalDateTime(dateStr) {
    if (!dateStr) return "-";
    const date = new Date(dateStr);
    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const dd = String(date.getDate()).padStart(2, '0');
    const hh = String(date.getHours()).padStart(2, '0');
    const min = String(date.getMinutes()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd} ${hh}:${min}`;
}

// Password Visibility Toggle for Login Page
function togglePasswordVisibility() {
    const passwordInput = document.getElementById("password");
    const icon = document.getElementById("password-toggle-icon");
    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        icon.classList.remove("fa-eye-slash");
        icon.classList.add("fa-eye");
    } else {
        passwordInput.type = "password";
        icon.classList.remove("fa-eye");
        icon.classList.add("fa-eye-slash");
    }
}

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
    const targetTab = document.getElementById(`tab-${tabId}`);
    if (targetTab) targetTab.classList.remove("hidden");

    // Manage top navigation buttons
    const backBtn = document.getElementById("nav-back-btn");
    const refreshBtn = document.getElementById("nav-refresh-btn");
    if (tabId === "home") {
        if (backBtn) backBtn.classList.add("hidden");
        if (refreshBtn) refreshBtn.classList.add("hidden");
    } else {
        if (backBtn) backBtn.classList.remove("hidden");
        if (refreshBtn) refreshBtn.classList.remove("hidden");
    }

    // Tab-specific initializations
    if (tabId === "search") {
        currentSearchStep = "parts";
        selectedSearchPart = null;
        selectedSearchManufacturer = null;
        selectedSearchModel = null;
        searchFilterTextModels = "";
        searchFilterTextItems = "";
        searchItemsPage = 1;
        switchSearchStep("parts");
        loadAllItems();
    }
    if (tabId === "reports") {
        loadReportsData();
    }
    if (tabId === "create-item") {
        resetCreateItemForm();
    }
}

function handleRefreshClick() {
    if (currentActiveTab === "search") {
        loadAllItems();
        showToast("Search items list refreshed", "info");
    } else if (currentActiveTab === "reports") {
        loadReportsData();
        showToast("Reports data refreshed", "info");
    } else if (currentActiveTab === "create-item") {
        loadMetadata();
        showToast("Metadata lists refreshed", "info");
    } else if (currentActiveTab === "add-stock") {
        loadMetadata();
        showToast("Stock items refreshed", "info");
    } else if (currentActiveTab === "remove-stock") {
        loadMetadata();
        showToast("Stock items refreshed", "info");
    }
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
    try {
        const response = await fetch(`${API_BASE}/inventory/items`);
        if (!response.ok) throw new Error();
        allInventoryItems = await response.json();

        // Dynamically render the current active search step
        if (currentSearchStep === "parts") {
            renderSearchParts();
        } else if (currentSearchStep === "manufacturers") {
            renderSearchManufacturers();
        } else if (currentSearchStep === "models") {
            renderSearchModels();
        } else if (currentSearchStep === "items") {
            renderSearchItems();
        }
    } catch (err) {
        showToast("Failed to load inventory items.", "danger");
    }
}

function switchSearchStep(step) {
    currentSearchStep = step;
    
    // Hide all search steps
    document.querySelectorAll(".search-step").forEach(el => el.classList.add("hidden"));
    
    // Show current search step
    const target = document.getElementById(`search-step-${step}`);
    if (target) target.classList.remove("hidden");

    // Manage top navigation buttons based on search step
    const backBtn = document.getElementById("nav-back-btn");
    const refreshBtn = document.getElementById("nav-refresh-btn");
    
    if (step === "parts") {
        if (backBtn) backBtn.classList.add("hidden");
    } else {
        if (backBtn) backBtn.classList.remove("hidden");
    }
    
    if (refreshBtn) refreshBtn.classList.remove("hidden");
}

function handleBackNavigation() {
    if (currentActiveTab === "search") {
        const internallyNavigated = goBackSearchStep();
        if (!internallyNavigated) {
            switchTab("home");
        }
    } else {
        switchTab("home");
    }
}

function goBackSearchStep() {
    if (currentSearchStep === "items") {
        if (selectedSearchModel) {
            currentSearchStep = "models";
            selectedSearchModel = null;
            renderSearchModels();
            switchSearchStep("models");
        } else if (selectedSearchManufacturer) {
            currentSearchStep = "models";
            renderSearchModels();
            switchSearchStep("models");
        } else {
            currentSearchStep = "manufacturers";
            renderSearchManufacturers();
            switchSearchStep("manufacturers");
        }
        return true;
    } else if (currentSearchStep === "models") {
        currentSearchStep = "manufacturers";
        selectedSearchManufacturer = null;
        renderSearchManufacturers();
        switchSearchStep("manufacturers");
        return true;
    } else if (currentSearchStep === "manufacturers") {
        currentSearchStep = "parts";
        selectedSearchPart = null;
        renderSearchParts();
        switchSearchStep("parts");
        return true;
    }
    return false; // at parts page, go back to home
}

function renderSearchParts() {
    const container = document.getElementById("search-parts-list");
    container.innerHTML = "";
    
    // Group allInventoryItems by PartType
    const partGroups = {};
    allInventoryItems.forEach(item => {
        if (!item.partType) return;
        const pt = item.partType;
        if (!partGroups[pt.id]) {
            partGroups[pt.id] = {
                id: pt.id,
                name: pt.name,
                imagePath: pt.imagePath,
                itemCount: 0,
                quantity: 0
            };
        }
        partGroups[pt.id].itemCount++;
        partGroups[pt.id].quantity += (item.stock ? item.stock.quantity : 0);
    });
    
    const partsArray = Object.values(partGroups).sort((a, b) => a.name.localeCompare(b.name));
    
    if (partsArray.length === 0) {
        document.getElementById("search-parts-status").innerText = "No parts found.";
        return;
    }
    document.getElementById("search-parts-status").innerText = `${partsArray.length} parts available.`;
    
    partsArray.forEach(part => {
        const card = document.createElement("div");
        card.className = "compact-card search-card";
        card.style.cursor = "pointer";
        card.onclick = () => selectSearchPartType(part);
        
        const imgSrc = part.imagePath ? `/${part.imagePath}` : "/assets/logo/logo.jpeg";
        
        card.innerHTML = `
            <img src="${imgSrc}" class="search-card-thumb" onerror="this.src='/assets/logo/logo.jpeg'">
            <div class="search-card-details">
                <h3>${part.name}</h3>
                <p class="search-card-meta">${part.itemCount} items | ${part.quantity} units in stock</p>
            </div>
        `;
        container.appendChild(card);
    });
}

function selectSearchPartType(part) {
    selectedSearchPart = part;
    document.getElementById("search-selected-part-name").innerText = part.name;
    document.getElementById("search-selected-part-name-2").innerText = part.name;
    
    currentSearchStep = "manufacturers";
    renderSearchManufacturers();
    switchSearchStep("manufacturers");
}

function renderSearchManufacturers() {
    const container = document.getElementById("search-manufacturers-list");
    container.innerHTML = "";
    
    if (!selectedSearchPart) return;
    
    // Group matching items by Manufacturer
    const mfrGroups = {};
    allInventoryItems.forEach(item => {
        if (item.partTypeId !== selectedSearchPart.id) return;
        if (!item.vehicleModel || !item.vehicleModel.manufacturer) return;
        
        const mfr = item.vehicleModel.manufacturer;
        if (!mfrGroups[mfr.id]) {
            mfrGroups[mfr.id] = {
                id: mfr.id,
                name: mfr.name,
                logoPath: mfr.logoPath,
                itemCount: 0,
                quantity: 0
            };
        }
        mfrGroups[mfr.id].itemCount++;
        mfrGroups[mfr.id].quantity += (item.stock ? item.stock.quantity : 0);
    });
    
    const mfrArray = Object.values(mfrGroups).sort((a, b) => a.name.localeCompare(b.name));
    
    if (mfrArray.length === 0) {
        document.getElementById("search-manufacturers-status").innerText = "No manufacturers found.";
        return;
    }
    document.getElementById("search-manufacturers-status").innerText = `${mfrArray.length} manufacturers available.`;
    
    mfrArray.forEach(mfr => {
        const card = document.createElement("div");
        card.className = "compact-card search-card";
        card.style.cursor = "pointer";
        card.onclick = () => selectSearchManufacturer(mfr);
        
        const imgSrc = mfr.logoPath ? `/${mfr.logoPath}` : "/assets/logo/logo.jpeg";
        
        card.innerHTML = `
            <img src="${imgSrc}" class="search-card-thumb" onerror="this.src='/assets/logo/logo.jpeg'">
            <div class="search-card-details">
                <h3>${mfr.name}</h3>
                <p class="search-card-meta">${mfr.itemCount} items | ${mfr.quantity} units in stock</p>
            </div>
        `;
        container.appendChild(card);
    });
}

function selectSearchManufacturer(mfr) {
    selectedSearchManufacturer = mfr;
    document.getElementById("search-selected-mfr-name").innerText = mfr.name;
    
    currentSearchStep = "models";
    document.getElementById("search-models-filter").value = "";
    searchFilterTextModels = "";
    renderSearchModels();
    switchSearchStep("models");
}

function handleViewAllItems() {
    selectedSearchManufacturer = null;
    selectedSearchModel = null;
    
    currentSearchStep = "items";
    document.getElementById("search-items-filter").value = "";
    searchFilterTextItems = "";
    searchItemsPage = 1;
    document.getElementById("search-final-title").innerText = selectedSearchPart.name;
    renderSearchItems();
    switchSearchStep("items");
}

function renderSearchModels() {
    const container = document.getElementById("search-models-list");
    container.innerHTML = "";
    
    if (!selectedSearchPart || !selectedSearchManufacturer) return;
    
    // 1. Get all models of the selected manufacturer
    const mfrModels = (metadataCache && metadataCache.models) 
        ? metadataCache.models.filter(m => m.vehicleManufacturerId === selectedSearchManufacturer.id)
        : [];
        
    // 2. Get items matching part type and selected manufacturer (directly or via compatibility)
    const matchingItems = allInventoryItems.filter(item => {
        if (item.partTypeId !== selectedSearchPart.id) return false;
        
        const directMatch = item.vehicleModel && item.vehicleModel.vehicleManufacturerId === selectedSearchManufacturer.id;
        
        const compatMatch = item.compatibleModels && item.compatibleModels.some(cm => 
            cm.manufacturer && cm.manufacturer.toLowerCase() === selectedSearchManufacturer.name.toLowerCase()
        );
        
        return directMatch || compatMatch;
    });
    
    // 3. For each model of the manufacturer, find associated items
    const modelRows = mfrModels.map(model => {
        const modelItems = [];
        
        matchingItems.forEach(item => {
            // Direct model match
            if (item.vehicleModelId === model.id) {
                modelItems.push(item);
                return;
            }
            
            // Compatibility model match
            const isCompat = item.compatibleModels && item.compatibleModels.some(cm => 
                cm.manufacturer && cm.manufacturer.toLowerCase() === selectedSearchManufacturer.name.toLowerCase() &&
                cm.model && cm.model.toLowerCase() === model.name.toLowerCase()
            );
            if (isCompat) {
                modelItems.push(item);
            }
        });
        
        // Calculate fields
        const qty = modelItems.reduce((acc, i) => acc + (i.stock ? i.stock.quantity : 0), 0);
        const uniqueBrands = [...new Set(modelItems.map(i => i.partBrand ? i.partBrand.name : null).filter(Boolean))].sort();
        const uniqueRacks = [...new Set(modelItems.map(i => i.rack ? i.rack.locationCode : null).filter(Boolean))].sort();
        const uniqueOrigins = [...new Set(modelItems.map(i => i.countryOfOrigin).filter(Boolean))].sort();
        
        const compatList = [...new Set(modelItems.flatMap(i => i.compatibleModels ? i.compatibleModels.map(cm => {
            let parts = [];
            if (cm.manufacturer) parts.push(cm.manufacturer);
            if (cm.model) parts.push(cm.model);
            if (cm.yearRange) parts.push(cm.yearRange);
            if (cm.brand) parts.push(cm.brand);
            if (cm.countryOfOrigin) parts.push(cm.countryOfOrigin);
            return parts.join(" ");
        }) : []))].sort();
        
        const firstImg = modelItems.map(i => i.imagePath).find(p => p) || "";
        
        return {
            id: model.id,
            name: model.name,
            yearRange: model.yearRange || "",
            itemCount: modelItems.length,
            quantity: qty,
            brands: uniqueBrands.join(", ") || "None",
            racks: uniqueRacks.join(", ") || "Unallocated",
            origins: uniqueOrigins.join(", ") || "N/A",
            compatText: compatList.join(", ") || "None",
            imagePath: firstImg
        };
    });
    
    // Sort model rows by name
    modelRows.sort((a, b) => a.name.localeCompare(b.name));
    
    // Filter rows based on search filter
    const filteredRows = modelRows.filter(row => {
        if (!searchFilterTextModels) return true;
        const q = searchFilterTextModels.toLowerCase();
        return row.name.toLowerCase().includes(q) || row.brands.toLowerCase().includes(q);
    });
    
    if (filteredRows.length === 0) {
        document.getElementById("search-models-status").innerText = "No models found.";
        return;
    }
    document.getElementById("search-models-status").innerText = `${filteredRows.length} models available.`;
    
    filteredRows.forEach(row => {
        const card = document.createElement("div");
        card.className = "compact-card search-model-card";
        
        const imgSrc = row.imagePath ? `/${row.imagePath}` : "/assets/logo/logo.jpeg";
        
        card.innerHTML = `
            <div class="model-image-panel" onclick='selectSearchModel(${JSON.stringify(row).replace(/'/g, "&#39;")})' style="cursor:pointer;">
                <img src="${imgSrc}" class="model-thumb" onerror="this.src='/assets/logo/logo.jpeg'">
            </div>
            <div class="model-details-panel">
                <h3 onclick='selectSearchModel(${JSON.stringify(row).replace(/'/g, "&#39;")})' style="cursor:pointer;">${row.name}</h3>
                <p class="model-year">${row.yearRange || 'All Years'}</p>
                <div class="model-meta-grid">
                    <div><strong>Available:</strong> ${row.quantity} units</div>
                    <div><strong>Brand:</strong> ${row.brands}</div>
                    <div><strong>Rack:</strong> ${row.racks}</div>
                    <div><strong>Origin:</strong> ${row.origins}</div>
                </div>
                <div class="model-compat-scroll">
                    <strong>Compat:</strong> ${row.compatText}
                </div>
                <button onclick="viewModelBarcodes(${row.id}, '${row.name.replace(/'/g, "\\'")}')" class="btn btn-secondary btn-sm btn-block" style="margin-top: 10px;">View Barcodes</button>
            </div>
        `;
        container.appendChild(card);
    });
}

function selectSearchModel(model) {
    selectedSearchModel = model;
    document.getElementById("search-final-title").innerText = `${selectedSearchManufacturer.name} ${model.name} ${selectedSearchPart.name}`;
    
    currentSearchStep = "items";
    document.getElementById("search-items-filter").value = "";
    searchFilterTextItems = "";
    searchItemsPage = 1;
    renderSearchItems();
    switchSearchStep("items");
}

function viewModelBarcodes(modelId, modelName) {
    const mockModel = { id: modelId, name: modelName };
    selectSearchModel(mockModel);
}

function renderSearchItems() {
    const container = document.getElementById("search-items-list");
    container.innerHTML = "";
    
    if (!selectedSearchPart) return;
    
    // Filter matching items
    let filteredItems = allInventoryItems.filter(item => {
        // Part Type match
        if (item.partTypeId !== selectedSearchPart.id) return false;
        
        // Manufacturer match
        if (selectedSearchManufacturer && (!item.vehicleModel || item.vehicleModel.vehicleManufacturerId !== selectedSearchManufacturer.id)) {
            return false;
        }
        
        // Model match
        if (selectedSearchModel && item.vehicleModelId !== selectedSearchModel.id) {
            return false;
        }
        
        return true;
    });
    
    // Out of stock filter
    if (!searchIncludeOutOfStock) {
        filteredItems = filteredItems.filter(item => item.stock && item.stock.quantity > 0);
    }
    
    // Text search filter
    if (searchFilterTextItems) {
        const q = searchFilterTextItems.toLowerCase();
        filteredItems = filteredItems.filter(item => {
            const modelName = item.vehicleModel ? item.vehicleModel.name.toLowerCase() : "";
            const brandName = item.partBrand ? item.partBrand.name.toLowerCase() : "";
            const mfrName = item.vehicleModel && item.vehicleModel.manufacturer ? item.vehicleModel.manufacturer.name.toLowerCase() : "";
            const desc = item.description ? item.description.toLowerCase() : "";
            const code = item.barcode ? item.barcode.toLowerCase() : "";
            
            const compatMatch = item.compatibleModels && item.compatibleModels.some(cm => {
                const cmMfr = cm.manufacturer ? cm.manufacturer.toLowerCase() : "";
                const cmMod = cm.model ? cm.model.toLowerCase() : "";
                const cmBrd = cm.brand ? cm.brand.toLowerCase() : "";
                const cmOri = cm.countryOfOrigin ? cm.countryOfOrigin.toLowerCase() : "";
                const cmYr = cm.yearRange ? cm.yearRange.toLowerCase() : "";
                return cmMfr.includes(q) || cmMod.includes(q) || cmBrd.includes(q) || cmOri.includes(q) || cmYr.includes(q);
            });
            
            return modelName.includes(q) || brandName.includes(q) || mfrName.includes(q) || desc.includes(q) || code.includes(q) || compatMatch;
        });
    }
    
    // Sort items by vehicle model name and brand
    filteredItems.sort((a, b) => {
        const modelA = a.vehicleModel ? a.vehicleModel.name : "";
        const modelB = b.vehicleModel ? b.vehicleModel.name : "";
        const brandA = a.partBrand ? a.partBrand.name : "";
        const brandB = b.partBrand ? b.partBrand.name : "";
        
        const cmp = modelA.localeCompare(modelB);
        if (cmp !== 0) return cmp;
        return brandA.localeCompare(brandB);
    });
    
    // Pagination calculation
    const totalItems = filteredItems.length;
    searchItemsTotalPages = Math.max(1, Math.ceil(totalItems / SEARCH_PAGE_SIZE));
    
    if (searchItemsPage > searchItemsTotalPages) {
        searchItemsPage = searchItemsTotalPages;
    }
    
    // Render status
    if (totalItems === 0) {
        document.getElementById("search-items-status").innerText = "No items found.";
        document.getElementById("search-pagination-info").innerText = "Page 1 of 1";
        return;
    }
    document.getElementById("search-items-status").innerText = `Showing ${Math.min(totalItems, (searchItemsPage - 1) * SEARCH_PAGE_SIZE + 1)}-${Math.min(totalItems, searchItemsPage * SEARCH_PAGE_SIZE)} of ${totalItems} items.`;
    document.getElementById("search-pagination-info").innerText = `Page ${searchItemsPage} of ${searchItemsTotalPages}`;
    
    // Slice items for current page
    const pageItems = filteredItems.slice((searchItemsPage - 1) * SEARCH_PAGE_SIZE, searchItemsPage * SEARCH_PAGE_SIZE);
    
    pageItems.forEach(item => {
        const card = document.createElement("div");
        card.className = "compact-card search-item-detail-card";
        
        const imgSrc = item.imagePath ? `/${item.imagePath}` : "/assets/logo/logo.jpeg";
        const qty = item.stock ? item.stock.quantity : 0;
        const brandName = item.partBrand ? item.partBrand.name : "N/A";
        const mfrName = item.vehicleModel && item.vehicleModel.manufacturer ? item.vehicleModel.manufacturer.name : "N/A";
        const modelName = item.vehicleModel ? item.vehicleModel.name : "N/A";
        const rackLoc = item.rack ? item.rack.locationCode : "Unallocated";
        const regDateStr = item.registeredDate ? new Date(item.registeredDate).toLocaleDateString() : "N/A";
        
        const compatTextList = item.compatibleModels ? item.compatibleModels.map(cm => {
            let parts = [];
            if (cm.manufacturer) parts.push(cm.manufacturer);
            if (cm.model) parts.push(cm.model);
            if (cm.yearRange) parts.push(cm.yearRange);
            if (cm.brand) parts.push(cm.brand);
            if (cm.countryOfOrigin) parts.push(cm.countryOfOrigin);
            return parts.join(" ");
        }) : [];
        const compatText = compatTextList.join(", ") || "None";
        
        card.innerHTML = `
            <div class="item-detail-image-panel">
                <img src="${imgSrc}" class="item-detail-thumb" onerror="this.src='/assets/logo/logo.jpeg'">
            </div>
            <div class="item-detail-info-panel">
                <h3>${modelName}</h3>
                <div class="item-detail-grid">
                    <div><strong>Manufacturer:</strong> ${mfrName}</div>
                    <div><strong>Brand:</strong> ${brandName}</div>
                    <div><strong>Origin:</strong> ${item.countryOfOrigin || 'N/A'}</div>
                    <div><strong>Pcode:</strong> ${item.secretPriceCode || 'None'}</div>
                    <div><strong>Registered:</strong> ${regDateStr}</div>
                    <div><strong>Rack:</strong> ${rackLoc}</div>
                    <div><strong>Stock:</strong> <span class="stock-badge ${qty === 0 ? 'out-stock' : qty <= item.lowStockThreshold ? 'low-stock' : 'in-stock'}">${qty} units</span></div>
                    <div><strong>Barcode:</strong> <span class="barcode-badge">${item.barcode}</span></div>
                </div>
                <div class="item-detail-compat">
                    <strong>Compat:</strong> ${compatText}
                </div>
                <button onclick="printBarcodeFromDetail('${item.barcode}')" class="btn btn-secondary btn-sm btn-block" style="margin-top: 15px;"><i class="fa-solid fa-print"></i> Print Barcode Label</button>
            </div>
        `;
        container.appendChild(card);
    });
}

function searchFirstPage() {
    if (searchItemsPage > 1) {
        searchItemsPage = 1;
        renderSearchItems();
    }
}
function searchPrevPage() {
    if (searchItemsPage > 1) {
        searchItemsPage--;
        renderSearchItems();
    }
}
function searchNextPage() {
    if (searchItemsPage < searchItemsTotalPages) {
        searchItemsPage++;
        renderSearchItems();
    }
}
function searchLastPage() {
    if (searchItemsPage < searchItemsTotalPages) {
        searchItemsPage = searchItemsTotalPages;
        renderSearchItems();
    }
}

function handleFilterModels(val) {
    searchFilterTextModels = val;
    renderSearchModels();
}
function handleFilterItems(val) {
    searchFilterTextItems = val;
    searchItemsPage = 1;
    renderSearchItems();
}
function toggleIncludeOutOfStock(val) {
    searchIncludeOutOfStock = val;
    searchItemsPage = 1;
    renderSearchItems();
}

async function handleQuickScan(barcode) {
    if (!barcode) return;
    try {
        const response = await fetch(`${API_BASE}/inventory/search?q=${encodeURIComponent(barcode)}`);
        if (response.ok) {
            const item = await response.json();
            
            // Navigate straight to Step 4 and filter only this item!
            selectedSearchPart = item.partType ? { id: item.partTypeId, name: item.partType.name } : { id: item.partTypeId, name: "Part" };
            selectedSearchManufacturer = item.vehicleModel && item.vehicleModel.manufacturer ? { id: item.vehicleModel.manufacturer.id, name: item.vehicleModel.manufacturer.name } : null;
            selectedSearchModel = item.vehicleModel ? { id: item.vehicleModelId, name: item.vehicleModel.name } : null;
            
            document.getElementById("search-final-title").innerText = item.barcode;
            currentSearchStep = "items";
            
            // Clear filters and set text search to exact barcode
            document.getElementById("search-items-filter").value = item.barcode;
            searchFilterTextItems = item.barcode;
            searchIncludeOutOfStock = true;
            document.getElementById("search-include-out-of-stock").checked = true;
            searchItemsPage = 1;
            
            renderSearchItems();
            switchSearchStep("items");
            
            showToast("Item found and filtered by barcode!", "success");
        } else {
            showToast("Barcode not found in inventory.", "warning");
        }
    } catch (e) {
        showToast("Error looking up barcode.", "danger");
    }
    // Clear the quick scan input
    document.getElementById("search-quick-scan-input").value = "";
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
            showToast("Print command successfully sent to Zebra printer.", "success");
        } else {
            showToast("Failed to communicate print command.", "danger");
        }
    } catch(e) {
        showToast("Printer communication error.", "danger");
    }
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
async function ensureInventoryItems() {
    if (!allInventoryItems || allInventoryItems.length === 0) {
        try {
            const response = await fetch(`${API_BASE}/inventory/items`);
            if (response.ok) {
                allInventoryItems = await response.json();
            }
        } catch (e) {}
    }
}

// Search matching barcodes as user types (emulates WPF search textbox/listbox)
async function handleStockSearch(type) {
    await ensureInventoryItems();
    const searchVal = document.getElementById(`${type}-search`).value.trim().toLowerCase();
    const listbox = document.getElementById(`${type}-search-listbox`);
    
    if (!searchVal) {
        listbox.innerHTML = "";
        listbox.classList.add("hidden");
        return;
    }
    
    const matches = allInventoryItems.filter(item => 
        item.barcode.toLowerCase().includes(searchVal)
    );
    
    if (matches.length === 0) {
        listbox.innerHTML = '<div class="listbox-item disabled-item">No matching barcodes</div>';
        listbox.classList.remove("hidden");
        return;
    }
    
    listbox.innerHTML = "";
    matches.slice(0, 10).forEach(item => {
        const div = document.createElement("div");
        div.className = "listbox-item";
        div.innerText = item.barcode;
        div.onclick = () => {
            document.getElementById(`${type}-search`).value = item.barcode;
            document.getElementById(`${type}-barcode`).value = item.barcode;
            listbox.classList.add("hidden");
            lookupStockItem(type, item.barcode);
        };
        listbox.appendChild(div);
    });
    listbox.classList.remove("hidden");
}

// Debounce manual typing in barcode fields
function handleBarcodeManualInput(type) {
    const barcodeVal = document.getElementById(`${type}-barcode`).value.trim();
    
    if (type === 'add' && addStockDebounceTimeout) clearTimeout(addStockDebounceTimeout);
    if (type === 'remove' && removeStockDebounceTimeout) clearTimeout(removeStockDebounceTimeout);
    
    const timeout = setTimeout(() => {
        lookupStockItem(type, barcodeVal);
    }, 250); // WPF 250ms delay
    
    if (type === 'add') addStockDebounceTimeout = timeout;
    else removeStockDebounceTimeout = timeout;
}

// Core lookup item details logic
async function lookupStockItem(type, barcode) {
    const detailsPanel = document.getElementById(`${type}-details-panel`);
    if (!barcode) {
        if (type === 'add') addStockCurrentItem = null;
        else removeStockCurrentItem = null;
        detailsPanel.innerHTML = "";
        detailsPanel.classList.add("hidden");
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/inventory/search?q=${encodeURIComponent(barcode)}`);
        if (response.ok) {
            const item = await response.json();
            if (type === 'add') addStockCurrentItem = item;
            else removeStockCurrentItem = item;
            
            const currentQuantity = item.stock ? item.stock.quantity : 0;
            const regDate = item.registeredDate ? new Date(item.registeredDate).toISOString().split('T')[0] : "-";
            const pcode = item.secretPriceCode || "-";
            
            let compatItemsHtml = "";
            if (item.compatibleModels && item.compatibleModels.length > 0) {
                compatItemsHtml = item.compatibleModels.map(cm => {
                    const text = `${cm.manufacturer || ''} ${cm.model || ''} ${cm.yearRange || ''} ${cm.brand || ''} ${cm.countryOfOrigin || ''}`.trim().replace(/\s+/g, ' ');
                    return `<li>• ${text}</li>`;
                }).join('');
            } else {
                compatItemsHtml = "<li>No compatibility links</li>";
            }
            
            if (type === 'add') {
                detailsPanel.innerHTML = `
                    <h4 style="font-weight: bold; margin-bottom: 15px; font-size: 18px; border-bottom: 1px solid var(--border-color); padding-bottom: 8px;">Item Details</h4>
                    <div class="detail-row">Part Type: ${item.partType?.name || 'N/A'}</div>
                    <div class="detail-row">Brand: ${item.partBrand?.name || 'N/A'}</div>
                    <div class="detail-row">Make: ${item.vehicleModel?.manufacturer?.name || 'N/A'}</div>
                    <div class="detail-row">Model: ${item.vehicleModel?.name || 'N/A'}</div>
                    <div class="detail-row">Rack: ${item.rack?.locationCode || '-'}</div>
                    <div class="detail-row">Registered Date: ${regDate}</div>
                    <div class="detail-row" style="font-weight: 600;">Pcode: ${pcode}</div>
                    <div class="detail-row" style="font-weight: bold; font-size: 16px; margin-top: 15px; margin-bottom: 15px;">Current Stock: ${currentQuantity}</div>
                    
                    <div style="border-top: 1px solid #eaeaea; border-bottom: 1px solid #eaeaea; padding: 15px 0; margin: 15px 0; text-align: center;">
                        <img src="${API_BASE}/barcode/image?text=${encodeURIComponent(item.barcode)}" alt="Barcode Image" style="height: 60px; max-width: 100%; display: block; margin: 0 auto 5px;">
                        <div style="font-weight: bold; font-size: 15px; margin-bottom: 12px;">${item.barcode}</div>
                        <button type="button" class="btn btn-secondary btn-sm btn-block" onclick="printBarcodeFromDetail('${item.barcode}')" style="font-weight: bold;"><i class="fa-solid fa-print"></i> Print Barcode Label</button>
                    </div>
                    
                    <div style="font-weight: bold; margin-bottom: 8px;">Compatible Models:</div>
                    <ul class="compat-bullets-list" style="list-style: none; padding-left: 0; font-size: 13px; max-height: 150px; overflow-y: auto;">
                        ${compatItemsHtml}
                    </ul>
                `;
            } else {
                detailsPanel.innerHTML = `
                    <h4 style="font-weight: bold; margin-bottom: 15px; font-size: 18px; border-bottom: 1px solid var(--border-color); padding-bottom: 8px;">Item Details</h4>
                    <div class="detail-row">Part Type: ${item.partType?.name || 'N/A'}</div>
                    <div class="detail-row">Brand: ${item.partBrand?.name || 'N/A'}</div>
                    <div class="detail-row">Make: ${item.vehicleModel?.manufacturer?.name || 'N/A'}</div>
                    <div class="detail-row">Model: ${item.vehicleModel?.name || 'N/A'}</div>
                    <div class="detail-row">Rack: ${item.rack?.locationCode || '-'}</div>
                    <div class="detail-row">Registered Date: ${regDate}</div>
                    <div class="detail-row" style="font-weight: 600;">Pcode: ${pcode}</div>
                    <div class="detail-row" style="font-weight: bold; font-size: 16px; margin-top: 15px; margin-bottom: 15px;">Current Stock: ${currentQuantity}</div>
                    
                    <div style="font-weight: bold; margin-bottom: 8px;">Compatible Models:</div>
                    <ul class="compat-bullets-list" style="list-style: none; padding-left: 0; font-size: 13px; max-height: 150px; overflow-y: auto;">
                        ${compatItemsHtml}
                    </ul>
                `;
            }
            detailsPanel.classList.remove("hidden");
        } else {
            if (type === 'add') addStockCurrentItem = null;
            else removeStockCurrentItem = null;
            detailsPanel.innerHTML = '<span class="danger-text"><i class="fa-solid fa-triangle-exclamation"></i> Barcode not found. Verify or register item first.</span>';
            detailsPanel.classList.remove("hidden");
        }
    } catch(e) {
        if (type === 'add') addStockCurrentItem = null;
        else removeStockCurrentItem = null;
        detailsPanel.innerHTML = "";
        detailsPanel.classList.add("hidden");
    }
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
            body: JSON.stringify({ 
                barcode, 
                quantity, 
                secretPriceCode: secretCode,
                orderIds: addStockPendingOrderIds 
            })
        });
        const data = await response.json();
        if (response.ok && data.success) {
            showToast(data.message, "success");
            resetAddStockForm();
            const refreshResponse = await fetch(`${API_BASE}/inventory/items`);
            if (refreshResponse.ok) allInventoryItems = await refreshResponse.json();
        } else {
            showToast(data.message || "Failed to add stock.", "danger");
        }
    } catch(e) {
        showToast("Error connecting to server.", "danger");
    }
}

function resetAddStockForm() {
    document.getElementById("add-stock-form").reset();
    document.getElementById("add-search").value = "";
    document.getElementById("add-search-listbox").innerHTML = "";
    document.getElementById("add-search-listbox").classList.add("hidden");
    const detailsPanel = document.getElementById("add-details-panel");
    detailsPanel.innerHTML = "";
    detailsPanel.classList.add("hidden");
    addStockCurrentItem = null;
    addStockPendingOrderIds = [];
}

// ISSUE / REMOVE STOCK LOGIC
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
            const refreshResponse = await fetch(`${API_BASE}/inventory/items`);
            if (refreshResponse.ok) allInventoryItems = await refreshResponse.json();
        } else {
            showToast(data.message || "Failed to remove stock.", "danger");
        }
    } catch(e) {
        showToast("Error connecting to server.", "danger");
    }
}

function resetRemoveStockForm() {
    document.getElementById("remove-stock-form").reset();
    document.getElementById("remove-search").value = "";
    document.getElementById("remove-search-listbox").innerHTML = "";
    document.getElementById("remove-search-listbox").classList.add("hidden");
    const detailsPanel = document.getElementById("remove-details-panel");
    detailsPanel.innerHTML = "";
    detailsPanel.classList.add("hidden");
    removeStockCurrentItem = null;
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

// SYSTE// REPORTS TABS SWITCHING
let activeReportSubTab = "ordering-queue"; // Default tab in WPF is Order Queue
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
            <td>${row.partType}</td>
            <td>${row.brand}</td>
            <td>${row.manufacturer}</td>
            <td>${row.model}</td>
            <td><strong>${row.description || 'N/A'}</strong></td>
            <td><small>${row.compatibleModelsText}</small></td>
            <td>${row.rack || '-'}</td>
            <td><strong>${row.quantity}</strong></td>
            <td>${row.lowStockThreshold}</td>
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
            <td>${row.partType}</td>
            <td>${row.brand}</td>
            <td>${row.manufacturer}</td>
            <td>${row.model}</td>
            <td><strong>${row.description || 'N/A'}</strong></td>
            <td><small>${row.compatibleModelsText}</small></td>
            <td>${row.rack || '-'}</td>
            <td class="danger-text"><strong>${row.quantity}</strong></td>
            <td>${row.lowStockThreshold}</td>
        `;
        tbody.appendChild(tr);
    });
}

function renderTransactionsReport(txs) {
    const tbody = document.getElementById("report-transactions-tbody");
    tbody.innerHTML = "";
    if (txs.length === 0) {
        tbody.innerHTML = '<tr><td colspan="9" class="no-data-cell">No transactions recorded.</td></tr>';
        return;
    }

    txs.forEach(tx => {
        const dateStr = formatLocalDateTime(tx.timestamp);
        const tr = document.createElement("tr");
        
        let actionClass = "success-text";
        if (tx.actionType === "OUT") actionClass = "danger-text";
        else if (tx.actionType === "REPLACEMENT") actionClass = "warning-text";

        tr.innerHTML = `
            <td><small>${dateStr}</small></td>
            <td><span class="barcode-badge">${tx.barcode}</span></td>
            <td>${tx.description || 'N/A'}</td>
            <td>${tx.partType}</td>
            <td>${tx.manufacturer}</td>
            <td>${tx.model}</td>
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
        pendBody.innerHTML = '<tr><td colspan="9" class="no-data-cell">No pending orders.</td></tr>';
    } else {
        pending.forEach((row) => {
            const dateStr = formatLocalDateTime(row.createdAt);
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td><input type="checkbox" class="pending-chk" data-ids="${row.orderIds.join(',')}"></td>
                <td>${row.partType}</td>
                <td>${row.brand}</td>
                <td>${row.manufacturer}</td>
                <td>${row.model}</td>
                <td><span class="barcode-badge">${row.barcode}</span></td>
                <td><strong>${row.quantity}</strong></td>
                <td><small>${dateStr}</small></td>
                <td><button onclick="placeSingleOrder([${row.orderIds.join(',')}])" class="btn btn-primary btn-sm" style="background-color: #0078D4; color: white; font-weight: bold; border: none; padding: 2px 10px; font-size: 12px; height: 28px; width: 70px;">Order</button></td>
            `;
            pendBody.appendChild(tr);
        });
    }

    // Render Ordered
    const ordBody = document.getElementById("report-ordered-tbody");
    ordBody.innerHTML = "";
    if (ordered.length === 0) {
        ordBody.innerHTML = '<tr><td colspan="9" class="no-data-cell">No active orders.</td></tr>';
    } else {
        ordered.forEach((row) => {
            const dateStr = formatLocalDateTime(row.orderedAt);
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td><input type="checkbox" class="ordered-chk" data-ids="${row.orderIds.join(',')}"></td>
                <td>${row.partType}</td>
                <td>${row.brand}</td>
                <td>${row.manufacturer}</td>
                <td>${row.model}</td>
                <td><span class="barcode-badge">${row.barcode}</span></td>
                <td><strong>${row.quantity}</strong></td>
                <td><small>${dateStr}</small></td>
                <td><button onclick="navigateToArriveOrder('${row.barcode}', ${row.quantity}, [${row.orderIds.join(',')}])" class="btn btn-success btn-sm" style="background-color: #2E7D32; color: white; font-weight: bold; border: none; padding: 2px 10px; font-size: 12px; height: 28px; width: 70px;">Arrived</button></td>
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
    const itemsToOrder = [];
    if (reportsCache && reportsCache.pendingOrders) {
        reportsCache.pendingOrders.forEach(row => {
            const hasMatch = row.orderIds.some(id => orderIds.includes(id));
            if (hasMatch) {
                itemsToOrder.push(row);
            }
        });
    }

    try {
        const response = await fetch(`${API_BASE}/reports/orders/place`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ orderIds })
        });
        if (response.ok) {
            showToast("Order status successfully updated to 'Ordered'", "success");
            
            if (itemsToOrder.length > 0) {
                generateOrderPDF(itemsToOrder);
            }
            
            loadReportsData();
        } else {
            showToast("Failed to place order.", "danger");
        }
    } catch(e) {
        showToast("Error connecting to server.", "danger");
    }
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

function navigateToArriveOrder(barcode, quantity, orderIds) {
    const firstBarcode = barcode.split(',')[0].trim();
    switchTab("add-stock");
    document.getElementById("add-barcode").value = firstBarcode;
    document.getElementById("add-quantity").value = quantity;
    addStockPendingOrderIds = orderIds || [];
    lookupStockItem("add", firstBarcode);
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

function generateOrderPDF(orderItems) {
    if (!orderItems || orderItems.length === 0) return;
    
    try {
        const { jsPDF } = window.jspdf;
        const doc = new jsPDF();
        
        // Title block
        doc.setFont("helvetica", "bold");
        doc.setFontSize(18);
        doc.setTextColor(30, 58, 138); // Navy
        doc.text("ALPINE AUTO A/C", 14, 20);
        
        doc.setFontSize(22);
        doc.setTextColor(0, 0, 0);
        doc.text("PURCHASE ORDER SLIP", 14, 30);
        
        // Date and Time (Local)
        doc.setFont("helvetica", "normal");
        doc.setFontSize(10);
        doc.setTextColor(100, 116, 139);
        const dateStr = formatLocalDateTime(new Date().toISOString());
        doc.text(`Placed Date/Time: ${dateStr}`, 14, 40);
        
        // Horizontal line separator
        doc.setDrawColor(226, 232, 240);
        doc.setLineWidth(0.5);
        doc.line(14, 46, 196, 46);
        
        // Headers and Rows mapping matching WPF PDF exactly
        const headers = [["Type", "Brand", "Manufacturer", "Model", "Barcode", "Qty", "Date Removed"]];
        const data = orderItems.map(item => [
            item.partType || "N/A",
            item.brand || "N/A",
            item.manufacturer || "N/A",
            item.model || "N/A",
            item.barcode || "N/A",
            `${item.quantity}`,
            formatLocalDateTime(item.createdAt)
        ]);
        
        // Draw Table
        doc.autoTable({
            startY: 50,
            head: headers,
            body: data,
            theme: "grid",
            headStyles: { fillColor: [30, 58, 138], textColor: [255, 255, 255], fontStyle: "bold" },
            styles: { fontSize: 9, cellPadding: 4, valign: "middle" },
            columnStyles: {
                5: { halign: "center" }
            }
        });
        
        showToast("PDF Order slip generated and downloaded successfully!", "success");
        const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
        doc.save(`Purchase_Order_${timestamp}.pdf`);
    } catch (e) {
        console.error("PDF generation error", e);
        showToast("Error generating PDF: " + e.message, "danger");
    }
}
