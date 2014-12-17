using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GamesWebAPI
{
    public class Collection
    {
        public string Username { get; set; }
        public List<CollectionItem> Games { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public class CollectionItem
    {
        public string GameId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }

        public bool IsExpansion { get; set; }
        public int YearPublished { get; set; }

        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int PlayingTime { get; set; }
        public List<string> Mechanics { get; set; }

        public decimal? BGGRating { get; set; }
        public decimal? AverageRating { get; set; }
        public int? Rank { get; set; }

        public List<string> Designers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Artists { get; set; }

        public int NumPlays { get; set; }
        public decimal? Rating { get; set; }

        public bool Owned { get; set; }
        public bool PreOrdered { get; set; }
        public bool ForTrade { get; set; }
        public bool PreviousOwned { get; set; }
        public bool Want { get; set; }
        public bool WantToBuy { get; set; }
        public bool WantToPlay { get; set; }
        public bool WishList { get; set; }

        public string UserComment { get; set; }

        public List<CollectionItem> Expansions { get; set; }

        public CollectionItem Clone()
        {
            return new CollectionItem
            {
                GameId = this.GameId,
                Name = this.Name,
                Description = this.Description,
                Image = this.Image,
                Thumbnail = this.Thumbnail,
                MinPlayers = this.MinPlayers,
                MaxPlayers = this.MaxPlayers,
                PlayingTime = this.PlayingTime,
                Mechanics = this.Mechanics,
                IsExpansion = this.IsExpansion,
                YearPublished = this.YearPublished,
                BGGRating = this.BGGRating,
                AverageRating = this.AverageRating,
                Rank = this.Rank,
                Designers = this.Designers,
                Publishers = this.Publishers,
                Artists = this.Artists,
                NumPlays = this.NumPlays,
                Rating = this.Rating,
                Owned = this.Owned,
                PreviousOwned = this.PreviousOwned,
                PreOrdered = this.PreOrdered,
                ForTrade = this.ForTrade,
                Want = this.Want,
                WantToPlay = this.WantToPlay,
                WantToBuy = this.WantToBuy,
                WishList = this.WishList,
                UserComment = this.UserComment,
                Expansions = this.Expansions,
            };
        }

    }

    public class Plays
    {
        public string Username { get; set; }
        public List<PlayItem> Items { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public class PlayItem
    {
        public string GameId { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }
        public DateTime? PlayDate { get; set; }
        public int NumPlays { get; set; }
        public string Comments { get; set; }
    }

    public class GameDetails
    {
        public string GameId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Thumbnail { get; set; }

        public int? MinPlayers { get; set; }
        public int? MaxPlayers { get; set; }
        public int? PlayingTime { get; set; }
        public List<string> Mechanics { get; set; }

        public bool IsExpansion { get; set; }
        public int? YearPublished { get; set; }

        public decimal? BggRating { get; set; }
        public decimal? AverageRating { get; set; }
        public int? Rank { get; set; }

        public List<string> Designers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Artists { get; set; }

        public List<BoardGameLink> Expansions { get; set; }
        public List<BoardGameLink> Expands { get; set; }

    }

    public class Comment
    {
        public string Username { get; set; }
        public decimal Rating { get; set; }
        public string Text { get; set; }
    }

    public class PlayerPollResult
    {
        public int NumPlayers { get; set; }
        public int Best { get; set; }
        public int Recommended { get; set; }
        public int NotRecommended { get; set; }

        public bool NumPlayersIsAndHigher { get; set; }
    }

    public class BoardGameLink
    {
        public string Name { get; set; }
        public string GameId { get; set; }
    }

}