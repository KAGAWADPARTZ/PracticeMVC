$(document).ready(function() {
    // Initialize the budget rules functionality
    initializeBudgetRules();
    
    // Set up form submission
    $('#budgetRuleForm').on('submit', function(e) {
        e.preventDefault();
        submitBudgetRule();
    });
    
    // Set up percentage change handlers
    $('#savingsPercentage, #needsPercentage, #wantsPercentage').on('input', function() {
        calculateAmounts();
    });
    
    // Set up total amount change handler
    $('#totalAmount').on('input', function() {
        calculateAmounts();
    });
});

function initializeBudgetRules() {
    // Reset modal when it's hidden
    $('#budgetRuleModal').on('hidden.bs.modal', function() {
        resetModal();
    });
    
    // Set default percentages
    $('#savingsPercentage').val(50);
    $('#needsPercentage').val(30);
    $('#wantsPercentage').val(20);
}

function calculateAmounts() {
    const totalAmount = parseFloat($('#totalAmount').val()) || 0;
    const savingsPercentage = parseFloat($('#savingsPercentage').val()) || 0;
    const needsPercentage = parseFloat($('#needsPercentage').val()) || 0;
    const wantsPercentage = parseFloat($('#wantsPercentage').val()) || 0;
    
    // Calculate amounts based on percentages
    const savingsAmount = (totalAmount * savingsPercentage / 100);
    const needsAmount = (totalAmount * needsPercentage / 100);
    const wantsAmount = (totalAmount * wantsPercentage / 100);
    
    // Update amount fields
    $('#savingsAmount').val(savingsAmount.toFixed(2));
    $('#needsAmount').val(needsAmount.toFixed(2));
    $('#wantsAmount').val(wantsAmount.toFixed(2));
    
    // Show validation summary
    showValidationSummary(totalAmount, savingsAmount, needsAmount, wantsAmount);
}

function showValidationSummary(total, savings, needs, wants) {
    const totalCalculated = savings + needs + wants;
    const difference = total - totalCalculated;
    
    let summaryText = `
        <div class="row">
            <div class="col-6">Total Income: ₱${total.toFixed(2)}</div>
            <div class="col-6">Total Budgeted: ₱${totalCalculated.toFixed(2)}</div>
        </div>
        <div class="row">
            <div class="col-6">Savings: ₱${savings.toFixed(2)} (${((savings/total)*100).toFixed(1)}%)</div>
            <div class="col-6">Needs: ₱${needs.toFixed(2)} (${((needs/total)*100).toFixed(1)}%)</div>
        </div>
        <div class="row">
            <div class="col-6">Wants: ₱${wants.toFixed(2)} (${((wants/total)*100).toFixed(1)}%)</div>
            <div class="col-6">Remaining: ₱${difference.toFixed(2)}</div>
        </div>
    `;
    
    $('#budgetSummaryText').html(summaryText);
    $('#validationSummary').show();
}

function submitBudgetRule() {
    // Get form data
    const formData = {
        TotalAmount: parseInt($('#totalAmount').val()),
        Savings: parseInt($('#savingsAmount').val()),
        Needs: parseInt($('#needsAmount').val()),
        Wants: parseInt($('#wantsAmount').val())
    };
    
    // Validate form data
    if (!validateFormData(formData)) {
        return;
    }
    
    // Show loading state
    const submitBtn = $('#budgetRuleForm button[type="submit"]');
    const originalText = submitBtn.text();
    submitBtn.prop('disabled', true).text('Saving...');
    
    // Submit to server
    $.ajax({
        url: '/BudgetRules/CreateBudgetRule',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(response) {
            if (response.success) {
                // Show success message
                showAlert('success', response.message || 'Budget rule created successfully!');
                
                // Close modal
                $('#budgetRuleModal').modal('hide');
                
                // Refresh the page to show new data
                setTimeout(function() {
                    location.reload();
                }, 1500);
            } else {
                showAlert('danger', response.message || 'Failed to create budget rule.');
            }
        },
        error: function(xhr, status, error) {
            console.error('Error creating budget rule:', error);
            let errorMessage = 'An error occurred while creating the budget rule.';
            
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            }
            
            showAlert('danger', errorMessage);
        },
        complete: function() {
            // Reset button state
            submitBtn.prop('disabled', false).text(originalText);
        }
    });
}

