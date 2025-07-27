// Load savings summary when page loads
document.addEventListener('DOMContentLoaded', function() {
    loadSavingsSummary();
    loadRecentTransactions();
});

// Function to load savings summary
function loadSavingsSummary() {
    fetch('/Home/GetSavingsSummary', {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' }
    })
    .then(res => res.ok ? res.json() : Promise.reject("Failed to load savings summary"))
    .then(data => {
        if (data.success) {
            updateSavingsSummaryDisplay(data);
        } else {
            console.error("Error loading savings summary:", data.message);
        }
    })
    .catch(err => {
        console.error("Error loading savings summary:", err);
    });
}

// Function to update savings summary display
function updateSavingsSummaryDisplay(data) {
    // Update total savings
    const totalSavingsElement = document.getElementById('totalSavings');
    if (totalSavingsElement) {
        totalSavingsElement.textContent = `₱${data.totalSavings.toFixed(2)}`;
    }

    // Update total withdrawals
    const totalWithdrawalsElement = document.getElementById('totalWithdrawals');
    if (totalWithdrawalsElement) {
        totalWithdrawalsElement.textContent = `₱${data.totalWithdrawals.toFixed(2)}`;
    }

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

    // Update transaction count
    const transactionCountElement = document.getElementById('transactionCount');
    if (transactionCountElement) {
        transactionCountElement.textContent = data.transactionCount;
    }
}

// Function to load recent transactions
function loadRecentTransactions() {
    fetch('/Home/GetRecentTransactions?limit=10', {
        method: 'GET',
        headers: { 'Content-Type': 'application/json' }
    })
    .then(res => res.ok ? res.json() : Promise.reject("Failed to load recent transactions"))
    .then(data => {
        if (data.success) {
            updateRecentTransactionsDisplay(data.transactions);
        } else {
            console.error("Error loading recent transactions:", data.message);
        }
    })
    .catch(err => {
        console.error("Error loading recent transactions:", err);
    });
}

// Function to update recent transactions display
function updateRecentTransactionsDisplay(transactions) {
    const transactionsContainer = document.getElementById('recentTransactions');
    if (!transactionsContainer) return;

    if (transactions.length === 0) {
        transactionsContainer.innerHTML = '<p class="text-muted">No transactions found.</p>';
        return;
    }

    let html = '';
    transactions.forEach(transaction => {
        const date = new Date(transaction.date).toLocaleDateString();
        const time = new Date(transaction.date).toLocaleTimeString();
        const typeClass = transaction.type === 'Savings' ? 'text-success' : 'text-danger';
        const icon = transaction.type === 'Savings' ? 'fa-plus' : 'fa-minus';

        html += `
            <div class="transaction-item d-flex justify-content-between align-items-center p-2 border-bottom">
                <div class="d-flex align-items-center">
                    <i class="fas ${icon} ${typeClass} me-2"></i>
                    <div>
                        <div class="fw-bold">${transaction.type}</div>
                        <small class="text-muted">${date} ${time}</small>
                    </div>
                </div>
                <div class="fw-bold ${typeClass}">
                    ${transaction.formattedAmount}
                </div>
            </div>
        `;
    });

    transactionsContainer.innerHTML = html;
}

// Function to refresh all savings data
function refreshSavingsData() {
    loadSavingsSummary();
    loadRecentTransactions();
}

// Export functions for use in other scripts
window.SavingsCalculator = {
    loadSavingsSummary,
    loadRecentTransactions,
    refreshSavingsData
}; 