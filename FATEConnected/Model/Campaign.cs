using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using FATEConnected.Services;
using LiteDB;

namespace FATEConnected.Model
{
    public class Campaign
    {
        [BsonId]
        public int Id { get; set; }
        public ulong Owner { get; set; }
        public ulong Server { get; set; }
        public string Name { get; set; }

        [BsonRef("Actors")]
        public List<Actor> Actors { get; set; } = new List<Actor>();
        public Dictionary<string, SkillValue> SkillTemplate { get; set; } = new Dictionary<string, SkillValue>
        {
            { "Athletics", new SkillValue(){ Defend = true } },
            { "Burglary", new SkillValue() },
            { "Contacts", new SkillValue() { Defend = true } },
            { "Crafts", new SkillValue() },
            { "Deceive", new SkillValue() { Defend = true } },
            { "Drive", new SkillValue() { Defend = true } },
            { "Empathy", new SkillValue() { Defend = true } },
            { "Fight", new SkillValue() { Defend = true, Attack = true } },
            { "Investigate", new SkillValue() },
            { "Lore", new SkillValue() },
            { "Notice", new SkillValue() { Defend = true } },
            { "Physique", new SkillValue() { Defend = true } },
            { "Provoke", new SkillValue() { Attack = true } },
            { "Rapport", new SkillValue() { Defend = true } },
            { "Resources", new SkillValue() },
            { "Shoot", new SkillValue() { Attack = true } },
            { "Stealth", new SkillValue() { Defend = true } },
            { "Will", new SkillValue() { Defend = true } }
        };
        public List<string> Aspects { get; set; } = new List<string>();

        public DiscordEmbed GetSummary()
        {
            var builder = new DiscordEmbedBuilder()
                .WithTitle(Name)
                .WithDescription(string.Join("\n",Aspects))
                .AddField("Characters",Actors.Count>0?string.Join("\n",Actors.Select(x=> x.Name)):"No players.");
            
            var body = new StringBuilder();
            var sort = SkillTemplate.OrderBy(x => x.Key).ToArray();

            var loops = Math.Ceiling((decimal)sort.Count() / 6);
            int index = 0;

            for (int i = 0; i < loops; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (index >= sort.Count()) break;

                    body.Append($"**{sort[index].Key}**\n> ");
                    body.Append($"{(sort[index].Value.Overcome ? Dictionaries.Icons["can_overcome"] : Dictionaries.Icons["cannot_overcome"])}");
                    body.Append($"{(sort[index].Value.Advantage ? Dictionaries.Icons["can_advantage"] : Dictionaries.Icons["cannot_advantage"])}");
                    body.Append($"{(sort[index].Value.Attack ? Dictionaries.Icons["can_attack"] : Dictionaries.Icons["cannot_attack"])}");
                    body.Append($"{(sort[index].Value.Defend ? Dictionaries.Icons["can_defend"] : Dictionaries.Icons["cannot_defend"])}\n");
                    index++;
                }
                builder.AddField("Skill Template", body.ToString(),true);
                body.Clear();
            }
            return builder.Build();
        }
    }
}
