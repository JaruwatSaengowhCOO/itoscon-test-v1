(function () {
    var sw = document.getElementById("langSwitch");
    var culture = document.getElementById("langCulture");
    var form = document.getElementById("langForm");

    if (!sw || !culture || !form) return;

    sw.addEventListener("change", function () {
        culture.value = sw.checked ? "en-US" : "th-TH";
        form.submit();
    });
})();