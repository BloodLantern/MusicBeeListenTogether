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
            RefreshListenersList().ContinueWith(_ =>
            {
                treeView1.ExpandAll();
                Refresh();
            });
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

        public async Task RefreshListenersList()
        {
            if (!await serverApi.UpdateListenerStates())
                return;

            TreeNodeCollection rootNodes = treeView1.Nodes;
            rootNodes.Clear();

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

                foreach (ListenerSharedState listener in listeners)
                {
                    if (listener.QueueOwner != queueOwner.Username)
                        continue;

                    rootNode.Nodes.Add(listener.Username);
                    listeners.RemoveAll(l => l.Username == listener.Username);
                    break;
                }
            }
        }

        public void UpdateStatusText(bool connected) => label1.Text = $"Status: {(connected ? "CONNECTED" : "DISCONNECTED")}";
    }
}

