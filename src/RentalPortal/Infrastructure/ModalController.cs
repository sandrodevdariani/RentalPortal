using Microsoft.AspNetCore.Mvc;

namespace RentalPortal.Infrastructure;

public abstract class ModalController : Controller
{
    protected IActionResult ModalForm(string viewName, object model) =>
        PartialView(viewName, model);

    protected IActionResult ModalSuccess(string refreshTarget, string viewName, object model)
    {
        Response.Headers["X-Modal-Result"] = "success";
        Response.Headers["X-Refresh-Target"] = refreshTarget;
        return PartialView(viewName, model);
    }

    protected IActionResult ModalSuccessComponent(string refreshTarget, string componentName, object args)
    {
        Response.Headers["X-Modal-Result"] = "success";
        Response.Headers["X-Refresh-Target"] = refreshTarget;
        return ViewComponent(componentName, args);
    }
}
