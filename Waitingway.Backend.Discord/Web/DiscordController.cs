using System.Security.Claims;
using Discord.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Waitingway.Backend.Database;
using Waitingway.Backend.Database.Models;

namespace Waitingway.Backend.Discord.Web;

public class DiscordController : Controller
{
    private readonly ILogger<DiscordController> _logger;
    private readonly DiscordConfig _config;
    private readonly WaitingwayContext _db;

    public DiscordController(ILogger<DiscordController> logger, DiscordConfig config, WaitingwayContext db)
    {
        _logger = logger;
        _config = config;
        _db = db;
    }

    [Route("")]
    public IActionResult DiscordInviteRedirect()
    {
        return Redirect(_config.InviteLink);
    }

    [Authorize(AuthenticationSchemes = DiscordDefaults.AuthenticationScheme)]
    [Route("link/{clientId:guid}")]
    public IActionResult LinkClient(Guid clientId)
    {
        var idString = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        if (idString == null)
        {
            return BadRequest("Failed to get user info, please try again.");
        }

        var discordId = ulong.Parse(idString);

        try
        {
            var linkInfo = new DiscordLinkInfo
            {
                ClientId = clientId,
                DiscordUserId = discordId
            };

            _db.DiscordLinkInfo.Add(linkInfo);
            _db.SaveChanges();
            _logger.LogInformation("Linked client {} to Discord user {}", clientId, discordId);
        }
        catch (DbUpdateException ex)
        {
            var sqlEx = ex.InnerException as PostgresException;
            if (sqlEx?.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogError("Failed to link client {} to Discord user {}: already linked", clientId, discordId);
                ViewBag.Error =
                    "Your Waitingway client is already linked to a Discord user. Please ask in the support channels if this is in error.";
            }
            else
            {
                _logger.LogError(ex, "Failed to link client {} to Discord user {}", clientId, discordId);
                throw;
            }
        }

        ViewBag.InviteLink = _config.InviteLink;
        return View();
    }
}