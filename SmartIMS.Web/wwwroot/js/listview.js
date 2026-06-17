(function () {
  function initListView(component) {
    var toggle = component.querySelector(".listview-settings-toggle");
    var panel = component.querySelector(".listview-settings-panel");
    var resetButton = component.querySelector(".listview-reset");
    var options = Array.prototype.slice.call(component.querySelectorAll(".listview-column-option"));
    var headers = Array.prototype.slice.call(component.querySelectorAll("th[data-col-key]"));

    if (!toggle || !panel) {
      return;
    }

    toggle.addEventListener("click", function () {
      var isHidden = panel.hasAttribute("hidden");
      panel.toggleAttribute("hidden", !isHidden);
      toggle.setAttribute("aria-expanded", isHidden ? "true" : "false");
    });

    options.forEach(function (option) {
      var checkbox = option.querySelector(".listview-column-visible");
      if (checkbox) {
        checkbox.addEventListener("change", function () {
          setColumnVisible(component, option.dataset.colKey, checkbox.checked);
          saveSettings(component);
        });
      }

      option.addEventListener("dragstart", function (event) {
        event.dataTransfer.setData("text/plain", option.dataset.colKey);
        event.dataTransfer.effectAllowed = "move";
        option.classList.add("is-dragging");
      });

      option.addEventListener("dragend", function () {
        option.classList.remove("is-dragging");
      });

      option.addEventListener("dragover", function (event) {
        event.preventDefault();
      });

      option.addEventListener("drop", function (event) {
        event.preventDefault();
        var sourceKey = event.dataTransfer.getData("text/plain");
        moveColumn(component, sourceKey, option.dataset.colKey);
        saveSettings(component);
      });
    });

    headers.forEach(function (header) {
      header.addEventListener("dragstart", function (event) {
        if (event.target.classList.contains("listview-resize-handle")) {
          event.preventDefault();
          return;
        }

        event.dataTransfer.setData("text/plain", header.dataset.colKey);
        event.dataTransfer.effectAllowed = "move";
      });

      header.addEventListener("dragover", function (event) {
        event.preventDefault();
      });

      header.addEventListener("drop", function (event) {
        event.preventDefault();
        var sourceKey = event.dataTransfer.getData("text/plain");
        moveColumn(component, sourceKey, header.dataset.colKey);
        saveSettings(component);
      });
    });

    component.querySelectorAll(".listview-resize-handle").forEach(function (handle) {
      handle.addEventListener("mousedown", function (event) {
        startResize(component, handle.closest("th"), event);
      });
    });

    if (resetButton) {
      resetButton.addEventListener("click", function () {
        resetSettings(component);
      });
    }
  }

  function setColumnVisible(component, key, visible) {
    component.querySelectorAll('[data-col-key="' + cssEscape(key) + '"]').forEach(function (cell) {
      if (cell.classList.contains("listview-column-option")) {
        return;
      }

      cell.classList.toggle("is-hidden", !visible);
    });
  }

  function moveColumn(component, sourceKey, targetKey) {
    if (!sourceKey || !targetKey || sourceKey === targetKey) {
      return;
    }

    moveOption(component, sourceKey, targetKey);

    var rows = component.querySelectorAll(".listview-table tr");
    rows.forEach(function (row) {
      var source = row.querySelector('[data-col-key="' + cssEscape(sourceKey) + '"]');
      var target = row.querySelector('[data-col-key="' + cssEscape(targetKey) + '"]');
      if (source && target && source !== target) {
        row.insertBefore(source, target);
      }
    });
  }

  function moveOption(component, sourceKey, targetKey) {
    var source = component.querySelector('.listview-column-option[data-col-key="' + cssEscape(sourceKey) + '"]');
    var target = component.querySelector('.listview-column-option[data-col-key="' + cssEscape(targetKey) + '"]');
    if (source && target && source !== target) {
      target.parentNode.insertBefore(source, target);
    }
  }

  function startResize(component, header, event) {
    if (!header) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    var startX = event.clientX;
    var startWidth = header.offsetWidth;
    var minWidth = parseInt(header.style.minWidth, 10) || 80;
    var key = header.dataset.colKey;

    function onMouseMove(moveEvent) {
      var nextWidth = Math.max(minWidth, startWidth + moveEvent.clientX - startX);
      setColumnWidth(component, key, nextWidth);
    }

    function onMouseUp() {
      document.removeEventListener("mousemove", onMouseMove);
      document.removeEventListener("mouseup", onMouseUp);
      saveSettings(component);
    }

    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);
  }

  function setColumnWidth(component, key, width) {
    component.querySelectorAll('[data-col-key="' + cssEscape(key) + '"]').forEach(function (cell) {
      if (cell.classList.contains("listview-column-option")) {
        cell.dataset.width = String(Math.round(width));
        return;
      }

      cell.style.width = Math.round(width) + "px";
    });
  }

  function collectSettings(component) {
    return Array.prototype.slice.call(component.querySelectorAll(".listview-column-option")).map(function (option, index) {
      var key = option.dataset.colKey;
      var checkbox = option.querySelector(".listview-column-visible");
      var header = component.querySelector('th[data-col-key="' + cssEscape(key) + '"]');
      var width = header ? header.offsetWidth : parseInt(option.dataset.width, 10);

      return {
        key: key,
        visible: checkbox ? checkbox.checked : true,
        width: Math.max(80, Math.round(width || parseInt(option.dataset.defaultWidth, 10) || 140)),
        order: index
      };
    });
  }

  function saveSettings(component) {
    var token = getAntiForgeryToken(component);
    return fetch("/ListViewSettings/Save", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "RequestVerificationToken": token
      },
      body: JSON.stringify({
        listKey: component.dataset.listviewKey,
        columns: collectSettings(component)
      })
    }).catch(function () {
      showStatus(component, "欄位設定儲存失敗");
    });
  }

  function resetSettings(component) {
    var token = getAntiForgeryToken(component);
    fetch("/ListViewSettings/Reset", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "RequestVerificationToken": token
      },
      body: JSON.stringify({
        listKey: component.dataset.listviewKey
      })
    }).then(function (response) {
      if (response.ok) {
        window.location.reload();
      } else {
        showStatus(component, "欄位設定重設失敗");
      }
    }).catch(function () {
      showStatus(component, "欄位設定重設失敗");
    });
  }

  function getAntiForgeryToken(component) {
    var token = component.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : "";
  }

  function showStatus(component, message) {
    var status = component.querySelector(".listview-status");
    if (!status) {
      status = document.createElement("div");
      status.className = "listview-status";
      component.prepend(status);
    }

    status.textContent = message;
  }

  function cssEscape(value) {
    if (window.CSS && CSS.escape) {
      return CSS.escape(value);
    }

    return String(value).replace(/"/g, '\\"');
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".listview-component").forEach(initListView);
  });
})();
