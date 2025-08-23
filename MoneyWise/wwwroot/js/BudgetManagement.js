// Budget Management JavaScript
$(document).ready(function() {
    // Initialize DataTable
    if ($('#budgetTable').length) {
        $('#budgetTable').DataTable({
            responsive: true,
            order: [[3, 'desc']], // Sort by last updated
            pageLength: 10,
            language: {
                search: "Search budget rules:",
                lengthMenu: "Show _MENU_ budget rules per page",
                info: "Showing _START_ to _END_ of _TOTAL_ budget rules",
                paginate: {
                    first: "First",
                    last: "Last",
                    next: "Next",
                    previous: "Previous"
                }
            }
        });
    }

    // Handle form submission
    $('#budgetForm').on('submit', function(e) {
        e.preventDefault();
        submitBudgetRule();
    });

    // Auto-calculate amounts when total income or percentages change
    $('#totalAmount, #savingsPercentage, #needsPercentage, #wantsPercentage').on('input', function() {
        calculateAmounts();
    });
});

// Calculate amounts based on custom percentages
function calculateAmounts() {
    const totalIncome = parseFloat($('#totalAmount').val()) || 0;
    const savingsPercent = parseFloat($('#savingsPercentage').val()) || 0;
    const needsPercent = parseFloat($('#needsPercentage').val()) || 0;
    const wantsPercent = parseFloat($('#wantsPercentage').val()) || 0;
    
    if (totalIncome > 0) {
        const savings = totalIncome * (savingsPercent / 100);
        const needs = totalIncome * (needsPercent / 100);
        const wants = totalIncome * (wantsPercent / 100);
        
        $('#savingsAmount').val(savings.toFixed(2));
        $('#needsAmount').val(needs.toFixed(2));
        $('#wantsAmount').val(wants.toFixed(2));
        
        // Show budget summary
        showBudgetSummary(totalIncome, savings, needs, wants, savingsPercent, needsPercent, wantsPercent);
    } else {
        // Hide summary if no income entered
        $('#validationSummary').hide();
    }
}

// Show budget summary
function showBudgetSummary(totalIncome, savings, needs, wants, savingsPercent, needsPercent, wantsPercent) {
    const total = savings + needs + wants;
    const difference = totalIncome - total;
    const totalPercent = savingsPercent + needsPercent + wantsPercent;
    const isExact100 = Math.abs(totalPercent - 100) <= 0.1; // Allow small rounding differences
    
    let summaryText = `
        <div class="row mt-2">
            <div class="col-6">
                <strong>Total Income:</strong> ₱${totalIncome.toFixed(2)}
            </div>
            <div class="col-6">
                <strong>Total Budget:</strong> ₱${total.toFixed(2)}
            </div>
        </div>
        <div class="row">
            <div class="col-6">
                <strong>Savings (${savingsPercent}%):</strong> ₱${savings.toFixed(2)}
            </div>
            <div class="col-6">
                <strong>Needs (${needsPercent}%):</strong> ₱${needs.toFixed(2)}
            </div>
        </div>
        <div class="row">
            <div class="col-6">
                <strong>Wants (${wantsPercent}%):</strong> ₱${wants.toFixed(2)}
            </div>
            <div class="col-6">
                <strong>Remaining:</strong> 
                <span class="${difference >= 0 ? 'text-success' : 'text-danger'}">
                    ₱${difference.toFixed(2)}
                </span>
            </div>
        </div>
        <div class="row mt-2">
            <div class="col-12">
                <strong>Total Percentage Used:</strong> 
                <span class="${isExact100 ? 'text-success' : 'text-danger'}">
                    ${totalPercent.toFixed(1)}%
                </span>
                ${!isExact100 ? `<br><small class="text-danger">⚠️ Percentages must equal exactly 100% to save</small>` : '<br><small class="text-success">✅ Percentages are valid!</small>'}
            </div>
        </div>
    `;
    
    $('#budgetSummaryText').html(summaryText);
    $('#validationSummary').show();
    
    // Enable/disable submit button based on percentage validation
    const submitBtn = $('#budgetForm button[type="submit"]');
    if (isExact100 && totalIncome > 0) {
        submitBtn.prop('disabled', false).removeClass('btn-secondary').addClass('btn-primary');
    } else {
        submitBtn.prop('disabled', true).removeClass('btn-primary').addClass('btn-secondary');
    }
}

