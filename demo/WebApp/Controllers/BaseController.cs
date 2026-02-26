using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase { }