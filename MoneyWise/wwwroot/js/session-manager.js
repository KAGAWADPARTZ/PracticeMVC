// Session Manager - Handles session expiration and automatic logout
class SessionManager {
    constructor() {
        this.sessionCheckInterval = null;
        this.warningShown = false;
        this.warningTimeout = null;
        this.init();
    }

    init() {
        // Start session monitoring
        this.startSessionMonitoring();
        
        // Add event listeners for user activity
        this.addActivityListeners();
        
        // Check session immediately
        this.checkSession();
    }

    startSessionMonitoring() {
        // Check session every 30 seconds
        this.sessionCheckInterval = setInterval(() => {
            this.checkSession();
        }, 30000); // 30 seconds
    }

    addActivityListeners() {
        // Reset session timer on user activity
        const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];
        events.forEach(event => {
            document.addEventListener(event, () => {
                this.resetSessionTimer();
            }, true);
        });
    }

    async checkSession() {
        try {
            const response = await fetch('/Home/GetSessionInfo', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (!response.ok) {
                if (response.status === 401 || response.status === 403) {
                    this.handleSessionExpired();
                    return;
                }
                throw new Error('Network response was not ok');
            }

            const data = await response.json();
            
            if (!data.success) {
                if (data.message && data.message.includes('expired')) {
                    this.handleSessionExpired();
                    return;
                }
                return;
            }

            const sessionInfo = data.data;
            
            // Check if session is expiring soon (within 5 minutes)
            if (sessionInfo.isExpiringSoon && !this.warningShown) {
                this.showSessionWarning(sessionInfo.formattedTime);
            }

            // Update session timer display if it exists
            this.updateSessionTimer(sessionInfo.formattedTime);

        } catch (error) {
            console.error('Error checking session:', error);
            // If we can't reach the server, assume session might be expired
            this.handleSessionExpired();
        }
    }

    showSessionWarning(remainingTime) {
        this.warningShown = true;
        
        // Create warning modal
        const modal = document.createElement('div');
        modal.className = 'modal fade show';
        modal.style.display = 'block';
        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-warning">
                        <h5 class="modal-title">
                            <i class="fas fa-exclamation-triangle"></i> Session Expiring Soon
                        </h5>
                    </div>
                    <div class="modal-body">
                        <p>Your session will expire in <strong>${remainingTime}</strong>.</p>
                        <p>Would you like to extend your session?</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" onclick="sessionManager.extendSession()">
                            <i class="fas fa-clock"></i> Extend Session
                        </button>
                        <button type="button" class="btn btn-primary" onclick="sessionManager.logout()">
                            <i class="fas fa-sign-out-alt"></i> Logout Now
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        // Auto-logout after 2 minutes if user doesn't respond
        this.warningTimeout = setTimeout(() => {
            this.handleSessionExpired();
        }, 120000); // 2 minutes
    }

    async extendSession() {
        try {
            // Clear warning
            this.clearWarning();
            
            // Simulate user activity by making a request
            const response = await fetch('/Home/GetSessionInfo', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (response.ok) {
                this.warningShown = false;
                this.showSuccessMessage('Session extended successfully!');
            } else {
                this.handleSessionExpired();
            }
        } catch (error) {
            console.error('Error extending session:', error);
            this.handleSessionExpired();
        }
    }

    clearWarning() {
        const modal = document.querySelector('.modal.show');
        if (modal) {
            modal.remove();
        }
        
        if (this.warningTimeout) {
            clearTimeout(this.warningTimeout);
            this.warningTimeout = null;
        }
    }

    resetSessionTimer() {
        // Reset warning flag when user is active
        this.warningShown = false;
    }

    updateSessionTimer(formattedTime) {
        // Update session timer in UI if it exists
        const timerElement = document.getElementById('session-timer');
        if (timerElement) {
            timerElement.textContent = formattedTime;
        }
    }

    showSuccessMessage(message) {
        // Show success message
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-success alert-dismissible fade show position-fixed';
        alertDiv.style.top = '20px';
        alertDiv.style.right = '20px';
        alertDiv.style.zIndex = '9999';
        alertDiv.innerHTML = `
            <i class="fas fa-check-circle"></i> ${message}
            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        `;

        document.body.appendChild(alertDiv);

        // Auto-remove after 3 seconds
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.remove();
            }
        }, 3000);
    }

    handleSessionExpired() {
        // Clear any existing warnings
        this.clearWarning();
        
        // Stop session monitoring
        if (this.sessionCheckInterval) {
            clearInterval(this.sessionCheckInterval);
        }

        // Show session expired message
        const modal = document.createElement('div');
        modal.className = 'modal fade show';
        modal.style.display = 'block';
        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title">
                            <i class="fas fa-exclamation-circle"></i> Session Expired
                        </h5>
                    </div>
                    <div class="modal-body">
                        <p>Your session has expired due to inactivity.</p>
                        <p>You will be redirected to the login page.</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-primary" onclick="sessionManager.redirectToLogin()">
                            <i class="fas fa-sign-in-alt"></i> Go to Login
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        // Auto-redirect after 3 seconds
        setTimeout(() => {
            this.redirectToLogin();
        }, 3000);
    }

    redirectToLogin() {
        window.location.href = '/Login/Index';
    }

    logout() {
        // Clear warning
        this.clearWarning();
        
        // Redirect to logout
        window.location.href = '/Login/Logout';
    }
}

// Initialize session manager when DOM is loaded
let sessionManager;
document.addEventListener('DOMContentLoaded', function() {
    sessionManager = new SessionManager();
});

// Export for global access
window.sessionManager = sessionManager;
