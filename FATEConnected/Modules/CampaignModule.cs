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
    
    public class CampaignModule : ApplicationCommandModule
    {
        public Services.Utilities utils;
        public LiteDatabase db;

        [SlashCommand("Campaign","View your active Campaign, or view any campaign.")]
        public async Task View(InteractionContext context, 
            [Autocomplete(typeof(CampaignAutoComplete))]
            [Option("Name","Name of the campaign.")]string Name = null)
        {
            var user = utils.GetUser(context.User.Id);

            if (Name.NullorEmpty())
            {
                if(user.Campaign == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have an active campaign! Assign one using the `/Campaigns Swap` to activate one or the `/Campaigns create` to create one!"));
                    return;
                }
                
                Campaign campaign = user.Campaign;

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .AddEmbed(campaign.GetSummary()));
                return;
            }
            else
            {
                var col = db.GetCollection<Campaign>("Campaigns");

                Campaign campaign = col.Include(x=>x.Actors).FindOne(x=>x.Server == context.Guild.Id && x.Name.StartsWith(Name.ToLower()));

                if(campaign == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Could not find a campaign in this server whose name starts with \"{Name}\"."));
                    return;
                }

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .AddEmbed(campaign.GetSummary()));
                return;
            }
        }

        [SlashCommandGroup("Campaigns", "Manage Campaigns in this server.")]
        public class CampaignSubModule : ApplicationCommandModule
        {
            [SlashCommandGroup("Manage","Manage the Campaigns directly.")]
            public class CampaignsSubManage : ApplicationCommandModule
            {
                public Services.Utilities utils;
                public LiteDatabase db;

                [SlashCommand("Create", "Creates a new campaign.")]
                public async Task Create(InteractionContext context,
                [Option("Name", "Name of this campaign.")] string Name)
                {
                    User user = utils.GetUser(context.User.Id);

                    var col = db.GetCollection<Campaign>("Campaigns");

                    if (col.Exists(x => x.Server == context.Guild.Id && x.Name == Name.ToLower()))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent($"Sorry, there's already a campaign in this server named \"{Name}\". No dupes are allowed."));
                        return;
                    }

                    Campaign campaign = new Campaign()
                    {
                        Owner = context.User.Id,
                        Server = context.Guild.Id,
                        Name = Name
                    };

                    int id = col.Insert(campaign);

                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Owner);
                    col.EnsureIndex(x => x.Aspects);
                    col.EnsureIndex(x => x.Server);
                    col.EnsureIndex(x => x.Actors);

                    Campaign inserted = col.FindById(id);

                    user.Campaign = inserted;

                    utils.UpdateUser(user);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Successfully created campaign **{Name}** and assigned it as your active campaign! Use the `/Campaigns` commands to start customizing your campaign!"));
                }

                [SlashCommand("Swap", "Swaps what is your active campaign.")]
                public async Task Swap(InteractionContext context,
                    [Autocomplete(typeof(OwnedCampaignAutoComplete))]
                [Option("Name","Name of the campaign.")]string Name)
                {
                    User user = utils.GetUser(context.User.Id);

                    var col = db.GetCollection<Campaign>("Campaigns");

                    Campaign campaign = col.FindOne(x => x.Name == Name.ToLower() && x.Server == context.Guild.Id && x.Owner == context.User.Id);

                    if (campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"There are no campaigns in this server that you own whose name is \"{Name}\"."));
                        return;
                    }

                    user.Campaign = campaign;

                    utils.UpdateUser(user);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Assigned {campaign.Name} as your active campaign!"));
                    return;
                }

                [SlashCommand("Evict", "Evits a character from your active campaign.")]
                public async Task Evict(InteractionContext context,
                    [Autocomplete(typeof(GMActorAutoComplete))]
                [Option("Name","Name of the character to evict.")]string Name)
                {
                    User user = utils.GetUser(context.User.Id);

                    if (user.Campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent("You do not have an active campaign! Assign one using the `/Campaigns Swap` to activate one or the `/Campaigns create` to create one!"));
                        return;
                    }

                    Campaign campaign = user.Campaign;

                    Actor actor = campaign.Actors.Find(x => x.Name.ToLower() == Name.ToLower());

                    if (actor == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent($"The character {Name} does not exist in the campaign {campaign.Name}!"));
                        return;
                    }
                    campaign.Actors.Remove(actor);

                    utils.UpdateCampaign(campaign);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent($"Removed character {actor.Name} from the campaign {campaign.Name}."));
                    return;
                }

                [SlashCommand("Delete", "Delete a campaign. **WARNING**: This cannot be undone.")]
                public async Task delete(InteractionContext context,
                    [Autocomplete(typeof(OwnedCampaignAutoComplete))]
                [Option("Name","Name of the campaign to be deleted.")]string Name)
                {
                    var col = db.GetCollection<Campaign>("Campaigns");

                    Campaign campaign = col.FindOne(x => x.Name == Name.ToLower() && x.Server == context.Guild.Id && x.Owner == context.User.Id);

                    if (campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"There are no campaigns in this server that you own whose name is \"{Name}\"."));
                        return;
                    }

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent($"Are you sure you want to delete the {campaign.Name} campaign?\n**WARNING: THIS CANNOT BE UNDONE.**")
                        .AddComponents(new DiscordButtonComponent[]
                        {
                        new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                        new DiscordButtonComponent(ButtonStyle.Danger,$"delCam,{campaign.Id}","Delete")
                        }));
                }
            }

            [SlashCommandGroup("Skills","Manage your active campaign's default skills.")]
            public class CampaignSkillManagement : ApplicationCommandModule
            {
                public Services.Utilities utils;
                public LiteDatabase db;

                [SlashCommand("Add","Adds a new skill to your campaign's defaults.")]
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
            [Option("Defend","Can this skill be used to defend?")]long Defend)
                {
                    User user = utils.GetUser(context.User.Id);

                    if (user.Campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have an active campaign! Assign one using the `/Campaigns Swap` to activate one or the `/Campaigns create` to create one!"));
                        return;
                    }

                    Campaign campaign = user.Campaign;

                    if (campaign.SkillTemplate.Keys.Any(x => x.ToLower() == Name.ToLower()))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"{campaign.Name} already has a skill named {Name}!"));
                        return;
                    }

                    campaign.SkillTemplate.Add(Name, new SkillValue()
                    {
                        Attack = Convert.ToBoolean(Attack),
                        Defend = Convert.ToBoolean(Defend),
                        Overcome = Convert.ToBoolean(Overcome),
                        Advantage = Convert.ToBoolean(Advantage)
                    });

                    utils.UpdateCampaign(campaign);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"Added skill **{Name}** to {campaign.Name}'s default skills!"));
                    return;
                }

                [SlashCommand("Remove","Removes a skill from your active campaign's defaults.")]
                public async Task Remove (InteractionContext context,
                    [Autocomplete(typeof(CampaignSkillAutoComplete))]
                    [Option("Name","Name of the skill to remove.")]string Name)
                {
                    User user = utils.GetUser(context.User.Id);

                    if (user.Campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have an active campaign! Assign one using the `/Campaigns Swap` to activate one or the `/Campaigns create` to create one!"));
                        return;
                    }

                    Campaign campaign = user.Campaign;

                    if (!campaign.SkillTemplate.Keys.Any(x => x.ToLower() == Name.ToLower()))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"{campaign.Name} does not have the \"{Name}\" skill!"));
                        return;
                    }

                    campaign.SkillTemplate.Remove(Name);

                    utils.UpdateCampaign(campaign);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"Removed skill {Name} from {campaign.Name}'s default skills!"));
                    return;
                }
            }
        
            [SlashCommandGroup("Aspects","Manage your active campaign's aspects.")]
            public class CampaignAspectManagement : ApplicationCommandModule
            {
                public Services.Utilities utils;
                public LiteDatabase db;

                [SlashCommand("Add","Adds a new aspect your active campaign.")]
                public async Task Add(InteractionContext context,[Option("Aspect","The full text of the aspect.")]string Aspect)
                {
                    User user = utils.GetUser(context.User.Id);

                    if (user.Campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have an active campaign! Assign one using the `/Campaigns Swap` to activate one or the `/Campaigns create` to create one!"));
                        return;
                    }

                    Campaign campaign = user.Campaign;

                    if (campaign.Aspects.Any(x=>x.ToLower() == Aspect.ToLower()))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"{campaign.Name} already has this exact aspect!"));
                        return;
                    }

                    campaign.Aspects.Add(Aspect);

                    utils.UpdateCampaign(campaign);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"Added the following aspect to the {campaign.Name} campaign!")
                            .AddEmbed(new DiscordEmbedBuilder().WithDescription(Aspect)));
                }
            
                [SlashCommand("Remove","Removes an aspect from your active campaign.")]
                public async Task Remove(InteractionContext context,
                    [Autocomplete(typeof(CampaignAspectAutoComplete))]
                    [Option("Aspect","Full text of the aspect.")]string Aspect)
                {
                    User user = utils.GetUser(context.User.Id);

                    if (user.Campaign == null)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("You do not have an active campaign! Assign one using the `/Campaigns Swap` to activate one or the `/Campaigns create` to create one!"));
                        return;
                    }

                    Campaign campaign = user.Campaign;

                    if (!campaign.Aspects.Any(x => x.ToLower() == Aspect.ToLower()))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"{campaign.Name} does not have this aspect!"));
                        return;
                    }
                    campaign.Aspects.RemoveAll(x => x.ToLower() == Aspect.ToLower());

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent($"Removed aspect `{Aspect}` from {campaign.Name}!"));
                    return;
                }
            }
        }
    }
}
