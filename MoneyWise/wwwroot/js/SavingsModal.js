// Savings Modal JavaScript - Completely Isolated Version
(function() {
    'use strict';
    
    // Only run this script if we're on a page with the savings modal
    if (typeof $ === 'undefined') {
        console.log('💰 SavingsModal.js: jQuery not loaded - skipping initialization');
        return;
    }
    
    $(document).ready(function() {
        console.log('💰 SavingsModal.js: Document ready - Checking if savings modal exists');
        
        // Only initialize if the savings modal exists on this page
        if ($('#savingsModal').length === 0) {
            console.log('💰 SavingsModal.js: No savings modal found on this page - completely skipping initialization');
            return;
        }
        
        console.log('💰 SavingsModal.js: Savings modal found - initializing savings modal');
        
        // Load savings data when modal opens
        $('#savingsModal').on('show.bs.modal', function () {
            console.log('💰 SavingsModal.js: Savings modal opening');
            loadSavingsData();
            resetSavingsForm();
        });

        // Reset form when modal opens
        function resetSavingsForm() {
            console.log('💰 SavingsModal.js: Resetting savings form');
            $('#editSavingsForm')[0].reset();
            $('#submitSavingsBtn').prop('disabled', true);
            $('#savingsAmount').removeClass('is-invalid');
            $('input[name="actionType"]').removeClass('is-invalid');
        }

        // Function to load savings data from the server
        function loadSavingsData() {
            console.log('💰 SavingsModal.js: Loading savings data');
            fetch('/Home/GetSavings', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            })
            .then(res => res.ok ? res.json() : Promise.reject("Failed to load savings"))
            .then(data => {
                console.log('💰 SavingsModal.js: Savings data loaded:', data);
                if (data.success) {
                    $('#savingsAmount').val(data.savingsAmount || 0);
                } else {
                    console.error("💰 SavingsModal.js: Error loading savings:", data.message);
                    $('#savingsAmount').val(0);
                }
            })
            .catch(err => {
                console.error("💰 SavingsModal.js: Error loading savings:", err);
                $('#savingsAmount').val(0);
            });
        }

        // Function to update savings summary display
        function updateSavingsSummaryDisplay(data) {
            console.log('💰 SavingsModal.js: Updating savings summary display:', data);
            // Update current balance
            const currentBalanceElement = $('#currentBalance');
            if (currentBalanceElement.length) {
                currentBalanceElement.text(`₱${data.currentBalance.toFixed(2)}`);
                // Add color coding for balance
                if (data.currentBalance > 0) {
                    currentBalanceElement.removeClass('text-danger').addClass('text-success');
                } else if (data.currentBalance < 0) {
                    currentBalanceElement.removeClass('text-success').addClass('text-danger');
                } else {
                    currentBalanceElement.removeClass('text-success text-danger');
                }
            }
        }

        // Form validation - ONLY for savings modal
        if ($('#editSavingsForm').length > 0) {
            $('#savingsAmount').on('input', validateSavingsForm);
            $('input[name="actionType"]').on('change', validateSavingsForm);
        }

        function validateSavingsForm() {
            console.log('💰 SavingsModal.js: Validating savings form');
            const amount = $('#savingsAmount').val();
            const actionType = $('input[name="actionType"]:checked');
            const submitBtn = $('#submitSavingsBtn');
            
            // Validate amount
            const amountValid = amount && parseFloat(amount) > 0;
            $('#savingsAmount').toggleClass('is-invalid', !amountValid);
            
            // Validate action type
            const actionValid = actionType.length > 0;
            $('input[name="actionType"]').toggleClass('is-invalid', !actionValid);
            
            // Enable/disable submit button
            submitBtn.prop('disabled', !(amountValid && actionValid));
            
            console.log('💰 SavingsModal.js: Validation result - Amount valid:', amountValid, 'Action valid:', actionValid);
        }

        // Handle form submission - ONLY for savings modal
        if ($('#editSavingsForm').length > 0) {
            $('#editSavingsForm').on('submit', function(e) {
                console.log('💰 SavingsModal.js: Savings form submit event triggered');
                e.preventDefault();

                const savingsAmount = $('#savingsAmount').val();
                const actionType = $('input[name="actionType"]:checked').val();

                console.log('💰 SavingsModal.js: Form submission - Amount:', savingsAmount, 'Action:', actionType);

                // Validate inputs
                if (!savingsAmount || parseFloat(savingsAmount) <= 0) {
                    console.log('💰 SavingsModal.js: Validation failed - invalid amount');
                    showSavingsAlert("Please enter a valid amount", "danger");
                    return;
                }

                if (!actionType) {
                    console.log('💰 SavingsModal.js: Validation failed - no action type');
                    showSavingsAlert("Please select an action type", "danger");
                    return;
                }

                console.log('💰 SavingsModal.js: Validation passed - sending request to /Home/UpdateSavings');

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
                    console.log('💰 SavingsModal.js: Update savings response:', data);
                    if (data.success) {
                        $('#savingsModal').modal('hide');
                        // Show success message
                        showSavingsAlert(data.message || "Transaction completed successfully!", "success");
                        // Refresh savings data
                        if (window.SavingsCalculator && window.SavingsCalculator.refreshSavingsData) {
                            window.SavingsCalculator.refreshSavingsData();
                        }
                    } else {
                        showSavingsAlert(data.message || "Error processing transaction", "danger");
                    }
                })
                .catch(err => {
                    console.error("💰 SavingsModal.js: Error updating savings:", err);
                    showSavingsAlert("Error processing transaction. Please try again.", "danger");
                });
            });
        }

        // Show savings-specific alert message
        function showSavingsAlert(message, type) {
            console.log('💰 SavingsModal.js: showSavingsAlert() called with message:', message, 'type:', type);
            
            const alertHtml = `
                <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                    <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'danger' ? 'exclamation-triangle' : 'info-circle'}"></i>
                    ${message}
                    <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
            `;
            
            // Remove existing savings alerts
            $('.alert').remove();
            
            // Add new alert at the top of the page
            $('.d-sm-flex.align-items-center.justify-content-between.mb-4').after(alertHtml);
            
            // Auto-dismiss after 5 seconds
            setTimeout(function() {
                $('.alert').fadeOut();
            }, 5000);
        }

        // Log savings modal elements
        console.log('💰 SavingsModal.js: Savings modal elements found:', {
            savingsModal: $('#savingsModal').length,
            editSavingsForm: $('#editSavingsForm').length,
            savingsAmount: $('#savingsAmount').length,
            actionTypeInputs: $('input[name="actionType"]').length,
            submitSavingsBtn: $('#submitSavingsBtn').length
        });

        console.log('💰 SavingsModal.js: Script fully loaded and ready');
    });
})();