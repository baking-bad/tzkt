namespace Microsoft.AspNetCore.Mvc
{
    static class ControllerBaseExtension
    {
        public static ActionResult Json(this ControllerBase controller, string json)
        {
            return json != null 
                ? controller.Content(json, "application/json")
                : controller.NoContent();
        }
    }
}
