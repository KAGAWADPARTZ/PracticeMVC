// Budget Management JavaScript - Clean Version with Comprehensive Logging and Complete Isolation
(function() {
    'use strict';
    
    // Only run this script if we're on a page with the budget modal
    if (typeof $ === 'undefined') {
        console.log('🔍 BudgetManagement.js: jQuery not loaded - skipping initialization');
        return;
    }
    
    $(document).ready(function() {
        console.log('🔍 BudgetManagement.js: Document ready - Initializing budget management');
        
        // Completely disable any global validation for the budget form
        if (typeof $.validator !== 'undefined') {
            console.log('🔍 BudgetManagement.js: jQuery validation detected - completely disabling for budget form');
            $('#budgetForm').validate({
                ignore: "*", // Ignore all validation
                onsubmit: false, // Disable submit validation
                onkeyup: false, // Disable keyup validation
                onfocusout: false, // Disable focusout validation
                onclick: false // Disable click validation
            });
            
            // Also disable any unobtrusive validation
            if ($.validator.unobtrusive) {
                console.log('🔍 BudgetManagement.js: Unobtrusive validation detected - disabling');
                $.validator.unobtrusive.adapters.add('required', ['dependency'], function (options) {
                    // Do nothing - disable all validation
                });
            }
        }
        
        // Override any global alert functions that might interfere
        if (typeof window.showSavingsAlert !== 'undefined') {
            console.log('🔍 BudgetManagement.js: Global showSavingsAlert detected - overriding for budget page');
            const originalShowSavingsAlert = window.showSavingsAlert;
            window.showSavingsAlert = function(message, type) {
                if (message && message.includes('valid amount')) {
                    console.log('🔍 BudgetManagement.js: Intercepted savings alert about valid amount - ignoring');
                    return;
                }
                return originalShowSavingsAlert.apply(this, arguments);
            };
        }
        
        // Initialize DataTable
        if ($('#budgetTable').length) {
            console.log('🔍 BudgetManagement.js: Initializing DataTable for budget table');
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

        // Handle budget form submission
        $('#budgetForm').on('submit', function(e) {
            console.log('🔍 BudgetManagement.js: Budget form submit event triggered');
            e.preventDefault();
            submitBudgetRule();
        });

        // Auto-validate percentages when they change
        $('#savingsPercentage, #needsPercentage, #wantsPercentage').on('input', function() {
            console.log('🔍 BudgetManagement.js: Percentage input changed - validating');
            validatePercentages();
        });

        // Initialize modal reset when opened
        $('#budgetModal').on('show.bs.modal', function() {
            console.log('🔍 BudgetManagement.js: Budget modal opening - resetting');
            resetBudgetModal();
        });

        // Reset modal when closed
        $('#budgetModal').on('hidden.bs.modal', function() {
            console.log('🔍 BudgetManagement.js: Budget modal closing - resetting');
            resetBudgetModal();
        });

        // Log all form elements to check for conflicts
        console.log('🔍 BudgetManagement.js: Checking for form conflicts...');
        console.log('🔍 BudgetManagement.js: Budget form elements found:', {
            budgetForm: $('#budgetForm').length,
            savingsPercentage: $('#savingsPercentage').length,
            needsPercentage: $('#needsPercentage').length,
            wantsPercentage: $('#wantsPercentage').length,
            budgetModal: $('#budgetModal').length
        });

        // Check for any other forms that might conflict
        console.log('🔍 BudgetManagement.js: All forms on page:', $('form').map(function() {
            return { id: this.id, action: this.action, class: this.className };
        }).get());

        // Check for any validation libraries
        console.log('🔍 BudgetManagement.js: jQuery validation loaded:', typeof $.validator !== 'undefined');
        if (typeof $.validator !== 'undefined') {
            console.log('🔍 BudgetManagement.js: jQuery validator methods:', Object.keys($.validator.methods));
        }
        
        // Check for any global functions that might interfere
        console.log('🔍 BudgetManagement.js: Global functions check:', {
            showSavingsAlert: typeof window.showSavingsAlert !== 'undefined',
            showAlert: typeof window.showAlert !== 'undefined',
            alert: typeof window.alert !== 'undefined'
        });
    });

    // Reset budget modal to default state
    function resetBudgetModal() {
        console.log('🔍 BudgetManagement.js: resetBudgetModal() called');
        $('#budgetForm')[0].reset();
        $('#budgetUserId').val('');
        $('#budgetModalLabel').text('Add Budget Rule');
        $('#budgetForm button[type="submit"]').text('Save Budget Rule');
        
        // Reset percentage fields to default values
        $('#savingsPercentage').val(50);
        $('#needsPercentage').val(30);
        $('#wantsPercentage').val(20);
        
        // Hide validation summary
        $('#validationSummary').hide();
        
        // Reset submit button state
        const submitBtn = $('#budgetForm button[type="submit"]');
        submitBtn.prop('disabled', true).removeClass('btn-primary').addClass('btn-secondary');
        
        console.log('🔍 BudgetManagement.js: Modal reset complete');
    }

    // Validate percentages and show summary
    function validatePercentages() {
        console.log('🔍 BudgetManagement.js: validatePercentages() called');
        const savingsPercent = parseFloat($('#savingsPercentage').val()) || 0;
        const needsPercent = parseFloat($('#needsPercentage').val()) || 0;
        const wantsPercent = parseFloat($('#wantsPercentage').val()) || 0;
        const totalPercent = savingsPercent + needsPercent + wantsPercent;
        
        console.log('🔍 BudgetManagement.js: Percentages - Savings:', savingsPercent, 'Needs:', needsPercent, 'Wants:', wantsPercent, 'Total:', totalPercent);
        
        // Show budget summary
        showBudgetSummary(savingsPercent, needsPercent, wantsPercent, totalPercent);
    }

    // Show budget summary
    function showBudgetSummary(savingsPercent, needsPercent, wantsPercent, totalPercent) {
        console.log('🔍 BudgetManagement.js: showBudgetSummary() called');
        const isExact100 = Math.abs(totalPercent - 100) <= 0.1; // Allow small rounding differences
        
        console.log('🔍 BudgetManagement.js: Is exact 100%:', isExact100);
        
        let summaryText = `
            <div class="row mt-2">
                <div class="col-6">
                    <strong>Savings:</strong> ${savingsPercent.toFixed(1)}%
                </div>
                <div class="col-6">
                    <strong>Needs:</strong> ${needsPercent.toFixed(1)}%
                </div>
            </div>
            <div class="row">
                <div class="col-6">
                    <strong>Wants:</strong> ${wantsPercent.toFixed(1)}%
                </div>
                <div class="col-6">
                    <strong>Total:</strong> 
                    <span class="${isExact100 ? 'text-success' : 'text-danger'}">
                        ${totalPercent.toFixed(1)}%
                    </span>
                </div>
            </div>
            <div class="row mt-2">
                <div class="col-12">
                    <strong>Status:</strong> 
                    <span class="${isExact100 ? 'text-success' : 'text-danger'}">
                        ${isExact100 ? '✅ Valid' : '⚠️ Invalid'}
                    </span>
                    ${!isExact100 ? `<br><small class="text-danger">Percentages must equal exactly 100% to save</small>` : '<br><small class="text-success">Ready to save!</small>'}
                </div>
            </div>
        `;
        
        $('#budgetSummaryText').html(summaryText);
        $('#validationSummary').show();
        
        // Enable/disable submit button based on percentage validation
        const submitBtn = $('#budgetForm button[type="submit"]');
        if (isExact100) {
            submitBtn.prop('disabled', false).removeClass('btn-secondary').addClass('btn-primary');
            console.log('🔍 BudgetManagement.js: Submit button enabled');
        } else {
            submitBtn.prop('disabled', true).removeClass('btn-primary').addClass('btn-secondary');
            console.log('🔍 BudgetManagement.js: Submit button disabled');
        }
    }

    // Submit budget rule
    function submitBudgetRule() {
        console.log('🔍 BudgetManagement.js: submitBudgetRule() called');
        
        const formData = {
            Savings: parseInt($('#savingsPercentage').val()) || 0,
            Needs: parseInt($('#needsPercentage').val()) || 0,
            Wants: parseInt($('#wantsPercentage').val()) || 0
        };

        console.log('🔍 BudgetManagement.js: Form data prepared:', formData);

        // Validate percentages
        const totalPercent = formData.Savings + formData.Needs + formData.Wants;
        console.log('🔍 BudgetManagement.js: Total percentage:', totalPercent);

        // Validate that percentages sum to 100%
        if (totalPercent !== 100) {
            console.log('🔍 BudgetManagement.js: Validation failed - percentages do not equal 100%');
            showBudgetAlert(`Percentages must sum to 100%. Current total: ${totalPercent}%`, 'danger');
            return;
        }

        console.log('🔍 BudgetManagement.js: Validation passed - proceeding with submission');

        // Show loading state
        const submitBtn = $('#budgetForm button[type="submit"]');
        const originalText = submitBtn.text();
        submitBtn.prop('disabled', true).text('Saving...');

        console.log('🔍 BudgetManagement.js: Sending AJAX request to /Budget/CreateBudgetRule');

        $.ajax({
            url: '/Budget/CreateBudgetRule',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function(response) {
                console.log('🔍 BudgetManagement.js: AJAX success response:', response);
                if (response.success) {
                    showBudgetAlert('Budget rule created successfully!', 'success');
                    $('#budgetModal').modal('hide');
                    refreshBudget();
                } else {
                    showBudgetAlert(response.message || 'Failed to create budget rule.', 'danger');
                }
            },
            error: function(xhr, status, error) {
                console.error('🔍 BudgetManagement.js: AJAX error:', { xhr, status, error });
                showBudgetAlert('An error occurred while creating the budget rule.', 'danger');
            },
            complete: function() {
                console.log('🔍 BudgetManagement.js: AJAX request completed');
                submitBtn.prop('disabled', false).text(originalText);
            }
        });
    }

    // Edit budget rule
    function editBudget(userId) {
        console.log('🔍 BudgetManagement.js: editBudget() called for user ID:', userId);
        
        // Get current budget data and populate modal
        $.ajax({
            url: `/Budget/GetBudgetRuleById/${userId}`,
            type: 'GET',
            success: function(response) {
                console.log('🔍 BudgetManagement.js: Edit budget response:', response);
                if (response.success && response.data) {
                    const budget = response.data;
                    $('#budgetUserId').val(budget.UserID);
                    
                    // Set percentages directly
                    $('#savingsPercentage').val(budget.Savings);
                    $('#needsPercentage').val(budget.Needs);
                    $('#wantsPercentage').val(budget.Wants);
                    
                    // Validate and show summary
                    validatePercentages();
                    
                    // Change modal title and button
                    $('#budgetModalLabel').text('Edit Budget Rule');
                    $('#budgetForm button[type="submit"]').text('Update Budget Rule');
                    
                    // Show modal
                    $('#budgetModal').modal('show');
                } else {
                    showBudgetAlert('Failed to load budget rule data.', 'danger');
                }
            },
            error: function(xhr, status, error) {
                console.error('🔍 BudgetManagement.js: Error loading budget rule:', error);
                showBudgetAlert('An error occurred while loading the budget rule.', 'danger');
            }
        });
    }

    // Delete budget rule
    function deleteBudget(userId) {
        console.log('🔍 BudgetManagement.js: deleteBudget() called for user ID:', userId);
        
        if (confirm('Are you sure you want to delete this budget rule? This action cannot be undone.')) {
            $.ajax({
                url: `/Budget/DeleteBudgetRule/${userId}`,
                type: 'DELETE',
                success: function(response) {
                    console.log('🔍 BudgetManagement.js: Delete response:', response);
                    if (response.success) {
                        showBudgetAlert('Budget rule deleted successfully!', 'success');
                        refreshBudget();
                    } else {
                        showBudgetAlert(response.message || 'Failed to delete budget rule.', 'danger');
                    }
                },
                error: function(xhr, status, error) {
                    console.error('🔍 BudgetManagement.js: Error deleting budget rule:', error);
                    showBudgetAlert('An error occurred while deleting the budget rule.', 'danger');
                }
            });
        }
    }

    // Refresh budget data
    function refreshBudget() {
        console.log('🔍 BudgetManagement.js: refreshBudget() called');
        location.reload();
    }

    // Export budget data to CSV
    function exportBudgetToCSV() {
        console.log('🔍 BudgetManagement.js: exportBudgetToCSV() called');
        const table = $('#budgetTable');
        const rows = table.find('tbody tr');
        
        let csv = 'Category,Percentage,Calculated Amount,Last Updated\n';
        
        rows.each(function() {
            const category = $(this).find('td:first').text().trim();
            const percentage = $(this).find('td:nth-child(2)').text().trim();
            const calculatedAmount = $(this).find('td:nth-child(3)').text().trim();
            const updated = $(this).find('td:nth-child(4)').text().trim();
            
            csv += `"${category}","${percentage}","${calculatedAmount}","${updated}"\n`;
        });
        
        const blob = new Blob([csv], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'budget_rules.csv';
        a.click();
        window.URL.revokeObjectURL(url);
    }

    // Print budget data
    function printBudget() {
        console.log('🔍 BudgetManagement.js: printBudget() called');
        const table = $('#budgetTable');
        const rows = table.find('tbody tr');
        const printWindow = window.open('', '_blank');
        
        let html = `
            <!DOCTYPE html>
            <html>
            <head>
                <title>Budget Rules Report</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 20px; }
                    table { border-collapse: collapse; width: 100%; margin-top: 20px; }
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
                            <th>Percentage</th>
                            <th>Calculated Amount</th>
                            <th>Last Updated</th>
                        </tr>
                    </thead>
                    <tbody>
        `;
        
        rows.each(function() {
            const category = $(this).find('td:first').text().trim();
            const percentage = $(this).find('td:nth-child(2)').text().trim();
            const calculatedAmount = $(this).find('td:nth-child(3)').text().trim();
            const updated = $(this).find('td:nth-child(4)').text().trim();
            
            html += `
                <tr>
                    <td>${category}</td>
                    <td>${percentage}</td>
                    <td>${calculatedAmount}</td>
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

    // Show budget-specific alert message (separate from savings modal)
    function showBudgetAlert(message, type) {
        console.log('🔍 BudgetManagement.js: showBudgetAlert() called with message:', message, 'type:', type);
        
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'danger' ? 'exclamation-triangle' : 'info-circle'}"></i>
                ${message}
                <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        `;
        
        // Remove existing budget alerts
        $('.alert').remove();
        
        // Add new alert at the top of the page
        $('.d-sm-flex.align-items-center.justify-content-between.mb-4').after(alertHtml);
        
        // Auto-dismiss after 5 seconds
        setTimeout(function() {
            $('.alert').fadeOut();
        }, 5000);
    }

    // Global error handler to catch any unexpected errors
    window.addEventListener('error', function(e) {
        console.error('🔍 BudgetManagement.js: Global error caught:', e);
        console.error('🔍 BudgetManagement.js: Error details:', {
            message: e.message,
            filename: e.filename,
            lineno: e.lineno,
            colno: e.colno,
            error: e.error
        });
    });

    // Log when the script is fully loaded
    console.log('🔍 BudgetManagement.js: Script fully loaded and ready');
})();
