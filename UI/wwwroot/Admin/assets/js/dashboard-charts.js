// ========================================
// ADMIN DASHBOARD CHARTS
// ========================================

(function () {
    'use strict';

    // Wait for DOM to be ready
    document.addEventListener('DOMContentLoaded', function () {

        // Check if we have the dashboard data
        if (typeof dashboardData === 'undefined') {
            console.warn('Dashboard data not loaded');
            return;
        }

        initializeCharts(dashboardData);
    });

    function initializeCharts(data) {
        // ========================================
        // SHIPMENTS & REVENUE TREND CHART
        // ========================================

        var ctx1 = document.getElementById('shipments-revenue-chart');
        if (ctx1) {
            var shipmentRevenueChart = new Chart(ctx1.getContext('2d'), {
                type: 'line',
                data: {
                    labels: data.shipmentsData.map(x => x.date),
                    datasets: [
                        {
                            label: data.labels?.shipments || 'Shipments',
                            data: data.shipmentsData.map(x => x.count),
                            borderColor: 'rgb(75, 192, 192)',
                            backgroundColor: 'rgba(75, 192, 192, 0.1)',
                            borderWidth: 1.5,
                            pointRadius: 2,
                            tension: 0.4,
                            yAxisID: 'y'
                        },
                        {
                            label: data.labels?.revenue || 'Revenue ($)',
                            data: data.revenueData.map(x => x.revenue),
                            borderColor: 'rgb(54, 162, 235)',
                            backgroundColor: 'rgba(54, 162, 235, 0.1)',
                            borderWidth: 1.5,
                            pointRadius: 2,
                            tension: 0.4,
                            yAxisID: 'y1'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: {
                        mode: 'index',
                        intersect: false
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top',
                            labels: {
                                boxWidth: 12,
                                font: {
                                    size: 10
                                },
                                padding: 8
                            }
                        }
                    },
                    scales: {
                        x: {
                            ticks: {
                                font: {
                                    size: 9
                                },
                                maxRotation: 45,
                                minRotation: 45
                            },
                            grid: {
                                display: false
                            }
                        },
                        y: {
                            type: 'linear',
                            display: true,
                            position: 'left',
                            ticks: {
                                font: {
                                    size: 9
                                }
                            },
                            title: {
                                display: false
                            }
                        },
                        y1: {
                            type: 'linear',
                            display: true,
                            position: 'right',
                            ticks: {
                                font: {
                                    size: 9
                                }
                            },
                            title: {
                                display: false
                            },
                            grid: {
                                drawOnChartArea: false
                            }
                        }
                    }
                }
            });
        }

        // ========================================
        // SHIPMENTS BY STATUS PIE CHART
        // ========================================

        var ctx2 = document.getElementById('status-chart');
        if (ctx2) {
            var localizedStatusLabels = {
                created: data.labels?.created || 'Created',
                updated: data.labels?.updated || 'Updated',
                approved: data.labels?.approved || 'Approved',
                readyforshipping: data.labels?.readyForShipping || 'Ready for Shipping',
                shipped: data.labels?.shipped || 'Shipped',
                delivered: data.labels?.delivered || 'Delivered',
                cancelled: data.labels?.cancelled || 'Cancelled',
                returned: data.labels?.returned || 'Returned',
                refunded: data.labels?.refunded || 'Refunded'
            };

            var rawStatusKeys = Object.keys(data.statusData || {});
            var statusLabels = rawStatusKeys.map(function (key) {
                var normalized = String(key || '').replace(/\s+/g, '').toLowerCase();
                return localizedStatusLabels[normalized] || data.labels?.unknown || key;
            });

            var statusChart = new Chart(ctx2.getContext('2d'), {
                type: 'doughnut',
                data: {
                    labels: statusLabels,
                    datasets: [{
                        data: Object.values(data.statusData),
                        backgroundColor: [
                            'rgb(255, 99, 132)',
                            'rgb(54, 162, 235)',
                            'rgb(255, 205, 86)',
                            'rgb(75, 192, 192)',
                            'rgb(153, 102, 255)'
                        ],
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                            labels: {
                                boxWidth: 10,
                                font: {
                                    size: 9
                                },
                                padding: 5
                            }
                        }
                    }
                }
            });
        }

        // ========================================
        // TOP DESTINATION CITIES BAR CHART
        // ========================================

        var ctx3 = document.getElementById('top-cities-chart');
        if (ctx3) {
            var citiesChart = new Chart(ctx3.getContext('2d'), {
                type: 'bar',
                data: {
                    labels: Object.keys(data.citiesData),
                    datasets: [{
                        label: data.labels?.shipments || 'Shipments',
                        data: Object.values(data.citiesData),
                        backgroundColor: 'rgba(54, 162, 235, 0.8)',
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        x: {
                            ticks: {
                                font: {
                                    size: 9
                                }
                            },
                            grid: {
                                display: false
                            }
                        },
                        y: {
                            beginAtZero: true,
                            ticks: {
                                font: {
                                    size: 9
                                }
                            },
                            title: {
                                display: false
                            }
                        }
                    }
                }
            });
        }
    }

})();