// Submit budget rule
function submitBudgetRule() {
    const formData = {
        Amount: parseFloat($('#totalAmount').val()) || 0,
        Savings: parseFloat($('#savingsAmount').val()) || 0,
        Needs: parseFloat($('#needsAmount').val()) || 0,
        Wants: parseFloat($('#wantsAmount').val()) || 0
    };

    // Get percentages for validation
    const savingsPercent = parseFloat($('#savingsPercentage').val()) || 0;
    const needsPercent = parseFloat($('#needsPercentage').val()) || 0;
    const wantsPercent = parseFloat($('#wantsPercentage').val()) || 0;
    const totalPercent = savingsPercent + needsPercent + wantsPercent;

    // Validate form data
    if (formData.Amount <= 0) {
        showAlert('Please enter a valid total income amount.', 'danger');
        return;
    }

    if (formData.Savings < 0 || formData.Needs < 0 || formData.Wants < 0) {
        showAlert('Please enter valid amounts for all categories.', 'danger');
        return;
    }

    // Validate that percentages sum to 100%
    if (Math.abs(totalPercent - 100) > 0.1) { // Allow small rounding differences
        showAlert(`Percentages must sum to 100%. Current total: ${totalPercent.toFixed(1)}%`, 'danger');
        return;
    }

    // Show loading state
    const submitBtn = $('#budgetForm button[type="submit"]');
    const originalText = submitBtn.text();
    submitBtn.prop('disabled', true).text('Saving...');

    $.ajax({
        url: '/Budget/CreateBudgetRule',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(response) {
            if (response.success) {
                showAlert('Budget rule created successfully!', 'success');
                $('#budgetModal').modal('hide');
                refreshBudget();
            } else {
                showAlert(response.message || 'Failed to create budget rule.', 'danger');
            }
        },
        error: function(xhr, status, error) {
            console.error('Error creating budget rule:', error);
            showAlert('An error occurred while creating the budget rule.', 'danger');
        },
        complete: function() {
            submitBtn.prop('disabled', false).text(originalText);
        }
    });
}

// Edit budget rule
function editBudget(userId) {
    // Get current budget data and populate modal
    $.ajax({
        url: `/Budget/GetBudgetRuleById/${userId}`,
        type: 'GET',
        success: function(response) {
            if (response.success && response.data) {
                const budget = response.data;
                $('#budgetUserId').val(budget.UserID);
                $('#totalAmount').val(budget.Amount);
                
                // Calculate percentages based on amounts
                const totalIncome = budget.Amount;
                const savingsPercent = totalIncome > 0 ? (budget.Savings / totalIncome * 100) : 0;
                const needsPercent = totalIncome > 0 ? (budget.Needs / totalIncome * 100) : 0;
                const wantsPercent = totalIncome > 0 ? (budget.Wants / totalIncome * 100) : 0;
                
                $('#savingsPercentage').val(savingsPercent.toFixed(1));
                $('#needsPercentage').val(needsPercent.toFixed(1));
                $('#wantsPercentage').val(wantsPercent.toFixed(1));
                
                $('#savingsAmount').val(budget.Savings);
                $('#needsAmount').val(budget.Needs);
                $('#wantsAmount').val(budget.Wants);
                
                $('#budgetModalLabel').text('Edit Budget Rule');
                $('#budgetForm button[type="submit"]').text('Update Budget Rule');
                
                $('#budgetModal').modal('show');
            } else {
                showAlert('Failed to load budget rule for editing.', 'danger');
            }
        },
        error: function(xhr, status, error) {
            console.error('Error loading budget rule:', error);
            showAlert('An error occurred while loading the budget rule.', 'danger');
        }
    });
}

