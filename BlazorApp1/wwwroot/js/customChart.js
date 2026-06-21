function load_chart(id, amounts, type) {
    switch (type) {
        case "outline":
            load_chart_outline(id, amounts);
            break;
        case "detailed":
            load_chart_detailed(id, amounts);
            break;
        default: break;
    }
}

function load_chart_outline(id, amounts) {
    const ctx = document.getElementById(id);
    labels = [];
    values = [];
    for (const key in amounts) {
        labels.push(key.toString());
        values.push(amounts[key]);
    }

    const data = {
        labels: labels,
        datasets: [{
            label: '可疑人數',
            data: values,
            backgroundColor: 'rgba(54, 162, 235, 0.5)',
            borderColor: 'rgb(54, 162, 235)',
            borderWidth: 2,
            borderRadius: 5,
            borderSkipped: false,
        }]
    };
    const config = {
        type: 'bar',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            aspectRatio: 2,
            plugins: {
                legend: {
                    position: 'top',
                },
                title: {
                    display: true,
                    text: '可疑人物統計'
                }
            },
            scales: {
                y: {
                    beginAtZero: true
                }
            },
            onResize: function (chart, size) {
                if (size.width <= 500) {
                    chart.width = size.width;
                    chart.height = 250;
                    var ratio = 250 / size.width * 100;
                    document.documentElement.style.setProperty('--chart-ratio', ratio + '%');
                    chart.config._config.options.aspectRatio = size.width / 250;
                }
            }
        }
    };

    document.documentElement.style.setProperty('--chart-ratio', '50%');
    _ = new Chart(ctx, config);
}

function load_chart_detailed(id, amounts) {
    const ctx = document.getElementById(id);
    labels = [];
    values = {
        pass: [],
        wait: [],
        wander: [],
    };
    
    for (const key in amounts['pass'])
        labels.push(key.toString());
    for (const key in amounts)
        for (const date in amounts[key])
            values[key].push(amounts[key][date]);

    const data = {
        labels: labels,
        datasets: [
            {
                label: 'pass',
                data: values.pass,
                borderColor: 'rgb(75, 192, 192)',
                backgroundColor: 'rgba(75, 192, 192, 0.5)',
                borderWidth: 2,
                borderRadius: 5,
            },
            {
                label: 'wait',
                data: values.wait,
                borderColor: 'rgb(255, 159, 64)',
                backgroundColor: 'rgba(255, 159, 64, 0.5)',
                borderWidth: 2,
                borderRadius: 5,
            },
            {
                label: 'wander',
                data: values.wander,
                borderColor: 'rgb(255, 99, 132)',
                backgroundColor: 'rgba(255, 99, 132, 0.5)',
                borderWidth: 2,
                borderRadius: 5,
            }
        ]
    };
    const config = {
        type: 'bar',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                },
                title: {
                    display: true,
                    text: '詳細統計圖'
                }
            },
            scales: {
                x: {
                    stacked: true,
                },
                y: {
                    stacked: true,
                    beginAtZero: true
                }
            },
            onResize: function (chart, size) {
                if (size.width <= 500) {
                    chart.width = size.width;
                    chart.height = 250;
                    var ratio = 250 / size.width * 100;
                    document.documentElement.style.setProperty('--chart-ratio', ratio + '%');
                    chart.config._config.options.aspectRatio = size.width / 250;
                }
            }
        }
    };

    document.documentElement.style.setProperty('--chart-ratio', '50%');
    _ = new Chart(ctx, config);
}
