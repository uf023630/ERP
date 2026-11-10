(function () {
  var root = document.querySelector("[data-customer-layout-test]");
  if (!root) return;

  var customers = JSON.parse(document.getElementById("customerData").textContent);
  var form = document.getElementById("customerForm");
  var search = document.getElementById("customerSearch");
  var rows = document.getElementById("customerRows");
  var panel = root.querySelector("[data-customer-detail]");
  var badge = root.querySelector("[data-customer-mode]");
  var hint = root.querySelector("[data-customer-mode-hint]");
  var feedback = root.querySelector("[data-customer-form-status]");
  var listStatus = root.querySelector("[data-customer-status]");
  var saveButton = root.querySelector("[data-customer-save]");
  var cancelButton = root.querySelector("[data-customer-cancel]");
  var canCreate = root.dataset.canCreate === "true";
  var canEdit = root.dataset.canEdit === "true";
  var fields = ["customerName", "contact", "phone", "fax", "email", "line", "taxId", "zipCode", "deliveryAddress", "paymentMethod", "bankAccount", "note"];
  var selectedIndex = -1;
  var returnIndex = -1;
  var filteredIndexes = [];
  var mode = "empty";
  var baseline = "";
  var busy = false;

  function collectForm() {
    var customer = {};
    fields.forEach(function (field) { customer[field] = form.elements[field].value.trim(); });
    return customer;
  }

  function fillForm(customer) {
    fields.forEach(function (field) { form.elements[field].value = customer[field] || ""; });
    form.elements.customerCode.value = customer.customerCode || "";
    baseline = JSON.stringify(collectForm());
  }

  function isDirty() {
    if (mode === "new" && form.elements.customerName.value.trim()) return true;
    return mode !== "empty" && JSON.stringify(collectForm()) !== baseline;
  }

  function discardAllowed() {
    return !isDirty() || window.confirm("目前有尚未存檔的內容，確定要放棄並切換嗎？");
  }

  function setFeedback(message, error) {
    feedback.textContent = message;
    feedback.classList.toggle("text-danger", !!error);
    feedback.classList.toggle("text-success", !!message && !error);
  }

  function refreshMode() {
    panel.dataset.mode = mode;
    var writable = mode === "new" ? canCreate : mode === "edit" && canEdit;
    fields.forEach(function (field) { form.elements[field].disabled = busy || !writable; });
    if (saveButton) {
      saveButton.disabled = busy || !writable;
      saveButton.textContent = busy ? "處理中…" : "存檔";
    }
    cancelButton.disabled = busy || mode === "empty";
    search.disabled = busy;
    if (mode === "new") {
      badge.textContent = "新增客戶｜尚未存檔";
      hint.textContent = "目前正在新增客戶。按「存檔」後，才會建立一筆新的客戶資料。";
    } else if (mode === "edit") {
      badge.textContent = canEdit ? "編輯既有客戶" : "檢視既有客戶";
      hint.textContent = "清單選取：" + customers[selectedIndex].customerName +
        (isDirty() ? "｜有尚未存檔的修改。" : canEdit ? "｜修改後按「存檔」更新此客戶。" : "｜目前為唯讀檢視。");
    } else {
      badge.textContent = "尚未選取客戶";
      hint.textContent = "請點選下方客戶，或按上方「新增」建立新客戶。";
    }
  }

  function renderRows() {
    rows.innerHTML = "";
    filteredIndexes.forEach(function (index) {
      var customer = customers[index];
      var row = document.createElement("tr");
      row.className = mode === "edit" && index === selectedIndex ? "is-selected" : "";
      row.dataset.customerIndex = String(index);
      ["customerCode", "customerName", "contact", "phone", "email", "line", "taxId", "deliveryAddress", "paymentMethod"].forEach(function (field) {
        var cell = document.createElement("td");
        cell.textContent = customer[field] || "";
        row.appendChild(cell);
      });
      row.addEventListener("click", function () { selectCustomer(index); });
      rows.appendChild(row);
    });
    listStatus.textContent = "共 " + filteredIndexes.length + " 筆";
  }

  function applyFilter() {
    var keyword = search.value.trim().toLowerCase();
    filteredIndexes = customers.map(function (_, index) { return index; }).filter(function (index) {
      return !keyword || ["customerCode"].concat(fields).some(function (field) {
        return String(customers[index][field] || "").toLowerCase().includes(keyword);
      });
    });
    // Filtering changes only the list; it must not replace an unsaved form.
    renderRows();
  }

  function selectCustomer(index, force) {
    if (busy || index < 0 || index >= customers.length) return;
    if (!force && mode === "edit" && selectedIndex === index) return;
    if (!force && !discardAllowed()) return;
    selectedIndex = index;
    mode = "edit";
    fillForm(customers[index]);
    setFeedback("");
    refreshMode();
    renderRows();
  }

  function createCustomer(copy) {
    if (!canCreate) { setFeedback("您沒有新增客戶的權限。", true); return; }
    if (copy && mode !== "edit") { setFeedback("請先選取要複製的客戶。", true); return; }
    if (!discardAllowed()) return;
    var source = copy ? Object.assign({}, customers[selectedIndex], {
      customerName: customers[selectedIndex].customerName + " 複本", taxId: "", customerCode: ""
    }) : { paymentMethod: "月結" };
    if (mode === "edit") returnIndex = selectedIndex;
    selectedIndex = -1;
    mode = "new";
    fillForm(source);
    setFeedback(copy ? "已帶入複本，存檔後會建立新客戶。" : "");
    refreshMode();
    renderRows();
    form.elements.customerName.focus();
  }

  function cancelEdit() {
    if (busy) return;
    if (mode === "edit") {
      fillForm(customers[selectedIndex]);
    } else {
      var index = returnIndex >= 0 && returnIndex < customers.length ? returnIndex : filteredIndexes[0];
      if (index !== undefined) selectCustomer(index, true);
      else { mode = "empty"; selectedIndex = -1; fillForm({}); }
    }
    setFeedback("已取消，尚未存檔的內容未寫入資料庫。");
    refreshMode();
    renderRows();
  }

  async function postCustomer(url, payload) {
    var response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json", "RequestVerificationToken": form.querySelector('[name="__RequestVerificationToken"]').value },
      body: JSON.stringify(payload)
    });
    var data = await response.json().catch(function () { return {}; });
    if (!response.ok || response.redirected) throw new Error(data.message || "操作失敗，請確認登入狀態與權限。");
    return data;
  }

  async function saveCustomer() {
    if (busy || mode === "empty") return;
    if (!(mode === "new" ? canCreate : canEdit)) { setFeedback("您沒有存檔所需的權限。", true); return; }
    if (!form.reportValidity()) return;
    var customer = collectForm();
    var creating = mode === "new";
    customer.customerId = creating ? null : customers[selectedIndex].customerId;
    busy = true;
    setFeedback("正在存檔…");
    refreshMode();
    try {
      customer = await postCustomer(root.dataset.saveUrl, customer);
      if (!customer.customerId) throw new Error("未收到存檔結果，請重新確認登入狀態。");
      if (creating) { customers.push(customer); selectedIndex = customers.length - 1; }
      else customers[selectedIndex] = customer;
      mode = "edit";
      fillForm(customer);
      applyFilter();
      if (!filteredIndexes.includes(selectedIndex)) { search.value = ""; applyFilter(); }
      setFeedback(creating ? "新客戶已存檔。" : "客戶修改已存檔。");
    } catch (error) {
      setFeedback(error.message || "無法連線至伺服器，請稍後再試。", true);
    } finally { busy = false; refreshMode(); }
  }

  async function deleteCustomer() {
    if (busy) return;
    if (mode !== "edit") { setFeedback("請先從清單選取要刪除的客戶。", true); return; }
    if (!window.confirm("確定要刪除「" + customers[selectedIndex].customerName + "」？歷史單據會保留，未存檔的修改將放棄。")) return;
    busy = true;
    refreshMode();
    try {
      await postCustomer(root.dataset.deleteUrl, { customerId: customers[selectedIndex].customerId });
      customers.splice(selectedIndex, 1);
      selectedIndex = -1;
      returnIndex = -1;
      mode = "empty";
      fillForm({});
      applyFilter();
      busy = false;
      if (filteredIndexes.length) selectCustomer(filteredIndexes[0], true);
      setFeedback("客戶已刪除。");
    } catch (error) { setFeedback(error.message || "無法連線至伺服器，請稍後再試。", true); }
    finally { busy = false; refreshMode(); }
  }

  function moveSelection(action) {
    if (!filteredIndexes.length) { setFeedback("沒有符合條件的客戶。", true); return; }
    var position = filteredIndexes.indexOf(selectedIndex);
    if (action === "first") position = 0;
    else if (action === "last") position = filteredIndexes.length - 1;
    else if (position < 0) position = 0;
    else if (action === "previous") position = Math.max(0, position - 1);
    else position = Math.min(filteredIndexes.length - 1, position + 1);
    selectCustomer(filteredIndexes[position]);
  }

  document.querySelector(".content-frame").addEventListener("smartims:toolbar-action", function (event) {
    var action = event.detail.action;
    if (!["create", "edit", "copy", "cancel", "delete", "find", "first", "previous", "next", "last"].includes(action)) return;
    event.preventDefault();
    if (busy) return;
    if (action === "create") createCustomer(false);
    else if (action === "copy") createCustomer(true);
    else if (action === "edit") saveCustomer();
    else if (action === "cancel") cancelEdit();
    else if (action === "delete") deleteCustomer();
    else if (action === "find") { search.focus(); search.select(); }
    else moveSelection(action);
  });
  form.addEventListener("submit", function (event) { event.preventDefault(); saveCustomer(); });
  form.addEventListener("input", function () { setFeedback(""); refreshMode(); });
  form.addEventListener("change", refreshMode);
  cancelButton.addEventListener("click", cancelEdit);
  search.addEventListener("input", function () { if (!busy) applyFilter(); });
  fillForm({});
  applyFilter();
  if (customers.length) selectCustomer(0, true);
  else refreshMode();
})();
