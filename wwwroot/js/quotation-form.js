// Quotation Builder: Dynamic rows & live calculation of Subtotals, Discount, VAT, and Grand Total
(function () {
    var form = document.getElementById('quotation-form');
    if (!form) return;

    function money(n) {
        return 'LKR ' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function num(val) {
        var n = parseFloat(val);
        return isNaN(n) ? 0 : n;
    }

    function reindex(tbody) {
        tbody.querySelectorAll('tr.quote-row').forEach(function (tr, index) {
            tr.querySelectorAll('[name]').forEach(function (el) {
                el.name = el.name.replace(/^([A-Za-z]+)\[[^\]]*\]/, '$1[' + index + ']');
            });
        });
    }

    function recalc() {
        var partsTotal = 0;
        form.querySelectorAll('[data-rows="parts"] tr.quote-row').forEach(function (tr) {
            var qty = num(tr.querySelector('.js-qty').value);
            var price = num(tr.querySelector('.js-price').value);
            var line = qty * price;
            tr.querySelector('.js-line-total').textContent = line.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            partsTotal += line;
        });

        var labourTotal = 0;
        form.querySelectorAll('[data-rows="labour"] tr.quote-row').forEach(function (tr) {
            var hours = num(tr.querySelector('.js-hours').value);
            var rate = num(tr.querySelector('.js-rate').value);
            var line = hours * rate;
            tr.querySelector('.js-line-total').textContent = line.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            labourTotal += line;
        });

        var netSubtotal = partsTotal + labourTotal;
        var discountPct = num(document.getElementById('input-discount').value);
        if (discountPct < 0) discountPct = 0;
        if (discountPct > 100) discountPct = 100;

        var discountAmt = netSubtotal * (discountPct / 100);
        var afterDiscount = netSubtotal - discountAmt;
        var vatChecked = document.getElementById('input-vat').checked;
        var vatAmt = vatChecked ? afterDiscount * 0.18 : 0;
        var grandTotal = afterDiscount + vatAmt;

        document.getElementById('display-parts-subtotal').textContent = money(partsTotal);
        document.getElementById('display-labour-subtotal').textContent = money(labourTotal);
        document.getElementById('display-net-subtotal').textContent = money(netSubtotal);
        document.getElementById('display-discount-amount').textContent = '- ' + money(discountAmt);
        document.getElementById('display-vat-amount').textContent = money(vatAmt);
        document.getElementById('display-grand-total').textContent = money(grandTotal);
    }

    function addRow(section) {
        var tbody = form.querySelector('[data-rows="' + section + '"]');
        var template = document.getElementById('tpl-' + section);
        var count = tbody.querySelectorAll('tr.quote-row').length;
        var html = template.innerHTML.split('__INDEX__').join(count);
        tbody.insertAdjacentHTML('beforeend', html);
        reindex(tbody);
        recalc();
    }

    // Event Delegation
    form.addEventListener('input', function (e) {
        if (e.target.matches('.js-qty, .js-price, .js-hours, .js-rate, #input-discount')) recalc();
    });

    form.addEventListener('change', function (e) {
        if (e.target.id === 'input-vat') recalc();
    });

    form.addEventListener('click', function (e) {
        var btnAdd = e.target.closest('[data-add-row]');
        if (btnAdd) {
            e.preventDefault();
            addRow(btnAdd.dataset.addRow);
            return;
        }

        var btnDel = e.target.closest('[data-remove-row]');
        if (btnDel) {
            e.preventDefault();
            var tr = btnDel.closest('tr');
            var tbody = tr.parentNode;
            tr.remove();
            reindex(tbody);
            recalc();
        }
    });

    recalc();
})();