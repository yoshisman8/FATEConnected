using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FATEConnected.Model
{
    public class RollString
    {
        public int ActorId { get; set; }
        public int[] Dice { get; set; }
        public int Bonus { get; set; }
        public string Skill { get; set; }

        public string Serialize()
        {
            return $"{ActorId};{string.Join('.',Dice)};{Bonus.ToString()};{Skill.ToString()}";
        }
        public RollString Deserialize(string input)
        {
            string[] args = input.Split(';');
            return new RollString()
            {
                ActorId = int.Parse(args[0]),
                Dice = args[1].Split('.').Select(x => int.Parse(x)).ToArray(),
                Bonus = int.Parse(args[2]),
                Skill = args[3]
            };
        }
    }
}
