using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FATEConnected.Model;
using System.Net;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Dice;

namespace FATEConnected.Services
{
    public class Utilities
    {
        private LiteDatabase database;

        public Utilities(LiteDatabase _db)
        {
            database = _db;
        }

        public User GetUser(ulong Id)
        {
            var col = database.GetCollection<User>("Users");

            if (col.Exists(x => x.Id == Id))
            {
                User user = col.Include(x=>x.Campaign)
                    .Include(x=>x.Campaign.Actors)
                    .Include(x => x.Primary)
                    .Include(x => x.Secondary)
                    .FindOne(x => x.Id == Id);
                
                if(user.Primary != null && user.Primary.Link > -1) user.Primary.LinkActor = GetActor(user.Primary.Link); 
                if(user.Secondary != null && user.Secondary.Link > -1) user.Secondary.LinkActor = GetActor(user.Secondary.Link);
                return user;
            }
            else
            {
                var User = new User()
                {
                    Id = Id
                };
                col.Insert(User);
                col.EnsureIndex(x => x.Id);
                col.EnsureIndex(x => x.Primary);
                col.EnsureIndex(x => x.Secondary);
                col.EnsureIndex(x => x.Campaign);

                User user = col.Include(x => x.Campaign)
                    .Include(x => x.Campaign.Actors)
                    .Include(x => x.Primary)
                    .Include(x => x.Secondary)
                    .FindOne(x => x.Id == Id);

                if (user.Primary != null && user.Primary.Link > -1) user.Primary.LinkActor = GetActor(user.Primary.Link);
                if (user.Secondary != null && user.Secondary.Link > -1) user.Secondary.LinkActor = GetActor(user.Secondary.Link);
                return user;
            }
        }
        public Actor GetActor(int id)
        {
            var actors = database.GetCollection<Actor>("Actors");

            return actors.FindById(id);
        }
        public void UpdateUser(User U)
        {
            var col = database.GetCollection<User>("Users");
            col.Update(U);
        }
        public void UpdateActor(Actor A)
        {
            var col = database.GetCollection<Actor>("Actors");

            col.Update(A);
        }
        public void UpdateCampaign(Campaign C)
        {
            var col = database.GetCollection<Campaign>("Campaigns");

            col.Update(C);
        }

    }
    public static class Dictionaries
    {
        public static Dictionary<string, string> Icons { get; set; } = new Dictionary<string, string>()
        {
            { "phy", "<:Physical:827589986283028490>" },
            { "phurt", "<:PHurt:827589602805415997>" },
            { "men", "<:Mental:685854267466449107>" },
            { "mhurt", "<:MHurt:974743998356914198>" },
            { "fate", "<:Fate:948212068413210658>" },
            { "empty", "<:Empty:685854267512455178>" },
            { "can_attack", "<:can_attack:974037923597013043>"},
            { "cannot_attack", "<:cannot_attack:974037921592123392>"},
            { "can_advantage", "<:can_advantage:974037922762330173>" },
            { "cannot_advantage", "<:cannot_advantage:974037921252384909>" },
            { "can_defend", "<:can_defend:974037923089494076>" },
            { "cannot_defend", "<:cannot_defend:974037921717977159>" },
            { "can_overcome", "<:can_overcome:974037922850426941>" },
            { "cannot_overcome", "<:cannot_overcome:974037921252384921>" }
        };
        public static Dictionary<int, string> Dice { get; set; } = new Dictionary<int, string>()
        {
            { 1, "<:plus:973966281340506192>" },
            { 0, "<:blank:973966280937844847>" },
            { -1, "<:minus:973966280929443861>" }
        };
        public static Dictionary<int, string> d20 { get; set; } = new Dictionary<int, string>()
        {
            {20, "<:d20_20:663149799792705557>" },
            {19, "<:d20_19:663149782847586304>" },
            {18, "<:d20_18:663149770621190145>" },
            {17, "<:d20_17:663149758885396502>" },
            {16, "<:d20_16:663149470216749107>" },
            {15, "<:d20_15:663149458963300352>" },
            {14, "<:d20_14:663149447278100500>" },
            {13, "<:d20_13:663149437459234846>" },
            {12, "<:d20_12:663149424909746207>" },
            {11, "<:d20_11:663149398712123415>" },
            {10, "<:d20_10:663149389396574212>" },
            {9, "<:d20_9:663149377954775076>" },
            {8, "<:d20_8:663149293695139840>" },
            {7, "<:d20_7:663149292743032852>" },
            {6, "<:d20_6:663149290532634635>" },
            {5, "<:d20_5:663147362608480276>" },
            {4, "<:d20_4:663147362512011305>" },
            {3, "<:d20_3:663147362067415041>" },
            {2, "<:d20_2:663147361954037825>" },
            {1, "<:d20_1:663146691016523779>" }
        };
        public static Dictionary<int, string> d12 { get; set; } = new Dictionary<int, string>()
        {
            {12, "<:d12_12:663152540426174484>" },
            {11, "<:d12_11:663152540472442900>" },
            {10, "<:d12_10:663152540439019527>" },
            {9, "<:d12_9:663152540199682061>" },
            {8, "<:d12_8:663152540459728947>" },
            {7, "<:d12_7:663152540116058133>" },
            {6, "<:d12_6:663152540484894740>" },
            {5, "<:d12_5:663152540250144804>" },
            {4, "<:d12_4:663152540426305546>" },
            {3, "<:d12_3:663152540161933326>" },
            {2, "<:d12_2:663152538291404821>" },
            {1, "<:d12_1:663152538396393482>" }
        };
        public static Dictionary<int, string> d10 { get; set; } = new Dictionary<int, string>()
        {
            {10, "<:d10_10:663158741352579122>" },
            {9, "<:d10_9:663158741331476480>" },
            {8, "<:d10_8:663158741079687189>" },
            {7, "<:d10_7:663158742636036138>" },
            {6, "<:d10_6:663158741121761280>" },
            {5, "<:d10_5:663158740576632843>" },
            {4, "<:d10_4:663158740685553713>" },
            {3, "<:d10_3:663158740442415175>" },
            {2, "<:d10_2:663158740496810011>" },
            {1, "<:d10_1:663158740463255592>" }
        };
        public static Dictionary<int, string> d8 { get; set; } = new Dictionary<int, string>()
        {
            {8, "<:d8_8:663158785795162112>" },
            {7, "<:d8_7:663158785841561629>" },
            {6, "<:d8_6:663158785774190595>" },
            {5, "<:d8_5:663158785271005185>" },
            {4, "<:d8_4:663158785107296286>" },
            {3, "<:d8_3:663158785543503920>" },
            {2, "<:d8_2:663158785224867880>" },
            {1, "<:d8_1:663158784859963473>" }
        };
        public static Dictionary<int, string> d6 { get; set; } = new Dictionary<int, string>()
        {
            {6, "<:d6_6:663158852551835678>" },
            {5, "<:d6_5:663158852136599564>" },
            {4, "<:d6_4:663158856247148566>" },
            {3, "<:d6_3:663158852358766632>" },
            {2, "<:d6_2:663158852354834452>" },
            {1, "<:d6_1:663158852354572309>" }
        };
        public static Dictionary<int, string> d4 { get; set; } = new Dictionary<int, string>()
        {
            {4, "<:d4_4:663158852472274944>" },
            {3, "<:d4_3:663158852178411560>" },
            {2, "<:d4_2:663158851734077462>" },
            {1, "<:d4_1:663158851909976085>" }
        };
    }
    public static class UserExtensions
    {
        public static string Bar(this int value, int max)
        {
            var sb = new StringBuilder();

            if (max > 10)
            {
                decimal percent = ((decimal)Math.Max(Math.Min(value, max), 0) / (decimal)max) * 10;

                var diff = 10 - Math.Ceiling(percent);

                for (int i = 0; i < Math.Ceiling(percent); i++)
                {
                    sb.Append(Dictionaries.Icons["fate"]);
                }
                for (int i = 0; i < diff; i++)
                {
                    sb.Append(Dictionaries.Icons["empty"]);
                }
            }
            else
            {
                for (int i = 0; i < value; i++)
                {
                    sb.Append(Dictionaries.Icons["fate"]);
                }
                for (int i = 0; i < Math.Max(0, max - value); i++)
                {
                    sb.Append(Dictionaries.Icons["empty"]);
                }
            }

            return sb.ToString();

        }
        public static bool NullorEmpty(this string _string)
        {
            if (_string == null) return true;
            if (_string == "") return true;
            else return false;
        }
        public static bool IsImageUrl(this string URL)
        {
            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create(URL);
                req.Method = "HEAD";
                using (var resp = req.GetResponse())
                {
                    return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                            .StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        public static string ParseResults(this RollResult roll)
        {
            var sb = new StringBuilder();

            foreach(var dice in roll.Values)
            {
                switch (dice.DieType)
                {
                    case DieType.Normal:
                        switch (dice.NumSides)
                        {
                            case 4:
                                sb.Append(Dictionaries.d4[(int)dice.Value] + " ");
                                break;
                            case 6:
                                sb.Append(Dictionaries.d6[(int)dice.Value] + " ");
                                break;
                            case 8:
                                sb.Append(Dictionaries.d8[(int)dice.Value] + " ");
                                break;
                            case 10:
                                sb.Append(Dictionaries.d10[(int)dice.Value] + " ");
                                break;
                            case 12:
                                sb.Append(Dictionaries.d12[(int)dice.Value] + " ");
                                break;
                            case 20:
                                sb.Append(Dictionaries.d20[(int)dice.Value] + " ");
                                break;
                            default:
                                sb.Append(dice.Value);
                                break;
                        }
                        break;
                    case DieType.Special:
                        switch ((SpecialDie)dice.Value)
                        {
                            case SpecialDie.Add:
                                sb.Append("+ ");
                                break;
                            case SpecialDie.CloseParen:
                                sb.Append(") ");
                                break;
                            case SpecialDie.Comma:
                                sb.Append(", ");
                                break;
                            case SpecialDie.Divide:
                                sb.Append("/ ");
                                break;
                            case SpecialDie.Multiply:
                                sb.Append("* ");
                                break;
                            case SpecialDie.Negate:
                                sb.Append("- ");
                                break;
                            case SpecialDie.OpenParen:
                                sb.Append("(");
                                break;
                            case SpecialDie.Subtract:
                                sb.Append("- ");
                                break;
                            case SpecialDie.Text:
                                sb.Append(dice.Data);
                                break;
                        }
                        break;
                    case DieType.Fudge:
                        if (dice.Value == -1 || dice.Value == 0 || dice.Value == 1) sb.Append(Dictionaries.Dice[(int)dice.Value]+" ");
                        break;
                    default:
                        sb.Append(dice.Value + " ");
                        break;
                }
            }

            return sb.ToString().Trim();
        }
    }
    public class SkillAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Primary == null) return Choices;

