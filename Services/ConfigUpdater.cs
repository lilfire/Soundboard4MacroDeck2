﻿using Soundboard4MacroDeck.Actions;
using Soundboard4MacroDeck.Models;
using SuchByte.MacroDeck.Backups;
using SuchByte.MacroDeck.Notifications;
using SuchByte.MacroDeck.Plugins;
using SuchByte.MacroDeck.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Soundboard4MacroDeck.Services;
internal class ConfigUpdater
{
    public static void MoveToContext()
    {
        bool hasActions = ProfileManager.Profiles.Any(profile => profile.Folders.Any(folder => folder.ActionButtons.Any()));
        if (hasActions)
        {
            SoundboardContext.RemoveBackupCreationHook();
            PluginInstance.DbContext.Db.Close();
            BackupManager.CreateBackup();
            PluginInstance.DbContext = new SoundboardContext();

            var db = PluginInstance.DbContext;
            db.Db.RunInTransaction(() =>
            {
                db.InsertAudioCategory(new AudioCategory { Name = "Default category" });
                foreach (var profile in ProfileManager.Profiles)
                {
                    foreach (var folder in profile.Folders)
                    {
                        foreach (var actionButton in folder.ActionButtons)
                        {
                            foreach (var action in actionButton.Actions)
                            {
                                ChangeConfiguration(action, db);
                            }
                            foreach (var action in actionButton.ActionsLongPress)
                            {
                                ChangeConfiguration(action, db);
                            }
                            foreach (var action in actionButton.ActionsLongPressRelease)
                            {
                                ChangeConfiguration(action, db);
                            }
                            foreach (var action in actionButton.ActionsRelease)
                            {
                                ChangeConfiguration(action, db);
                            }

                            foreach (var action in actionButton.EventListeners.SelectMany(eventListener => eventListener.Actions))
                            {
                                ChangeConfiguration(action, db);
                            }
                        }
                    }
                }
                ProfileManager.Save();
            });
            SoundboardContext.AddBackupCreationHook();
            filesAdded.Clear();
            filesAdded = null;

            NotificationManager.Notify(PluginInstance.Current, "SoundBoard Upgrade", "A major update was performed. A backup has been made.");
        }
    }

    private static readonly Type[] ActionTypes =
        {
            typeof(SoundboardPlayAction),
            typeof(SoundboardPlayStopAction),
            typeof(SoundboardOverlapAction),
            typeof(SoundboardLoopAction),
        };

    private static Dictionary<string, int> filesAdded = new();

    private static void ChangeConfiguration(PluginAction action, SoundboardContext db)
    {
        if (ActionTypes.Contains(action.GetType()))
        {
            var actionParametersLegacy = ActionParameters.Deserialize(action.Configuration);
            var data = BitConverter.ToString(actionParametersLegacy.FileData);
            if (!filesAdded.TryGetValue(data, out var entryId))
            {
                entryId = db.InsertAudioFile(new AudioFile { Data = actionParametersLegacy.FileData, Name = actionParametersLegacy.FileName, CategoryId = 1 });
                filesAdded.Add(data, entryId);
            }

            var actionParameters = new ActionParametersV2
            {
                FileName = actionParametersLegacy.FileName,
                Volume = actionParametersLegacy.Volume,
                UseDefaultDevice = actionParametersLegacy.UseDefaultDevice,
                OutputDeviceId = actionParametersLegacy.OutputDeviceId,
                SyncButtonState = actionParametersLegacy.SyncButtonState,
                AudioFileId = entryId
            };
            action.ConfigurationSummary = $"{actionParameters.AudioFileId} - {actionParameters.FileName}";
            action.Configuration = actionParameters.Serialize();

        }
    }
}
