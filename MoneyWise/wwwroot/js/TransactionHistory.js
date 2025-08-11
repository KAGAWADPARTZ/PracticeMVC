// Transaction History JavaScript functionality
$(document).ready(function() {
    // Initialize DataTable for transaction table
    initializeTransactionTable();
    
    // Set up event listeners
    setupEventListeners();
});

function initializeTransactionTable() {
    if ($('#transactionTable tbody tr').length > 0) {
        $('#transactionTable').DataTable({
            "order": [[3, "desc"]], // Sort by date descending
            "pageLength": 10,
            "language": {
                "search": "Search transactions:",
                "lengthMenu": "Show _MENU_ transactions per page",
                "info": "Showing _START_ to _END_ of _TOTAL_ transactions"
            },
            "responsive": true,
            "dom": '<"top"lf>rt<"bottom"ip><"clear">'
        });
    }
}

function setupEventListeners() {
    // Refresh button click handler
    $('#refreshHistoryBtn').on('click', function() {
        refreshHistory();
    });
    
    // Export CSV button click handler
    $('#exportCsvBtn').on('click', function() {
        exportToCSV();
    });
    
    // Print button click handler
    $('#printHistoryBtn').on('click', function() {
        printHistory();
    });
}

function refreshHistory() {
    // Show loading state
    const refreshBtn = $('button[onclick="refreshHistory()"]');
    const originalText = refreshBtn.html();
    refreshBtn.html('<i class="fas fa-spinner fa-spin"></i> Refreshing...');
    refreshBtn.prop('disabled', true);
    
    // Reload the page after a short delay to show the loading state
    setTimeout(() => {
        location.reload();
    }, 500);
}

function viewTransactionDetails(transactionId) {
    // Show loading in modal
    $('#transactionModalBody').html(`
        <div class="text-center">
            <i class="fas fa-spinner fa-spin fa-2x text-primary mb-3"></i>
            <p>Loading transaction details...</p>
        </div>
    `);
    
    $('#transactionModal').modal('show');
    
    // In a real application, you would make an AJAX call here
    // For now, we'll show a simple message
    setTimeout(() => {
        $('#transactionModalBody').html(`
            <div class="text-center">
                <i class="fas fa-info-circle fa-3x text-info mb-3"></i>
                <h6>Transaction #${transactionId}</h6>
                <p class="text-muted">Detailed transaction information would be displayed here.</p>
                <div class="mt-3">
                    <small class="text-muted">
                        <i class="fas fa-clock"></i> 
                        Transaction details could include:
                    </small>
                    <ul class="list-unstyled mt-2">
                        <li><i class="fas fa-tag text-primary"></i> Category</li>
                        <li><i class="fas fa-comment text-info"></i> Description</li>
                        <li><i class="fas fa-map-marker-alt text-warning"></i> Location</li>
                        <li><i class="fas fa-receipt text-success"></i> Receipt</li>
                    </ul>
                </div>
            </div>
        `);
    }, 1000);
}

function exportToCSV() {
    // Get table data
    const table = $('#transactionTable').DataTable();
    const data = table.data().toArray();
    
    if (data.length === 0) {
        showAlert('No data to export', 'warning');
        return;
    }
    
    // Create CSV content
    let csvContent = "Transaction ID,Amount,Type,Date\n";
    
    data.forEach(row => {
        const transactionId = row[0];
        const amount = row[1].replace(/[^\d.-]/g, ''); // Extract numeric value
        const type = row[2].includes('Deposit') ? 'Deposit' : 'Withdrawal';
        const date = row[3];
        
        csvContent += `${transactionId},${amount},${type},${date}\n`;
    });
    
    // Create and download file
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `transaction_history_${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    showAlert('CSV exported successfully!', 'success');
}

function printHistory() {
    // Hide elements that shouldn't be printed
    const elementsToHide = $('.btn, .dropdown, .sidebar, .topbar');
    elementsToHide.hide();
    
    // Print the page
    window.print();
    
    // Show elements back
    elementsToHide.show();
}

function showAlert(message, type = 'info') {
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'warning' ? 'exclamation-triangle' : 'info-circle'}"></i>
            ${message}
            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `;
    
    // Insert alert at the top of the card body
    $('.card-body').first().prepend(alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        $('.alert').fadeOut();
    }, 5000);
}

// Utility function to format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-PH', {
        style: 'currency',
        currency: 'PHP'
    }).format(amount);
}

// Utility function to format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}
