// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct UserEventObject 
    {
        /// <summary>Unique id of the event object.</summary>
        internal readonly long Id;
        /// <summary>Unique id of the parent game.</summary>
        internal readonly long GameId;
        /// <summary>Unique id of the parent mod.</summary>
        internal readonly long ModId;
        /// <summary>Unique id of the user who performed the action.</summary>
        internal readonly long UserId;
        /// <summary>Unix timestamp of date the event occurred.</summary>
        internal readonly long DateAdded;
        /// <summary>Type of event that was triggered. List of possible events:<br/><br/>- USER_TEAM_JOIN<br/>- USER_TEAM_LEAVE<br/>- USER_SUBSCRIBE<br/>- USER_UNSUBSCRIBE</summary>
        internal readonly string EventType;

        /// <param name="id">Unique id of the event object.</param>
        /// <param name="gameId">Unique id of the parent game.</param>
        /// <param name="modId">Unique id of the parent mod.</param>
        /// <param name="userId">Unique id of the user who performed the action.</param>
        /// <param name="dateAdded">Unix timestamp of date the event occurred.</param>
        /// <param name="eventType">Type of event that was triggered. List of possible events:<br/><br/>- USER_TEAM_JOIN<br/>- USER_TEAM_LEAVE<br/>- USER_SUBSCRIBE<br/>- USER_UNSUBSCRIBE</param>
        [JsonConstructor]
        public UserEventObject(
            long id,
            long game_id,
            long mod_id,
            long user_id,
            long date_added,
            string event_type
        ) {
            Id = id;
            GameId = game_id;
            ModId = mod_id;
            UserId = user_id;
            DateAdded = date_added;
            EventType = event_type;
        }
    }
}
