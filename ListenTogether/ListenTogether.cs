using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using JetBrains.Annotations;
using Timer = System.Threading.Timer;

namespace MusicBeePlugin
{
    [UsedImplicitly]
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private readonly PluginInfo about = new();

        private readonly ServerApi serverApi;

        public Plugin() => serverApi = new(this);

        private Form1 form;
        private Timer refreshListeningStatesTimer;

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

            mbApiInterface.MB_AddMenuItem("mnuTools/Listen Together", null, MenuClicked);
            
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
            refreshListeningStatesTimer.Dispose();
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
                    _ = serverApi.Connect();
                    refreshListeningStatesTimer = new(_ => serverApi.UpdateListenerStates().Wait(), null, ServerApi.AutoRefreshTime, ServerApi.AutoRefreshTime);

                    bool result = mbApiInterface.Library_QueryFiles("Plague Tale");
                    bool result2 = mbApiInterface.Library_QueryFilesEx("Plague Tale", out string[] files);
                    break;
                
                case NotificationType.TrackChanged:
                    if (serverApi.Connected)
                        _ = serverApi.UpdatePlayingTrack();
                    break;
                
                case NotificationType.PlayStateChanged:
                    if (serverApi.Connected)
                    {
                        switch (mbApiInterface.Player_GetPlayState())
                        {
                            case PlayState.Playing:
                                _ = serverApi.UpdatePlayingTrack().ContinueWith(_ => form?.RefreshListenersList());
                                break;
                            
                            case PlayState.Paused:
                                _ = serverApi.ClearPlayingTrack().ContinueWith(_ => form?.RefreshListenersList());
                                break;
                        }
                    }
                    break;
            }
        }

        private void MenuClicked(object sender, EventArgs args)
        {
            form ??= new(serverApi);
            form.Show();
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        //public string[] GetProviders()
        //{
        //    return null;
        //}

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        //public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        //{
        //    return null;
        //}

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        //public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        //{
        //    //Return Convert.ToBase64String(artworkBinaryData)
        //    return null;
        //}

        //  presence of this function indicates to MusicBee that this plugin has a dockable panel. MusicBee will create the control and pass it as the panel parameter
        //  you can add your own controls to the panel if needed
        //  you can control the scrollable area of the panel using the mbApiInterface.MB_SetPanelScrollableArea function
        //  to set a MusicBee header for the panel, set about.TargetApplication in the Initialise function above to the panel header text
        [UsedImplicitly]
        public int OnDockablePanelCreated(Control panel)
        {
            //    return the height of the panel and perform any initialisation here
            //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
            //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
            //    = 0 indicates to MusicBee this control resizeable
            //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
            float dpiScaling = 0;
            using (Graphics g = panel.CreateGraphics())
            {
                dpiScaling = g.DpiY / 96f;
            }
            panel.Paint += panel_Paint;
            return Convert.ToInt32(100 * dpiScaling);
        }

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        //public List<ToolStripItem> GetHeaderMenuItems()
        //{
        //    List<ToolStripItem> list = new List<ToolStripItem>();
        //    list.Add(new ToolStripMenuItem("A menu item"));
        //    return list;
        //}

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Red);
            TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        }

        private static string XmlFilter(string[] tags, string query, bool isStrict,
            SearchSource source = SearchSource.None)
        {
            short src;
            if (source != SearchSource.None)
            {
                src = (short) source;
            }
            else
            {
                var userDefaults = UserSettings.Instance.Source != SearchSource.None;
                src = (short)
                    (userDefaults
                        ? UserSettings.Instance.Source
                        : SearchSource.Library);
            }


            var filter = new XElement("Source",
                new XAttribute("Type", src));

            var conditions = new XElement("Conditions",
                new XAttribute("CombineMethod", "Any"));
            foreach (var tag in tags)
            {
                var condition = new XElement("Condition",
                    new XAttribute("Field", tag),
                    new XAttribute("Comparison", isStrict ? "Is" : "Contains"),
                    new XAttribute("Value", query));
                conditions.Add(condition);
            }
            filter.Add(conditions);

            return filter.ToString();
        }

        public ListeningState GetListeningState()
        {
            return new()
            {
                TrackTitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle),
                TrackAlbum = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Album),
                TrackArtists = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artists),
                FileUrl = mbApiInterface.NowPlaying_GetFileUrl(),
                Position = mbApiInterface.Player_GetPosition(),
                Time = DateTime.Now
            };
        }

        public void SetListeningState(ListeningState newState)
        {
            mbApiInterface.Player_SetRepeat(RepeatMode.None);
            mbApiInterface.Player_SetShuffle(false);
            mbApiInterface.NowPlayingList_PlayNow(newState.FileUrl);
            mbApiInterface.Player_SetPosition(newState.Position);
        }
    }
}
