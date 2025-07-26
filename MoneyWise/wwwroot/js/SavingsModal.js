document.getElementById("editSavingsForm").addEventListener("submit", function(e) {
    e.preventDefault();

    const savingsAmount = document.getElementById("savingsAmount").value;
    const savingsGoal = document.getElementById("savingsGoal").value;

    // Example: send to backend (adjust URL and logic as needed)
    fetch('/Dashboard/UpdateSavings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ savingsAmount, savingsGoal })
    })
    .then(res => res.ok ? res.json() : Promise.reject("Failed"))
    .then(data => {
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('savingsModal'));
        modal.hide();
        // Optionally refresh part of page or show success message
    })
    .catch(err => alert("Error updating savings"));
});