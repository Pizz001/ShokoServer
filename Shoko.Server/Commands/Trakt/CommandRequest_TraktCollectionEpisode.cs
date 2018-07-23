﻿using System;
using System.Xml;
using Shoko.Commons.Queue;
using Shoko.Models.Queue;
using Shoko.Models.Server;
using Shoko.Server.Models;
using Shoko.Server.Providers.TraktTV;
using Shoko.Server.Repositories;

namespace Shoko.Server.Commands
{
    [Serializable]
    [Command(CommandRequestType.Trakt_EpisodeCollection)]
    public class CommandRequest_TraktCollectionEpisode : CommandRequestImplementation
    {
        public int AnimeEpisodeID { get; set; }
        public int Action { get; set; }

        public TraktSyncAction ActionEnum => (TraktSyncAction) Action;

        public override CommandRequestPriority DefaultPriority => CommandRequestPriority.Priority9;

        public override QueueStateStruct PrettyDescription => new QueueStateStruct
        {
            queueState = QueueStateEnum.SyncTraktEpisodes,
            extraParams = new[] {AnimeEpisodeID.ToString(), Action.ToString()}
        };

        public CommandRequest_TraktCollectionEpisode()
        {
        }

        public CommandRequest_TraktCollectionEpisode(int animeEpisodeID, TraktSyncAction action)
        {
            AnimeEpisodeID = animeEpisodeID;
            Action = (int) action;
            Priority = (int) DefaultPriority;

            GenerateCommandID();
        }

        public override void ProcessCommand()
        {
            logger.Info("Processing CommandRequest_TraktCollectionEpisode: {0}-{1}", AnimeEpisodeID, Action);

            try
            {
                if (!ServerSettings.Instance.Trakt_IsEnabled || string.IsNullOrEmpty(ServerSettings.Instance.Trakt_AuthToken)) return;

                SVR_AnimeEpisode ep = Repo.AnimeEpisode.GetByID(AnimeEpisodeID);
                if (ep != null)
                {
                    TraktSyncType syncType = TraktSyncType.CollectionAdd;
                    if (ActionEnum == TraktSyncAction.Remove) syncType = TraktSyncType.CollectionRemove;
                    TraktTVHelper.SyncEpisodeToTrakt(ep, syncType);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error processing CommandRequest_TraktCollectionEpisode: {0} - {1} - {2}", AnimeEpisodeID,
                    Action,
                    ex);
            }
        }

        /// <summary>
        /// This should generate a unique key for a command
        /// It will be used to check whether the command has already been queued before adding it
        /// </summary>
        public override void GenerateCommandID()
        {
            CommandID = $"CommandRequest_TraktCollectionEpisode{AnimeEpisodeID}-{Action}";
        }

        public override bool LoadFromDBCommand(CommandRequest cq)
        {
            CommandID = cq.CommandID;
            CommandRequestID = cq.CommandRequestID;
            Priority = cq.Priority;
            CommandDetails = cq.CommandDetails;
            DateTimeUpdated = cq.DateTimeUpdated;

            // read xml to get parameters
            if (CommandDetails.Trim().Length > 0)
            {
                XmlDocument docCreator = new XmlDocument();
                docCreator.LoadXml(CommandDetails);

                // populate the fields
                AnimeEpisodeID =
                    int.Parse(TryGetProperty(docCreator, "CommandRequest_TraktCollectionEpisode", "AnimeEpisodeID"));
                Action = int.Parse(TryGetProperty(docCreator, "CommandRequest_TraktCollectionEpisode", "Action"));
            }

            return true;
        }

        public override CommandRequest ToDatabaseObject()
        {
            GenerateCommandID();

            CommandRequest cq = new CommandRequest
            {
                CommandID = CommandID,
                CommandType = CommandType,
                Priority = Priority,
                CommandDetails = ToXML(),
                DateTimeUpdated = DateTime.Now
            };
            return cq;
        }
    }
}