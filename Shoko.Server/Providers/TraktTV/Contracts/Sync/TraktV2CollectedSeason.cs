﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Shoko.Server.Providers.TraktTV.Contracts.Sync
{
    [DataContract]
    public class TraktV2CollectedSeason
    {
        [DataMember(Name = "number")]
        public int number { get; set; }

        [DataMember(Name = "episodes")]
        public List<TraktV2CollectedEpisode> episodes { get; set; }

        public override string ToString()
        {
            if (episodes != null)
                return string.Format("Season: {0} - Episodes Collected: {1}", number, episodes.Count);
            else
                return string.Format("Season: {0}", number);
        }
    }
}