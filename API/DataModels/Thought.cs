using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace API.DataModels
{
    public class Thought
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ThoughtId { get; set; } // Unique identifier for the thought (MongoDB ObjectId)

        public Content Content { get; set; } // The main content of the thought
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Timestamp when the thought was created
        public DateTime UpdatedAt { get; set; } = DateTime.Now;// Timestamp when the thought was last updated
        public List<Reminder> Reminders { get; set; } // Array of reminder objects
        public List<Interaction> Interactions { get; set; } // Array of interaction objects
        public string Status { get; set; } // Status of the thought (e.g., "active", "completed", "dismissed")
    }

    public class Content
    {
        public string Text { get; set; } // Text content of the thought
        public string VoiceMemoUrl { get; set; } // URL to the recorded voice memo (if applicable)
        public string ImageUrl { get; set; } // URL to any associated image (if applicable)
        public List<string> Links { get; set; } // Array of URLs or links related to the thought
        public List<string> ListItems { get; set; } // Array of items if it's a list (e.g., shopping list)
    }

    public class Reminder
    {
        public string ReminderId { get; set; } // Unique identifier for the reminder
        public Trigger Trigger { get; set; } // Trigger details
        public DateTime NotificationTime { get; set; } // When the notification should be sent
    }

    public class Trigger
    {
        public string Type { get; set; } // Type of trigger (e.g., "time", "location", "website")
        public string Value { get; set; } // Value for the trigger (e.g., specific time, URL, etc.)
        public bool IsRecurring { get; set; } // Whether the reminder is recurring
        public string Frequency { get; set; } // Frequency if recurring (e.g., "daily", "weekly")
    }

    public class Interaction
    {
        public string InteractionId { get; set; } // Unique identifier for the interaction
        public string Type { get; set; } // Type of interaction (e.g., "query", "update", "delete")
        public DateTime Timestamp { get; set; } // When the interaction occurred
        public string Details { get; set; } // Additional details about the interaction
    }
}