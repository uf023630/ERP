(function ($) {
    "use strict";
    var names = {
        ProductName: "商品名稱", ProductCode: "商品編號", Quantity: "數量", UnitPrice: "單價",
        Thickness: "厚度", Width: "寬度", Length: "長度", Specification: "規格", Unit: "單位",
        Remark: "備註", CustomerId: "客戶", TaxRate: "稅率", TaxType: "稅別",
        QuoteDate: "報價日期", ExpireDate: "有效日期"
    };
    function fieldLabel(field) {
        var key = field.dataset.field || (field.name || "").split(".").pop();
        var label = field.labels && field.labels[0];
        var text = names[key] || (label && label.textContent.trim()) || field.getAttribute("aria-label") || "此欄位";
        var row = field.closest("[data-line]");
        var number = row && row.querySelector("[data-line-number]");
        return number ? "第 " + number.textContent.trim() + " 項商品的" + text : text;
    }
    window.smartimsValidationFieldLabel = fieldLabel;
    $.extend($.validator.messages, {
        required: "此欄位為必填。", remote: "請檢查輸入內容。",
        email: "請輸入有效的電子郵件地址。", url: "請輸入有效的網址。",
        date: "請輸入有效的日期。", dateISO: "請輸入有效的日期（年－月－日）。",
        number: "請輸入有效的數值。", digits: "請輸入整數。",
        creditcard: "請輸入有效的信用卡號碼。", equalTo: "兩次輸入的內容必須相同。",
        maxlength: $.validator.format("最多可輸入 {0} 個字元。"),
        minlength: $.validator.format("至少須輸入 {0} 個字元。"),
        rangelength: $.validator.format("請輸入 {0} 至 {1} 個字元。"),
        range: $.validator.format("請輸入介於 {0} 至 {1} 的數值。"),
        max: $.validator.format("請輸入小於或等於 {0} 的數值。"),
        min: $.validator.format("請輸入大於或等於 {0} 的數值。"),
        step: $.validator.format("請輸入 {0} 的整數倍。")
    });
    var defaultMessage = $.validator.prototype.defaultMessage;
    $.validator.prototype.defaultMessage = function (field, rule) {
        var message = defaultMessage.call(this, field, rule);
        if (typeof message === "string" && !/[\u3400-\u9fff]/.test(message)) {
            var fallback = $.validator.messages[rule.method];
            message = typeof fallback === "function" ? fallback(rule.parameters) : (fallback || "輸入格式不正確。");
        }
        return fieldLabel(field) + "：" + message;
    };

    function clearNativeMessage(event) {
        var field = event.target;
        if (field.dataset && field.dataset.localizedValidity) {
            field.setCustomValidity("");
            delete field.dataset.localizedValidity;
        }
    }
    document.addEventListener("input", clearNativeMessage, true);
    document.addEventListener("change", clearNativeMessage, true);
    document.addEventListener("invalid", function (event) {
        var field = event.target;
        var v = field.validity;
        if (!v || v.customError) return;
        var message = "輸入格式不正確。";
        if (v.valueMissing) message = "此欄位為必填。";
        else if (v.badInput) message = "請輸入有效的數值。";
        else if (v.typeMismatch) message = field.type === "email" ? "請輸入有效的電子郵件地址。" : "請輸入有效的網址。";
        else if (v.rangeUnderflow) message = "請輸入大於或等於 " + field.min + " 的數值。";
        else if (v.rangeOverflow) message = "請輸入小於或等於 " + field.max + " 的數值。";
        else if (v.stepMismatch) message = "請依欄位允許的精度輸入數值。";
        else if (v.tooLong) message = "最多可輸入 " + field.maxLength + " 個字元。";
        else if (v.tooShort) message = "至少須輸入 " + field.minLength + " 個字元。";
        field.setCustomValidity(fieldLabel(field) + "：" + message);
        field.dataset.localizedValidity = "true";
    }, true);
})(jQuery);
