using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Form1 : Form
    {
        private ServerApi ServerApi { get; }

        public Form1(ServerApi serverApi)
        {
            ServerApi = serverApi;

            InitializeComponent();

            UpdateConnectionStatus(serverApi.Connected);

            serverApi.OnPostConnect += OnServerApiOnOnPostConnect;
            serverApi.OnPostDisconnect += OnServerApiOnOnPostDisconnect;
            serverApi.OnPostUpdatePlayingTrack += RefreshListenersListAsync;
            serverApi.OnPostClearPlayingTrack += RefreshListenersListAsync;
            serverApi.OnPostUpdateListenerStates += RefreshListenersListAsync;

            // Remember to unsubscribe to the previous events
            Closed += (_, _) =>
            {
                serverApi.OnPostConnect -= OnServerApiOnOnPostConnect;
                serverApi.OnPostDisconnect -= OnServerApiOnOnPostDisconnect;
                serverApi.OnPostUpdatePlayingTrack -= RefreshListenersListAsync;
                serverApi.OnPostClearPlayingTrack -= RefreshListenersListAsync;
                serverApi.OnPostUpdateListenerStates -= RefreshListenersListAsync;
            };

            _ = serverApi.UpdateListenerStates();

            return;

            void OnServerApiOnOnPostConnect()
            {
                BeginInvoke(() =>
                {
                    UpdateConnectionStatus(true);
                    RefreshListenersList();
                });
            }

            void OnServerApiOnOnPostDisconnect()
            {
                BeginInvoke(() =>
                {
                    UpdateConnectionStatus(false);
                    RefreshListenersList();
                });
            }
        }

        public void RefreshListenersListAsync() => BeginInvoke(RefreshListenersList); // TODO - Don't update the listeners list if it didn't change

        public void RefreshListenersList()
        {
            string previousSelectedNodeUser = treeView1.SelectedNode?.Tag as string;
            treeView1.SelectedNode = null;

            TreeNodeCollection rootNodes = treeView1.Nodes;
            rootNodes.Clear();

            List<ListenerSharedState> queueOwners = [];
            List<ListenerSharedState> listeners = [];
            foreach (ListenerSharedState listener in ServerApi.ListenerSharedStates)
            {
                if (listener.QueueOwner == null)
                    queueOwners.Add(listener);
                else
                    listeners.Add(listener);
            }

            TreeNode previousSelectedNode = null;

            foreach (ListenerSharedState queueOwner in queueOwners)
            {
                string nodeText = queueOwner.Username;
                if (!queueOwner.State.IsIdle())
                {
                    nodeText += $" - '{queueOwner.State.TrackTitle}' from '{queueOwner.State.TrackAlbum}'";
                    if (!string.IsNullOrWhiteSpace(queueOwner.State.TrackArtists))
                        nodeText += $" by '{queueOwner.State.TrackArtists}'";
                }

                TreeNode rootNode = rootNodes.Add(nodeText);
                rootNode.Tag = queueOwner.Username;

                if (previousSelectedNode == null && queueOwner.Username == previousSelectedNodeUser)
                    previousSelectedNode = rootNode;

                if (queueOwner.Username == ServerApi.LocalSharedState.QueueOwner)
                    treeView1.SelectedNode = rootNode;

                foreach (ListenerSharedState listener in listeners)
                {
                    if (listener.QueueOwner != queueOwner.Username)
                        continue;

                    TreeNode child = rootNode.Nodes.Add(listener.Username);
                    child.Tag = listener.Username;
                }

                listeners.RemoveAll(l => l.QueueOwner == queueOwner.Username);
            }

            treeView1.ExpandAll();

            if (previousSelectedNodeUser != null)
                treeView1.SelectedNode = previousSelectedNode;

            UpdateJoinButton();

            Refresh();
        }

        public void UpdateConnectionStatus(bool connected)
        {
            label1.Text = $"Status: {(connected ? "CONNECTED" : "DISCONNECTED")}";

            // Disable the Reconnect and enable the Refresh button if we're already connected
            button1.Enabled = !connected;
            button2.Enabled = connected;

            Refresh();
        }

        private void UpdateJoinButton()
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                button3.Enabled = false;
                return;
            }

            button3.Enabled = CanJoinQueue((string) selectedNode.Tag);
        }

        private bool CanJoinQueue(string queueOwner)
        {
            ListenerSharedState localState = ServerApi.LocalSharedState;
            return queueOwner != localState.Username && queueOwner != localState.QueueOwner;
        }

        public async Task<bool> JoinQueue(string username)
        {
            if (!CanJoinQueue(username))
                return false;

            button4.Enabled = true;

            // Joining the queue will update the states
            return await ServerApi.JoinListeningQueue(username);
        }

        public async Task LeaveQueue()
        {
            button4.Enabled = false;

            await ServerApi.LeaveListeningQueue();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Connect to the server. If it fails, leave the button like this. If it works, disable it.
            ServerApi.Connect().ContinueWith(task =>
            {
                if (task.IsCanceled || !task.Result)
                    return;

                button1.Enabled = false;
                UpdateConnectionStatus(true);
                RefreshListenersList();
            });
        }

        private void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Go duoQ with root node
            _ = JoinQueue((string) e.Node.Tag);
            // FIXME - Currently we can join the queue of someone who's already in a queue
        }

        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            // Cancel selection and instead select root node
            TreeNode node = e.Node;
            TreeNode parent = node.Parent;

            if (parent == null)
                return;

            e.Cancel = true;

            treeView1.SelectedNode = parent;
        }

        private void button2_Click(object sender, EventArgs e) => _ = ServerApi.UpdateListenerStates();

        private void button3_Click(object sender, EventArgs e) => _ = JoinQueue((string) treeView1.SelectedNode.Tag);

        private void button4_Click(object sender, EventArgs e) => _ = LeaveQueue();

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateJoinButton();

            // Enable/Disable button4 according to whether the user is already in a queue
            button4.Enabled = ServerApi.InQueue;
        }
    }
}
