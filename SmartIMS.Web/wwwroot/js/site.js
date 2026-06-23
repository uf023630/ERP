// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
(function () {
  var actionLabels = {
    create: "新增",
    delete: "刪除",
    copy: "複製",
    edit: "修改",
    cancel: "取消",
    find: "尋找",
    print: "列印",
    first: "第一筆",
    previous: "上一筆",
    next: "下一筆",
    last: "最後一筆"
  };

  function initAppToolbar() {
    var toolbar = document.querySelector(".app-toolbar");
    var target = document.querySelector("[data-toolbar-page]");
    if (!toolbar || !target) {
      return;
    }

    var buttons = Array.prototype.slice.call(toolbar.querySelectorAll("[data-toolbar-action]"));
    var status = toolbar.querySelector(".app-toolbar-status");
    var permissions = toSet(toolbar.dataset.userPermissions);

    buttons.forEach(function (button) {
      button.addEventListener("click", function () {
        if (button.disabled) {
          return;
        }

        runToolbarAction(toolbar, target, button.dataset.toolbarAction, status);
        refreshToolbar(toolbar, target, permissions);
      });
    });

    document.addEventListener("smartims:toolbar-refresh", function () {
      refreshToolbar(toolbar, target, permissions);
    });

    refreshToolbar(toolbar, target, permissions);
  }

  function refreshToolbar(toolbar, target, permissions) {
    var supportedActions = toSet(target.dataset.toolbarActions);
    var pageCode = target.dataset.toolbarPage || "";

    toolbar.querySelectorAll("[data-toolbar-action]").forEach(function (button) {
      var action = button.dataset.toolbarAction;
      var permissionSuffix = button.dataset.permissionSuffix;
      var actionPermission = pageCode && permissionSuffix ? pageCode + "_" + permissionSuffix : "";
      var isSupported = supportedActions.has(action);
      var isAllowed = !actionPermission || permissions.has(actionPermission) || permissions.has(pageCode);

      button.disabled = !(isSupported && isAllowed);
    });
  }

  function runToolbarAction(toolbar, target, action, status) {
    var actionEvent = new CustomEvent("smartims:toolbar-action", {
      bubbles: true,
      cancelable: true,
      detail: {
        action: action,
        label: actionLabels[action] || action,
        pageCode: target.dataset.toolbarPage || "",
        target: target
      }
    });

    target.dispatchEvent(actionEvent);
    if (actionEvent.defaultPrevented) {
      setStatus(status, actionLabels[action] || action);
      return;
    }

    if (action === "print") {
      window.print();
      return;
    }

    if (action === "find") {
      focusSearchTarget(target, status);
      return;
    }

    if (["first", "previous", "next", "last"].indexOf(action) >= 0) {
      moveListSelection(target, action, status);
      return;
    }

    setStatus(status, (actionLabels[action] || action) + "尚未連接目前頁面");
  }

  function focusSearchTarget(target, status) {
    var input = target.querySelector('input[type="search"], input[name*="search" i], input[name*="keyword" i], input.form-control:not([readonly])');
    if (input) {
      input.focus();
      input.select();
      setStatus(status, "尋找");
    } else {
      setStatus(status, "目前頁面沒有可尋找欄位");
    }
  }

  function moveListSelection(target, action, status) {
    var rows = Array.prototype.slice.call(target.querySelectorAll(".listview-table tbody tr"));
    if (rows.length === 0) {
      setStatus(status, "目前頁面沒有資料列");
      return;
    }

    var selectedIndex = rows.findIndex(function (row) {
      return row.classList.contains("is-selected");
    });

    if (action === "first") {
      selectedIndex = 0;
    } else if (action === "last") {
      selectedIndex = rows.length - 1;
    } else if (action === "previous") {
      selectedIndex = selectedIndex <= 0 ? 0 : selectedIndex - 1;
    } else if (action === "next") {
      selectedIndex = selectedIndex < 0 ? 0 : Math.min(rows.length - 1, selectedIndex + 1);
    }

    rows.forEach(function (row, index) {
      row.classList.toggle("is-selected", index === selectedIndex);
    });

    rows[selectedIndex].scrollIntoView({ block: "nearest" });
    setStatus(status, (actionLabels[action] || action) + " " + (selectedIndex + 1) + "/" + rows.length);
  }

  function setStatus(status, message) {
    if (status) {
      status.textContent = message;
    }
  }

  function toSet(value) {
    return new Set(String(value || "")
      .split(/[,\s]+/)
      .map(function (item) { return item.trim(); })
      .filter(Boolean));
  }

  document.addEventListener("DOMContentLoaded", initAppToolbar);
})();
