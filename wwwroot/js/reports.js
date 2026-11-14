// Option 10 - dashboard, reports and profit pages
// Charts (Chart.js), CSV export, print button, quick date ranges and the date-range validation rule.
var RP = (function () {
    'use strict';

    var css = getComputedStyle(document.documentElement);
    function token(name, fallback) { return (css.getPropertyValue(name) || '').trim() || fallback; }

    var C = {
        red: token('--dt-red', '#E50914'),
        grid: token('--dt-gray-700', '#3A3A3A'),
        text: token('--dt-gray-400', '#A0A0A0'),
        success: token('--dt-success', '#22C55E'),
        warning: token('--dt-warning', '#FACC15'),
        info: token('--dt-info', '#3B82F6'),
        error: token('--dt-error', '#EF4444'),
        steel: '#7d7d7d'
    };

    if (window.Chart) {
        Chart.defaults.color = C.text;
        Chart.defaults.borderColor = C.grid;
        Chart.defaults.font.family = "'Inter', 'Segoe UI', sans-serif";
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
        Chart.defaults.plugins.legend.labels.boxWidth = 8;
        Chart.defaults.plugins.tooltip.padding = 10;
    }

    // ---------- helpers
    function money(n) { return 'LKR ' + Math.round(n).toLocaleString('en-US'); }
    function short(n) {
        var a = Math.abs(n);
        if (a >= 1e6) return (n / 1e6).toFixed(a >= 1e7 ? 0 : 1).replace(/\.0$/, '') + 'M';
        if (a >= 1e3) return Math.round(n / 1e3) + 'K';
        return String(n);
    }
    function sum(arr) { return arr.reduce(function (t, x) { return t + x; }, 0); }

    function canvas(id) {
        var el = document.getElementById(id);
        if (!el) return null;
        if (!window.Chart) {
            el.parentNode.innerHTML = '<div class="rp-chart-fallback"><i class="bi bi-bar-chart-line"></i>' +
                '<span>The chart could not be loaded.<br>Check your internet connection and refresh.</span></div>';
            return null;
        }
        return el;
    }

    // ---------- charts
    // Stacked columns (revenue by month, payroll by month, mechanic jobs)
    function stacked(id, payload, opts) {
        opts = opts || {};
        var el = canvas(id); if (!el) return null;
        var colors = opts.colors || [C.red, C.steel];
        var fmt = opts.count ? function (v) { return v; } : money;
        return new Chart(el, {
            type: 'bar',
            data: {
                labels: payload.labels,
                datasets: payload.datasets.map(function (d, i) {
                    return { label: d.label, data: d.data, backgroundColor: colors[i % colors.length], borderRadius: 4, maxBarThickness: 48 };
                })
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: {
                    x: { stacked: true, grid: { display: false } },
                    y: { stacked: true, beginAtZero: true, ticks: { precision: 0, callback: opts.count ? undefined : short } }
                },
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function (c) { return ' ' + c.dataset.label + ': ' + fmt(c.parsed.y); },
                            footer: function (items) { return 'Total: ' + fmt(sum(items.map(function (i) { return i.parsed.y; }))); }
                        }
                    }
                }
            }
        });
    }

    // Horizontal bars (most-used parts)
    function hbar(id, payload, opts) {
        opts = opts || {};
        var el = canvas(id); if (!el) return null;
        return new Chart(el, {
            type: 'bar',
            data: { labels: payload.labels, datasets: [{ label: payload.datasets[0].label, data: payload.datasets[0].data, backgroundColor: C.red, borderRadius: 4, maxBarThickness: 26 }] },
            options: {
                indexAxis: 'y', responsive: true, maintainAspectRatio: false,
                scales: { x: { beginAtZero: true, ticks: { precision: 0 } }, y: { grid: { display: false } } },
                plugins: { legend: { display: false }, tooltip: { callbacks: { label: function (c) { return ' ' + c.dataset.label + ': ' + c.parsed.x; } } } }
            }
        });
    }

    // Doughnut (jobs by status) - same colours as the job board
    function doughnut(id, payload) {
        var el = canvas(id); if (!el) return null;
        return new Chart(el, {
            type: 'doughnut',
            data: { labels: payload.labels, datasets: [{ data: payload.datasets[0].data, backgroundColor: [C.info, C.warning, C.error, C.success], borderColor: token('--dt-gray-900', '#1E1E1E'), borderWidth: 3 }] },
            options: { responsive: true, maintainAspectRatio: false, cutout: '64%', plugins: { legend: { position: 'bottom' } } }
        });
    }

    // Profit waterfall: floating bars from start to end
    function waterfall(id, payload) {
        var el = canvas(id); if (!el) return null;
        var n = payload.labels.length;
        var colors = payload.labels.map(function (_, i) {
            if (i === 0) return C.success;
            if (i === n - 1) return payload.netIsLoss ? C.error : C.success;
            return C.red;
        });
        return new Chart(el, {
            type: 'bar',
            data: {
                labels: payload.labels,
                datasets: [{ label: 'Amount', data: payload.labels.map(function (_, i) { return [payload.starts[i], payload.ends[i]]; }), backgroundColor: colors, borderRadius: 4, maxBarThickness: 64 }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: { x: { grid: { display: false } }, y: { ticks: { callback: short } } },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (c) {
                                var s = c.raw[0], e = c.raw[1];
                                var amount = Math.abs(e - s);
                                var isCost = c.dataIndex > 0 && c.dataIndex < n - 1;
                                if (c.dataIndex === n - 1) return ' ' + (e < 0 ? 'Loss: -' : 'Profit: ') + money(Math.abs(e)).replace('LKR ', 'LKR ');
                                return ' ' + (isCost ? '-' : '') + money(amount);
                            }
                        }
                    }
                }
            }
        });
    }

    // Income and costs as lines, net profit as green / red columns
    function trend(id, payload) {
        var el = canvas(id); if (!el) return null;
        var income = payload.datasets[0], costs = payload.datasets[1], net = payload.datasets[2];
        return new Chart(el, {
            data: {
                labels: payload.labels,
                datasets: [
                    { type: 'bar', label: net.label, data: net.data, order: 2, borderRadius: 4, maxBarThickness: 40,
                      backgroundColor: net.data.map(function (v) { return v < 0 ? C.error : C.success; }) },
                    { type: 'line', label: income.label, data: income.data, order: 1, borderColor: C.info, backgroundColor: C.info, tension: .3, pointRadius: 4 },
                    { type: 'line', label: costs.label, data: costs.data, order: 1, borderColor: C.warning, backgroundColor: C.warning, tension: .3, pointRadius: 4, borderDash: [6, 4] }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                scales: { x: { grid: { display: false } }, y: { ticks: { callback: short } } },
                plugins: { legend: { position: 'bottom' }, tooltip: { callbacks: { label: function (c) { return ' ' + c.dataset.label + ': ' + money(c.parsed.y); } } } }
            }
        });
    }

    // Dashboard revenue chart with the 6 / 12 month switch
    function revenueWithSwitch(id, payload) {
        var chart = stacked(id, payload);
        if (!chart) return;
        function show(months) {
            var from = Math.max(0, payload.labels.length - months);
            chart.data.labels = payload.labels.slice(from);
            chart.data.datasets.forEach(function (ds, i) { ds.data = payload.datasets[i].data.slice(from); });
            chart.update();
        }
        document.querySelectorAll('[data-rp-months]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                document.querySelectorAll('[data-rp-months]').forEach(function (b) { b.classList.toggle('active', b === btn); });
                show(parseInt(btn.getAttribute('data-rp-months'), 10));
            });
        });
        var active = document.querySelector('[data-rp-months].active');
        if (active) show(parseInt(active.getAttribute('data-rp-months'), 10));
    }

    // Charts get dark text on a white page when printing
    function printColours(forPrint) {
        if (!window.Chart) return;
        var text = forPrint ? '#222' : C.text, grid = forPrint ? '#cccccc' : C.grid;
        Object.keys(Chart.instances).forEach(function (key) {
            var ch = Chart.instances[key];
            var scales = ch.options.scales || {};
            Object.keys(scales).forEach(function (k) {
                if (scales[k].ticks) scales[k].ticks.color = text;
                if (scales[k].grid) scales[k].grid.color = grid;
            });
            var plugins = ch.options.plugins || {};
            if (plugins.legend && plugins.legend.labels) plugins.legend.labels.color = text;
            ch.resize();
            ch.update('none');
        });
    }
    window.addEventListener('beforeprint', function () { printColours(true); });
    window.addEventListener('afterprint', function () { printColours(false); });

    // ---------- CSV export: <button data-rp-export="#table-id" data-filename="monthly-revenue">
    function csvCell(text) {
        text = String(text).replace(/\s+/g, ' ').trim();
        return /[",\n]/.test(text) ? '"' + text.replace(/"/g, '""') + '"' : text;
    }
    function isoDate(d) {
        var m = d.getMonth() + 1, day = d.getDate();
        return d.getFullYear() + '-' + (m < 10 ? '0' : '') + m + '-' + (day < 10 ? '0' : '') + day;
    }

    document.addEventListener('click', function (e) {
        var exportBtn = e.target.closest('[data-rp-export]');
        if (exportBtn) {
            var table = document.querySelector(exportBtn.getAttribute('data-rp-export'));
            if (!table) return;
            var lines = [];
            table.querySelectorAll('tr').forEach(function (tr) {
                var cells = [];
                tr.querySelectorAll('th, td').forEach(function (cell) {
                    var raw = cell.getAttribute('data-csv');
                    cells.push(csvCell(raw !== null ? raw : cell.textContent));
                });
                lines.push(cells.join(','));
            });
            var blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
            var link = document.createElement('a');
            link.href = URL.createObjectURL(blob);
            link.download = (exportBtn.getAttribute('data-filename') || 'report') + '-' + isoDate(new Date()) + '.csv';
            document.body.appendChild(link);
            link.click();
            link.remove();
            setTimeout(function () { URL.revokeObjectURL(link.href); }, 1000);
            return;
        }

        if (e.target.closest('[data-rp-print]')) { window.print(); return; }

        // Quick date ranges: <button data-rp-range="last-month">
        var rangeBtn = e.target.closest('[data-rp-range]');
        if (rangeBtn) {
            var form = rangeBtn.closest('form');
            var now = new Date(), y = now.getFullYear(), m = now.getMonth();
            var from, to = now;
            switch (rangeBtn.getAttribute('data-rp-range')) {
                case 'this-month': from = new Date(y, m, 1); break;
                case 'last-month': from = new Date(y, m - 1, 1); to = new Date(y, m, 0); break;
                case '3-months': from = new Date(y, m - 2, 1); break;
                case '6-months': from = new Date(y, m - 5, 1); break;
                case 'this-year': from = new Date(y, 0, 1); break;
                default: return;
            }
            form.querySelector('[name="From"]').value = isoDate(from);
            form.querySelector('[name="To"]').value = isoDate(to);
            form.submit();
        }
    });

    // ---------- validation: "To" must not be earlier than "From" (works with jquery.validate.unobtrusive)
    if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
        jQuery.validator.addMethod('rpdaterange', function (value, element, fromId) {
            var from = document.getElementById(fromId);
            if (!value || !from || !from.value) return true;
            return from.value <= value;                       // yyyy-MM-dd strings sort like dates
        });
        jQuery.validator.unobtrusive.adapters.add('rpdaterange', ['from'], function (options) {
            options.rules['rpdaterange'] = options.params.from;
            options.messages['rpdaterange'] = options.message;
        });
        jQuery(function () {
            jQuery('.rp-filter [name="From"]').on('change', function () {
                var to = jQuery(this).closest('form').find('[name="To"]');
                if (to.val()) { to.valid(); }
            });
        });
    }

    return { stacked: stacked, hbar: hbar, doughnut: doughnut, waterfall: waterfall, trend: trend, revenueWithSwitch: revenueWithSwitch };
})();
