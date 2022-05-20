using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using System.Threading.Tasks;
using FATEConnected.Model;
using System.Linq;
using FATEConnected.Services;
using Dice;

namespace FATEConnected.Modules
{
    public class GameplayModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Roll","Roll Fate dice.")]
        public async Task Roll(InteractionContext context,[Option("Extra","Flat bonus to this roll.")]long bonus = 0)
        {

            var roll = Roller.Roll($"4dF {(bonus != 0 ? $"{(bonus>0?"+":"")}{bonus}" : "")}");

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent($"{context.User.Mention} rolls some dice.")
                .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Dice Roll").WithDescription($"{roll.ParseResults()} = `{roll.Value}`")));
        }


        [SlashCommand("Check","Roll a check as your active/primary character")]
        public async Task Check(InteractionContext context,
            [Autocomplete(typeof(SkillAutoComplete))]
            [Option("Skill","Skill to use for this check.")]string Skill,
            [Option("Extra","Additional Bonuses/Penalties to this roll.")]long bonus = 0)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (!actor.Skills.ContainsKey(Skill))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} does not have a skill whose name is equal to \"{Skill}\"."));
                return;
            }

            var skill = actor.Skills[Skill];

            var roll = Roller.Roll($"4dF {(skill.Rank != Rank.Mediocre ? $"{(skill.Rank > 0 ? "+" : "")}{(int)skill.Rank}" : "")} {(bonus != 0 ? $"{(bonus > 0 ? "+" : "")}{bonus}" : "")}");

            var parsed = new RollString()
            {
                ActorId = actor.Id,
                Bonus = (int)skill.Rank + (int)bonus,
                Dice = roll.Values.Where(x => x.DieType == DieType.Fudge).Select(x => (int)x.Value).ToArray(),
                Skill = Skill
            };

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{actor.Name} names a {Skill} check!")
                        .WithDescription($"{roll.ParseResults()} = `{roll.Value}`")
                        .WithColor(new DiscordColor(actor.Color))
                        .WithThumbnail(actor.Image))
                    .AddComponents(new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,$"fate,b,{parsed.Serialize()}","Spend Fate (+2)",false,new DiscordComponentEmoji("✨")),
                        new DiscordButtonComponent(ButtonStyle.Primary,$"fate,r,{parsed.Serialize()}","Spend Fate (Reroll)",false,new DiscordComponentEmoji("🎲"))
                    }));
        }


        [SlashCommand("SubCheck", "Roll a check as your Secondary character")]
        public async Task SubCheck(InteractionContext context,
            [Autocomplete(typeof(SkillSecondaryAutoComplete))]
            [Option("Skill","Skill to use for this check.")]string Skill,
            [Option("Extra", "Additional Bonuses/Penalties to this roll.")] long bonus = 0)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Secondary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an secondary character! Use `/Characters Swap` and assign the Primary character to activate both the Primary and Secondary characters!"));
                return;
            }

            Actor actor = user.Secondary;

            if (!actor.Skills.ContainsKey(Skill))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} does not have a skill whose name is equal to \"{Skill}\"."));
                return;
            }

            var skill = actor.Skills[Skill];

            var roll = Roller.Roll($"4dF {(skill.Rank != Rank.Mediocre ? $"{(skill.Rank > 0 ? "+" : "")}{(int)skill.Rank}" : "")} {(bonus != 0 ? $"{(bonus > 0 ? "+" : "")}{bonus}" : "")}");

            var parsed = new RollString()
            {
                ActorId = actor.Id,
                Bonus = (int)skill.Rank + (int)bonus,
                Dice = roll.Values.Where(x => x.DieType == DieType.Fudge).Select(x => (int)x.Value).ToArray(),
                Skill = Skill
            };

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"{actor.Name} names a {Skill} check!")
                        .WithDescription($"{roll.ParseResults()} = `{roll.Value}`")
                        .WithColor(new DiscordColor(actor.Color))
                        .WithThumbnail(actor.Image))
                    .AddComponents(new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,$"fate,b,{parsed.Serialize()}","Spend Fate (+2)",false,new DiscordComponentEmoji("✨")),
                        new DiscordButtonComponent(ButtonStyle.Primary,$"fate,r,{parsed.Serialize()}","Spend Fate (Reroll)",false,new DiscordComponentEmoji("🎲"))
                    }));
        }


        [SlashCommand("Stress", "Add stress to your active character.")]
        public async Task Stress(InteractionContext context,
            [Option("Type", "Is this Physical or Mental stress?")] StressType type,
            [Option("Severity", "Severity of the stress. Ranges 1 thru 4.")] long value,
            [Autocomplete(typeof(GMActorAutoComplete))]
            [Option("Name","GM Only. Name of the character.")]string Name)
        {
            value = Math.Abs(value);
            if (value < 1) value = 1;
            else if (value > 4) value = 4;

            Actor actor = null;

            User user = utils.GetUser(context.User.Id);

            if (Name.NullorEmpty())
            {
                if (user.Primary == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                    return;
                }

                actor = user.Primary;
            }
            else
            {
                Campaign campaign = user.Campaign;

                if(campaign == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent($"You do not have an active campaign!"));
                    return;
                }
                Actor _actor = user.Campaign.Actors.Find(x => x.Name.ToLower() == Name.ToLower());
                if (_actor == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent($"The {campaign.Name} does not have a character named \"{Name}\"."));
                    return;
                }
                actor = _actor;
            }
            
            bool KO = false;

            switch (type)
            {
                case StressType.Physical:
                    for(int i = (int)value; i < 5; i++)
                    {
                        if (i == 5) { KO = true; break; }
                        else if (actor.PStress[i]) continue;
                        else { actor.PStress[i] = true; break; }
                    }
                    break;
                case StressType.Mental:
                    for (int i = (int)value; i < 5; i++)
                    {
                        if (i == 5) { KO = true; break; }
                        else if (actor.MStress[i]) continue;
                        else { actor.MStress[i] = true; break; }
                    }
                    break;
            }

            utils.UpdateActor(actor);

            string message = "";

            if (actor.Link > -1)
            {
                if (KO) message = $"{actor.Name} & {actor.LinkActor.Name} took more {type} stress than what they could take! They are out of the fight!";
                else message = $"{actor.Name} & {actor.LinkActor.Name} took {value}{type} stress!";
            }
            else
            {
                if (KO) message = $"{actor.Name} took more {type} stress than what they could take! They are out of the fight!";
                else message = $"{actor.Name} took {value}{type} stress!";
            }

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(message));
            return;
        }


        [SlashCommand("Fate", "Increase or Decrease your Fate points on your active character.")]
        public async Task Fate(InteractionContext context, [Option("Value", "Positive numbers add, Negative numbers decrease.")] long value)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            actor.Fate = (int)Math.Max(0, actor.Fate + value);

            utils.UpdateActor(actor);

            if (user.Secondary != null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent($"Updated {actor.Name} & {user.Secondary.Name}'s health to **{actor.Fate}**."));
                return;
            }
            else
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Updated {actor.Name}'s health to **{actor.Fate}**."));
                return;
            }
        }
    
        [SlashCommand("Refresh","Refreshes your Fate Points to their refresh values.")]
        public async Task Refresh(InteractionContext context)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            actor.Fate = Math.Max(actor.Fate, actor.Refresh);

            

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Refreshed {actor.Name}'s Fate points!"));
            return;
        }
        [SlashCommand("Heal", "Removes all stress.")]
        public async Task Heal(InteractionContext context)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            for (int i = 1; i < 4; i++)
            {
                actor.PStress[i] = false;
                actor.MStress[i] = false;
            }

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Cleared {actor.Name}'s Stress!"));
            return;
        }
    }
}
