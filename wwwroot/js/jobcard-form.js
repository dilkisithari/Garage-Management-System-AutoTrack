// Job card form: add / remove rows and live totals (page 25)
(function () {
    var form = document.getElementById('jobcard-form');
    if (!form) return;

    function money(n) {
        return 'LKR ' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function num(value) {
        var n = parseFloat(value);
        return isNaN(n) ? 0 : n;
    }

    function body(section) {
        return form.querySelector('[data-rows="' + section + '"]');
    }

    // Rename Parts[3].Quantity -> Parts[0].Quantity so ASP.NET can bind a clean list
    function reindex(tbody) {
        tbody.querySelectorAll('tr.jc-row').forEach(function (tr, index) {
            tr.querySelectorAll('[name]').forEach(function (el) {
                el.name = el.name.replace(/^([A-Za-z]+)\[[^\]]*\]/, '$1[' + index + ']');
            });
        });
    }

    function toggleEmpty(tbody) {
        var empty = tbody.querySelector('.jc-empty');
        if (empty) empty.classList.toggle('d-none', tbody.querySelectorAll('tr.jc-row').length > 0);
    }

    // Sum one table (parts or labour): qty * price per row
    function sumRows(section) {
        var total = 0;
        body(section).querySelectorAll('tr.jc-row').forEach(function (tr) {
            var qty = num(tr.querySelector('.js-qty').value);
            var price = num(tr.querySelector('.js-price').value);
            var line = qty * price;
            tr.querySelector('.js-line-total').textContent =
                line.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            total += line;
        });
        return total;
    }

    function recalc() {
        var parts = sumRows('part');
        var labour = sumRows('labour');
        document.getElementById('total-parts').textContent = money(parts);
        document.getElementById('total-labour').textContent = money(labour);
        document.getElementById('total-grand').textContent = money(parts + labour);
    }

    function addRow(section) {
        var tbody = body(section);
        var template = document.getElementById('tpl-' + section);
        var count = tbody.querySelectorAll('tr.jc-row').length;
        var html = template.innerHTML.split('__INDEX__').join(count);

        var empty = tbody.querySelector('.jc-empty');
        if (empty) {
            empty.insertAdjacentHTML('beforebegin', html);
        } else {
            tbody.insertAdjacentHTML('beforeend', html);
        }

        toggleEmpty(tbody);
        recalc();

        var rows = tbody.querySelectorAll('tr.jc-row');
        var firstInput = rows[rows.length - 1].querySelector('input[type="text"]');
        if (firstInput) firstInput.focus();
    }

    // Add buttons
    form.querySelectorAll('[data-add-row]').forEach(function (btn) {
        btn.addEventListener('click', function () { addRow(btn.dataset.addRow); });
    });

    // Remove buttons (works for rows added later too)
    form.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-remove-row');
        if (!btn) return;
        var tbody = btn.closest('tbody');
        btn.closest('tr').remove();
        reindex(tbody);
        toggleEmpty(tbody);
        recalc();
    });

    // Recalculate while typing
    form.addEventListener('input', function (e) {
        if (e.target.matches('.js-qty, .js-price')) recalc();
    });

    // Upper-case the vehicle number as it is typed
    var vehicle = form.querySelector('[name="VehicleNo"]');
    if (vehicle) {
        vehicle.addEventListener('input', function () {
            var pos = vehicle.selectionStart;
            vehicle.value = vehicle.value.toUpperCase();
            vehicle.setSelectionRange(pos, pos);
        });
    }

    // Before submit: tidy the indexes of all tables
    form.addEventListener('submit', function () {
        ['task', 'part', 'labour'].forEach(function (section) { reindex(body(section)); });
    });

    // First paint
    ['task', 'part', 'labour'].forEach(function (section) { toggleEmpty(body(section)); });
    recalc();
})();
