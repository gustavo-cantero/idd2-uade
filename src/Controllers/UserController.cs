using IDD2.Model;
using Microsoft.AspNetCore.Mvc;

namespace IDD2.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(ConfigurationModel config) : ControllerBase
{
    [HttpPost("{userId:int}/visit/{productId}")]
    public async Task<IActionResult> Post(int userId, string productId)
    {
        await Data.User.CreateRelation(config, userId, productId, null);
        return Ok();
    }

    [HttpGet("{userId:int}/category")]
    public async Task<IActionResult> Category(int userId) =>
        Ok(await Data.User.GetCategory(config, userId));
}