function validateFormData(data) {
    // Check if total amount is provided
    if (!data.TotalAmount || data.TotalAmount <= 0) {
        showAlert('danger', 'Please enter a valid total monthly income.');
        return false;
    }
    
    // Check if all amounts are provided
    if (!data.SavingsAmount || !data.NeedsAmount || !data.WantsAmount) {
        showAlert('danger', 'Please ensure all budget amounts are calculated.');
        return false;
    }
    
    // Check if amounts add up to total (with small tolerance for floating point)
    const total = data.SavingsAmount + data.NeedsAmount + data.WantsAmount;
    const difference = Math.abs(data.TotalAmount - total);
    
    if (difference > 0.01) {
        showAlert('danger', `Budget amounts (₱${total.toFixed(2)}) must equal total income (₱${data.TotalAmount.toFixed(2)}).`);
        return false;
    }
    
    return true;
}

function showAlert(type, message) {
    // Remove existing alerts
    $('.alert').remove();
    
    // Create new alert
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-triangle'}"></i>
            ${message}
            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `;
    
    // Insert alert at the top of the page
    $('.d-sm-flex').after(alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut();
    }, 5000);
}

function resetModal() {
    // Reset form
    $('#budgetRuleForm')[0].reset();
    
    // Reset hidden fields
    $('#budgetRuleId').val('');
    
    // Reset percentages to defaults
    $('#savingsPercentage').val(50);
    $('#needsPercentage').val(30);
    $('#wantsPercentage').val(20);
    
    // Clear calculated amounts
    $('#savingsAmount').val('');
    $('#needsAmount').val('');
    $('#wantsAmount').val('');
    
    // Hide validation summary
    $('#validationSummary').hide();
    
    // Reset modal title
    $('#budgetRuleModalLabel').text('Add Budget Rule');
    
    // Reset submit button
    $('#budgetRuleForm button[type="submit"]').text('Save Budget Rule');
}

function editBudgetRule(id) {
    // Load budget rule data for editing
    $.ajax({
        url: `/BudgetRules/GetBudgetRuleById/${id}`,
        type: 'GET',
        success: function(response) {
            if (response.success) {
                const budgetRule = response.data;
                
                // Populate form fields
                $('#budgetRuleId').val(budgetRule.BudgetRulesID);
                $('#totalAmount').val(budgetRule.TotalAmount);
                $('#savingsAmount').val(budgetRule.Savings);
                $('#needsAmount').val(budgetRule.Needs);
                $('#wantsAmount').val(budgetRule.Wants);
                
                // Calculate percentages
                const total = budgetRule.TotalAmount;
                $('#savingsPercentage').val(((budgetRule.Savings / total) * 100).toFixed(1));
                $('#needsPercentage').val(((budgetRule.Needs / total) * 100).toFixed(1));
                $('#wantsPercentage').val(((budgetRule.Wants / total) * 100).toFixed(1));
                
                // Update modal title and button
                $('#budgetRuleModalLabel').text('Edit Budget Rule');
                $('#budgetRuleForm button[type="submit"]').text('Update Budget Rule');
                
                // Show validation summary
                showValidationSummary(total, budgetRule.Savings, budgetRule.Needs, budgetRule.Wants);
                
                // Show modal
                $('#budgetRuleModal').modal('show');
            } else {
                showAlert('danger', response.message || 'Failed to load budget rule for editing.');
            }
        },
        error: function() {
            showAlert('danger', 'An error occurred while loading the budget rule.');
        }
    });
}

function deleteBudgetRule(id) {
    if (confirm('Are you sure you want to delete this budget rule? This action cannot be undone.')) {
        $.ajax({
            url: `/BudgetRules/DeleteBudgetRule/${id}`,
            type: 'DELETE',
            success: function(response) {
                if (response.success) {
                    showAlert('success', 'Budget rule deleted successfully!');
                    setTimeout(function() {
                        location.reload();
                    }, 1500);
                } else {
                    showAlert('danger', response.message || 'Failed to delete budget rule.');
                }
            },
            error: function() {
                showAlert('danger', 'An error occurred while deleting the budget rule.');
            }
        });
    }
}

function refreshBudgetRules() {
    location.reload();
}

function exportToCSV() {
    // Implementation for CSV export
    showAlert('info', 'CSV export functionality will be implemented soon.');
}

function printBudgetRules() {
    // Implementation for printing
    window.print();
}