            Actor actor = user.Primary;

            var Filtered = actor.Skills.Where(x => x.Key.ToLower().StartsWith(input.ToLower()));
            
            if(Filtered.Count() == 0) return Choices;

            foreach (var item in Filtered)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Key, item.Key));
            }
            return Choices;
        }
    }
    public class SkillSecondaryAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Secondary == null) return Choices;

            Actor actor = user.Secondary;

            var Filtered = actor.Skills.Where(x => x.Key.ToLower().StartsWith(input.ToLower()));

            if (Filtered.Count() == 0) return Choices;

            foreach (var item in Filtered)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Key, item.Key));
            }
            return Choices;
        }
    }
    public class AspectAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Primary == null) return Choices;

            Actor actor = user.Primary;

            var Filtered = actor.Aspects.Where(x => x.ToLower().StartsWith(input.ToLower()));

            if (Filtered.Count() == 0) return Choices;

            foreach (var item in Filtered)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item, item));
            }
            return Choices;
        }
    }
    public class StuntAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Primary == null) return Choices;

            Actor actor = user.Primary;

            var Filtered = actor.Stunts.Where(x => x.Name.ToLower().StartsWith(input.ToLower()));

            if (Filtered.Count() == 0) return Choices;

            foreach (var item in Filtered)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Name, item.Name));
            }
            return Choices;
        }
    }
    public class ConsequenceAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Primary == null) return Choices;

            Actor actor = user.Primary;

            var Filtered = actor.Consequences.Where(x => x.Value.ToLower().StartsWith(input.ToLower()));

            if (Filtered.Count() == 0) return Choices;

            foreach (var item in Filtered)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Value, item.Key));
            }
            return Choices;
        }
    }
    public class ActorAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            var col = database.GetCollection<Actor>("Actors");

            var Actors = col.Find(x=> x.Server == ctx.Guild.Id && x.Name.StartsWith(input.ToLower()));

            if(Actors.Count() == 0) return Choices;

            foreach(var item in Actors)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Name, item.Name));
            }

            return Choices;
        }
    }
    public class GMActorAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Campaign == null) return Choices;

            var Actors = user.Campaign.Actors.Where(x => x.Name.ToLower().StartsWith(input.ToLower()));

            if (Actors.Count() == 0) return Choices;

            foreach (var item in Actors)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Name, item.Name));
            }

            return Choices;
        }
    }
    public class OwnedActorAutoComplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            var col = database.GetCollection<Actor>("Actors");

            var Actors = col.Find(x => x.Owner == ctx.User.Id && x.Server == ctx.Guild.Id && x.Name.StartsWith(input.ToLower()));

            if (Actors.Count() == 0) return Choices;

            foreach (var item in Actors)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Name, item.Name));
            }

            return Choices;
        }
    }
    public class CampaignAutoComplete : IAutocompleteProvider
    {

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();
            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            var col = database.GetCollection<Campaign>("Campaigns");

            var Games = col.Find(x => x.Server == ctx.Guild.Id && x.Name.StartsWith(input.ToLower()));

            if (Games.Count() == 0) return Choices;

            foreach (var item in Games)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Name, item.Name));
            }

            return Choices;
        }
    }
    public class OwnedCampaignAutoComplete : IAutocompleteProvider
    {

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();
            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            var col = database.GetCollection<Campaign>("Campaigns");

            var Games = col.Find(x => x.Owner == ctx.User.Id && x.Server == ctx.Guild.Id && x.Name.StartsWith(input.ToLower()));

            if (Games.Count() == 0) return Choices;

            foreach (var item in Games)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Name, item.Name));
            }

            return Choices;
        }
    }
    public class CampaignSkillAutoComplete : IAutocompleteProvider
    {

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Campaign == null) return Choices;

            var skills = user.Campaign.SkillTemplate.Where(x => x.Key.ToLower().StartsWith(input.ToLower()));

            if (skills.Count() == 0) return Choices;

            foreach (var item in skills)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item.Key, item.Key));
            }

            return Choices;
        }
    }
    public class CampaignAspectAutoComplete : IAutocompleteProvider
    {

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            LiteDatabase database = ctx.Services.GetService<LiteDatabase>();

            Utilities utils = ctx.Services.GetService<Utilities>();

            var Choices = new List<DiscordAutoCompleteChoice>();

            string input = ctx.OptionValue as string;

            if (input.NullorEmpty()) return Choices;

            User user = utils.GetUser(ctx.User.Id);

            if (user.Campaign == null) return Choices;

            var Aspects = user.Campaign.Aspects.Where(x => x.ToLower().StartsWith(input.ToLower()));

            if (Aspects.Count() == 0) return Choices;

            foreach (var item in Aspects)
            {
                Choices.Add(new DiscordAutoCompleteChoice(item, item));
            }

            return Choices;
        }
    }
}
