(() => {
  "use strict";

  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const state = {
    token: sessionStorage.getItem("taskmate.accessToken"),
    user: null,
    authMode: "login",
    view: "all",
    page: 1,
    pageSize: 20,
    totalPages: 0,
    totalCount: 0,
    searchTimer: null,
    loadId: 0,
    toastTimer: null,
  };

  const labels = {
    status: { ToDo: "To do", InProgress: "In progress", Completed: "Done" },
    category: { Assignment: "Assignment", Exam: "Exam", Project: "Project", Personal: "Personal" },
    priority: { High: "High", Medium: "Medium", Low: "Low" },
  };

  function localDateString(date = new Date()) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  function displayDate(dateString, options = { month: "short", day: "numeric" }) {
    if (!dateString) return "No due date";
    const [year, month, day] = dateString.split("-").map(Number);
    return new Intl.DateTimeFormat(undefined, options).format(new Date(year, month - 1, day));
  }

  function setMessage(element, message = "") {
    element.textContent = message;
    element.hidden = !message;
  }

  function showToast(message, isError = false) {
    const toast = $("#toast");
    toast.textContent = message;
    toast.classList.toggle("error", isError);
    toast.classList.add("show");
    clearTimeout(state.toastTimer);
    state.toastTimer = setTimeout(() => toast.classList.remove("show"), 3200);
  }

  async function api(path, options = {}) {
    const headers = new Headers(options.headers || {});
    if (options.body !== undefined) headers.set("Content-Type", "application/json");
    if (state.token) headers.set("Authorization", `Bearer ${state.token}`);

    let response;
    try {
      response = await fetch(path, { ...options, headers });
    } catch {
      throw new Error("Can’t reach TaskMate right now. Check your connection and try again.");
    }

    if (response.status === 401 && state.token) {
      signOut(false);
      throw new Error("Your session has ended. Please sign in again.");
    }
    if (response.status === 204) return null;

    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("application/json") ? await response.json() : null;
    if (!response.ok) {
      const validation = payload?.errors && Object.values(payload.errors).flat().join(" ");
      throw new Error(validation || payload?.detail || payload?.title || "Something went wrong. Please try again.");
    }
    return payload;
  }

  function showAuth() {
    $("#auth-view").classList.remove("hidden");
    $("#app-view").classList.add("hidden");
    $("#auth-email").value = "";
    $("#auth-password").value = "";
    $("#auth-name").value = "";
    setMessage($("#auth-message"));
    setAuthMode("login");
  }

  function signOut(showNotice = true) {
    state.token = null;
    state.user = null;
    sessionStorage.removeItem("taskmate.accessToken");
    showAuth();
    if (showNotice) showToast("You’re signed out. See you soon.");
  }

  function setAuthMode(mode) {
    state.authMode = mode;
    const registering = mode === "register";
    $("#login-tab").classList.toggle("active", !registering);
    $("#register-tab").classList.toggle("active", registering);
    $("#login-tab").setAttribute("aria-selected", String(!registering));
    $("#register-tab").setAttribute("aria-selected", String(registering));
    $("#name-field").classList.toggle("hidden", !registering);
    $("#auth-name").required = registering;
    $("#auth-password").setAttribute("autocomplete", registering ? "new-password" : "current-password");
    $("#auth-eyebrow").textContent = registering ? "A FRESH START" : "WELCOME BACK";
    $("#auth-title").textContent = registering ? "Let’s get you organized." : "Good to see you.";
    $("#auth-subtitle").textContent = registering
      ? "Create your free account and make space for what matters."
      : "Sign in and pick up right where you left off.";
    $("#password-hint").classList.toggle("hidden", !registering);
    $("#auth-submit").querySelector("span:first-child").textContent = registering ? "Create my account" : "Sign in";
    setMessage($("#auth-message"));
  }

  async function completeSignIn(payload) {
    state.token = payload.accessToken;
    state.user = payload.user;
    sessionStorage.setItem("taskmate.accessToken", payload.accessToken);
    $("#auth-view").classList.add("hidden");
    $("#app-view").classList.remove("hidden");
    $("#profile-name").textContent = payload.user.name;
    $("#profile-email").textContent = payload.user.email;
    $("#avatar").textContent = [...payload.user.name.trim()][0]?.toUpperCase() || "S";
    setWelcome();
    await refreshDashboard();
    await loadTasks();
  }

  function setWelcome() {
    const now = new Date();
    const hour = now.getHours();
    const greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
    const firstName = state.user?.name?.trim().split(/\s+/)[0] || "there";
    $("#welcome-date").textContent = new Intl.DateTimeFormat(undefined, { weekday: "long", month: "long", day: "numeric" }).format(now).toUpperCase();
    $("#today-label").textContent = new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(now);
    $("#welcome-title").textContent = `${greeting}, ${firstName}.`;
    $("#welcome-copy").textContent = hour < 11 ? "Let’s make today feel a little more manageable." : "Here’s what’s on your plate. You’ve got this.";
  }

  async function refreshDashboard() {
    const stats = await api(`/api/dashboard?today=${encodeURIComponent(localDateString())}`);
    $("#stat-total").textContent = stats.totalTasks;
    $("#stat-completed").textContent = stats.completedTasks;
    $("#stat-upcoming").textContent = stats.upcomingDeadlines;
    $("#stat-overdue").textContent = stats.overdueTasks;
    $("#completion-percent").textContent = `${stats.completionPercent}%`;
    $("#nav-total").textContent = stats.totalTasks;
    $("#nav-upcoming").textContent = stats.upcomingDeadlines;
  }

  function createSvg(pathMarkup, viewBox = "0 0 24 24") {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", viewBox);
    svg.setAttribute("aria-hidden", "true");
    svg.innerHTML = pathMarkup;
    return svg;
  }

  function createTaskRow(task) {
    const row = document.createElement("article");
    row.className = `task-row${task.status === "Completed" ? " is-complete" : ""}`;
    row.dataset.id = task.id;

    const check = document.createElement("button");
    check.className = `task-check${task.status === "Completed" ? " checked" : ""}`;
    check.type = "button";
    check.setAttribute("aria-label", task.status === "Completed" ? `Mark ${task.title} incomplete` : `Mark ${task.title} complete`);
    check.setAttribute("aria-pressed", String(task.status === "Completed"));
    check.addEventListener("click", () => updateTask(task, { status: task.status === "Completed" ? "ToDo" : "Completed" }));

    const main = document.createElement("div");
    main.className = "task-main";
    const title = document.createElement("p");
    title.className = "task-title";
    title.textContent = task.title;
    const description = document.createElement("p");
    description.className = "task-description";
    description.textContent = task.description || task.category;
    main.append(title, description);

    const category = document.createElement("div");
    category.className = "category-cell";
    const dot = document.createElement("span");
    dot.className = `category-dot ${task.category.toLowerCase()}`;
    const categoryLabel = document.createElement("span");
    categoryLabel.textContent = labels.category[task.category] || task.category;
    category.append(dot, categoryLabel);

    const due = document.createElement("div");
    const isOverdue = task.dueDate && task.dueDate < localDateString() && task.status !== "Completed";
    due.className = `due-cell${isOverdue ? " overdue" : ""}${task.dueDate ? "" : " no-date"}`;
    due.append(createSvg("<rect x=\"3.5\" y=\"5.5\" width=\"17\" height=\"15\" rx=\"2.5\"/><path d=\"M7.5 3.5v4M16.5 3.5v4M3.5 10h17\"/>"));
    const dueLabel = document.createElement("span");
    dueLabel.textContent = task.dueDate ? (isOverdue ? `Overdue · ${displayDate(task.dueDate)}` : displayDate(task.dueDate)) : "No date";
    due.append(dueLabel);

    const priority = document.createElement("span");
    priority.className = `priority-pill priority-${task.priority.toLowerCase()}`;
    priority.textContent = labels.priority[task.priority] || task.priority;

    const statusCell = document.createElement("div");
    statusCell.className = "status-cell";
    const status = document.createElement("button");
    status.type = "button";
    status.className = `status-pill status-${task.status.toLowerCase()}`;
    status.textContent = labels.status[task.status] || task.status;
    status.setAttribute("aria-label", `Change status for ${task.title}. Current status: ${labels.status[task.status]}`);
    status.title = "Click to change status";
    status.addEventListener("click", () => {
      const next = { ToDo: "InProgress", InProgress: "Completed", Completed: "ToDo" }[task.status] || "ToDo";
      updateTask(task, { status: next });
    });
    statusCell.append(status);

    const actions = document.createElement("div");
    actions.className = "task-actions";
    const edit = document.createElement("button");
    edit.type = "button";
    edit.className = "task-action";
    edit.setAttribute("aria-label", `Edit ${task.title}`);
    edit.title = "Edit task";
    edit.append(createSvg("<path d=\"m14 5 5 5M4 20l4.2-.9L19.3 8a2.1 2.1 0 0 0-3-3L5.2 16.1 4 20Z\"/>"));
    edit.addEventListener("click", () => openTaskDialog(task));
    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "task-action delete";
    remove.setAttribute("aria-label", `Delete ${task.title}`);
    remove.title = "Delete task";
    remove.append(createSvg("<path d=\"M4 7h16M10 11v6m4-6v6M6 7l1 13h10l1-13M9 7V4h6v3\"/>"));
    remove.addEventListener("click", () => deleteTask(task));
    actions.append(edit, remove);

    row.append(check, main, category, due, priority, statusCell, actions);
    return row;
  }

  function makeTaskQuery() {
    const params = new URLSearchParams();
    const chosenStatus = $("#status-filter").value || (state.view === "completed" ? "Completed" : "");
    if (chosenStatus) params.set("status", chosenStatus);
    if ($("#category-filter").value) params.set("category", $("#category-filter").value);
    if ($("#priority-filter").value) params.set("priority", $("#priority-filter").value);
    if ($("#task-search").value.trim()) params.set("search", $("#task-search").value.trim());
    if (state.view === "upcoming") {
      const today = localDateString();
      const week = new Date();
      week.setDate(week.getDate() + 7);
      params.set("dueFrom", today);
      params.set("dueThrough", localDateString(week));
      params.set("excludeCompleted", "true");
    }
    params.set("page", String(state.page));
    params.set("pageSize", String(state.pageSize));
    return params;
  }

  async function loadTasks() {
    const loadId = ++state.loadId;
    const list = $("#task-list");
    list.setAttribute("aria-busy", "true");
    list.innerHTML = "<div class=\"loading-state\">Gathering your tasks…</div>";
    $("#task-empty").classList.add("hidden");
    $("#pagination").classList.add("hidden");
    try {
      const params = makeTaskQuery();
      const result = await api(`/api/tasks?${params.toString()}`);
      if (loadId !== state.loadId) return;
      state.totalPages = result.totalPages;
      state.totalCount = result.totalCount;
      if (state.totalPages > 0 && state.page > state.totalPages) {
        state.page = state.totalPages;
        await loadTasks();
        return;
      }
      $("#task-count").textContent = result.totalCount;
      list.replaceChildren(...result.items.map(createTaskRow));
      setEmptyState(result.totalCount === 0);
      setPagination(result);
    } catch (error) {
      if (loadId !== state.loadId) return;
      list.replaceChildren();
      setEmptyState(true, "We couldn’t load your tasks.", error.message);
      showToast(error.message, true);
    } finally {
      if (loadId === state.loadId) list.setAttribute("aria-busy", "false");
    }
  }

  function setEmptyState(isEmpty, title, copy) {
    $("#task-empty").classList.toggle("hidden", !isEmpty);
    if (!isEmpty) return;
    const hasFilters = Boolean($("#task-search").value || $("#status-filter").value || $("#category-filter").value || $("#priority-filter").value || state.view !== "all");
    $("#empty-title").textContent = title || (hasFilters ? "No tasks match those filters." : "Your list is clear.");
    $("#empty-copy").textContent = copy || (hasFilters ? "Try changing your search or clearing a filter." : "Add your first task and give your week a little shape.");
    $("#empty-add").classList.toggle("hidden", hasFilters || Boolean(title));
  }

  function setPagination(result) {
    const pagination = $("#pagination");
    const show = result.totalCount > 0;
    pagination.classList.toggle("hidden", !show);
    const first = result.totalCount ? (result.page - 1) * result.pageSize + 1 : 0;
    const last = Math.min(result.page * result.pageSize, result.totalCount);
    $("#page-summary").textContent = `Showing ${first}–${last} of ${result.totalCount}`;
    $("#page-prev").disabled = result.page <= 1;
    $("#page-next").disabled = result.page >= result.totalPages;
  }

  async function saveTask(event) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const id = $("#task-id").value;
    const payload = {
      title: $("#task-title").value.trim(),
      description: $("#task-description").value.trim() || null,
      dueDate: $("#task-due").value || null,
      priority: $("#task-priority").value,
      status: $("#task-status").value,
      category: $("#task-category").value,
    };
    const saveButton = $("#task-save");
    saveButton.disabled = true;
    saveButton.textContent = id ? "Saving…" : "Creating…";
    setMessage($("#task-form-message"));
    try {
      await api(id ? `/api/tasks/${id}` : "/api/tasks", {
        method: id ? "PUT" : "POST",
        body: JSON.stringify(payload),
      });
      $("#task-dialog").close();
      state.page = 1;
      await Promise.all([loadTasks(), refreshDashboard()]);
      showToast(id ? "Your task is updated." : "Your task is on the list.");
    } catch (error) {
      setMessage($("#task-form-message"), error.message);
    } finally {
      saveButton.disabled = false;
      saveButton.innerHTML = "Save task <span aria-hidden=\"true\">→</span>";
    }
  }

  function openTaskDialog(task = null) {
    $("#task-form").reset();
    $("#task-id").value = task?.id || "";
    $("#dialog-title").textContent = task ? "Edit task" : "Add a task";
    $("#task-title").value = task?.title || "";
    $("#task-description").value = task?.description || "";
    $("#task-due").value = task?.dueDate || "";
    $("#task-category").value = task?.category || "Assignment";
    $("#task-priority").value = task?.priority || "Medium";
    $("#task-status").value = task?.status || "ToDo";
    setMessage($("#task-form-message"));
    $("#task-dialog").showModal();
    requestAnimationFrame(() => $("#task-title").focus());
  }

  async function updateTask(task, changes) {
    const payload = {
      title: task.title,
      description: task.description,
      dueDate: task.dueDate,
      priority: task.priority,
      status: changes.status || task.status,
      category: task.category,
    };
    try {
      await api(`/api/tasks/${task.id}`, { method: "PUT", body: JSON.stringify(payload) });
      await Promise.all([loadTasks(), refreshDashboard()]);
      showToast(changes.status === "Completed" ? "One less thing on your mind. Nice work." : "Task status updated.");
    } catch (error) {
      showToast(error.message, true);
    }
  }

  async function deleteTask(task) {
    if (!window.confirm(`Delete “${task.title}”? This can’t be undone.`)) return;
    try {
      await api(`/api/tasks/${task.id}`, { method: "DELETE" });
      await Promise.all([loadTasks(), refreshDashboard()]);
      showToast("Task deleted.");
    } catch (error) {
      showToast(error.message, true);
    }
  }

  function setView(view) {
    state.view = view;
    state.page = 1;
    if (view !== "completed") $("#status-filter").value = "";
    $$(".nav-link").forEach(button => button.classList.toggle("active", button.dataset.view === view));
    const heading = view === "upcoming" ? "Upcoming" : view === "completed" ? "Completed" : "All tasks";
    $("#breadcrumb-current").textContent = heading;
    $("#tasks-heading").firstChild.textContent = `${heading} `;
    loadTasks();
    closeSidebar();
  }

  function closeSidebar() {
    $("#sidebar").classList.remove("open");
    $("#mobile-menu").setAttribute("aria-label", "Open navigation");
  }

  function bindEvents() {
    $("#login-tab").addEventListener("click", () => setAuthMode("login"));
    $("#register-tab").addEventListener("click", () => setAuthMode("register"));
    $("#auth-form").addEventListener("submit", async event => {
      event.preventDefault();
      const registering = state.authMode === "register";
      const button = $("#auth-submit");
      const original = registering ? "Create my account" : "Sign in";
      button.disabled = true;
      button.querySelector("span:first-child").textContent = registering ? "Creating your account…" : "Signing in…";
      setMessage($("#auth-message"));
      const body = {
        email: $("#auth-email").value.trim(),
        password: $("#auth-password").value,
      };
      if (registering) body.name = $("#auth-name").value.trim();
      try {
        const payload = await api(`/api/auth/${registering ? "register" : "login"}`, {
          method: "POST",
          body: JSON.stringify(body),
        });
        await completeSignIn(payload);
      } catch (error) {
        setMessage($("#auth-message"), error.message);
      } finally {
        button.disabled = false;
        button.querySelector("span:first-child").textContent = original;
      }
    });

    $("#logout-button").addEventListener("click", () => signOut());
    $("#add-task-top").addEventListener("click", () => openTaskDialog());
    $("#add-task-panel").addEventListener("click", () => openTaskDialog());
    $("#empty-add").addEventListener("click", () => openTaskDialog());
    $("#dialog-close").addEventListener("click", () => $("#task-dialog").close());
    $("#dialog-cancel").addEventListener("click", () => $("#task-dialog").close());
    $("#task-form").addEventListener("submit", saveTask);
    $("#task-dialog").addEventListener("click", event => {
      if (event.target === $("#task-dialog")) $("#task-dialog").close();
    });

    $$(".nav-link").forEach(button => button.addEventListener("click", () => setView(button.dataset.view)));
    ["#status-filter", "#category-filter", "#priority-filter"].forEach(selector => {
      $(selector).addEventListener("change", () => {
        state.view = "all";
        state.page = 1;
        $$(".nav-link").forEach(button => button.classList.toggle("active", button.dataset.view === "all"));
        $("#breadcrumb-current").textContent = "All tasks";
        $("#tasks-heading").firstChild.textContent = "All tasks ";
        loadTasks();
      });
    });
    $("#task-search").addEventListener("input", () => {
      state.page = 1;
      clearTimeout(state.searchTimer);
      state.searchTimer = setTimeout(loadTasks, 220);
    });
    $("#clear-filters").addEventListener("click", () => {
      $("#status-filter").value = "";
      $("#category-filter").value = "";
      $("#priority-filter").value = "";
      $("#task-search").value = "";
      setView("all");
    });
    $("#page-prev").addEventListener("click", () => { if (state.page > 1) { state.page -= 1; loadTasks(); } });
    $("#page-next").addEventListener("click", () => { if (state.page < state.totalPages) { state.page += 1; loadTasks(); } });
    $("#mobile-menu").addEventListener("click", () => {
      const open = $("#sidebar").classList.toggle("open");
      $("#mobile-menu").setAttribute("aria-label", open ? "Close navigation" : "Open navigation");
    });
    $(".main-column").addEventListener("click", event => {
      if (window.innerWidth <= 720 && $("#sidebar").classList.contains("open") && !event.target.closest("#mobile-menu")) closeSidebar();
    });
    document.addEventListener("keydown", event => {
      if (event.key === "/" && !["INPUT", "TEXTAREA", "SELECT"].includes(document.activeElement?.tagName) && !$("#task-dialog").open) {
        event.preventDefault();
        $("#task-search").focus();
      }
      if (event.key === "Escape") closeSidebar();
    });
  }

  async function initialize() {
    const year = String(new Date().getFullYear());
    $("#year").textContent = year;
    $("#footer-year").textContent = year;
    bindEvents();
    if (!state.token) {
      showAuth();
      return;
    }
    try {
      state.user = await api("/api/auth/me");
      $("#auth-view").classList.add("hidden");
      $("#app-view").classList.remove("hidden");
      $("#profile-name").textContent = state.user.name;
      $("#profile-email").textContent = state.user.email;
      $("#avatar").textContent = [...state.user.name.trim()][0]?.toUpperCase() || "S";
      setWelcome();
      await Promise.all([refreshDashboard(), loadTasks()]);
    } catch (error) {
      if (state.token) {
        signOut(false);
        setMessage($("#auth-message"), error.message);
      }
    }
  }

  initialize();
})();
