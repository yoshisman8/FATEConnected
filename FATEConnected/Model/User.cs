using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace FATEConnected.Model
{
    public class User
    {
        [BsonId]
        public ulong Id { get; set; }

        [BsonRef("Actors")]
        
        public Actor Primary { get; set; } 
        [BsonRef("Actors")]
        public Actor Secondary { get; set; }

        [BsonRef("Campaigns")]
        public Campaign Campaign { get; set; }
    }
}
