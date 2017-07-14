using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace OverwatchDataAPI.WebParser
{
    public class HeroPlaytimeWebParser
    {
        public static async Task<Dictionary<string, TimeSpan>> Parse(string battleTag, string region = "eu", string platform = "pc", string queueType = "competitive")
        {
            return OptimizeTimePrecision(await GetHeroTimeInfos(battleTag, region, platform, queueType));
        }

        public static Dictionary<string, int> Check(string battleTag, string region = "eu", string platform = "pc", string queueType = "competitive")
        {
            var heroTimeInfos = GetHeroTimeInfos(battleTag, region, platform, queueType).Result;
            var optimizedHeroTimes = OptimizeTimePrecision(heroTimeInfos);

            var result = new Dictionary<string, int>();
            foreach (var hero in heroTimeInfos.Keys)
            {
                var info = heroTimeInfos[hero];
                var optTime = optimizedHeroTimes[hero];
                if (info.Unit.StartsWith("s", StringComparison.CurrentCultureIgnoreCase))
                    result.Add(hero, info.Time.CompareTo((int)Math.Floor(optTime.TotalSeconds)));
                else if (info.Unit.StartsWith("m", StringComparison.CurrentCultureIgnoreCase))
                    result.Add(hero, info.Time.CompareTo((int)Math.Floor(optTime.TotalMinutes)));
                else if (info.Unit.StartsWith("h", StringComparison.CurrentCultureIgnoreCase))
                    result.Add(hero, info.Time.CompareTo((int)Math.Floor(optTime.TotalHours)));
            }
            return result;
        }

        private static Dictionary<string, TimeSpan> OptimizeTimePrecision(Dictionary<string, HeroTimeInfo> heroTimeInfos)
        {
            var maxItem = heroTimeInfos.Values.MaxBy(heroTimeInfo => heroTimeInfo.Time);
            var maxItemTime = ParseTime(maxItem.Time, maxItem.Unit);

            return heroTimeInfos.ToDictionary(pair => pair.Key,
                pair => TimeSpan.FromMilliseconds(pair.Value.Percent / maxItem.Percent * maxItemTime.TotalMilliseconds));
        }

        private static async Task<Dictionary<string, HeroTimeInfo>> GetHeroTimeInfos(string accName, string region, string platform, string queueType)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument doc;
            if (platform == "pc")
                doc = await web.LoadFromWebAsync($"https://playoverwatch.com/en-us/career/{platform}/{region}/{accName.Replace("#", "-")}");
            else
                doc = await web.LoadFromWebAsync($"https://playoverwatch.com/en-us/career/{platform}/{accName}");

            var items = doc.DocumentNode.SelectNodes($"//div[@id='{queueType}']//div[@data-category-id='overwatch.guid.0x0860000000000021']//div[@class='progress-2 m-animated progress-category-item']");

            var heroTimeInfos = new Dictionary<string, HeroTimeInfo>();
            foreach (var item in items)
            {
                var name = item.SelectNodes(".//div[@class='title']/text()")[0].InnerText;
                var timeWithUnit = item.SelectNodes(".//div[@class='description']/text()")[0].InnerText.Split(' ');
                var percent = double.Parse(item.Attributes["data-overwatch-progress-percent"].Value);

                if (timeWithUnit.Length == 2 && int.TryParse(timeWithUnit[0], out int time))
                    heroTimeInfos.Add(name, new HeroTimeInfo { Time = time, Unit = timeWithUnit[1], Percent = percent });
                else
                    heroTimeInfos.Add(name, new HeroTimeInfo());
            }
            return heroTimeInfos;
        }

        // add 0.5 because the times on the website are probably floored
        private static TimeSpan ParseTime(int time, string unit)
        {
            if (string.IsNullOrEmpty(unit))
                return new TimeSpan();

            double timeEst = time + 0.5;
            unit = unit.ToLower();
            if (unit.StartsWith("s"))
                return TimeSpan.FromSeconds(timeEst);
            if (unit.StartsWith("m"))
                return TimeSpan.FromMinutes(timeEst);
            if (unit.StartsWith("h"))
                return TimeSpan.FromHours(timeEst);
            return new TimeSpan();
        }

        private class HeroTimeInfo
        {
            public int Time { get; set; }
            public string Unit { get; set; }
            public double Percent { get; set; }

            public override string ToString()
            {
                return $"{Time} {Unit} ({Percent:0.##%})";
            }
        }
    }
}