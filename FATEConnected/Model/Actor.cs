using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using System.Linq;
using System.Collections;
using DSharpPlus.Entities;
using FATEConnected.Services;
using DSharpPlus;
using DSharpPlus.SlashCommands;

namespace FATEConnected.Model
{
    public class Actor
    {
        [BsonId]
        public int Id { get; set; }
        public ulong Owner { get; set; }
        public ulong Server { get; set; }
        public string Name { get; set; }
        public int Fate { get; set; } = 3;
        public int Refresh { get; set; } = 3;

        public int Link { get; set; } = -1;

        [BsonIgnore]
        public Actor LinkActor { get; set; } = null;
        public bool Secondary { get;set; } = false;

        public Dictionary<int,bool> PStress { get; set; } = new Dictionary<int, bool>()
        {
            { 1, false  },
            { 2, false  },
            { 3, false  },
            { 4, false  }
        };
        public Dictionary<int, bool> MStress { get; set; } = new Dictionary<int, bool>()
        {
            { 1, false  },
            { 2, false  },
            { 3, false  },
            { 4, false  }
        };


        public string Color { get; set; } = "#696866";
        public string Image { get; set; }


        public Dictionary<string,SkillValue> Skills { get; set; } = new Dictionary<string, SkillValue>
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
        public List<Stunt> Stunts { get; set; } = new List<Stunt>();
        public Dictionary<Consequence,string> Consequences { get; set; } = new Dictionary<Consequence,string>();


