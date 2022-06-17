namespace Microsoft.AspNetCore.Mvc
{
    static class ControllerBaseExtension
    {
        public static ActionResult Bytes(this ControllerBase controller, byte[] bytes)
        {
            return bytes != null
                ? controller.File(bytes, "application/json")
                : controller.NoContent();
        }
    }
}
