// Load savings data when modal opens
$('#savingsModal').on('show.bs.modal', function () {
    loadSavingsData();
    resetForm();
});

// Reset form when modal opens
function resetForm() {
    document.getElementById("editSavingsForm").reset();
    document.getElementById("submitSavingsBtn").disabled = true;
    document.getElementById("savingsAmount").classList.remove('is-invalid');
    document.querySelectorAll('input[name="actionType"]').forEach(radio => {
        radio.classList.remove('is-invalid');
    });
}

// Function to load savings data from the server
function loadSavingsData() {
    fetch('/Home/GetSavings', {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' }
    })
    .then(res => res.ok ? res.json() : Promise.reject("Failed to load savings"))
    .then(data => {
        if (data.success) {
            document.getElementById("savingsAmount").value = data.savingsAmount || 0;
            // Removed goal field since it's not in the model anymore
        } else {
            console.error("Error loading savings:", data.message);
            // Set default values if loading fails
            document.getElementById("savingsAmount").value = 0;
        }
    })
    .catch(err => {
        console.error("Error loading savings:", err);
        // Set default values if loading fails
        document.getElementById("savingsAmount").value = 0;
    });
}

// Function to update savings summary display
function updateSavingsSummaryDisplay(data) {
    // Update current balance
    const currentBalanceElement = document.getElementById('currentBalance');
    if (currentBalanceElement) {
        currentBalanceElement.textContent = `₱${data.currentBalance.toFixed(2)}`;
        // Add color coding for balance
        if (data.currentBalance > 0) {
            currentBalanceElement.classList.add('text-success');
            currentBalanceElement.classList.remove('text-danger');
        } else if (data.currentBalance < 0) {
            currentBalanceElement.classList.add('text-danger');
            currentBalanceElement.classList.remove('text-success');
        } else {
            currentBalanceElement.classList.remove('text-success', 'text-danger');
        }
    }
}

// Form validation
document.getElementById("savingsAmount").addEventListener("input", validateForm);
document.querySelectorAll('input[name="actionType"]').forEach(radio => {
    radio.addEventListener("change", validateForm);
});

function validateForm() {
    const amount = document.getElementById("savingsAmount").value;
    const actionType = document.querySelector('input[name="actionType"]:checked');
    const submitBtn = document.getElementById("submitSavingsBtn");
    
    // Validate amount
    const amountValid = amount && parseFloat(amount) > 0;
    document.getElementById("savingsAmount").classList.toggle('is-invalid', !amountValid);
    
    // Validate action type
    const actionValid = actionType !== null;
    document.querySelectorAll('input[name="actionType"]').forEach(radio => {
        radio.classList.toggle('is-invalid', !actionValid);
    });
    
    // Enable/disable submit button
    submitBtn.disabled = !(amountValid && actionValid);
}

// Handle form submission
document.getElementById("editSavingsForm").addEventListener("submit", function(e) {
    e.preventDefault();

    const savingsAmount = document.getElementById("savingsAmount").value;
    const actionType = document.querySelector('input[name="actionType"]:checked').value;

    // Validate inputs
    if (!savingsAmount || parseFloat(savingsAmount) <= 0) {
        alert("Please enter a valid amount");
        return;
    }

    if (!actionType) {
        alert("Please select an action type");
        return;
    }

    fetch('/Home/UpdateSavings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            savingsAmount: parseFloat(savingsAmount), 
            action: actionType 
        })
    })
    .then(res => res.ok ? res.json() : Promise.reject("Failed to update savings"))
    .then(data => {
        if (data.success) {
            $('#savingsModal').modal('hide');
            // Show success message
            alert(data.message || "Transaction completed successfully!");
            // Refresh savings data
            if (window.SavingsCalculator && window.SavingsCalculator.refreshSavingsData) {
                window.SavingsCalculator.refreshSavingsData();
            }
        } else {
            alert(data.message || "Error processing transaction");
        }
    })
    .catch(err => {
        console.error("Error updating savings:", err);
        alert("Error processing transaction. Please try again.");
    });
});