        public int GetMaxStress(bool mental)
        {
            
            if(Link> -1 && LinkActor != null)
            {
                int score = 0;
                if (mental)
                {
                    score = Math.Max((int)LinkActor.Skills["Will"].Rank, (int)Skills["Will"].Rank);
                    return 2 + (score >= 1 ? 1 : 0) + (score >= 3 ? 1 : 0);
                }
                else
                {
                    score = Math.Max((int)LinkActor.Skills["Physique"].Rank, (int)Skills["Physique"].Rank);
                    return 2 + (score >= 1 ? 1 : 0) + (score >= 3 ? 1 : 0);
                }
                
            }
            else
            {
                if (mental) return 2 + ((int)Skills["Will"].Rank >= 1 ? 1 : 0) + ((int)Skills["Will"].Rank >= 3 ? 1 : 0);
                return 2 + ((int)Skills["Physique"].Rank >= 1 ? 1 : 0) + ((int)Skills["Physique"].Rank >= 3 ? 1 : 0);
            }
        }
        public DiscordInteractionResponseBuilder GetSheet(int Page)
        {
            DiscordEmbed Embed = null;

            if(Page == 0)
            {
                var MainPage = new DiscordEmbedBuilder()
                    .WithTitle(Name)
                    .WithColor(new DiscordColor(Color))
                    .WithThumbnail(Image);

                var body = new StringBuilder();

                if (Link > -1 && Secondary)
                {
                    body.AppendLine($"Fate `[{LinkActor.Fate}/{LinkActor.Refresh}]`");
                    body.AppendLine($"> {LinkActor.Fate.Bar(LinkActor.Refresh)}");

                    body.AppendLine($"Physical Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(false); i++)
                    {
                        body.Append(LinkActor.PStress[i] ? Dictionaries.Icons["phurt"] : Dictionaries.Icons["phy"]);
                    }
                    body.Append("\n");

                    body.AppendLine($"Mental Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(true); i++)
                    {
                        body.Append(LinkActor.MStress[i] ? Dictionaries.Icons["mhurt"] : Dictionaries.Icons["men"]);
                    }


                }
                else
                {
                    body.AppendLine($"Fate `[{Fate}/{Refresh}]`");
                    body.AppendLine($"> {Fate.Bar(Refresh)}");

                    body.AppendLine($"Physical Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(false); i++)
                    {
                        body.Append(PStress[i] ? Dictionaries.Icons["phurt"] : Dictionaries.Icons["phy"]);
                    }
                    body.Append("\n");

                    body.AppendLine($"Mental Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(true); i++)
                    {
                        body.Append(MStress[i] ? Dictionaries.Icons["mhurt"] : Dictionaries.Icons["men"]);
                    }
                }

                MainPage.WithDescription(body.ToString());

                body.Clear();

                foreach(var asp in Aspects.OrderBy(x=>x))
                {
                    body.AppendLine($"• {asp}");
                }

                if (body.Length > 0) MainPage.AddField("Aspects", body.ToString(), true);
                else MainPage.AddField("Aspects", "No Aspects", true);

                body.Clear();

                if (Consequences.ContainsKey(Consequence.Mild)) body.AppendLine($":two: {Consequences[Consequence.Mild]}");
                else body.AppendLine(":two: Empty.");

                if (Consequences.ContainsKey(Consequence.Physical)) body.AppendLine($":two: {Consequences[Consequence.Physical]}");
                if (Consequences.ContainsKey(Consequence.Mental)) body.AppendLine($":two: {Consequences[Consequence.Mental]}");

                if (Consequences.ContainsKey(Consequence.Moderate)) body.AppendLine($":four: {Consequences[Consequence.Moderate]}");
                else body.AppendLine(":four: Empty.");

                if (Consequences.ContainsKey(Consequence.Severe)) body.AppendLine($":six: {Consequences[Consequence.Severe]}");
                else body.AppendLine(":six: Empty.");

                MainPage.AddField("Consequences", body.ToString(), true);

                Embed = MainPage.Build();
            }
            else if (Page == 1)
            {
                var SkillsPage = new DiscordEmbedBuilder()
                    .WithTitle(Name)
                    .WithColor(new DiscordColor(Color))
                    .WithThumbnail(Image);

                var body = new StringBuilder();

                if (Link > -1 && Secondary)
                {
                    body.AppendLine($"Fate `[{LinkActor.Fate}/{LinkActor.Refresh}]`");
                    body.AppendLine($"> {LinkActor.Fate.Bar(LinkActor.Refresh)}");

                    body.AppendLine($"Physical Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(false); i++)
                    {
                        body.Append(LinkActor.PStress[i] ? Dictionaries.Icons["phurt"] : Dictionaries.Icons["phy"]);
                    }
                    body.Append("\n");

                    body.AppendLine($"Mental Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(true); i++)
                    {
                        body.Append(LinkActor.MStress[i] ? Dictionaries.Icons["mhurt"] : Dictionaries.Icons["men"]);
                    }


                }
                else
                {
                    body.AppendLine($"Fate `[{Fate}/{Refresh}]`");
                    body.AppendLine($"> {Fate.Bar(Refresh)}");

                    body.AppendLine($"Physical Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(false); i++)
                    {
                        body.Append(PStress[i] ? Dictionaries.Icons["phurt"] : Dictionaries.Icons["phy"]);
                    }
                    body.Append("\n");

                    body.AppendLine($"Mental Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(true); i++)
                    {
                        body.Append(MStress[i] ? Dictionaries.Icons["mhurt"] : Dictionaries.Icons["men"]);
                    }
                }

                SkillsPage.WithDescription(body.ToString());

                body.Clear();

                var sort = Skills.OrderBy(x => x.Key).ToArray();

                var loops = Math.Ceiling((decimal)sort.Count() / 6);
                int index = 0;

                for (int i = 0; i < loops; i++)
                {
                    for(int j = 0; j < 6; j++)
                    {
                        if (index >= sort.Count()) break;

                        body.Append($"**{sort[index].Key}** - {sort[index].Value.Rank} ({((int)sort[index].Value.Rank>0?"+":"")}{(int)sort[index].Value.Rank})\n> ");
                        body.Append($"{(sort[index].Value.Overcome ? Dictionaries.Icons["can_overcome"] : Dictionaries.Icons["cannot_overcome"])}");
                        body.Append($"{(sort[index].Value.Advantage ? Dictionaries.Icons["can_advantage"] : Dictionaries.Icons["cannot_advantage"])}");
                        body.Append($"{(sort[index].Value.Attack ? Dictionaries.Icons["can_attack"] : Dictionaries.Icons["cannot_attack"])}");
                        body.Append($"{(sort[index].Value.Defend ? Dictionaries.Icons["can_defend"] : Dictionaries.Icons["cannot_defend"])}\n");
                        index++;
                    }
                    SkillsPage.AddField("Skills", body.ToString(),true);
                    body.Clear();
                }

                

                Embed = SkillsPage.Build();
            }
            else if (Page == 2)
            {
                var StuntsPage = new DiscordEmbedBuilder()
                    .WithTitle(Name)
                    .WithColor(new DiscordColor(Color))
                    .WithThumbnail(Image);

                var body = new StringBuilder();

                if (Link > -1 && Secondary)
                {
                    body.AppendLine($"Fate `[{LinkActor.Fate}/{LinkActor.Refresh}]`");
                    body.AppendLine($"> {LinkActor.Fate.Bar(LinkActor.Refresh)}");

                    body.AppendLine($"Physical Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(false); i++)
                    {
                        body.Append(LinkActor.PStress[i] ? Dictionaries.Icons["phurt"] : Dictionaries.Icons["phy"]);
                    }
                    body.Append("\n");

                    body.AppendLine($"Mental Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(true); i++)
                    {
                        body.Append(LinkActor.MStress[i] ? Dictionaries.Icons["mhurt"] : Dictionaries.Icons["men"]);
                    }


                }
                else
                {
                    body.AppendLine($"Fate `[{Fate}/{Refresh}]`");
                    body.AppendLine($"> {Fate.Bar(Refresh)}");

                    body.AppendLine($"Physical Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(false); i++)
                    {
                        body.Append(PStress[i] ? Dictionaries.Icons["phurt"] : Dictionaries.Icons["phy"]);
                    }
                    body.Append("\n");

                    body.AppendLine($"Mental Stress");
                    body.Append("> ");
                    for (int i = 1; i <= GetMaxStress(true); i++)
                    {
                        body.Append(MStress[i] ? Dictionaries.Icons["mhurt"] : Dictionaries.Icons["men"]);
                    }
                }

                StuntsPage.WithDescription(body.ToString());

                body.Clear();

                foreach (var stunt in Stunts.OrderBy(x => x.Name))
                {
                    StuntsPage.AddField(stunt.Name, stunt.Description);
                }

                Embed = StuntsPage.Build();
            }

            var buttons = new List<DiscordComponent>();

            if (Link >- 1 ) buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"sh,{Id},swap", "Swap", false, new DiscordComponentEmoji("🔁")));

            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"sh,{Id},0", "Summary"));
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"sh,{Id},1", "Skills"));
            buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"sh,{Id},2", "Stunts"));

            var builder = new DiscordInteractionResponseBuilder()
                .AddEmbed(Embed)
                .AddComponents(buttons);

            return builder;
        }
    }

    public class StressNode
    {
        public bool Ticked { get; set; }
        public bool Available { get; set; }
    }
    public class SkillValue
    {
        public Rank Rank { get; set; } = Rank.Mediocre;
        public bool Overcome { get; set; } = true;
        public bool Advantage { get; set; } = true;
        public bool Attack { get; set; } = false;
        public bool Defend { get; set; } = false;
    }

    public class Stunt
    {
        public string Name { get; set; }
        public string Description { get; set; }
        
    }
    public enum Consequence
    {
        [ChoiceName("Mild (2)")]
        Mild = 0,
        [ChoiceName("Physical (2)")]
        Physical = 1,
        [ChoiceName("Mental (2)")]
        Mental = 2,
        [ChoiceName("Moderate (4)")]
        Moderate = 3,
        [ChoiceName("Severe (6)")]
        Severe = 4
    };
    public enum Rank { 
        [ChoiceName("Terrible (-2)")]
        Terrible = -2,
        [ChoiceName("Poor (-1)")]
        Poor = -1,
        [ChoiceName("Mediocre (+0)")]
        Mediocre = 0,
        [ChoiceName("Average (+1)")]
        Average = 1,
        [ChoiceName("Fair (+2)")]
        Fair = 2,
        [ChoiceName("Good (+3)")]
        Good = 3,
        [ChoiceName("Great (+4)")]
        Great = 4,
        [ChoiceName("Superb (+5)")]
        Superb = 5,
        [ChoiceName("Fantastic (+6)")]
        Fantastic = 6,
        [ChoiceName("Epic (+7)")]
        Epic = 7,
        [ChoiceName("Legendary (+8)")]
        Legendary = 8
    }
    public enum StressType
    {
        [ChoiceName("Physical")]
        Physical,
        [ChoiceName("Mental")]
        Mental
    }
}
