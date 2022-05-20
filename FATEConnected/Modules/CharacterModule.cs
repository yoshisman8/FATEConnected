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

namespace FATEConnected.Modules
{
    public class CharacterModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        
        [SlashCommand("Character", "View a character on this server. Or view your active character.")]
        public async Task View(
            InteractionContext ctx,
            [Autocomplete(typeof(ActorAutoComplete))]
            [Option("Name","Name of the character.")]string Name = null)
        {
            if (Name.NullorEmpty())
            {
                User user = utils.GetUser(ctx.User.Id);

                if (user.Primary == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have assigned any character as your active character. Use `/Characters Swap` to swap your active character or `/Character Create` to create a new character if you have none!"));
                    return;
                }

                Actor actor = user.Primary;

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    actor.GetSheet(0));
            }
            else
            {
                var col = db.GetCollection<Actor>("Actors");

                var query = col.Find(x => x.Server == ctx.Guild.Id && x.Name.StartsWith(Name.ToLower()));

                if (query == null || query.Count() == 0)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have assigned any character as your active character. Use `/Characters Swap` to swap your active character or `/Character Create` to create a new character if you have none!"));
                    return;
                }

                Actor actor = query.FirstOrDefault();

                if (actor.Link > -1) actor.LinkActor = col.FindById(actor.Link);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    actor.GetSheet(0));
            }
        }

        
    }
    #region Character Management
    [SlashCommandGroup("Characters", "Character Management Commands")]
    public class ChararcterSubModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Create", "Creates a character.")]
        public async Task Create(InteractionContext ctx, [Option("Name", "Name of the Character")] string Name,
            [Autocomplete(typeof(CampaignAutoComplete))]
                [Option("Campaign","(Optional) Name of the campaign this character belongs to.")]string Campaign = null)
        {
            User user = utils.GetUser(ctx.User.Id);

            var ACol = db.GetCollection<Actor>("Actors");
            var CCol = db.GetCollection<Campaign>("Campaigns");


            if (ACol.Exists(x => x.Server == ctx.Guild.Id && x.Name.ToLower() == Name.ToLower()))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"There is already a character with the name of \"{Name}\" in this server! Sorry, no dupes allowed!"));
                return;
            }

            Actor template = new Actor()
            {
                Owner = ctx.User.Id,
                Server = ctx.Guild.Id,
                Name = Name
            };

            int id = ACol.Insert(template);

            ACol.EnsureIndex(x => x.Owner);
            ACol.EnsureIndex(x => x.Server);
            ACol.EnsureIndex(x => x.Link);
            ACol.EnsureIndex(x => x.Secondary);
            ACol.EnsureIndex("Name", "LOWER($.Name)");

            Actor actor = ACol.FindById(id);
            string Message = $"Created character **{Name}**! This character has been assigned as your active character.";

            if (!Campaign.NullorEmpty())
            {
                Campaign C = CCol.FindOne(x => x.Name == Campaign.ToLower());

                if (C != null)
                {
                    actor.Skills = C.SkillTemplate;
                    C.Actors.Add(actor);

                    Message += $"/n{actor} has been added to the camapgin **{C.Name}**.";

                    utils.UpdateActor(actor);
                    utils.UpdateCampaign(C);
                }
            }

            user.Primary = actor;

            utils.UpdateUser(user);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(Message));
        }

        [SlashCommand("Rename", "Renames a character you own.")]
        public async Task Rename(InteractionContext ctx,
            [Autocomplete(typeof(OwnedActorAutoComplete))]
                [Option("Name","Name of the character to rename.")]string Name,
            [Option("New", "New name to give this character.")] string NewName)
        {
            var col = db.GetCollection<Actor>("Actors");

            Actor actor = col.FindOne(x => x.Owner == ctx.User.Id && x.Server == ctx.Guild.Id && x.Name.StartsWith(Name.ToLower()));

            string old = Name;

            if (actor == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Could not find any character you own in this server with the name \"{Name}\". Double check your spelling and try again!"));
                return;
            }

            actor.Name = NewName;

            utils.UpdateActor(actor);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Renamed character {old} to **{NewName}**!"));
        }

        [SlashCommand("Delete", "Deletes a character you own.")]
        public async Task Delete(InteractionContext ctx,
            [Autocomplete(typeof(OwnedActorAutoComplete))]
                [Option("Name","Name of the character to delete.")]string Name)
        {
            var col = db.GetCollection<Actor>("Actors");

            Actor actor = col.FindOne(x => x.Owner == ctx.User.Id && x.Server == ctx.Guild.Id && x.Name.StartsWith(Name.ToLower()));

            if (actor == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Could not find any character you own in this server with the name \"{Name}\". Double check your spelling and try again!"));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Are you sure you want to delete {actor.Name}?\nThis character will be removed from all Campaigns.\n**This cannot be undone.**")
                    .AddComponents(new DiscordButtonComponent[]
                    {
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,$"delChar,{actor.Id}","Delete")
                    }));
        }

        [SlashCommand("Swap", "Swaps your active character.")]
        public async Task Swap(InteractionContext ctx,
            [Autocomplete(typeof(OwnedActorAutoComplete))]
                [Option("Name","Name of your character.")]string Name)
        {
            var col = db.GetCollection<Actor>("Actors");

            Actor actor = col.Include(x => x.Link).FindOne(x => x.Owner == ctx.User.Id && x.Server == ctx.Guild.Id && x.Name.StartsWith(Name.ToLower()));

            if (actor == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Could not find any character you own in this server with the name \"{Name}\". Double check your spelling and try again!"));
                return;
            }

            User user = utils.GetUser(ctx.User.Id);

            user.Primary = actor;
            string Response = "";

            if (actor.Link > -1 && !actor.Secondary)
            {
                Actor secondary = col.FindById(actor.Link);
                user.Secondary = secondary;

                Response = $"Assigned character {actor.Name} as your Primary active character and {secondary.Name} as your Secondary active character!";
            }
            else
            {
                Response = $"Assigned character {actor.Name} as your active character!";
                user.Secondary = null;
            }

            utils.UpdateUser(user);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(Response));
        }

        [SlashCommand("Link", "(Commetfall Specific) Links two characters together.")]
        public async Task Link(InteractionContext context,
            [Autocomplete(typeof(OwnedActorAutoComplete))]
                [Option("Primary","Name of the primary/master character.")]string Primary,
            [Autocomplete(typeof(OwnedActorAutoComplete))]
                [Option("Secondary","Name of the secondary/guardian character.")]string Secondary)
        {
            var col = db.GetCollection<Actor>("Actors");

            Actor master = col.FindOne(x => x.Owner == context.User.Id && x.Server == context.Guild.Id && x.Name.StartsWith(Primary.ToLower()));
            Actor slave = col.FindOne(x => x.Owner == context.User.Id && x.Server == context.Guild.Id && x.Name.StartsWith(Secondary.ToLower()));

            if (master == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Could not find a character you own on this server whose name starts with \"{Primary}\"."));
                return;
            }
            if (slave == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Could not find a character you own on this server whose name starts with \"{Secondary}\"."));
                return;
            }
            if (master.Link > -1 || slave.Link > -1)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("One of these characters is already linked to another character! Unlink them first before attempting to establshing a new link using `/Characters unlink`."));
                return;
            }
            User user = utils.GetUser(context.User.Id);

            master.Link = slave.Id;
            slave.Link = master.Id;
            slave.Secondary = true;

            utils.UpdateActor(master);
            utils.UpdateActor(slave);

            user.Primary = master;
            user.Secondary = slave;

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Characters **{master.Name}**(Primary) and **{slave.Name}** (Secondary) are now *Linked*. Both characters have been assigned as your primary and secondary active characters.\nThe Secondary character now shares Physical Stress, Mental Stress, Fate Points and Refresh with their linked Primary character. To Unlink these characters, use the `/Characters Unlink` command."));
            return;
        }

        [SlashCommand("Unlink", "(Commetfall Specific) Unlinks a character from all other characters.")]
        public async Task Unlink(InteractionContext context,
            [Autocomplete(typeof(OwnedActorAutoComplete))]
                [Option("Name","Name of the Primary or Secondary character. Both will be unlinked.")]string Name)
        {
            var col = db.GetCollection<Actor>("Actors");

            Actor actor1 = col.FindOne(x => x.Owner == context.User.Id && x.Server == context.Guild.Id && x.Name.StartsWith(Name.ToLower()));

            if (actor1 == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Could not find a character you own on this server whose name starts with \"{Name}\"."));
                return;
            }

            if (actor1.Link == -1)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{actor1.Name} is not linked to any other character!"));
                return;
            }

            Actor actor2 = col.FindById(actor1.Link);

            actor1.Link = -1;
            actor1.Secondary = false;
            actor2.Link = -1;
            actor2.Secondary = false;

            utils.UpdateActor(actor1);
            utils.UpdateActor(actor2);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Characters {actor1.Name} and {actor2.Name} have been unlinked! They are now independent sheets."));
            return;

        }

    }
    #endregion

    #region Misc Settings
    [SlashCommandGroup("Set", "Sets additional attributes of your active character.")]
    public class SetCommands : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Color", "Sets the color for your active character.")]
        public async Task Color(InteractionContext ctx,
            [Option("Code", "The Hexadecimal Color code (#AABBCC format).")] string Code)
        {
            User user = utils.GetUser(ctx.User.Id);

            if (user.Primary == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;


            try
            {
                var color = new DiscordColor(Code);

                actor.Color = color.ToString();

                utils.UpdateActor(actor);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(color)
                    .WithDescription($"Changed {actor.Name}'s Sheet color to " + color.ToString() + "!")));
            }
            catch
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("This value is not a valid Hex color code (#AABBCC)."));
                return;
            }
        }

        [SlashCommand("Image", "Sets the image for your active character.")]
        public async Task Image(InteractionContext context, [Option("Image", "Image URL")] string url)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (!url.IsImageUrl())
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("This value is not a valid `.png` or `.jpeg` image URL!"));
                return;
            }

            actor.Image = url;

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent($"Updated {actor.Name}'s image!")
                .AddEmbed(new DiscordEmbedBuilder().WithColor(new DiscordColor(actor.Color)).WithImageUrl(url).Build()));
        }

        [SlashCommand("Refresh", "Sets the Refresh for your active character.")]
        public async Task Refresh(InteractionContext context, [Option("Refresh", "The refresh value.")] long value)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            int refresh = (int)value;

            actor.Refresh = refresh;

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name}'s Refresh is now **{refresh}**."));
            return;
        }

    }
    #endregion

    #region Skill Management
    [SlashCommandGroup("Skills", "Manage your active character's skills.")]
    public class SkillModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Add", "Add a new skill to your character.")]
        public async Task Add(InteractionContext context,
            [Option("Name", "Name of the Skill.")] string Name,
                [Choice("Can be used to Overcome an Obstacle",1)]
                [Choice("Cannot be used to Overcome an Obstacle",0)]
            [Option("Overcome","Can this skill be used to overcome an obstacle?")]long Overcome,
                [Choice("Can be used to Create and Advantage",1)]
                [Choice("Cannot be used to Create and Advantage",0)]
            [Option("Advantage","Can this skill be used to Create and Advantage?")]long Advantage,
                [Choice("Can be used to Attack",1)]
                [Choice("Cannot be used to Attack",0)]
            [Option("Attack","Can this skill be used to attack?")]long Attack,
                [Choice("Can be used to Defend",1)]
                [Choice("Cannot be used to Defend",0)]
            [Option("Defend","Can this skill be used to defend?")]long Defend,
            [Option("Rank", "What rank does this character have with this skill?")] Rank rank = Model.Rank.Mediocre)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (actor.Skills.Keys.Any(x => x.ToLower() == Name.ToLower()))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} already has a skill named {Name}!"));
                return;
            }

            actor.Skills.Add(Name, new SkillValue()
            {
                Attack = Convert.ToBoolean(Attack),
                Defend = Convert.ToBoolean(Defend),
                Overcome = Convert.ToBoolean(Overcome),
                Advantage = Convert.ToBoolean(Advantage),
                Rank = rank
            });

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Added skill **{Name}** to {actor.Name}'s sheet!"));
            return;
        }

        [SlashCommand("Remove", "Removes a skill from your active character.")]
        public async Task Remove(InteractionContext context,
            [Autocomplete(typeof(SkillAutoComplete))]
                [Option("Name","Name of the skill.")] string Name)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (Name.ToLower() == "physique" || Name.ToLower() == "will" || Name.ToLower() == "Athletics" || Name.ToLower() == "Notice" || Name.ToLower() == "Empahty" || Name.ToLower() == "Rapport")
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"You cannot remove that skill from any character as they are vital to the base game!"));
                return;
            }

            if (!actor.Skills.ContainsKey(Name))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} does not have a skill whose name is equal to \"{Name}\"."));
                return;
            }

            actor.Skills.Remove(Name);

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Removed skill{Name} from {actor.Name}'s sheet."));
            return;
        }

        [SlashCommand("Ranks", "Set the Rank of a skill.")]
        public async Task Rank(InteractionContext context,
            [Autocomplete(typeof(SkillAutoComplete))]
                [Option("Name","Name of the skill.")]string Name, [Option("Rank", "Rank you are setting this skill to.")] Rank rank = Model.Rank.Mediocre)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (!actor.Skills.ContainsKey(Name))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} does not have a skill whose name is equal to \"{Name}\"."));
                return;
            }

            actor.Skills[Name].Rank = rank;

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Set {actor.Name}'s {Name} skill rank to {rank}."));
            return;
        }
    }
    #endregion

    #region Aspect Management
    [SlashCommandGroup("Aspects", "Manage your active character's aspects.")]
    public class AspectModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Add", "Add a new aspect to your active character.")]
        public async Task Add(InteractionContext context, [Option("Aspect", "The name of the aspect.")] string name)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (actor.Aspects.Any(x => x.ToLower() == name.ToLower()))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} already has an aspect with that exact wording!"));
                return;
            }

            actor.Aspects.Add(name);

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Added new aspect to {actor.Name}'s sheet!")
                    .AddEmbed(new DiscordEmbedBuilder()
                            .WithDescription(name)
                            .WithColor(new DiscordColor(actor.Color))
                            .WithThumbnail(actor.Image))
                    );
            return;
        }

        [SlashCommand("Remove", "Removes an aspect from your active character.")]
        public async Task Remove(InteractionContext context,
            [Autocomplete(typeof(AspectAutoComplete))]
                [Option("Aspect", "Exact wording on the aspect.")]string Aspect)
        {
            User user = utils.GetUser(context.User.Id);

            if (user.Primary == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                return;
            }

            Actor actor = user.Primary;

            if (!actor.Aspects.Any(x => x.ToLower() == Aspect.ToLower()))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} doesn't have that aspect!"));
                return;
            }

            actor.Aspects.Remove(Aspect);

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Removed the following aspect from {actor.Name}'s sheet.")
                    .AddEmbed(new DiscordEmbedBuilder()
                            .WithDescription(Aspect)
                            .WithColor(DiscordColor.Red)
                            .WithThumbnail(actor.Image)));
            return;
        }
    }
    #endregion

    #region Consequence Management
    [SlashCommandGroup("Consequences", "Manage your active character's aspects.")]
    public class ConsequenceModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Add", "Add a new consequence.")]
        public async Task Add(InteractionContext context,
            [Option("Tier", "Consequence tier.")] Consequence Tier,
            [Option("Consequence", "Consequnce text.")] string body,
            [Autocomplete(typeof(GMActorAutoComplete))]
                [Option("Character","GM Only. Name of the actor to add this consequence to.")]string name)
        {
            Actor actor = null;

            User user = utils.GetUser(context.User.Id);

            if (name.NullorEmpty())
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

                if (campaign == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent($"You do not have an active campaign!"));
                    return;
                }
                Actor _actor = user.Campaign.Actors.Find(x => x.Name.ToLower() == name.ToLower());
                if (_actor == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent($"The {campaign.Name} does not have a character named \"{name}\"."));
                    return;
                }
                actor = _actor;
            }

            if (actor.Consequences.ContainsKey(Tier))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} already has an consequence of this tier!"));
                return;
            }
            actor.Consequences.Add(Tier, name);

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Added new Consequence to {actor.Name}'s sheet!"));
            return;
        }

        [SlashCommand("Remove", "Removes an aspect from your active character.")]
        public async Task Remove(InteractionContext context,
            [Option("Tier", "Which tier of consequence to remove.")] Consequence Tier,
            [Autocomplete(typeof(GMActorAutoComplete))]
                [Option("Character","GM Only. Name of the actor to add this consequence to.")]string name)
        {
            User user = utils.GetUser(context.User.Id);

            Actor actor = null;

            if (!name.NullorEmpty())
            {
                if (user.Campaign != null)
                {
                    Actor _actor = user.Campaign.Actors.Find(x => x.Name.ToLower() == name.ToLower());
                    if (actor != null) actor = _actor;
                }
            }
            else
            {
                if (user.Primary == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("You do not have an active character. Create one using the `/Character Create` command or select an existing one using `/Character Swap` first!"));
                    return;
                }
                actor = user.Primary;
            }

            if (!actor.Consequences.ContainsKey(Tier))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{actor.Name} doesn't have any consequence of that tier!"));
                return;
            }

            actor.Consequences.Remove(Tier);

            utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Removed the {Tier} condition from {actor.Name}'s sheet."));
            return;
        }


    }
    #endregion

}
