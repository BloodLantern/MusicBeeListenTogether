using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Form1 : Form
    {
        private readonly ServerApi serverApi;

        public Form1(ServerApi serverApi)
        {
            this.serverApi = serverApi;

            InitializeComponent();

            UpdateStatusText(serverApi.Connected);

            if (!serverApi.Connected)
                return;

            // Disable the Reconnect button if we're already connected
            button1.Enabled = false;

            // Initialize treeView1 contents with the currently connected users and select the current queue owner (if applicable)
            RefreshListenersList().ContinueWith(refreshTask =>
            {
                if (!refreshTask.Result)
                    return;

                Invoke(() =>
                {
                    treeView1.ExpandAll();
                    Refresh();
                });
            });
        }

        public async Task<bool> RefreshListenersList()
        {
            TreeNodeCollection rootNodes = treeView1.Nodes;
            rootNodes.Clear();
            
            if (!await serverApi.UpdateListenerStates())
                return false;

            List<ListenerSharedState> listeners = new(serverApi.ListenerSharedStates);
            List<ListenerSharedState> queueOwners = new(serverApi.ListenerSharedStates.Where(l => l.QueueOwner == null));
            // Remove all queueOwners from listeners
            listeners.RemoveAll(l => queueOwners.Exists(qo => l.Username == qo.Username));

            foreach (ListenerSharedState queueOwner in queueOwners)
            {
                string nodeText = queueOwner.Username;
                if (!queueOwner.State.IsIdle())
                    nodeText += $" - {queueOwner.State.TrackTitle} from {queueOwner.State.TrackAlbum} by {queueOwner.State.TrackArtists}";

                TreeNode rootNode = rootNodes.Add(nodeText);
                rootNode.Tag = queueOwner.Username;

                foreach (ListenerSharedState listener in listeners)
                {
                    if (listener.QueueOwner != queueOwner.Username)
                        continue;

                    TreeNode child = rootNode.Nodes.Add(listener.Username);
                    child.Tag = listener.Username;
                    listeners.RemoveAll(l => l.Username == listener.Username);
                    break;
                }
            }

            return true;
        }

        public void UpdateStatusText(bool connected) => label1.Text = $"Status: {(connected ? "CONNECTED" : "DISCONNECTED")}";

        public async Task<bool> JoinQueue(string username) => await serverApi.JoinListeningQueue(username);

        public async Task<bool> LeaveQueue() => await serverApi.LeaveListeningQueue();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            _ = RefreshListenersList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Connect to the server. If it fails, leave the button like this. If it works, disable it.
            serverApi.Connect().ContinueWith(task =>
            {
                if (!task.Result)
                    return;
                
                button1.Enabled = false;
                UpdateStatusText(true);
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

        private void button2_Click(object sender, EventArgs e) => _ = RefreshListenersList();

        private void button3_Click(object sender, EventArgs e) => _ = JoinQueue((string) treeView1.SelectedNode.Tag);

        private void button4_Click(object sender, EventArgs e) => _ = LeaveQueue();
    }
}

