function checkSession() {
    fetch('/api/session/validate')
        .then(res => res.json())
        .then(data => {
            if (!data.valid) {
                const modal = new bootstrap.Modal(document.getElementById('sessionExpiredModal'));
                modal.show();
                clearInterval(sessionCheckInterval);
            }
        })
        .catch(err => {
            console.error("Session check failed", err);
        });
}

// ⏱ Ping every 10 seconds (adjust as needed)
const sessionCheckInterval = setInterval(checkSession, 10000);