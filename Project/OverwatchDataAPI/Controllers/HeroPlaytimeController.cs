using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OverwatchDataAPI.WebParser;

namespace OverwatchDataAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class HeroPlaytimeController : Controller
    {
        private static readonly string[] RegionCodes = {"us", "eu", "kr", "cn"};
        private static readonly string[] PlatformCodes = { "pc", "psn", "xbl" };
        private static readonly string[] QueueTypes = { "competitive", "quickplay" };
        private static readonly Regex BattleTagRegex = new Regex(@"^[^\d#][^\#]{2,11}[#-][0-9]{4,5}$", RegexOptions.Compiled);

        [HttpGet]
        public async Task<IActionResult> Get(string battletag, string region = "eu", string platform = "pc", string queue = "competitive")
        {
            region = region.ToLower();
            platform = platform.ToLower();
            queue = queue.ToLower();

            if (!(RegionCodes.Contains(region) && PlatformCodes.Contains(platform) && QueueTypes.Contains(queue)) || platform == "pc" && !BattleTagRegex.Match(battletag).Success)
                return BadRequest();

            var result = await HeroPlaytimeWebParser.Parse(battletag, region, platform, queue);

            return Ok(result);
        }
    }
}
