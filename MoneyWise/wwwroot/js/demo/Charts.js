document.addEventListener("DOMContentLoaded", function () {
        fetch("/Home/GetMonthlyEarnings")
            .then(response => response.json())
            .then(result => {
                if (result.success) {
                    const labels = Object.keys(result.data);
                    const values = Object.values(result.data);

                    const ctx = document.getElementById("myAreaChart").getContext("2d");
                    new Chart(ctx, {
                        type: "line",
                        data: {
                            labels: labels,
                            datasets: [{
                                label: "Monthly Earnings",
                                data: values,
                                backgroundColor: "rgba(78, 115, 223, 0.05)",
                                borderColor: "rgba(78, 115, 223, 1)",
                                borderWidth: 2,
                                fill: true,
                                tension: 0.3
                            }]
                        },
                        options: {
                            responsive: true,
                            plugins: {
                                legend: { display: false }
                            },
                            scales: {
                                y: {
                                    beginAtZero: true,
                                    ticks: {
                                        callback: function (value) {
                                            return '₱' + value.toLocaleString();
                                        }
                                    }
                                }
                            }
                        }
                    });
                } else {
                    console.error(result.message);
                }
            })
    });