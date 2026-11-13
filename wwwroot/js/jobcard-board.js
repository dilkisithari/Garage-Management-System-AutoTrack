// Job board: filter cards by mechanic and search text (page 24)
(function () {
    var mechanicSelect = document.getElementById('filter-mechanic');
    var searchBox = document.getElementById('filter-search');
    var clearBtn = document.getElementById('filter-clear');
    if (!mechanicSelect || !searchBox) return;

    function applyFilters() {
        var mechanic = mechanicSelect.value.toLowerCase();
        var text = searchBox.value.trim().toLowerCase();

        document.querySelectorAll('.job-card').forEach(function (card) {
            var okMechanic = !mechanic || card.dataset.mechanic.toLowerCase() === mechanic;
            var okText = !text || card.dataset.search.indexOf(text) !== -1;
            card.classList.toggle('d-none', !(okMechanic && okText));
        });

        // Update the count badge and the empty message of each column
        document.querySelectorAll('.dt-board-col').forEach(function (col) {
            var visible = col.querySelectorAll('.job-card:not(.d-none)').length;
            col.querySelector('.col-count').textContent = visible;
            col.querySelector('.col-empty').classList.toggle('d-none', visible > 0);
        });
    }

    mechanicSelect.addEventListener('change', applyFilters);
    searchBox.addEventListener('input', applyFilters);
    clearBtn.addEventListener('click', function () {
        mechanicSelect.value = '';
        searchBox.value = '';
        applyFilters();
    });
})();