// Delete budget rule
function deleteBudget(userId) {
    if (confirm('Are you sure you want to delete this budget rule? This action cannot be undone.')) {
        $.ajax({
            url: `/Budget/DeleteBudgetRule/${userId}`,
            type: 'DELETE',
            success: function(response) {
                if (response.success) {
                    showAlert('Budget rule deleted successfully!', 'success');
                    refreshBudget();
                } else {
                    showAlert(response.message || 'Failed to delete budget rule.', 'danger');
                }
            },
            error: function(xhr, status, error) {
                console.error('Error deleting budget rule:', error);
                showAlert('An error occurred while deleting the budget rule.', 'danger');
            }
        });
    }
}

// Refresh budget data
function refreshBudget() {
    location.reload();
}

// Export budget to CSV
function exportBudgetToCSV() {
    const table = $('#budgetTable').DataTable();
    const data = table.data().toArray();
    
    if (data.length === 0) {
        showAlert('No budget data to export.', 'warning');
        return;
    }

    let csv = 'Category,Budget Amount,Percentage,Last Updated\n';
    
    // Add budget data
    data.forEach(function(row) {
        const category = $(row[0]).text().trim();
        const amount = $(row[1]).text().trim();
        const percentage = $(row[2]).text().trim();
        const updated = $(row[3]).text().trim();
        
        csv += `"${category}","${amount}","${percentage}","${updated}"\n`;
    });

    // Download CSV file
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `budget_rules_${new Date().toISOString().split('T')[0]}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
    
    showAlert('Budget data exported to CSV successfully!', 'success');
}

// Print budget
function printBudget() {
    const printWindow = window.open('', '_blank');
    const table = $('#budgetTable').DataTable();
    const data = table.data().toArray();
    
    let html = `
        <html>
        <head>
            <title>Budget Rules Report</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f2f2f2; }
                h1 { color: #333; }
                .summary { margin-bottom: 20px; }
            </style>
        </head>
        <body>
            <h1>Budget Rules Report</h1>
            <div class="summary">
                <p><strong>Generated:</strong> ${new Date().toLocaleString()}</p>
            </div>
            <table>
                <thead>
                    <tr>
                        <th>Category</th>
                        <th>Budget Amount</th>
                        <th>Percentage</th>
                        <th>Last Updated</th>
                    </tr>
                </thead>
                <tbody>
    `;
    
    data.forEach(function(row) {
        const category = $(row[0]).text().trim();
        const amount = $(row[1]).text().trim();
        const percentage = $(row[2]).text().trim();
        const updated = $(row[3]).text().trim();
        
        html += `
            <tr>
                <td>${category}</td>
                <td>${amount}</td>
                <td>${percentage}</td>
                <td>${updated}</td>
            </tr>
        `;
    });
    
    html += `
                </tbody>
            </table>
        </body>
        </html>
    `;
    
    printWindow.document.write(html);
    printWindow.document.close();
    printWindow.print();
}

// Show alert message
function showAlert(message, type) {
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'danger' ? 'exclamation-triangle' : 'info-circle'}"></i>
            ${message}
            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `;
    
    // Remove existing alerts
    $('.alert').remove();
    
    // Add new alert at the top of the page
    $('.d-sm-flex.align-items-center.justify-content-between.mb-4').after(alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut();
    }, 5000);
}

// Reset modal when closed
$('#budgetModal').on('hidden.bs.modal', function() {
    $('#budgetForm')[0].reset();
    $('#budgetUserId').val('');
    $('#budgetModalLabel').text('Add Budget Rule');
    $('#budgetForm button[type="submit"]').text('Save Budget Rule');
    
    // Reset percentage fields to default values
    $('#savingsPercentage').val(50);
    $('#needsPercentage').val(30);
    $('#wantsPercentage').val(20);
    
    // Clear amount fields
    $('#savingsAmount').val('');
    $('#needsAmount').val('');
    $('#wantsAmount').val('');
    
    // Hide validation summary
    $('#validationSummary').hide();
    
    // Reset submit button state
    const submitBtn = $('#budgetForm button[type="submit"]');
    submitBtn.prop('disabled', true).removeClass('btn-primary').addClass('btn-secondary');
});
