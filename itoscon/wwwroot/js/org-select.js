(function () {
    var division = document.getElementById("DivisionId");
    var department = document.getElementById("DepartmentId");
    var team = document.getElementById("TeamId");

    if (!division || !department || !team) return;

    // แสดงเฉพาะตัวเลือกที่อยู่ใต้ค่าแม่ที่เลือก ถ้าค่าเดิมไม่อยู่ในกลุ่มนั้นให้ล้างค่า
    function filter(select, parentValue) {
        var keep = select.value;
        var keepValid = false;

        for (var i = 0; i < select.options.length; i++) {
            var opt = select.options[i];
            if (!opt.hasAttribute("data-parent")) continue; // ข้ามตัวเลือก "— เลือก… —"

            var show = parentValue !== "" && opt.getAttribute("data-parent") === parentValue;
            opt.hidden = !show;
            opt.disabled = !show;

            if (show && opt.value === keep) keepValid = true;
        }

        if (!keepValid) select.value = "";
    }

    division.addEventListener("change", function () {
        filter(department, division.value);
        filter(team, department.value);
    });

    department.addEventListener("change", function () {
        filter(team, department.value);
    });

    // ตอนโหลดหน้า ให้กรองตามค่าที่เลือกไว้ (กรณีแก้ไข)
    filter(department, division.value);
    filter(team, department.value);
})();