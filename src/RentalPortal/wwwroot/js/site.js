(() => {
    const toggle = document.getElementById("nav-toggle");
    if (toggle) {
        toggle.addEventListener("click", () => document.body.classList.toggle("nav-open"));
    }

    const modalEl = document.getElementById("app-modal");
    if (!modalEl) {
        return;
    }

    const content = document.getElementById("app-modal-content");
    const dialog = modalEl.querySelector(".modal-dialog");
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

    const modalMessage = (title, message) => `
        <div class="modal-header">
            <h2 class="modal-title">${title}</h2>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body"><p>${message}</p></div>
        <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
        </div>`;

    const readModalResponse = async (response) => {
        if (response.status === 401) {
            content.innerHTML = modalMessage("Sign in required", "Please log in again to continue.");
            return null;
        }
        if (response.status === 403) {
            content.innerHTML = modalMessage("Not allowed", "You do not have permission to do that.");
            return null;
        }
        if (response.status === 404) {
            content.innerHTML = modalMessage("Not found", "That record is no longer available.");
            return null;
        }
        return response;
    };

    const openModal = async (url, sizeClass) => {
        dialog.className = "modal-dialog " + (sizeClass || "");
        content.innerHTML = '<div class="p-5 text-center text-muted">Loading…</div>';
        modal.show();
        const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!(await readModalResponse(response))) {
            return;
        }
        content.innerHTML = await response.text();
    };

    document.addEventListener("click", (event) => {
        const trigger = event.target.closest("[data-modal-url]");
        if (!trigger) {
            return;
        }
        event.preventDefault();
        openModal(trigger.getAttribute("data-modal-url"), trigger.getAttribute("data-modal-size") || "");
    });

    document.addEventListener("submit", async (event) => {
        const form = event.target.closest("#app-modal-content form");
        if (!form) {
            return;
        }

        event.preventDefault();
        const response = await fetch(form.action, {
            method: (form.method || "POST").toUpperCase(),
            body: new FormData(form),
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        if (!(await readModalResponse(response))) {
            return;
        }

        if (response.headers.get("X-Modal-Result") === "success") {
            const targetSelector = response.headers.get("X-Refresh-Target");
            const html = (await response.text()).trim();
            const target = targetSelector ? document.querySelector(targetSelector) : null;
            if (target) {
                const wrapper = document.createElement("div");
                wrapper.innerHTML = html;
                const next = wrapper.firstElementChild;
                if (next) {
                    target.replaceWith(next);
                }
            }
            modal.hide();
            return;
        }

        content.innerHTML = await response.text();
    });
})();
