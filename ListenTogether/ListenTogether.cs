using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using JetBrains.Annotations;
using Timer = System.Threading.Timer;

namespace MusicBeePlugin;

[UsedImplicitly]
public partial class Plugin
{
    private MusicBeeApiInterface mbApiInterface;
    private readonly PluginInfo about = new();

    private ServerApi serverApi;

    private Timer refreshListeningStatesTimer;

    private string[] lastPlayingList;
    private RepeatMode lastRepeatState;
    private bool lastShuffleState;

    [UsedImplicitly]
    public PluginInfo Initialise(IntPtr apiInterfacePtr)
    {
        mbApiInterface = new();
        mbApiInterface.Initialise(apiInterfacePtr);
        about.PluginInfoVersion = PluginInfoVersion;
        about.Name = "MusicBee Listen Together";
        about.Description = "Monkey together strong";
        about.Author = "BloodLantern, YohannDR";
        about.TargetApplication = "Listen Together";   //  the name of a Plugin Storage device or panel header for a dockable panel
        about.Type = PluginType.General;
        about.VersionMajor = 1;  // your plugin version
        about.VersionMinor = 0;
        about.Revision = 1;
        about.MinInterfaceVersion = MinInterfaceVersion;
        about.MinApiRevision = MinApiRevision;
        about.ReceiveNotifications = ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents;
        about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

        mbApiInterface.MB_AddMenuItem("mnuTools/Listen Together", null, ListenTogetherMenu);
        mbApiInterface.MB_AddMenuItem("mnuTools/Update Git Repository", null, UpdateGitRepository);

        return about;
    }

    [UsedImplicitly]
    public bool Configure(IntPtr panelHandle)
    {
        // save any persistent settings in a sub-folder of this path
        string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
        // keep in mind the panel width is scaled according to the font the user has selected
        // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
        if (panelHandle != IntPtr.Zero)
        {
            Panel configPanel = (Panel)Control.FromHandle(panelHandle);
            Label prompt = new();
            prompt.AutoSize = true;
            prompt.Location = new(0, 0);
            prompt.Text = "prompt:";
            TextBox textBox = new();
            textBox.Bounds = new(60, 0, 100, textBox.Height);
            configPanel.Controls.AddRange([prompt, textBox]);
        }
        return false;
    }

    // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
    // its up to you to figure out whether anything has changed and needs updating
    [UsedImplicitly]
    public void SaveSettings()
    {
        // save any persistent settings in a sub-folder of this path
        string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
    }

    // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
    [UsedImplicitly]
    public void Close(PluginCloseReason reason)
    {
        refreshListeningStatesTimer?.Dispose();
        _ = serverApi.Disconnect();
    }

    // uninstall this plugin - clean up any persisted files
    [UsedImplicitly]
    public void Uninstall()
    {
    }

    // receive event notifications from MusicBee
    // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
    [UsedImplicitly]
    public void ReceiveNotification(string sourceFileUrl, NotificationType type)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (type)
        {
            case NotificationType.PluginStartup:
                const string AnyFileQuery = """
                                             <SmartPlaylist>
                                               <Source Type="1">
                                                 <Conditions CombineMethod="All">
                                                   <Condition Field="Title" Comparison="IsNotNull" />
                                                 </Conditions>
                                                 <Limit FilterDuplicates="False" Enabled="True" Count="1" Type="Items" SelectedBy="Random" />
                                               </Source>
                                             </SmartPlaylist>
                                             """;
                mbApiInterface.Library_QueryFilesEx(AnyFileQuery, out string[] files);
                if (files.Length > 0)
                {
                    string file = files[0]; // files.Length should be 1
                    GitCommands.RepositoryPath = Path.GetDirectoryName(file);
                }

                serverApi = new(this);

                serverApi.OnPostConnect += () => refreshListeningStatesTimer = new(
                    async void (_) =>
                    {
                        try
                        {
                            if (serverApi.Connected)
                                await serverApi.UpdateListenerStates();
                        }
                        catch (Exception)
                        {
                            // We don't care if the operation fails, we still want to do it repetitively
                        }
                    },
                    null,
                    ServerApi.AutoRefreshTime,
                    ServerApi.AutoRefreshTime
                );

                serverApi.OnPreDisconnect += () => refreshListeningStatesTimer.Dispose();

                serverApi.OnPreJoinListeningQueue += () =>
                {
                    mbApiInterface.NowPlayingList_QueryFilesEx(null, out lastPlayingList);
                    lastRepeatState = mbApiInterface.Player_GetRepeat();
                    lastShuffleState = mbApiInterface.Player_GetShuffle();
                };

                serverApi.OnPostLeaveListeningQueue += () =>
                {
                    mbApiInterface.NowPlayingList_Clear();
                    mbApiInterface.NowPlayingList_QueueFilesNext(lastPlayingList);
                    mbApiInterface.Player_SetRepeat(lastRepeatState);
                    mbApiInterface.Player_SetShuffle(lastShuffleState);
                };

                _ = serverApi.Connect();
                break;

            case NotificationType.TrackChanged:
                if (serverApi.Connected)
                    _ = serverApi.UpdatePlayingTrack();
                break;

            case NotificationType.PlayStateChanged:
                if (serverApi.Connected)
                {
                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    // ReSharper disable once ConvertSwitchStatementToSwitchExpression
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                            _ = serverApi.UpdatePlayingTrack();
                            break;

                        case PlayState.Paused:
                            _ = serverApi.ClearPlayingTrack();
                            break;
                    }
                }
                break;
        }
    }

    private void ListenTogetherMenu(object sender, EventArgs args)
    {
        Form1 form = new(serverApi);
        form.Show();
    }

    private static void UpdateGitRepository(object sender, EventArgs args) => GitCommands.UpdateRepository();

    public ListeningState GetListeningState()
    {
        return new()
        {
            TrackTitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle),
            TrackAlbum = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Album),
            TrackArtists = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artists),
            Position = mbApiInterface.Player_GetPosition(),
            Time = DateTime.Now
        };
    }

    private void UpdatePlayerPosition(ListeningState targetState, bool checkOffset)
    {
        int targetPosition = targetState.Position + (int) (DateTime.Now - targetState.Time).TotalMilliseconds;
        // Only update the player position if it is offset of more than 5s
        if (!checkOffset || Math.Abs(mbApiInterface.Player_GetPosition() - targetPosition) > 5000)
            mbApiInterface.Player_SetPosition(targetPosition);
    }

    public void SetListeningState(ListeningState newState)
    {
        if (!serverApi.LocalSharedState.State.IsDifferentTrackFrom(newState))
        {
            UpdatePlayerPosition(newState, true);
            return;
        }

        string query = $"""
                        <SmartPlaylist>
                          <Source Type="1">
                            <Conditions CombineMethod="All">
                              <Condition Field="Title" Comparison="Is" Value="{SecurityElement.Escape(newState.TrackTitle)}" />
                              <Condition Field="Album" Comparison="Is" Value="{SecurityElement.Escape(newState.TrackAlbum)}" />
                            </Conditions>
                          </Source>
                        </SmartPlaylist>
                        """;

        mbApiInterface.Library_QueryFilesEx(query, out string[] files);

        if (files.Length == 0)
            return;

        mbApiInterface.NowPlayingList_Clear();
        mbApiInterface.Player_SetRepeat(RepeatMode.None);
        mbApiInterface.Player_SetShuffle(false);
        mbApiInterface.NowPlayingList_PlayNow(files.First());

        UpdatePlayerPosition(newState, false);
    }
}
