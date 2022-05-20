using Dice;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using FATEConnected.Model;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FATEConnected.Services
{
    public class ButtonService
    {
        private LiteDatabase database;
        private Utilities utils;
        public ButtonService(DiscordClient client, LiteDatabase _db, Utilities _utils)
        {
            database = _db;
            utils = _utils;
        }

        public async Task HandleButtonAsync(DiscordClient c, ComponentInteractionCreateEventArgs e)
        {
            var args = e.Id.Split(',');

            if (args.Length == 0) return;

            User user = utils.GetUser(e.User.Id);

            if (args[0] == "cancel")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                     new DiscordInteractionResponseBuilder().WithContent("Operation Cancelled!"));
            }
            else if (args[0] == "sh")
            {
                var col = database.GetCollection<Actor>("Actors");

                Actor actor = utils.GetActor(int.Parse(args[1]));

                if (actor == null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().WithContent("Sorry, this character no longer exists!"));
                    return;
                }
                if (actor.Link > -1)
                {
                    Actor link = col.FindById(actor.Link);
                    if (link == null)
                    {
                        actor.Link = -1;
                        actor.Secondary = false;

                        utils.UpdateActor(actor);
                    }
                    else
                    {
                        actor.LinkActor = col.FindById(actor.Link);
                    }
                }
                if(args[2] == "swap" && actor.LinkActor != null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        actor.LinkActor.GetSheet(0));
                }
                else await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, actor.GetSheet(int.Parse(args[2])));
            }
            else if(args[0] == "delChar")
            {
                int id = int.Parse(args[1]);

                var actorCollection = database.GetCollection<Actor>("Actors");

                var campaignCollection = database.GetCollection<Campaign>("Campaigns");

                var userCollection = database.GetCollection<User>("Users");

                var users = userCollection.Find(x=> x.Primary.Id == id || x.Secondary.Id == id);

                foreach(var u in users)
                {
                    if (u.Primary.Id == id) u.Primary = null;
                    if (u.Secondary.Id == id) u.Secondary = null;
                    utils.UpdateUser(u);
                }

                var campaigns = campaignCollection.Include(x=>x.Actors).Find(x=> x.Actors.Any(x=>x.Id == id));

                foreach(var camp in campaigns)
                {
                    var index = camp.Actors.FindIndex(x=>x.Id == id);
                    camp.Actors.RemoveAt(index);
                    utils.UpdateCampaign(camp);
                }

                Actor actor = actorCollection.FindById(id);

                actorCollection.Delete(id);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Character {actor.Name} has been deleted!"));
            }
            else if(args[0] == "delCam")
            {
                int id = int.Parse(args[1]);

                var campaignCollection = database.GetCollection<Campaign>("Campaigns");

                var userCollection = database.GetCollection<User>("Users");

                var users = userCollection.Include(x=>x.Campaign).Find(x=>x.Campaign.Id == id);
                
                Campaign campaign = campaignCollection.FindById(id);

                foreach(var u in users)
                {
                    u.Campaign = null;
                    utils.UpdateUser(u);
                }

                campaignCollection.Delete(id);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"Campaign {campaign.Name} has been deleted!"));
            }
            else if (args[0] == "fate")
            {
                RollString rollData = new RollString().Deserialize(args[2]);

                Actor actor = utils.GetActor(rollData.ActorId);

                if(actor == null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().WithContent("Sorry, this character no longer exists!"));
                    return;
                }

                if(actor.Fate == 0)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Not enough Fate Points to boost!")
                            .AddEmbed(new DiscordEmbedBuilder()
                                .WithTitle($"{actor.Name} makes a {rollData.Skill} check!")
                                .WithThumbnail(actor.Image)
                                .WithColor(new DiscordColor(actor.Color))
                                .WithDescription($"{string.Join(" + ", rollData.Dice.Select(x => Dictionaries.Dice[x]))} {(rollData.Bonus != 0 ? $"{(rollData.Bonus > 0 ? "+" : "")}{rollData.Bonus} " : "")} = `{(rollData.Dice.Sum() + rollData.Bonus)}`"))
                            .AddComponents(new DiscordButtonComponent[]
                            {
                                new DiscordButtonComponent(ButtonStyle.Primary,$"fate,b,{rollData.Serialize()}","Spend Fate (+2)",false,new DiscordComponentEmoji("✨")),
                                new DiscordButtonComponent(ButtonStyle.Primary,$"fate,r,{rollData.Serialize()}","Spend Fate (Reroll)",false,new DiscordComponentEmoji("🎲"))
                            }));
                    return;
                }

                actor.Fate--;

                utils.UpdateActor(actor);

                if(args[1] == "r")
                {
                    var roll = Roller.Roll($"4dF {(rollData.Bonus != 0 ? $"{(rollData.Bonus > 0 ? "+" : "")}{rollData.Bonus}" : "")}");

                    rollData.Dice = roll.Values.Where(x=>x.DieType == DieType.Fudge).Select(x=>(int)x.Value).ToArray();

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Re-rolled dice using a Fate Point!")
                            .AddEmbed(new DiscordEmbedBuilder()
                                .WithTitle($"{actor.Name} makes a {rollData.Skill} check!")
                                .WithThumbnail(actor.Image)
                                .WithColor(new DiscordColor(actor.Color))
                                .WithDescription($"{roll.ParseResults()} = `{roll.Value}`")));
                }
                else
                {
                    var sb = new StringBuilder();

                    foreach(var die in rollData.Dice)
                    {
                        sb.Append(Dictionaries.Dice[die]);
                    }
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Added +2 to this check using a Fate Point!")
                            .AddEmbed(new DiscordEmbedBuilder()
                                .WithTitle($"{actor.Name} makes a {rollData.Skill} check!")
                                .WithThumbnail(actor.Image)
                                .WithColor(new DiscordColor(actor.Color))
                                .WithDescription($"{string.Join(" + ",rollData.Dice.Select(x=>Dictionaries.Dice[x]))} {(rollData.Bonus != 0 ? $"{(rollData.Bonus > 0 ? "+" : "")}{rollData.Bonus} " : "")}+2 = `{(rollData.Dice.Sum()+rollData.Bonus+2)}`")));
                }
            }
        }
    }
}
