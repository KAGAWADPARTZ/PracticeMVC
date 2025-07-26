document.getElementById("editSavingsForm").addEventListener("submit", function (e) {
    e.preventDefault();

    const savingsAmount = document.getElementById("savingsAmount").value;
    const savingsGoal = document.getElementById("savingsGoal").value;

    fetch('/Index/UpdateSavings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ savingsAmount, savingsGoal })
    })
        .then(res => res.ok ? res.json() : Promise.reject("Failed"))
        .then(data => {
            $('#savingsModal').modal('hide'); // Bootstrap 4-compatible way to close modal
            // Optionally refresh page or show notification
        })
        .catch(err => alert("Error updating savings"));
});