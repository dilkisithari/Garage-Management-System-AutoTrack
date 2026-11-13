// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Shared scripts for every page
(function () {
    // Show / hide password buttons: <button data-toggle-password>
    document.querySelectorAll('[data-toggle-password]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var input = btn.closest('.input-group').querySelector('input');
            var icon = btn.querySelector('i');
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            if (icon) icon.className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
        });
    });

    // Success alerts disappear after 5 seconds
    document.querySelectorAll('.alert-success.dt-alert').forEach(function (el) {
        setTimeout(function () {
            if (window.bootstrap) { bootstrap.Alert.getOrCreateInstance(el).close(); }
        }, 5000);
    });
})();
