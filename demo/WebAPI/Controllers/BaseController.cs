using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase { }